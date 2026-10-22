using System;
using System.Text;
using HidSharp;
using System.Threading;
using System.Threading.Tasks;

public class HidReceiver
{
    // Mirrors ui_mode_t in ESDeck's ui_settings.h (0=deck, 1=monitor, 2=media, 3=touchpad)
    public enum EspMode { Deck = 0, Monitor = 1, Media = 2, Touchpad = 3 }

    private const int VendorId = 0x303A;
    // 0x4005: composite firmware (vendor HID + USB keyboard/mouse);
    // 0x4004: older vendor-only firmware. Both accepted.
    private static readonly int[] ProductIds = { 0x4005, 0x4004 };

    // The vendor interface is the one with the 64-byte feature report
    // (65 incl. report ID). The keyboard/mouse interface of the composite
    // firmware has a 1-byte feature report and is owned by Windows anyway.
    private const int VendorFeatureLen = 65;

    /// <summary>
    /// Finds the ESDeck vendor HID interface, or null. A composite device
    /// shows up as several HID devices with the same VID/PID, so picking
    /// by VID/PID alone could land on the keyboard/mouse one.
    /// </summary>
    public static HidDevice FindVendorDevice()
    {
        foreach (var d in DeviceList.Local.GetHidDevices(VendorId))
        {
            if (Array.IndexOf(ProductIds, d.ProductID) < 0) continue;
            try
            {
                // Exactly the vendor feature report: the Precision Touchpad
                // interface (fw 0x0102+) has a bigger one (256-byte blob).
                if (d.GetMaxFeatureReportLength() == VendorFeatureLen)
                    return d;
            }
            catch
            {
                // descriptor not readable (interface owned by the OS) -- skip
            }
        }
        return null;
    }

    private HidDevice _device;
    private HidStream _stream;
    private CancellationTokenSource _cts;

    // Fires when a whole Deck action has arrived (page=0xFB, chunked):
    // the pressed button as UTF-8 JSON text (label + action fields), see
    // HID_ACTION_* in ESDeck's usb_hid.h. Invoked on the read thread.
    public event Action<string> OnActionReceived;

    // Fires for firmware-update status reports (page=0xFA): status byte +
    // uint32 value, see OtaUploader. Invoked on the read thread.
    public event Action<byte, uint> OnOtaStatus;

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

    // Fires for Touchpad mode reports (page=0xFD), raw buffer + length as
    // read. Invoked synchronously on the read thread (low latency); the
    // buffer is reused by the next Read(), so handlers must not keep it.
    public event Action<byte[], int> OnTouchpadReport;

    // Fires for mini-keyboard reports (page=0xFC), same contract as
    // OnTouchpadReport (read thread, buffer reused after return).
    public event Action<byte[], int> OnKeyboardReport;

    public bool Open()
    {
        _device = FindVendorDevice();
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
    // Deck action reassembly (page 0xFB). Chunk layout after HidSharp's
    // report-ID byte [0]:
    //   [1] 0xFB  [2] msg_id  [3] seq  [4] flags (1=first, 2=last)
    //   [5..6] total length (uint16 LE)  [7..] data
    // The bytes are only decoded once the whole message is in: a UTF-8
    // character (e.g. a Chinese path) can be split across two chunks.
    // Read thread only, no locking needed.
    // ------------------------------------------------------------------

    private const byte ActionPage = 0xFB;
    private const byte ActionFlagFirst = 0x01;
    private const byte ActionFlagLast = 0x02;
    private const int ActionHeaderLen = 7;      // incl. report ID byte
    private const int ActionMaxLen = 4096;      // HID_ACTION_MAX_LEN

    private byte[] _actBuf;
    private int _actLen;
    private int _actGot;
    private byte _actId;
    private byte _actNextSeq;
    private bool _actActive;

    private void HandleActionChunk(byte[] buf, int len)
    {
        if (len <= ActionHeaderLen) return;

        byte id = buf[2];
        byte seq = buf[3];
        byte flags = buf[4];
        int total = buf[5] | (buf[6] << 8);

        if ((flags & ActionFlagFirst) != 0)
        {
            // A new message always restarts -- an unfinished older one is dropped.
            _actActive = false;
            if (total <= 0 || total > ActionMaxLen || seq != 0) return;
            _actBuf = new byte[total];
            _actLen = total;
            _actGot = 0;
            _actId = id;
            _actNextSeq = 0;
            _actActive = true;
        }

        if (!_actActive || id != _actId || seq != _actNextSeq || total != _actLen)
        {
            _actActive = false;   // lost / out-of-order chunk: give up on this message
            return;
        }

        int n = Math.Min(len - ActionHeaderLen, _actLen - _actGot);
        Buffer.BlockCopy(buf, ActionHeaderLen, _actBuf, _actGot, n);
        _actGot += n;
        _actNextSeq++;

        if (_actGot < _actLen && (flags & ActionFlagLast) == 0) return;

        _actActive = false;
        if (_actGot == _actLen && (flags & ActionFlagLast) != 0)
            OnActionReceived?.Invoke(Encoding.UTF8.GetString(_actBuf, 0, _actLen));
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
                    const byte BTN_MODE_TOUCHPAD = 0x06;

                    if (btn == BTN_MODE_DECK || btn == BTN_MODE_MONITOR || btn == BTN_MODE_MEDIA ||
                        btn == BTN_MODE_TOUCHPAD)
                    {
                        EspMode mode = btn == BTN_MODE_MONITOR  ? EspMode.Monitor
                                     : btn == BTN_MODE_MEDIA    ? EspMode.Media
                                     : btn == BTN_MODE_TOUCHPAD ? EspMode.Touchpad
                                     : EspMode.Deck;
                        OnModeReport?.Invoke(mode);
                    }
                    else
                        OnMonitorControl?.Invoke(btn);
                }
                else if (page == 0xFD)
                {
                    OnTouchpadReport?.Invoke(buf, len);
                }
                else if (page == 0xFC)
                {
                    OnKeyboardReport?.Invoke(buf, len);
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
                else if (page == ActionPage)
                {
                    HandleActionChunk(buf, len);
                }
                else if (page == 0xFA && len >= 7)
                {
                    uint value = (uint)(buf[3] | (buf[4] << 8) | (buf[5] << 16) | (buf[6] << 24));
                    OnOtaStatus?.Invoke(btn, value);
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