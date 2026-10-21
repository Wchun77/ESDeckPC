using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// Deck config file I/O -- one JSON per config (see Deckconfig.cs).
    /// Written as UTF-8 without BOM: the ESP reads the bytes as is, and
    /// Json.NET leaves non-ASCII text (Chinese paths/URLs) unescaped.
    /// </summary>
    public static class ConfigLoader
    {
        public static PcConfig Load(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<PcConfig>(json) ?? new PcConfig();
            if (cfg.Pages == null) cfg.Pages = new System.Collections.Generic.List<PcPage>();
            if (cfg.Settings == null) cfg.Settings = new PcSettings();
            return cfg;
        }

        public static void Save(PcConfig config, string path)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
    }
}
