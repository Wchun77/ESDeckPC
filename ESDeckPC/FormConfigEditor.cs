using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormConfigEditor : Form
    {
        private PcConfig _pcConfig;
        private string _pcPath;
        private string _espPath;

        private string _assetsBackgroundsDir = null;
        private string _assetsIconsDir = null;

        /// <summary>
        /// Fired after a successful save. Argument is the new PC JSON path.
        /// </summary>
        public event EventHandler<string> ConfigSaved;

        private DeckPreviewPanel _preview;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }

        public FormConfigEditor(PcConfig pcConfig, string pcPath, string espPath)
        {
            InitializeComponent();

            _pcConfig = pcConfig;
            _pcPath = pcPath;
            _espPath = espPath;

            this.Text = $"Config Editor - {Path.GetFileName(pcPath)}";

            ResolveAssetDirs();

            if (!string.IsNullOrEmpty(_espPath) && File.Exists(_espPath))
            {
                try
                {
                    var espConfig = ConfigLoader.LoadEsp(_espPath);
                    for (int pi = 0; pi < _pcConfig.Pages.Count; pi++)
                    {
                        if (pi >= espConfig.Pages.Count) break;
                        _pcConfig.Pages[pi].BgImage = espConfig.Pages[pi].BgImage ?? "";
                        for (int bi = 0; bi < _pcConfig.Pages[pi].Buttons.Count; bi++)
                        {
                            if (bi >= espConfig.Pages[pi].Buttons.Count) break;
                            _pcConfig.Pages[pi].Buttons[bi].Icon = espConfig.Pages[pi].Buttons[bi].Icon ?? "";
                        }
                    }
                }
                catch { }
            }

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

            lstPages.SelectedIndexChanged += lstPages_SelectedIndexChanged;
            lstPages.MouseUp += lstPages_MouseUp;
            lstPages.KeyDown += lstPages_KeyDown;
            btnSave.Click += btnSave_Click;
            btnDiscard.Click += btnDiscard_Click;

            LoadPages();
        }

        // ------------------------------------------------------------------
        // Asset path resolution (same rule as MonitorEditor)
        // config\deck\xxx.json -> root -> assets\backgrounds / assets\icons
        // ------------------------------------------------------------------

        private void ResolveAssetDirs()
        {
            _assetsBackgroundsDir = null;
            _assetsIconsDir = null;

            if (string.IsNullOrEmpty(_espPath)) return;

            // ESP JSON lives at {root}\config\deck\esp_xxx.json
            var deckDir = Path.GetDirectoryName(_espPath);
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
        }

        // ------------------------------------------------------------------
        // Pages
        // ------------------------------------------------------------------

        private void LoadPages()
        {
            lstPages.Items.Clear();
            foreach (var pg in _pcConfig.Pages)
                lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");

            if (lstPages.Items.Count > 0)
                lstPages.SelectedIndex = 0;
        }

        private void lstPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstPages.SelectedIndex;
            if (idx < 0 || idx >= _pcConfig.Pages.Count) return;
            _preview.SetPage(_pcConfig.Pages[idx], _assetsBackgroundsDir, _assetsIconsDir);
        }

        // ------------------------------------------------------------------
        // Preview events -> Button editor
        // ------------------------------------------------------------------

        private void Preview_EditButtonRequested(object sender, int btnIdx)
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0 || pageIdx >= _pcConfig.Pages.Count) return;
            OpenButtonEditor(_pcConfig.Pages[pageIdx], btnIdx);
        }

        private void Preview_AddButtonRequested(object sender, EventArgs e)
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0 || pageIdx >= _pcConfig.Pages.Count) return;

            var page = _pcConfig.Pages[pageIdx];
            var newBtn = new PcButton { Label = "", Action = "launch" };
            page.Buttons.Add(newBtn);

            int newIdx = page.Buttons.Count - 1;
            using (var dlg = new FormButtonEditor(newBtn))
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
            if (pageIdx < 0 || pageIdx >= _pcConfig.Pages.Count) return;
            var page = _pcConfig.Pages[pageIdx];
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
            if (pageIdx < 0 || pageIdx >= _pcConfig.Pages.Count) return;
            var page = _pcConfig.Pages[pageIdx];
            if (btnIdx < 0 || btnIdx >= page.Buttons.Count) return;

            var button = page.Buttons[btnIdx];
            string action = button.Action?.ToLower();

            switch (action)
            {
                case "launch":
                case "media":
                case "discord":
                case "mouse_click":
                    // Execute via ActionExecutor using 1-based page/btn index
                    string result = ActionExecutor.Run(_pcConfig, (byte)(pageIdx + 1), (byte)(btnIdx + 1));
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

        private void OpenButtonEditor(PcPage page, int btnIdx)
        {
            var button = page.Buttons[btnIdx];
            using (var dlg = new FormButtonEditor(button))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    _preview.SetPage(page, _assetsBackgroundsDir, _assetsIconsDir);
            }
        }

        // ------------------------------------------------------------------
        // Save / Discard
        // ------------------------------------------------------------------

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = Path.GetDirectoryName(_pcPath);

                // Pre-calculate CRC the same way SavePair does, so we can
                // check for existing files before committing the write.
                string pcJson = Newtonsoft.Json.JsonConvert.SerializeObject(
                    _pcConfig, Newtonsoft.Json.Formatting.Indented);
                string crc = ConfigLoader.Crc16(pcJson).ToString("X4");

                string newPcPath = Path.Combine(folder, $"pc_{crc}.json");
                string newEspPath = Path.Combine(folder, $"esp_{crc}.json");
                bool pcExists = File.Exists(newPcPath);
                bool espExists = File.Exists(newEspPath);

                if (pcExists || espExists)
                {
                    string existing = (pcExists && espExists)
                        ? $"pc_{crc}.json and esp_{crc}.json"
                        : pcExists ? $"pc_{crc}.json" : $"esp_{crc}.json";

                    var confirm = MessageBox.Show(
                        $"{existing} already exist.\n\nOverwrite?",
                        "Config Editor",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes) return;
                }

                ConfigLoader.SavePair(_pcConfig, folder);

                string startupTxt = Path.Combine(folder, "startup.txt");
                File.WriteAllText(startupTxt, $"esp_{crc}.json", Encoding.ASCII);

                MessageBox.Show($"Saved as pc_{crc}.json / esp_{crc}.json", "Config Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                Process.Start("explorer.exe", folder);

                ConfigSaved?.Invoke(this, newPcPath);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed: {ex.Message}", "Config Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDiscard_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Discard all changes?", "Config Editor",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                this.Close();
        }
        // ------------------------------------------------------------------
        // Page list management (preserved from original)
        // ------------------------------------------------------------------

        private void UpdatePageList()
        {
            int sel = lstPages.SelectedIndex;
            lstPages.Items.Clear();
            foreach (var pg in _pcConfig.Pages)
                lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");
            if (sel >= 0 && sel < lstPages.Items.Count)
                lstPages.SelectedIndex = sel;
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
                    string name = PromptBackground(_pcConfig.Pages[idx].BgImage ?? "");
                    if (name == null) return;
                    _pcConfig.Pages[idx].BgImage = name;
                    int sel = lstPages.SelectedIndex;
                    if (sel == idx)
                        _preview.SetPage(_pcConfig.Pages[idx], _assetsBackgroundsDir, _assetsIconsDir);
                };
                menu.Items.Add(itemBg);

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

                if (idx < _pcConfig.Pages.Count - 1)
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
            _pcConfig.Pages.Add(new PcPage { Name = name });
            UpdatePageList();
            lstPages.SelectedIndex = lstPages.Items.Count - 1;
        }

        private void RenamePage(int idx)
        {
            string name = PromptInput("Rename Page", "New name:", _pcConfig.Pages[idx].Name);
            if (string.IsNullOrWhiteSpace(name)) return;
            _pcConfig.Pages[idx].Name = name;
            UpdatePageList();
            lstPages.SelectedIndex = idx;
        }

        private void DeletePage(int idx)
        {
            if (_pcConfig.Pages.Count <= 1)
            {
                MessageBox.Show("At least one page is required.", "ESDeck PC",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete page \"{_pcConfig.Pages[idx].Name}\"?", "ESDeck PC",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            _pcConfig.Pages.RemoveAt(idx);
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
            var page = _pcConfig.Pages[oldIdx];
            _pcConfig.Pages.RemoveAt(oldIdx);
            _pcConfig.Pages.Insert(newIdx, page);
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