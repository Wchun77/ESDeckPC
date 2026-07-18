using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace ESDeckPC
{
    /// <summary>
    /// Boot animation converter: pick a video, trim a range with a visual
    /// dual-thumb slider + thumbnail strip, then export a JPEG frame
    /// sequence (frame_%04d.jpg) into a subfolder under the SD card's
    /// assets/boot/ -- matching firmware's multi-animation picker, which
    /// scans assets/boot/&lt;name&gt;/ subfolders (see boot_anim.c
    /// get_selected_anim / SD_DIR_ASSETS_BOOT).
    ///
    /// Ports the parameter ranges and ffmpeg -vf filter strings directly
    /// from the standalone tools/boot_anim_converter.py (fps 1-30 default
    /// 12, quality 2-20 default 5, crop/pad/stretch aspect modes at
    /// 800x480), but replaces the old "duration from start" spinner with
    /// a real in/out range selected on the timeline, and replaces the
    /// PATH-based ffmpeg lookup with the bundled Tools/ffmpeg.exe (see
    /// FfmpegRunner) since this app ships to other people's machines.
    ///
    /// UI construction lives in FormBootAnimConverter.Designer.cs, per
    /// this project's Form/Form.Designer.cs split convention -- this file
    /// only has the constructor and business logic.
    ///
    /// NOTE: this form depends on LibVLCSharp.WinForms + LibVLCSharp +
    /// VideoLAN.LibVLC.Windows (NuGet). Those packages are not wired into
    /// this project's packages.config/csproj yet -- add them via Visual
    /// Studio's NuGet package manager before building.
    /// </summary>
    public partial class FormBootAnimConverter : Form
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        private const int ScreenW = 800;
        private const int ScreenH = 480;
        private const string FrameFilePattern = "frame_%04d.jpg";
        private static readonly Regex FrameFileRegex = new Regex(@"^frame_\d{4}\.jpg$", RegexOptions.IgnoreCase);

        // Order matches the original Python tool's ASPECT_MODES (crop/pad/stretch).
        private static readonly (string Label, string Mode)[] AspectModes = new[]
        {
            ("Crop to Fill", "crop"),
            ("Pad (Letterbox)", "pad"),
            ("Stretch to Fill", "stretch"),
        };

        // ------------------------------------------------------------------
        // DWM dark title bar (matches the other importer forms)
        // ------------------------------------------------------------------

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
        }

        // Space toggles play/pause no matter which control has focus --
        // none of this form's controls need a literal typed space
        // character (video/output paths are read-only, the combo is a
        // DropDownList). Handling it here, before it reaches a focused
        // control, also avoids double-firing when _btnPlayPause itself has
        // focus (a focused Button's own default Space-click behavior would
        // otherwise fire too).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space)
            {
                BtnPlayPause_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.S)
            {
                BtnSetEndHere_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _media;

        private double _videoDurationSec = 0;
        private string _thumbTempDir;
        private System.Windows.Forms.Timer _previewWatchTimer;
        private bool _isConverting = false;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormBootAnimConverter()
        {
            InitializeComponent();

            AppendLog("Keyboard shortcuts:");
            AppendLog("  A / D        - select left / right trim thumb");
            AppendLog("  Left / Right - nudge selected thumb 1s (Shift = 5s)");
            AppendLog("  Z / X        - nudge left thumb -0.1s / +0.1s");
            AppendLog("  C / V        - nudge right thumb -0.1s / +0.1s");
            AppendLog("  Space        - play / pause preview");
            AppendLog("  S            - move right thumb to current playback position");
            AppendLog("");

            RefreshEstimate();

            if (!FfmpegRunner.IsAvailable())
            {
                AppendLog("ffmpeg executable not found (Tools/ffmpeg.exe) -- the deployment may be incomplete.");
                _btnConvert.Enabled = false;
            }

            try
            {
                Core.Initialize();
                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC) { Mute = true }; // this is a trim tool, preview audio is never needed
                _videoView.MediaPlayer = _mediaPlayer;
            }
            catch (Exception ex)
            {
                AppendLog("LibVLC initialization failed, preview playback unavailable: " + ex.Message);
            }

            _previewWatchTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _previewWatchTimer.Tick += PreviewWatchTimer_Tick;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            _previewWatchTimer?.Stop();
            _previewWatchTimer?.Dispose();

            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _media?.Dispose();
                _libVLC?.Dispose();
            }
            catch { /* best effort cleanup */ }

            CleanupThumbTempDir();
        }

        // ------------------------------------------------------------------
        // Video load
        // ------------------------------------------------------------------

        private void BtnBrowseVideo_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Video File";
                dlg.Filter = "Video files (*.mp4;*.mov;*.avi;*.mkv;*.webm)|*.mp4;*.mov;*.avi;*.mkv;*.webm|All files (*.*)|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _ = LoadVideoAsync(dlg.FileName);
            }
        }

        private async Task LoadVideoAsync(string path)
        {
            if (_libVLC == null)
            {
                AppendLog("LibVLC not initialized, cannot load video.");
                return;
            }

            _txtVideoPath.Text = path;
            AppendLog("Loading video: " + path);

            try
            {
                _media?.Dispose();
                _media = new Media(_libVLC, path, FromType.FromPath);

                await _media.Parse(MediaParseOptions.ParseLocal);

                long durationMs = _media.Duration;
                if (durationMs <= 0)
                {
                    AppendLog("Could not read video duration -- please check the file is valid.");
                    return;
                }

                _videoDurationSec = durationMs / 1000.0;
                _slider.TotalDurationSec = _videoDurationSec;
                _slider.SetRange(0, Math.Min(_videoDurationSec, 3.0));
                _slider.CurrentSec = _slider.StartSec;
                UpdateRangeLabel();
                RefreshEstimate();

                _mediaPlayer.Media = _media;

                // Play then immediately pause once playback actually starts,
                // so the view shows frame 0 (rather than a black VideoView)
                // and MediaPlayer.Time-based seeking works right away.
                EventHandler<EventArgs> onPlaying = null;
                onPlaying = (s, e) =>
                {
                    _mediaPlayer.Playing -= onPlaying;
                    BeginInvoke(new Action(() => _mediaPlayer.Pause()));
                };
                _mediaPlayer.Playing += onPlaying;
                _mediaPlayer.Play();

                StartThumbnailExtraction(path, _videoDurationSec);
            }
            catch (Exception ex)
            {
                AppendLog("Failed to load video: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Thumbnail strip (background ffmpeg extraction, full video length)
        // ------------------------------------------------------------------

        private void StartThumbnailExtraction(string videoPath, double durationSec)
        {
            CleanupThumbTempDir();
            _thumbTempDir = Path.Combine(Path.GetTempPath(), "esdeck_boot_thumbs_" + Guid.NewGuid().ToString("N"));

            const int thumbW = 160, thumbH = 90;
            const int targetCount = 30;
            double interval = Math.Max(0.3, durationSec / targetCount);
            int expectedCount = Math.Max(1, (int)Math.Ceiling(durationSec / interval));

            Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(_thumbTempDir);

                    string vf = $"fps=1/{interval.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                $"scale={thumbW}:{thumbH}:force_original_aspect_ratio=increase,crop={thumbW}:{thumbH}";
                    string outPattern = Path.Combine(_thumbTempDir, "thumb_%04d.jpg");

                    var args = new[]
                    {
                        "-y", "-i", videoPath,
                        "-vf", vf,
                        "-q:v", "5",
                        "-start_number", "0",
                        outPattern,
                    };

                    FfmpegRunner.Run(args, expectedCount, line => { }, pct => { }, out string err);
                    if (err != null)
                    {
                        BeginInvoke(new Action(() => AppendLog("Thumbnail extraction failed: " + err)));
                        return;
                    }

                    var files = Directory.GetFiles(_thumbTempDir, "thumb_*.jpg").OrderBy(f => f).ToList();
                    var bitmaps = new List<Bitmap>();
                    foreach (var f in files)
                    {
                        try
                        {
                            using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                                bitmaps.Add(new Bitmap(Image.FromStream(fs)));
                        }
                        catch { /* skip unreadable thumb */ }
                    }

                    if (bitmaps.Count > 0 && !IsDisposed)
                    {
                        BeginInvoke(new Action(() => _slider.SetThumbnails(bitmaps)));
                    }
                }
                catch (Exception ex)
                {
                    if (!IsDisposed)
                        BeginInvoke(new Action(() => AppendLog("Thumbnail extraction exception: " + ex.Message)));
                }
            });
        }

        private void CleanupThumbTempDir()
        {
            if (string.IsNullOrEmpty(_thumbTempDir)) return;
            try { if (Directory.Exists(_thumbTempDir)) Directory.Delete(_thumbTempDir, true); }
            catch { /* best effort */ }
            _thumbTempDir = null;
        }

        // ------------------------------------------------------------------
        // Range slider / preview interaction
        // ------------------------------------------------------------------

        private void Slider_RangeChanged(object sender, EventArgs e)
        {
            UpdateRangeLabel();
            RefreshEstimate();

            if (_mediaPlayer == null) return;

            // Only touch playback if the trim range moved out from under
            // where the playhead already is -- otherwise leave it exactly
            // where it was. Nudging one thumb shouldn't yank the preview
            // over to it every time; it should only move if the playhead
            // is no longer inside [Start, End] at all, in which case it
            // snaps to whichever edge it fell past.
            double curSec = _slider.CurrentSec;
            if (curSec < 0) return; // nothing has played/positioned yet

            double clamped = Math.Max(_slider.StartSec, Math.Min(_slider.EndSec, curSec));
            if (Math.Abs(clamped - curSec) < 0.001) return; // still inside the range, leave it alone

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _previewWatchTimer.Stop();
                _btnPlayPause.Text = "Play";
            }

            if (_mediaPlayer.IsSeekable)
                _mediaPlayer.Time = (long)(clamped * 1000);
            _slider.CurrentSec = clamped;
        }

        /// <summary>
        /// Live scrub -- fired continuously while dragging the playhead
        /// (or the middle of the timeline) on the slider, independent of
        /// the trim range.
        /// </summary>
        private void Slider_SeekRequested(object sender, double sec)
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _previewWatchTimer.Stop();
                _btnPlayPause.Text = "Play";
            }

            if (_mediaPlayer.IsSeekable)
                _mediaPlayer.Time = (long)(sec * 1000);
        }

        private void UpdateRangeLabel()
        {
            _lblRange.Text = $"{FormatTime(_slider.StartSec)} ~ {FormatTime(_slider.EndSec)}  ({(_slider.EndSec - _slider.StartSec):0.0}s)";
        }

        private static string FormatTime(double sec)
        {
            int m = (int)(sec / 60);
            double s = sec - m * 60;
            return $"{m:00}:{s:00.0}";
        }

        /// <summary>
        /// Toggles between play and pause. Play always plays just the
        /// selected range -- if the current position is outside [Start,
        /// End) (or a previous preview already ran to the end), it first
        /// rewinds to Start; otherwise it resumes from wherever Pause left
        /// off.
        /// </summary>
        private void BtnPlayPause_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _previewWatchTimer.Stop();
                _btnPlayPause.Text = "Play";
                return;
            }

            double curSec = _mediaPlayer.Time / 1000.0;
            if (curSec < _slider.StartSec || curSec >= _slider.EndSec)
            {
                curSec = _slider.StartSec;
                _mediaPlayer.Time = (long)(curSec * 1000);
            }

            _mediaPlayer.Play();
            _previewWatchTimer.Start();
            _btnPlayPause.Text = "Pause";
        }

        /// <summary>
        /// Moves the end (out-point) thumb to wherever playback currently
        /// is -- lets the user just play until it looks right and hit this
        /// instead of pausing, eyeballing the time, and dragging the thumb
        /// to match. Works whether the video is currently playing or was
        /// last paused at some position (DualRangeSlider.CurrentSec tracks
        /// both cases already).
        /// </summary>
        private void BtnSetEndHere_Click(object sender, EventArgs e)
        {
            // Pause first -- otherwise playback keeps rolling right past
            // the new End the instant it's set (the auto-stop-at-End check
            // in PreviewWatchTimer_Tick was captured from the End at the
            // time Play was pressed, so it doesn't know End just moved).
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _previewWatchTimer.Stop();
                _btnPlayPause.Text = "Play";
            }

            _slider.SetEndToCurrent();
        }

        private void PreviewWatchTimer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer == null) return;

            double curSec = _mediaPlayer.Time / 1000.0;
            _slider.CurrentSec = curSec;

            // Read End live rather than a snapshot from when Play was
            // pressed -- otherwise dragging/nudging End further out mid-
            // playback would still pause at the old position.
            if (curSec >= _slider.EndSec)
            {
                _mediaPlayer.Pause();
                _previewWatchTimer.Stop();
                _btnPlayPause.Text = "Play";
            }
        }

        // ------------------------------------------------------------------
        // Output folder -- one FolderBrowserDialog picks both the
        // destination and the animation's folder name at once (its own
        // "Make New Folder" button covers naming a new one; navigating
        // into an existing assets/boot/<name> folder covers re-exporting
        // into one that's already there).
        // ------------------------------------------------------------------

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select or create the animation folder (normally under assets\\boot\\<name> on the SD card)";
                if (!string.IsNullOrEmpty(_txtOutputDir.Text) && Directory.Exists(_txtOutputDir.Text))
                    dlg.SelectedPath = _txtOutputDir.Text;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                _txtOutputDir.Text = dlg.SelectedPath;
            }
        }

        // ------------------------------------------------------------------
        // Convert
        // ------------------------------------------------------------------

        private void NumFps_ValueChanged(object sender, EventArgs e)
        {
            RefreshEstimate();
        }

        private void RefreshEstimate()
        {
            if (_slider == null || _lblEstFrames == null) return;
            double range = Math.Max(0, _slider.EndSec - _slider.StartSec);
            int n = Math.Max(1, (int)Math.Round(range * (double)_numFps.Value));
            _lblEstFrames.Text = $"{n} frames";
        }

        private async void BtnConvert_Click(object sender, EventArgs e)
        {
            if (_isConverting) return;

            if (string.IsNullOrWhiteSpace(_txtVideoPath.Text) || !File.Exists(_txtVideoPath.Text))
            {
                MessageBox.Show("Please select a valid input video file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtOutputDir.Text))
            {
                MessageBox.Show("Please select an output folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double start = _slider.StartSec;
            double end = _slider.EndSec;
            double duration = end - start;
            if (duration < 0.1)
            {
                MessageBox.Show("Selected range is too short -- drag the range slider to select at least 0.1 seconds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outDir = _txtOutputDir.Text;
            string folderName = Path.GetFileName(outDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            bool exists = Directory.Exists(outDir) && Directory.GetFiles(outDir, "frame_*.jpg").Length > 0;
            if (exists)
            {
                var confirm = MessageBox.Show(
                    $"Folder \"{folderName}\" already has animation frames. Overwrite?",
                    "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;
            }

            int fps = (int)_numFps.Value;
            int quality = (int)_numQuality.Value;
            string aspectMode = AspectModes[_cmbAspect.SelectedIndex].Mode;
            string videoPath = _txtVideoPath.Text;

            _isConverting = true;
            _btnConvert.Enabled = false;
            _progressBar.Value = 0;
            _txtLog.Clear();

            int totalFrames = Math.Max(1, (int)Math.Round(duration * fps));
            AppendLog($"Starting conversion: {folderName}, range {FormatTime(start)}~{FormatTime(end)}, {fps}fps, quality {quality}, {aspectMode}");

            bool ok = await Task.Run(() => RunConvert(videoPath, outDir, start, duration, fps, quality, aspectMode, totalFrames));

            _isConverting = false;
            _btnConvert.Enabled = true;

            if (ok)
            {
                int actualFrames = Directory.Exists(outDir)
                    ? Directory.GetFiles(outDir).Count(f => FrameFileRegex.IsMatch(Path.GetFileName(f)))
                    : 0;
                AppendLog($"Conversion complete: {actualFrames} frames.");
                MessageBox.Show(
                    $"Conversion complete. Generated {actualFrames} JPEG frames, written to:\n{outDir}",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private bool RunConvert(string videoPath, string outDir, double startSec, double durationSec,
                                 int fps, int quality, string aspectMode, int totalFrames)
        {
            try
            {
                Directory.CreateDirectory(outDir);

                // Clear old frame_*.jpg so a shorter re-export doesn't leave
                // stale trailing frames from a previous longer run.
                foreach (var f in Directory.GetFiles(outDir))
                {
                    if (FrameFileRegex.IsMatch(Path.GetFileName(f)))
                    {
                        try { File.Delete(f); } catch { /* best effort */ }
                    }
                }

                string vf = BuildFfmpegVf(aspectMode, ScreenW, ScreenH);
                string outPattern = Path.Combine(outDir, FrameFilePattern);

                var args = new[]
                {
                    "-y",
                    "-ss", startSec.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-i", videoPath,
                    "-t", durationSec.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-r", fps.ToString(),
                    "-vf", vf,
                    "-q:v", quality.ToString(),
                    "-start_number", "0",
                    outPattern,
                };

                bool ok = FfmpegRunner.Run(
                    args, totalFrames,
                    line => { if (!IsDisposed) BeginInvoke(new Action(() => AppendLog(line))); },
                    pct => { if (!IsDisposed) BeginInvoke(new Action(() => _progressBar.Value = Math.Min(100, Math.Max(0, pct)))); },
                    out string err);

                if (!ok)
                {
                    if (!IsDisposed) BeginInvoke(new Action(() => AppendLog("Error: " + err)));
                    return false;
                }

                bool anyFrame = Directory.GetFiles(outDir).Any(f => FrameFileRegex.IsMatch(Path.GetFileName(f)));
                if (!anyFrame)
                {
                    if (!IsDisposed) BeginInvoke(new Action(() => AppendLog("Error: ffmpeg finished but produced no frames.")));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!IsDisposed) BeginInvoke(new Action(() => AppendLog("Conversion exception: " + ex.Message)));
                return false;
            }
        }

        /// <summary>
        /// Ported directly from boot_anim_converter.py's build_ffmpeg_vf().
        /// </summary>
        private static string BuildFfmpegVf(string mode, int width, int height)
        {
            switch (mode)
            {
                case "crop":
                    return $"scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height}";
                case "pad":
                    return $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
                           $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=black";
                default: // stretch
                    return $"scale={width}:{height}";
            }
        }

        // ------------------------------------------------------------------
        // Log
        // ------------------------------------------------------------------

        private void AppendLog(string text)
        {
            if (_txtLog.InvokeRequired)
            {
                _txtLog.BeginInvoke(new Action(() => AppendLog(text)));
                return;
            }
            _txtLog.AppendText(text + Environment.NewLine);
        }
    }
}
