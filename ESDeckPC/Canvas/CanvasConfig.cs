using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// Canvas mode files, all under the "Canvas" folder next to the exe:
    ///   configs/   page sets (*.json) -- which pages the ESP sidebar shows
    ///   pages/     page library, one folder per page (index.html + its files)
    ///   shared/    files any page can use (CSS, scripts, fonts)
    ///   active.txt file name of the config in use (in configs/)
    /// Paths inside a config are relative to the Canvas folder, so the whole
    /// folder can be copied to another PC as is.
    /// </summary>
    public static class CanvasPaths
    {
        public static string Root => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Canvas");
        public static string Configs => Path.Combine(Root, "configs");
        public static string Pages => Path.Combine(Root, "pages");
        public static string Shared => Path.Combine(Root, "shared");
        public static string ActiveFile => Path.Combine(Root, "active.txt");

        public static void Ensure()
        {
            Directory.CreateDirectory(Configs);
            Directory.CreateDirectory(Pages);
            Directory.CreateDirectory(Shared);
        }

        /// <summary>File name (in configs/) of the config in use, or null.</summary>
        public static string ActiveConfigName
        {
            get
            {
                try
                {
                    if (!File.Exists(ActiveFile)) return null;
                    string name = File.ReadAllText(ActiveFile).Trim();
                    return name.Length == 0 ? null : name;
                }
                catch { return null; }
            }
            set
            {
                Ensure();
                File.WriteAllText(ActiveFile, value ?? "");
                ActiveChanged?.Invoke();
            }
        }

        /// <summary>ActiveConfigName was set (from the editor, or from the ESP
        /// through CanvasLink). Raised on the thread that set it.</summary>
        public static event Action ActiveChanged;

        /// <summary>Absolute path for a path relative to the Canvas folder.</summary>
        public static string Resolve(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative)) return null;
            return Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>Path relative to the Canvas folder with '/' separators
        /// (as stored in configs), or the absolute path if it's outside.</summary>
        public static string MakeRelative(string absolute)
        {
            string root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(absolute);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/');
            return full;
        }

        /// <summary>Page folders in the library (those with an index.html),
        /// as paths relative to the Canvas folder, e.g. "pages/clock".</summary>
        public static List<string> ListPageDirs()
        {
            Ensure();
            return Directory.GetDirectories(Pages)
                .Where(d => File.Exists(Path.Combine(d, "index.html")))
                .Select(MakeRelative)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> ListConfigs()
        {
            Ensure();
            return Directory.GetFiles(Configs, "*.json")
                .Select(Path.GetFileName)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    // Only the [JsonProperty] members go into the file (computed paths stay out).
    [JsonObject(MemberSerialization.OptIn)]
    public class CanvasPageEntry
    {
        // Page folder, relative to the Canvas folder ("pages/clock"); the
        // page is its index.html.
        [JsonProperty("dir")]
        public string Dir { get; set; } = "";

        // Shown on the sidebar button when there's no icon.
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        // Sidebar icon, relative to the Canvas folder. Empty = the page
        // folder's icon.png if there is one, else the name as text.
        [JsonProperty("icon")]
        public string Icon { get; set; } = "";

        public string IndexPath => Path.Combine(CanvasPaths.Resolve(Dir) ?? "", "index.html");

        public string IconPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Icon)) return CanvasPaths.Resolve(Icon);
                string def = Path.Combine(CanvasPaths.Resolve(Dir) ?? "", "icon.png");
                return File.Exists(def) ? def : null;
            }
        }

        /// <summary>URL inside the WebView (the Canvas folder is mapped to
        /// https://canvas.esdeck/).</summary>
        public string Url => CanvasRenderer.HostUrl + Dir.Trim('/').Replace('\\', '/') + "/index.html";
    }

    /// <summary>The ESP's Settings page look while in Canvas mode. Both
    /// paths relative to the Canvas folder; empty = gear glyph / plain bg.
    /// Sent to the ESP as images -- nothing for Canvas lives on the SD card.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CanvasSettingsLook
    {
        [JsonProperty("bg_image")]
        public string BgImage { get; set; } = "";

        [JsonProperty("side_icon")]
        public string SideIcon { get; set; } = "";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CanvasConfig
    {
        public const int MaxPages = 4;   // = CANVAS_MAX_PAGES on the ESP

        [JsonProperty("pages")]
        public List<CanvasPageEntry> Pages { get; set; } = new List<CanvasPageEntry>();

        [JsonProperty("settings")]
        public CanvasSettingsLook Settings { get; set; } = new CanvasSettingsLook();

        public static CanvasConfig Load(string path)
        {
            var cfg = JsonConvert.DeserializeObject<CanvasConfig>(File.ReadAllText(path)) ?? new CanvasConfig();
            if (cfg.Pages == null) cfg.Pages = new List<CanvasPageEntry>();
            if (cfg.Settings == null) cfg.Settings = new CanvasSettingsLook();
            if (cfg.Pages.Count > MaxPages) cfg.Pages = cfg.Pages.Take(MaxPages).ToList();
            return cfg;
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>The config named in active.txt, or null (none chosen /
        /// missing / unreadable -- error says which).</summary>
        public static CanvasConfig LoadActive(out string name, out string error)
        {
            error = null;
            name = CanvasPaths.ActiveConfigName;
            if (name == null) { error = "no config selected (Canvas > Edit Pages)"; return null; }
            string path = Path.Combine(CanvasPaths.Configs, name);
            if (!File.Exists(path)) { error = $"config not found: {name}"; return null; }
            try { return Load(path); }
            catch (Exception ex) { error = $"config {name}: {ex.Message}"; return null; }
        }
    }
}
