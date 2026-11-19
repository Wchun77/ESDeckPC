using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

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

        // ------------------------------------------------------------------
        // Eject: the usual Windows sequence for removable media -- flush,
        // lock, dismount, allow removal, eject. The last step sends SCSI
        // START STOP UNIT (eject) to the device, which the firmware takes
        // as "PC is done" and restarts into HID mode.
        // ------------------------------------------------------------------

        private const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;
        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;
        private const uint IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share,
            IntPtr security, uint creation, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle dev, uint code,
            byte[] inBuf, int inSize, IntPtr outBuf, int outSize, out int returned, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(SafeFileHandle file);

        /// <summary>
        /// Ejects the drive at root (e.g. "E:\"). Returns null on success,
        /// otherwise why it failed (most often: a file on the card is still
        /// open in some program, so the volume can't be locked).
        /// </summary>
        public static string Eject(string root)
        {
            string letter = root.Substring(0, 2);   // "E:"
            using (var h = CreateFile(@"\\.\" + letter, GENERIC_READ | GENERIC_WRITE,
                       FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                if (h.IsInvalid) return $"cannot open {letter} (error {Marshal.GetLastWin32Error()})";

                FlushFileBuffers(h);   // best effort; the dismount below flushes too

                bool locked = false;
                for (int i = 0; i < 10 && !locked; i++)
                {
                    locked = DeviceIoControl(h, FSCTL_LOCK_VOLUME, null, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
                    if (!locked) Thread.Sleep(300);
                }
                if (!locked) return $"{letter} is in use -- close any file opened from it and try again";

                if (!DeviceIoControl(h, FSCTL_DISMOUNT_VOLUME, null, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                    return $"cannot dismount {letter} (error {Marshal.GetLastWin32Error()})";

                var allow = new byte[] { 0 };   // PREVENT_MEDIA_REMOVAL { PreventMediaRemoval = FALSE }
                DeviceIoControl(h, IOCTL_STORAGE_MEDIA_REMOVAL, allow, 1, IntPtr.Zero, 0, out _, IntPtr.Zero);

                if (!DeviceIoControl(h, IOCTL_STORAGE_EJECT_MEDIA, null, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                    return $"cannot eject {letter} (error {Marshal.GetLastWin32Error()})";
            }
            return null;
        }
    }
}
