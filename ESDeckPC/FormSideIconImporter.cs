using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormSideIconImporter : Form
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        // Matches the monitor sidebar button size in firmware
        // (main/ui/ui_monitor.c: lv_obj_set_size(btn, 64, 56)).
        private const int OutputW = 64;
        private const int OutputH = 56;

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

        // Scaled bitmap dimensions in logical (non-DisplayScale) pixels.
        // Fill mode: always >= OutputW x OutputH (zoom-fill, cropped by panning).
        // Fit mode: always <= OutputW x OutputH (whole image visible, letterboxed).
        private int _scaledW, _scaledH;

        // Current top-left offset of the scaled image relative to the output
        // canvas, in logical pixels. Fill mode: always <= 0 on both axes
        // (panning crops the overflow). Fit mode: always >= 0 (centered,
        // clamped to the same position regardless of drag -- see
        // ClampOffsetX/Y, there is no overflow to pan through).
        private int _offsetX, _offsetY;

        // Pan drag state (Fill mode only)
        private bool _dragging = false;
        private Point _dragStart;
        private int _offsetXAtDrag, _offsetYAtDrag;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormSideIconImporter()
        {
            InitializeComponent();
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
            canvas.Cursor = rbFill.Checked ? Cursors.Hand : Cursors.Default;
            lblHint.Text = rbFill.Checked
                ? "Drag to pan (Fill mode)\r\nOutput: 64 x 56 PNG"
                : "Whole image shown, letterboxed\r\nOutput: 64 x 56 PNG";

            if (_source == null) return;
            RecomputeLayout();
            canvas.Invalidate();
        }

        private void RecomputeLayout()
        {
            if (rbFill.Checked)
            {
                // Zoom-fill: scale so the image fully covers the output
                // canvas, overflow on one axis gets cropped by panning.
                float zx = (float)OutputW / _source.Width;
                float zy = (float)OutputH / _source.Height;
                float z = Math.Max(zx, zy);
                _scaledW = (int)Math.Ceiling(_source.Width * z);
                _scaledH = (int)Math.Ceiling(_source.Height * z);
            }
            else
            {
                // Fit: scale so the whole image is visible, no cropping.
                // Whatever doesn't cover the canvas stays transparent, letting
                // the button's own background color show through in firmware.
                float zx = (float)OutputW / _source.Width;
                float zy = (float)OutputH / _source.Height;
                float z = Math.Min(zx, zy);
                _scaledW = Math.Max(1, (int)Math.Round(_source.Width * z));
                _scaledH = Math.Max(1, (int)Math.Round(_source.Height * z));
            }

            _offsetX = (OutputW - _scaledW) / 2;
            _offsetY = (OutputH - _scaledH) / 2;
        }

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_source == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save sidebar icon PNG";
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
                        "Sidebar Icon Importer", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Output boundary -- the actual 64x56 asset edge, in both modes.
            using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1))
                g.DrawRectangle(pen, 0, 0, OutputW - 1, OutputH - 1);
        }

        // ------------------------------------------------------------------
        // Pan (Fill mode only -- Fit mode's clamp always resolves back to the
        // centered offset since there is no overflow to pan through, see
        // ClampOffsetX/Y, but the drag is still gated here for a clean cursor)
        // ------------------------------------------------------------------

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _source == null || !rbFill.Checked) return;
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
            canvas.Cursor = rbFill.Checked ? Cursors.Hand : Cursors.Default;
        }

        private int ClampOffsetX(int x) => Math.Max(OutputW - _scaledW, Math.Min(0, x));
        private int ClampOffsetY(int y) => Math.Max(OutputH - _scaledH, Math.Min(0, y));

        // ------------------------------------------------------------------
        // Crop to 64x56 bitmap (always starts fully transparent -- Fill mode
        // ends up covering every pixel anyway, Fit mode keeps its letterbox
        // transparent so the firmware button's own background shows through)
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
