using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;

namespace ESDeckPC
{
    /// <summary>
    /// Renders Canvas pages with WebView2, invisibly: the WebView lives in a
    /// borderless tool window parked far off-screen (no taskbar / Alt+Tab
    /// entry, never activated). Background throttling is switched off so
    /// the page keeps running and painting while nobody can see it.
    ///
    /// The viewport is forced to 720 x 480 CSS px at scale 1 (DevTools
    /// device metrics), so captures are exactly the ESP's canvas, whatever
    /// the monitor DPI. The Canvas folder is served as https://canvas.esdeck/.
    ///
    /// AttachTo() / Detach() move the same WebView into the PC's Canvas
    /// window and back (only one of the ESP and the PC shows Canvas).
    ///
    /// UI thread only (WebView2 requirement).
    /// </summary>
    public sealed class CanvasRenderer : IDisposable
    {
        public const int W = 720;
        public const int H = 480;
        public const string HostName = "canvas.esdeck";
        public const string HostUrl = "https://" + HostName + "/";

        // Time after NavigationCompleted before the first capture: web fonts,
        // images and the first script tick.
        private const int SettleMs = 400;
        private const int NavigateTimeoutMs = 8000;

        private HostForm _form;
        private CoreWebView2Controller _ctl;
        private CoreWebView2 _core;
        private Task _init;
        private double _scale = 1.0;     // view scale while shown in the Canvas window (layout stays 720 x 480)

        public event Action<string> OnLog;
        public bool IsReady => _core != null;
        public string CurrentUrl { get; private set; }

        private sealed class HostForm : Form
        {
            public HostForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(-10000, -10000);
                ClientSize = new Size(W, H);
                Text = "ESDeck Canvas";
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    const int WS_EX_TOOLWINDOW = 0x00000080;
                    const int WS_EX_NOACTIVATE = 0x08000000;
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    return cp;
                }
            }
        }

        /// <summary>Creates the WebView once; later calls return the same task.</summary>
        public Task InitAsync(object bridge) => _init ?? (_init = DoInitAsync(bridge));

        private async Task DoInitAsync(object bridge)
        {
            _form = new HostForm();
            _form.Show();

            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ESDeckPC", "WebView2");
            var opts = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments =
                    "--disable-features=CalculateNativeWinOcclusion " +
                    "--disable-background-timer-throttling " +
                    "--disable-renderer-backgrounding " +
                    "--disable-backgrounding-occluded-windows",
            };
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir, opts);

            _ctl = await env.CreateCoreWebView2ControllerAsync(_form.Handle);
            _ctl.Bounds = new Rectangle(0, 0, W, H);
            _ctl.IsVisible = true;
            _core = _ctl.CoreWebView2;

            var s = _core.Settings;
            s.AreDefaultContextMenusEnabled = false;
            s.IsZoomControlEnabled = false;
            s.IsStatusBarEnabled = false;
            s.AreDevToolsEnabled = true;     // Canvas > Open DevTools

            CanvasPaths.Ensure();
            _core.SetVirtualHostNameToFolderMapping(HostName, CanvasPaths.Root,
                                                    CoreWebView2HostResourceAccessKind.Allow);
            if (bridge != null) _core.AddHostObjectToScript("esdeck", bridge);

            // Every page gets window.esdeck (PC actions, log) and no
            // scrollbars / text selection -- it's a touch screen.
            await _core.AddScriptToExecuteOnDocumentCreatedAsync(EsdeckScript);
            await _core.AddScriptToExecuteOnDocumentCreatedAsync(TouchScreenScript);

            await SetViewportAsync();
            Log("WebView2 ready " + env.BrowserVersionString);
        }

        /// <summary>window.esdeck on top of the "esdeck" host object (a
        /// CanvasBridge). Also used by the editor preview (read-only bridge).</summary>
        internal const string EsdeckScript = @"
(() => {
  const host = () => chrome.webview.hostObjects.esdeck;
  window.esdeck = {
    action: a => host().Action(typeof a === 'string' ? a : JSON.stringify(a)),
    log:    m => host().Log(String(m)),
    stats:  async () => JSON.parse(await host().Stats()),
    media:  async () => JSON.parse(await host().Media()),
    mediaCover: () => host().MediaCover(),
    mediaCmd: c => host().MediaCmd(String(c)),
    fetch: async url => {   // GET via ESDeckPC (no CORS): { ok, status, text, error }
      const id = await host().FetchStart(String(url));
      for (;;) {
        const r = await host().FetchResult(id);
        if (r) return JSON.parse(r);
        await new Promise(res => setTimeout(res, 100));
      }
    },
  };
})();";

        /// <summary>No scrollbars / text selection, same as on the ESP.</summary>
        internal const string TouchScreenScript = @"
(() => {
  const css = '::-webkit-scrollbar{display:none} html,body{user-select:none;-webkit-user-select:none}';
  const add = () => { const st = document.createElement('style'); st.textContent = css; document.head.appendChild(st); };
  if (document.head) add(); else document.addEventListener('DOMContentLoaded', add);
})();";

        // scale: the 720 x 480 layout drawn smaller / larger (Canvas window
        // resized); always 1 while rendering for the ESP.
        private Task SetViewportAsync() =>
            _core == null ? Task.CompletedTask :
            _core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride",
                $"{{\"width\":{W},\"height\":{H},\"deviceScaleFactor\":1,\"mobile\":false," +
                $"\"scale\":{_scale.ToString("0.####", CultureInfo.InvariantCulture)}}}");

        /// <summary>Loads url (also when it's the current one = reload) and
        /// waits until it has loaded and settled.</summary>
        public async Task NavigateAsync(string url)
        {
            var done = new TaskCompletionSource<bool>();
            void handler(object s, CoreWebView2NavigationCompletedEventArgs e)
            {
                if (!e.IsSuccess) Log($"page load failed: {e.WebErrorStatus} ({url})");
                done.TrySetResult(e.IsSuccess);
            }
            _core.NavigationCompleted += handler;
            try
            {
                CurrentUrl = url;
                _core.Navigate(url);
                await Task.WhenAny(done.Task, Task.Delay(NavigateTimeoutMs));
            }
            finally
            {
                _core.NavigationCompleted -= handler;
            }
            await SetViewportAsync();
            await Task.Delay(SettleMs);
        }

        public async Task BlankAsync()
        {
            if (_core == null) return;
            CurrentUrl = null;
            _core.Navigate("about:blank");
            await Task.Yield();
        }

        /// <summary>Screenshot of the page, 720 x 480, 32bpp ARGB.</summary>
        public async Task<Bitmap> CaptureAsync()
        {
            string json = await _core.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",
                "{\"format\":\"png\",\"fromSurface\":true}");
            byte[] png = Convert.FromBase64String((string)JObject.Parse(json)["data"]);

            using (var ms = new MemoryStream(png))
            using (var src = new Bitmap(ms))
            {
                var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);
                    g.DrawImageUnscaled(src, 0, 0);
                }
                return bmp;
            }
        }

        // ------------------------------------------------------------------
        // Input (DevTools Input domain, page coordinates)
        // ------------------------------------------------------------------

        public Task MouseAsync(string type, int x, int y, int clickCount = 0)
        {
            string btn = type == "mouseMoved" ? "none" : "left";
            return _core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",
                $"{{\"type\":\"{type}\",\"x\":{x},\"y\":{y},\"button\":\"{btn}\",\"clickCount\":{clickCount}}}");
        }

        public Task WheelAsync(int x, int y, int deltaX, int deltaY)
        {
            return _core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",
                $"{{\"type\":\"mouseWheel\",\"x\":{x},\"y\":{y},\"deltaX\":{deltaX},\"deltaY\":{deltaY}}}");
        }

        public void OpenDevTools() => _core?.OpenDevToolsWindow();

        // ------------------------------------------------------------------
        // Shown on the PC (Canvas window) instead of off-screen
        // ------------------------------------------------------------------

        /// <summary>Moves the WebView into host (a visible panel, 3:2, any size):
        /// the same page keeps running, now with real mouse / keyboard input.
        /// Call Detach() before host's handle goes away.</summary>
        public void AttachTo(Control host)
        {
            if (_ctl == null || host == null) return;
            if (_ctl.ParentWindow != host.Handle) _ctl.ParentWindow = host.Handle;
            _ctl.IsVisible = true;
            FitTo(host);
        }

        /// <summary>Host resized: same 720 x 480 page, drawn at host's size
        /// (only while attached to it).</summary>
        public void FitTo(Control host)
        {
            if (_ctl == null || host == null || _ctl.ParentWindow != host.Handle) return;
            var size = host.ClientSize;
            if (size.Width < 1 || size.Height < 1) return;
            _ctl.Bounds = new Rectangle(0, 0, size.Width, size.Height);
            double s = size.Width / (double)W;
            if (Math.Abs(s - _scale) > 0.0005)
            {
                _scale = s;
                _ = SetViewportAsync();
            }
        }

        /// <summary>Back to the off-screen form (rendering for the ESP).</summary>
        public void Detach()
        {
            if (_ctl == null || _form == null) return;
            if (_ctl.ParentWindow != _form.Handle) _ctl.ParentWindow = _form.Handle;
            _ctl.Bounds = new Rectangle(0, 0, W, H);
            if (_scale != 1.0)
            {
                _scale = 1.0;            // captures for the ESP must be 720 x 480
                _ = SetViewportAsync();
            }
        }

        private void Log(string msg) => OnLog?.Invoke("Canvas: " + msg);

        public void Dispose()
        {
            try { _ctl?.Close(); } catch { }
            _ctl = null;
            _core = null;
            _form?.Dispose();
            _form = null;
        }
    }
}
