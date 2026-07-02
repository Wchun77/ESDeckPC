using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public static class ActionExecutor
    {
        // ------------------------------------------------------------------
        // P/Invoke
        // ------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

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
            [FieldOffset(0)] public HARDWAREINPUT hi;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        // Extended keys that require KEYEVENTF_EXTENDEDKEY
        private static readonly HashSet<byte> ExtendedKeys = new HashSet<byte>
        {
            0x5B, // VK_LWIN
            0x5C, // VK_RWIN
            0x11, // VK_CONTROL
            0x12, // VK_MENU
            0x2E, // VK_DELETE
            0x26, // VK_UP
            0x28, // VK_DOWN
            0x25, // VK_LEFT
            0x27, // VK_RIGHT
        };

        // ------------------------------------------------------------------
        // Tables
        // ------------------------------------------------------------------

        private static readonly Dictionary<string, byte> MediaKeys = new Dictionary<string, byte>
        {
            { "play_pause", 0xB3 },
            { "stop",       0xB2 },
            { "prev",       0xB1 },
            { "next",       0xB0 },
            { "vol_up",     0xAF },
            { "vol_down",   0xAE },
            { "mute",       0xAD },
        };

        private static readonly Dictionary<string, byte> VkMap = new Dictionary<string, byte>
        {
            { "win",   0x5B },
            { "ctrl",  0x11 },
            { "alt",   0x12 },
            { "shift", 0x10 },
            { "enter", 0x0D },
            { "tab",   0x09 },
            { "esc",   0x1B },
            { "del",   0x2E },
            { "up",    0x26 },
            { "down",  0x28 },
            { "left",  0x25 },
            { "right", 0x27 },
            { "f1",    0x70 }, { "f2",  0x71 }, { "f3",  0x72 }, { "f4",  0x73 },
            { "f5",    0x74 }, { "f6",  0x75 }, { "f7",  0x76 }, { "f8",  0x77 },
            { "f9",    0x78 }, { "f10", 0x79 }, { "f11", 0x7A }, { "f12", 0x7B },
            { "prtscr", 0x2C },
        };

        private static readonly Dictionary<string, string> SendKeysModifiers = new Dictionary<string, string>
        {
            { "ctrl",  "^" },
            { "alt",   "%" },
            { "shift", "+" },
        };

        private static readonly Dictionary<string, string> SendKeysSpecial = new Dictionary<string, string>
        {
            { "enter", "{ENTER}" }, { "tab",   "{TAB}"   },
            { "esc",   "{ESC}"   }, { "del",   "{DEL}"   },
            { "up",    "{UP}"    }, { "down",  "{DOWN}"  },
            { "left",  "{LEFT}"  }, { "right", "{RIGHT}" },
            { "f1",  "{F1}"  }, { "f2",  "{F2}"  }, { "f3",  "{F3}"  }, { "f4",  "{F4}"  },
            { "f5",  "{F5}"  }, { "f6",  "{F6}"  }, { "f7",  "{F7}"  }, { "f8",  "{F8}"  },
            { "f9",  "{F9}"  }, { "f10", "{F10}" }, { "f11", "{F11}" }, { "f12", "{F12}" },
        };

        // ------------------------------------------------------------------
        // HID keycode to Windows VK mapping
        // Only functional keys that cannot use Unicode path
        // ------------------------------------------------------------------
        private static readonly Dictionary<byte, byte> HidToVkTable = new Dictionary<byte, byte>
        {
            { 0x28, 0x0D }, // Enter
            { 0x2A, 0x08 }, // Backspace
            { 0x2B, 0x09 }, // Tab
            { 0x29, 0x1B }, // Escape
            { 0x2C, 0x20 }, // Space
            { 0x4F, 0x27 }, // Right arrow
            { 0x50, 0x25 }, // Left arrow
            { 0x51, 0x28 }, // Down arrow
            { 0x52, 0x26 }, // Up arrow
        };

        // HID keycodes that are plain characters — handled via Unicode path
        // HID 0x04-0x27 = a-z + digits, 0x2D-0x38 = punctuation
        // Map HID → Unicode char (unshifted)
        private static readonly Dictionary<byte, char> HidToCharUnshifted = new Dictionary<byte, char>
        {
            { 0x04, 'a' }, { 0x05, 'b' }, { 0x06, 'c' }, { 0x07, 'd' },
            { 0x08, 'e' }, { 0x09, 'f' }, { 0x0A, 'g' }, { 0x0B, 'h' },
            { 0x0C, 'i' }, { 0x0D, 'j' }, { 0x0E, 'k' }, { 0x0F, 'l' },
            { 0x10, 'm' }, { 0x11, 'n' }, { 0x12, 'o' }, { 0x13, 'p' },
            { 0x14, 'q' }, { 0x15, 'r' }, { 0x16, 's' }, { 0x17, 't' },
            { 0x18, 'u' }, { 0x19, 'v' }, { 0x1A, 'w' }, { 0x1B, 'x' },
            { 0x1C, 'y' }, { 0x1D, 'z' },
            { 0x1E, '1' }, { 0x1F, '2' }, { 0x20, '3' }, { 0x21, '4' },
            { 0x22, '5' }, { 0x23, '6' }, { 0x24, '7' }, { 0x25, '8' },
            { 0x26, '9' }, { 0x27, '0' },
            { 0x2D, '-' }, { 0x2E, '=' }, { 0x2F, '[' }, { 0x30, ']' },
            { 0x31, '\\'},{ 0x33, ';' }, { 0x34, '\''}, { 0x35, '`' },
            { 0x36, ',' }, { 0x37, '.' }, { 0x38, '/' },
        };

        // Shifted variants
        private static readonly Dictionary<byte, char> HidToCharShifted = new Dictionary<byte, char>
        {
            { 0x04, 'A' }, { 0x05, 'B' }, { 0x06, 'C' }, { 0x07, 'D' },
            { 0x08, 'E' }, { 0x09, 'F' }, { 0x0A, 'G' }, { 0x0B, 'H' },
            { 0x0C, 'I' }, { 0x0D, 'J' }, { 0x0E, 'K' }, { 0x0F, 'L' },
            { 0x10, 'M' }, { 0x11, 'N' }, { 0x12, 'O' }, { 0x13, 'P' },
            { 0x14, 'Q' }, { 0x15, 'R' }, { 0x16, 'S' }, { 0x17, 'T' },
            { 0x18, 'U' }, { 0x19, 'V' }, { 0x1A, 'W' }, { 0x1B, 'X' },
            { 0x1C, 'Y' }, { 0x1D, 'Z' },
            { 0x1E, '!' }, { 0x1F, '@' }, { 0x20, '#' }, { 0x21, '$' },
            { 0x22, '%' }, { 0x23, '^' }, { 0x24, '&' }, { 0x25, '*' },
            { 0x26, '(' }, { 0x27, ')' },
            { 0x2D, '_' }, { 0x2E, '+' }, { 0x2F, '{' }, { 0x30, '}' },
            { 0x31, '|' }, { 0x33, ':' }, { 0x34, '"' }, { 0x35, '~' },
            { 0x36, '<' }, { 0x37, '>' }, { 0x38, '?' },
        };

        // ------------------------------------------------------------------
        // Public entry point
        // ------------------------------------------------------------------

        public static string Run(PcConfig config, byte page, byte btn)
        {
            if (config == null)
                return "config not loaded";

            if (page == 0x00)
                return ExecKeyboard(btn);

            int pageIdx = page - 1;
            int btnIdx = btn - 1;

            if (pageIdx < 0 || pageIdx >= config.Pages.Count)
                return $"page {page} not found";

            var pg = config.Pages[pageIdx];

            if (btnIdx < 0 || btnIdx >= pg.Buttons.Count)
                return $"page {page} btn {btn} not found";

            var button = pg.Buttons[btnIdx];

            switch (button.Action?.ToLower())
            {
                case "launch": return ExecLaunch(button.Target);
                case "hotkey": return ExecHotkey(button.Keys);
                case "media": return ExecMedia(button.Target);
                case "discord": return ExecDiscord(button.Target, button.ChannelId);
                case "scroll": return ExecScroll(button.Target, button.Amount);
                case "sequence": return ExecSequence(button.Keys);
                case "text": return ExecText(button.Target);
                case "mouse_click": return ExecMouseClick(button.Target);
                default: return $"unknown action: {button.Action}";
            }
        }

        // ------------------------------------------------------------------
        // Action handlers
        // ------------------------------------------------------------------

        private static string ExecLaunch(string target)
        {
            if (string.IsNullOrEmpty(target))
                return "launch failed: target is empty";
            try
            {
                bool isExecutable = target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                                 || target.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)
                                 || target.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);

                ProcessStartInfo psi = isExecutable
                    ? new ProcessStartInfo(target) { UseShellExecute = true }
                    : new ProcessStartInfo("explorer.exe", $"\"{target}\"") { UseShellExecute = true };

                Process.Start(psi);
                return $"launch: {target}";
            }
            catch (Exception ex)
            {
                return $"launch failed: {ex.Message}";
            }
        }

        private static string ExecHotkey(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
                return "hotkey failed: keys is empty";

            try
            {
                bool hasWin = keys.Exists(k => k.ToLower() == "win");

                if (hasWin)
                    SendInputCombo(keys);
                else
                    SendKeys.SendWait(BuildSendKeys(keys));

                return $"hotkey: {string.Join("+", keys)}";
            }
            catch (Exception ex)
            {
                return $"hotkey failed: {ex.Message}";
            }
        }

        private static string ExecMedia(string target)
        {
            if (string.IsNullOrEmpty(target))
                return "media failed: target is empty";

            if (!MediaKeys.TryGetValue(target.ToLower(), out byte vk))
                return $"media failed: unknown command {target}";

            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            return $"media: {target}";
        }

        // ------------------------------------------------------------------
        // Discord action handler
        // target: mute | unmute | deafen | undeafen | join_channel | leave_channel
        // channelId: required for join_channel
        // ------------------------------------------------------------------

        private static string ExecDiscord(string target, string channelId)
        {
            if (string.IsNullOrEmpty(target))
                return "discord failed: target is empty";

            if (!DiscordRpcClient.Instance.IsConnected)
                return "discord failed: not connected";

            switch (target.ToLower())
            {
                case "mute":
                    _ = DiscordRpcClient.Instance.SetMuteAsync(true);
                    return "discord: mute";

                case "unmute":
                    _ = DiscordRpcClient.Instance.SetMuteAsync(false);
                    return "discord: unmute";

                case "deafen":
                    _ = DiscordRpcClient.Instance.SetDeafAsync(true);
                    return "discord: deafen";

                case "undeafen":
                    _ = DiscordRpcClient.Instance.SetDeafAsync(false);
                    return "discord: undeafen";

                case "join_channel":
                    if (string.IsNullOrEmpty(channelId))
                        return "discord failed: channel_id is empty";
                    _ = DiscordRpcClient.Instance.SelectVoiceChannelAsync(channelId);
                    return $"discord: join_channel {channelId}";

                case "leave_channel":
                    _ = DiscordRpcClient.Instance.LeaveVoiceChannelAsync();
                    return "discord: leave_channel";

                default:
                    return $"discord failed: unknown target '{target}'";
            }
        }

        // ------------------------------------------------------------------
        // scroll action handler
        // target : up | down | left | right
        // amount : wheel delta units, default 120 (one notch)
        // ------------------------------------------------------------------
        private static string ExecScroll(string target, int? amount)
        {
            if (string.IsNullOrEmpty(target))
                return "scroll failed: target is empty";

            int delta = amount.HasValue ? amount.Value : 120;
            bool horizontal = target.ToLower() == "left" || target.ToLower() == "right";

            // Negative delta scrolls down / right
            if (target.ToLower() == "down" || target.ToLower() == "right")
                delta = -delta;

            const uint MOUSEEVENTF_WHEEL = 0x0800;
            const uint MOUSEEVENTF_HWHEEL = 0x1000;
            const int INPUT_MOUSE = 0;

            var inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.mouseData = delta;
            inputs[0].u.mi.dwFlags = horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            return $"scroll: {target} ({delta})";
        }

        // ------------------------------------------------------------------
        // sequence action handler
        // keys: list of hotkey combos sent sequentially with 50 ms gap
        // each entry uses the same format as hotkey action: ["ctrl+f", "ctrl+t"]
        // ------------------------------------------------------------------
        private static string ExecSequence(List<string> keySequence)
        {
            if (keySequence == null || keySequence.Count == 0)
                return "sequence failed: keys is empty";

            try
            {
                foreach (string combo in keySequence)
                {
                    var parts = new List<string>(combo.Split(','));
                    for (int i = 0; i < parts.Count; i++)
                        parts[i] = parts[i].Trim();

                    bool hasWin = parts.Exists(k => k.ToLower() == "win");
                    if (hasWin)
                        SendInputCombo(parts);
                    else
                        SendKeys.SendWait(BuildSendKeys(parts));

                    System.Threading.Thread.Sleep(50);
                }
                return $"sequence: {keySequence.Count} step(s)";
            }
            catch (Exception ex)
            {
                return $"sequence failed: {ex.Message}";
            }
        }

        // ------------------------------------------------------------------
        // Keyboard mode entry point
        // Called when page == 0x00
        // key_byte: bit7 = shift, bit6:0 = HID keycode
        // ------------------------------------------------------------------
        private static string ExecKeyboard(byte keyByte)
        {
            bool shift = (keyByte & 0x80) != 0;
            byte hidKey = (byte)(keyByte & 0x7F);

            // Functional keys — send via VK code
            if (HidToVkTable.TryGetValue(hidKey, out byte vk))
            {
                SendInputVk(vk);
                return $"keyboard: VK=0x{vk:X2}";
            }

            // Character keys — send via Unicode to bypass IME
            var charTable = shift ? HidToCharShifted : HidToCharUnshifted;
            if (charTable.TryGetValue(hidKey, out char ch))
            {
                SendInputUnicode(ch);
                return $"keyboard: char='{ch}'";
            }

            return $"keyboard: unknown HID 0x{hidKey:X2}";
        }

        // ------------------------------------------------------------------
        // SendInput combo (used when win key is present)
        // ------------------------------------------------------------------

        private static void SendInputCombo(List<string> keys)
        {
            var vks = new List<byte>();

            foreach (string k in keys)
            {
                string lower = k.ToLower();
                if (VkMap.TryGetValue(lower, out byte vk))
                    vks.Add(vk);
                else if (lower.Length == 1)
                    vks.Add((byte)(VkKeyScan(lower[0]) & 0xFF));
            }

            var inputs = new INPUT[vks.Count * 2];

            for (int i = 0; i < vks.Count; i++)
            {
                uint flags = ExtendedKeys.Contains(vks[i]) ? KEYEVENTF_EXTENDEDKEY : 0;
                inputs[i].type = INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = vks[i];
                inputs[i].u.ki.dwFlags = flags;
            }

            for (int i = 0; i < vks.Count; i++)
            {
                int idx = vks.Count + i;
                byte vk = vks[vks.Count - 1 - i];
                uint flags = KEYEVENTF_KEYUP | (ExtendedKeys.Contains(vk) ? KEYEVENTF_EXTENDEDKEY : 0);
                inputs[idx].type = INPUT_KEYBOARD;
                inputs[idx].u.ki.wVk = vk;
                inputs[idx].u.ki.dwFlags = flags;
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // ------------------------------------------------------------------
        // Send a single VK keypress (down + up)
        // Used for functional keys: Enter, Bksp, Tab, Esc, arrows, Space
        // ------------------------------------------------------------------
        private static void SendInputVk(byte vk)
        {
            uint flags = ExtendedKeys.Contains(vk) ? KEYEVENTF_EXTENDEDKEY : 0;

            var inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[0].u.ki.dwFlags = flags;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = vk;
            inputs[1].u.ki.dwFlags = flags | KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // ------------------------------------------------------------------
        // Send a Unicode character (down + up)
        // Bypasses IME completely — works regardless of input method state
        // ------------------------------------------------------------------
        private static void SendInputUnicode(char ch)
        {
            var inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = 0;
            inputs[0].u.ki.wScan = ch;
            inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = 0;
            inputs[1].u.ki.wScan = ch;
            inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // ------------------------------------------------------------------
        // SendKeys string builder (no win key)
        // ------------------------------------------------------------------

        private static string BuildSendKeys(List<string> keys)
        {
            string result = "";
            string tail = "";

            foreach (string k in keys)
            {
                string lower = k.ToLower();

                if (SendKeysModifiers.TryGetValue(lower, out string mod))
                {
                    result += mod + "(";
                    tail += ")";
                }
                else if (SendKeysSpecial.TryGetValue(lower, out string special))
                {
                    result += special;
                }
                else
                {
                    result += lower.Length == 1 ? lower : "{" + lower.ToUpper() + "}";
                }
            }

            return result + tail;
        }
        private static string ExecMouseClick(string target)
        {
            bool isDouble = string.Equals(target, "double", StringComparison.OrdinalIgnoreCase);
            SendMouseClick();
            if (isDouble)
            {
                System.Threading.Thread.Sleep(50);
                SendMouseClick();
            }
            return $"mouse_click: {(isDouble ? "double" : "single")}";
        }

        private static void SendMouseClick()
        {
            const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            const uint MOUSEEVENTF_LEFTUP = 0x0004;
            const int INPUT_MOUSE = 0;

            var inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
            inputs[1].type = INPUT_MOUSE;
            inputs[1].u.mi.dwFlags = MOUSEEVENTF_LEFTUP;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // ------------------------------------------------------------------
        // text action handler
        // Sends each character in target as a Unicode keystroke
        // ------------------------------------------------------------------
        private static string ExecText(string target)
        {
            if (target == null)
                return "text failed: target is null";

            foreach (char ch in target)
                SendInputUnicode(ch);

            return $"text: {target.Length} char(s)";
        }

    }
}