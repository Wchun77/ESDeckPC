using Newtonsoft.Json;

namespace ESDeckPC
{
    // Mirrors ui_touchpad_config.h's ui_touchpad_config_t -- same schema as
    // Media's (config/touchpad/*.json):
    //
    //   {
    //       "bg_image": "desk.jpg",
    //       "settings": {
    //           "bg_image": "panel_bg.jpg",
    //           "side_icon": "mouse.png"
    //       }
    //   }
    //
    // Own model rather than reusing MediaConfig, same "each mode owns its own
    // copy" convention as the other configs. The sidebar keyboard button
    // icon is fixed on the device for now, so it's not in the schema.
    public class TouchpadConfig
    {
        // Touchpad content background, empty = flat color (0x222222).
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

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
