using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace ESDeckPC
{
    /// <summary>
    /// Collects system data via LibreHardwareMonitor and sends it to the
    /// ESP32 via HID Output Report every second while subscribed.
    ///
    /// Data format (7-byte OUT report):
    ///   byte[0] = 0x03 (cmd: data)
    ///   byte[1] = cpu_usage  (0-100)
    ///   byte[2] = cpu_temp   (0-150 degrees C)
    ///   byte[3] = ram_usage  (0-100)
    ///   byte[4] = gpu_usage  (0-100)
    ///   byte[5] = reserved
    ///   byte[6] = reserved
    /// </summary>
    public class MonitorSender : IDisposable
    {
        private const byte CMD_DATA = 0x03;

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

        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            InitHardwareMonitor();

            // Send time sync first before starting the data timer
            SendTimeSync();

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

                byte cpuUsage = 0, cpuTemp = 0, ramUsage = 0, gpuUsage = 0;

                foreach (var hw in _computer.Hardware)
                {
                    switch (hw.HardwareType)
                    {
                        case HardwareType.Cpu:
                            cpuUsage = ReadSensor(hw, SensorType.Load, "CPU Total", cpuUsage);
                            cpuTemp = ReadSensorMax(hw, SensorType.Temperature, cpuTemp);
                            break;

                        case HardwareType.Memory:
                            ramUsage = ReadSensor(hw, SensorType.Load, "Memory", ramUsage);
                            break;

                        case HardwareType.GpuNvidia:
                        case HardwareType.GpuAmd:
                        case HardwareType.GpuIntel:
                            gpuUsage = ReadSensor(hw, SensorType.Load, "GPU Core", gpuUsage);
                            break;
                    }
                }

                SendData(cpuUsage, cpuTemp, ramUsage, gpuUsage);
            }
            catch (Exception ex)
            {
                Log($"Monitor collect error: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Sensor helper — find sensor by name fragment and type
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
                if (sensor.SensorType == type && sensor.Value.HasValue)
                {
                    if (sensor.Value.Value > max)
                    {
                        max = sensor.Value.Value;
                        found = true;
                    }
                }
            }
            if (!found) return fallback;
            if (max < 0) max = 0;
            if (max > 255) max = 255;
            return (byte)max;
        }

        // ------------------------------------------------------------------
        // Send OUT report via HidReceiver
        // ------------------------------------------------------------------

        private void SendData(byte cpuUsage, byte cpuTemp,
                      byte ramUsage, byte gpuUsage)
        {
            var report = new byte[9];   // Report ID + 8 bytes
            report[0] = 0x00;           // Report ID
            report[1] = CMD_DATA;
            report[2] = cpuUsage;
            report[3] = cpuTemp;
            report[4] = ramUsage;
            report[5] = gpuUsage;
            report[6] = 0x00;
            report[7] = 0x00;
            report[8] = 0x00;

            bool ok = _receiver.WriteReport(report);
            if (!ok)
                Log("Monitor: write failed (device disconnected?)");
        }

        private void SendTimeSync()
        {
            const byte CMD_TIME = 0x04;
            var now = DateTime.Now;

            var report = new byte[9];   // Report ID + 8 bytes
            report[0] = 0x00;           // Report ID
            report[1] = CMD_TIME;
            report[2] = (byte)(now.Year - 2000);
            report[3] = (byte)now.Month;
            report[4] = (byte)now.Day;
            report[5] = (byte)now.Hour;
            report[6] = (byte)now.Minute;
            report[7] = (byte)now.Second;
            report[8] = 0x00;

            bool ok = _receiver.WriteReport(report);
            Log(ok ? $"Monitor: time sync sent {now:yyyy-MM-dd HH:mm:ss}"
                   : "Monitor: time sync write failed");
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
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

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