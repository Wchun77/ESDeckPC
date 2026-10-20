using System;
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
        private Bitmap _previewBmp = null;

        // True when the Settings entry is selected; false = Touchpad.
        private bool _settingsSelected = false;

        private static readonly Color ColSelected = Color.FromArgb(140, 30, 30); // dark red
        private static readonly Color ColNormal = Color.FromArgb(55, 55, 58);
        private static readonly Color ColMenuBack = Color.FromArgb(45, 45, 48);
        private static readonly Color ColMenuFore = Color.FromArgb(220, 220, 220);

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
            btnTouchpadPage.Click += (s, e) => ShowTouchpadSettings();
            btnSettingsPage.Click += (s, e) => ShowSettingsPage();
            btnSettingsPage.MouseUp += BtnSettingsPage_MouseUp;
            btnTouchpadPage.MouseUp += BtnTouchpadPage_MouseUp;

            ShowTouchpadSettings();
        }

        // ------------------------------------------------------------------
        // Context menu (dark theme, same style as FormMonitorEditor's)
        // ------------------------------------------------------------------

        private ContextMenuStrip BuildDarkMenu()
        {
            return new ContextMenuStrip
            {
                BackColor = ColMenuBack,
                ForeColor = ColMenuFore,
                Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()),
            };
        }

        private sealed class DarkMenuColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(80, 30, 30);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(80, 30, 30);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(80, 30, 30);
            public override Color MenuItemBorder => Color.FromArgb(100, 100, 100);
            public override Color MenuBorder => Color.FromArgb(80, 80, 80);
            public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
        }

        // ------------------------------------------------------------------
        // Settings entry -- side_icon has no staged UI field (same convention
        // as Monitor's Clock/Settings side_icon), set directly from the
        // right-click menu. Touchpad has no side_icon at all in the schema.
        // ------------------------------------------------------------------

        // Touchpad entry -- the sidebar keyboard button's icon, set from the
        // right-click menu the same way Settings' side_icon is.
        private void BtnTouchpadPage_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var cms = BuildDarkMenu();
            var miKbIcon = new ToolStripMenuItem("Set Keyboard Icon");
            miKbIcon.Click += (s, ev) =>
            {
                string name = FormSideIconPicker.Show(this, "Set Keyboard Icon",
                    _cfg.KeyboardIcon ?? "", "KB", _assetsSideIconsDir);
                if (name == null) return;
                _cfg.KeyboardIcon = name;
                if (!_settingsSelected) RefreshPreview();   // updates the status line
            };
            cms.Items.Add(miKbIcon);

            cms.Show(Cursor.Position);
        }

        private void BtnSettingsPage_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var cms = BuildDarkMenu();
            var miSideIcon = new ToolStripMenuItem("Set Side Icon");
            miSideIcon.Click += (s, ev) =>
            {
                string name = FormSideIconPicker.Show(this, "Set Side Icon - Settings",
                    _cfg.Settings.SideIcon ?? "", "Settings", _assetsSideIconsDir);
                if (name == null) return;
                _cfg.Settings.SideIcon = name;
            };
            cms.Items.Add(miSideIcon);

            cms.Show(Cursor.Position);
        }

        // ------------------------------------------------------------------
        // Entry switching
        // ------------------------------------------------------------------

        private void ShowTouchpadSettings()
        {
            _settingsSelected = false;

            if (_ucTouchpadBg == null)
            {
                _ucTouchpadBg = new UC_MediaBgPanel { Dock = DockStyle.Top };
                _ucTouchpadBg.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucTouchpadBg);
            pnlSettingsHost.ResumeLayout();

            _ucTouchpadBg.ApplyConfig(_cfg.BgImage, _assetsBackgroundsDir, "Touchpad Background");

            SetSelectedButton(btnTouchpadPage);
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
            pnlSettingsHost.ResumeLayout();

            _ucSettingsBg.ApplyConfig(_cfg.Settings.BgImage, _assetsBackgroundsDir, "Settings Background");

            SetSelectedButton(btnSettingsPage);
            RefreshPreview();
        }

        private void SetSelectedButton(Button selected)
        {
            btnTouchpadPage.BackColor = (selected == btnTouchpadPage) ? ColSelected : ColNormal;
            btnSettingsPage.BackColor = (selected == btnSettingsPage) ? ColSelected : ColNormal;
        }

        // ------------------------------------------------------------------
        // JSON handlers -- same New/Open/Save/SaveAs shape as
        // FormMediaEditor, just against TouchpadConfig.
        // ------------------------------------------------------------------

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New touchpad JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = "touchpad.json";
                if (dlg.ShowDialog() != DialogResult.OK) return;
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
                if (!string.IsNullOrEmpty(_jsonPath))
                    dlg.InitialDirectory = Path.GetDirectoryName(_jsonPath);
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<TouchpadConfig>(
                                  File.ReadAllText(dlg.FileName)) ?? new TouchpadConfig();
                    if (cfg.Settings == null) cfg.Settings = new TouchpadSettingsCfg();
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
                if (!string.IsNullOrEmpty(_jsonPath))
                    dlg.InitialDirectory = Path.GetDirectoryName(_jsonPath);
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

            Bitmap newBmp;
            if (_settingsSelected)
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
        // Config <-> UI sync
        // ------------------------------------------------------------------

        private void ReadUiIntoConfig()
        {
            if (_ucTouchpadBg != null) _cfg.BgImage = _ucTouchpadBg.ReadBgImage();
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

            foreach (var btn in new[] { btnJsonNew, btnJsonOpen, btnJsonSave, btnJsonSaveAs, btnTouchpadPage, btnSettingsPage })
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
