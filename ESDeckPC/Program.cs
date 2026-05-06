using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace ESDeckPC
{
    internal static class Program
    {
        private static string LockFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "app_lock");
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);
        private static FileStream _lockStream;
        private static string[] parts;
        private static System.Timers.Timer _updateTimer;
        private static int opening_time = 0;

        [STAThread]
        static void Main()
        {
            try
            {
                var (isLocked, message) = CheckLockStatus();
                if (isLocked)
                {
                    MessageBox.Show(message, "Application Already Running",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CreateLockFile();
                StartUpdateTimer();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormM());
            }
            finally
            {
                StopUpdateTimer();
                ReleaseLockFile();
            }
        }

        // ------------------------------------------------------------------
        // Lock status check
        // ------------------------------------------------------------------

        private static (bool isLocked, string message) CheckLockStatus()
        {
            try
            {
                if (!File.Exists(LockFile)) return (false, null);

                try
                {
                    using (var fs = new FileStream(LockFile, FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite))
                    {
                        var content = new StreamReader(fs).ReadToEnd();
                        var p = content.Split(new[] { "||" }, StringSplitOptions.None);

                        if (p.Length < 4) return (true, "Abnormal lock file detected");

                        var timestamp = DateTime.Parse(p[0]);
                        if (DateTime.Now - timestamp > Timeout)
                            return (false, null);

                        return (true,
                            $"ESDeck PC is already running.\n" +
                            $"User: {p[1]}\nTime: {p[0]}\nOpen duration: {p[3]} minutes");
                    }
                }
                catch (IOException)
                {
                    return (true, "The application is already running.");
                }
            }
            catch
            {
                return (false, null);
            }
        }

        // ------------------------------------------------------------------
        // Lock file management
        // ------------------------------------------------------------------

        private static void CreateLockFile()
        {
            try
            {
                ReleaseLockFile();

                _lockStream = new FileStream(LockFile, FileMode.Create,
                    FileAccess.ReadWrite, FileShare.ReadWrite);

                parts = new string[]
                {
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"{Environment.UserName}@{Environment.MachineName}",
                    $"PID:{Process.GetCurrentProcess().Id}",
                    $"{opening_time}"
                };

                string content = string.Join("||", parts);
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                _lockStream.Write(bytes, 0, bytes.Length);
                _lockStream.Flush();

                File.SetAttributes(LockFile, FileAttributes.Hidden);
            }
            catch
            {
                // lock file creation failure is non-fatal
            }
        }

        private static void ReleaseLockFile()
        {
            try
            {
                if (_lockStream != null)
                {
                    _lockStream.Close();
                    _lockStream.Dispose();
                    _lockStream = null;
                }

                if (File.Exists(LockFile))
                    File.Delete(LockFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Release lock failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Timestamp update timer
        // ------------------------------------------------------------------

        private static void StartUpdateTimer()
        {
            _updateTimer = new System.Timers.Timer(1 * 60 * 1000);
            _updateTimer.Elapsed += UpdateLockFileTimestamp;
            _updateTimer.AutoReset = true;
            _updateTimer.Enabled = true;
        }

        private static void StopUpdateTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        private static void UpdateLockFileTimestamp(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (parts != null && parts.Length >= 4)
                {
                    parts[0] = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    if (int.TryParse(parts[3], out int current))
                        opening_time = current;

                    opening_time++;
                    parts[3] = opening_time.ToString();

                    string updated = string.Join("||", parts);

                    using (var fs = new FileStream(LockFile, FileMode.Open,
                        FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(updated);
                        writer.Flush();
                        fs.SetLength(updated.Length);
                    }
                }
                else
                {
                    StopUpdateTimer();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update lock failed: {ex.Message}");
                StopUpdateTimer();
            }
        }
    }
}