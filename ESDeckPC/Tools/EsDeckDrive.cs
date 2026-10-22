using System;
using System.IO;

namespace ESDeckPC
{
    /// <summary>
    /// Finds the ESDeck SD card while the device is in Storage Mode, so the
    /// editors' and importers' file dialogs can start there instead of
    /// wherever Windows last left them. The card is recognised by having
    /// both config\ and assets\ at its root (the layout the firmware
    /// uses); only removable drives are checked.
    /// </summary>
    public static class EsDeckDrive
    {
        /// <summary>SD card root (e.g. "E:\"), or null if not plugged in / not in Storage Mode.</summary>
        public static string FindRoot()
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (d.DriveType != DriveType.Removable || !d.IsReady) continue;
                    string root = d.RootDirectory.FullName;
                    if (Directory.Exists(Path.Combine(root, "config")) &&
                        Directory.Exists(Path.Combine(root, "assets")))
                        return root;
                }
                catch
                {
                    // drive vanished / not accessible -- skip
                }
            }
            return null;
        }

        /// <summary>
        /// A folder on the SD card, e.g. Dir("config", "deck"). Falls back to
        /// the card root if that folder doesn't exist yet; null without a card.
        /// </summary>
        public static string Dir(params string[] parts)
        {
            string root = FindRoot();
            if (root == null) return null;
            string path = Path.Combine(root, Path.Combine(parts));
            return Directory.Exists(path) ? path : root;
        }
    }
}
