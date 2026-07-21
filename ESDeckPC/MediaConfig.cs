using Newtonsoft.Json;

namespace ESDeckPC
{
    // Mirrors ui_media_config.h's ui_media_config_t / the JSON schema
    // documented there:
    //
    //   {
    //       "bg_image": "sunset.jpg",
    //       "settings": {
    //           "bg_image": "panel_bg.jpg",
    //           "side_icon": "music.png"
    //       }
    //   }
    //
    // Kept as its own model rather than reusing MonitorConfig's
    // MonitorSettingsCfg for the "settings" object -- same "each mode owns
    // its own copy" convention Deckconfig.cs's EspSettings already follows
    // even though the shape is identical to Monitor's.
    public class MediaConfig
    {
        // Media player card's own background, empty = flat color (0x222222).
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        [JsonProperty("settings")]
        public MediaSettingsCfg Settings { get; set; } = new MediaSettingsCfg();
    }

    public class MediaSettingsCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = show gear glyph on sidebar button
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }
}
