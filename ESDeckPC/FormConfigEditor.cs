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

        private UC_DeckButton _draggedUC = null;

        private int _insertIndex = -1;
        private UC_DeckButton _highlightLeft = null;
        private UC_DeckButton _highlightRight = null;
        private Color _highlightOrigLeft;
        private Color _highlightOrigRight;

        public FormConfigEditor(PcConfig pcConfig, string pcPath, string espPath)
        {
            InitializeComponent();

            _pcConfig = pcConfig;
            _pcPath = pcPath;
            _espPath = espPath;

            this.Text = $"Config Editor - {Path.GetFileName(pcPath)}";

            lstPages.SelectedIndexChanged += lstPages_SelectedIndexChanged;
            lstPages.MouseUp += lstPages_MouseUp;
            lstPages.KeyDown += lstPages_KeyDown;
            btnSave.Click += btnSave_Click;
            btnDiscard.Click += btnDiscard_Click;
            pnlGrid.AllowDrop = true;
            pnlGrid.DragEnter += pnlGrid_DragEnter;
            pnlGrid.DragOver += pnlGrid_DragOver;
            pnlGrid.DragDrop += pnlGrid_DragDrop;
            pnlGrid.DragLeave += pnlGrid_DragLeave;
            pnlGrid.MouseUp += pnlGrid_MouseUp;

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

            LoadPages();
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
            BuildGrid(_pcConfig.Pages[idx]);
        }

        // ------------------------------------------------------------------
        // Grid
        // ------------------------------------------------------------------

        private void BuildGrid(PcPage page)
        {
            pnlGrid.Controls.Clear();

            for (int i = 0; i < page.Buttons.Count; i++)
            {
                var uc = new UC_DeckButton();
                uc.SetData(page.Buttons[i]);

                int btnIdx = i;
                uc.EditClicked += (s, ev) =>
                {
                    var sender_uc = s as UC_DeckButton;
                    int currentIdx = pnlGrid.Controls.GetChildIndex(sender_uc);
                    OpenButtonEditor(page, currentIdx);
                };
                uc.MouseUp += UC_MouseUp;

                pnlGrid.Controls.Add(uc);
            }
        }

        // ------------------------------------------------------------------
        // Button editor
        // ------------------------------------------------------------------

        private void OpenButtonEditor(PcPage page, int btnIdx)
        {
            var button = page.Buttons[btnIdx];
            using (var dlg = new FormButtonEditor(button))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // refresh the UC_DeckButton
                    var uc = pnlGrid.Controls[btnIdx] as UC_DeckButton;
                    uc?.SetData(button);
                }
            }
        }

        // ------------------------------------------------------------------
        // Save / Discard
        // ------------------------------------------------------------------

        private void btnSave_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Save and overwrite? New files will be generated.",
                "Config Editor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                string folder = Path.GetDirectoryName(_pcPath);
                string crc = ConfigLoader.SavePair(_pcConfig, folder);
                string newPcPath = Path.Combine(folder, $"pc_{crc}.json");

                // Write startup.txt so ESP loads the new config on next boot
                string startupTxt = Path.Combine(folder, "startup.txt");
                File.WriteAllText(startupTxt, $"esp_{crc}.json", Encoding.ASCII);

                MessageBox.Show($"Saved as pc_{crc}.json / esp_{crc}.json", "Config Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Open the folder in Explorer
                Process.Start("explorer.exe", folder);

                this.DialogResult = DialogResult.OK;
                this.Tag = newPcPath;
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
        // Drag & Drop
        // ------------------------------------------------------------------

        private void pnlGrid_DragEnter(object sender, DragEventArgs e)
        {
            ClearDragHighlight();
            if (e.Data.GetDataPresent(typeof(UC_DeckButton)))
            {
                _draggedUC = e.Data.GetData(typeof(UC_DeckButton)) as UC_DeckButton;
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void pnlGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(UC_DeckButton)))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Move;
            Point client = pnlGrid.PointToClient(new Point(e.X, e.Y));

            int newInsert = CalcInsertIndex(client);

            if (newInsert != _insertIndex)
            {
                _insertIndex = newInsert;
                ClearDragHighlight();

                int count = pnlGrid.Controls.Count;
                if (_insertIndex >= 0 && count > 0)
                {
                    // highlight left neighbour
                    int leftIdx = _insertIndex - 1;
                    if (leftIdx >= 0 && pnlGrid.Controls[leftIdx] is UC_DeckButton left
                        && left != _draggedUC)
                    {
                        _highlightOrigLeft = left.BackColor;
                        left.BackColor = Color.FromArgb(60, 100, 140);
                        _highlightLeft = left;
                    }

                    // highlight right neighbour
                    int rightIdx = _insertIndex;
                    if (rightIdx < count && pnlGrid.Controls[rightIdx] is UC_DeckButton right
                        && right != _draggedUC)
                    {
                        _highlightOrigRight = right.BackColor;
                        right.BackColor = Color.FromArgb(60, 100, 140);
                        _highlightRight = right;
                    }
                }
            }
        }

        private void pnlGrid_DragDrop(object sender, DragEventArgs e)
        {
            ClearDragHighlight();

            if (_draggedUC == null || _insertIndex < 0) return;

            int oldIdx = pnlGrid.Controls.GetChildIndex(_draggedUC);
            int newIdx = _insertIndex;

            if (newIdx == oldIdx || newIdx == oldIdx + 1)
            {
                _draggedUC = null;
                _insertIndex = -1;
                return;
            }

            // adjust insert index for removal
            int modelNew = newIdx > oldIdx ? newIdx - 1 : newIdx;

            // update UI
            pnlGrid.Controls.SetChildIndex(_draggedUC, modelNew);

            // sync model
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx >= 0)
            {
                var buttons = _pcConfig.Pages[pageIdx].Buttons;
                var item = buttons[oldIdx];
                buttons.RemoveAt(oldIdx);
                buttons.Insert(modelNew, item);
            }

            _draggedUC = null;
            _insertIndex = -1;
        }

        private void pnlGrid_DragLeave(object sender, EventArgs e)
        {
            ClearDragHighlight();
            _draggedUC = null;
            _insertIndex = -1;
        }

        private UC_DeckButton GetUCAt(Point clientPoint)
        {
            foreach (Control c in pnlGrid.Controls)
            {
                if (c is UC_DeckButton uc && c.Bounds.Contains(clientPoint))
                    return uc;
            }
            return null;
        }

        private int CalcInsertIndex(Point client)
        {
            int count = pnlGrid.Controls.Count;
            if (count == 0) return 0;

            for (int i = 0; i < count; i++)
            {
                var uc = pnlGrid.Controls[i] as UC_DeckButton;
                if (uc == null) continue;

                Rectangle b = uc.Bounds;
                int mid = b.Left + b.Width / 2;

                if (client.Y >= b.Top - 4 && client.Y <= b.Bottom + 4)
                {
                    if (client.X < mid)
                        return i;         // insert before this one
                    else if (i == count - 1 || client.X < b.Right + 4)
                        return i + 1;     // insert after this one
                }
            }

            // below all controls or to the right of last — append
            return count;
        }

        private void ClearDragHighlight()
        {
            if (_highlightLeft != null)
            {
                _highlightLeft.BackColor = _highlightOrigLeft;
                _highlightLeft = null;
            }
            if (_highlightRight != null)
            {
                _highlightRight.BackColor = _highlightOrigRight;
                _highlightRight = null;
            }
        }

        // ------------------------------------------------------------------
        // Right-click context menu
        // ------------------------------------------------------------------

        private void pnlGrid_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            Point client = e.Location;
            UC_DeckButton target = GetUCAt(client);

            if (target == null)
                ShowGridMenu(e.Location);
        }

        private void UC_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var uc = sender as UC_DeckButton;
            Point pos = uc.PointToScreen(e.Location);
            Point rel = pnlGrid.PointToClient(pos);
            ShowButtonMenu(rel, uc);
        }

        private void ShowGridMenu(Point location)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemAdd = new ToolStripMenuItem("Add Button");
            itemAdd.ForeColor = Color.FromArgb(220, 220, 220);
            itemAdd.Click += (s, e) => AddButton();
            menu.Items.Add(itemAdd);

            menu.Show(pnlGrid, location);
        }

        private void ShowButtonMenu(Point location, UC_DeckButton uc)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemAdd = new ToolStripMenuItem("Add Button");
            itemAdd.ForeColor = Color.FromArgb(220, 220, 220);
            itemAdd.Click += (s, e) => AddButton();

            var itemDel = new ToolStripMenuItem("Delete Button");
            itemDel.ForeColor = Color.FromArgb(220, 80, 80);
            itemDel.Click += (s, e) => DeleteButton(uc);

            menu.Items.Add(itemAdd);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemDel);

            menu.Show(pnlGrid, location);
        }

        private void AddButton()
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0) return;

            var page = _pcConfig.Pages[pageIdx];
            if (page.Buttons.Count >= 12)
            {
                MessageBox.Show("Max 12 buttons per page.", "ESDeck PC",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newButton = new PcButton { Label = "New", Action = "launch", Target = "" };

            using (var dlg = new FormButtonEditor(newButton, isNew: true))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
            }

            page.Buttons.Add(newButton);
            BuildGrid(page);
            UpdatePageList();
        }

        private void DeleteButton(UC_DeckButton uc)
        {
            int pageIdx = lstPages.SelectedIndex;
            if (pageIdx < 0) return;

            int btnIdx = pnlGrid.Controls.GetChildIndex(uc);
            if (btnIdx < 0) return;

            var result = MessageBox.Show(
                $"Delete \"{uc.Button.Label}\"?", "ESDeck PC",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            _pcConfig.Pages[pageIdx].Buttons.RemoveAt(btnIdx);
            BuildGrid(_pcConfig.Pages[pageIdx]);
            UpdatePageList();
        }

        private void UpdatePageList()
        {
            int sel = lstPages.SelectedIndex;
            lstPages.Items.Clear();
            foreach (var pg in _pcConfig.Pages)
                lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");
            if (sel >= 0 && sel < lstPages.Items.Count)
                lstPages.SelectedIndex = sel;
        }

        // ------------------------------------------------------------------
        // Page right-click menu
        // ------------------------------------------------------------------

        private void lstPages_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            int idx = lstPages.IndexFromPoint(e.Location);

            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(45, 45, 48);
            menu.ForeColor = Color.FromArgb(220, 220, 220);
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            menu.ShowImageMargin = false;

            var itemAdd = new ToolStripMenuItem("Add Page");
            itemAdd.ForeColor = Color.FromArgb(220, 220, 220);
            itemAdd.Click += (s, ev) => AddPage();
            menu.Items.Add(itemAdd);

            if (idx >= 0)
            {
                var itemRename = new ToolStripMenuItem("Rename");
                itemRename.ForeColor = Color.FromArgb(220, 220, 220);
                itemRename.Click += (s, ev) => RenamePage(idx);

                var itemBg = new ToolStripMenuItem("Set Background");
                itemBg.ForeColor = Color.FromArgb(220, 220, 220);
                itemBg.Click += (s, ev) =>
                {
                    string name = PromptInput("Set Background", "Image filename (e.g. bg.jpg):",
                        _pcConfig.Pages[idx].BgImage ?? "");
                    if (name == null) return;
                    _pcConfig.Pages[idx].BgImage = name.Trim();
                };
                menu.Items.Add(itemBg);

                var itemDel = new ToolStripMenuItem("Delete Page");
                itemDel.ForeColor = Color.FromArgb(220, 80, 80);
                itemDel.Click += (s, ev) => DeletePage(idx);

                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemRename);

                if (idx > 0)
                {
                    var itemUp = new ToolStripMenuItem("Move Up");
                    itemUp.ForeColor = Color.FromArgb(220, 220, 220);
                    itemUp.Click += (s, ev) => MovePage(idx, idx - 1);
                    menu.Items.Add(itemUp);
                }

                if (idx < _pcConfig.Pages.Count - 1)
                {
                    var itemDown = new ToolStripMenuItem("Move Down");
                    itemDown.ForeColor = Color.FromArgb(220, 220, 220);
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

            var lbl = new Label
            {
                Text = label,
                Location = new Point(12, 12),
                AutoSize = true,
                ForeColor = Color.Gray
            };
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

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
    }
}