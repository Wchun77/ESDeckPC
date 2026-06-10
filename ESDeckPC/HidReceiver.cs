using System;
using HidSharp;
using System.Threading;
using System.Threading.Tasks;

public class HidReceiver
{
    private const int VendorId = 0x303A;
    private const int ProductId = 0x4004;

    private HidDevice _device;
    private HidStream _stream;
    private CancellationTokenSource _cts;

    // Fires for normal button presses (page != 0xFF)
    public event Action<byte, byte> OnButtonPressed;

    // Fires when ESP sends monitor control: subscribe (0x01) or unsubscribe (0x02)
    public event Action<byte> OnMonitorControl;

    public bool Open()
    {
        var list = DeviceList.Local;
        _device = list.GetHidDeviceOrNull(VendorId, ProductId);
        if (_device == null) return false;

        try
        {
            _stream = _device.Open();
            _stream.ReadTimeout = 1000;
            System.Diagnostics.Debug.WriteLine(
                $"opened, max input={_device.GetMaxInputReportLength()}" +
                $" max feature={_device.GetMaxFeatureReportLength()}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"open error: {ex.Message}");
            return false;
        }
    }

    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => ReadLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _stream?.Close();
    }

    // ------------------------------------------------------------------
    // Send a Feature report to the device via SetReport (Control transfer).
    // report[0] must be the HID Report ID (0x00 when no Report IDs used).
    // Returns true on success.
    // ------------------------------------------------------------------

    public bool WriteReport(byte[] report)
    {
        if (_stream == null) return false;
        try
        {
            _stream.SetFeature(report);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetFeature error: {ex.Message}");
            return false;
        }
    }

    // ------------------------------------------------------------------
    // Read loop
    // ------------------------------------------------------------------

    private void ReadLoop(CancellationToken token)
    {
        var buf = new byte[65];
        while (!token.IsCancellationRequested)
        {
            try
            {
                int len = _stream.Read(buf, 0, buf.Length);
                if (len < 3) continue;

                byte page = buf[1];
                byte btn = buf[2];

                if (page == 0xFF)
                {
                    // Monitor control message from ESP
                    OnMonitorControl?.Invoke(btn);
                }
                else
                {
                    OnButtonPressed?.Invoke(page, btn);
                }
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"read error: {ex.Message}");
                break;
            }
        }
    }
}