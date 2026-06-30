using System;
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

        private UC_ClockSettings _ucClock = null;
        private UC_PageSettings _ucPage = null;
        private Bitmap _previewBmp = null;

        // The data page currently selected in flpPages, or null when Clock is selected.
        private MonitorPageCfg _selectedPage = null;

        private static readonly Color ColPageSelected = Color.FromArgb(140, 30, 30); // dark red
        private static readonly Color ColPageNormal = Color.FromArgb(55, 55, 58);
        private static readonly Color ColMenuBack = Color.FromArgb(45, 45, 48);
        private static readonly Color ColMenuFore = Color.FromArgb(220, 220, 220);

        // Drag-reorder state
        private Button _dragButton = null;

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
            this.Icon = Resources.computer;
            StyleDarkButtons();

            btnJsonNew.Click += BtnJsonNew_Click;
            btnJsonOpen.Click += BtnJsonOpen_Click;
            btnJsonSave.Click += BtnJsonSave_Click;
            btnClockPage.Click += BtnClockPage_Click;

            flpPages.AllowDrop = true;
            flpPages.MouseUp += FlpPages_MouseUp;
            flpPages.DragOver += FlpPages_DragOver;
            flpPages.DragDrop += FlpPages_DragDrop;

            RebuildPageButtons();
            ShowClockSettings();
            RefreshPreview();
        }

        // ------------------------------------------------------------------
        // Page button list (flpPages) management
        // ------------------------------------------------------------------

        private void RebuildPageButtons()
        {
            foreach (var c in flpPages.Controls.OfType<Button>().ToList())
            {
                c.MouseDown -= PageButton_MouseDown;
                c.MouseMove -= PageButton_MouseMove;
                c.MouseUp -= PageButton_MouseUp;
                c.Click -= PageButton_Click;
            }
            flpPages.Controls.Clear();

            foreach (var page in _cfg.Pages)
                flpPages.Controls.Add(CreatePageButton(page));
        }

        private Button CreatePageButton(MonitorPageCfg page)
        {
            var btn = new Button
            {
                Text = page.Name,
                Tag = page,
                Size = new Size(60, 55),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColPageNormal,
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 8.5f),
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.MouseDown += PageButton_MouseDown;
            btn.MouseMove += PageButton_MouseMove;
            btn.MouseUp += PageButton_MouseUp;
            btn.Click += PageButton_Click;
            return btn;
        }

        private void PageButton_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var page = (MonitorPageCfg)btn.Tag;
            ShowPageSettings(page);
        }

        // ------------------------------------------------------------------
        // Drag-to-reorder (data pages only; Clock button is not in flpPages)
        // ------------------------------------------------------------------

        private void PageButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragButton = (Button)sender;
        }

        private void PageButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var btn = (Button)sender;
            if (_dragButton != btn) return;

            bool dragging = Math.Abs(e.X - btn.Width / 2) > SystemInformation.DragSize.Width
                         || Math.Abs(e.Y - btn.Height / 2) > SystemInformation.DragSize.Height;
            if (!dragging) return;

            btn.DoDragDrop(btn, DragDropEffects.Move);
            _dragButton = null;
        }

        private void PageButton_MouseUp(object sender, MouseEventArgs e)
        {
            _dragButton = null;
            if (e.Button != MouseButtons.Right) return;
            ShowPageButtonContextMenu((Button)sender, Cursor.Position);
        }

        private void FlpPages_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            // Right-click landed on empty flpPages area (not on a button,
            // since button clicks are handled by PageButton_MouseUp and
            // would have already shown their own menu).
            if (flpPages.GetChildAtPoint(e.Location) != null) return;
            ShowEmptyAreaContextMenu(Cursor.Position);
        }

        private void FlpPages_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Button)))
                e.Effect = DragDropEffects.Move;
        }

        private void FlpPages_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(Button))) return;
            var dragged = (Button)e.Data.GetData(typeof(Button));

            var p = flpPages.PointToClient(new Point(e.X, e.Y));
            int newIndex = FindDropIndex(dragged, p);

            flpPages.Controls.SetChildIndex(dragged, newIndex);
            ReorderPagesFromButtons();
        }

        /// <summary>
        /// Finds the index to drop "dragged" at, based on proximity to the
        /// midpoint of each other button. More forgiving than relying on
        /// GetChildAtPoint, which often returns the dragged button itself
        /// or nothing at all if the cursor lands between buttons.
        /// </summary>
        private int FindDropIndex(Button dragged, Point dropPointClient)
        {
            var others = flpPages.Controls.OfType<Button>().Where(b => b != dragged).ToList();
            if (others.Count == 0) return 0;

            for (int i = 0; i < others.Count; i++)
            {
                var b = others[i];
                int midX = b.Left + b.Width / 2;
                if (dropPointClient.X < midX)
                    return flpPages.Controls.GetChildIndex(b, false);
            }

            // Dropped past the last button: insert after it.
            var last = others[others.Count - 1];
            return flpPages.Controls.GetChildIndex(last, false) + 1;
        }

        private void ReorderPagesFromButtons()
        {
            var ordered = flpPages.Controls.OfType<Button>()
                                            .Select(b => (MonitorPageCfg)b.Tag)
                                            .ToList();
            _cfg.Pages.Clear();
            _cfg.Pages.AddRange(ordered);
        }

        // ------------------------------------------------------------------
        // Context menus (built dynamically; dark theme)
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

        private void ShowEmptyAreaContextMenu(Point screenPos)
        {
            var cms = BuildDarkMenu();
            var miAdd = new ToolStripMenuItem("Add")
            {
                Enabled = _cfg.Pages.Count < MonitorPageLimits.MaxPages,
            };
            miAdd.Click += (s, e) => AddNewPage();
            cms.Items.Add(miAdd);

            cms.Show(screenPos);
        }

        private void ShowPageButtonContextMenu(Button btn, Point screenPos)
        {
            var page = (MonitorPageCfg)btn.Tag;
            var cms = BuildDarkMenu();

            var miEdit = new ToolStripMenuItem("Edit");
            miEdit.Click += (s, e) => EditPageName(btn, page);
            cms.Items.Add(miEdit);

            var miDelete = new ToolStripMenuItem("Delete");
            miDelete.Click += (s, e) => DeletePage(btn, page);
            cms.Items.Add(miDelete);

            cms.Show(screenPos);
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
        // Add / Edit / Delete page
        // ------------------------------------------------------------------

        private void AddNewPage()
        {
            if (_cfg.Pages.Count >= MonitorPageLimits.MaxPages)
            {
                MessageBox.Show(this, $"Maximum of {MonitorPageLimits.MaxPages} pages reached.",
                               "Limit reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = FormTabNamePrompt.Show(this, "New page", $"Page {_cfg.Pages.Count + 1}");
            if (name == null) return; // cancelled

            var page = new MonitorPageCfg { Name = name };
            _cfg.Pages.Add(page);

            var btn = CreatePageButton(page);
            flpPages.Controls.Add(btn);

            ShowPageSettings(page);
        }

        private void EditPageName(Button btn, MonitorPageCfg page)
        {
            string name = FormTabNamePrompt.Show(this, "Rename page", page.Name);
            if (name == null) return; // cancelled

            page.Name = name;
            btn.Text = name;
            if (_ucPage != null && _ucPage.BoundPage == page)
                _ucPage.ApplyConfig(page); // refresh name textbox if currently shown
        }

        private void DeletePage(Button btn, MonitorPageCfg page)
        {
            var result = MessageBox.Show(this, $"Delete page \"{page.Name}\"?", "Confirm delete",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            _cfg.Pages.Remove(page);
            flpPages.Controls.Remove(btn);
            btn.MouseDown -= PageButton_MouseDown;
            btn.MouseMove -= PageButton_MouseMove;
            btn.MouseUp -= PageButton_MouseUp;
            btn.Click -= PageButton_Click;
            btn.Dispose();

            if (_selectedPage == page)
                ShowClockSettings(); // fall back to Clock if the deleted page was selected
        }

        // ------------------------------------------------------------------
        // Page tab switching
        // ------------------------------------------------------------------

        private void BtnClockPage_Click(object sender, EventArgs e)
        {
            ShowClockSettings();
        }

        private void ShowClockSettings()
        {
            _selectedPage = null;

            if (_ucClock == null)
            {
                _ucClock = new UC_ClockSettings { Dock = DockStyle.Top };
                _ucClock.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucClock);
            pnlSettingsHost.ResumeLayout();

            _ucClock.ApplyConfig(_cfg.Clock);

            SetSelectedButton(btnClockPage);
            SetStatus("");
            RefreshPreview();
        }

        private void ShowPageSettings(MonitorPageCfg page)
        {
            _selectedPage = page;

            if (_ucPage == null)
            {
                _ucPage = new UC_PageSettings { Dock = DockStyle.Top };
                _ucPage.PreviewInvalidated += (s, e) => RefreshPreview();
            }

            pnlSettingsHost.SuspendLayout();
            pnlSettingsHost.Controls.Clear();
            pnlSettingsHost.Controls.Add(_ucPage);
            pnlSettingsHost.ResumeLayout();

            _ucPage.ApplyConfig(page);

            var btn = flpPages.Controls.OfType<Button>().FirstOrDefault(b => ReferenceEquals(b.Tag, page));
            SetSelectedButton(btn);
            SetStatus("");
            RefreshPreview();
        }

        private void SetSelectedButton(Button selected)
        {
            btnClockPage.BackColor = (selected == btnClockPage) ? ColPageSelected : ColPageNormal;
            foreach (var btn in flpPages.Controls.OfType<Button>())
                btn.BackColor = (btn == selected) ? ColPageSelected : ColPageNormal;
        }

        // ------------------------------------------------------------------
        // JSON handlers
        // ------------------------------------------------------------------

        private void BtnJsonNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "New monitor JSON";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.FileName = "monitor.json";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _cfg = new MonitorConfig();
                _jsonPath = dlg.FileName;
                RebuildPageButtons();
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
                if (!string.IsNullOrEmpty(_jsonPath))
                    dlg.InitialDirectory = Path.GetDirectoryName(_jsonPath);
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var cfg = JsonConvert.DeserializeObject<MonitorConfig>(
                                  File.ReadAllText(dlg.FileName)) ?? new MonitorConfig();
                    _cfg = cfg;
                    _jsonPath = dlg.FileName;
                    RebuildPageButtons();
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
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save monitor JSON";
                    dlg.Filter = "JSON files (*.json)|*.json";
                    dlg.FileName = "monitor.json";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    _jsonPath = dlg.FileName;
                    UpdateJsonPathLabel();
                }
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

            Bitmap newBmp;
            if (_selectedPage != null)
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
            if (_ucPage != null && _selectedPage != null)
                _ucPage.ApplyConfig(_selectedPage);
        }

        private void ReadUiIntoConfig()
        {
            _ucClock?.ReadConfig(_cfg.Clock);
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

            foreach (var btn in new[] { btnJsonNew, btnJsonOpen, btnJsonSave, btnClockPage })
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