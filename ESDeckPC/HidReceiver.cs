using System;
using HidSharp;
using System.Threading;
using System.Threading.Tasks;

public class HidReceiver
{
    // Mirrors ui_mode_t in ESDeck's ui_settings.h (0=deck, 1=monitor, 2=media)
    public enum EspMode { Deck = 0, Monitor = 1, Media = 2 }

    private const int VendorId = 0x303A;
    private const int ProductId = 0x4004;

    private HidDevice _device;
    private HidStream _stream;
    private CancellationTokenSource _cts;

    // Fires for normal button presses (page != 0xFF)
    public event Action<byte, byte> OnButtonPressed;

    // Fires when ESP sends monitor control: subscribe (0x01) or unsubscribe (0x02)
    public event Action<byte> OnMonitorControl;

    // Fires when ESP sends media control (page=0xFE): subscribe (0x01) or
    // unsubscribe (0x02) -- independent namespace from monitor's page=0xFF.
    public event Action<byte> OnMediaControl;

    // Fires when ESP sends a seek request (page=0xFE, btn=0x06) with the
    // target position in milliseconds.
    public event Action<uint> OnMediaSeek;

    // Fires when ESP replies to a mode query with its current UI mode
    public event Action<EspMode> OnModeReport;

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
    // The buffer is automatically padded or trimmed to match the device's
    // declared max feature report length so the HID driver accepts it.
    // Returns true on success.
    // ------------------------------------------------------------------

    public bool WriteReport(byte[] report)
    {
        if (_stream == null) return false;
        try
        {
            int required = _device.GetMaxFeatureReportLength();
            byte[] buf = report;
            if (report.Length != required)
            {
                buf = new byte[required];
                Buffer.BlockCopy(report, 0, buf, 0,
                                 Math.Min(report.Length, required));
            }
            _stream.SetFeature(buf);
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
                    const byte BTN_SUBSCRIBE = 0x01;
                    const byte BTN_UNSUBSCRIBE = 0x02;
                    const byte BTN_MODE_DECK = 0x03;
                    const byte BTN_MODE_MONITOR = 0x04;
                    const byte BTN_MODE_MEDIA = 0x05;

                    if (btn == BTN_MODE_DECK || btn == BTN_MODE_MONITOR || btn == BTN_MODE_MEDIA)
                    {
                        EspMode mode = btn == BTN_MODE_MONITOR ? EspMode.Monitor
                                     : btn == BTN_MODE_MEDIA   ? EspMode.Media
                                     : EspMode.Deck;
                        OnModeReport?.Invoke(mode);
                    }
                    else
                        OnMonitorControl?.Invoke(btn);
                }
                else if (page == 0xFE)
                {
                    const byte BTN_SEEK = 0x06;

                    if (btn == BTN_SEEK && len >= 7)
                    {
                        uint positionMs = (uint)(buf[3] | (buf[4] << 8) | (buf[5] << 16) | (buf[6] << 24));
                        OnMediaSeek?.Invoke(positionMs);
                    }
                    else
                    {
                        OnMediaControl?.Invoke(btn);
                    }
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