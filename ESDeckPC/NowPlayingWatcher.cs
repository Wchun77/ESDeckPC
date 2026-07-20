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

        // Fires whenever playback state or timeline actually changes (play/
        // pause/seek/track change) -- NowPlayingSender hooks this to
        // Nudge() an immediate HID send instead of waiting out its normal
        // 1s cycle, so e.g. an ESP button press shows up on the ESP's own
        // icon quickly instead of up to ~1s later.
        public event Action OnStateChanged;

        // ------------------------------------------------------------------
        // Live state -- read by NowPlayingSender to build HID reports.
        //
        // Windows only fires TimelinePropertiesChanged on discrete events
        // (play, pause, seek, track change) -- NOT continuously while a
        // track is playing. So the raw Position from the OS is a snapshot
        // that goes stale between those events. Position below interpolates
        // from that snapshot using real elapsed time while IsPlaying, the
        // same way the OS-level SMTC API expects consumers to do it.
        // ------------------------------------------------------------------
        private TimeSpan _positionBase   = TimeSpan.Zero;   // last known Position from Windows
        private DateTime _positionBaseAt = DateTime.UtcNow; // wall-clock time _positionBase was captured

        public TimeSpan Position
        {
            get
            {
                if (!IsPlaying) return _positionBase;

                TimeSpan interpolated = _positionBase + (DateTime.UtcNow - _positionBaseAt);
                if (Duration > TimeSpan.Zero && interpolated > Duration) return Duration;
                return interpolated;
            }
        }

        public TimeSpan Duration { get; private set; } = TimeSpan.Zero;
        public bool IsPlaying { get; private set; } = false;

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

            if (session == null)
            {
                // No source to report -- don't leave stale position/duration
                // sitting around claiming playback is still happening.
                _positionBase   = TimeSpan.Zero;
                _positionBaseAt = DateTime.UtcNow;
                Duration        = TimeSpan.Zero;
                IsPlaying       = false;
                return;
            }

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

            LogTimeline(args.Position, args.EndTime, args.LastUpdatedTime.UtcDateTime);
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
        // Playback control -- ESP button presses arrive here via
        // FormM.OnMediaControl. Fire-and-forget: the resulting state change
        // (if any) comes back through the normal OnAnyPlaybackStateChanged /
        // OnAnyTimelinePropertyChanged events like any other change, there's
        // no separate "did it work" path.
        // ------------------------------------------------------------------

        public void TogglePlayPause()
        {
            _ = _focusedSession?.ControlSession?.TryTogglePlayPauseAsync();
        }

        public void Next()
        {
            _ = _focusedSession?.ControlSession?.TrySkipNextAsync();
        }

        public void Previous()
        {
            _ = _focusedSession?.ControlSession?.TrySkipPreviousAsync();
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

                var timeline = session.ControlSession.GetTimelineProperties();
                if (timeline != null)
                    LogTimeline(timeline.Position, timeline.EndTime, timeline.LastUpdatedTime.UtcDateTime);
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
            bool nowPlaying = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            if (nowPlaying != IsPlaying)
            {
                // Freeze the interpolated position at the transition instant
                // (reads Position under the *old* IsPlaying before flipping
                // it below) -- makes pause/resume correct even if this event
                // arrives without an accompanying TimelinePropertiesChanged,
                // instead of drifting forward by however long we were paused.
                _positionBase   = Position;
                _positionBaseAt = DateTime.UtcNow;
                IsPlaying       = nowPlaying;
                OnStateChanged?.Invoke();
            }

            string line = $"NowPlaying: playback status = {status}";

            if (line == _lastStatusLine) return;
            _lastStatusLine = line;
            Log(line);
        }

        private void LogTimeline(TimeSpan position, TimeSpan duration, DateTime capturedAtUtc)
        {
            // Anchor interpolation to when the *app* says it captured this
            // position (LastUpdatedTime), not when we happened to receive
            // the notification. Some apps (observed with Chrome) push
            // updates with a delay or reuse an older checkpoint, so using
            // "now" here made the interpolated position start out wrong
            // whenever you tuned in mid-playback -- it would only correct
            // itself once a fresh pause/resume forced an accurate update.
            // Guard against a missing/bogus LastUpdatedTime (default value,
            // in the future, or implausibly old) by falling back to "now".
            if (capturedAtUtc == default ||
                capturedAtUtc > DateTime.UtcNow ||
                capturedAtUtc < DateTime.UtcNow.AddDays(-1))
            {
                capturedAtUtc = DateTime.UtcNow;
            }

            _positionBase   = position;
            _positionBaseAt = capturedAtUtc;
            Duration        = duration;
            OnStateChanged?.Invoke();

            string line = $"NowPlaying: position={position:hh\\:mm\\:ss} / duration={duration:hh\\:mm\\:ss}";
            if (line == _lastPositionLine) return;
            _lastPositionLine = line;
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
