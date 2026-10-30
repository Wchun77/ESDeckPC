using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Canvas mode, PC side -- mirrors "Canvas mode" in ESDeck's usb_hid.h.
    ///
    /// The ESP asks for a page (entering the mode, a sidebar tap, or right
    /// after ESDeckPC connects); this loads it in the hidden WebView,
    /// captures it and sends it as a full batch (+ sidebar icons when it
    /// entered / reconnected). While the ESP stays in Canvas mode the page
    /// is captured every FrameIntervalMs and only the changed 16 px tiles
    /// are sent. Touches from the ESP become clicks (tap) or wheel scrolling
    /// (drag) in the page. Files under Canvas/ are watched: saving the page
    /// or the config shows up on the ESP right away.
    ///
    /// Canvas window (FormCanvasWindow): Canvas runs on one side at a time.
    /// While the ESP is in Canvas mode it owns the page and the window says
    /// so; when the ESP leaves (or disconnects) the same WebView moves into
    /// the window and keeps going there, until the ESP enters Canvas again.
    /// One page instance, so there's one state (localStorage, timers...).
    ///
    /// UI thread, except the HID writes (one batch at a time, background).
    /// </summary>
    public sealed class CanvasLink : IDisposable
    {
        // PC -> ESP (Feature report: [0] report ID, [1] cmd, [2] gen, [3..] payload)
        private const byte CMD_BEGIN = 0x20, CMD_REC = 0x21, CMD_DATA = 0x22, CMD_END = 0x23;
        private const byte CMD_CFG = 0x24;                  // one config list entry, [2] = EV_LIST seq
        private const byte CFG_F_ACTIVE = 0x01;
        private const int CfgNameMax = 58;                  // UTF-8 bytes (+ NUL = HID_CANVAS_CFG_NAME_MAX)
        private const int CfgListMax = 64;                  // CANVAS_CFG_LIST_MAX on the ESP (must match)
        private const byte F_FULL = 0x01, F_EMPTY = 0x02, F_ICONS = 0x04;
        private const byte REC_RECT = 1, REC_ICON = 2, REC_GEAR = 3, REC_SETTINGS_BG = 4;
        // ESP -> PC (IN report: [0] report ID, [1] page 0xF9, [2] event, ...)
        public const byte Page = 0xF9;
        private const byte EV_REQUEST = 0x01, EV_TOUCH = 0x02, EV_LEAVE = 0x03, EV_LIST = 0x04, EV_SELECT = 0x05;
        private const byte REQ_ENTER = 0, REQ_PAGE = 1, REQ_SYNC = 2;
        private const byte TOUCH_DOWN = 0, TOUCH_MOVE = 1, TOUCH_UP = 2;

        private const int ReportSize = 65;
        private const int DataPerReport = ReportSize - 3;   // 62
        private const int FrameIntervalMs = 150;
        private const int DragThreshold = 10;               // px before a touch becomes scrolling

        private sealed class Rec
        {
            public byte Type, Enc;
            public int X, Y, W, H;
            public byte[] Data;
        }

        private readonly HidReceiver _hid;
        private readonly Control _ui;
        private readonly CanvasRenderer _renderer = new CanvasRenderer();
        private readonly CanvasBridge _bridge;
        private readonly Timer _frameTimer;
        private readonly Timer _reloadTimer;
        private readonly object _sendLock = new object();
        private FileSystemWatcher _watcher;

        private bool _active;            // ESP is in Canvas mode
        private int _page;
        private CanvasConfig _cfg;
        private string _cfgName;         // file name _cfg was loaded from
        private int[] _lastPx;           // last frame the ESP has
        private bool _busy;              // a capture / batch is in progress
        private byte _gen;

        // a full-page request that came in while busy (latest wins)
        private bool _pendingFull, _pendingIcons, _pendingReload;
        private int _pendingPage;

        // file changes, collected until _reloadTimer fires
        private bool _changedConfig, _changedPage;

        // Canvas window (PC side); shows Canvas while the ESP doesn't
        private FormCanvasWindow _window;
        private int _pcPage;
        private int _pcGen;              // bumped per PcShow, so an older one stops

        // touch
        private bool _touchDown, _dragging;
        private int _downX, _downY, _lastX, _lastY;

        public event Action<string> OnLog;
        public bool Active => _active;
        internal CanvasBridge Bridge => _bridge;

        public CanvasLink(HidReceiver hid, Control ui)
        {
            _hid = hid;
            _ui = ui;
            _bridge = new CanvasBridge(Log);
            _renderer.OnLog += Log;
            _frameTimer = new Timer { Interval = FrameIntervalMs };
            _frameTimer.Tick += (s, e) => FrameTick();
            _reloadTimer = new Timer { Interval = 400 };
            _reloadTimer.Tick += (s, e) => ReloadTick();
        }

        // ------------------------------------------------------------------
        // Events from the ESP / ESDeckPC
        // ------------------------------------------------------------------

        /// <summary>HID read thread: buffer is reused after return, so copy.</summary>
        public void HandleReport(byte[] buf, int len)
        {
            if (len < 5) return;
            var b = new byte[Math.Max(len, 8)];
            Buffer.BlockCopy(buf, 0, b, 0, len);
            _ui.BeginInvoke((Action)(() => OnEvent(b)));
        }

        private void OnEvent(byte[] b)
        {
            switch (b[2])
            {
                case EV_REQUEST:
                    int page = b[3];
                    byte reason = b[4];
                    if (!_active) Log(reason == REQ_SYNC ? "ESP is in Canvas mode, sending page" : "ESP entered Canvas mode");
                    if (!_active && _window != null) PcYield();
                    _active = true;
                    StartWatcher();
                    // entering / reconnecting: sidebar icons too, config re-read
                    RequestFull(page, reason != REQ_PAGE, false);
                    break;

                case EV_TOUCH:
                    if (_active) _ = Touch(b[3], b[4] | (b[5] << 8), b[6] | (b[7] << 8));
                    break;

                case EV_LEAVE:
                    Deactivate("ESP left Canvas mode");
                    break;

                case EV_LIST:
                    SendConfigList(b[3]);
                    break;

                case EV_SELECT:
                    SelectFromEsp(ReadName(b, 3));
                    break;
            }
        }

        private static string ReadName(byte[] b, int off)
        {
            int end = off;
            while (end < b.Length && b[end] != 0) end++;
            return Encoding.UTF8.GetString(b, off, end - off).Trim();
        }

        // ------------------------------------------------------------------
        // Select Config on the ESP
        // ------------------------------------------------------------------

        /// <summary>The ESP's Select Config dialog wants the list: one CMD_CFG
        /// per config, tagged with the request's seq.</summary>
        private void SendConfigList(byte seq)
        {
            List<string> names;
            try { names = CanvasPaths.ListConfigs(); }
            catch (Exception ex) { Log("cannot list configs: " + ex.Message); names = new List<string>(); }

            var tooLong = names.Where(n => Encoding.UTF8.GetByteCount(n) > CfgNameMax).ToList();
            foreach (var n in tooLong) Log($"config name too long for the ESP, not listed: {n}");
            names = names.Except(tooLong).ToList();
            if (names.Count > CfgListMax)
            {
                Log($"{names.Count} configs, the ESP lists the first {CfgListMax}");
                names = names.Take(CfgListMax).ToList();
            }
            string active = CanvasPaths.ActiveConfigName;

            Task.Run(() =>
            {
                lock (_sendLock)
                {
                    if (names.Count == 0)
                    {
                        Write(CMD_CFG, seq, new byte[] { 0, 0, 0, 0 });
                        return;
                    }
                    for (int i = 0; i < names.Count; i++)
                    {
                        byte[] name = Encoding.UTF8.GetBytes(names[i]);
                        var payload = new byte[3 + name.Length + 1];
                        payload[0] = (byte)i;
                        payload[1] = (byte)names.Count;
                        payload[2] = string.Equals(names[i], active, StringComparison.OrdinalIgnoreCase) ? CFG_F_ACTIVE : (byte)0;
                        Buffer.BlockCopy(name, 0, payload, 3, name.Length);
                        if (!Write(CMD_CFG, seq, payload)) return;
                    }
                }
            });
        }

        /// <summary>A config picked on the ESP: make it the active one (the
        /// editor follows through CanvasPaths.ActiveChanged) and send its
        /// first page with the sidebar icons.</summary>
        private void SelectFromEsp(string name)
        {
            if (!_active || string.IsNullOrEmpty(name)) return;
            string match;
            try { match = CanvasPaths.ListConfigs().FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)); }
            catch { match = null; }
            if (match == null)
            {
                Log($"ESP picked {name}, but it's not in configs/ anymore");
                return;
            }
            Log($"ESP switched to config {match}");
            _cfgName = match;                       // so the watcher doesn't reload a second time
            try { CanvasPaths.ActiveConfigName = match; }
            catch (Exception ex) { Log("cannot write active.txt: " + ex.Message); }
            RequestFull(0, true, true);
        }

        public void OnModeReport(HidReceiver.EspMode mode)
        {
            // In Canvas mode the ESP follows its mode reply with a request.
            if (mode != HidReceiver.EspMode.Canvas) Deactivate(null);
        }

        public void OnDisconnected() => Deactivate("HID disconnected");

        public void Deactivate(string why)
        {
            if (!_active) return;
            _active = false;
            _frameTimer.Stop();
            _lastPx = null;
            _pendingFull = _pendingIcons = _pendingReload = false;
            _touchDown = false;
            if (why != null) Log(why + (_window != null ? ", continuing in the Canvas window" : ", stopped"));
            if (_window != null) _ = PcShow(_page, false, false);   // the page the ESP had, still running
            else _ = _renderer.BlankAsync();   // stop the page's scripts
        }

        // ------------------------------------------------------------------
        // Canvas window (PC side)
        // ------------------------------------------------------------------

        public bool HasWindow => _window != null;

        /// <summary>The window opened: show Canvas there unless the ESP has it.</summary>
        public void OpenWindow(FormCanvasWindow window)
        {
            _window = window;
            StartWatcher();
            if (_active) window.ShowEspOwned(_cfg);
            else _ = PcShow(_pcPage, true, false);
        }

        /// <summary>The window is closing (handle still alive): take the
        /// WebView back; stop the page unless the ESP is using it.</summary>
        public void CloseWindow(FormCanvasWindow window)
        {
            if (_window != window) return;
            _window = null;
            _pcGen++;
            _renderer.Detach();
            if (!_active) _ = _renderer.BlankAsync();
        }

        /// <summary>The window was resized: rescale the page if it's shown there.</summary>
        public void PcResized(FormCanvasWindow window)
        {
            if (_window == window && !_active) _renderer.FitTo(window.PageHost);
        }

        /// <summary>Sidebar tap in the window.</summary>
        public void PcSelect(int page)
        {
            if (_window != null && !_active) _ = PcShow(page, false, false);
        }

        /// <summary>The ESP entered Canvas mode while the window had it.</summary>
        private void PcYield()
        {
            _pcGen++;
            _renderer.Detach();
            _window.ShowEspOwned(_cfg);
            Log("Canvas window: ESP took over");
        }

        private async Task PcShow(int page, bool loadCfg, bool reload)
        {
            var w = _window;
            if (w == null || _active) return;
            int gen = ++_pcGen;

            if (loadCfg || _cfg == null)
            {
                _cfg = CanvasConfig.LoadActive(out string name, out string error);
                _cfgName = name;
                if (_cfg == null) Log(error);
            }
            if (_cfg == null || _cfg.Pages.Count == 0)
            {
                _renderer.Detach();
                w.ShowEmpty(_cfg);
                return;
            }

            try { await _renderer.InitAsync(_bridge); }
            catch (Exception ex)
            {
                Log("WebView2 failed to start (is the WebView2 Runtime installed?): " + ex.Message);
                w.ShowEmpty(_cfg);
                return;
            }
            if (gen != _pcGen || _window != w || _active) return;

            if (page < 0 || page >= _cfg.Pages.Count) page = 0;
            _pcPage = page;
            var entry = _cfg.Pages[page];
            if (!File.Exists(entry.IndexPath)) Log($"page not found: {entry.IndexPath}");
            w.ShowPcOwned(_cfg, page);
            _renderer.AttachTo(w.PageHost);
            if (reload || _renderer.CurrentUrl != entry.Url)
                await _renderer.NavigateAsync(entry.Url);
        }

        public void OpenDevTools()
        {
            if (_renderer.IsReady) _renderer.OpenDevTools();
            else Log("not started yet -- switch the ESP to Canvas mode first");
        }

        // ------------------------------------------------------------------
        // Full page
        // ------------------------------------------------------------------

        private void RequestFull(int page, bool icons, bool reload)
        {
            if (_busy)
            {
                _pendingFull = true;
                _pendingPage = page;
                _pendingIcons |= icons;
                _pendingReload |= reload;
                return;
            }
            _ = RunFull(page, icons, reload);
        }

        private async Task RunFull(int page, bool icons, bool reload)
        {
            _busy = true;
            try { await DoFull(page, icons, reload); }
            catch (Exception ex) { Log("error: " + ex.Message); }
            finally { _busy = false; }
            RunPending();
        }

        private void RunPending()
        {
            if (!_active || !_pendingFull) return;
            int p = _pendingPage;
            bool i = _pendingIcons, r = _pendingReload;
            _pendingFull = _pendingIcons = _pendingReload = false;
            _ = RunFull(p, i, r);
        }

        private async Task DoFull(int page, bool icons, bool reload)
        {
            if (icons || _cfg == null)
            {
                _cfg = CanvasConfig.LoadActive(out string name, out string error);
                _cfgName = name;
                if (_cfg != null) Log($"config: {name} ({_cfg.Pages.Count} pages)");
                else Log(error);
            }
            _window?.ShowEspOwned(_cfg);   // sidebar follows the config the ESP shows
            if (_cfg == null || _cfg.Pages.Count == 0)
            {
                _frameTimer.Stop();
                _lastPx = null;
                await SendAsync(F_EMPTY, 0, 0, new List<Rec>());
                return;
            }

            try { await _renderer.InitAsync(_bridge); }
            catch (Exception ex)
            {
                Log("WebView2 failed to start (is the WebView2 Runtime installed?): " + ex.Message);
                await SendAsync(F_EMPTY, 0, 0, new List<Rec>());
                return;
            }

            if (page < 0 || page >= _cfg.Pages.Count) page = 0;
            _page = page;
            var entry = _cfg.Pages[page];
            if (!File.Exists(entry.IndexPath)) Log($"page not found: {entry.IndexPath}");

            var sw = Stopwatch.StartNew();
            if (reload || _renderer.CurrentUrl != entry.Url)
                await _renderer.NavigateAsync(entry.Url);
            if (!_active) return;

            Bitmap bmp = await _renderer.CaptureAsync();
            var pages = _cfg.Pages.ToList();
            var look = _cfg.Settings ?? new CanvasSettingsLook();
            int[] px = null;
            var recs = await Task.Run(() =>
            {
                px = CanvasEncoder.ToPixels(bmp);
                var list = CanvasEncoder.Strips(CanvasRenderer.W, CanvasRenderer.H)
                    .Select(r => ToRec(CanvasEncoder.Encode(bmp, px, r))).ToList();
                if (icons)
                {
                    for (int i = 0; i < pages.Count; i++)
                    {
                        using (var ib = CanvasEncoder.RenderIcon(pages[i].IconPath, pages[i].Name))
                        {
                            var ipx = CanvasEncoder.ToPixels(ib);
                            var area = new Rectangle(0, 0, ib.Width, ib.Height);
                            list.Add(new Rec
                            {
                                Type = REC_ICON, Enc = CanvasEncoder.EncRle,
                                X = i, Y = 0, W = ib.Width, H = ib.Height,
                                Data = CanvasEncoder.Rle(ipx, ib.Width, area),
                            });
                        }
                    }

                    // Settings look (gear icon, page background) -- left out = defaults
                    using (var gear = CanvasEncoder.RenderIconFile(CanvasPaths.Resolve(look.SideIcon)))
                    {
                        if (gear != null)
                        {
                            var gpx = CanvasEncoder.ToPixels(gear);
                            list.Add(new Rec
                            {
                                Type = REC_GEAR, Enc = CanvasEncoder.EncRle,
                                W = gear.Width, H = gear.Height,
                                Data = CanvasEncoder.Rle(gpx, gear.Width, new Rectangle(0, 0, gear.Width, gear.Height)),
                            });
                        }
                    }
                    using (var sbg = CanvasEncoder.LoadCover(CanvasPaths.Resolve(look.BgImage), CanvasRenderer.W, CanvasRenderer.H))
                    {
                        if (sbg != null)
                        {
                            var spx = CanvasEncoder.ToPixels(sbg);
                            foreach (var r in CanvasEncoder.Strips(sbg.Width, sbg.Height))
                            {
                                var rec = ToRec(CanvasEncoder.Encode(sbg, spx, r, true));
                                rec.Type = REC_SETTINGS_BG;
                                list.Add(rec);
                            }
                        }
                    }
                }
                return list;
            });
            bmp.Dispose();
            if (!_active) return;

            byte flags = (byte)(F_FULL | (icons ? F_ICONS : 0));
            bool ok = await SendAsync(flags, page, pages.Count, recs);
            if (ok)
            {
                _lastPx = px;
                _frameTimer.Start();
            }
            Log($"page {page + 1} \"{entry.Name}\": {(ok ? "sent" : "send failed")}, " +
                $"{recs.Sum(r => r.Data.Length) / 1024} KB, {sw.ElapsedMilliseconds} ms");
        }

        private static Rec ToRec(CanvasEncoder.Rect r) => new Rec
        {
            Type = REC_RECT, Enc = r.Enc,
            X = r.Area.X, Y = r.Area.Y, W = r.Area.Width, H = r.Area.Height,
            Data = r.Data,
        };

        // ------------------------------------------------------------------
        // Live updates: changed tiles only
        // ------------------------------------------------------------------

        private async void FrameTick()
        {
            if (!_active || _busy || _lastPx == null || !_renderer.IsReady) return;
            _busy = true;
            try
            {
                Bitmap bmp = await _renderer.CaptureAsync();
                int[] prev = _lastPx;
                int[] px = null;
                var recs = await Task.Run(() =>
                {
                    px = CanvasEncoder.ToPixels(bmp);
                    var rects = CanvasEncoder.Diff(prev, px, CanvasRenderer.W, CanvasRenderer.H);
                    // a quarter of the page or more changed (scrolling, page
                    // transitions): favor size over sharpness
                    bool big = rects.Sum(r => r.Width * r.Height) * 4 >= CanvasRenderer.W * CanvasRenderer.H;
                    return rects.Select(r => ToRec(CanvasEncoder.Encode(bmp, px, r, big))).ToList();
                });
                bmp.Dispose();
                if (recs.Count > 0 && _active && _lastPx == prev)
                {
                    if (await SendAsync(0, _page, 0, recs)) _lastPx = px;
                }
            }
            catch (Exception ex)
            {
                Log("update failed: " + ex.Message);
            }
            finally
            {
                _busy = false;
            }
            RunPending();
        }

        // ------------------------------------------------------------------
        // Touch -> page input
        // ------------------------------------------------------------------

        private async Task Touch(byte action, int x, int y)
        {
            if (!_renderer.IsReady || _lastPx == null) return;
            try
            {
                switch (action)
                {
                    case TOUCH_DOWN:
                        _touchDown = true;
                        _dragging = false;
                        _downX = _lastX = x;
                        _downY = _lastY = y;
                        await _renderer.MouseAsync("mouseMoved", x, y);
                        break;

                    case TOUCH_MOVE:
                        if (!_touchDown) return;
                        if (!_dragging && (Math.Abs(x - _downX) > DragThreshold || Math.Abs(y - _downY) > DragThreshold))
                            _dragging = true;
                        if (_dragging)
                        {
                            int dx = x - _lastX, dy = y - _lastY;
                            _lastX = x;
                            _lastY = y;
                            // content follows the finger
                            if (dx != 0 || dy != 0) await _renderer.WheelAsync(_downX, _downY, -dx, -dy);
                        }
                        break;

                    case TOUCH_UP:
                        if (!_touchDown) return;
                        _touchDown = false;
                        if (!_dragging)
                        {
                            await _renderer.MouseAsync("mousePressed", _downX, _downY, 1);
                            await _renderer.MouseAsync("mouseReleased", _downX, _downY, 1);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("touch failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // File watching
        // ------------------------------------------------------------------

        private void StartWatcher()
        {
            if (_watcher != null) return;
            try
            {
                CanvasPaths.Ensure();
                _watcher = new FileSystemWatcher(CanvasPaths.Root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                };
                FileSystemEventHandler h = (s, e) => _ui.BeginInvoke((Action)(() => OnFileChanged(e.FullPath)));
                _watcher.Changed += h;
                _watcher.Created += h;
                _watcher.Deleted += h;
                _watcher.Renamed += (s, e) => _ui.BeginInvoke((Action)(() => OnFileChanged(e.FullPath)));
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Log("cannot watch the Canvas folder: " + ex.Message);
            }
        }

        private bool PcShowing => _window != null && !_active;

        private void OnFileChanged(string fullPath)
        {
            if (!_active && !PcShowing) return;
            string rel = CanvasPaths.MakeRelative(fullPath).ToLowerInvariant();
            // active.txt naming the config already shown (written by SelectFromEsp,
            // or Use This Config on the same one): nothing to reload
            if (rel == "active.txt" &&
                string.Equals(CanvasPaths.ActiveConfigName, _cfgName, StringComparison.OrdinalIgnoreCase))
                return;
            if (rel.StartsWith("configs/") || rel == "active.txt")
                _changedConfig = true;
            else if (rel.StartsWith("shared/") || IsCurrentPage(rel))
                _changedPage = true;
            else
                return;
            _reloadTimer.Stop();
            _reloadTimer.Start();   // wait for the burst of writes an editor makes
        }

        private bool IsCurrentPage(string rel)
        {
            int cur = _active ? _page : _pcPage;
            if (_cfg == null || cur >= _cfg.Pages.Count) return false;
            string dir = _cfg.Pages[cur].Dir.Trim('/').Replace('\\', '/').ToLowerInvariant();
            return rel == dir || rel.StartsWith(dir + "/");
        }

        private void ReloadTick()
        {
            _reloadTimer.Stop();
            bool cfg = _changedConfig, page = _changedPage;
            _changedConfig = _changedPage = false;
            if (PcShowing)
            {
                if (cfg || page) _ = PcShow(_pcPage, cfg, true);
                return;
            }
            if (!_active) return;
            if (cfg)
            {
                Log("config changed, reloading");
                RequestFull(_page, true, true);
            }
            else if (page)
            {
                Log("page changed, reloading");
                RequestFull(_page, false, true);
            }
        }

        // ------------------------------------------------------------------
        // HID
        // ------------------------------------------------------------------

        private Task<bool> SendAsync(byte flags, int page, int count, List<Rec> recs) =>
            Task.Run(() => SendBatch(flags, page, count, recs));

        private bool SendBatch(byte flags, int page, int count, List<Rec> recs)
        {
            lock (_sendLock)
            {
                byte gen = unchecked(++_gen);
                if (!Write(CMD_BEGIN, gen, new byte[] { flags, (byte)page, (byte)count })) return false;

                foreach (var r in recs)
                {
                    int len = r.Data.Length;
                    byte[] hdr =
                    {
                        r.Type, r.Enc,
                        (byte)r.X, (byte)(r.X >> 8), (byte)r.Y, (byte)(r.Y >> 8),
                        (byte)r.W, (byte)(r.W >> 8), (byte)r.H, (byte)(r.H >> 8),
                        (byte)len, (byte)(len >> 8), (byte)(len >> 16), (byte)(len >> 24),
                    };
                    if (!Write(CMD_REC, gen, hdr)) return false;
                    for (int off = 0; off < len; off += DataPerReport)
                        if (!Write(CMD_DATA, gen, r.Data, off, Math.Min(DataPerReport, len - off))) return false;
                }
                return Write(CMD_END, gen, null);
            }
        }

        private bool Write(byte cmd, byte gen, byte[] payload, int off = 0, int n = -1)
        {
            var report = new byte[ReportSize];
            report[1] = cmd;
            report[2] = gen;
            if (payload != null)
            {
                if (n < 0) n = payload.Length - off;
                Buffer.BlockCopy(payload, off, report, 3, n);
            }
            return _hid.WriteReport(report);
        }

        private void Log(string msg)
        {
            if (msg != null) OnLog?.Invoke(msg.StartsWith("Canvas") ? msg : "Canvas: " + msg);
        }

        public void Dispose()
        {
            _frameTimer.Dispose();
            _reloadTimer.Dispose();
            _watcher?.Dispose();
            _renderer.Dispose();
        }
    }
}
