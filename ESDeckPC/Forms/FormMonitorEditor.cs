using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ESDeckPC.Properties;
using Newtonsoft.Json;

namespace ESDeckPC
{
    public partial class FormMonitorEditor : Form
    {
        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private string _jsonPath = null;
        private MonitorConfig _cfg = new MonitorConfig();

        // Resolved from _jsonPath when it matches "...\config\monitor\xxx.json"
        // (USB layout convention). Null when the path doesn't match, in which
        // case preview-from-filename is simply skipped (browse-to-load still works).
        private string _assetsBackgroundsDir = null;
        private string _assetsFontsDir = null;
        private string _assetsSideIconsDir = null;

        private UC_MonitorClockSettings _ucClock = null;
        private UC_MonitorPageSettings _ucPage = null;
        private UC_MonitorSettingsPanel _ucSettings = null;
        private Bitmap _previewBmp = null;

        // The data page currently selected in the sidebar, or null when Clock
        // or Settings is selected (_settingsSelected distinguishes the two).
        private MonitorPageCfg _selectedPage = null;

        // True when the fixed Settings entry (the gear) is selected.
        private bool _settingsSelected = false;

        // "Side Icon" field shown above whichever entry's settings panel is
        // up -- writes the selected entry's side_icon (Clock / page / gear).
        private UC_SideIconField _ucSideIcon = null;

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

        public FormMonitorEditor()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnJsonSave.Click += BtnJsonSave_Click;
            btnJsonSaveAs.Click += BtnJsonSaveAs_Click;

            // The sidebar is the page list: Clock first (fixed), then the data
            // pages, then the gear for Settings.
            sideView.FixedLeading = 1;
            sideView.ItemClick += SideView_ItemClick;
            sideView.ItemMenu += SideView_ItemMenu;
            sideView.ItemMoved += SideView_ItemMoved;
            sideView.AddClick += (s, e) => AddNewPage();

            _ucSideIcon = new UC_SideIconField { Dock = DockStyle.Top };
            _ucSideIcon.Changed += SideIcon_Changed;

            ShowClockSettings();
        }

        // ------------------------------------------------------------------
        // Sidebar (sideView): the ESP's sidebar as the page list.
        //   click        Clock / page / gear -> its settings (the page name
        //                is edited there)
        //   drag         reorder pages (Clock stays first)
        //   "+"          add a page (until MonitorPageLimits.MaxPages)
        //   right click  Delete / Add Page
        // ------------------------------------------------------------------

        private void RefreshSidebar()
        {
            var items = new List<SidebarItem>
            {
                new SidebarItem(EspSidebarRenderer.Resolve(_assetsSideIconsDir, _cfg.Clock.SideIcon), "Clock"),
            };
            foreach (var p in _cfg.Pages)
                items.Add(new SidebarItem(EspSidebarRenderer.Resolve(_assetsSideIconsDir, p.SideIcon), p.Name));
            int sel = _settingsSelected ? -1 : _selectedPage == null ? 0 : _cfg.Pages.IndexOf(_selectedPage) + 1;
            sideView.AllowAdd = _cfg.Pages.Count < MonitorPageLimits.MaxPages;
            sideView.SetItems(items, EspSidebarRenderer.Resolve(_assetsSideIconsDir, _cfg.Settings.SideIcon),
                              sel, _settingsSelected);
        }

        private void SideView_ItemClick(object sender, int idx)
        {
            if (idx == EspSidebarRenderer.HitGear) ShowSettingsPage();
            else if (idx == 0) ShowClockSettings();
            else ShowPageSettings(_cfg.Pages[idx - 1]);
        }

        private void SideView_ItemMenu(object sender, SidebarMenuEventArgs e)
        {
            var cms = BuildDarkMenu();
            if (e.Index >= 1)
            {
                var page = _cfg.Pages[e.Index - 1];
                var miDelete = new ToolStripMenuItem("Delete");
                miDelete.Click += (s, ev) => DeletePage(page);
                cms.Items.Add(miDelete);
                cms.Items.Add(new ToolStripSeparator());
            }
            var miAdd = new ToolStripMenuItem("Add Page") { Enabled = _cfg.Pages.Count < MonitorPageLimits.MaxPages };
            miAdd.Click += (s, ev) => AddNewPage();
            cms.Items.Add(miAdd);
            cms.Show(e.ScreenPoint);
        }

        // sidebar indices: 0 = Clock, page i = i + 1
        private void SideView_ItemMoved(object sender, SidebarMoveEventArgs e)
        {
            var page = _cfg.Pages[e.From - 1];
            _cfg.Pages.RemoveAt(e.From - 1);
            _cfg.Pages.Insert(e.To - 1, page);
            RefreshSidebar();
        }

        private void SideIcon_Changed(object sender, EventArgs e)
        {
            string name = _ucSideIcon.FileName;
            if (_settingsSelected) _cfg.Settings.SideIcon = name;
            else if (_selectedPage != null) _selectedPage.SideIcon = name;
            else _cfg.Clock.SideIcon = name;
            RefreshSidebar();
        }

        /// <summary>Show uc in the properties panel with the Side Icon field above it.</summary>
        private void HostPanel(Control uc, string sideIcon, string fallbackText)
        {
            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(uc);
            pnlSettingsHost.Controls.Add(_ucSideIcon);   // added last = docked first = on top
            pnlSettingsHost.ResumeLayout();
            _ucSideIcon.Bind(sideIcon, _assetsSideIconsDir, fallbackText);
        }

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
        // Add / Delete page
        // ------------------------------------------------------------------

        private void AddNewPage()
        {
            if (_cfg.Pages.Count >= MonitorPageLimits.MaxPages)
            {
                MessageBox.Show(this, $"Maximum of {MonitorPageLimits.MaxPages} pages reached.",
                               "Limit reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = FormPageNamePrompt.Show(this, "New page", $"Page {_cfg.Pages.Count + 1}");
            if (name == null) return; // cancelled

            var page = new MonitorPageCfg { Name = name };
            _cfg.Pages.Add(page);
            ShowPageSettings(page);
        }

        private void DeletePage(MonitorPageCfg page)
        {
            var result = MessageBox.Show(this, $"Delete page \"{page.Name}\"?", "Confirm delete",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            _cfg.Pages.Remove(page);
            if (_selectedPage == page)
            {
                _selectedPage = null;   // don't let ReadUiIntoConfig write the panel into the deleted page
                ShowClockSettings();    // fall back to Clock if the deleted page was selected
            }
            else
                RefreshSidebar();
        }

        // ------------------------------------------------------------------
        // Entry switching -- Clock / data page / Settings, each with its own
        // settings panel and the Side Icon field above it.
        // ------------------------------------------------------------------

        private void ShowClockSettings()
        {
            _selectedPage = null;
            _settingsSelected = false;

            if (_ucClock == null)
            {
                _ucClock = new UC_MonitorClockSettings { Dock = DockStyle.Top };
                _ucClock.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            HostPanel(_ucClock, _cfg.Clock.SideIcon, "Clock");

            _ucClock.ApplyConfig(_cfg.Clock, _assetsBackgroundsDir, _assetsFontsDir);

            SetStatus("");
            RefreshPreview();
        }

        private void ShowPageSettings(MonitorPageCfg page)
        {
            _selectedPage = page;
            _settingsSelected = false;

            if (_ucPage == null)
            {
                _ucPage = new UC_MonitorPageSettings { Dock = DockStyle.Top };
                _ucPage.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            HostPanel(_ucPage, page.SideIcon, page.Name);

            _ucPage.ApplyConfig(page, _assetsBackgroundsDir);

            SetStatus("");
            RefreshPreview();
        }

        private void ShowSettingsPage()
        {
            _selectedPage = null;
            _settingsSelected = true;

            if (_ucSettings == null)
            {
                _ucSettings = new UC_MonitorSettingsPanel { Dock = DockStyle.Top };
                _ucSettings.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            HostPanel(_ucSettings, _cfg.Settings.SideIcon, "Settings");

            _ucSettings.ApplyConfig(_cfg.Settings, _assetsBackgroundsDir);

            SetStatus("Settings");
            RefreshPreview();
        }

        // ------------------------------------------------------------------
        // JSON handlers
        // ------------------------------------------------------------------

        // File dialogs start in the current file's folder, else in the
        // ESDeck SD card's config\monitor (Storage Mode), else Windows' default.
        private string DialogDir() =>
            !string.IsNullOrEmpty(_jsonPath) ? Path.GetDirectoryName(_jsonPath)
                                              : EsDeckDrive.Dir("config", "monitor");

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New monitor JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = "monitor.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _cfg = new MonitorConfig();
                _jsonPath = dlg.FileName;
                _assetsBackgroundsDir = null;
                _assetsFontsDir = null;
                _assetsSideIconsDir = null;
                ShowClockSettings();
                UpdateJsonPathLabel();
                RefreshPreview();
            }
        }

        private void BtnJsonOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open monitor JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<MonitorConfig>(
                                  File.ReadAllText(dlg.FileName)) ?? new MonitorConfig();
                    _cfg = cfg;
                    _jsonPath = dlg.FileName;
                    ResolveAssetDirsFromJsonPath();
                        ShowClockSettings();
                    UpdateJsonPathLabel();
                    RefreshPreview();
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

        /// <summary>
        /// Shows the SaveFileDialog and updates _jsonPath on confirmation.
        /// Returns false if the user cancelled.
        /// </summary>
        private bool PromptForSavePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save monitor JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = !string.IsNullOrEmpty(_jsonPath)
                    ? Path.GetFileName(_jsonPath)
                    : "monitor.json";
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
                "Monitor Editor",
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
                if (_ucSettings == null) return; // nothing to render yet
                newBmp = MonitorPageRenderer.RenderBackgroundOnly(_ucSettings.BgBitmap);
                SetStatus("Settings");
            }
            else if (_selectedPage != null)
            {
                // Data page selected: render the 2x2 cell grid layout.
                newBmp = MonitorPageRenderer.Render(_selectedPage, _ucPage?.BgBitmap);
                SetStatus($"Page: {_selectedPage.Name}");
            }
            else
            {
                if (_ucClock == null) return; // nothing to render yet

                newBmp = MonitorClockRenderer.Render(
                    _cfg.Clock, _ucClock.FontTime, _ucClock.FontSec, _ucClock.FontDate, _ucClock.BgBitmap);

                var ft = _ucClock.FontTime;
                var fs = _ucClock.FontSec;
                var fd = _ucClock.FontDate;
                string t = ft != null ? $"time lineH={ft.LineHeight} {ft.DebugInfo()}" : "time=no font";
                string s = fs != null ? $"sec lineH={fs.LineHeight} {fs.DebugInfo()}" : "sec=no font";
                string d = fd != null ? $"date lineH={fd.LineHeight} {fd.DebugInfo()}" : "date=no font";
                SetStatus($"{t}  |  {s}  |  {d}");
            }

            picPreview.Image = null;  // detach before dispose
            _previewBmp?.Dispose();
            _previewBmp = newBmp;
            picPreview.Image = _previewBmp;
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        private void ApplyConfigToUi(MonitorConfig cfg)
        {
            _ucClock?.ApplyConfig(cfg.Clock);
            _ucSettings?.ApplyConfig(cfg.Settings, _assetsBackgroundsDir);
            if (_ucPage != null && _selectedPage != null)
                _ucPage.ApplyConfig(_selectedPage);
        }

        private void ReadUiIntoConfig()
        {
            _ucClock?.ReadConfig(_cfg.Clock);
            _ucSettings?.ReadConfig(_cfg.Settings);
            if (_ucPage != null && _selectedPage != null)
                _ucPage.ReadConfig(_selectedPage);
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
                ? $"Monitor Editor - {Path.GetFileName(_jsonPath)}"
                : "Monitor Editor";
        }

        /// <summary>
        /// USB layout convention: {root}\config\monitor\xxx.json alongside
        /// {root}\assets\backgrounds\ and {root}\assets\fonts\bin\clock\. The
        /// fonts folder now splits by mechanism, not by feature -- .bin
        /// (pre-rasterized, what the clock uses) vs .ttf (raw outline font,
        /// rasterized on demand on the device for CJK notification text) are
        /// two different loading paths, so they live in separate subtrees;
        /// "bin" additionally has a per-feature leaf ("clock" here) since a
        /// .bin is baked for one specific pixel size/feature and isn't
        /// reusable the way a .ttf is. Matches SD_DIR_ASSETS_FONTS_BIN_CLOCK
        /// in the firmware's app_config.h. If the current _jsonPath matches
        /// this layout, resolves the asset folders; otherwise all are set to
        /// null and filename-only preview (existing JSON's bg_image/font_*
        /// fields) is simply skipped. Does not affect Browse-to-load, which
        /// always works regardless.
        /// </summary>
        private void ResolveAssetDirsFromJsonPath()
        {
            _assetsBackgroundsDir = null;
            _assetsFontsDir = null;
            _assetsSideIconsDir = null;

            if (string.IsNullOrEmpty(_jsonPath)) return;

            // .../config/monitor/xxx.json -> monitorDir -> configDir -> root
            var monitorDir = Path.GetDirectoryName(_jsonPath);
            if (string.IsNullOrEmpty(monitorDir)) return;
            if (!string.Equals(Path.GetFileName(monitorDir), "monitor", StringComparison.OrdinalIgnoreCase))
                return;

            var configDir = Path.GetDirectoryName(monitorDir);
            if (string.IsNullOrEmpty(configDir)) return;
            if (!string.Equals(Path.GetFileName(configDir), "config", StringComparison.OrdinalIgnoreCase))
                return;

            var root = Path.GetDirectoryName(configDir);
            if (string.IsNullOrEmpty(root)) return;

            _assetsBackgroundsDir = Path.Combine(root, "assets", "backgrounds");
            _assetsFontsDir = Path.Combine(root, "assets", "fonts", "bin", "clock");
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