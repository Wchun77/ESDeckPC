using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// Media config editor -- New/Open/Save/SaveAs a config/media/*.json file
    /// (multi-config picker on the device side just scans that folder; there
    /// is no in-editor config list, same as Deck/Monitor's editors) with two
    /// fixed entries: Player (top-level bg_image) and Settings
    /// (settings.bg_image/side_icon). Much smaller than FormMonitorEditor --
    /// no pages/cells/clock, so no flpPages/drag-reorder machinery is needed.
    /// </summary>
    public partial class FormMediaEditor : Form
    {
        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private string _jsonPath = null;
        private MediaConfig _cfg = new MediaConfig();

        // Resolved from _jsonPath when it matches "...\config\media\xxx.json"
        // (USB layout convention). Null when the path doesn't match, in which
        // case preview-from-filename is simply skipped (browse-to-load still works).
        private string _assetsBackgroundsDir = null;
        private string _assetsSideIconsDir = null;

        // Same UC_MediaBgPanel class hosted twice -- Player and Settings are
        // both "just a bg_image", so one control type covers both entries.
        private UC_MediaBgPanel _ucPlayerBg = null;
        private UC_MediaBgPanel _ucSettingsBg = null;
        private Bitmap _previewBmp = null;

        // True when the Settings entry is selected; false = Player.
        private bool _settingsSelected = false;

        // Settings' side_icon field, shown above its background panel.
        private UC_SideIconField _ucSideIcon = null;


        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref v, sizeof(int));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetWindowTheme(pnlSettingsHost.Handle, "DarkMode_Explorer", null);
        }

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormMediaEditor()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnJsonSave.Click += BtnJsonSave_Click;
            btnJsonSaveAs.Click += BtnJsonSaveAs_Click;
            picSide.MouseUp += PicSide_MouseUp;

            _ucSideIcon = new UC_SideIconField { Dock = DockStyle.Top };
            _ucSideIcon.Changed += (s, e) => { _cfg.Settings.SideIcon = _ucSideIcon.FileName; RefreshPreview(); };

            ShowPlayerSettings();
        }

        // ------------------------------------------------------------------
        // Entry switching
        // ------------------------------------------------------------------

        private void ShowPlayerSettings()
        {
            _settingsSelected = false;

            if (_ucPlayerBg == null)
            {
                _ucPlayerBg = new UC_MediaBgPanel { Dock = DockStyle.Top };
                _ucPlayerBg.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucPlayerBg);
            pnlSettingsHost.ResumeLayout();

            _ucPlayerBg.ApplyConfig(_cfg.BgImage, _assetsBackgroundsDir, "Player Background");

            RefreshPreview();
        }

        private void ShowSettingsPage()
        {
            _settingsSelected = true;

            if (_ucSettingsBg == null)
            {
                _ucSettingsBg = new UC_MediaBgPanel { Dock = DockStyle.Top };
                _ucSettingsBg.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucSettingsBg);
            pnlSettingsHost.Controls.Add(_ucSideIcon);   // added last = docked first = on top
            pnlSettingsHost.ResumeLayout();
            _ucSideIcon.Bind(_cfg.Settings.SideIcon, _assetsSideIconsDir, "Settings");

            _ucSettingsBg.ApplyConfig(_cfg.Settings.BgImage, _assetsBackgroundsDir, "Settings Background");

            RefreshPreview();
        }

        // Drop both entry panels before switching to another config: a panel
        // that's still alive keeps the previous file's text, and
        // ReadUiIntoConfig() (first thing RefreshPreview() does) would copy
        // it into the new _cfg -- e.g. file A's Settings bg ending up in B.
        // Same as FormTouchpadEditor.ResetEntryPanels().
        private void ResetEntryPanels()
        {
            pnlSettingsHost.Controls.Clear();
            _ucPlayerBg?.Dispose();   _ucPlayerBg = null;
            _ucSettingsBg?.Dispose(); _ucSettingsBg = null;
        }

        // ------------------------------------------------------------------
        // JSON handlers -- same New/Open/Save/SaveAs shape as
        // FormMonitorEditor, just against MediaConfig.
        // ------------------------------------------------------------------

        // File dialogs start in the current file's folder, else in the
        // ESDeck SD card's config\media (Storage Mode), else Windows' default.
        private string DialogDir() =>
            !string.IsNullOrEmpty(_jsonPath) ? Path.GetDirectoryName(_jsonPath)
                                              : EsDeckDrive.Dir("config", "media");

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New media JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = "media.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                ResetEntryPanels();
                _cfg = new MediaConfig();
                _jsonPath = dlg.FileName;
                _assetsBackgroundsDir = null;
                _assetsSideIconsDir = null;
                ShowPlayerSettings();
                UpdateJsonPathLabel();
            }
        }

        private void BtnJsonOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open media JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<MediaConfig>(
                                  File.ReadAllText(dlg.FileName)) ?? new MediaConfig();
                    if (cfg.Settings == null) cfg.Settings = new MediaSettingsCfg();
                    ResetEntryPanels();
                    _cfg = cfg;
                    _jsonPath = dlg.FileName;
                    ResolveAssetDirsFromJsonPath();
                    ShowPlayerSettings();
                    UpdateJsonPathLabel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open JSON:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnJsonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_jsonPath))
            {
                if (!PromptForSavePath()) return;
            }
            SaveToCurrentPath();
        }

        private void BtnJsonSaveAs_Click(object sender, EventArgs e)
        {
            if (!PromptForSavePath()) return; // always shows the dialog, regardless of _jsonPath
            SaveToCurrentPath();
        }

        private bool PromptForSavePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save media JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = !string.IsNullOrEmpty(_jsonPath)
                    ? Path.GetFileName(_jsonPath)
                    : "media.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return false;

                _jsonPath = dlg.FileName;
                UpdateJsonPathLabel();
                return true;
            }
        }

        private void SaveToCurrentPath()
        {
            var confirm = MessageBox.Show(
                $"Save changes to {Path.GetFileName(_jsonPath)}?",
                "Media Editor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                ReadUiIntoConfig();
                File.WriteAllText(_jsonPath,
                    JsonConvert.SerializeObject(_cfg, Formatting.Indented));
                SetStatus($"Saved: {Path.GetFileName(_jsonPath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}",
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------------
        // Preview
        // ------------------------------------------------------------------

        private void RefreshPreview()
        {
            ReadUiIntoConfig();
            RefreshSidebar();

            Bitmap newBmp;
            if (_settingsSelected)
            {
                if (_ucSettingsBg == null) return; // nothing to render yet
                // Settings overlay rendering is shared firmware code across
                // Deck/Monitor/Media (ui_settings.c), so reuse Monitor's
                // existing background-only renderer rather than duplicating it.
                newBmp = MonitorPageRenderer.RenderBackgroundOnly(_ucSettingsBg.BgBitmap);

                // Side icon has no staged text field (set via right-click,
                // same as Monitor's Clock/Settings), so surface it in the
                // status line -- otherwise there's no way to confirm it was
                // actually read back from the opened JSON.
                string icon = string.IsNullOrEmpty(_cfg.Settings.SideIcon) ? "(none)" : _cfg.Settings.SideIcon;
                string bgNote = (!string.IsNullOrEmpty(_cfg.Settings.BgImage) && _ucSettingsBg.BgBitmap == null)
                    ? "  [bg image not found on disk]" : "";
                SetStatus($"Settings  |  side_icon: {icon}{bgNote}");
            }
            else
            {
                if (_ucPlayerBg == null) return; // nothing to render yet
                newBmp = MediaPreviewRenderer.Render(_ucPlayerBg.BgBitmap);

                string bgNote = (!string.IsNullOrEmpty(_cfg.BgImage) && _ucPlayerBg.BgBitmap == null)
                    ? "  [bg image not found on disk]" : "";
                SetStatus($"Player{bgNote}");
            }

            picPreview.Image = null; // detach before dispose
            _previewBmp?.Dispose();
            _previewBmp = newBmp;
            picPreview.Image = _previewBmp;
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;

        // ------------------------------------------------------------------
        // Sidebar preview (picSide): the ESP's Media sidebar -- level bar
        // (shown at a fixed level) and the gear -- also the entry switcher:
        // click the bar for the Player, the gear for Settings (its side icon
        // is set in the Side Icon field on the left).
        // ------------------------------------------------------------------

        private void RefreshSidebar()
        {
            var bmp = EspSidebarRenderer.RenderMedia(
                EspSidebarRenderer.Resolve(_assetsSideIconsDir, _cfg.Settings.SideIcon), _settingsSelected, 60);
            var old = picSide.Image;
            picSide.Image = bmp;
            old?.Dispose();
        }

        private void PicSide_MouseUp(object sender, MouseEventArgs e)
        {
            int hit = EspSidebarRenderer.HitMedia(e.Location);
            if (e.Button != MouseButtons.Left) return;
            if (hit == EspSidebarRenderer.HitGear) ShowSettingsPage();
            else if (hit == EspSidebarRenderer.HitContent) ShowPlayerSettings();
        }

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        private void ReadUiIntoConfig()
        {
            if (_ucPlayerBg != null) _cfg.BgImage = _ucPlayerBg.ReadBgImage();
            if (_ucSettingsBg != null) _cfg.Settings.BgImage = _ucSettingsBg.ReadBgImage();
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

            foreach (var btn in new[] { btnJsonNew, btnJsonOpen, btnJsonSave, btnJsonSaveAs })
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

        private void UpdateJsonPathLabel()
        {
            lblJsonPath.Text = _jsonPath != null ? Path.GetFileName(_jsonPath) : "(no file)";
            this.Text = _jsonPath != null
                ? $"Media Editor - {Path.GetFileName(_jsonPath)}"
                : "Media Editor";
        }

        /// <summary>
        /// USB layout convention: {root}\config\media\xxx.json alongside
        /// {root}\assets\backgrounds\ and {root}\assets\side_icons\. If the
        /// current _jsonPath matches this layout, resolves the two asset
        /// folders; otherwise both are set to null and filename-only preview
        /// (existing JSON's bg_image/side_icon fields) is simply skipped.
        /// Does not affect Browse-to-load, which always works regardless.
        /// </summary>
        private void ResolveAssetDirsFromJsonPath()
        {
            _assetsBackgroundsDir = null;
            _assetsSideIconsDir = null;

            if (string.IsNullOrEmpty(_jsonPath)) return;

            // .../config/media/xxx.json -> mediaDir -> configDir -> root
            var mediaDir = Path.GetDirectoryName(_jsonPath);
            if (string.IsNullOrEmpty(mediaDir)) return;
            if (!string.Equals(Path.GetFileName(mediaDir), "media", StringComparison.OrdinalIgnoreCase))
                return;

            var configDir = Path.GetDirectoryName(mediaDir);
            if (string.IsNullOrEmpty(configDir)) return;
            if (!string.Equals(Path.GetFileName(configDir), "config", StringComparison.OrdinalIgnoreCase))
                return;

            var root = Path.GetDirectoryName(configDir);
            if (string.IsNullOrEmpty(root)) return;

            _assetsBackgroundsDir = Path.Combine(root, "assets", "backgrounds");
            _assetsSideIconsDir = Path.Combine(root, "assets", "side_icons");
        }

        // ------------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------------

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _previewBmp?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
