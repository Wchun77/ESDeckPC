using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// "Side Icon" field for the editors' properties panel: a 1:1 mockup of
    /// the device button (SidebarButtonPreview), the file name, Browse... and
    /// Clear. Replaces the right-click "Set Side Icon" dialog as the main way
    /// to set side_icon / keyboard_icon. A PNG picked outside
    /// assets\side_icons can be copied in, since the device only looks there.
    /// </summary>
    public partial class UC_SideIconField : UserControl
    {
        private string _fileName = "";
        private string _sideIconsDir;
        private string _fallbackText = "";
        private Bitmap _bmp;

        /// <summary>Raised when the user picks or clears the icon.</summary>
        public event EventHandler Changed;

        public string FileName => _fileName;

        public UC_SideIconField()
        {
            InitializeComponent();
            StyleDarkButtons();
            btnBrowse.Click += BtnBrowse_Click;
            btnClear.Click += (s, e) => SetFile("", true);
            picThumb.Paint += (s, e) =>
                SidebarButtonPreview.Draw(e.Graphics, new Rectangle(0, 0, picThumb.Width, picThumb.Height), _bmp, _fallbackText);
        }

        /// <summary>Show this entry's icon. title = group caption ("Side Icon", "Keyboard Icon").</summary>
        public void Bind(string fileName, string sideIconsDir, string fallbackText, string title = "Side Icon")
        {
            grpIcon.Text = title;
            _sideIconsDir = sideIconsDir;
            _fallbackText = fallbackText ?? "";
            SetFile(fileName ?? "", false);
        }

        private void SetFile(string name, bool raise)
        {
            _fileName = name;
            lblName.Text = name.Length > 0 ? name : "(none)";
            _bmp?.Dispose();
            _bmp = null;
            lblWarn.Text = "";

            string path = EspSidebarRenderer.Resolve(_sideIconsDir, name);
            if (path != null)
            {
                if (!File.Exists(path)) lblWarn.Text = "not found in assets\\side_icons";
                else
                {
                    try
                    {
                        using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                        using (var img = Image.FromStream(ms))
                            _bmp = new Bitmap(img);
                        if (_bmp.Width != EspSidebarRenderer.Btn || _bmp.Height != EspSidebarRenderer.Btn)
                            lblWarn.Text = $"{_bmp.Width}x{_bmp.Height} -- the button is 64x64";
                    }
                    catch { lblWarn.Text = "cannot read this image"; }
                }
            }
            picThumb.Invalidate();
            if (raise) Changed?.Invoke(this, EventArgs.Empty);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            string picked;
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select side icon";
                dlg.Filter = "PNG image (*.png)|*.png";
                if (!string.IsNullOrEmpty(_sideIconsDir) && Directory.Exists(_sideIconsDir))
                    dlg.InitialDirectory = _sideIconsDir;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                picked = dlg.FileName;
            }

            string name = Path.GetFileName(picked);
            if (!string.IsNullOrEmpty(_sideIconsDir) && Directory.Exists(_sideIconsDir) &&
                !string.Equals(Path.GetFullPath(Path.GetDirectoryName(picked)).TrimEnd('\\'),
                               Path.GetFullPath(_sideIconsDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                // The device only reads assets\side_icons: offer to copy it there.
                var ask = MessageBox.Show(this, $"Copy {name} into assets\\side_icons?\n\n" +
                    "The device only looks for side icons there.", "Side Icon",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (ask == DialogResult.Cancel) return;
                if (ask == DialogResult.Yes)
                {
                    string dest = Path.Combine(_sideIconsDir, name);
                    if (File.Exists(dest) && MessageBox.Show(this, $"{name} already exists in assets\\side_icons. Replace it?",
                            "Side Icon", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    try { File.Copy(picked, dest, true); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Copy failed:\n{ex.Message}", "Side Icon",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            SetFile(name, true);
        }

        private void StyleDarkButtons()
        {
            var darkBg = Color.FromArgb(55, 55, 58);
            var darkBorder = Color.FromArgb(80, 80, 80);
            var lightFg = Color.FromArgb(220, 220, 220);
            var font = new Font("Consolas", 8.5f);
            foreach (var btn in new[] { btnBrowse, btnClear })
            {
                btn.BackColor = darkBg;
                btn.ForeColor = lightFg;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.Font = font;
            }
        }

        private void DisposeOwnedResources() => _bmp?.Dispose();
    }
}
