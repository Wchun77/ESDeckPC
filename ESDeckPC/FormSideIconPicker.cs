using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Shared modal dialog for picking a sidebar page icon (side_icon).
    /// Used by both the deck editor (FormConfigEditor) and the monitor
    /// editor (FormMonitorEditor, for data pages and the Clock page alike)
    /// -- always the same dialog, so the preview and behavior can't drift
    /// between the two. Shows a 1:1 mockup of the real 64x56 firmware
    /// sidebar button (see SidebarButtonPreview) so what you see here is
    /// what the device will actually show.
    /// PNG only -- the firmware button is a fixed 64x56 and expects a
    /// pre-cropped asset (see FormSideIconImporter).
    /// </summary>
    public partial class FormSideIconPicker : Form
    {
        private readonly string _initialDir;
        private readonly string _fallbackText;
        private Bitmap _iconBitmap = null;

        public string FileName => txtFile.Text.Trim();

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
        }

        public FormSideIconPicker(string title, string currentFileName,
                                   string fallbackText, string initialDir)
        {
            InitializeComponent();

            Text = title;
            _initialDir = initialDir;
            _fallbackText = fallbackText;

            txtFile.Text = currentFileName ?? "";

            if (!string.IsNullOrEmpty(currentFileName) && !string.IsNullOrEmpty(initialDir))
            {
                var path = Path.Combine(initialDir, currentFileName);
                if (File.Exists(path))
                {
                    try { _iconBitmap = new Bitmap(path); }
                    catch { _iconBitmap = null; }
                }
            }

            btnBrowse.Click += BtnBrowse_Click;
            btnClear.Click += BtnClear_Click;
            canvas.Paint += Canvas_Paint;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var bounds = new Rectangle(0, 0, canvas.Width, canvas.Height);
            SidebarButtonPreview.Draw(e.Graphics, bounds, _iconBitmap, _fallbackText);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select side icon";
                ofd.Filter = "PNG image (*.png)|*.png";
                if (!string.IsNullOrEmpty(_initialDir) && Directory.Exists(_initialDir))
                    ofd.InitialDirectory = _initialDir;

                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var loaded = new Bitmap(ofd.FileName);
                    _iconBitmap?.Dispose();
                    _iconBitmap = loaded;
                    txtFile.Text = Path.GetFileName(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                canvas.Invalidate();
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtFile.Text = "";
            _iconBitmap?.Dispose();
            _iconBitmap = null;
            canvas.Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _iconBitmap?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Shows the dialog and returns the picked filename (empty string
        /// if cleared) on OK, or null if the user cancelled.
        /// </summary>
        public static string Show(IWin32Window owner, string title, string currentFileName,
                                   string fallbackText, string initialDir)
        {
            using (var dlg = new FormSideIconPicker(title, currentFileName, fallbackText, initialDir))
            {
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.FileName : null;
            }
        }
    }
}
