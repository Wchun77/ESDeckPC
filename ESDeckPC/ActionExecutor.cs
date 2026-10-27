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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

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
        // Public entry point
        // ------------------------------------------------------------------

        /// <summary>
        /// Executes one Deck button action. The button comes from the ESP
        /// (HidReceiver.OnActionReceived -> JSON -> DeckButton): the device
        /// carries the deck config and sends the pressed button's action,
        /// so there's no PC-side config lookup any more.
        /// </summary>
        public static string Run(DeckButton button)
        {
            if (button == null)
                return "no action";

            switch (button.Action?.ToLower())
            {
                case "launch": return ExecLaunch(button.Target);
                case "hotkey": return ExecHotkey(button.Keys);
                case "media": return ExecMedia(button.Target);
                case "discord": return ExecDiscord(button.Target, button.ChannelId);
                case "scroll": return ExecScroll(button.Target, button.Amount);
                case "sequence": return ExecSequence(button.Keys);
                case "text": return ExecText(button.Target);
                case "paste": return ExecPaste(button.Target);
                case "mouse_click": return ExecMouseClick(button.Target);
                case "power": return ExecPower(button.Target);
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
        // power action handler
        // target: shutdown | restart | logoff | lock | sleep
        // ------------------------------------------------------------------
        private static string ExecPower(string target)
        {
            if (string.IsNullOrEmpty(target))
                return "power failed: target is empty";

            try
            {
                switch (target.ToLower())
                {
                    case "shutdown":
                        Process.Start(new ProcessStartInfo("shutdown", "/s /t 0") { UseShellExecute = true, CreateNoWindow = true });
                        return "power: shutdown";

                    case "restart":
                        Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { UseShellExecute = true, CreateNoWindow = true });
                        return "power: restart";

                    case "logoff":
                        Process.Start(new ProcessStartInfo("shutdown", "/l") { UseShellExecute = true, CreateNoWindow = true });
                        return "power: logoff";

                    case "lock":
                        LockWorkStation();
                        return "power: lock";

                    case "sleep":
                        Application.SetSuspendState(PowerState.Suspend, false, false);
                        return "power: sleep";

                    default:
                        return $"power failed: unknown target '{target}'";
                }
            }
            catch (Exception ex)
            {
                return $"power failed: {ex.Message}";
            }
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

        // ------------------------------------------------------------------
        // paste: put the text on the clipboard, send Ctrl+V, then put the
        // user's clipboard back. Unlike "text" (one SendInput per char) the
        // IME never sees it, so CJK text and full-width punctuation can't
        // get reordered. Apps where Ctrl+V isn't paste (some terminals)
        // should keep using "text".
        // ------------------------------------------------------------------

        // Clipboard history (Win+V) and cloud sync skip data carrying this format.
        private const string ExcludeFromHistory = "ExcludeClipboardContentFromMonitorProcessing";
        // Time for the target app to read the clipboard after Ctrl+V
        // before the old content goes back.
        private const int PasteRestoreDelayMs = 500;
        private static readonly object PasteLock = new object();

        private static string ExecPaste(string target)
        {
            if (string.IsNullOrEmpty(target))
                return "paste failed: target is empty";

            // Clipboard needs an STA thread; callers (HID receiver, Canvas
            // bridge) may not be one, and the restore delay must not block them.
            string result = null;
            var sent = new System.Threading.ManualResetEvent(false);
            var worker = new System.Threading.Thread(() =>
            {
                lock (PasteLock)   // back-to-back pastes: never back up our own text
                {
                    DataObject backup = null;
                    try
                    {
                        backup = BackupClipboard();
                        var data = new DataObject();
                        data.SetText(target, TextDataFormat.UnicodeText);
                        data.SetData(ExcludeFromHistory, new System.IO.MemoryStream(new byte[] { 0 }));
                        Clipboard.SetDataObject(data, true, 5, 50);
                        SendCtrlV();
                        result = $"paste: {target.Length} char(s)";
                    }
                    catch (Exception ex)
                    {
                        result = $"paste failed: {ex.Message}";
                    }
                    finally
                    {
                        sent.Set();
                    }
                    if (result.StartsWith("paste failed")) return;

                    System.Threading.Thread.Sleep(PasteRestoreDelayMs);
                    try
                    {
                        // Only if it's still ours -- the user may have copied something meanwhile.
                        if (Clipboard.ContainsText() && Clipboard.GetText() == target)
                        {
                            if (backup != null) Clipboard.SetDataObject(backup, true, 5, 50);
                            else Clipboard.Clear();
                        }
                    }
                    catch { /* clipboard busy: leave the pasted text there */ }
                }
            });
            worker.SetApartmentState(System.Threading.ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();

            return sent.WaitOne(3000) ? result : "paste failed: timed out";
        }

        /// <summary>Copy of every format currently on the clipboard that can
        /// be read back (text, images, files, HTML, RTF...); null if empty.
        /// Delay-rendered or app-private formats may not survive.</summary>
        private static DataObject BackupClipboard()
        {
            IDataObject cur = Clipboard.GetDataObject();
            if (cur == null) return null;

            var copy = new DataObject();
            bool any = false;
            foreach (string fmt in cur.GetFormats(false))
            {
                try
                {
                    object v = cur.GetData(fmt, false);
                    if (v == null) continue;
                    copy.SetData(fmt, false, v);
                    any = true;
                }
                catch { /* unreadable format: skip */ }
            }
            if (!any) return null;
            copy.SetData(ExcludeFromHistory, new System.IO.MemoryStream(new byte[] { 0 }));
            return copy;
        }

        private static void SendCtrlV()
        {
            const ushort VK_CONTROL = 0x11, VK_V = 0x56;
            var inputs = new INPUT[4];
            ushort[] vks = { VK_CONTROL, VK_V, VK_V, VK_CONTROL };
            for (int i = 0; i < 4; i++)
            {
                inputs[i].type = INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = vks[i];
                inputs[i].u.ki.dwFlags = i < 2 ? 0 : KEYEVENTF_KEYUP;
            }
            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

    }
}