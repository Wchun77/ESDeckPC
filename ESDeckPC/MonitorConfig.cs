using Newtonsoft.Json;

namespace ESDeckPC
{
    public class MonitorConfig
    {
        [JsonProperty("clock")]
        public MonitorClockCfg Clock { get; set; } = new MonitorClockCfg();

        [JsonProperty("system")]
        public MonitorSystemCfg System { get; set; } = new MonitorSystemCfg();
    }

    public class MonitorClockCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        [JsonProperty("font_time")]
        public string FontTime { get; set; } = "";

        [JsonProperty("font_sec")]
        public string FontSec { get; set; } = "";

        [JsonProperty("font_date")]
        public string FontDate { get; set; } = "";

        // Stored as hex string without '#', e.g. "F0F2FF"
        [JsonProperty("col_time")]
        public string ColTime { get; set; } = "F0F2FF";

        [JsonProperty("col_colon")]
        public string ColColon { get; set; } = "F0F2FF";

        [JsonProperty("col_date")]
        public string ColDate { get; set; } = "F0F2FF";

        [JsonProperty("col_day")]
        public string ColDay { get; set; } = "F0F2FF";

        [JsonProperty("col_sec")]
        public string ColSec { get; set; } = "F0F2FF";

        [JsonProperty("sep_color")]
        public string SepColor { get; set; } = "F0F2FF";

        [JsonProperty("sep_width")]
        public int SepWidth { get; set; } = 1;
    }

    public class MonitorSystemCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";
    }
}
