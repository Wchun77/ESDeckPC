using System;
using System.Text;
using System.Threading;

namespace ESDeckPC
{
    /// <summary>
    /// Firmware update over HID -- mirrors HID_OTA_* in ESDeck's usb_hid.h.
    ///
    /// PC -> ESP (Feature report, [0] report ID, [1] cmd):
    ///   BEGIN 0x0B : [2..5] image size (uint32 LE)
    ///   DATA  0x0C : [2..3] chunk seq (uint16 LE), [4..64] 61 data bytes
    ///   ABORT 0x0D
    /// ESP -> PC (IN report, via HidReceiver.OnOtaStatus): status + uint32
    ///   ERASED 1 / ACK 2 (bytes consumed) / DONE 3 / ERROR 4 (code)
    ///
    /// Flow control: never more than Window bytes beyond the last ACK in
    /// flight (the ESP's receive buffer is twice that). The ESP erases the
    /// whole partition first, so the first ACK can take several seconds.
    /// Run() blocks -- call it from a background thread.
    /// </summary>
    public class OtaUploader
    {
        private const byte CMD_BEGIN = 0x0B;
        private const byte CMD_DATA = 0x0C;
        private const byte CMD_ABORT = 0x0D;
        private const byte ST_ERASED = 0x01;
        private const byte ST_ACK = 0x02;
        private const byte ST_DONE = 0x03;
        private const byte ST_ERROR = 0x04;

        private const int ReportSize = 65;
        private const int Chunk = 61;           // HID_OTA_CHUNK
        private const int Window = 8192;        // HID_OTA_WINDOW
        private const int EraseTimeoutMs = 60000;
        private const int AckTimeoutMs = 10000;
        private const int DoneTimeoutMs = 30000;

        // esp_image_header_t (24) + segment header (8) + esp_app_desc_t:
        // magic(4) secure_version(4) reserv1(8) version[32] project_name[32]
        private const int AppDescOffset = 32;
        private const int VersionOffset = AppDescOffset + 16;
        private const int ProjectOffset = VersionOffset + 32;

        private readonly HidReceiver _hid;
        private readonly object _lock = new object();
        private readonly AutoResetEvent _evt = new AutoResetEvent(false);
        private long _acked;
        private bool _erased;
        private bool _done;
        private int _error = -1;

        /// <summary>0..100 as data gets acknowledged (any thread).</summary>
        public event Action<int> OnProgress;
        public event Action<string> OnLog;

        public OtaUploader(HidReceiver hid)
        {
            _hid = hid;
        }

        /// <summary>Feed HidReceiver.OnOtaStatus here (read thread).</summary>
        public void HandleStatus(byte status, uint value)
        {
            lock (_lock)
            {
                switch (status)
                {
                    case ST_ERASED: _erased = true; break;
                    case ST_ACK: _erased = true; _acked = value; break;
                    case ST_DONE: _done = true; break;
                    case ST_ERROR: _error = (int)value; break;
                }
            }
            _evt.Set();
        }

        /// <summary>
        /// Basic check that this is an ESP app image built from the ESDeck
        /// project; returns null if OK, else the reason. The device checks
        /// the project name again before writing anything.
        /// </summary>
        public static string Validate(byte[] image, out string project, out string version)
        {
            project = version = "";
            if (image == null || image.Length < ProjectOffset + 32 || image[0] != 0xE9)
                return "Not an ESP32 firmware image (.bin).";
            project = ReadCStr(image, ProjectOffset, 32);
            version = ReadCStr(image, VersionOffset, 32);
            if (project.IndexOf("ESDeck", StringComparison.OrdinalIgnoreCase) < 0)
                return $"This image is for project \"{project}\", not ESDeck.";
            return null;
        }

        private static string ReadCStr(byte[] b, int off, int len)
        {
            int n = 0;
            while (n < len && b[off + n] != 0) n++;
            return Encoding.ASCII.GetString(b, off, n);
        }

        private static string ErrorText(int code)
        {
            switch (code)
            {
                case 1: return "an update is already running";
                case 2: return "image size rejected";
                case 3: return "data lost on the way (sequence error)";
                case 4: return "receive buffer overflow";
                case 5: return "flash write / verify failed (wrong image?)";
                case 6: return "device can't accept updates right now";
                default: return $"error {code}";
            }
        }

        public bool Run(byte[] image)
        {
            lock (_lock)
            {
                _acked = 0;
                _erased = _done = false;
                _error = -1;
            }
            _evt.Reset();

            var begin = new byte[ReportSize];
            begin[1] = CMD_BEGIN;
            uint size = (uint)image.Length;
            begin[2] = (byte)size; begin[3] = (byte)(size >> 8);
            begin[4] = (byte)(size >> 16); begin[5] = (byte)(size >> 24);
            if (!_hid.WriteReport(begin)) { Log("OTA: could not start (HID write failed)"); return false; }
            Log($"OTA: sending {image.Length / 1024} KB, device is erasing flash...");

            long sent = 0;
            ushort seq = 0;
            int lastPct = -1;
            var report = new byte[ReportSize];

            while (sent < image.Length)
            {
                long inFlight;
                bool erased;
                lock (_lock)
                {
                    if (_error >= 0) return Fail(ErrorText(_error));
                    inFlight = sent - _acked;
                    erased = _erased;
                    int pct = (int)(_acked * 100 / image.Length);
                    if (pct != lastPct) { lastPct = pct; OnProgress?.Invoke(pct); }
                }

                if (inFlight + Chunk > Window)
                {
                    if (!_evt.WaitOne(erased ? AckTimeoutMs : EraseTimeoutMs))
                        return Fail(erased ? "device stopped responding" : "erase timed out");
                    continue;
                }

                int n = (int)Math.Min(Chunk, image.Length - sent);
                Array.Clear(report, 0, report.Length);
                report[1] = CMD_DATA;
                report[2] = (byte)seq;
                report[3] = (byte)(seq >> 8);
                Buffer.BlockCopy(image, (int)sent, report, 4, n);
                if (!_hid.WriteReport(report)) return Fail("HID write failed");
                sent += n;
                seq++;
            }

            // All sent -- wait for the last ACKs and the verdict.
            var deadline = DateTime.UtcNow.AddMilliseconds(DoneTimeoutMs);
            while (true)
            {
                lock (_lock)
                {
                    if (_error >= 0) return Fail(ErrorText(_error));
                    int pct = (int)(_acked * 100 / image.Length);
                    if (pct != lastPct) { lastPct = pct; OnProgress?.Invoke(pct); }
                    if (_done) { Log("OTA: done, device is rebooting"); return true; }
                }
                int left = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (left <= 0) return Fail("no confirmation from device");
                _evt.WaitOne(Math.Min(left, 1000));
            }
        }

        private bool Fail(string why)
        {
            var abort = new byte[ReportSize];
            abort[1] = CMD_ABORT;
            _hid.WriteReport(abort);
            Log($"OTA failed: {why}");
            return false;
        }

        private void Log(string msg) => OnLog?.Invoke(msg);
    }
}
