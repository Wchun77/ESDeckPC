using System.IO;
using ESDeckPC.Properties;

namespace ESDeckPC
{
    /// <summary>
    /// Where the importers' Load / Browse dialogs start: the folder the last
    /// source file came from, kept in the user settings so it survives a
    /// restart. Needed because Windows' own "last folder" memory is shared
    /// with the Save dialogs, which point at the SD card -- after one save,
    /// the next Load would start on the card instead of the originals.
    /// One folder for images (Bg / Icon / Side Icon importers share it),
    /// one for videos (Boot Animation).
    /// </summary>
    internal static class SourceDirs
    {
        public static string Image => Read(() => Settings.Default.LastImageSourceDir);
        public static string Video => Read(() => Settings.Default.LastVideoSourceDir);

        // A broken settings file must never take a Load button down with it:
        // without a remembered folder Windows just picks its own.
        private static string Read(System.Func<string> get)
        {
            try { return Existing(get()); }
            catch { return ""; }
        }

        public static void RememberImage(string file) => Remember(file, false);
        public static void RememberVideo(string file) => Remember(file, true);

        private static void Remember(string file, bool video)
        {
            string dir = Path.GetDirectoryName(file);
            if (string.IsNullOrEmpty(dir)) return;
            try
            {
                if (video) Settings.Default.LastVideoSourceDir = dir;
                else Settings.Default.LastImageSourceDir = dir;
                Settings.Default.Save();
            }
            catch { /* settings file not writable: just don't remember */ }
        }

        private static string Existing(string dir) =>
            !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : "";
    }
}
