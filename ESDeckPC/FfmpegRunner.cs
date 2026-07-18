using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ESDeckPC
{
    /// <summary>
    /// Shared ffmpeg process wrapper. Resolves the bundled Tools/ffmpeg.exe
    /// next to the running exe (never relies on PATH/env vars, since this
    /// app is redistributed to other machines) and runs it with stderr
    /// progress parsing. Used by both the thumbnail-strip extraction and
    /// the final frame export in the boot animation converter, per
    /// "ESDeck開機動畫轉檔工具-規劃.md" section 4/5.
    /// </summary>
    public static class FfmpegRunner
    {
        private static readonly Regex FrameRegex = new Regex(@"frame=\s*(\d+)", RegexOptions.Compiled);

        public static string GetFfmpegPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg.exe");
        }

        public static bool IsAvailable()
        {
            return File.Exists(GetFfmpegPath());
        }

        /// <summary>
        /// Runs ffmpeg with the given arguments (each array entry is one
        /// logical argument; quoting is handled internally). Blocking --
        /// call from a background thread. totalFrames &lt;= 0 disables
        /// progress percentage reporting (onProgress simply won't fire).
        /// </summary>
        public static bool Run(string[] args, int totalFrames,
                                Action<string> onLog, Action<int> onProgress,
                                out string errorMessage)
        {
            errorMessage = null;

            if (!IsAvailable())
            {
                errorMessage = "ffmpeg executable not found. The deployment may be incomplete -- please reinstall.";
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = GetFfmpegPath(),
                Arguments = BuildArgString(args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var proc = new Process { StartInfo = psi })
            {
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) HandleLine(e.Data, totalFrames, onLog, onProgress); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) HandleLine(e.Data, totalFrames, onLog, onProgress); };

                try
                {
                    proc.Start();
                }
                catch (Exception ex)
                {
                    errorMessage = $"Failed to start ffmpeg: {ex.Message}";
                    return false;
                }

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    errorMessage = $"ffmpeg failed (exit code {proc.ExitCode}), see the log.";
                    return false;
                }
            }

            onProgress?.Invoke(100);
            return true;
        }

        private static void HandleLine(string line, int totalFrames, Action<string> onLog, Action<int> onProgress)
        {
            onLog?.Invoke(line);
            if (totalFrames <= 0) return;

            var m = FrameRegex.Match(line);
            if (!m.Success) return;

            if (int.TryParse(m.Groups[1].Value, out int cur))
            {
                int pct = Math.Min(99, (int)(cur * 100L / totalFrames));
                onProgress?.Invoke(pct);
            }
        }

        /// <summary>
        /// ProcessStartInfo on net48 takes a single command-line string, not
        /// an argument array, so each argument gets quoted individually
        /// when it contains spaces (paths in particular).
        /// </summary>
        private static string BuildArgString(string[] args)
        {
            var parts = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i] ?? "";
                parts[i] = a.IndexOf(' ') >= 0 ? $"\"{a}\"" : a;
            }
            return string.Join(" ", parts);
        }
    }
}
