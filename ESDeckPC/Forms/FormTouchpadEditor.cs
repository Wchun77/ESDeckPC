using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// Touchpad config editor -- same layout/flow as FormMediaEditor:
    /// New/Open/Save/SaveAs a config/touchpad/*.json file with two fixed
    /// entries: Touchpad (top-level bg_image) and Settings
    /// (settings.bg_image/side_icon). The keyboard button icon is not
    /// editable yet (fixed glyph on the device).
    /// </summary>
    public partial class FormTouchpadEditor : Form
    {
        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private string _jsonPath = null;
        private TouchpadConfig _cfg = new TouchpadConfig();

        // Resolved from _jsonPath when it matches "...\config\touchpad\xxx.json"
        // (USB layout convention). Null when the path doesn't match, in which
        // case preview-from-filename is simply skipped (browse-to-load still works).
        private string _assetsBackgroundsDir = null;
        private string _assetsSideIconsDir = null;

        // Same UC_MediaBgPanel class hosted twice -- Touchpad and Settings are
        // both "just a bg_image", so one control type covers both entries.
        private UC_MediaBgPanel _ucTouchpadBg = null;
        private UC_MediaBgPanel _ucSettingsBg = null;
        private UC_TouchpadSidebarPanel _ucSidebar = null;
        private Bitmap _previewBmp = null;

        // True when the Settings entry is selected; false = Touchpad.
        private bool _settingsSelected = false;
        private bool _sidebarSelected = false;

        // Icon field shown above the Touchpad (keyboard_icon) or Settings
        // (side_icon) panel; _iconForSettings says which one it writes.
        private UC_SideIconField _ucIcon = null;
        private bool _iconForSettings = false;

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

        public FormTouchpadEditor()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnJsonSave.Click += BtnJsonSave_Click;
            btnJsonSaveAs.Click += BtnJsonSaveAs_Click;
            picSide.MouseUp += PicSide_MouseUp;

            _ucIcon = new UC_SideIconField { Dock = DockStyle.Top };
            _ucIcon.Changed += (s, e) =>
            {
                if (_iconForSettings) _cfg.Settings.SideIcon = _ucIcon.FileName;
                else _cfg.KeyboardIcon = _ucIcon.FileName;
                RefreshPreview();   // status line / sidebar preview
            };

            ShowTouchpadSettings();
        }

        // ------------------------------------------------------------------
        // Entry switching
        // ------------------------------------------------------------------

        private void ShowTouchpadSettings()
        {
            _settingsSelected = false;
            _sidebarSelected = false;

            if (_ucTouchpadBg == null)
            {
                _ucTouchpadBg = new UC_MediaBgPanel { Dock = DockStyle.Top };
                _ucTouchpadBg.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucTouchpadBg);
            pnlSettingsHost.Controls.Add(_ucIcon);   // added last = docked first = on top
            pnlSettingsHost.ResumeLayout();
            _iconForSettings = false;
            _ucIcon.Bind(_cfg.KeyboardIcon, _assetsSideIconsDir, "KB", "Keyboard Icon");

            _ucTouchpadBg.ApplyConfig(_cfg.BgImage, _assetsBackgroundsDir, "Touchpad Background");

            RefreshPreview();
        }

        private void ShowSettingsPage()
        {
            _settingsSelected = true;
            _sidebarSelected = false;

            if (_ucSettingsBg == null)
            {
                _ucSettingsBg = new UC_MediaBgPanel { Dock = DockStyle.Top };
                _ucSettingsBg.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucSettingsBg);
            pnlSettingsHost.Controls.Add(_ucIcon);   // added last = docked first = on top
            pnlSettingsHost.ResumeLayout();
            _iconForSettings = true;
            _ucIcon.Bind(_cfg.Settings.SideIcon, _assetsSideIconsDir, "Settings");

            _ucSettingsBg.ApplyConfig(_cfg.Settings.BgImage, _assetsBackgroundsDir, "Settings Background");

            RefreshPreview();
        }

        private void ShowSidebarPage()
        {
            _settingsSelected = false;
            _sidebarSelected = true;

            if (_ucSidebar == null)
            {
                _ucSidebar = new UC_TouchpadSidebarPanel { Dock = DockStyle.Top };
                _ucSidebar.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucSidebar);
            pnlSettingsHost.ResumeLayout();

            _ucSidebar.ApplyConfig(_cfg, _assetsSideIconsDir);

            RefreshPreview();
        }

        /// <summary>
        /// Drop the entry panels after New/Open: ReadUiIntoConfig() copies
        /// every existing panel back into _cfg, so a panel still holding the
        /// previous file's values would overwrite the freshly loaded config.
        /// They're recreated (and re-bound) the next time their entry is shown.
        /// </summary>
        private void ResetEntryPanels()
        {
            pnlSettingsHost.Controls.Clear();
            _ucTouchpadBg?.Dispose(); _ucTouchpadBg = null;
            _ucSettingsBg?.Dispose(); _ucSettingsBg = null;
            _ucSidebar?.Dispose();    _ucSidebar = null;
        }

        // ------------------------------------------------------------------
        // JSON handlers -- same New/Open/Save/SaveAs shape as
        // FormMediaEditor, just against TouchpadConfig.
        // ------------------------------------------------------------------

        // File dialogs start in the current file's folder, else in the
        // ESDeck SD card's config\touchpad (Storage Mode), else Windows' default.
        private string DialogDir() =>
            !string.IsNullOrEmpty(_jsonPath) ? Path.GetDirectoryName(_jsonPath)
                                              : EsDeckDrive.Dir("config", "touchpad");

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New touchpad JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = "touchpad.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                ResetEntryPanels();
                _cfg = new TouchpadConfig();
                _jsonPath = dlg.FileName;
                ResolveAssetDirsFromJsonPath();
                ShowTouchpadSettings();
                UpdateJsonPathLabel();
            }
        }

        private void BtnJsonOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open touchpad JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<TouchpadConfig>(
                                  File.ReadAllText(dlg.FileName)) ?? new TouchpadConfig();
                    if (cfg.Settings == null) cfg.Settings = new TouchpadSettingsCfg();
                    if (cfg.Hotkeys == null) cfg.Hotkeys = new System.Collections.Generic.List<TouchpadHotkey>();
                    ResetEntryPanels();
                    _cfg = cfg;
                    _jsonPath = dlg.FileName;
                    ResolveAssetDirsFromJsonPath();
                    ShowTouchpadSettings();
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
                dlg.Title = "Save touchpad JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = !string.IsNullOrEmpty(_jsonPath)
                    ? Path.GetFileName(_jsonPath)
                    : "touchpad.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return false;

                _jsonPath = dlg.FileName;
                ResolveAssetDirsFromJsonPath();
                UpdateJsonPathLabel();
                return true;
            }
        }

        private void SaveToCurrentPath()
        {
            var confirm = MessageBox.Show(
                $"Save changes to {Path.GetFileName(_jsonPath)}?",
                "Touchpad Editor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string sidebarErr = _ucSidebar?.Validate();
            if (sidebarErr != null)
            {
                MessageBox.Show($"Not saved -- fix the sidebar hotkey first:\n{sidebarErr}",
                                "Touchpad Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
            if (_sidebarSelected)
            {
                // The hotkeys show in picSide; keep the touchpad page in the
                // content area so the whole screen reads as on the device.
                newBmp = TouchpadPreviewRenderer.Render(_ucTouchpadBg?.BgBitmap);

                string err = _ucSidebar?.Validate();
                SetStatus(err != null
                    ? $"Sidebar  |  {err}"
                    : $"Sidebar  |  {_cfg.Hotkeys.Count}/{TouchpadConfig.MaxHotkeys} hotkeys  (shown when the device's Sidebar setting is Hotkeys)");
            }
            else if (_settingsSelected)
            {
                if (_ucSettingsBg == null) return; // nothing to render yet
                // Settings overlay rendering is shared firmware code across
                // every mode (ui_settings.c), so reuse Monitor's
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
                if (_ucTouchpadBg == null) return; // nothing to render yet
                newBmp = TouchpadPreviewRenderer.Render(_ucTouchpadBg.BgBitmap);

                string bgNote = (!string.IsNullOrEmpty(_cfg.BgImage) && _ucTouchpadBg.BgBitmap == null)
                    ? "  [bg image not found on disk]" : "";
                // keyboard_icon has no staged field either (right-click),
                // so show it here like Settings shows its side_icon.
                string kbIcon = string.IsNullOrEmpty(_cfg.KeyboardIcon) ? "(none)" : _cfg.KeyboardIcon;
                SetStatus($"Touchpad  |  keyboard_icon: {kbIcon}{bgNote}");
            }

            picPreview.Image = null; // detach before dispose
            _previewBmp?.Dispose();
            _previewBmp = newBmp;
            picPreview.Image = _previewBmp;
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;

        // ------------------------------------------------------------------
        // Sidebar preview (picSide): the ESP's Touchpad sidebar as it looks
        // with the device's Sidebar setting on Hotkeys -- keyboard button,
        // hotkeys, gear -- also the entry switcher: keyboard -> Touchpad
        // (keyboard icon), hotkey area -> Sidebar (hotkeys and their icons),
        // gear -> Settings (side icon).
        // ------------------------------------------------------------------

        private void RefreshSidebar()
        {
            var hotkeys = new List<SidebarItem>();
            foreach (var hk in _cfg.Hotkeys)
                hotkeys.Add(new SidebarItem(EspSidebarRenderer.Resolve(_assetsSideIconsDir, hk.Icon),
                    !string.IsNullOrEmpty(hk.Label) ? hk.Label : string.Join("+", hk.Keys)));
            var bmp = EspSidebarRenderer.RenderTouchpad(
                EspSidebarRenderer.Resolve(_assetsSideIconsDir, _cfg.KeyboardIcon), hotkeys,
                EspSidebarRenderer.Resolve(_assetsSideIconsDir, _cfg.Settings.SideIcon), _settingsSelected);
            var old = picSide.Image;
            picSide.Image = bmp;
            old?.Dispose();
        }

        private void PicSide_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int hit = EspSidebarRenderer.HitTouchpad(e.Location, _cfg.Hotkeys.Count);
            if (hit == EspSidebarRenderer.HitKeyboard) ShowTouchpadSettings();
            else if (hit == EspSidebarRenderer.HitGear) ShowSettingsPage();
            // anywhere between the keyboard and gear cells, so the Sidebar
            // entry is reachable even with no hotkeys yet
            else if (e.Y >= EspSidebarRenderer.Cell && e.Y < EspSidebarRenderer.ColumnH) ShowSidebarPage();
        }

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        private void ReadUiIntoConfig()
        {
            if (_ucTouchpadBg != null) _cfg.BgImage = _ucTouchpadBg.ReadBgImage();
            if (_ucSettingsBg != null) _cfg.Settings.BgImage = _ucSettingsBg.ReadBgImage();
            if (_ucSidebar != null) _ucSidebar.ReadInto(_cfg);
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
                ? $"Touchpad Editor - {Path.GetFileName(_jsonPath)}"
                : "Touchpad Editor";
        }

        /// <summary>
        /// USB layout convention: {root}\config\touchpad\xxx.json alongside
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

            // .../config/touchpad/xxx.json -> touchpadDir -> configDir -> root
            var touchpadDir = Path.GetDirectoryName(_jsonPath);
            if (string.IsNullOrEmpty(touchpadDir)) return;
            if (!string.Equals(Path.GetFileName(touchpadDir), "touchpad", StringComparison.OrdinalIgnoreCase))
                return;

            var configDir = Path.GetDirectoryName(touchpadDir);
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
