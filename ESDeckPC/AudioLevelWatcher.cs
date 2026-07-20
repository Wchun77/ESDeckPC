using System;
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

        // DataAvailable fires roughly every ~10-20ms; logging every buffer
        // would flood the log, so only log the peak seen since the last tick.
        private static readonly TimeSpan LogInterval = TimeSpan.FromMilliseconds(500);
        private DateTime _lastLogTime = DateTime.MinValue;
        private float _peakSinceLastLog = 0f;

        public event Action<string> OnLog;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public void Start()
        {
            if (_started) return;
            _started = true;

            try
            {
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();

                var fmt = _capture.WaveFormat;
                Log($"Audio: loopback capture started ({fmt.SampleRate}Hz, {fmt.BitsPerSample}bit, {fmt.Encoding}, {fmt.Channels}ch)");
            }
            catch (Exception ex)
            {
                Log($"Audio: failed to start loopback capture ({ex.Message})");
                _started = false;
            }
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;

            if (_capture != null)
            {
                try
                {
                    _capture.StopRecording();
                }
                catch (Exception ex)
                {
                    Log($"Audio: error while stopping ({ex.Message})");
                }
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
            if (peak > _peakSinceLastLog) _peakSinceLastLog = peak;

            var now = DateTime.UtcNow;
            if (now - _lastLogTime < LogInterval) return;
            _lastLogTime = now;

            int level = (int)Math.Round(Math.Min(1f, _peakSinceLastLog) * 100);
            Log($"Audio: level={level}");
            _peakSinceLastLog = 0f;
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
