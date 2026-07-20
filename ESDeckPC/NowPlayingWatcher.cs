using System;
using System.Threading;
using Windows.Media.Control;
using WindowsMediaController;

namespace ESDeckPC
{
    /// <summary>
    /// Read-only probe for the OS-wide "Now Playing" media session (Windows
    /// System Media Transport Controls), built on top of the
    /// Dubya.WindowsMediaController wrapper around Windows.Media.Control.
    ///
    /// This exists to confirm we can reliably pull song title / artist /
    /// playback state / position from Windows before designing the HID
    /// transfer protocol for Media mode (see
    /// doc/ESDeck_Media模式開發筆記.md 第 4 節). It does not touch the ESP
    /// or HID layer -- output only goes through OnLog.
    /// </summary>
    public class NowPlayingWatcher : IDisposable
    {
        private MediaManager _mediaManager;
        private MediaManager.MediaSession _focusedSession;
        private bool _started = false;
        private bool _disposed = false;

        // De-dupe: MediaManager fires OnAny* once from its own event handler
        // and again when we manually re-query on focus change, and some
        // apps (observed with Chrome/YouTube) fire the same OS event twice
        // in a row. Skip re-logging a line that is identical to the last one
        // logged in that category.
        private string _lastMediaLine;
        private string _lastStatusLine;
        private string _lastFocusLine;
        private string _lastPositionLine;

        // Safety net: GlobalSystemMediaTransportControlsSessionManager is
        // known to sometimes stop firing CurrentSessionChanged (see
        // https://github.com/DubyaDude/WindowsMediaController/issues/6),
        // which leaves us stuck showing "no focused session" forever. Poll
        // ForceUpdate() at a low rate only while we have no focused session
        // to self-heal without spamming re-queries once we're in sync.
        private Timer _recoveryTimer;
        private static readonly TimeSpan RecoveryInterval = TimeSpan.FromSeconds(3);

        public event Action<string> OnLog;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public void Start()
        {
            if (_started) return;
            _started = true;

            _mediaManager = new MediaManager();
            _mediaManager.OnFocusedSessionChanged += OnFocusedSessionChanged;
            _mediaManager.OnAnyMediaPropertyChanged += OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged += OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyTimelinePropertyChanged += OnAnyTimelinePropertyChanged;

            try
            {
                _mediaManager.Start();
                Log("NowPlaying: watcher started");
            }
            catch (Exception ex)
            {
                Log($"NowPlaying: failed to start ({ex.Message})");
            }

            _recoveryTimer = new Timer(RecoveryTick, null, RecoveryInterval, RecoveryInterval);
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;

            _recoveryTimer?.Dispose();
            _recoveryTimer = null;

            if (_mediaManager != null)
            {
                _mediaManager.OnFocusedSessionChanged -= OnFocusedSessionChanged;
                _mediaManager.OnAnyMediaPropertyChanged -= OnAnyMediaPropertyChanged;
                _mediaManager.OnAnyPlaybackStateChanged -= OnAnyPlaybackStateChanged;
                _mediaManager.OnAnyTimelinePropertyChanged -= OnAnyTimelinePropertyChanged;

                try
                {
                    _mediaManager.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"NowPlaying: error while stopping ({ex.Message})");
                }

                _mediaManager = null;
            }

            _focusedSession = null;
            Log("NowPlaying: watcher stopped");
        }

        // ------------------------------------------------------------------
        // MediaManager event handlers -- MediaManager fires OnAny* for every
        // open session (Spotify, Chrome tab, etc), so filter to whichever
        // session Windows currently considers "focused" to avoid log spam.
        // ------------------------------------------------------------------

        private void OnFocusedSessionChanged(MediaManager.MediaSession session)
        {
            _focusedSession = session;

            string line = session == null
                ? "NowPlaying: no focused session"
                : $"NowPlaying: focused session -> {session.Id}";

            if (line != _lastFocusLine)
            {
                _lastFocusLine = line;
                Log(line);
            }

            if (session == null) return;
            LogCurrentProperties(session);
        }

        private void OnAnyMediaPropertyChanged(MediaManager.MediaSession sender,
            GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            if (!IsFocused(sender)) return;
            LogMediaProperties(args);
        }

        private void OnAnyPlaybackStateChanged(MediaManager.MediaSession sender,
            GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            if (!IsFocused(sender)) return;
            LogPlaybackStatus(args?.PlaybackStatus);
        }

        private void OnAnyTimelinePropertyChanged(MediaManager.MediaSession sender,
            GlobalSystemMediaTransportControlsSessionTimelineProperties args)
        {
            if (!IsFocused(sender)) return;
            if (args == null) return;

            string line = $"NowPlaying: position={args.Position:hh\\:mm\\:ss} / duration={args.EndTime:hh\\:mm\\:ss}";
            if (line == _lastPositionLine) return;
            _lastPositionLine = line;
            Log(line);
        }

        // ------------------------------------------------------------------
        // Recovery -- see _recoveryTimer remarks above
        // ------------------------------------------------------------------

        private void RecoveryTick(object state)
        {
            if (_focusedSession != null) return;

            try
            {
                _mediaManager?.ForceUpdate();
            }
            catch
            {
                // best-effort; next tick will retry
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private bool IsFocused(MediaManager.MediaSession session)
        {
            return session != null && _focusedSession != null && session.Id == _focusedSession.Id;
        }

        private async void LogCurrentProperties(MediaManager.MediaSession session)
        {
            try
            {
                var props = await session.ControlSession.TryGetMediaPropertiesAsync();
                LogMediaProperties(props);

                var playback = session.ControlSession.GetPlaybackInfo();
                LogPlaybackStatus(playback?.PlaybackStatus);
            }
            catch (Exception ex)
            {
                Log($"NowPlaying: failed to read initial properties ({ex.Message})");
            }
        }

        private void LogMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties props)
        {
            string title = string.IsNullOrEmpty(props?.Title) ? "(unknown)" : props.Title;
            string artist = string.IsNullOrEmpty(props?.Artist) ? props?.AlbumArtist : props.Artist;
            string line = $"NowPlaying: title=\"{title}\" artist=\"{artist}\"";

            if (line == _lastMediaLine) return;
            _lastMediaLine = line;
            Log(line);
        }

        private void LogPlaybackStatus(GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
        {
            string line = $"NowPlaying: playback status = {status}";

            if (line == _lastStatusLine) return;
            _lastStatusLine = line;
            Log(line);
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        // ------------------------------------------------------------------
        // IDisposable
        // ------------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
