using System;
using System.Threading;

namespace ESDeckPC
{
    /// <summary>
    /// Sends Now Playing progress (position/duration/playing) to the ESP32
    /// via HID Feature Report once per second while subscribed. Mirrors
    /// MonitorSender's Subscribe()/Unsubscribe() lifecycle and worker-thread
    /// pattern; reads live state from a NowPlayingWatcher instance rather
    /// than polling Windows itself.
    ///
    /// OUT report layout (65 bytes total: Report ID + 64 payload):
    ///
    ///   CMD_NOWPLAYING_PROGRESS (0x06):
    ///     [0]     Report ID   = 0x00
    ///     [1]     CMD         = 0x06
    ///     [2..5]  position_ms (uint32, little-endian)
    ///     [6..9]  duration_ms (uint32, little-endian)
    ///     [10]    playing     (0/1)
    ///     [11..64] reserved
    ///
    /// Numeric only -- title/artist are not part of this protocol yet
    /// (needs a PC-rendered image pipeline, no CJK font on-device). See
    /// doc/ESDeck_Media模式開發筆記.md 第 4 節.
    /// </summary>
    public class NowPlayingSender : IDisposable
    {
        private const int REPORT_SIZE = 65;
        private const byte CMD_NOWPLAYING_PROGRESS = 0x06;
        private const int SEND_PERIOD_MS = 1000;

        private readonly HidReceiver _receiver;
        private readonly NowPlayingWatcher _watcher;

        private Thread _workerThread;
        private volatile bool _subscribed = false;
        private volatile bool _disposed = false;

        // Lets the worker wake up and send immediately instead of waiting
        // out the rest of the 1s cycle -- see Nudge(). Without this, an ESP
        // button press (or any other play/pause/seek) could take up to
        // ~1s to show up on the ESP even though the PC-side state changed
        // right away.
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);

        public event Action<string> OnLog;

        public NowPlayingSender(HidReceiver receiver, NowPlayingWatcher watcher)
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
                Name = "NowPlayingSender_Worker",
            };
            _workerThread.Start();

            Log("NowPlaying: started sending");
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            bool joined = _workerThread?.Join(3000) ?? true;
            if (!joined)
                Log("NowPlaying: worker thread did not exit in time");

            _workerThread = null;

            Log("NowPlaying: stopped sending");
        }

        // ------------------------------------------------------------------
        // Worker -- normally one send per second, same cadence as
        // MonitorSender, but Nudge() can wake it early for an immediate
        // out-of-cycle send.
        // ------------------------------------------------------------------

        private void WorkerLoop()
        {
            while (_subscribed)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    SendProgress();
                }
                catch (Exception ex)
                {
                    Log($"NowPlaying: send error: {ex.Message}");
                }

                int remaining = SEND_PERIOD_MS - (int)sw.ElapsedMilliseconds;
                if (remaining > 0) _wake.WaitOne(remaining);
            }
        }

        /// <summary>
        /// Wakes the worker immediately to send the current state right
        /// now instead of waiting out the rest of the 1s cycle. Call this
        /// whenever something caused a real, out-of-band state change
        /// (e.g. NowPlayingWatcher.OnStateChanged). No-op while not
        /// subscribed.
        /// </summary>
        public void Nudge()
        {
            if (_subscribed) _wake.Set();
        }

        private void SendProgress()
        {
            uint positionMs = ClampToUInt32(_watcher.Position.TotalMilliseconds);
            uint durationMs = ClampToUInt32(_watcher.Duration.TotalMilliseconds);
            bool playing = _watcher.IsPlaying;

            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;
            report[1] = CMD_NOWPLAYING_PROGRESS;
            WriteUInt32LE(report, 2, positionMs);
            WriteUInt32LE(report, 6, durationMs);
            report[10] = (byte)(playing ? 1 : 0);
            // [11..64] reserved

            bool ok = _receiver.WriteReport(report);
            if (!ok)
                Log("NowPlaying: write failed (device disconnected?)");
        }

        private static uint ClampToUInt32(double totalMs)
        {
            if (double.IsNaN(totalMs) || totalMs < 0) return 0;
            if (totalMs > uint.MaxValue) return uint.MaxValue;
            return (uint)totalMs;
        }

        private static void WriteUInt32LE(byte[] buf, int offset, uint value)
        {
            buf[offset]     = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
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
            _wake.Dispose();
        }
    }
}
