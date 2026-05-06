using System.Collections.Generic;
using Newtonsoft.Json;

namespace ESDeckPC
{
    // ------------------------------------------------------------------
    // PC JSON model
    // ------------------------------------------------------------------

    public class PcConfig
    {
        [JsonProperty("pages")]
        public List<PcPage> Pages { get; set; } = new List<PcPage>();
    }

    public class PcPage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("buttons")]
        public List<PcButton> Buttons { get; set; } = new List<PcButton>();

        // from esp JSON, not written to pc JSON
        [JsonIgnore]
        public string BgImage { get; set; }
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

        // from esp JSON, not written to pc JSON
        [JsonIgnore]
        public string Icon { get; set; }
    }

    // ------------------------------------------------------------------
    // ESP JSON model
    // ------------------------------------------------------------------

    public class EspConfig
    {
        [JsonProperty("pages")]
        public List<EspPage> Pages { get; set; } = new List<EspPage>();
    }

    public class EspPage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("bg_image")]
        public string BgImage { get; set; }

        [JsonProperty("buttons")]
        public List<EspButton> Buttons { get; set; } = new List<EspButton>();
    }

    public class EspButton
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}