using Newtonsoft.Json;

namespace ESDeckPC
{
    // Mirrors ui_touchpad_config.h's ui_touchpad_config_t -- same schema as
    // Media's (config/touchpad/*.json):
    //
    //   {
    //       "bg_image": "desk.jpg",
    //       "keyboard_icon": "kb.png",
    //       "settings": {
    //           "bg_image": "panel_bg.jpg",
    //           "side_icon": "mouse.png"
    //       }
    //   }
    //
    // Own model rather than reusing MediaConfig, same "each mode owns its own
    // copy" convention as the other configs.
    public class TouchpadConfig
    {
        // Touchpad content background, empty = flat color (0x222222).
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // Sidebar keyboard button image, filename only under assets/side_icons;
        // empty = keyboard glyph.
        [JsonProperty("keyboard_icon")]
        public string KeyboardIcon { get; set; } = "";

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
}
