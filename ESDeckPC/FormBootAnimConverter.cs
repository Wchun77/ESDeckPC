using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    /// Preview playback uses the Windows Media Player ActiveX control
    /// (AxWMPLib.AxWindowsMediaPlayer) rather than LibVLC -- WMP ships
    /// with Windows itself, so unlike the old LibVLCSharp +
    /// VideoLAN.LibVLC.Windows NuGet trio (~380MB of bundled decoders),
    /// this adds zero bytes to the install. The tradeoff is codec
    /// coverage: WMP only decodes what Windows' built-in codecs support
    /// (H.264 is the safe baseline; HEVC in particular is often NOT
    /// installed by default). See ProbeVideo/SupportedPreviewCodecs
    /// below -- unsupported files are rejected up front with a message
    /// telling the user to convert first, rather than silently failing
    /// to preview.
    ///
    /// WMP is ONLY ever touched when the user explicitly presses Play.
    /// Loading a video, dragging a trim thumb or the playhead, and
    /// landing on the End marker all show a static frame extracted via
    /// ffmpeg instead (see ExtractFramePreview / _previewImage). Earlier
    /// revisions also used WMP for that idle/scrubbing preview -- calling
    /// Play() automatically on load, then racing to catch and pause it
    /// before the user could see it move. WMP's asynchronous open/
    /// buffer/play event sequence fires an unpredictable number of times
    /// per load, and reacting to it correctly every single time proved
    /// unreliable in practice (auto-play glitches, stale thumbnails, UI
    /// stalls from overlapping ffmpeg processes spawned by a handler that
    /// re-ran more often than expected). Routing everything except
    /// actual playback through ffmpeg single-frame extraction sidesteps
    /// all of that: it's WYSIWYG (same decoder path as the real export),
    /// and WMP's event timing no longer matters for anything except the
    /// brief window between pressing Play and the video actually starting
    /// to move -- normal player startup latency, not a glitch.
    ///
    /// NOTE: the WMP COM reference needs a one-time Visual Studio step --
    /// see the COMReference comment in ESDeckPC.csproj.
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

        // _wmp's underlying ActiveX/COM object isn't actually alive until
        // its own window handle is created, which is guaranteed to have
        // happened for every child control by OnLoad -- unlike the Form
        // constructor, it's safe to touch _wmp's COM properties here.
        // Mostly just idle-state hygiene now since WMP isn't engaged
        // until Play is pressed, but cheap and harmless to set early.
        //
        // _wmp.Visible = false here matters far more than it looks --
        // _wmp is a windowed ActiveX control (it owns a real native HWND),
        // and Windows always paints a windowed control's HWND on top of
        // any plain GDI-drawn sibling (like _previewImage) that shares its
        // screen area, regardless of Z-order or the sibling's own Visible
        // property ("airspace" problem). Toggling only _previewImage's
        // Visible never actually hid WMP's own output -- WMP's blank/
        // playing surface was bleeding through on top of it the entire
        // time, which is what made the preview look like it only ever
        // showed anything correct while WMP itself was actually playing.
        // The real fix is to hide _wmp itself (see StopPlaybackIfPlaying/
        // BtnPlayPause_Click) whenever _previewImage should be the one
        // visible.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _wmp.uiMode = "none"; // hide WMP's own transport bar -- we drive playback with our own controls/shortcuts
            _wmp.settings.mute = true;
            _wmp.settings.volume = 0;
            _wmp.Visible = false;

            // videoHost (the plain Panel hosting both _wmp and
            // _previewImage stacked on top of each other) isn't double-
            // buffered by default -- Panel doesn't expose that property
            // publicly, so it's set via reflection here. Without it,
            // toggling one child's Visible off while the other's flips on
            // can show a flash of the panel's own background color for a
            // frame in between, on top of whatever WMP/DirectShow-level
            // causes are already in play.
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, videoHost, new object[] { true });
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

        private double _videoDurationSec = 0;
        private string _thumbTempDir;
        private System.Windows.Forms.Timer _previewWatchTimer;
        private bool _isConverting = false;

        // Bumped on every LoadVideo() call. Background work kicked off by
        // an older load (thumbnail extraction, frame preview extraction)
        // captures the generation it was started for and checks it again
        // before applying results, so a slow leftover task from a
        // previous video can't clobber state for whatever video is
        // actually loaded now.
        private int _loadGeneration = 0;

        // Debounces preview-frame requests while dragging a thumb or the
        // playhead. Extracting a frame via ffmpeg spawns a real OS
        // process -- doing that on every single mouse-move tick (which
        // can fire dozens of times/sec) would spawn a pile of overlapping
        // ffmpeg processes with their results racing each other.
        // RequestFramePreview() resets this timer on every call, so while
        // the drag keeps producing new positions the timer never actually
        // fires; the instant the drag pauses or the mouse is released, it
        // fires once and extracts exactly one frame at wherever things
        // ended up.
        private System.Windows.Forms.Timer _framePreviewTimer;
        private double _pendingFramePreviewSec = double.NaN;

        // Extra staleness guard alongside _loadGeneration -- if two frame
        // requests are in flight (e.g. a slow one from a moment ago and a
        // newer one from wherever the drag ended), only the most recently
        // *requested* one's result should ever actually get applied.
        private int _frameRequestSeq = 0;

        // Path currently (or most recently) loaded into _wmp -- distinct
        // from _txtVideoPath.Text, which reflects the selected video and
        // may not match this at all if WMP hasn't been engaged for it yet
        // (nothing touches _wmp until the user actually presses Play).
        private string _wmpLoadedPath;

        // Set right when _wmp.URL is (re)assigned in BtnPlayPause_Click.
        // Wmp_PlayStateChange does exactly one seek to
        // _pendingFirstSeekTarget the first time currentMedia becomes
        // available afterwards, then leaves WMP alone to keep playing --
        // unlike the old load-time logic, this only ever runs because the
        // user just asked to play, so any brief startup latency here is
        // expected player behavior, not an unwanted "sneaks forward"
        // glitch.
        private bool _pendingFirstSeek = false;
        private double _pendingFirstSeekTarget = 0;

        // Polls _wmp's actual position after a resume-from-drag seek+play,
        // instead of blindly guessing a fixed delay before revealing it --
        // see BtnPlayPause_Click's resume branch / ResumePlayTimer_Tick.
        // A fixed-delay version of this was tried first and still let a
        // stale frame (from wherever _wmp was before this seek) show
        // through occasionally, because the fixed delay was a guess, not
        // a confirmation that the seek had actually landed. Polling
        // currentPosition until it reads at/past the target (or a safety
        // timeout elapses) reveals _wmp only once we have real evidence
        // it's caught up.
        private System.Windows.Forms.Timer _resumePlayTimer;
        private double _pendingResumeSec;
        private DateTime _resumePollStartUtc;
        private const int ResumePollTimeoutMs = 600;

        // Guards BtnPlayPause_Click against rapid repeat presses (mashing
        // Space, or OS key-repeat if held down). Each press issues a real
        // synchronous COM call into WMP (pause/play, sometimes a
        // currentPosition seek), but WMP's own internal state doesn't
        // finish settling instantly -- firing another one before the
        // previous command has taken effect queues up contradictory
        // play/pause/seek requests faster than WMP can keep up with,
        // which is what shows on screen as the video jittering back and
        // forth. A short cooldown between toggles fixes it without
        // making single deliberate presses feel any less responsive.
        private DateTime _lastPlayPauseToggleUtc = DateTime.MinValue;
        private const int PlayPauseCooldownMs = 200;

        // Preview playback only trusts Windows' own built-in codecs (via
        // WMP) -- this is the deliberate lightweight-install tradeoff, see
        // the class doc comment. If ffmpeg reports something outside this
        // list, LoadVideo refuses to preview it and tells the user to
        // convert the file first instead of loading a black/broken player.
        private static readonly HashSet<string> SupportedPreviewCodecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "h264", "mpeg4", "msmpeg4v3", "wmv3", "vc1",
        };
        private static readonly Regex VideoCodecRegex = new Regex(@"Video:\s*([a-zA-Z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex DurationRegex = new Regex(@"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", RegexOptions.Compiled);

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

            _previewWatchTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _previewWatchTimer.Tick += PreviewWatchTimer_Tick;

            _framePreviewTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _framePreviewTimer.Tick += FramePreviewTimer_Tick;

            _resumePlayTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _resumePlayTimer.Tick += ResumePlayTimer_Tick;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            _previewWatchTimer?.Stop();
            _previewWatchTimer?.Dispose();
            _framePreviewTimer?.Stop();
            _framePreviewTimer?.Dispose();
            _resumePlayTimer?.Stop();
            _resumePlayTimer?.Dispose();

            try { _wmp?.Ctlcontrols?.stop(); }
            catch { /* best effort cleanup */ }

            _previewImage?.Image?.Dispose();

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

                LoadVideo(dlg.FileName);
            }
        }

        private void LoadVideo(string path)
        {
            // Duration/codec both come from a single ffmpeg probe -- see
            // ProbeVideo's doc comment.
            ProbeVideo(path, out string codec, out double durationSec);

            if (codec != null && !SupportedPreviewCodecs.Contains(codec))
            {
                AppendLog($"Preview not supported for codec \"{codec}\". Please convert the video to H.264 (MP4) first -- e.g. with HandBrake or any online video converter -- then select it again.");
                MessageBox.Show(
                    $"This video uses the \"{codec}\" codec, which the built-in preview player can't decode.\n\n" +
                    "Please convert it to H.264 (MP4) using a tool like HandBrake or an online converter, then select the converted file.",
                    "Unsupported Video Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (durationSec <= 0)
            {
                AppendLog("Could not read video duration -- please check the file is valid.");
                return;
            }

            // Drop back to a stopped state -- if a previous video was
            // still mid-playback, switching away from it shouldn't leave
            // WMP running underneath.
            StopPlaybackIfPlaying();

            _txtVideoPath.Text = path;
            AppendLog("Loading video: " + path);

            _loadGeneration++;
            int myGeneration = _loadGeneration;

            // Clear both immediately rather than waiting for the new
            // extraction to finish -- otherwise the previous video's
            // frame/thumbnail strip visibly lingers for however long the
            // new one takes to generate.
            _slider.SetThumbnails(null);
            var oldImg = _previewImage.Image;
            _previewImage.Image = null;
            oldImg?.Dispose();

            _videoDurationSec = durationSec;
            _slider.TotalDurationSec = _videoDurationSec;
            _slider.SetRange(0, Math.Min(_videoDurationSec, 3.0));
            _slider.CurrentSec = _slider.StartSec;
            UpdateRangeLabel();
            RefreshEstimate();

            StartThumbnailExtraction(path, _videoDurationSec, myGeneration);

            // Called directly (not through the drag-debounce path) -- this
            // is a single one-off request, not a burst of mouse-move
            // events, so there's no reason to add the debounce's extra
            // ~100ms delay before the frame even starts extracting.
            ExtractFramePreview(_slider.StartSec, myGeneration);

            // Deliberately NOT touching _wmp here at all -- see the class
            // doc comment. It's only ever loaded/played the first time
            // the user actually presses Play for this video (see
            // BtnPlayPause_Click).
        }

        /// <summary>
        /// Runs the bundled ffmpeg.exe with no output (just -i) to read the
        /// input stream info from stderr, pulling out both the video codec
        /// name and duration in one shot. Codec/duration are left at
        /// null/0 (not thrown) if probing isn't possible -- callers treat
        /// that as "let WMP try anyway" for codec, but treat duration
        /// &lt;= 0 as a hard failure since nothing else in this form works
        /// without it.
        /// </summary>
        private static void ProbeVideo(string path, out string codec, out double durationSec)
        {
            codec = null;
            durationSec = 0;

            if (!FfmpegRunner.IsAvailable()) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = FfmpegRunner.GetFfmpegPath(),
                    Arguments = $"-hide_banner -i \"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                using (var proc = Process.Start(psi))
                {
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(5000);

                    var codecMatch = VideoCodecRegex.Match(stderr);
                    if (codecMatch.Success)
                        codec = codecMatch.Groups[1].Value.ToLowerInvariant();

                    var durMatch = DurationRegex.Match(stderr);
                    if (durMatch.Success)
                    {
                        double h = double.Parse(durMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                        double m = double.Parse(durMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                        double s = double.Parse(durMatch.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                        durationSec = h * 3600 + m * 60 + s;
                    }
                }
            }
            catch
            {
                // codec/durationSec stay at their default null/0 -- caller
                // handles that as "probing failed".
            }
        }

        /// <summary>
        /// Extracts a single JPEG frame at the given position via ffmpeg
        /// and shows it in _previewImage. This -- not WMP -- drives the
        /// visible preview for everything except actual playback: it's
        /// WYSIWYG (same decoder path as the real export) and has no
        /// asynchronous open/buffer/play state machine to race against.
        /// generation/mySeq both guard against a slow request finishing
        /// after a newer one (or a newer video) has already superseded it.
        /// </summary>
        private void ExtractFramePreview(double sec, int generation)
        {
            string path = _txtVideoPath.Text;
            if (string.IsNullOrEmpty(path) || !FfmpegRunner.IsAvailable()) return;

            int mySeq = ++_frameRequestSeq;

            Task.Run(() =>
            {
                string tmpFile = Path.Combine(Path.GetTempPath(), "esdeck_boot_frame_" + Guid.NewGuid().ToString("N") + ".jpg");
                try
                {
                    // Two-stage seek -- NOT plain "-ss sec -i path" (fast
                    // but only keyframe-accurate: for a video with a
                    // sparse-keyframe/scene-cut-heavy GOP structure, that
                    // snaps to a keyframe that can be a full scene away
                    // from the requested time, which was the actual cause
                    // of every "rolls back"/"shows the wrong frame" symptom
                    // chased earlier in this file's history) and NOT plain
                    // "-i path -ss sec" either (frame-accurate but decodes
                    // sequentially from the nearest keyframe all the way to
                    // sec, which can take well over a second if sec is far
                    // from any keyframe -- that showed up as the SAME
                    // symptom again, just delayed: the correct frame still
                    // arrived, but late enough to look like the picture
                    // "changes on its own" after a pause with no
                    // interaction). Coarse input-seek to 5s before the
                    // target, then a precise output-seek for the small
                    // remainder, caps the decode distance so this is
                    // consistently fast AND exact regardless of where sec
                    // falls relative to the video's keyframes.
                    double fastSeek = Math.Max(0, sec - 5.0);
                    double fineSeek = sec - fastSeek;
                    var args = new[]
                    {
                        "-y",
                        "-ss", fastSeek.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        "-i", path,
                        "-ss", fineSeek.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        "-frames:v", "1",
                        "-q:v", "3",
                        tmpFile,
                    };

                    FfmpegRunner.Run(args, 0, line => { }, pct => { }, out string err);

                    if (generation != _loadGeneration || mySeq != _frameRequestSeq || !File.Exists(tmpFile))
                        return; // superseded, or extraction failed -- leave whatever was showing

                    Bitmap bmp;
                    using (var fs = new FileStream(tmpFile, FileMode.Open, FileAccess.Read))
                        bmp = new Bitmap(Image.FromStream(fs));

                    if (IsDisposed)
                    {
                        bmp.Dispose();
                        return;
                    }

                    BeginInvoke(new Action(() =>
                    {
                        if (generation != _loadGeneration || mySeq != _frameRequestSeq)
                        {
                            bmp.Dispose();
                            return;
                        }
                        var old = _previewImage.Image;
                        _previewImage.Image = bmp;
                        old?.Dispose();

                        // Swap away from _wmp only now, once _previewImage
                        // actually holds the matching frame -- avoids ever
                        // showing _previewImage before it has the right
                        // contents (that ordering is what fixed the
                        // original "pause rolls back" flash). Every pause
                        // path (PauseWmpInPlace + this swap) ends up here,
                        // including plain in-place pauses -- not just
                        // because it's the source-of-truth frame, but
                        // because _wmp is a windowed ActiveX control whose
                        // own surface can go stale (show old cached
                        // content) if the form is moved, covered, or
                        // otherwise repainted while paused, since nothing
                        // is decoding new frames to refresh it. A plain
                        // GDI PictureBox like _previewImage doesn't have
                        // that problem -- it always repaints correctly.
                        _wmp.Visible = false;
                        _previewImage.Visible = true;
                    }));
                }
                catch
                {
                    /* best effort -- leave whatever was showing */
                }
                finally
                {
                    try { if (File.Exists(tmpFile)) File.Delete(tmpFile); }
                    catch { /* best effort */ }
                }
            });
        }

        private void RequestFramePreview(double sec)
        {
            _pendingFramePreviewSec = sec;
            _framePreviewTimer.Stop();
            _framePreviewTimer.Start();
        }

        private void FramePreviewTimer_Tick(object sender, EventArgs e)
        {
            _framePreviewTimer.Stop();
            if (double.IsNaN(_pendingFramePreviewSec)) return;

            double sec = _pendingFramePreviewSec;
            _pendingFramePreviewSec = double.NaN;
            ExtractFramePreview(sec, _loadGeneration);
        }

        /// <summary>
        /// Plain pause -- pauses WMP if it's actively playing and resets
        /// the Play/Pause button and watch timer, nothing else. Used by
        /// Slider_RangeChanged/Slider_SeekRequested, which pause and then
        /// immediately seek/extract a frame for wherever the drag ended
        /// up anyway via RequestFramePreview -- no need for the extra
        /// PauseWmpInPlace redraw-forcing work first.
        ///
        /// Callers that are pausing "in place" instead (the user hit
        /// Pause/Space, playback reached End, Set-End-Here while playing)
        /// use PauseWmpInPlace, not this.
        /// </summary>
        private void StopPlaybackIfPlaying()
        {
            try
            {
                if (_wmp?.currentMedia != null && _wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
                    _wmp.Ctlcontrols.pause();
            }
            catch { /* best effort */ }

            _previewWatchTimer.Stop();
            _btnPlayPause.Text = "Play";
        }

        /// <summary>
        /// Pauses WMP at the current position, syncs _slider.CurrentSec to
        /// it, and returns that position (NaN if nothing was playing).
        ///
        /// Does NOT force an extra redraw seek here (an earlier revision
        /// did -- immediately re-asserting currentPosition after pause()
        /// to work around AxWindowsMediaPlayer's render buffer lagging a
        /// frame or two behind on pause). That's no longer needed: every
        /// caller of this follows up with ExtractFramePreview, which
        /// shows the actually-correct frame (via ffmpeg, not WMP's own
        /// rendering) within a couple hundred ms regardless, and it turns
        /// out that extra forced seek was itself causing a visible flash
        /// on _wmp's surface right at the moment of pausing -- fixing the
        /// precision of a frame that's about to be covered up anyway
        /// wasn't worth that cost.
        ///
        /// Syncing _slider.CurrentSec here matters on its own, though:
        /// it's otherwise only updated once per _previewWatchTimer tick
        /// (every 50ms) during playback, so by the moment pause actually
        /// happens it can be lagging up to one tick behind.
        /// BtnPlayPause_Click's resume branch reads _slider.CurrentSec to
        /// decide where to resume -- without this sync it would resume
        /// from that stale, slightly-earlier value and visibly seek
        /// backward before playing.
        /// </summary>
        private double PauseWmpInPlace()
        {
            double pos = double.NaN;
            try
            {
                if (_wmp?.currentMedia != null && _wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    pos = _wmp.Ctlcontrols.currentPosition;
                    _wmp.Ctlcontrols.pause();
                    _slider.CurrentSec = pos;
                }
            }
            catch { /* best effort */ }

            _previewWatchTimer.Stop();
            _btnPlayPause.Text = "Play";
            return pos;
        }

        /// <summary>
        /// Fires on every WMP play-state transition. Only does anything
        /// while _pendingFirstSeek is set (right after BtnPlayPause_Click
        /// (re)assigns _wmp.URL): the first time currentMedia becomes
        /// available afterwards, seek once to _pendingFirstSeekTarget and
        /// then get out of the way, letting playback continue. The
        /// early-out matters -- this can fire many times in a row while
        /// WMP works through its own internal buffering/transitioning
        /// states, and it should be a cheap no-op for all of those except
        /// the one that actually matters.
        /// </summary>
        private void Wmp_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (!_pendingFirstSeek || _wmp.currentMedia == null) return;

            _pendingFirstSeek = false;

            _wmp.settings.mute = true;
            _wmp.settings.volume = 0;
            _wmp.Ctlcontrols.currentPosition = _pendingFirstSeekTarget;
        }

        // ------------------------------------------------------------------
        // Thumbnail strip (background ffmpeg extraction, full video length)
        // ------------------------------------------------------------------

        private void StartThumbnailExtraction(string videoPath, double durationSec, int generation)
        {
            CleanupThumbTempDir();

            // Captured into a local instead of read back from the
            // _thumbTempDir field inside the background Task below -- if
            // the user picks another video before this extraction
            // finishes, LoadVideo's next call reassigns the field (and
            // calls CleanupThumbTempDir again) out from under this still-
            // running Task. Using a local means this Task always operates
            // on the folder it actually created, regardless of what the
            // field points to by the time it gets around to reading it.
            string thumbDir = Path.Combine(Path.GetTempPath(), "esdeck_boot_thumbs_" + Guid.NewGuid().ToString("N"));
            _thumbTempDir = thumbDir;

            const int thumbW = 160, thumbH = 90;
            const int targetCount = 30;
            double interval = Math.Max(0.3, durationSec / targetCount);
            int expectedCount = Math.Max(1, (int)Math.Ceiling(durationSec / interval));

            Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(thumbDir);

                    string vf = $"fps=1/{interval.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                $"scale={thumbW}:{thumbH}:force_original_aspect_ratio=increase,crop={thumbW}:{thumbH}";
                    string outPattern = Path.Combine(thumbDir, "thumb_%04d.jpg");

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
                        if (generation == _loadGeneration && !IsDisposed)
                            BeginInvoke(new Action(() => AppendLog("Thumbnail extraction failed: " + err)));
                        return;
                    }

                    var files = Directory.GetFiles(thumbDir, "thumb_*.jpg").OrderBy(f => f).ToList();
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

                    // Discard results from a superseded load -- if the user
                    // picked another video while this was still running,
                    // applying these now would show the wrong video's strip.
                    if (generation != _loadGeneration) return;

                    if (bitmaps.Count > 0 && !IsDisposed)
                    {
                        BeginInvoke(new Action(() => _slider.SetThumbnails(bitmaps)));
                    }
                }
                catch (Exception ex)
                {
                    if (generation == _loadGeneration && !IsDisposed)
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

            // Only touch the preview if the trim range moved out from
            // under where the playhead already is -- otherwise leave it
            // exactly where it was. Nudging one thumb shouldn't yank the
            // preview over to it every time; it should only move if the
            // playhead is no longer inside [Start, End] at all, in which
            // case it snaps to whichever edge it fell past.
            double curSec = _slider.CurrentSec;
            if (curSec < 0) return; // nothing has played/positioned yet

            double clamped = Math.Max(_slider.StartSec, Math.Min(_slider.EndSec, curSec));
            if (Math.Abs(clamped - curSec) < 0.001) return; // still inside the range, leave it alone

            StopPlaybackIfPlaying();
            _slider.CurrentSec = clamped;
            RequestFramePreview(clamped);
        }

        /// <summary>
        /// Live scrub -- fired continuously while dragging the playhead
        /// (or the middle of the timeline) on the slider, independent of
        /// the trim range.
        /// </summary>
        private void Slider_SeekRequested(object sender, double sec)
        {
            StopPlaybackIfPlaying();
            RequestFramePreview(sec);
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
        /// off. This is the ONLY place that ever touches _wmp.URL/Play --
        /// see the class doc comment for why.
        /// </summary>
        private void BtnPlayPause_Click(object sender, EventArgs e)
        {
            string path = _txtVideoPath.Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            var nowUtc = DateTime.UtcNow;
            if ((nowUtc - _lastPlayPauseToggleUtc).TotalMilliseconds < PlayPauseCooldownMs)
                return; // still settling from the last toggle -- ignore this repeat
            _lastPlayPauseToggleUtc = nowUtc;

            if (_wmp.currentMedia != null && _wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                // Just pausing -- deliberately NOT calling ExtractFramePreview
                // here anymore. _wmp's own paused frame is already correct
                // (confirmed once the forced re-seek in PauseWmpInPlace was
                // removed -- see that method's comment), so swapping over
                // to a freshly-ffmpeg-extracted _previewImage ~100-140ms
                // later was pure overhead: log data confirmed the position
                // is always right and applies quickly, but the swap itself
                // (WMP's live-decoded frame vs. a JPEG re-encode of the same
                // instant, plus the Visible toggle between the two
                // controls) still reads as a visible blink on every single
                // pause. The previewImage hand-off exists to cover a
                // narrower case -- _wmp's windowed surface going stale if
                // the form is moved/covered while paused (see
                // ExtractFramePreview's comment) -- which is rare enough
                // that it's a better trade to accept vs. flickering on
                // every ordinary pause.
                PauseWmpInPlace();
                return;
            }

            double curSec = _slider.CurrentSec;

            // Epsilon tolerance on the End-boundary check -- a drag that
            // lands the playhead visually "right at" the End thumb (via
            // DualRangeSlider.SeekTo's own Math.Min(_endSec, sec) clamp)
            // can still end up a tiny fraction below _slider.EndSec due to
            // floating-point rounding in the pixel-to-seconds conversion,
            // so a strict ">=" comparison could miss it and treat "at the
            // end" as "still playable", letting playback continue instead
            // of resetting to Start like the user would expect.
            const double EndBoundaryEpsilon = 0.05;
            bool outOfRange = curSec < 0 || curSec < _slider.StartSec || curSec >= _slider.EndSec - EndBoundaryEpsilon;
            if (outOfRange)
                curSec = _slider.StartSec;

            _wmp.settings.mute = true;
            _wmp.settings.volume = 0;
            _wmp.uiMode = "none";

            if (_wmpLoadedPath != path)
            {
                // First time playing this particular video in this
                // session -- WMP has nothing loaded yet (LoadVideo never
                // touches it). Load it now; Wmp_PlayStateChange does a
                // one-time seek to curSec once it's actually ready. Some
                // startup latency here is normal since the user just
                // pressed Play themselves.
                _previewImage.Visible = false;
                _wmp.Visible = true; // must show the ActiveX control itself -- see OnLoad's comment
                _wmpLoadedPath = path;
                _pendingFirstSeek = true;
                _pendingFirstSeekTarget = curSec;
                _wmp.URL = path;
                _wmp.Ctlcontrols.play();

                _previewWatchTimer.Start();
                _btnPlayPause.Text = "Pause";
            }
            else if (_wmp.Visible && !outOfRange)
            {
                // Plain resume from an in-place pause, still within
                // [Start, End) -- _wmp was never hidden (pausing no longer
                // swaps to _previewImage, see that branch above), so it's
                // already sitting on the exact right frame with nothing
                // stale to clear first. No seek, no delay -- just
                // continue.
                //
                // Deliberately gated on !outOfRange too -- if curSec got
                // reset to Start above (e.g. this is a re-press right
                // after auto-stopping at End), _wmp is still sitting at/
                // past End internally even though it's Visible, and
                // blindly calling play() here would just resume forward
                // from there instead of actually restarting at Start.
                // That case falls through to the else branch below, which
                // does seek first.
                _wmp.Ctlcontrols.play();
                _previewWatchTimer.Start();
                _btnPlayPause.Text = "Pause";
            }
            else
            {
                // Resuming after _previewImage took over -- i.e. the user
                // dragged/scrubbed elsewhere while paused, which does
                // still swap to _previewImage (see Slider_RangeChanged/
                // Slider_SeekRequested). _previewImage stays showing the
                // correct target frame while _wmp seeks and starts
                // playing in the background, still hidden -- Resume
                // PlayTimer_Tick polls _wmp's actual position and only
                // reveals it (swapping away from _previewImage) once
                // there's real evidence it has caught up to curSec, not
                // just a guessed delay. See that field's comment for why
                // a fixed delay wasn't reliable enough.
                _wmp.Ctlcontrols.currentPosition = curSec;
                _wmp.Ctlcontrols.play();
                _pendingResumeSec = curSec;
                _resumePollStartUtc = DateTime.UtcNow;
                _resumePlayTimer.Stop();
                _resumePlayTimer.Start();

                _btnPlayPause.Text = "Pause";
            }
        }

        /// <summary>
        /// Ticks every 20ms after BtnPlayPause_Click's resume branch
        /// issues the seek+play to _pendingResumeSec, polling _wmp's own
        /// reported position. _previewImage keeps showing the correct
        /// frame the whole time this is polling, so there's nothing wrong
        /// on screen while it waits -- _wmp is only revealed (swapping
        /// away from _previewImage) once its currentPosition confirms it
        /// has actually reached the target, or the safety timeout runs
        /// out (best-effort fallback in case a hidden control's position
        /// genuinely doesn't advance until shown -- reveals it anyway
        /// rather than getting stuck).
        ///
        /// An earlier revision used a single fixed ~120ms delay instead of
        /// polling, guessing that was enough time for the seek to land.
        /// That guess wasn't reliable -- the delay either wasn't always
        /// long enough, or a redundant reseek right before reveal (since
        /// removed) reintroduced the same race it was meant to avoid.
        /// Polling replaces the guess with an actual check.
        /// </summary>
        private void ResumePlayTimer_Tick(object sender, EventArgs e)
        {
            if (_wmp?.currentMedia == null)
            {
                _resumePlayTimer.Stop();
                return;
            }

            double pos;
            try { pos = _wmp.Ctlcontrols.currentPosition; }
            catch { pos = _pendingResumeSec; }

            bool caughtUp = pos >= _pendingResumeSec - 0.1;
            bool timedOut = (DateTime.UtcNow - _resumePollStartUtc).TotalMilliseconds >= ResumePollTimeoutMs;

            if (!caughtUp && !timedOut)
                return; // keep polling next tick

            _resumePlayTimer.Stop();
            _previewImage.Visible = false;
            _wmp.Visible = true;
            _previewWatchTimer.Start();
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
            if (_wmp?.currentMedia != null && _wmp.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                // PauseWmpInPlace already syncs _slider.CurrentSec to the
                // exact pause position. No ExtractFramePreview hand-off
                // here either -- see BtnPlayPause_Click's pause branch.
                PauseWmpInPlace();
            }

            _slider.SetEndToCurrent();
        }

        private void PreviewWatchTimer_Tick(object sender, EventArgs e)
        {
            if (_wmp?.currentMedia == null) return;

            double curSec = _wmp.Ctlcontrols.currentPosition;
            _slider.CurrentSec = curSec;

            // Read End live rather than a snapshot from when Play was
            // pressed -- otherwise dragging/nudging End further out mid-
            // playback would still pause at the old position.
            if (curSec >= _slider.EndSec)
            {
                // Auto-stop at End -- same reasoning as the other pause
                // sites, no ExtractFramePreview hand-off.
                PauseWmpInPlace();
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

                // Two-stage seek, same reasoning as ExtractFramePreview's
                // comment: a single "-ss startSec -i videoPath" is fast but
                // only keyframe-accurate, so the exported sequence's first
                // frame (and everything after it, since -t/-r count frames
                // from there) could start at a noticeably different point
                // than what the preview showed -- especially on sparse-
                // keyframe video. Splitting into a coarse input-seek to
                // 5s before the target, then a precise output-seek for the
                // remainder, keeps this fast for long videos while landing
                // exactly on startSec.
                double fastSeek = Math.Max(0, startSec - 5.0);
                double fineSeek = startSec - fastSeek;

                var args = new[]
                {
                    "-y",
                    "-ss", fastSeek.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-i", videoPath,
                    "-ss", fineSeek.ToString(System.Globalization.CultureInfo.InvariantCulture),
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
