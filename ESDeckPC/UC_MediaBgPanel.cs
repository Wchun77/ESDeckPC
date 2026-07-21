using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Generic "just a background image" properties panel, shared by both of
    /// Media's fixed entries (Player's top-level bg_image, Settings' own
    /// settings.bg_image) -- same layout/behavior as Monitor's
    /// UC_SettingsPanel, but takes a plain filename string instead of a typed
    /// cfg object, since Media's two bg_image fields don't share one
    /// settings-shaped struct the way Monitor's Clock/Page/Settings all do.
    /// side_icon (Settings entry only) is set via btnSettingsPage's
    /// right-click menu in FormMediaEditor, same convention Monitor/Clock
    /// uses -- this panel never touches it.
    /// </summary>
    public partial class UC_MediaBgPanel : UserControl
    {
        private Bitmap _bgBitmap = null;
        public Bitmap BgBitmap => _bgBitmap;

        /// <summary>
        /// Raised whenever a change in this control should cause the host
        /// form to re-render the preview.
        /// </summary>
        public event EventHandler PreviewInvalidated;

        // True while ApplyConfig is in progress; suppresses the reentrant
        // PreviewInvalidated that setting txtBgImage.Text would otherwise raise.
        private bool _isApplying = false;

        public UC_MediaBgPanel()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnBgBrowse.Click += BtnBgBrowse_Click;
            btnBgClear.Click += BtnBgClear_Click;
        }

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebinds this control to the given bg_image filename, and tries to
        /// resolve+load it from the given USB assets/backgrounds folder
        /// (pass null to skip resolution -- filename is still shown as text).
        /// groupTitle lets the same control read "Player Background" or
        /// "Settings Background" depending on which entry is selected.
        /// </summary>
        public void ApplyConfig(string bgImage, string backgroundsDir, string groupTitle)
        {
            _isApplying = true;
            try
            {
                grpBg.Text = groupTitle;
                txtBgImage.Text = bgImage ?? "";

                _bgBitmap?.Dispose();
                _bgBitmap = null;
                if (!string.IsNullOrEmpty(backgroundsDir) && !string.IsNullOrEmpty(bgImage))
                {
                    var path = Path.Combine(backgroundsDir, bgImage);
                    if (File.Exists(path))
                    {
                        try { _bgBitmap = new Bitmap(path); }
                        catch { _bgBitmap = null; }
                    }
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        public string ReadBgImage() => txtBgImage.Text;

        // ------------------------------------------------------------------
        // Background image
        // ------------------------------------------------------------------

        private void BtnBgBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select background image";
                dlg.Filter = "JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _bgBitmap?.Dispose();
                    _bgBitmap = new Bitmap(dlg.FileName);
                    txtBgImage.Text = Path.GetFileName(dlg.FileName);
                    RaisePreviewInvalidated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnBgClear_Click(object sender, EventArgs e)
        {
            _bgBitmap?.Dispose();
            _bgBitmap = null;
            txtBgImage.Text = "";
            RaisePreviewInvalidated();
        }

        // ------------------------------------------------------------------
        // Dark styling (cannot go inside InitializeComponent)
        // ------------------------------------------------------------------

        private void StyleDarkButtons()
        {
            var darkBg = Color.FromArgb(55, 55, 58);
            var darkBorder = Color.FromArgb(80, 80, 80);
            var lightFg = Color.FromArgb(220, 220, 220);
            var font = new Font("Consolas", 8.5f);

            foreach (var btn in new[] { btnBgBrowse, btnBgClear })
            {
                btn.BackColor = darkBg;
                btn.ForeColor = lightFg;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.Font = font;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void RaisePreviewInvalidated()
        {
            if (_isApplying) return; // suppress reentrancy during ApplyConfig
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------------

        /// <summary>
        /// Releases bitmap resources owned by this control. Called from the
        /// Designer-generated Dispose(bool) override.
        /// </summary>
        private void DisposeOwnedResources()
        {
            _bgBitmap?.Dispose();
        }
    }
}
