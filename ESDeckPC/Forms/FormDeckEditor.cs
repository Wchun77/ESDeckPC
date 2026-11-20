using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ESDeckPC
{
    public partial class FormDeckEditor : Form
    {
        // The one deck config file being edited (config\deck\<name>.json on
        // the SD card) -- display + actions together, see Deckconfig.cs.
        private DeckConfig _deckConfig = new DeckConfig();
        private string _jsonPath = null;

        private string _assetsBackgroundsDir = null;
        private string _assetsIconsDir = null;
        private string _assetsSideIconsDir = null;

        private DeckPreviewPanel _preview;
        private EditorDirtyTracker _dirty;   // unsaved changes: " *" in the title, prompt before losing them

        // Selected sidebar entry: a page (_pageIdx) or the gear (Settings,
        // not part of _deckConfig.Pages; _pageIdx is -1 then).
        private int _pageIdx = -1;
        private bool _settingsSelected = false;
        private bool _binding = false;   // BindProps() filling the panel: don't write back

        // UI_DECK_CONFIG_MAX_PAGES in ui_deck_config.h
        private const int MaxPages = 255;

        private DeckPage CurrentPage =>
            !_settingsSelected && _pageIdx >= 0 && _pageIdx < _deckConfig.Pages.Count ? _deckConfig.Pages[_pageIdx] : null;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }

        public FormDeckEditor()
        {
            InitializeComponent();

            StyleDarkButtons();

            // Create DeckPreviewPanel and attach to picPreview's location/size
            _preview = new DeckPreviewPanel
            {
                Location = picPreview.Location,
                Size = picPreview.Size,
            };
            picPreview.Visible = false; // keep in Designer, hidden at runtime
            pnlPreview.Controls.Add(_preview);
            _preview.BringToFront();

            _preview.EditButtonRequested += Preview_EditButtonRequested;
            _preview.AddButtonRequested += Preview_AddButtonRequested;
            _preview.ClearButtonRequested += Preview_ClearButtonRequested;
            _preview.ReorderCompleted += (s, e) => UpdatePageList();
            _preview.ButtonClicked += Preview_ButtonClicked;
            _preview.MoveButtonRequested += (s, e) => MoveButton(_pageIdx, e.ButtonIndex, e.TargetPage);
            _preview.DragOutside += Preview_DragOutside;
            _preview.MoveHiddenRequested += (s, target) => MoveHiddenButtons(_pageIdx, target);

            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnJsonSave.Click += BtnSave_Click;
            btnJsonSaveAs.Click += BtnSaveAs_Click;

            // The sidebar is the page list, the left panel the selected entry's properties.
            sideView.ItemClick += SideView_ItemClick;
            sideView.ItemMenu += SideView_ItemMenu;
            sideView.ItemMoved += (s, e) => MovePage(e.From, e.To);
            sideView.AddClick += (s, e) => AddPage();
            txtPageName.TextChanged += TxtPageName_TextChanged;
            ucSideIcon.Changed += UcSideIcon_Changed;
            ucBg.PreviewInvalidated += UcBg_PreviewInvalidated;

            LoadPages();
            UpdateJsonPathLabel();
            ActiveControl = sideView;   // don't start with the caret in a text box

            _dirty = new EditorDirtyTracker(this, () => _deckConfig, TitleText);
            _dirty.MarkSaved();
        }

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

        private void SetStatus(string msg) => lblStatus.Text = msg;

        /// <summary>Status line: what's shown, its button count, and a missing background.</summary>
        private void UpdateStatus()
        {
            var page = CurrentPage;
            string bg = page != null ? page.BgImage : _deckConfig.Settings.BgImage;
            string bgNote = !string.IsNullOrEmpty(bg) && !string.IsNullOrEmpty(_assetsBackgroundsDir) &&
                            !File.Exists(Path.Combine(_assetsBackgroundsDir, bg))
                ? "  [bg image not found on disk]" : "";
            SetStatus(page != null
                ? $"Page: {page.Name}  |  {page.Buttons.Count}/{DeckPreviewPanel.MaxButtons} buttons{bgNote}"
                : $"Settings{bgNote}");
        }


        // ------------------------------------------------------------------
        // Asset path resolution (same rule as MonitorEditor)
        // config\deck\xxx.json -> root -> assets\backgrounds / assets\icons
        // ------------------------------------------------------------------

        private void ResolveAssetDirs()
        {
            _assetsBackgroundsDir = null;
            _assetsIconsDir = null;
            _assetsSideIconsDir = null;

            if (string.IsNullOrEmpty(_jsonPath)) return;

            // Deck JSON lives at {root}\config\deck\xxx.json
            var deckDir = Path.GetDirectoryName(_jsonPath);
            if (string.IsNullOrEmpty(deckDir)) return;
            if (!string.Equals(Path.GetFileName(deckDir), "deck",
                StringComparison.OrdinalIgnoreCase)) return;

            var configDir = Path.GetDirectoryName(deckDir);
            if (string.IsNullOrEmpty(configDir)) return;
            if (!string.Equals(Path.GetFileName(configDir), "config",
                StringComparison.OrdinalIgnoreCase)) return;

            var root = Path.GetDirectoryName(configDir);
            if (string.IsNullOrEmpty(root)) return;

            _assetsBackgroundsDir = Path.Combine(root, "assets", "backgrounds");
            _assetsIconsDir = Path.Combine(root, "assets", "icons");
            _assetsSideIconsDir = Path.Combine(root, "assets", "side_icons");
        }

        // ------------------------------------------------------------------
        // Pages
        // ------------------------------------------------------------------

        private void LoadPages()
        {
            if (_deckConfig.Pages.Count > 0)
                SelectPage(0);
            else
                ShowSettingsPreview();   // nothing else to show; also clears a stale page
        }

        private void SelectPage(int idx)
        {
            if (idx < 0 || idx >= _deckConfig.Pages.Count) return;
            _settingsSelected = false;
            _pageIdx = idx;

            _preview.AllowButtons = true;
            SyncMoveTargets();
            _preview.SetPage(_deckConfig.Pages[idx], _assetsBackgroundsDir, _assetsIconsDir);
            BindProps();
            RefreshSidebar();
        }

        // ------------------------------------------------------------------
        // Moving a button to another page: right-click > Move to Page, or
        // drag it from the preview onto a page in the sidebar. It goes to
        // the end of that page; the current page stays shown.
        // ------------------------------------------------------------------

        private void SyncMoveTargets()
        {
            var names = new List<string>();
            var full = new List<bool>();
            foreach (var pg in _deckConfig.Pages)
            {
                names.Add(pg.Name);
                full.Add(pg.Buttons.Count >= DeckPreviewPanel.MaxButtons);
            }
            _preview.MoveTargetNames = names;
            _preview.MoveTargetFull = full;
            _preview.CurrentPageIndex = _pageIdx;
        }

        private void Preview_DragOutside(object sender, DeckDragOutsideEventArgs e)
        {
            int from = _pageIdx;
            Point pt = sideView.PointToClient(e.ScreenPoint);
            int to = sideView.ClientRectangle.Contains(pt) ? sideView.HitTest(pt) : EspSidebarRenderer.HitNone;
            e.Accept = to >= 0 && to != from && to < _deckConfig.Pages.Count &&
                       _deckConfig.Pages[to].Buttons.Count < DeckPreviewPanel.MaxButtons;
            if (e.Accept && e.Released) MoveButton(from, e.ButtonIndex, to);
        }

        /// <summary>Pages with more buttons than the ESP shows (older configs).</summary>
        private void WarnOverfullPages()
        {
            var lines = new List<string>();
            foreach (var pg in _deckConfig.Pages)
                if (pg.Buttons.Count > DeckPreviewPanel.MaxButtons)
                    lines.Add($"  {pg.Name}: {pg.Buttons.Count} buttons");
            if (lines.Count == 0) return;
            MessageBox.Show(
                $"The ESDeck shows {DeckPreviewPanel.MaxButtons} buttons per page. These pages have more:\n\n" +
                string.Join("\n", lines) +
                "\n\nThe extra buttons are kept in the file but not shown. To move them: open the page, " +
                "right-click the preview > Move Hidden Buttons to Page.",
                "Deck Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Buttons past the 12th on fromPage -> end of toPage, as many as fit.</summary>
        private void MoveHiddenButtons(int fromPage, int toPage)
        {
            if (fromPage < 0 || fromPage >= _deckConfig.Pages.Count) return;
            if (toPage < 0 || toPage >= _deckConfig.Pages.Count || toPage == fromPage) return;
            var src = _deckConfig.Pages[fromPage].Buttons;
            var dst = _deckConfig.Pages[toPage].Buttons;
            int moved = 0;
            while (src.Count > DeckPreviewPanel.MaxButtons && dst.Count < DeckPreviewPanel.MaxButtons)
            {
                dst.Add(src[DeckPreviewPanel.MaxButtons]);
                src.RemoveAt(DeckPreviewPanel.MaxButtons);
                moved++;
            }
            if (src.Count > DeckPreviewPanel.MaxButtons)
                MessageBox.Show($"Moved {moved}; \"{_deckConfig.Pages[toPage].Name}\" is now full. " +
                                $"{src.Count - DeckPreviewPanel.MaxButtons} still hidden -- move them to another page.",
                                "Deck Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _preview.SetPage(_deckConfig.Pages[fromPage], _assetsBackgroundsDir, _assetsIconsDir);
            UpdatePageList();
        }

        private void MoveButton(int fromPage, int btnIdx, int toPage)
        {
            if (fromPage < 0 || fromPage >= _deckConfig.Pages.Count) return;
            if (toPage < 0 || toPage >= _deckConfig.Pages.Count || toPage == fromPage) return;
            if (_deckConfig.Pages[toPage].Buttons.Count >= DeckPreviewPanel.MaxButtons) return;   // full
            var src = _deckConfig.Pages[fromPage];
            if (btnIdx < 0 || btnIdx >= src.Buttons.Count) return;

            var btn = src.Buttons[btnIdx];
            src.Buttons.RemoveAt(btnIdx);
            _deckConfig.Pages[toPage].Buttons.Add(btn);
            _preview.SetPage(src, _assetsBackgroundsDir, _assetsIconsDir);
            UpdatePageList();   // button counts in the list
        }

        // ------------------------------------------------------------------
        // Settings entry (the gear; fixed, not part of _deckConfig.Pages).
        // Preview is background-only: Settings has no buttons in the schema,
        // so AllowButtons is turned off and the wrapper DeckPage we build
        // here always has an empty Buttons list.
        // ------------------------------------------------------------------

        private void ShowSettingsPreview()
        {
            _settingsSelected = true;
            _pageIdx = -1;

            _preview.AllowButtons = false;
            var settingsPage = new DeckPage
            {
                Name = "Settings",
                BgImage = _deckConfig.Settings.BgImage,
                Buttons = new List<DeckButton>(),
            };
            _preview.SetPage(settingsPage, _assetsBackgroundsDir, _assetsIconsDir);
            BindProps();
            RefreshSidebar();
        }

        // ------------------------------------------------------------------
        // Properties panel (left): page name, side icon and background of
        // the selected page, or side icon and background of Settings.
        // ------------------------------------------------------------------

        private void BindProps()
        {
            _binding = true;
            try
            {
                var page = CurrentPage;
                pnlName.Visible = page != null;
                if (page != null)
                {
                    txtPageName.Text = page.Name ?? "";
                    ucSideIcon.Bind(page.SideIcon, _assetsSideIconsDir, page.Name);
                    ucBg.ApplyConfig(page.BgImage, _assetsBackgroundsDir, "Page background");
                }
                else
                {
                    ucSideIcon.Bind(_deckConfig.Settings.SideIcon, _assetsSideIconsDir, "Settings");
                    ucBg.ApplyConfig(_deckConfig.Settings.BgImage, _assetsBackgroundsDir, "Settings background");
                }
            }
            finally { _binding = false; }
        }

        private void TxtPageName_TextChanged(object sender, EventArgs e)
        {
            var page = CurrentPage;
            if (_binding || page == null) return;
            page.Name = txtPageName.Text;
            SyncMoveTargets();   // names in the preview's Move to Page menu
            RefreshSidebar();    // a button without a side icon shows the name
        }

        private void UcSideIcon_Changed(object sender, EventArgs e)
        {
            if (_binding) return;
            var page = CurrentPage;
            if (page != null) page.SideIcon = ucSideIcon.FileName;
            else _deckConfig.Settings.SideIcon = ucSideIcon.FileName;
            RefreshSidebar();
        }

        private void UcBg_PreviewInvalidated(object sender, EventArgs e)
        {
            if (_binding) return;
            var page = CurrentPage;
            if (page != null)
            {
                page.BgImage = ucBg.ReadBgImage();
                _preview.SetPage(page, _assetsBackgroundsDir, _assetsIconsDir);
                UpdateStatus();
            }
            else
            {
                _deckConfig.Settings.BgImage = ucBg.ReadBgImage();
                ShowSettingsPreview();
            }
        }

        // ------------------------------------------------------------------
        // Sidebar (sideView): the ESP's page column and gear as the page list.
        //   click        page / gear -> its preview and properties
        //   drag         reorder pages
        //   "+"          add a page
        //   right click  Delete Page / Add Page
        //   wheel        scroll when there are more pages than fit
        // A button dragged out of the preview onto a sidebar page moves there.
        // ------------------------------------------------------------------

        private void RefreshSidebar()
        {
            var items = _deckConfig.Pages
                .Select(p => new SidebarItem(EspSidebarRenderer.Resolve(_assetsSideIconsDir, p.SideIcon), p.Name))
                .ToList();
            sideView.AllowAdd = _deckConfig.Pages.Count < MaxPages;
            sideView.SetItems(items, EspSidebarRenderer.Resolve(_assetsSideIconsDir, _deckConfig.Settings.SideIcon),
                              _settingsSelected ? -1 : _pageIdx, _settingsSelected);
            UpdateStatus();
        }

        private void SideView_ItemClick(object sender, int idx)
        {
            if (idx == EspSidebarRenderer.HitGear) ShowSettingsPreview();
            else SelectPage(idx);
        }

        private void SideView_ItemMenu(object sender, SidebarMenuEventArgs e)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemAdd = new ToolStripMenuItem("Add Page")
            {
                ForeColor = Color.FromArgb(220, 220, 220),
                Enabled = _deckConfig.Pages.Count < MaxPages,
            };
            itemAdd.Click += (s, ev) => AddPage();
            menu.Items.Add(itemAdd);

            if (e.Index >= 0)
            {
                int idx = e.Index;
                var itemDel = new ToolStripMenuItem("Delete Page") { ForeColor = Color.FromArgb(220, 80, 80) };
                itemDel.Click += (s, ev) => DeletePage(idx);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemDel);
            }
            menu.Show(e.ScreenPoint);
        }

        // ------------------------------------------------------------------
        // Preview events -> Button editor
        // ------------------------------------------------------------------

        private void Preview_EditButtonRequested(object sender, int btnIdx)
        {
            int pageIdx = _pageIdx;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;
            OpenButtonEditor(_deckConfig.Pages[pageIdx], btnIdx);
        }

        private void Preview_AddButtonRequested(object sender, EventArgs e)
        {
            int pageIdx = _pageIdx;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;

            var page = _deckConfig.Pages[pageIdx];
            if (page.Buttons.Count >= DeckPreviewPanel.MaxButtons) return;   // menu item is disabled anyway
            var newBtn = new DeckButton { Label = "", Action = "launch" };
            page.Buttons.Add(newBtn);

            int newIdx = page.Buttons.Count - 1;
            using (var dlg = new FormDeckButtonEditor(newBtn, true, _assetsIconsDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _preview.SetPage(page, _assetsBackgroundsDir, _assetsIconsDir);
                    UpdatePageList();
                }
                else
                {
                    // User cancelled: remove the button that was tentatively added
                    page.Buttons.RemoveAt(newIdx);
                }
            }
        }

        private void Preview_ClearButtonRequested(object sender, int btnIdx)
        {
            int pageIdx = _pageIdx;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;
            var page = _deckConfig.Pages[pageIdx];
            if (btnIdx < 0 || btnIdx >= page.Buttons.Count) return;

            var result = MessageBox.Show(
                $"Remove button \"{page.Buttons[btnIdx].Label}\"?", "ESDeck PC",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            page.Buttons.RemoveAt(btnIdx);
            _preview.SetPage(page, _assetsBackgroundsDir, _assetsIconsDir);
            UpdatePageList();
        }

        private void Preview_ButtonClicked(object sender, int btnIdx)
        {
            int pageIdx = _pageIdx;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;
            var page = _deckConfig.Pages[pageIdx];
            if (btnIdx < 0 || btnIdx >= page.Buttons.Count) return;

            var button = page.Buttons[btnIdx];
            string action = button.Action?.ToLower();

            switch (action)
            {
                case "launch":
                case "media":
                case "discord":
                case "mouse_click":
                    string result = ActionExecutor.Run(button);
                    if (result != null && result.Contains("failed"))
                    {
                        MessageBox.Show(result, "Action failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;

                case "hotkey":
                case "sequence":
                case "scroll":
                case "text":
                case "paste":
                case "power":
                    MessageBox.Show(
                        $"Action \"{button.Action}\" is not supported in preview mode.",
                        "Not supported",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;

                default:
                    if (!string.IsNullOrEmpty(action))
                    {
                        MessageBox.Show(
                            $"Unknown action \"{button.Action}\".",
                            "Not supported",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    break;
            }
        }

        private void OpenButtonEditor(DeckPage page, int btnIdx)
        {
            var button = page.Buttons[btnIdx];
            using (var dlg = new FormDeckButtonEditor(button, false, _assetsIconsDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    _preview.SetPage(page, _assetsBackgroundsDir, _assetsIconsDir);
            }
        }

        // ------------------------------------------------------------------
        // JSON: New / Open / Save / Save As -- same shape as the Media /
        // Touchpad editors. One file per config; the name is free (the ESP
        // lists every *.json in config\deck). Save the file over the same
        // name and the device picks it up on its next boot (leaving Storage
        // Mode restarts it); a new name needs Select Config on the device.
        // ------------------------------------------------------------------

        private const string DefaultFileName = "deck.json";

        // File dialogs start in the current file's folder, else in the
        // ESDeck SD card's config\deck (Storage Mode), else Windows' default.
        private string DialogDir() =>
            !string.IsNullOrEmpty(_jsonPath) ? Path.GetDirectoryName(_jsonPath)
                                              : EsDeckDrive.Dir("config", "deck");

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            if (!_dirty.ConfirmDiscard(SaveInteractive)) return;
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New deck JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = DefaultFileName;
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _deckConfig = new DeckConfig();
                _jsonPath = dlg.FileName;
                ResolveAssetDirs();
                LoadPages();
                UpdateJsonPathLabel();
                _dirty.MarkSaved();
            }
        }

        private void BtnJsonOpen_Click(object sender, EventArgs e)
        {
            if (!_dirty.ConfirmDiscard(SaveInteractive)) return;
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open deck JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var cfg = JsonConvert.DeserializeObject<DeckConfig>(
                                  File.ReadAllText(dlg.FileName, Encoding.UTF8)) ?? new DeckConfig();
                    if (cfg.Pages == null) cfg.Pages = new List<DeckPage>();
                    if (cfg.Settings == null) cfg.Settings = new DeckSettingsCfg();
                    _deckConfig = cfg;
                    _jsonPath = dlg.FileName;
                    ResolveAssetDirs();
                    LoadPages();
                    UpdateJsonPathLabel();
                    WarnOverfullPages();
                    _dirty.MarkSaved();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open JSON:\n{ex.Message}", "Deck Editor",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e) => SaveInteractive();

        private void BtnSaveAs_Click(object sender, EventArgs e)
        {
            if (!PromptForSavePath()) return;   // always asks, regardless of _jsonPath
            SaveToCurrentPath();
        }

        private bool PromptForSavePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Save deck JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = !string.IsNullOrEmpty(_jsonPath)
                    ? Path.GetFileName(_jsonPath)
                    : DefaultFileName;
                dlg.InitialDirectory = DialogDir() ?? "";
                if (dlg.ShowDialog() != DialogResult.OK) return false;

                _jsonPath = dlg.FileName;
                ResolveAssetDirs();
                UpdateJsonPathLabel();
                return true;
            }
        }

        private bool SaveToCurrentPath()
        {
            try
            {
                // File.WriteAllText's default is UTF-8 without BOM, same as
                // the other editors; Json.NET leaves Chinese text unescaped,
                // and the ESP reads the bytes as is.
                File.WriteAllText(_jsonPath,
                    JsonConvert.SerializeObject(_deckConfig, Formatting.Indented));
                SetStatus($"Saved: {Path.GetFileName(_jsonPath)}");
                _dirty.MarkSaved();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}", "Deck Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>Save (asking for a path the first time). Button, Ctrl+S, and the unsaved-changes prompt.</summary>
        private bool SaveInteractive()
        {
            if (string.IsNullOrEmpty(_jsonPath) && !PromptForSavePath()) return false;
            return SaveToCurrentPath();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveInteractive();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!e.Cancel && !_dirty.ConfirmDiscard(SaveInteractive)) e.Cancel = true;
            base.OnFormClosing(e);
        }

        private string TitleText() =>
            _jsonPath != null ? $"Deck Editor - {Path.GetFileName(_jsonPath)}" : "Deck Editor";

        private void UpdateJsonPathLabel()
        {
            lblJsonPath.Text = _jsonPath != null ? Path.GetFileName(_jsonPath) : "(no file)";
            _dirty?.RefreshTitle();
        }

        // ------------------------------------------------------------------
        // Page list management
        // ------------------------------------------------------------------

        /// <summary>Pages or their buttons changed: refresh the move targets and the sidebar.</summary>
        private void UpdatePageList()
        {
            SyncMoveTargets();
            RefreshSidebar();
        }

        private void AddPage()
        {
            if (_deckConfig.Pages.Count >= MaxPages) return;
            string name = FormPageNamePrompt.Show(this, "New page", $"Page {_deckConfig.Pages.Count + 1}");
            if (string.IsNullOrWhiteSpace(name)) return;
            _deckConfig.Pages.Add(new DeckPage { Name = name });
            SelectPage(_deckConfig.Pages.Count - 1);
        }

        private void DeletePage(int idx)
        {
            if (_deckConfig.Pages.Count <= 1)
            {
                MessageBox.Show("At least one page is required.", "ESDeck PC",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete page \"{_deckConfig.Pages[idx].Name}\"?", "ESDeck PC",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            var shown = CurrentPage;
            _deckConfig.Pages.RemoveAt(idx);
            if (_settingsSelected) UpdatePageList();
            else if (shown != null && _deckConfig.Pages.Contains(shown)) SelectPage(_deckConfig.Pages.IndexOf(shown));
            else SelectPage(Math.Min(idx, _deckConfig.Pages.Count - 1));
        }

        /// <summary>Sidebar drag: page oldIdx goes to newIdx; the shown page stays shown.</summary>
        private void MovePage(int oldIdx, int newIdx)
        {
            var shown = CurrentPage;
            var page = _deckConfig.Pages[oldIdx];
            _deckConfig.Pages.RemoveAt(oldIdx);
            _deckConfig.Pages.Insert(newIdx, page);
            if (shown != null) _pageIdx = _deckConfig.Pages.IndexOf(shown);
            _preview.CurrentPageIndex = _pageIdx;
            UpdatePageList();
        }
    }
}