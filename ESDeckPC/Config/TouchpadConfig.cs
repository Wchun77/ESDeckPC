using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ESDeckPC
{
    // Mirrors ui_touchpad_config.h's ui_touchpad_config_t (config/touchpad/*.json):
    //
    //   {
    //       "bg_image": "desk.jpg",
    //       "keyboard_icon": "kb.png",
    //       "hotkeys": [
    //           { "label": "Copy", "icon": "copy.png", "keys": ["ctrl", "c"] }
    //       ],
    //       "settings": {
    //           "bg_image": "panel_bg.jpg",
    //           "side_icon": "mouse.png"
    //       }
    //   }
    //
    // Own model rather than reusing MediaConfig, same "each mode owns its own
    // copy" convention as the other configs.
    //
    // Whether the sidebar shows these hotkeys, the scroll wheel or nothing is
    // a device setting (Settings > Custom > Touchpad > Sidebar, NVS), not part
    // of the config. An old "sidebar_mode" field is ignored on load (Json.NET
    // skips unknown members) and dropped on the next save.
    public class TouchpadConfig
    {
        /// <summary>Must match TP_HOTKEY_MAX in ui_touchpad_config.h.</summary>
        public const int MaxHotkeys = 4;

        // Touchpad content background, empty = flat color (0x222222).
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // Sidebar keyboard button image, filename only under assets/side_icons;
        // empty = keyboard glyph.
        [JsonProperty("keyboard_icon")]
        public string KeyboardIcon { get; set; } = "";

        // Shown when the device's Sidebar setting is Hotkeys; at most
        // MaxHotkeys, packed (the device lays them out top-down).
        [JsonProperty("hotkeys")]
        public List<TouchpadHotkey> Hotkeys { get; set; } = new List<TouchpadHotkey>();

        [JsonProperty("settings")]
        public TouchpadSettingsCfg Settings { get; set; } = new TouchpadSettingsCfg();
    }

    public class TouchpadSettingsCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = show gear glyph on sidebar button
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }

    /// <summary>
    /// One sidebar hotkey. Executed by the ESP itself (sent as a keyboard
    /// report), not through Deck's page/button actions -- only the "keys"
    /// format is the same as a Deck hotkey.
    /// </summary>
    public class TouchpadHotkey
    {
        [JsonProperty("label")]
        public string Label { get; set; } = "";

        // filename only, under assets/side_icons; empty = label text on the button
        [JsonProperty("icon")]
        public string Icon { get; set; } = "";

        // e.g. ["ctrl", "c"]: modifiers + exactly one key
        [JsonProperty("keys")]
        public List<string> Keys { get; set; } = new List<string>();
    }

    /// <summary>
    /// Key names the device accepts for a touchpad hotkey. Keep in sync with
    /// s_key_names / char_to_vk() in ESDeck's ui_touchpad_config.c.
    /// </summary>
    public static class TouchpadHotkeyKeys
    {
        private static readonly HashSet<string> Modifiers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ctrl", "shift", "alt", "win" };

        private static readonly HashSet<string> Named =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "enter", "tab", "esc", "del", "backspace", "space",
                "up", "down", "left", "right", "home", "end", "pgup", "pgdn",
                "insert", "prtscr",
                "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12",
            };

        private const string Punct = @"-=[]\;',./`";   // verbatim: contains a backslash

        /// <summary>"ctrl, c" / "ctrl+c" -> ["ctrl", "c"].</summary>
        public static List<string> Split(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.Split(new[] { ',', '+', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .Where(s => s.Length > 0)
                       .ToList();
        }

        private static bool IsMainKey(string k)
        {
            if (Named.Contains(k)) return true;
            if (k.Length != 1) return false;
            char c = k[0];
            return char.IsLetterOrDigit(c) && c < 128 || Punct.IndexOf(c) >= 0;
        }

        public static bool IsValid(List<string> keys, out string error)
        {
            error = null;
            int main = 0;
            foreach (var k in keys)
            {
                if (Modifiers.Contains(k)) continue;
                if (!IsMainKey(k)) { error = $"unknown key \"{k}\""; return false; }
                main++;
            }
            if (main == 0) { error = "needs one non-modifier key (e.g. ctrl, c)"; return false; }
            if (main > 1) { error = "only one non-modifier key allowed"; return false; }
            return true;
        }
    }
}
