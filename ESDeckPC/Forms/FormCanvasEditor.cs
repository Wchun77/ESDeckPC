using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ESDeckPC
{
    /// <summary>
    /// Canvas > Edit Pages: pick which pages (up to 4) a config shows on the
    /// ESP sidebar, their order, names and icons, and which config is in use.
    /// Everything is saved under the Canvas folder; while the ESP is in
    /// Canvas mode, saving shows up there right away (CanvasLink watches the
    /// folder). Built in code, no designer file.
    /// </summary>
    public class FormCanvasEditor : Form
    {
        private static readonly Color Bg = Color.FromArgb(28, 28, 28);
        private static readonly Color PanelColor = Color.FromArgb(40, 40, 40);
        private static readonly Color Fg = Color.FromArgb(220, 220, 220);

        private readonly ComboBox _cboConfig = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label _lblActive = new Label { AutoSize = true, ForeColor = Color.Gray };
        private readonly ListBox _lstLibrary = new ListBox { IntegralHeight = false };
        private readonly TextBox _txtSearch = new TextBox();
        private List<string> _libAll = new List<string>();   // every page folder; _lstLibrary shows the matches
        private readonly ListBox _lstPages = new ListBox { IntegralHeight = false };
        private readonly TextBox _txtName = new TextBox();
        private readonly TextBox _txtIcon = new TextBox();
        private readonly PictureBox _picIcon = new PictureBox
        {
            Size = new Size(64, 64), SizeMode = PictureBoxSizeMode.Normal,
            BackColor = Color.FromArgb(0x2a, 0x2a, 0x2a), Margin = new Padding(6, 0, 0, 0),
        };

        // Grey hint text in an empty TextBox (.NET Framework has no PlaceholderText).
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // Dark scroll bars on the list boxes (Windows 10 1809+; ignored elsewhere).
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private static void DarkScrollBars(Control c)
        {
            c.HandleCreated += (s, e) => { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
        }

        // Live preview of the selected page (its own WebView2, separate from
        // the one that renders for the ESP; no esdeck object, so buttons
        // don't do anything on the PC).
        private readonly Panel _previewBox = new Panel { BackColor = Color.Black };
        private readonly Label _lblPreview = new Label { AutoSize = true, ForeColor = Color.Silver };

        // The whole ESP screen (800 x 480): sidebar picture on the left
        // (page icons + gear, drawn like the ESP draws them), the page or
        // the Settings mock on the right.
        private readonly PictureBox _sidePic = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(0x11, 0x11, 0x11), Cursor = Cursors.Hand };
        private readonly PictureBox _setPic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.StretchImage, Visible = false, BackColor = Color.FromArgb(0x22, 0x22, 0x22) };
        private bool _showSettings;
        private int _previewIndex = -1;       // sidebar slot being previewed, -1 = a library page not in this config
        private Button _btnPageView, _btnSetView;
        private WebView2 _web;
        private Task _webInit;
        private string _previewDir;
        private readonly Timer _previewTimer = new Timer { Interval = 250 };   // selection settles before loading

        private static void SetHint(TextBox box, string hint)
        {
            if (box.IsHandleCreated)
                SendMessage(box.Handle, EM_SETCUEBANNER, (IntPtr)1, hint ?? "");   // 1 = keep showing while focused
        }
        private readonly TextBox _txtSetBg = new TextBox { Width = 210 };
        private readonly TextBox _txtSetIcon = new TextBox { Width = 170 };
        private readonly Label _lblStatus = new Label { AutoSize = true, ForeColor = Color.Gray };

        private CanvasConfig _cfg = new CanvasConfig();
        private string _cfgName;        // file name in configs/, null = unsaved new
        private bool _dirty;
        private bool _loading;

        public FormCanvasEditor()
        {
            Text = "Canvas Pages";
            ClientSize = new Size(900, 620);
            MinimumSize = new Size(880, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            ForeColor = Fg;
            Font = new Font("Segoe UI", 9f);

            BuildLayout();
            CanvasPaths.Ensure();
            RefreshLibrary();
            RefreshConfigs(CanvasPaths.ActiveConfigName);
            CanvasPaths.ActiveChanged += OnActiveChanged;
        }

        // The config in use was changed elsewhere (e.g. Select Config on the ESP).
        private void OnActiveChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke((Action)UpdateActiveLabel);
            else UpdateActiveLabel();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CanvasPaths.ActiveChanged -= OnActiveChanged;
            _previewTimer.Dispose();
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        private Button MakeButton(string text, EventHandler click, int width = 90)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 28,
                FlatStyle = FlatStyle.Flat, BackColor = PanelColor, ForeColor = Fg,
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            b.Click += click;
            return b;
        }

        private static Label Caption(string text) => new Label { Text = text, AutoSize = true, ForeColor = Color.Silver };

        private void Style(Control c)
        {
            c.BackColor = PanelColor;
            c.ForeColor = Fg;
            if (c is TextBox t) t.BorderStyle = BorderStyle.FixedSingle;
            if (c is ListBox l) l.BorderStyle = BorderStyle.FixedSingle;
        }

        private void BuildLayout()
        {
            foreach (Control c in new Control[] { _cboConfig, _lstLibrary, _lstPages, _txtName, _txtIcon, _txtSetBg, _txtSetIcon, _txtSearch }) Style(c);
            DarkScrollBars(_lstLibrary);
            DarkScrollBars(_lstPages);
            _cboConfig.FlatStyle = FlatStyle.Flat;

            // --- top: config file ---
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(10, 8, 10, 0), WrapContents = false };
            _cboConfig.Width = 200;
            _cboConfig.SelectedIndexChanged += (s, e) => { if (!_loading) OpenSelectedConfig(); };
            top.Controls.Add(new Label { Text = "Config:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            top.Controls.Add(_cboConfig);
            top.Controls.Add(MakeButton("New...", (s, e) => NewConfig(), 70));
            top.Controls.Add(MakeButton("Save", (s, e) => Save(), 70));
            top.Controls.Add(MakeButton("Use This Config", (s, e) => UseThisConfig(), 120));
            _lblActive.Margin = new Padding(10, 7, 0, 0);
            top.Controls.Add(_lblActive);

            // --- bottom: folder helpers + status ---
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(10, 6, 10, 0), WrapContents = false };
            bottom.Controls.Add(MakeButton("New Page...", (s, e) => NewPage(), 100));
            bottom.Controls.Add(MakeButton("Open Canvas Folder", (s, e) =>
                Process.Start("explorer.exe", $"\"{CanvasPaths.Root}\""), 140));
            bottom.Controls.Add(MakeButton("Refresh Library", (s, e) => RefreshLibrary(), 120));
            _lblStatus.Margin = new Padding(10, 7, 0, 0);
            bottom.Controls.Add(_lblStatus);

            // --- middle: library | buttons | pages + details ---
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10, 4, 10, 4), ColumnCount = 3, RowCount = 1 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            var left = new Panel { Dock = DockStyle.Fill };
            var capLib = Caption("Page library (Canvas/pages)");
            capLib.Dock = DockStyle.Top;
            _lstLibrary.Dock = DockStyle.Fill;
            _lstLibrary.DoubleClick += (s, e) => AddSelected();
            _lstLibrary.SelectedIndexChanged += (s, e) => QueuePreview(_lstLibrary.SelectedItem as string);
            // search: filters the list while typing (space = several words, all must match)
            var searchRow = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(0, 2, 0, 4) };
            _txtSearch.Dock = DockStyle.Fill;
            _txtSearch.TextChanged += (s, e) => FilterLibrary();
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { _txtSearch.Clear(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down && _lstLibrary.Items.Count > 0) { _lstLibrary.Focus(); _lstLibrary.SelectedIndex = 0; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Enter && _lstLibrary.Items.Count > 0)
                {
                    if (_lstLibrary.SelectedIndex < 0) _lstLibrary.SelectedIndex = 0;
                    AddSelected(); e.SuppressKeyPress = true;
                }
            };
            searchRow.Controls.Add(_txtSearch);
            left.Controls.Add(_lstLibrary);
            left.Controls.Add(searchRow);
            left.Controls.Add(capLib);

            var mid = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8, 30, 0, 0) };
            mid.Controls.Add(MakeButton("Add >", (s, e) => AddSelected()));
            mid.Controls.Add(MakeButton("< Remove", (s, e) => RemoveSelected()));
            mid.Controls.Add(MakeButton("Up", (s, e) => MovePage(-1)));
            mid.Controls.Add(MakeButton("Down", (s, e) => MovePage(1)));

            var right = new Panel { Dock = DockStyle.Fill };
            var capPages = Caption($"Sidebar pages (max {CanvasConfig.MaxPages}, top first)");
            capPages.Dock = DockStyle.Top;
            // at most 4 entries, so only as tall as that; the preview gets the rest
            _lstPages.Dock = DockStyle.Top;
            _lstPages.Height = _lstPages.ItemHeight * CanvasConfig.MaxPages + 8;
            _lstPages.SelectedIndexChanged += (s, e) =>
            {
                if (!_loading) ShowDetails();
                int i = _lstPages.SelectedIndex;
                if (i >= 0 && i < _cfg.Pages.Count) QueuePreview(_cfg.Pages[i].Dir);
            };

            var details = new TableLayoutPanel { Dock = DockStyle.Top, Height = 76, ColumnCount = 4, RowCount = 2, Padding = new Padding(0, 6, 0, 0) };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            _txtName.Dock = DockStyle.Fill;
            _txtIcon.Dock = DockStyle.Fill;
            _txtName.TextChanged += (s, e) => EditDetails();
            _txtIcon.TextChanged += (s, e) => EditDetails();
            // list text only when leaving the box -- changing a ListBox item
            // drops the selection (and with it the text box's focus)
            _txtName.Leave += (s, e) => UpdateListText();
            details.Controls.Add(new Label { Text = "Name", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
            details.Controls.Add(_txtName, 1, 0);
            details.Controls.Add(new Label { Text = "Icon", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 1);
            details.Controls.Add(_txtIcon, 1, 1);
            details.Controls.Add(MakeButton("Browse...", (s, e) => BrowseIcon(), 76), 2, 1);
            details.Controls.Add(_picIcon, 3, 0);
            details.SetRowSpan(_picIcon, 2);   // preview = exactly what the ESP sidebar gets

            var prevHead = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 8, 0, 0), WrapContents = false };
            _lblPreview.Text = "預覽：點選左邊或上面的頁面";
            _lblPreview.Margin = new Padding(0, 6, 8, 0);
            _btnPageView = MakeButton("頁面", (s, e) => SetSettingsView(false), 56);
            _btnSetView = MakeButton("設定", (s, e) => SetSettingsView(true), 56);
            prevHead.Controls.Add(_btnPageView);
            prevHead.Controls.Add(_btnSetView);
            prevHead.Controls.Add(MakeButton("重新整理", (s, e) => { UpdateDevice(); ShowPreview(_previewDir, true); }, 80));
            prevHead.Controls.Add(_lblPreview);
            _lblPreview.Margin = new Padding(8, 6, 0, 0);
            var prevArea = new Panel { Dock = DockStyle.Fill };
            prevArea.Controls.Add(_sidePic);
            prevArea.Controls.Add(_previewBox);
            _previewBox.Controls.Add(_setPic);
            _sidePic.MouseClick += SidebarClick;
            prevArea.Resize += (s, e) => LayoutPreview(prevArea);
            _previewTimer.Tick += (s, e) => { _previewTimer.Stop(); ShowPreview(_pendingPreview, false); };

            // docked controls: the last one added is laid out first
            right.Controls.Add(prevArea);
            right.Controls.Add(prevHead);
            right.Controls.Add(details);
            right.Controls.Add(_lstPages);
            right.Controls.Add(capPages);

            grid.Controls.Add(left, 0, 0);
            grid.Controls.Add(mid, 1, 0);
            grid.Controls.Add(right, 2, 0);

            // --- Settings page look on the ESP (sent as images, not on the SD card) ---
            var look = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(10, 6, 10, 0), WrapContents = false };
            _txtSetBg.TextChanged += (s, e) => EditLook();
            _txtSetIcon.TextChanged += (s, e) => EditLook();
            look.Controls.Add(new Label { Text = "Settings  背景", AutoSize = true, Margin = new Padding(0, 7, 4, 0) });
            look.Controls.Add(_txtSetBg);
            look.Controls.Add(MakeButton("...", (s, e) => BrowseInto(_txtSetBg, "Settings background (scaled to 720 x 480)"), 30));
            look.Controls.Add(new Label { Text = "齒輪圖示", AutoSize = true, Margin = new Padding(14, 7, 4, 0) });
            look.Controls.Add(_txtSetIcon);
            look.Controls.Add(MakeButton("...", (s, e) => BrowseInto(_txtSetIcon, "Gear icon (shown at 64 x 64)"), 30));

            Controls.Add(grid);
            Controls.Add(look);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        // ------------------------------------------------------------------
        // Config files
        // ------------------------------------------------------------------

        private void RefreshConfigs(string select)
        {
            _loading = true;
            _cboConfig.Items.Clear();
            foreach (var c in CanvasPaths.ListConfigs()) _cboConfig.Items.Add(c);
            _loading = false;

            int i = select == null ? -1 : _cboConfig.Items.IndexOf(select);
            if (i < 0 && _cboConfig.Items.Count > 0) i = 0;
            if (i >= 0) _cboConfig.SelectedIndex = i;   // -> OpenSelectedConfig
            else { _cfg = new CanvasConfig(); _cfgName = null; RefreshPages(); }
            UpdateActiveLabel();
        }

        private void OpenSelectedConfig()
        {
            if (!ConfirmDiscard()) { _loading = true; _cboConfig.SelectedItem = _cfgName; _loading = false; return; }
            string name = _cboConfig.SelectedItem as string;
            if (name == null) return;
            try
            {
                _cfg = CanvasConfig.Load(Path.Combine(CanvasPaths.Configs, name));
                _cfgName = name;
                _dirty = false;
                SetStatus($"Opened {name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Cannot read {name}:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RefreshPages();
        }

        private void NewConfig()
        {
            if (!ConfirmDiscard()) return;
            string name = FormPageNamePrompt.Show(this, "New config name", "home");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = SafeName(name);
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) name += ".json";
            string path = Path.Combine(CanvasPaths.Configs, name);
            if (File.Exists(path))
            {
                MessageBox.Show(this, $"{name} already exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            new CanvasConfig().Save(path);
            _dirty = false;
            RefreshConfigs(name);
        }

        private void Save()
        {
            UpdateListText();
            if (_cfgName == null) { NewConfig(); return; }
            try
            {
                _cfg.Save(Path.Combine(CanvasPaths.Configs, _cfgName));
                _dirty = false;
                if (CanvasPaths.ActiveConfigName == null) CanvasPaths.ActiveConfigName = _cfgName;
                UpdateActiveLabel();
                SetStatus($"Saved {_cfgName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Save failed:\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UseThisConfig()
        {
            if (_cfgName == null) return;
            if (_dirty) Save();
            CanvasPaths.ActiveConfigName = _cfgName;
            UpdateActiveLabel();
            SetStatus($"Now using {_cfgName}");
        }

        private void UpdateActiveLabel()
        {
            string a = CanvasPaths.ActiveConfigName;
            _lblActive.Text = a == null ? "In use: (none)" : $"In use: {a}";
        }

        private bool ConfirmDiscard()
        {
            if (!_dirty) return true;
            var r = MessageBox.Show(this, $"Save changes to {_cfgName ?? "this config"}?", Text,
                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.Yes) Save();
            _dirty = false;
            return true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateIconPreview();   // the hint needs the TextBox handle, which exists only now
            SetHint(_txtSearch, $"搜尋頁面…（共 {_libAll.Count} 個）");
            _lstPages.Height = _lstPages.ItemHeight * CanvasConfig.MaxPages + 8;   // real font / DPI known now
            _txtSearch.Focus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ConfirmDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------------
        // Pages
        // ------------------------------------------------------------------

        private void RefreshLibrary()
        {
            _libAll = CanvasPaths.ListPageDirs();
            FilterLibrary();
        }

        /// <summary>Shows the page folders matching the search box (every
        /// space-separated word must appear, case-insensitive), keeping the
        /// selection when it still matches.</summary>
        private void FilterLibrary()
        {
            string sel = _lstLibrary.SelectedItem as string;
            var words = _txtSearch.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var hits = _libAll.Where(d => words.All(w => d.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            _lstLibrary.BeginUpdate();
            _lstLibrary.Items.Clear();
            foreach (var d in hits) _lstLibrary.Items.Add(d);
            _lstLibrary.EndUpdate();
            if (sel != null && hits.Contains(sel)) _lstLibrary.SelectedItem = sel;
            SetHint(_txtSearch, $"搜尋頁面…（共 {_libAll.Count} 個）");
            if (words.Length > 0) SetStatus($"{hits.Count} / {_libAll.Count} pages");
        }

        // ------------------------------------------------------------------
        // Preview
        // ------------------------------------------------------------------

        private string _pendingPreview;

        private void QueuePreview(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            _pendingPreview = dir;
            _previewIndex = _cfg.Pages.FindIndex(p => string.Equals(p.Dir, dir, StringComparison.OrdinalIgnoreCase));
            if (_showSettings) SetSettingsView(false); else UpdateDevice();
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        /// <summary>Largest 800 x 480 (5:3) screen that fits, centered:
        /// 80 px sidebar + 720 px content, same proportions as the ESP.</summary>
        private void LayoutPreview(Control area)
        {
            int w = area.ClientSize.Width, h = area.ClientSize.Height;
            if (w * 3 > h * 5) w = h * 5 / 3; else h = w * 3 / 5;
            w = Math.Max(w, 10); h = Math.Max(h, 6);
            int x = (area.ClientSize.Width - w) / 2, side = w * 80 / 800;
            _sidePic.SetBounds(x, 0, side, h);
            _previewBox.SetBounds(x + side, 0, w - side, h);
            ApplyPreviewZoom();
        }

        // ------------------------------------------------------------------
        // ESP screen mock: sidebar + Settings page
        // ------------------------------------------------------------------

        private void SetSettingsView(bool on)
        {
            _showSettings = on;
            if (_web != null) _web.Visible = !on;
            _setPic.Visible = on;
            if (on) _setPic.BringToFront();
            _lblPreview.Text = on ? "預覽：Settings（模擬，字型與實機略有不同）" : (_previewDir != null ? $"預覽：{_previewDir}" : "預覽：點選左邊或上面的頁面");
            UpdateDevice();
        }

        /// <summary>Redraws the sidebar and the Settings mock from the
        /// config being edited (call after any page / icon / look change).</summary>
        private void UpdateDevice()
        {
            var oldSide = _sidePic.Image;
            _sidePic.Image = RenderSidebar();
            oldSide?.Dispose();
            if (_showSettings)
            {
                var oldSet = _setPic.Image;
                _setPic.Image = RenderSettings();
                oldSet?.Dispose();
            }
            if (_btnPageView != null)
            {
                _btnPageView.BackColor = _showSettings ? PanelColor : Color.FromArgb(0, 85, 204);
                _btnSetView.BackColor = _showSettings ? Color.FromArgb(0, 85, 204) : PanelColor;
            }
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
        {
            var g = new GraphicsPath();
            int d = rad * 2;
            g.AddArc(r.X, r.Y, d, d, 180, 90);
            g.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            g.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            g.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            g.CloseFigure();
            return g;
        }

        // sidebar layout on the ESP (ui_canvas.c / ui.c): buttons 64 x 64 at
        // x 8, first at y 8, 80 px apart; gear at (8, 408); selected = 3 px
        // blue outline (#0055cc); background #111111
        private const int SbBtn = 64, SbX = 8, SbY0 = 8, SbStep = 80, GearY = 408;

        private Bitmap RenderSidebar()
        {
            var bmp = new Bitmap(80, 480);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(0x11, 0x11, 0x11));
                var blue = Color.FromArgb(0x00, 0x55, 0xcc);
                for (int i = 0; i < _cfg.Pages.Count && i < CanvasConfig.MaxPages; i++)
                {
                    var r = new Rectangle(SbX, SbY0 + i * SbStep, SbBtn, SbBtn);
                    Bitmap icon = null;
                    try { icon = CanvasEncoder.RenderIcon(_cfg.Pages[i].IconPath, _cfg.Pages[i].Name); } catch { }
                    using (var path = Rounded(r, 8))
                    {
                        g.SetClip(path);
                        if (icon != null) g.DrawImage(icon, r);
                        else using (var b = new SolidBrush(Color.FromArgb(0x2a, 0x2a, 0x2a))) g.FillRectangle(b, r);
                        g.ResetClip();
                        if (!_showSettings && i == _previewIndex)
                            using (var pen = new Pen(blue, 3)) g.DrawPath(pen, Rounded(Rectangle.Inflate(r, 2, 2), 9));
                    }
                    icon?.Dispose();
                }
                // gear: the config's side_icon, or the built-in gear glyph
                var gr = new Rectangle(SbX, GearY, SbBtn, SbBtn);
                Bitmap gearImg = null;
                try { gearImg = CanvasEncoder.RenderIconFile(CanvasPaths.Resolve(_cfg.Settings?.SideIcon)); } catch { }
                using (var path = Rounded(gr, 8))
                {
                    bool glyphSel = _showSettings && gearImg == null;
                    using (var b = new SolidBrush(glyphSel ? blue : Color.FromArgb(0x2a, 0x2a, 0x2a))) g.FillPath(b, path);
                    if (gearImg != null)
                    {
                        g.SetClip(path); g.DrawImage(gearImg, gr); g.ResetClip();
                        if (_showSettings) using (var pen = new Pen(blue, 3)) g.DrawPath(pen, Rounded(Rectangle.Inflate(gr, 2, 2), 9));
                    }
                    else
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        using (var f = new Font("Segoe UI Symbol", 20f, FontStyle.Regular, GraphicsUnit.Pixel))
                        using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            g.DrawString("\u2699", f, Brushes.White, gr, fmt);
                    }
                }
                gearImg?.Dispose();
            }
            return bmp;
        }

        // Settings page on the ESP (ui_settings.c): background image scaled
        // to cover + 50 % black mask (else #222222); 16 px padding; header 40
        // px (close button, "Settings" centered); rows 48 px, 8 px apart,
        // #2a2a2a at 50 %, radius 6, label 16 px at x + 12
        private Bitmap RenderSettings()
        {
            var bmp = new Bitmap(720, 480);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(0x22, 0x22, 0x22));
                Bitmap bg = null;
                try { bg = CanvasEncoder.LoadCover(CanvasPaths.Resolve(_cfg.Settings?.BgImage), 720, 480); } catch { }
                if (bg != null)
                {
                    g.DrawImage(bg, 0, 0, 720, 480);
                    using (var mask = new SolidBrush(Color.FromArgb(128, 0, 0, 0))) g.FillRectangle(mask, 0, 0, 720, 480);
                    bg.Dispose();
                }
                var btnBrush = new SolidBrush(Color.FromArgb(128, 0x2a, 0x2a, 0x2a));
                using (btnBrush)
                using (var title = new Font("Segoe UI", 20f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var rowFont = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var sym = new Font("Segoe UI Symbol", 16f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var left = new StringFormat { LineAlignment = StringAlignment.Center })
                using (var txt = new SolidBrush(Color.FromArgb(0xee, 0xee, 0xee)))
                {
                    var close = new Rectangle(16, 18, 36, 36);
                    using (var path = Rounded(close, 6)) g.FillPath(btnBrush, path);
                    g.DrawString("\u2715", sym, txt, close, center);
                    g.DrawString("Settings", title, Brushes.White, new RectangleF(16, 16, 688, 40), center);
                    string[] rows = { "System", "Bluetooth", "Custom", "Mode" };
                    for (int i = 0; i < rows.Length; i++)
                    {
                        var r = new Rectangle(16, 68 + i * 56, 688, 48);
                        using (var path = Rounded(r, 6)) g.FillPath(btnBrush, path);
                        g.DrawString(rows[i], rowFont, txt, new RectangleF(r.X + 12, r.Y, 400, r.Height), left);
                        g.DrawString("\u203A", rowFont, txt, new RectangleF(r.Right - 30, r.Y, 20, r.Height), center);
                    }
                }
            }
            return bmp;
        }

        /// <summary>Tap on the sidebar mock: a page icon previews that page,
        /// the gear shows the Settings mock -- like on the ESP.</summary>
        private void SidebarClick(object sender, MouseEventArgs e)
        {
            if (_sidePic.Height <= 0) return;
            int y = e.Y * 480 / _sidePic.Height;
            if (y >= GearY - 4) { SetSettingsView(true); return; }
            int i = (y - SbY0 + (SbStep - SbBtn) / 2) / SbStep;
            if (i < 0 || i >= _cfg.Pages.Count) return;
            _lstPages.SelectedIndex = i;      // -> details + preview of that page
            SetSettingsView(false);
        }

        /// <summary>Zoom so the page's CSS viewport is exactly 720 x 480
        /// whatever the box size and monitor DPI.</summary>
        private void ApplyPreviewZoom()
        {
            if (_web?.CoreWebView2 == null || _previewBox.Width < 10) return;
            double dpi = DeviceDpi / 96.0;
            _web.ZoomFactor = _previewBox.Width / (720.0 * dpi);
        }

        private async void ShowPreview(string dir, bool reload)
        {
            if (string.IsNullOrEmpty(dir)) return;
            try
            {
                if (_webInit == null) _webInit = InitPreviewAsync();
                await _webInit;
                if (_web?.CoreWebView2 == null || IsDisposed) return;
                string url = CanvasRenderer.HostUrl + dir.Trim('/').Replace('\\', '/') + "/index.html";
                if (reload || dir != _previewDir) _web.CoreWebView2.Navigate(url);
                _previewDir = dir;
                if (!_showSettings) _lblPreview.Text = $"預覽：{dir}";
            }
            catch (Exception ex)
            {
                _lblPreview.Text = "預覽無法啟動：" + ex.Message;
            }
        }

        private async Task InitPreviewAsync()
        {
            // own data folder: the ESP renderer's WebView2 uses different
            // browser options, and two environments on one folder must match
            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ESDeckPC", "WebView2Preview");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Black };
            _previewBox.Controls.Add(_web);
            _web.Visible = !_showSettings;
            _setPic.BringToFront();
            await _web.EnsureCoreWebView2Async(env);
            var core = _web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.SetVirtualHostNameToFolderMapping(CanvasRenderer.HostName, CanvasPaths.Root,
                                                   CoreWebView2HostResourceAccessKind.Allow);
            // no scroll bars, same as on the ESP
            await core.AddScriptToExecuteOnDocumentCreatedAsync(
                "document.addEventListener('DOMContentLoaded', () => { const s = document.createElement('style');" +
                " s.textContent = '::-webkit-scrollbar{display:none}'; document.head.appendChild(s); });");
            core.NavigationCompleted += (s, e) => ApplyPreviewZoom();
            ApplyPreviewZoom();
        }

        private void ShowLook()
        {
            _loading = true;
            _txtSetBg.Text = _cfg.Settings?.BgImage ?? "";
            _txtSetIcon.Text = _cfg.Settings?.SideIcon ?? "";
            _loading = false;
        }

        private void EditLook()
        {
            if (_loading) return;
            if (_cfg.Settings == null) _cfg.Settings = new CanvasSettingsLook();
            _cfg.Settings.BgImage = _txtSetBg.Text.Trim();
            _cfg.Settings.SideIcon = _txtSetIcon.Text.Trim();
            _dirty = true;
            UpdateDevice();
        }

        private void BrowseInto(TextBox box, string title)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
                InitialDirectory = CanvasPaths.Root,
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string rel = CanvasPaths.MakeRelative(dlg.FileName);
                if (Path.IsPathRooted(rel))
                    SetStatus("Tip: keep images inside the Canvas folder so it can be copied to another PC");
                box.Text = rel;
            }
        }

        private void RefreshPages(int select = -1)
        {
            _lstPages.Items.Clear();
            foreach (var p in _cfg.Pages)
                _lstPages.Items.Add(string.IsNullOrWhiteSpace(p.Name) ? p.Dir : $"{p.Name}   ({p.Dir})");
            if (select >= 0 && select < _lstPages.Items.Count) _lstPages.SelectedIndex = select;
            ShowDetails();
            ShowLook();
            UpdateDevice();
        }

        private void AddSelected()
        {
            string dir = _lstLibrary.SelectedItem as string;
            if (dir == null) return;
            if (_cfg.Pages.Count >= CanvasConfig.MaxPages)
            {
                SetStatus($"At most {CanvasConfig.MaxPages} pages");
                return;
            }
            string name = Path.GetFileName(dir.TrimEnd('/'));
            _cfg.Pages.Add(new CanvasPageEntry { Dir = dir, Name = name });
            _dirty = true;
            RefreshPages(_cfg.Pages.Count - 1);
        }

        private void RemoveSelected()
        {
            int i = _lstPages.SelectedIndex;
            if (i < 0) return;
            _cfg.Pages.RemoveAt(i);
            _dirty = true;
            RefreshPages(Math.Min(i, _cfg.Pages.Count - 1));
        }

        private void MovePage(int delta)
        {
            int i = _lstPages.SelectedIndex, j = i + delta;
            if (i < 0 || j < 0 || j >= _cfg.Pages.Count) return;
            var p = _cfg.Pages[i];
            _cfg.Pages.RemoveAt(i);
            _cfg.Pages.Insert(j, p);
            _dirty = true;
            RefreshPages(j);
        }

        /// <summary>Sidebar button preview (same renderer as what's sent to
        /// the ESP) and the hint telling what an empty Icon field means.</summary>
        private void UpdateIconPreview()
        {
            int i = _lstPages.SelectedIndex;
            var old = _picIcon.Image;
            _picIcon.Image = null;
            old?.Dispose();
            if (i < 0 || i >= _cfg.Pages.Count) { SetHint(_txtIcon, ""); return; }

            var p = _cfg.Pages[i];
            string def = Path.Combine(CanvasPaths.Resolve(p.Dir) ?? "", "icon.png");
            SetHint(_txtIcon, File.Exists(def)
                ? $"預設：{p.Dir.TrimEnd('/')}/icon.png"
                : "預設：顯示名稱文字");
            try { _picIcon.Image = CanvasEncoder.RenderIcon(p.IconPath, p.Name); }
            catch { /* unreadable image: no preview */ }
        }

        private void ShowDetails()
        {
            int i = _lstPages.SelectedIndex;
            _loading = true;
            _txtName.Enabled = _txtIcon.Enabled = i >= 0;
            _txtName.Text = i >= 0 ? _cfg.Pages[i].Name : "";
            _txtIcon.Text = i >= 0 ? _cfg.Pages[i].Icon : "";
            _loading = false;
            UpdateIconPreview();
        }

        private void EditDetails()
        {
            int i = _lstPages.SelectedIndex;
            if (_loading || i < 0) return;
            _cfg.Pages[i].Name = _txtName.Text.Trim();
            _cfg.Pages[i].Icon = _txtIcon.Text.Trim();
            _dirty = true;
            UpdateIconPreview();
            UpdateDevice();
        }

        private void UpdateListText()
        {
            int i = _lstPages.SelectedIndex;
            if (i < 0 || i >= _cfg.Pages.Count) return;
            string text = string.IsNullOrWhiteSpace(_cfg.Pages[i].Name)
                ? _cfg.Pages[i].Dir : $"{_cfg.Pages[i].Name}   ({_cfg.Pages[i].Dir})";
            if ((string)_lstPages.Items[i] == text) return;
            _loading = true;
            _lstPages.Items[i] = text;
            _lstPages.SelectedIndex = i;   // setting an item drops the selection
            _loading = false;
        }

        private void BrowseIcon()
        {
            int i = _lstPages.SelectedIndex;
            if (i < 0) return;
            using (var dlg = new OpenFileDialog
            {
                Title = "Sidebar icon (shown at 64 x 64)",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
                InitialDirectory = CanvasPaths.Resolve(_cfg.Pages[i].Dir) ?? CanvasPaths.Root,
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string rel = CanvasPaths.MakeRelative(dlg.FileName);
                if (Path.IsPathRooted(rel))
                    SetStatus("Tip: keep icons inside the Canvas folder so it can be copied to another PC");
                _txtIcon.Text = rel;
            }
        }

        /// <summary>Creates pages/&lt;name&gt;/index.html from a starter template.</summary>
        private void NewPage()
        {
            string name = FormPageNamePrompt.Show(this, "New page folder name", "mypage");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = SafeName(name);
            string dir = Path.Combine(CanvasPaths.Pages, name);
            if (Directory.Exists(dir))
            {
                MessageBox.Show(this, $"pages/{name} already exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "index.html"), PageTemplate.Replace("{{NAME}}", name));
            RefreshLibrary();
            _lstLibrary.SelectedItem = $"pages/{name}";
            SetStatus($"Created pages/{name}/index.html");
        }

        private static string SafeName(string name) =>
            Regex.Replace(name.Trim(), @"[\\/:*?""<>|\s]+", "_");

        private void SetStatus(string text) => _lblStatus.Text = text;

        private const string PageTemplate = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<link rel=""stylesheet"" href=""../../shared/base.css"">
<style>
  /* the page is 720 x 480 */
  .big { font-size: 64px; font-weight: 700; }
</style>
</head>
<body>
  <div class=""center"">
    <div class=""big"">{{NAME}}</div>
    <div class=""dim"" id=""time""></div>
    <button class=""btn"" onclick=""esdeck.action({ action: 'launch', target: 'notepad.exe' })"">Notepad</button>
  </div>
<script>
  const t = document.getElementById('time');
  const tick = () => t.textContent = new Date().toLocaleTimeString();
  tick(); setInterval(tick, 1000);
</script>
</body>
</html>
";
    }
}
