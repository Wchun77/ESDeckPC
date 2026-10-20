using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ESDeckPC
{
    /// <summary>
    /// Turns ESDeck mini-keyboard HID reports (page 0xFC) into real key
    /// input via SendInput.
    ///
    /// Report layout (HidSharp buffer, [0] = report ID 0x00):
    ///   [1] 0xFC  [2] cmd (0x01 down / 0x02 up)  [3] vk  [4] mods
    ///   mods: bit0 Shift, bit1 Ctrl, bit2 Alt, bit3 Win
    /// Mirrors HID_KB_* in ESDeck's usb_hid.h.
    ///
    /// Keys are sent as virtual-key codes (plus the matching scan code),
    /// never as KEYEVENTF_UNICODE -- Unicode input bypasses the IME, VK input
    /// goes through it, so typing with the Chinese IME on works like a real
    /// keyboard. Modifiers are pressed before the key down and released
    /// after the key up. Auto-repeat arrives as repeated DOWN reports.
    ///
    /// Process() runs on HidReceiver's read thread; the lock only guards
    /// the held-key state against ReleaseAll() from the UI thread.
    /// Same UIPI note as TouchpadInput: no input into elevated windows.
    /// </summary>
    public class KeyboardInput
    {
        public const byte Page = 0xFC;
        private const byte CMD_DOWN = 0x01;
        private const byte CMD_UP = 0x02;

        private const byte MOD_SHIFT = 0x01;
        private const byte MOD_CTRL = 0x02;
        private const byte MOD_ALT = 0x04;
        private const byte MOD_WIN = 0x08;

        private static readonly (byte bit, ushort vk)[] ModKeys =
        {
            (MOD_CTRL,  0xA2),  // VK_LCONTROL
            (MOD_SHIFT, 0xA0),  // VK_LSHIFT
            (MOD_ALT,   0xA4),  // VK_LMENU
            (MOD_WIN,   0x5B),  // VK_LWIN
        };

        private readonly object _lock = new object();
        private byte _heldMods;
        private ushort _downVk;   // 0 = none

        // ------------------------------------------------------------------
        // Win32
        // ------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // Explicit layout so the union is as large as MOUSEINPUT (the biggest
        // member) -- SendInput rejects calls whose cbSize != sizeof(INPUT).
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MAPVK_VK_TO_VSC = 0;

        private static bool IsExtended(ushort vk)
        {
            switch (vk)
            {
                case 0x21: case 0x22: case 0x23: case 0x24:   // PgUp PgDn End Home
                case 0x25: case 0x26: case 0x27: case 0x28:   // arrows
                case 0x2D: case 0x2E:                         // Insert Delete
                case 0x5B: case 0x5C:                         // LWin RWin
                case 0xA3: case 0xA5:                         // RCtrl RAlt
                    return true;
                default:
                    return false;
            }
        }

        private static INPUT Key(ushort vk, bool up)
        {
            var i = new INPUT { type = INPUT_KEYBOARD };
            i.u.ki.wVk = vk;
            i.u.ki.wScan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            i.u.ki.dwFlags = (up ? KEYEVENTF_KEYUP : 0) | (IsExtended(vk) ? KEYEVENTF_EXTENDEDKEY : 0);
            return i;
        }

        private static void Send(List<INPUT> inputs)
        {
            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>buf/len exactly as returned by HidStream.Read().</summary>
        public void Process(byte[] buf, int len)
        {
            if (len < 5 || buf[1] != Page) return;

            byte cmd = buf[2];
            ushort vk = buf[3];
            byte mods = (byte)(buf[4] & (MOD_SHIFT | MOD_CTRL | MOD_ALT | MOD_WIN));
            if (vk == 0) return;

            var inputs = new List<INPUT>(8);
            lock (_lock)
            {
                if (cmd == CMD_DOWN)
                {
                    // Press modifiers this key needs that aren't held yet
                    // (repeats arrive with the same mods -> nothing extra).
                    foreach (var m in ModKeys)
                        if ((mods & m.bit) != 0 && (_heldMods & m.bit) == 0)
                            inputs.Add(Key(m.vk, up: false));
                    _heldMods |= mods;

                    inputs.Add(Key(vk, up: false));
                    _downVk = vk;
                }
                else if (cmd == CMD_UP)
                {
                    inputs.Add(Key(vk, up: true));
                    if (_downVk == vk) _downVk = 0;
                    AddModReleases(inputs);
                }
            }
            Send(inputs);
        }

        /// <summary>
        /// Release whatever is still held (device unplugged mid-key, ...).
        /// </summary>
        public void ReleaseAll()
        {
            var inputs = new List<INPUT>(5);
            lock (_lock)
            {
                if (_downVk != 0)
                {
                    inputs.Add(Key(_downVk, up: true));
                    _downVk = 0;
                }
                AddModReleases(inputs);
            }
            Send(inputs);
        }

        // Caller holds _lock. Reverse order of press.
        private void AddModReleases(List<INPUT> inputs)
        {
            for (int i = ModKeys.Length - 1; i >= 0; i--)
                if ((_heldMods & ModKeys[i].bit) != 0)
                    inputs.Add(Key(ModKeys[i].vk, up: true));
            _heldMods = 0;
        }
    }
}
