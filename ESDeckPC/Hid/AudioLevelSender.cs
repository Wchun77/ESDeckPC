using System;
using System.Threading;

namespace ESDeckPC
{
    /// <summary>
    /// Sends the system audio output level to the ESP32 via HID Feature
    /// Report while subscribed, mirroring NowPlayingSender's
    /// Subscribe()/Unsubscribe() lifecycle and worker-thread pattern; reads
    /// live state from an AudioLevelWatcher instance.
    ///
    /// Sent at 10Hz (every 100ms) -- smoother than NowPlayingSender's 1Hz
    /// since this drives a visual VU-meter bar, but well short of the
    /// 20-30Hz the project notes eventually want for a proper spectrum
    /// bar (see doc/ESDeck_Media模式開發筆記.md 第 5 節); revisit once FFT
    /// band splitting replaces this single-value "簡單版".
    ///
    /// OUT report layout (65 bytes total: Report ID + 64 payload):
    ///
    ///   CMD_AUDIO_LEVEL (0x07):
    ///     [0]  Report ID = 0x00
    ///     [1]  CMD       = 0x07
    ///     [2]  level     (0-100)
    ///     [3..64] reserved
    /// </summary>
    public class AudioLevelSender : IDisposable
    {
        private const int REPORT_SIZE = 65;
        private const byte CMD_AUDIO_LEVEL = 0x07;
        private const int SEND_PERIOD_MS = 100;

        private readonly HidReceiver _receiver;
        private readonly AudioLevelWatcher _watcher;

        private Thread _workerThread;
        private volatile bool _subscribed = false;
        private volatile bool _disposed = false;

        public event Action<string> OnLog;

        public AudioLevelSender(HidReceiver receiver, AudioLevelWatcher watcher)
        {
            _receiver = receiver;
            _watcher = watcher;
        }

        // ------------------------------------------------------------------
        // Subscribe / Unsubscribe
        // ------------------------------------------------------------------

        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "AudioLevelSender_Worker",
            };
            _workerThread.Start();

            Log("Audio: started sending");
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            bool joined = _workerThread?.Join(3000) ?? true;
            if (!joined)
                Log("Audio: worker thread did not exit in time");

            _workerThread = null;

            Log("Audio: stopped sending");
        }

        // ------------------------------------------------------------------
        // Worker
        // ------------------------------------------------------------------

        private void WorkerLoop()
        {
            while (_subscribed)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    SendLevel();
                }
                catch (Exception ex)
                {
                    Log($"Audio: send error: {ex.Message}");
                }

                int remaining = SEND_PERIOD_MS - (int)sw.ElapsedMilliseconds;
                if (remaining > 0) Thread.Sleep(remaining);
            }
        }

        private void SendLevel()
        {
            int level = _watcher.Level;
            if (level < 0) level = 0;
            if (level > 100) level = 100;

            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;
            report[1] = CMD_AUDIO_LEVEL;
            report[2] = (byte)level;
            // [3..64] reserved

            bool ok = _receiver.WriteReport(report);
            if (!ok)
                Log("Audio: write failed (device disconnected?)");
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        // ------------------------------------------------------------------
        // IDisposable
        // ------------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Unsubscribe();
        }
    }
}
