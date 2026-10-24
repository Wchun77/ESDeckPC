using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace ESDeckPC
{
    /// <summary>
    /// What a Canvas page can ask ESDeckPC to do -- exposed to the page as
    /// window.esdeck (see CanvasRenderer):
    ///   esdeck.action({ action: "launch", target: "notepad.exe" })
    ///   esdeck.action({ action: "hotkey", keys: ["ctrl", "shift", "esc"] })
    ///   esdeck.log("text")             -> ESDeckPC log
    ///   esdeck.stats()                 -> JSON: CPU / GPU / RAM / net / disk
    ///                                     (same sensors as Monitor mode)
    ///   esdeck.media()                 -> JSON: now playing (title, artist,
    ///                                     position, duration, playing, cover)
    ///   esdeck.mediaCover()            -> cover art as a data: URL, or ""
    ///   esdeck.mediaCmd("toggle" | "next" | "prev")
    /// action takes the same fields as a Deck button (launch, hotkey, media,
    /// text, power, ...), run by ActionExecutor. Both return promises; action
    /// resolves to ActionExecutor's result text.
    ///
    /// Called by WebView2 on the UI thread.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CanvasBridge
    {
        private readonly System.Action<string> _log;
        private MonitorSender _monitor;
        private NowPlayingWatcher _nowPlaying;
        private volatile string _cover = "";     // data: URL of the current cover art
        private volatile int _coverVer = 0;      // bumped whenever _cover changes

        public CanvasBridge(System.Action<string> log)
        {
            _log = log;
        }

        /// <summary>Data sources for stats() / media(). Internal, so it isn't
        /// exposed to pages.</summary>
        internal void Attach(MonitorSender monitor, NowPlayingWatcher nowPlaying)
        {
            _monitor = monitor;
            _nowPlaying = nowPlaying;
            if (_nowPlaying != null)
            {
                _nowPlaying.OnMediaPropertiesChanged += p => { _ = LoadCoverAsync(p); };
                _ = LoadCoverAsync(_nowPlaying.CurrentProperties);
            }
        }

        // ------------------------------------------------------------------
        // System stats
        // ------------------------------------------------------------------

        /// <summary>Latest sensor values (see MonitorSender.RequestForCanvas);
        /// asking keeps the sensors running, so poll about once a second.</summary>
        public string Stats() => _monitor?.RequestForCanvas() ?? "{}";

        // ------------------------------------------------------------------
        // Now playing
        // ------------------------------------------------------------------

        public string Media()
        {
            var np = _nowPlaying;
            var p = np?.CurrentProperties;
            if (np == null || p == null)
                return "{\"active\":false}";
            return JsonConvert.SerializeObject(new
            {
                active = true,
                title = p.Title ?? "",
                artist = p.Artist ?? "",
                album = p.AlbumTitle ?? "",
                pos = (long)np.Position.TotalMilliseconds,
                dur = (long)np.Duration.TotalMilliseconds,
                playing = np.IsPlaying,
                cover = _coverVer,           // changes when mediaCover() has a new picture
            });
        }

        public string MediaCover() => _cover;

        public void MediaCmd(string cmd)
        {
            var np = _nowPlaying;
            if (np == null) return;
            switch ((cmd ?? "").ToLowerInvariant())
            {
                case "toggle": np.TogglePlayPause(); break;
                case "next": np.Next(); break;
                case "prev": np.Previous(); break;
            }
        }

        private async Task LoadCoverAsync(GlobalSystemMediaTransportControlsSessionMediaProperties props)
        {
            string url = "";
            try
            {
                var thumbRef = props?.Thumbnail;
                if (thumbRef != null)
                {
                    using (var stream = await thumbRef.OpenReadAsync())
                    {
                        // DataReader, same as NowPlayingImageSender (no
                        // System.Runtime.WindowsRuntime reference needed)
                        var reader = new DataReader(stream);
                        await reader.LoadAsync((uint)stream.Size);
                        var bytes = new byte[stream.Size];
                        reader.ReadBytes(bytes);
                        bool png = bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50;
                        url = (png ? "data:image/png;base64," : "data:image/jpeg;base64,") + Convert.ToBase64String(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke("Canvas: cover art unavailable (" + ex.Message + ")");
            }
            _cover = url;
            _coverVer++;
        }

        public string Action(string json)
        {
            DeckButton button;
            try
            {
                button = JsonConvert.DeserializeObject<DeckButton>(json);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Canvas action: bad JSON ({ex.Message})");
                return "bad JSON";
            }
            string result = ActionExecutor.Run(button);
            _log?.Invoke($"Canvas action [{button?.Label ?? button?.Action}]: {result}");
            return result;
        }

        public void Log(string message)
        {
            _log?.Invoke("Canvas page: " + message);
        }
    }
}
