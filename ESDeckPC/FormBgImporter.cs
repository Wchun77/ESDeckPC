using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormBgImporter : Form
    {
        // ------------------------------------------------------------------
        // Constants
        // ------------------------------------------------------------------

        private const int OutputW = 720;
        private const int OutputH = 480;
        private const int JpgQuality = 90;

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

        // Scaled bitmap dimensions (zoom-fill, always >= OutputW x OutputH)
        private int _scaledW, _scaledH;

        // Current top-left offset of the scaled image relative to the canvas
        // (always <= 0 on both axes)
        private int _offsetX, _offsetY;

        // Pan drag state
        private bool _dragging = false;
        private Point _dragStart;
        private int _offsetXAtDrag, _offsetYAtDrag;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormBgImporter()
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
                dlg.Title = "Select image";
                dlg.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    // Load into a memory copy immediately so the original file
                    // is not locked. This allows saving back to the same path.
                    Bitmap loaded = new Bitmap(dlg.FileName);
                    _source?.Dispose();
                    _source = new Bitmap(loaded);
                    loaded.Dispose();
                    _sourceFileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                    lblFile.Text = Path.GetFileName(dlg.FileName);
                    lblFile.ForeColor = Color.FromArgb(180, 180, 180);

                    CalcZoomFill();
                    CenterOffset();
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
        // Save
        // ------------------------------------------------------------------

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_source == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save background JPG";
                dlg.Filter = "JPEG image (*.jpg)|*.jpg";
                dlg.FileName = _sourceFileName + ".jpg";

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var output = CropToBitmap())
                    {
                        SaveJpg(output, dlg.FileName, JpgQuality);
                    }

                    MessageBox.Show($"Saved: {Path.GetFileName(dlg.FileName)}",
                        "Background Importer", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Rule-of-thirds grid (dashed lines)
            using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
            {
                pen.DashStyle = DashStyle.Dash;
                int x1 = OutputW / 3;
                int x2 = OutputW * 2 / 3;
                int y1 = OutputH / 3;
                int y2 = OutputH * 2 / 3;
                g.DrawLine(pen, x1, 0, x1, OutputH);
                g.DrawLine(pen, x2, 0, x2, OutputH);
                g.DrawLine(pen, 0, y1, OutputW, y1);
                g.DrawLine(pen, 0, y2, OutputW, y2);
            }

            // Crop boundary
            using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1))
                g.DrawRectangle(pen, 0, 0, OutputW - 1, OutputH - 1);
        }

        // ------------------------------------------------------------------
        // Pan
        // ------------------------------------------------------------------

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _source == null) return;
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
            canvas.Cursor = Cursors.Hand;
        }

        // ------------------------------------------------------------------
        // Zoom-fill calculation
        // ------------------------------------------------------------------

        private void CalcZoomFill()
        {
            float zx = (float)OutputW / _source.Width;
            float zy = (float)OutputH / _source.Height;
            float z = Math.Max(zx, zy);
            _scaledW = (int)Math.Ceiling(_source.Width * z);
            _scaledH = (int)Math.Ceiling(_source.Height * z);
        }

        private void CenterOffset()
        {
            _offsetX = (OutputW - _scaledW) / 2;
            _offsetY = (OutputH - _scaledH) / 2;
        }

        private int ClampOffsetX(int x) => Math.Max(OutputW - _scaledW, Math.Min(0, x));
        private int ClampOffsetY(int y) => Math.Max(OutputH - _scaledH, Math.Min(0, y));

        // ------------------------------------------------------------------
        // Crop to 720x480 bitmap
        // ------------------------------------------------------------------

        private Bitmap CropToBitmap()
        {
            var output = new Bitmap(OutputW, OutputH);
            using (var g = Graphics.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Black);
                g.DrawImage(_source, _offsetX, _offsetY, _scaledW, _scaledH);
            }
            return output;
        }

        // ------------------------------------------------------------------
        // JPG export
        // ------------------------------------------------------------------

        private static void SaveJpg(Bitmap bmp, string path, int quality)
        {
            var encoder = GetJpgEncoder();
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
            bmp.Save(path, encoder, encoderParams);
        }

        private static ImageCodecInfo GetJpgEncoder()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.MimeType == "image/jpeg")
                    return codec;
            throw new Exception("JPEG encoder not found.");
        }
    }
}