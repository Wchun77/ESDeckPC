using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Left-panel properties UC for the fixed Settings entry (Monitor editor).
    /// Only a background image -- side_icon is set via btnSettingsPage's own
    /// right-click menu, same as Clock's side_icon, since it doesn't need a
    /// staged text field the way bg_image does. Background group is a direct
    /// copy of UC_PageSettings' grpPageBg (same layout/behavior), just without
    /// the page name field or cell combos that don't apply to Settings.
    /// </summary>
    public partial class UC_SettingsPanel : UserControl
    {
        private Bitmap _bgBitmap = null;
        public Bitmap BgBitmap => _bgBitmap;

        /// <summary>
        /// Raised whenever a change in this control should cause the host
        /// form to re-render the preview.
        /// </summary>
        public event EventHandler PreviewInvalidated;

        // True while ApplyConfig is in progress; see UC_ClockSettings for rationale.
        private bool _isApplying = false;

        public UC_SettingsPanel()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnSettingsBgBrowse.Click += BtnSettingsBgBrowse_Click;
            btnSettingsBgClear.Click += BtnSettingsBgClear_Click;
        }

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebinds this control to the given Settings config, and tries to
        /// resolve+load the background image from the given USB
        /// assets/backgrounds folder using the filename already stored in
        /// the JSON. Pass null to skip resolution (filename still shown as text).
        /// </summary>
        public void ApplyConfig(MonitorSettingsCfg cfg, string backgroundsDir)
        {
            _isApplying = true;
            try
            {
                txtSettingsBgImage.Text = cfg.BgImage ?? "";

                _bgBitmap?.Dispose();
                _bgBitmap = null;
                if (!string.IsNullOrEmpty(backgroundsDir) && !string.IsNullOrEmpty(cfg.BgImage))
                {
                    var path = Path.Combine(backgroundsDir, cfg.BgImage);
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

        public void ReadConfig(MonitorSettingsCfg cfg)
        {
            cfg.BgImage = txtSettingsBgImage.Text;
        }

        // ------------------------------------------------------------------
        // Background image
        // ------------------------------------------------------------------

        private void BtnSettingsBgBrowse_Click(object sender, EventArgs e)
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
                    txtSettingsBgImage.Text = Path.GetFileName(dlg.FileName);
                    RaisePreviewInvalidated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSettingsBgClear_Click(object sender, EventArgs e)
        {
            _bgBitmap?.Dispose();
            _bgBitmap = null;
            txtSettingsBgImage.Text = "";
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

            foreach (var btn in new[] { btnSettingsBgBrowse, btnSettingsBgClear })
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
        /// Releases bitmap resources owned by this control.
        /// Called from the Designer-generated Dispose(bool) override,
        /// same pattern as UC_PageSettings.DisposeOwnedResources().
        /// </summary>
        private void DisposeOwnedResources()
        {
            _bgBitmap?.Dispose();
        }
    }
}
