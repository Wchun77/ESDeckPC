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
    /// Single background worker thread, single Computer instance:
    ///   every 1s  -- CPU / GPU / RAM / Network + HID send
    ///   every 10s -- Storage (SMART, slow I/O), piggybacked on the same tick
    ///
    /// NOTE: LibreHardwareMonitorLib's driver layer (Ring0 / kernel driver
    /// access) is a static, process-wide, non-thread-safe resource shared by
    /// every Computer instance. Using two Computer objects on two separate
    /// threads (the previous design) let Open()/Accept()/Close() race across
    /// threads, which could close or access the shared driver handle out from
    /// under the other thread and crash with an access violation inside
    /// LibreHardwareMonitorLib (e.g. Ring0.Close). Using a single Computer
    /// instance on a single thread makes that race impossible by construction.
    ///
    /// OUT report layout (65 bytes total: Report ID + 64 payload):
    ///
    ///   CMD_DATA (0x03):
    ///     [0]  Report ID   = 0x00
    ///     [1]  CMD         = 0x03
    ///     [2]  cpu_usage   0-100 %
    ///     [3]  cpu_temp    0-150 degrees C
    ///     [4]  ram_usage   0-100 %
    ///     [5]  gpu_usage   0-100 %
    ///     [6]  gpu_temp    0-150 degrees C
    ///     [7]  gpu_vram    0-100 %
    ///     [8]  cpu_freq    0-255 (MHz / 100)
    ///     [9]  net_up      0-255 MB/s
    ///     [10] net_down    0-255 MB/s
    ///     [11] disk_usage  0-100 % (Total Activity, updated every 10s)
    ///     [12] cpu_power   0-255 W
    ///     [13] gpu_power   0-255 (NVIDIA: Load %, AMD/Intel: W)
    ///     [14] ssd_life    0-100 % remaining (updated every 10s)
    ///     [15..64] reserved
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
        private const int REPORT_SIZE = 65;
        private const byte CMD_DATA = 0x03;
        private const byte CMD_TIME = 0x04;
        private const byte CMD_QUERY = 0x05;
        private const int STORAGE_PERIOD = 10;  // Storage poll every N fast-thread ticks

        // ------------------------------------------------------------------
        // Snapshot of one fast sensor poll cycle
        // ------------------------------------------------------------------

        private struct MonitorData
        {
            public byte CpuUsage;
            public byte CpuTemp;
            public byte RamUsage;
            public byte GpuUsage;
            public byte GpuTemp;
            public byte GpuVram;
            public byte CpuFreq;   // MHz / 100
            public byte NetUp;     // MB/s
            public byte NetDown;   // MB/s
            public byte CpuPower;  // W (capped at 255)
            public byte GpuPower;  // NVIDIA: Load %, AMD/Intel: W (capped at 255)
        }

        private readonly HidReceiver _receiver;

        // Single worker thread, single Computer instance (see class remarks)
        private Thread _workerThread;
        private Computer _computer;
        private volatile bool _subscribed = false;
        private volatile bool _disposed = false;

        // Storage values -- refreshed every STORAGE_PERIOD ticks
        private int _diskUsage = 0;  // 0-100 %
        private int _ssdLife = 0;  // 0-100 % remaining

        // Network delta
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

            _workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "MonitorSender_Worker",
            };
            _workerThread.Start();

            Log("Monitor: started sending");
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            // Join blocks until the worker loop finishes its current cycle
            // and calls Computer.Close() itself, from the same thread that
            // opened it. We deliberately do not touch _computer from here.
            bool joined = _workerThread?.Join(12000) ?? true;

            if (!joined)
                Log("Monitor: worker thread did not exit in time");

            _workerThread = null;

            _prevNetSent = -1;
            _prevNetReceived = -1;

            Log("Monitor: stopped sending");
        }

        // ------------------------------------------------------------------
        // Worker -- CPU / GPU / RAM / Network every 1s, Storage every
        // STORAGE_PERIOD ticks. Single thread, single Computer instance --
        // see class remarks for why this must not be split across threads.
        // ------------------------------------------------------------------

        private void WorkerLoop()
        {
            var computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
            };
            _computer = computer;
            computer.Open();
            computer.Accept(new UpdateVisitor());

            int tick = 0;

            while (_subscribed)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    computer.Accept(new UpdateVisitor());
                    MonitorData data = CollectFastSensors();

                    if (tick % STORAGE_PERIOD == 0)
                        CollectStorageSensors();

                    SendTimeSync();
                    SendData(data);
                }
                catch (Exception ex)
                {
                    Log($"Monitor error: {ex.Message}");
                }

                tick++;

                int remaining = 1000 - (int)sw.ElapsedMilliseconds;
                if (remaining > 0) Thread.Sleep(remaining);
            }

            // This thread created the Computer, and only this thread ever
            // touches it, so closing it here cannot race with anything.
            try
            {
                computer.Close();
            }
            catch (Exception ex)
            {
                Log($"Monitor: computer close error: {ex.Message}");
            }
            finally
            {
                _computer = null;
            }
        }

        // ------------------------------------------------------------------
        // Fast sensor collection
        // ------------------------------------------------------------------

        private MonitorData CollectFastSensors()
        {
            var data = new MonitorData();
            var computer = _computer;
            if (computer == null) return data;

            foreach (var hw in computer.Hardware)
            {
                switch (hw.HardwareType)
                {
                    case HardwareType.Cpu:
                        data.CpuUsage = ReadSensor(hw, SensorType.Load, "CPU Total", data.CpuUsage);
                        data.CpuTemp = ReadSensorMaxFiltered(hw, SensorType.Temperature, data.CpuTemp);
                        data.CpuFreq = ReadCpuFreq(hw, data.CpuFreq);
                        data.CpuPower = ReadSensor(hw, SensorType.Power, "Package", data.CpuPower);
                        break;

                    case HardwareType.Memory:
                        data.RamUsage = ReadSensor(hw, SensorType.Load, "Memory", data.RamUsage);
                        break;

                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        data.GpuUsage = ReadSensor(hw, SensorType.Load, "GPU Core", data.GpuUsage);
                        data.GpuTemp = ReadSensorMaxFiltered(hw, SensorType.Temperature, data.GpuTemp);
                        data.GpuVram = ReadSensor(hw, SensorType.Load, "GPU Memory", data.GpuVram);

                        // GPU power sensor name differs by vendor:
                        //   NVIDIA  -- Load  | "GPU Power"
                        //   AMD     -- Power | "GPU Package"
                        //   Intel   -- Power | "GPU Power"
                        data.GpuPower = ReadSensor(hw, SensorType.Load, "GPU Power", 0);
                        if (data.GpuPower == 0)
                            data.GpuPower = ReadSensor(hw, SensorType.Power, "GPU Package", 0);
                        if (data.GpuPower == 0)
                            data.GpuPower = ReadSensor(hw, SensorType.Power, "GPU Power", 0);
                        break;
                }
            }

            CollectNetwork(ref data);
            return data;
        }

        // ------------------------------------------------------------------
        // Storage sensor collection -- runs on slow thread
        // ------------------------------------------------------------------

        private void CollectStorageSensors()
        {
            var computer = _computer;
            if (computer == null) return;

            int diskUsage = 0;
            int ssdLife = 100;
            bool foundLife = false;

            foreach (var hw in computer.Hardware)
            {
                if (hw.HardwareType != HardwareType.Storage) continue;

                foreach (var sensor in hw.Sensors)
                {
                    if (!sensor.Value.HasValue) continue;

                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.IndexOf("Total Activity", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        float v = sensor.Value.Value;
                        if (v > diskUsage) diskUsage = (int)v;
                    }

                    if (sensor.SensorType == SensorType.Level &&
                        sensor.Name.IndexOf("Remaining Life", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Remaining Life: direct percentage remaining
                        if (!foundLife || (int)sensor.Value.Value < ssdLife)
                        {
                            ssdLife = (int)sensor.Value.Value;
                            foundLife = true;
                        }
                    }
                    else if (sensor.SensorType == SensorType.Level &&
                             sensor.Name.IndexOf("Percentage Used", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Percentage Used: invert to get remaining
                        int remaining = 100 - (int)sensor.Value.Value;
                        if (remaining < 0) remaining = 0;
                        if (!foundLife || remaining < ssdLife)
                        {
                            ssdLife = remaining;
                            foundLife = true;
                        }
                    }
                }
            }

            _diskUsage = diskUsage;
            _ssdLife = foundLife ? ssdLife : 0;
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

        /// <summary>
        /// Returns the maximum value among all sensors of the given type,
        /// excluding derived sensors such as "Distance to TjMax" that would
        /// produce misleadingly low readings when temperatures are high.
        /// </summary>
        private static byte ReadSensorMaxFiltered(IHardware hw, SensorType type, byte fallback)
        {
            float max = float.MinValue;
            bool found = false;

            foreach (var sensor in hw.Sensors)
            {
                if (sensor.SensorType != type) continue;
                if (!sensor.Value.HasValue) continue;

                // Skip derived / inverted sensors that do not represent real temperature
                string name = sensor.Name;
                if (name.IndexOf("Distance", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (name.IndexOf("TjMax", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (name.IndexOf("Average", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                if (sensor.Value.Value > max)
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

            float mhz = total / count / 100.0f;
            if (mhz < 0) mhz = 0;
            if (mhz > 255) mhz = 255;
            return (byte)mhz;
        }

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
        // Debug helper -- dump all sensors for a hardware node to the log.
        // Call LogSensors(hw, "CPU") / LogSensors(hw, "GPU") inside
        // CollectFastSensors when investigating a new machine, then remove
        // the calls once the sensor list is confirmed.
        // ------------------------------------------------------------------

        private void LogSensors(IHardware hw, string tag)
        {
            foreach (var sensor in hw.Sensors)
            {
                if (!sensor.Value.HasValue) continue;
                Log($"[{tag}] {sensor.SensorType} | {sensor.Name} = {sensor.Value.Value:F3}");
            }
        }

        // ------------------------------------------------------------------
        // Send helpers
        // ------------------------------------------------------------------

        private void SendData(MonitorData data)
        {
            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;
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
            report[11] = (byte)Math.Min(_diskUsage, 255);
            report[12] = data.CpuPower;
            report[13] = data.GpuPower;
            report[14] = (byte)Math.Min(_ssdLife, 255);
            // [15..64] reserved

            bool ok = _receiver.WriteReport(report);
            if (!ok)
                Log("Monitor: write failed (device disconnected?)");
        }

        private void SendTimeSync()
        {
            var now = DateTime.Now;

            var report = new byte[REPORT_SIZE];
            report[0] = 0x00;
            report[1] = CMD_TIME;
            report[2] = (byte)(now.Year - 2000);
            report[3] = (byte)now.Month;
            report[4] = (byte)now.Day;
            report[5] = (byte)now.Hour;
            report[6] = (byte)now.Minute;
            report[7] = (byte)now.Second;

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