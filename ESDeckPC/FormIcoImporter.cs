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

        private const int MaxSize = 100;

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
        private Bitmap _scaled = null;
        private string _sourceFileName = null;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormIcoImporter()
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
                    Bitmap loaded = new Bitmap(dlg.FileName);
                    _source?.Dispose();
                    _source = new Bitmap(loaded);
                    loaded.Dispose();

                    _sourceFileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                    lblFile.Text = Path.GetFileName(dlg.FileName);
                    lblFile.ForeColor = Color.FromArgb(180, 180, 180);

                    BuildScaled();
                    UpdatePreview();
                    btnSave.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ------------------------------------------------------------------
        // Scale: fit long edge to 100, keep aspect ratio
        // ------------------------------------------------------------------

        private void BuildScaled()
        {
            float scale = Math.Min((float)MaxSize / _source.Width,
                                   (float)MaxSize / _source.Height);
            int w = Math.Max(1, (int)Math.Round(_source.Width * scale));
            int h = Math.Max(1, (int)Math.Round(_source.Height * scale));

            _scaled?.Dispose();
            _scaled = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(_scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.DrawImage(_source, 0, 0, w, h);
            }

            lblSize.Text = $"Output size: {w} x {h} px";
        }

        // ------------------------------------------------------------------
        // Preview
        // ------------------------------------------------------------------

        private void UpdatePreview()
        {
            if (_scaled == null) return;
            var copy = new Bitmap(_scaled);
            picPreview.Image?.Dispose();
            picPreview.Image = copy;
        }

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_scaled == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save icon PNG";
                dlg.Filter = "PNG image (*.png)|*.png";
                dlg.FileName = _sourceFileName + ".png";

                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    _scaled.Save(dlg.FileName, ImageFormat.Png);

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
    }
}