using System.Collections.Generic;
using Newtonsoft.Json;

namespace ESDeckPC
{
    public class MonitorConfig
    {
        // Settings page's own bg_image/side_icon -- same "fixed entry, own
        // top-level JSON object" convention as Clock, so the PC/ESP config
        // stays a single 1:1 file (unlike Deck's split pc/esp json pair).
        [JsonProperty("settings")]
        public MonitorSettingsCfg Settings { get; set; } = new MonitorSettingsCfg();

        [JsonProperty("clock")]
        public MonitorClockCfg Clock { get; set; } = new MonitorClockCfg();

        [JsonProperty("pages")]
        public List<MonitorPageCfg> Pages { get; set; } = new List<MonitorPageCfg>();
    }

    public class MonitorSettingsCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = show gear glyph on sidebar button
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }

    public class MonitorClockCfg
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = show "Clock" text on sidebar button
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";

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

        // 0-255, default 255 (matches firmware ui_monitor_config.c defaults)
        [JsonProperty("opa_time")]
        public byte OpaTime { get; set; } = 255;

        [JsonProperty("opa_colon")]
        public byte OpaColon { get; set; } = 255;

        [JsonProperty("opa_date")]
        public byte OpaDate { get; set; } = 255;

        [JsonProperty("opa_day")]
        public byte OpaDay { get; set; } = 255;

        [JsonProperty("opa_sec")]
        public byte OpaSec { get; set; } = 255;

        // Pixel gap between time digits and colon, default 30
        [JsonProperty("colon_gap")]
        public int ColonGap { get; set; } = 30;
    }

    public class MonitorPageCfg
    {
        public const int CellCount = 4;

        [JsonProperty("name")]
        public string Name { get; set; } = "Page";

        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        // filename only, under assets/side_icons; empty = show page name text on sidebar button
        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";

        // null entry = empty slot
        [JsonProperty("cells")]
        public string[] Cells { get; set; } = new string[CellCount];
    }

    public static class MonitorPageLimits
    {
        public const int MaxPages = 3;
    }
}