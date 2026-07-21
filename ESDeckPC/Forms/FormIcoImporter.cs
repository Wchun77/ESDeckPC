using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormIcoImporter : Form
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        // Matches the deck grid button's icon area in firmware
        // (main/ui/ui_deck.c: img_cont is 100x100 inside a 160x150 button).
        private const int OutputW = 100;
        private const int OutputH = 100;

        // ------------------------------------------------------------------
        // DWM dark title bar
        // ------------------------------------------------------------------

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
        }

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private Bitmap _source = null;
        private string _sourceFileName = null;

        // Current source-pixels -> canvas-pixels scale factor. Set to the
        // auto-fit/auto-fill value whenever an image loads or the mode
        // radio changes, then freely adjustable afterward via the mouse
        // wheel (Canvas_MouseWheel) -- e.g. shrinking below the auto-Fill
        // zoom so a non-square source doesn't fill every pixel.
        private float _zoom = 1f;

        // Scaled bitmap dimensions in logical pixels, derived from _zoom.
        private int _scaledW, _scaledH;

        // Current top-left offset of the scaled image relative to the output
        // canvas, in logical pixels. Centered on any axis where the image is
        // smaller than the canvas (nothing to pan there); clamped to keep
        // the canvas fully covered on any axis where it's larger -- see
        // ClampOffsetX/Y.
        private int _offsetX, _offsetY;

        // Pan drag state (only while the image is larger than the canvas on
        // at least one axis -- see Canvas_MouseDown)
        private bool _dragging = false;
        private Point _dragStart;
        private int _offsetXAtDrag, _offsetYAtDrag;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormIcoImporter()
        {
            InitializeComponent();
            UpdateHint();
        }

        // ------------------------------------------------------------------
        // Load
        // ------------------------------------------------------------------

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select icon image";
                dlg.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    // Load into a memory copy immediately so the original file
                    // is not locked. Source alpha (if the PNG was pre-cut-out)
                    // is preserved as-is -- this form never decides on its own
                    // whether the result is transparent, it only composites
                    // whatever the source already has.
                    Bitmap loaded = new Bitmap(dlg.FileName);
                    _source?.Dispose();
                    _source = new Bitmap(loaded);
                    loaded.Dispose();

                    _sourceFileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                    lblFile.Text = Path.GetFileName(dlg.FileName);
                    lblFile.ForeColor = Color.FromArgb(180, 180, 180);

                    RecomputeLayout();
                    btnSave.Enabled = true;
                    canvas.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ------------------------------------------------------------------
        // Mode switch
        // ------------------------------------------------------------------

        private void Mode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHint();

            if (_source == null) return;
            RecomputeLayout();
            canvas.Invalidate();
        }

        private void UpdateHint()
        {
            lblHint.Text = rbFill.Checked
                ? "Drag/arrows to pan, scroll to zoom\r\nOutput: 100 x 100 PNG"
                : "Whole image shown, letterboxed\r\nDrag/arrows/scroll  |  100 x 100 PNG";
        }

        /// <summary>
        /// Resets to the automatic Fill/Fit zoom for the current mode and
        /// centers the image. Called on load and on mode switch -- manual
        /// zoom (mouse wheel) always starts fresh from here.
        /// </summary>
        private void RecomputeLayout()
        {
            float zx = (float)OutputW / _source.Width;
            float zy = (float)OutputH / _source.Height;
            _zoom = rbFill.Checked ? Math.Max(zx, zy) : Math.Min(zx, zy);

            ApplyZoom();
            _offsetX = (OutputW - _scaledW) / 2;
            _offsetY = (OutputH - _scaledH) / 2;
        }

        private void ApplyZoom()
        {
            _scaledW = Math.Max(1, (int)Math.Round(_source.Width * _zoom));
            _scaledH = Math.Max(1, (int)Math.Round(_source.Height * _zoom));
        }

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_source == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save icon PNG";
                dlg.Filter = "PNG image (*.png)|*.png";
                dlg.FileName = _sourceFileName + ".png";

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var output = CropToBitmap())
                    {
                        output.Save(dlg.FileName, ImageFormat.Png);
                    }

                    MessageBox.Show($"Saved: {Path.GetFileName(dlg.FileName)}",
                        "Icon Importer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save failed:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ------------------------------------------------------------------
        // Canvas paint
        // ------------------------------------------------------------------

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.FromArgb(18, 18, 18));

            if (_source == null) return;

            g.DrawImage(_source, _offsetX, _offsetY, _scaledW, _scaledH);

            // Output boundary -- the actual 100x100 asset edge, in both modes.
            using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1))
                g.DrawRectangle(pen, 0, 0, OutputW - 1, OutputH - 1);
        }

        // ------------------------------------------------------------------
        // Pan -- draggable on any axis where the scaled image doesn't
        // exactly match the canvas size, in either direction: bigger lets
        // you slide which part gets cropped, smaller lets you slide the
        // image to one side within its transparent margin (handy when the
        // source itself is off-center) -- see ClampOffsetX/Y.
        // ------------------------------------------------------------------

        private bool CanPan => _scaledW != OutputW || _scaledH != OutputH;

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _source == null || !CanPan) return;
            _dragging = true;
            _dragStart = e.Location;
            _offsetXAtDrag = _offsetX;
            _offsetYAtDrag = _offsetY;
            canvas.Cursor = Cursors.SizeAll;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            int dx = e.X - _dragStart.X;
            int dy = e.Y - _dragStart.Y;
            _offsetX = ClampOffsetX(_offsetXAtDrag + dx);
            _offsetY = ClampOffsetY(_offsetYAtDrag + dy);
            canvas.Invalidate();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
            canvas.Cursor = CanPan ? Cursors.Hand : Cursors.Default;
        }

        // ------------------------------------------------------------------
        // Zoom (mouse wheel) -- keeps the point under the cursor stationary
        // while zooming, like most image editors. Free to go smaller than
        // the auto-Fill zoom (revealing transparent margin the button's own
        // background shows through) or larger than auto-Fit.
        // ------------------------------------------------------------------

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_source == null) return;

            float oldZoom = _zoom;
            float factor = e.Delta > 0 ? 1.1f : 1f / 1.1f;

            // Bounds scale with the image's own fit/fill zoom, not a fixed
            // number -- a large source photo can have a natural fit zoom
            // well under a fixed floor, and a fixed floor above that would
            // clamp every zoom-out right back UP past where you started
            // (jumping bigger when scrolling "down", and getting stuck
            // there since every further scroll re-clamps to the same floor).
            float zx = (float)OutputW / _source.Width;
            float zy = (float)OutputH / _source.Height;
            float fitZoom = Math.Min(zx, zy);
            float fillZoom = Math.Max(zx, zy);
            float minZoom = fitZoom * 0.25f;
            float maxZoom = fillZoom * 8f;
            float newZoom = Math.Max(minZoom, Math.Min(maxZoom, oldZoom * factor));
            if (Math.Abs(newZoom - oldZoom) < 0.0001f) return;

            // Point under the cursor, in source-image pixel space.
            float cursorSrcX = (e.X - _offsetX) / oldZoom;
            float cursorSrcY = (e.Y - _offsetY) / oldZoom;

            _zoom = newZoom;
            ApplyZoom();

            _offsetX = ClampOffsetX((int)Math.Round(e.X - cursorSrcX * _zoom));
            _offsetY = ClampOffsetY((int)Math.Round(e.Y - cursorSrcY * _zoom));

            canvas.Cursor = CanPan ? Cursors.Hand : Cursors.Default;
            canvas.Invalidate();
        }

        // ------------------------------------------------------------------
        // Arrow-key nudge -- intercepted at the form level (ProcessCmdKey)
        // rather than a control KeyDown handler, because rbFill/rbFit are
        // RadioButtons and would otherwise eat arrow keys themselves to
        // switch Fill/Fit selection whenever one of them has focus. Plain
        // arrow = 1px, Shift+arrow = 5px for faster large moves.
        // ------------------------------------------------------------------

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_source != null)
            {
                int step = (keyData & Keys.Shift) == Keys.Shift ? 5 : 1;
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.Left:  NudgeOffset(-step, 0); return true;
                    case Keys.Right: NudgeOffset(step, 0); return true;
                    case Keys.Up:    NudgeOffset(0, -step); return true;
                    case Keys.Down:  NudgeOffset(0, step); return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void NudgeOffset(int dx, int dy)
        {
            _offsetX = ClampOffsetX(_offsetX + dx);
            _offsetY = ClampOffsetY(_offsetY + dy);
            canvas.Invalidate();
        }

        // Bigger-than-canvas: offset ranges over [OutputW-scaledW, 0], i.e.
        // the image can slide but must always fully cover the canvas.
        // Smaller-than-canvas: OutputW-scaledW is positive, so the range
        // flips to [0, OutputW-scaledW] -- the image can slide from flush
        // left/top to flush right/bottom while staying fully inside the
        // canvas the whole time. Either way it's just clamping between 0
        // and (OutputW - scaledW) regardless of which one is bigger.
        private int ClampOffsetX(int x) =>
            Math.Max(Math.Min(0, OutputW - _scaledW), Math.Min(Math.Max(0, OutputW - _scaledW), x));
        private int ClampOffsetY(int y) =>
            Math.Max(Math.Min(0, OutputH - _scaledH), Math.Min(Math.Max(0, OutputH - _scaledH), y));

        // ------------------------------------------------------------------
        // Crop to 100x100 bitmap (always starts fully transparent -- covered
        // completely if the zoomed image fills the canvas, otherwise the
        // uncovered margin stays transparent so the firmware button's own
        // background shows through)
        // ------------------------------------------------------------------

        private Bitmap CropToBitmap()
        {
            var output = new Bitmap(OutputW, OutputH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(_source, _offsetX, _offsetY, _scaledW, _scaledH);
            }
            return output;
        }
    }
}
