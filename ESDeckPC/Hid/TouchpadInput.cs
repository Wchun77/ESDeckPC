using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ESDeckPC
{
    /// <summary>
    /// Turns ESDeck Touchpad-mode HID reports (page 0xFD) into real mouse
    /// input via SendInput.
    ///
    /// Report layout (HidSharp buffer, [0] = report ID 0x00):
    ///   [1] 0xFD  [2] 0x01  [3..4] dx int16 LE  [5..6] dy int16 LE
    ///   [7] wheel int8 finger pixels (+ = scroll up)  [8] button mask
    ///   or, horizontal wheel only:
    ///   [1] 0xFD  [2] 0x02  [3] hwheel int8 finger pixels (+ = scroll right)  [4] button mask
    /// Button mask: bit0 L, bit1 R, bit3 Back (XBUTTON1), bit4 Forward (XBUTTON2).
    /// Mirrors HID_TP_* in ESDeck's usb_hid.h.
    ///
    /// Process() is called directly on HidReceiver's read thread -- no UI
    /// thread hop, so pointer latency stays at one HID poll interval.
    /// SendInput is thread-safe; the lock only guards the small amount of
    /// state below against ReleaseAll() from the UI thread.
    ///
    /// Note (Windows UIPI): SendInput cannot click into windows running at
    /// a higher integrity level than this app (elevated apps, UAC prompt,
    /// lock screen). The cursor still moves, clicks there are ignored.
    /// </summary>
    public class TouchpadInput
    {
        public const byte Page = 0xFD;
        private const byte CMD_MOVE = 0x01;
        private const byte CMD_HWHEEL = 0x02;
        private const byte BTN_LEFT = 0x01;
        private const byte BTN_RIGHT = 0x02;
        private const byte BTN_MIDDLE = 0x04;
        private const byte BTN_BACK = 0x08;
        private const byte BTN_FORWARD = 0x10;

        /// <summary>
        /// Multiplier from touch pixels to mouse mickeys, applied before
        /// Windows' own pointer speed / "Enhance pointer precision".
        /// </summary>
        public double Sensitivity { get; set; } = 2.0;

        /// <summary>
        /// Wheel units (WHEEL_DELTA = 120 per classic notch) per finger pixel
        /// on the ESP wheel strip. Sent as fractional deltas for smooth
        /// scrolling; 5.0 = one notch per 24 px, same speed as before.
        /// </summary>
        public double WheelPerPixel { get; set; } = 5.0;

        private readonly object _lock = new object();
        private double _remX;
        private double _remY;
        private double _remWheel;
        private double _remHWheel;
        private byte _mask;

        // ------------------------------------------------------------------
        // Win32
        // ------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;   // largest union member; keyboard/hardware not used here
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;

        private static INPUT Mouse(uint flags, int dx = 0, int dy = 0, int data = 0)
        {
            var i = new INPUT { type = INPUT_MOUSE };
            i.mi.dx = dx;
            i.mi.dy = dy;
            i.mi.mouseData = data;
            i.mi.dwFlags = flags;
            return i;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>buf/len exactly as returned by HidStream.Read().</summary>
        public void Process(byte[] buf, int len)
        {
            if (len < 5 || buf[1] != Page) return;
            if (buf[2] == CMD_HWHEEL) { ProcessHWheel(buf); return; }
            if (len < 9 || buf[2] != CMD_MOVE) return;

            short dx = (short)(buf[3] | (buf[4] << 8));
            short dy = (short)(buf[5] | (buf[6] << 8));
            sbyte wheel = unchecked((sbyte)buf[7]);
            byte mask = buf[8];

            var inputs = new List<INPUT>(4);

            lock (_lock)
            {
                // Order matters: move first, then wheel, then buttons -- the
                // ESP packs motion that happened before a button change into
                // the same report (e.g. the last bit of a drag before "up").
                if (dx != 0 || dy != 0)
                {
                    double fx = dx * Sensitivity + _remX;
                    double fy = dy * Sensitivity + _remY;
                    int ix = (int)Math.Truncate(fx);
                    int iy = (int)Math.Truncate(fy);
                    _remX = fx - ix;
                    _remY = fy - iy;
                    if (ix != 0 || iy != 0)
                        inputs.Add(Mouse(MOUSEEVENTF_MOVE, ix, iy));
                }

                // Partial (non-120) deltas: apps with smooth-scroll support
                // (browsers, Office, VS Code...) scroll by that amount; older
                // apps accumulate until a full notch, so nothing is lost.
                if (wheel != 0)
                {
                    double fw = wheel * WheelPerPixel + _remWheel;
                    int iw = (int)Math.Truncate(fw);
                    _remWheel = fw - iw;
                    if (iw != 0)
                        inputs.Add(Mouse(MOUSEEVENTF_WHEEL, data: iw));
                }

                AddButtonDiff(inputs, _mask, mask);
                _mask = mask;
            }

            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Release any button the ESP left held (device unplugged mid-drag,
        /// mode switched before the release report got out, ...).
        /// </summary>
        public void ReleaseAll()
        {
            var inputs = new List<INPUT>(2);
            lock (_lock)
            {
                AddButtonDiff(inputs, _mask, 0);
                _mask = 0;
                _remX = _remY = _remWheel = _remHWheel = 0;
            }
            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        private void ProcessHWheel(byte[] buf)
        {
            sbyte hwheel = unchecked((sbyte)buf[3]);
            byte mask = buf[4];
            var inputs = new List<INPUT>(3);
            lock (_lock)
            {
                if (hwheel != 0)
                {
                    double fw = hwheel * WheelPerPixel + _remHWheel;
                    int iw = (int)Math.Truncate(fw);
                    _remHWheel = fw - iw;
                    if (iw != 0)
                        inputs.Add(Mouse(MOUSEEVENTF_HWHEEL, data: iw));
                }
                AddButtonDiff(inputs, _mask, mask);
                _mask = mask;
            }
            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        private static void AddButtonDiff(List<INPUT> inputs, byte oldMask, byte newMask)
        {
            byte changed = (byte)(oldMask ^ newMask);
            if ((changed & BTN_LEFT) != 0)
                inputs.Add(Mouse((newMask & BTN_LEFT) != 0 ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP));
            if ((changed & BTN_RIGHT) != 0)
                inputs.Add(Mouse((newMask & BTN_RIGHT) != 0 ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP));
            if ((changed & BTN_MIDDLE) != 0)
                inputs.Add(Mouse((newMask & BTN_MIDDLE) != 0 ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP));
            // Back / Forward = mouse buttons 4 / 5 (two-finger swipe on the ESP)
            if ((changed & BTN_BACK) != 0)
                inputs.Add(Mouse((newMask & BTN_BACK) != 0 ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, data: XBUTTON1));
            if ((changed & BTN_FORWARD) != 0)
                inputs.Add(Mouse((newMask & BTN_FORWARD) != 0 ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP, data: XBUTTON2));
        }
    }
}
