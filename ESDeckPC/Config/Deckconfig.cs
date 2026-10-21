using System.Collections.Generic;
using Newtonsoft.Json;

namespace ESDeckPC
{
    // ------------------------------------------------------------------
    // Deck config model -- one file (config/deck/<name>.json) holds both
    // what the ESP shows (name, bg_image, side_icon, label, icon) and what
    // a button does (action, target, keys, amount, channel_id). The ESP
    // loads it, and on a press sends that button (minus icon) to ESDeckPC
    // as JSON, which deserializes it into a PcButton and runs it.
    //
    // (Class names kept from the old pc_XXXX.json / esp_XXXX.json pair --
    // an old pc_ file is already this format, only without the display
    // fields.)
    // ------------------------------------------------------------------

    public class PcConfig
    {
        [JsonProperty("pages")]
        public List<PcPage> Pages { get; set; } = new List<PcPage>();

        // Settings page's own bg_image/side_icon
        [JsonProperty("settings")]
        public PcSettings Settings { get; set; } = new PcSettings();
    }

    public class PcSettings
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = gear glyph
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }

    public class PcPage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("buttons")]
        public List<PcButton> Buttons { get; set; } = new List<PcButton>();

        // filename only, under assets/backgrounds; empty = flat color
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = page name on the sidebar
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }

    public class PcButton
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        // launch / media
        [JsonProperty("target")]
        public string Target { get; set; }

        // hotkey
        [JsonProperty("keys")]
        public List<string> Keys { get; set; }

        // scroll: amount in WHEEL_DELTA units (default 120 = 1 notch)
        [JsonProperty("amount")]
        public int? Amount { get; set; }

        // discord: join_channel
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        // filename only, under assets/icons; empty = label text.
        // Display only -- the ESP strips it before sending the button.
        [JsonProperty("icon")]
        public string Icon { get; set; } = "";
    }
}
