using System;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace ESDeckPC
{
    /// <summary>
    /// Collects system data via LibreHardwareMonitor and sends it to the
    /// ESP32 via HID Feature Report every second while subscribed.
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
    ///     [8..64] reserved
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
        }

        private readonly HidReceiver _receiver;
        private System.Windows.Forms.Timer _timer;
        private Computer _computer;
        private bool _subscribed = false;
        private bool _disposed = false;

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

            InitHardwareMonitor();

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += OnTick;
            _timer.Start();

            Log("Monitor: started sending");
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            _computer?.Close();
            _computer = null;

            Log("Monitor: stopped sending");
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
            };
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
        }

        // ------------------------------------------------------------------
        // Timer tick — collect and send
        // ------------------------------------------------------------------

        private void OnTick(object sender, EventArgs e)
        {
            if (!_subscribed) return;
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
                        break;

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
                }
            }

            return data;
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
            // [8..64] reserved, zero-filled by default

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