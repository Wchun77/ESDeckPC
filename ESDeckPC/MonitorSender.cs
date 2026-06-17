using System;
using System.Net.NetworkInformation;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace ESDeckPC
{
    /// <summary>
    /// Collects system data via LibreHardwareMonitor and sends it to the
    /// ESP32 via HID Feature Report every second while subscribed.
    ///
    /// Runs entirely on a background thread -- no UI thread involvement.
    /// HidSharp WriteReport is thread-safe so HID sends happen directly
    /// from the background thread without marshalling back to the UI.
    ///
    /// OUT report layout (65 bytes total: Report ID + 64 payload):
    ///
    ///   CMD_DATA (0x03):
    ///     [0]  Report ID  = 0x00
    ///     [1]  CMD        = 0x03
    ///     [2]  cpu_usage  0-100 %
    ///     [3]  cpu_temp   0-150 degrees C
    ///     [4]  ram_usage  0-100 %
    ///     [5]  gpu_usage  0-100 %
    ///     [6]  gpu_temp   0-150 degrees C
    ///     [7]  gpu_vram   0-100 %
    ///     [8]  cpu_freq   0-255 (MHz / 100, max 25500 MHz)
    ///     [9]  net_up     0-255 MB/s
    ///     [10] net_down   0-255 MB/s
    ///     [11] disk_usage 0-100 %
    ///     [12..64] reserved
    ///
    ///   CMD_TIME (0x04):
    ///     [0]  Report ID  = 0x00
    ///     [1]  CMD        = 0x04
    ///     [2]  year       (year - 2000)
    ///     [3]  month      1-12
    ///     [4]  day        1-31
    ///     [5]  hour       0-23
    ///     [6]  minute     0-59
    ///     [7]  second     0-59
    ///     [8..64] reserved
    ///
    ///   CMD_QUERY (0x05):
    ///     [0]  Report ID  = 0x00
    ///     [1]  CMD        = 0x05
    ///     [2..64] unused
    /// </summary>
    public class MonitorSender : IDisposable
    {
        // Report ID byte + 64-byte payload = 65 total.
        // Must match HID_FEATURE_PAYLOAD_SIZE in usb_hid.h.
        private const int REPORT_SIZE = 65;

        private const byte CMD_DATA = 0x03;
        private const byte CMD_TIME = 0x04;
        private const byte CMD_QUERY = 0x05;

        // cpu_freq encoding: value = MHz / 100, capped at 255 (= 25.5 GHz)
        // ------------------------------------------------------------------
        // Snapshot of one sensor poll cycle
        // ------------------------------------------------------------------

        private struct MonitorData
        {
            public byte CpuUsage;
            public byte CpuTemp;
            public byte RamUsage;
            public byte GpuUsage;
            public byte GpuTemp;
            public byte GpuVram;
            public byte CpuFreq;    // % of rated max (>100 = overclocked, capped at 200)
            public byte NetUp;      // MB/s
            public byte NetDown;    // MB/s
            public byte DiskUsage;
        }

        private readonly HidReceiver _receiver;
        private Thread _thread;
        private Computer _computer;
        private volatile bool _subscribed = false;
        private volatile bool _disposed = false;

        // Network: previous byte counts for delta calculation
        private long _prevNetSent = -1;
        private long _prevNetReceived = -1;

        public event Action<string> OnLog;

        public MonitorSender(HidReceiver receiver)
        {
            _receiver = receiver;
        }

        // ------------------------------------------------------------------
        // Subscribe / Unsubscribe
        // ------------------------------------------------------------------

        public void SendQuery()
        {
            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;
            report[1] = CMD_QUERY;
            _receiver.WriteReport(report);
            Log("Monitor: sent mode query");
        }

        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            _thread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "MonitorSender",
            };
            _thread.Start();

            Log("Monitor: started sending");
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            // Thread will exit on its own when _subscribed goes false.
            // Join with a timeout so we don't block the UI indefinitely.
            _thread?.Join(2000);
            _thread = null;

            _computer?.Close();
            _computer = null;

            _prevNetSent = -1;
            _prevNetReceived = -1;

            Log("Monitor: stopped sending");
        }

        // ------------------------------------------------------------------
        // Background worker -- runs entirely off the UI thread
        // ------------------------------------------------------------------

        private void WorkerLoop()
        {
            InitHardwareMonitor();

            while (_subscribed)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    _computer.Accept(new UpdateVisitor());
                    MonitorData data = CollectSensors();
                    SendTimeSync();
                    SendData(data);
                }
                catch (Exception ex)
                {
                    Log($"Monitor collect error: {ex.Message}");
                }

                // Sleep for the remainder of the 1-second interval
                int elapsed = (int)sw.ElapsedMilliseconds;
                int remaining = 1000 - elapsed;
                if (remaining > 0)
                    Thread.Sleep(remaining);
            }

            _computer?.Close();
            _computer = null;
        }

        // ------------------------------------------------------------------
        // Hardware monitor init
        // ------------------------------------------------------------------

        private void InitHardwareMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
            };
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
        }

        // ------------------------------------------------------------------
        // Sensor collection
        // ------------------------------------------------------------------

        private MonitorData CollectSensors()
        {
            var data = new MonitorData();

            foreach (var hw in _computer.Hardware)
            {
                switch (hw.HardwareType)
                {
                    case HardwareType.Cpu:
                        data.CpuUsage = ReadSensor(hw, SensorType.Load, "CPU Total", data.CpuUsage);
                        data.CpuTemp = ReadSensorMax(hw, SensorType.Temperature, data.CpuTemp);
                        data.CpuFreq = ReadCpuFreq(hw, data.CpuFreq); break;

                    case HardwareType.Memory:
                        data.RamUsage = ReadSensor(hw, SensorType.Load, "Memory", data.RamUsage);
                        break;

                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        data.GpuUsage = ReadSensor(hw, SensorType.Load, "GPU Core", data.GpuUsage);
                        data.GpuTemp = ReadSensorMax(hw, SensorType.Temperature, data.GpuTemp);
                        data.GpuVram = ReadSensor(hw, SensorType.Load, "GPU Memory", data.GpuVram);
                        break;

                    case HardwareType.Storage:
                        data.DiskUsage = ReadSensor(hw, SensorType.Load, "Total Activity", data.DiskUsage);
                        break;
                }
            }

            CollectNetwork(ref data);
            return data;
        }

        // ------------------------------------------------------------------
        // CPU frequency -- average across all cores encoded as MHz / 100.
        // Intel: "CPU Core #N", AMD: "Core #N" Clock sensors.
        // Excludes "Bus Speed" (~100 MHz, also Clock type).
        // Max value 255 = 25500 MHz, sufficient for any current CPU.
        // ------------------------------------------------------------------

        private static byte ReadCpuFreq(IHardware hw, byte fallback)
        {
            float total = 0;
            int count = 0;

            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType != SensorType.Clock) continue;
                if (!sensor.Value.HasValue) continue;

                string name = sensor.Name;
                bool isIntelCore = name.IndexOf("CPU Core #", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isAmdCore = name.StartsWith("Core #", StringComparison.OrdinalIgnoreCase);
                if (!isIntelCore && !isAmdCore) continue;

                total += sensor.Value.Value;
                count++;
            }

            if (count == 0) return fallback;

            float mhz = total / count / 100.0f;   /* encode as MHz / 100 */
            if (mhz < 0) mhz = 0;
            if (mhz > 255) mhz = 255;
            return (byte)mhz;
        }

        // ------------------------------------------------------------------
        // Network -- delta bytes since last poll, converted to MB/s
        // ------------------------------------------------------------------

        private void CollectNetwork(ref MonitorData data)
        {
            try
            {
                long sent = 0;
                long received = 0;
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    var stats = nic.GetIPv4Statistics();
                    sent += stats.BytesSent;
                    received += stats.BytesReceived;
                }

                if (_prevNetSent >= 0)
                {
                    float upMB = (sent - _prevNetSent) / 1048576.0f;
                    float downMB = (received - _prevNetReceived) / 1048576.0f;
                    if (upMB < 0) upMB = 0;
                    if (downMB < 0) downMB = 0;
                    if (upMB > 255) upMB = 255;
                    if (downMB > 255) downMB = 255;
                    data.NetUp = (byte)upMB;
                    data.NetDown = (byte)downMB;
                }

                _prevNetSent = sent;
                _prevNetReceived = received;
            }
            catch
            {
                // Network stats unavailable -- leave as zero
            }
        }

        // ------------------------------------------------------------------
        // Sensor helpers
        // ------------------------------------------------------------------

        private static byte ReadSensor(IHardware hw, SensorType type,
                                       string nameFragment, byte fallback)
        {
            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType == type &&
                    sensor.Name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    sensor.Value.HasValue)
                {
                    float v = sensor.Value.Value;
                    if (v < 0) v = 0;
                    if (v > 255) v = 255;
                    return (byte)v;
                }
            }
            return fallback;
        }

        private static byte ReadSensorMax(IHardware hw, SensorType type, byte fallback)
        {
            float max = float.MinValue;
            bool found = false;

            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType == type && sensor.Value.HasValue &&
                    sensor.Value.Value > max)
                {
                    max = sensor.Value.Value;
                    found = true;
                }
            }

            if (!found) return fallback;
            if (max < 0) max = 0;
            if (max > 255) max = 255;
            return (byte)max;
        }

        // ------------------------------------------------------------------
        // Send helpers
        // ------------------------------------------------------------------

        private void SendData(MonitorData data)
        {
            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;           // Report ID
            report[1] = CMD_DATA;
            report[2] = data.CpuUsage;
            report[3] = data.CpuTemp;
            report[4] = data.RamUsage;
            report[5] = data.GpuUsage;
            report[6] = data.GpuTemp;
            report[7] = data.GpuVram;
            report[8] = data.CpuFreq;
            report[9] = data.NetUp;
            report[10] = data.NetDown;
            report[11] = data.DiskUsage;
            // [12..64] reserved, zero-filled by default

            bool ok = _receiver.WriteReport(report);
            if (!ok)
                Log("Monitor: write failed (device disconnected?)");
        }

        private void SendTimeSync()
        {
            var now = DateTime.Now;

            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;           // Report ID
            report[1] = CMD_TIME;
            report[2] = (byte)(now.Year - 2000);
            report[3] = (byte)now.Month;
            report[4] = (byte)now.Day;
            report[5] = (byte)now.Hour;
            report[6] = (byte)now.Minute;
            report[7] = (byte)now.Second;
            // [8..64] reserved, zero-filled by default

            bool ok = _receiver.WriteReport(report);
            if (!ok) Log("Monitor: time sync write failed");
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

    // ------------------------------------------------------------------
    // LibreHardwareMonitor visitor required to trigger sensor updates
    // ------------------------------------------------------------------

    internal class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}