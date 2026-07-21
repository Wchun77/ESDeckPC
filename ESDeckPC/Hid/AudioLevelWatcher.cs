using System;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace ESDeckPC
{
    /// <summary>
    /// Read-only probe for system audio output level via WASAPI loopback
    /// capture (NAudio) -- "listens" to whatever Windows is currently
    /// playing without a physical microphone. Confirms we can pull a
    /// VU-meter-style volume value before wiring it into the Media mode
    /// sidebar level bar (see doc/ESDeck_Media模式開發筆記.md 第 5 節).
    /// Output only goes through OnLog -- no HID, no UI beyond that.
    ///
    /// Simple version only (single peak value 0-100), matching the "簡單版"
    /// described in the note. FFT band splitting is a later step once this
    /// basic path is confirmed working.
    /// </summary>
    public class AudioLevelWatcher : IDisposable
    {
        private WasapiLoopbackCapture _capture;
        private bool _started = false;
        private bool _disposed = false;

        // WasapiLoopbackCapture()'s parameterless constructor grabs
        // whatever the default render device happens to be *at
        // construction time* and stays bound to it -- if the user later
        // switches Windows' default output (e.g. speakers -> headphones),
        // the capture keeps listening to the old (now silent) device and
        // Level reads 0 forever, even though audio is clearly playing.
        // Poll the actual default device id at a low rate and restart
        // capture on it whenever it changes, rather than a one-shot bind.
        private MMDeviceEnumerator _enumerator;
        private string _deviceId;
        private Timer _deviceCheckTimer;
        private static readonly TimeSpan DeviceCheckInterval = TimeSpan.FromSeconds(2);

        // Live level -- read by AudioLevelSender to build HID reports.
        // Updated every buffer (~10-20ms), independent of the log throttle
        // above, so a poller always sees a fresh value regardless of its
        // own cadence. WasapiLoopbackCapture simply stops firing
        // DataAvailable entirely during true silence (no "silent buffer"
        // events), so without SilenceTimeout this would freeze at whatever
        // the last loud value was instead of dropping to 0 when audio stops.
        private static readonly TimeSpan SilenceTimeout = TimeSpan.FromMilliseconds(300);
        private float _currentPeak = 0f;
        private DateTime _lastDataAt = DateTime.MinValue;

        // Attack/release envelope -- a real VU meter doesn't jump straight
        // to the instantaneous sample peak every buffer (that reads as
        // jittery/rigid, since raw peaks swing wildly buffer to buffer).
        // Real meters rise fast on a transient and fall back slower, which
        // is what actually reads as "smooth" motion. Attack is fast enough
        // to still feel responsive to beats; release is slow enough to
        // avoid visible flicker.
        private static readonly double AttackSeconds = 0.03;
        private static readonly double ReleaseSeconds = 0.25;
        private float _smoothedPeak = 0f;
        private DateTime _lastSmoothAt = DateTime.UtcNow;

        public int Level
        {
            get
            {
                if (DateTime.UtcNow - _lastDataAt > SilenceTimeout) return 0;
                return (int)Math.Round(Math.Min(1f, _currentPeak) * 100);
            }
        }

        public event Action<string> OnLog;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public void Start()
        {
            if (_started) return;
            _started = true;

            _enumerator = new MMDeviceEnumerator();

            if (!StartCaptureOnDefaultDevice())
            {
                _started = false;
                return;
            }

            _deviceCheckTimer = new Timer(CheckDefaultDeviceChanged, null, DeviceCheckInterval, DeviceCheckInterval);
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;

            _deviceCheckTimer?.Dispose();
            _deviceCheckTimer = null;

            StopCapture();

            _enumerator?.Dispose();
            _enumerator = null;
        }

        private bool StartCaptureOnDefaultDevice()
        {
            try
            {
                var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _deviceId = device.ID;

                _capture = new WasapiLoopbackCapture(device);
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();

                var fmt = _capture.WaveFormat;
                Log($"Audio: loopback capture started on \"{device.FriendlyName}\" ({fmt.SampleRate}Hz, {fmt.BitsPerSample}bit, {fmt.Encoding}, {fmt.Channels}ch)");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Audio: failed to start loopback capture ({ex.Message})");
                return false;
            }
        }

        /// <summary>
        /// Stops and disposes the current capture without touching
        /// _started/_deviceCheckTimer/_enumerator -- shared by Stop() and
        /// the device-swap path in CheckDefaultDeviceChanged().
        /// Unsubscribes RecordingStopped first so that event (which fires
        /// asynchronously) doesn't race with a subsequently-started
        /// replacement capture touching the same _capture field.
        /// </summary>
        private void StopCapture()
        {
            if (_capture == null) return;

            try
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.RecordingStopped -= OnRecordingStopped;
                _capture.StopRecording();
                _capture.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Audio: error while stopping ({ex.Message})");
            }
            _capture = null;
        }

        /// <summary>
        /// Runs on a ThreadPool timer thread, not the capture thread or UI
        /// thread -- StartCaptureOnDefaultDevice()/StopCapture() don't
        /// touch anything UI-affine, and Log() already marshals through
        /// OnLog same as everywhere else in this class.
        /// </summary>
        private void CheckDefaultDeviceChanged(object state)
        {
            if (!_started) return;

            try
            {
                var current = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (current.ID == _deviceId) return;

                Log($"Audio: default output device changed to \"{current.FriendlyName}\", restarting capture");
                StopCapture();
                StartCaptureOnDefaultDevice();
            }
            catch (Exception ex)
            {
                // e.g. no active render device at all momentarily -- next
                // tick will retry, nothing to clean up here since we
                // haven't torn down the still-working old capture (if any).
                Log($"Audio: default device check failed ({ex.Message})");
            }
        }

        // ------------------------------------------------------------------
        // Capture callbacks -- fire on NAudio's own capture thread, not the
        // UI thread. Log() -> OnLog is the same pattern used by MonitorSender
        // / NowPlayingWatcher, and AppendLog on the receiving end already
        // marshals to the UI thread.
        // ------------------------------------------------------------------

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            float peak = ReadPeak(e);
            var now = DateTime.UtcNow;

            double dt = (now - _lastSmoothAt).TotalSeconds;
            _lastSmoothAt = now;
            if (dt < 0 || dt > 1) dt = 0;   // guard first call / any clock weirdness

            double tau = peak > _smoothedPeak ? AttackSeconds : ReleaseSeconds;
            double coeff = 1.0 - Math.Exp(-dt / tau);
            _smoothedPeak += (float)((peak - _smoothedPeak) * coeff);

            _currentPeak = _smoothedPeak;
            _lastDataAt  = now;
        }

        /// <summary>
        /// WASAPI shared-mode loopback almost always hands us IEEE float
        /// 32-bit samples (the engine's mix format), but fall back to
        /// 16-bit PCM just in case a given machine reports something else --
        /// this is only a feasibility probe, not the final pipeline.
        /// </summary>
        private float ReadPeak(WaveInEventArgs e)
        {
            var format = _capture.WaveFormat;
            float peak = 0f;

            if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                int sampleCount = e.BytesRecorded / 4;
                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = Math.Abs(BitConverter.ToSingle(e.Buffer, i * 4));
                    if (sample > peak) peak = sample;
                }
            }
            else if (format.BitsPerSample == 16)
            {
                int sampleCount = e.BytesRecorded / 2;
                for (int i = 0; i < sampleCount; i++)
                {
                    short raw = BitConverter.ToInt16(e.Buffer, i * 2);
                    float sample = Math.Abs(raw / 32768f);
                    if (sample > peak) peak = sample;
                }
            }
            // else: unsupported format for this quick probe, report silence

            return peak;
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Log($"Audio: capture stopped with error ({e.Exception.Message})");
            else
                Log("Audio: capture stopped");

            _capture?.Dispose();
            _capture = null;
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
