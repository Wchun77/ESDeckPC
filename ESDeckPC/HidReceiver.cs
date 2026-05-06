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

    public event Action<byte, byte> OnButtonPressed;

    public bool Open()
    {
        var list = DeviceList.Local;
        _device = list.GetHidDeviceOrNull(VendorId, ProductId);
        if (_device == null) return false;

        try
        {
            _stream = _device.Open();
            _stream.ReadTimeout = 1000;
            System.Diagnostics.Debug.WriteLine($"opened, max input={_device.GetMaxInputReportLength()}");
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

    private void ReadLoop(CancellationToken token)
    {
        var buf = new byte[65];
        while (!token.IsCancellationRequested)
        {
            try
            {
                int len = _stream.Read(buf, 0, buf.Length);
                if (len >= 3)
                {
                    byte page = buf[1];
                    byte btn = buf[2];
                    OnButtonPressed?.Invoke(page, btn);
                }
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"error: {ex.Message}");
                break;
            }
        }
    }
}