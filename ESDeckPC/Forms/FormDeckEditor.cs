using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        // Settings entry (btnSettingsPage) -- not part of _deckConfig.Pages,
        // shown/hidden the same way FormMonitorEditor's fixed Clock button
        // works. Tracked separately since lstPages' own SelectedIndex can't
        // represent "Settings selected".
        private bool _settingsSelected = false;
        private static readonly Color ColSettingsNormal = Color.FromArgb(45, 45, 48);
        private static readonly Color ColSettingsSelected = Color.FromArgb(0, 85, 204);

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

            // Create DeckPreviewPanel and attach to picDeckPreview's location/size
            _preview = new DeckPreviewPanel
            {
                Location = picDeckPreview.Location,
                Size = picDeckPreview.Size,
            };
            picDeckPreview.Visible = false; // keep in Designer, hidden at runtime
            grpPreview.Controls.Add(_preview);
            _preview.BringToFront();

            _preview.EditButtonRequested += Preview_EditButtonRequested;
            _preview.AddButtonRequested += Preview_AddButtonRequested;
            _preview.ClearButtonRequested += Preview_ClearButtonRequested;
            _preview.ReorderCompleted += (s, e) => UpdatePageList();
            _preview.ButtonClicked += Preview_ButtonClicked;
            _preview.MoveButtonRequested += (s, e) => MoveButton(lstPages.SelectedIndex, e.ButtonIndex, e.TargetPage);
            _preview.DragOutside += Preview_DragOutside;

            lstPages.SelectedIndexChanged += lstPages_SelectedIndexChanged;
            lstPages.MouseUp += lstPages_MouseUp;
            lstPages.KeyDown += lstPages_KeyDown;
            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnSave.Click += BtnSave_Click;
            btnSaveAs.Click += BtnSaveAs_Click;

            btnSettingsPage.BackColor = ColSettingsNormal;
            btnSettingsPage.Click += (s, e) => ShowSettingsPreview();
            btnSettingsPage.MouseUp += BtnSettingsPage_MouseUp;

            LoadPages();
            UpdateJsonPathLabel();
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
            lstPages.Items.Clear();
            foreach (var pg in _deckConfig.Pages)
                lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");

            if (lstPages.Items.Count > 0)
                lstPages.SelectedIndex = 0;
            else
                ShowSettingsPreview();   // nothing else to show; also clears a stale page
        }

        private void lstPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstPages.SelectedIndex;
            if (idx < 0 || idx >= _deckConfig.Pages.Count) return;

            SetSettingsButtonSelected(false);
            _preview.AllowButtons = true;
            SyncMoveTargets();
            _preview.SetPage(_deckConfig.Pages[idx], _assetsBackgroundsDir, _assetsIconsDir);
        }

        // ------------------------------------------------------------------
        // Moving a button to another page: right-click > Move to Page, or
        // drag it from the preview onto a page in the page list. It goes to
        // the end of that page; the current page stays shown.
        // ------------------------------------------------------------------

        private void SyncMoveTargets()
        {
            var names = new List<string>();
            foreach (var pg in _deckConfig.Pages) names.Add(pg.Name);
            _preview.MoveTargetNames = names;
            _preview.CurrentPageIndex = lstPages.SelectedIndex;
        }

        private void Preview_DragOutside(object sender, DeckDragOutsideEventArgs e)
        {
            int from = lstPages.SelectedIndex;
            Point pt = lstPages.PointToClient(e.ScreenPoint);
            int to = lstPages.ClientRectangle.Contains(pt) ? lstPages.IndexFromPoint(pt) : ListBox.NoMatches;
            e.Accept = to != ListBox.NoMatches && to != from && to < _deckConfig.Pages.Count;
            if (e.Accept && e.Released) MoveButton(from, e.ButtonIndex, to);
        }

        private void MoveButton(int fromPage, int btnIdx, int toPage)
        {
            if (fromPage < 0 || fromPage >= _deckConfig.Pages.Count) return;
            if (toPage < 0 || toPage >= _deckConfig.Pages.Count || toPage == fromPage) return;
            var src = _deckConfig.Pages[fromPage];
            if (btnIdx < 0 || btnIdx >= src.Buttons.Count) return;

            var btn = src.Buttons[btnIdx];
            src.Buttons.RemoveAt(btnIdx);
            _deckConfig.Pages[toPage].Buttons.Add(btn);
            _preview.SetPage(src, _assetsBackgroundsDir, _assetsIconsDir);
            UpdatePageList();   // button counts in the list
        }

        // ------------------------------------------------------------------
        // Settings entry (fixed, not part of _deckConfig.Pages -- same idea as
        // FormMonitorEditor's btnClockPage). Preview is background-only:
        // Settings has no buttons in the schema, so AllowButtons is turned
        // off and the wrapper DeckPage we build here always has an empty
        // Buttons list.
        // ------------------------------------------------------------------

        private void SetSettingsButtonSelected(bool selected)
        {
            _settingsSelected = selected;
            btnSettingsPage.BackColor = selected ? ColSettingsSelected : ColSettingsNormal;
        }

        private void ShowSettingsPreview()
        {
            lstPages.SelectedIndex = -1;   // harmless no-op via the idx<0 guard above
            SetSettingsButtonSelected(true);

            _preview.AllowButtons = false;
            var settingsPage = new DeckPage
            {
                Name = "Settings",
                BgImage = _deckConfig.Settings.BgImage,
                Buttons = new List<DeckButton>(),
            };
            _preview.SetPage(settingsPage, _assetsBackgroundsDir, _assetsIconsDir);
        }

        private void BtnSettingsPage_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemBg = new ToolStripMenuItem("Set Background") { ForeColor = Color.FromArgb(220, 220, 220) };
            itemBg.Click += (s, ev) =>
            {
                string name = PromptBackground(_deckConfig.Settings.BgImage ?? "");
                if (name == null) return;
                _deckConfig.Settings.BgImage = name;
                if (_settingsSelected)
                    ShowSettingsPreview();
            };
            menu.Items.Add(itemBg);

            var itemSideIcon = new ToolStripMenuItem("Set Side Icon") { ForeColor = Color.FromArgb(220, 220, 220) };
            itemSideIcon.Click += (s, ev) =>
            {
                string name = FormSideIconPicker.Show(this, "Set Side Icon - Settings",
                    _deckConfig.Settings.SideIcon ?? "", "Settings", _assetsSideIconsDir);
                if (name == null) return;
                _deckConfig.Settings.SideIcon = name;
                // No _preview refresh -- same as page side_icon, DeckPreviewPanel
                // shows the grid/background, not the sidebar switcher.
            };
            menu.Items.Add(itemSideIcon);

            menu.Show(btnSettingsPage, e.Location);
        }

        // ------------------------------------------------------------------
        // Preview events -> Button editor
        // ------------------------------------------------------------------

        private void Preview_EditButtonRequested(object sender, int btnIdx)
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;
            OpenButtonEditor(_deckConfig.Pages[pageIdx], btnIdx);
        }

        private void Preview_AddButtonRequested(object sender, EventArgs e)
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0 || pageIdx >= _deckConfig.Pages.Count) return;

            var page = _deckConfig.Pages[pageIdx];
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
            int pageIdx = lstPages.SelectedIndex;
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
            int pageIdx = lstPages.SelectedIndex;
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
            }
        }

        private void BtnJsonOpen_Click(object sender, EventArgs e)
        {
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open JSON:\n{ex.Message}", "Deck Editor",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_jsonPath) && !PromptForSavePath()) return;
            SaveToCurrentPath();
        }

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

        private void SaveToCurrentPath()
        {
            var confirm = MessageBox.Show(
                $"Save changes to {Path.GetFileName(_jsonPath)}?",
                "Deck Editor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                // File.WriteAllText's default is UTF-8 without BOM, same as
                // the other editors; Json.NET leaves Chinese text unescaped,
                // and the ESP reads the bytes as is.
                File.WriteAllText(_jsonPath,
                    JsonConvert.SerializeObject(_deckConfig, Formatting.Indented));
                lblJsonPath.Text = $"Saved: {Path.GetFileName(_jsonPath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}", "Deck Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateJsonPathLabel()
        {
            lblJsonPath.Text = _jsonPath != null ? Path.GetFileName(_jsonPath) : "(no file)";
            this.Text = _jsonPath != null
                ? $"Deck Editor - {Path.GetFileName(_jsonPath)}"
                : "Deck Editor";
        }

        // ------------------------------------------------------------------
        // Page list management (preserved from original)
        // ------------------------------------------------------------------

        private void UpdatePageList()
        {
            int sel = lstPages.SelectedIndex;
            lstPages.Items.Clear();
            foreach (var pg in _deckConfig.Pages)
                lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");
            if (sel >= 0 && sel < lstPages.Items.Count)
                lstPages.SelectedIndex = sel;
            SyncMoveTargets();   // page added / renamed / removed
        }

        private void lstPages_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            int idx = lstPages.IndexFromPoint(e.Location);

            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemAdd = new ToolStripMenuItem("Add Page") { ForeColor = Color.FromArgb(220, 220, 220) };
            itemAdd.Click += (s, ev) => AddPage();
            menu.Items.Add(itemAdd);

            if (idx >= 0)
            {
                var itemRename = new ToolStripMenuItem("Rename") { ForeColor = Color.FromArgb(220, 220, 220) };
                itemRename.Click += (s, ev) => RenamePage(idx);

                var itemBg = new ToolStripMenuItem("Set Background") { ForeColor = Color.FromArgb(220, 220, 220) };
                itemBg.Click += (s, ev) =>
                {
                    string name = PromptBackground(_deckConfig.Pages[idx].BgImage ?? "");
                    if (name == null) return;
                    _deckConfig.Pages[idx].BgImage = name;
                    int sel = lstPages.SelectedIndex;
                    if (sel == idx)
                        _preview.SetPage(_deckConfig.Pages[idx], _assetsBackgroundsDir, _assetsIconsDir);
                };
                menu.Items.Add(itemBg);

                var itemSideIcon = new ToolStripMenuItem("Set Side Icon") { ForeColor = Color.FromArgb(220, 220, 220) };
                itemSideIcon.Click += (s, ev) =>
                {
                    string name = FormSideIconPicker.Show(this, "Set Side Icon",
                        _deckConfig.Pages[idx].SideIcon ?? "", _deckConfig.Pages[idx].Name, _assetsSideIconsDir);
                    if (name == null) return;
                    _deckConfig.Pages[idx].SideIcon = name;
                    // No _preview refresh here -- DeckPreviewPanel shows the
                    // grid page (buttons/background), not the sidebar page
                    // switcher, so side_icon has nothing to redraw there.
                };
                menu.Items.Add(itemSideIcon);

                var itemDel = new ToolStripMenuItem("Delete Page") { ForeColor = Color.FromArgb(220, 80, 80) };
                itemDel.Click += (s, ev) => DeletePage(idx);

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemRename);

                if (idx > 0)
                {
                    var itemUp = new ToolStripMenuItem("Move Up") { ForeColor = Color.FromArgb(220, 220, 220) };
                    itemUp.Click += (s, ev) => MovePage(idx, idx - 1);
                    menu.Items.Add(itemUp);
                }

                if (idx < _deckConfig.Pages.Count - 1)
                {
                    var itemDown = new ToolStripMenuItem("Move Down") { ForeColor = Color.FromArgb(220, 220, 220) };
                    itemDown.Click += (s, ev) => MovePage(idx, idx + 1);
                    menu.Items.Add(itemDown);
                }

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemDel);
            }

            menu.Show(lstPages, e.Location);
        }

        private void AddPage()
        {
            string name = PromptInput("Add Page", "Page name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            _deckConfig.Pages.Add(new DeckPage { Name = name });
            UpdatePageList();
            lstPages.SelectedIndex = lstPages.Items.Count - 1;
        }

        private void RenamePage(int idx)
        {
            string name = PromptInput("Rename Page", "New name:", _deckConfig.Pages[idx].Name);
            if (string.IsNullOrWhiteSpace(name)) return;
            _deckConfig.Pages[idx].Name = name;
            UpdatePageList();
            lstPages.SelectedIndex = idx;
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

            _deckConfig.Pages.RemoveAt(idx);
            UpdatePageList();
            lstPages.SelectedIndex = Math.Min(idx, lstPages.Items.Count - 1);
        }

        private void lstPages_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control) return;
            int idx = lstPages.SelectedIndex;
            if (idx < 0) return;

            if (e.KeyCode == Keys.Up && idx > 0)
            {
                MovePage(idx, idx - 1);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && idx < lstPages.Items.Count - 1)
            {
                MovePage(idx, idx + 1);
                e.SuppressKeyPress = true;
            }
        }

        private void MovePage(int oldIdx, int newIdx)
        {
            var page = _deckConfig.Pages[oldIdx];
            _deckConfig.Pages.RemoveAt(oldIdx);
            _deckConfig.Pages.Insert(newIdx, page);
            UpdatePageList();
            lstPages.SelectedIndex = newIdx;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Shows a small dialog for picking a background image.
        /// Returns the short filename on OK, empty string on Clear+OK, or null on Cancel.
        /// </summary>
        private string PromptBackground(string currentFileName)
        {
            var dlgForm = new Form
            {
                Text = "Set Background",
                ClientSize = new Size(360, 90),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = this.Font,
            };

            int v = 1;
            DwmSetWindowAttribute(dlgForm.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));

            var darkBg = Color.FromArgb(45, 45, 48);
            var darkBorder = Color.FromArgb(80, 80, 80);
            var lightFg = Color.FromArgb(220, 220, 220);

            var txt = new TextBox
            {
                Text = currentFileName,
                Location = new Point(12, 12),
                Size = new Size(336, 22),
                BackColor = darkBg,
                ForeColor = lightFg,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
            };

            var btnBrowse = new Button
            {
                Text = "Browse",
                Location = new Point(12, 46),
                Size = new Size(90, 26),
                BackColor = darkBg,
                ForeColor = lightFg,
                FlatStyle = FlatStyle.Flat,
            };
            btnBrowse.FlatAppearance.BorderColor = darkBorder;

            var btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(108, 46),
                Size = new Size(90, 26),
                BackColor = darkBg,
                ForeColor = lightFg,
                FlatStyle = FlatStyle.Flat,
            };
            btnClear.FlatAppearance.BorderColor = darkBorder;

            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(258, 46),
                Size = new Size(90, 26),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);

            btnBrowse.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select background image";
                    ofd.Filter = "JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg";
                    if (!string.IsNullOrEmpty(_assetsBackgroundsDir) &&
                        Directory.Exists(_assetsBackgroundsDir))
                        ofd.InitialDirectory = _assetsBackgroundsDir;
                    if (ofd.ShowDialog() == DialogResult.OK)
                        txt.Text = Path.GetFileName(ofd.FileName);
                }
            };

            btnClear.Click += (s, e) => txt.Text = "";

            dlgForm.Controls.AddRange(new Control[] { txt, btnBrowse, btnClear, btnOk });
            dlgForm.AcceptButton = btnOk;

            if (dlgForm.ShowDialog(this) == DialogResult.OK)
                return txt.Text.Trim();

            return null;
        }

        private string PromptInput(string title, string label, string defaultValue = "")
        {
            Form prompt = new Form
            {
                Text = title,
                ClientSize = new Size(300, 100),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = this.Font,
            };

            int value = 1;
            DwmSetWindowAttribute(prompt.Handle, 20, ref value, sizeof(int));

            var lbl = new Label { Text = label, Location = new Point(12, 12), AutoSize = true, ForeColor = Color.Gray };
            var txt = new TextBox
            {
                Text = defaultValue,
                Location = new Point(12, 28),
                Width = 270,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
            var btn = new Button
            {
                Text = "OK",
                Location = new Point(207, 58),
                Width = 75,
                Height = 26,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);

            prompt.Controls.AddRange(new Control[] { lbl, txt, btn });
            prompt.AcceptButton = btn;

            return prompt.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}