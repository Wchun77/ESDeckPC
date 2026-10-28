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
    /// Modes > Canvas Editor: pick which pages (up to 4) a config shows on the
    /// ESP sidebar, their order, names and icons, and which config is in use.
    /// Everything is saved under the Canvas folder; while the ESP is in
    /// Canvas mode, saving shows up there right away (CanvasLink watches the
    /// folder).
    ///
    /// Layout: FormCanvasEditor.Designer.cs (open in the designer). Done here
    /// because the designer can't: hint texts, dark scroll bars, the upper
    /// band's height, the 800 x 480 preview's size and its WebView2.
    /// </summary>
    public partial class FormCanvasEditor : Form
    {
        private static readonly Color PanelColor = Color.FromArgb(40, 40, 40);
        private static readonly Color ActiveBlue = ActiveBlue;

        private List<string> _libAll = new List<string>();   // every page folder; lstLibrary shows the matches

        // Grey hint text in an empty TextBox (.NET Framework has no PlaceholderText).
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private static void SetHint(TextBox box, string hint)
        {
            if (box.IsHandleCreated)
                SendMessage(box.Handle, EM_SETCUEBANNER, (IntPtr)1, hint ?? "");   // 1 = keep showing while focused
        }

        // Dark scroll bars on the list boxes (Windows 10 1809+; ignored elsewhere).
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private static void DarkScrollBars(Control c)
        {
            c.HandleCreated += (s, e) => { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
            if (c.IsHandleCreated) { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } }
        }

        // Preview: the whole ESP screen at 800 x 480 (fixed in the designer,
        // like the other modes' previews) -- picSide is the sidebar
        // (page icons + gear, drawn like the ESP draws them), panelPreviewBox
        // the page (its own WebView2, separate from the one that renders for
        // the ESP, with a read-only esdeck: real stats / media, actions not
        // run) or the Settings mock (picSettings).
        private bool _showSettings;
        private int _previewIndex = -1;       // sidebar slot being previewed, -1 = a library page not in this config
        private WebView2 _web;
        private Task _webInit;
        private string _previewDir;
        private readonly CanvasBridge _liveBridge;   // ESP renderer's bridge; preview gets a read-only copy

        private CanvasConfig _cfg = new CanvasConfig();
        private string _cfgName;        // file name in configs/, null = unsaved new
        private bool _dirty;
        private bool _loading;

        public FormCanvasEditor(CanvasBridge liveBridge) : this()
        {
            _liveBridge = liveBridge;
        }

        public FormCanvasEditor()
        {
            InitializeComponent();
            lblPages.Text = $"Sidebar pages (max {CanvasConfig.MaxPages}, top first)";
            DarkScrollBars(lstLibrary);
            DarkScrollBars(lstPages);

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
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------------
        // Designer event handlers
        // ------------------------------------------------------------------

        private void cboConfig_SelectedIndexChanged(object sender, EventArgs e) { if (!_loading) OpenSelectedConfig(); }
        private void btnNewConfig_Click(object sender, EventArgs e) => NewConfig();
        private void btnSave_Click(object sender, EventArgs e) => Save();
        private void btnUse_Click(object sender, EventArgs e) => UseThisConfig();

        private void btnNewPage_Click(object sender, EventArgs e) => NewPage();
        private void btnOpenFolder_Click(object sender, EventArgs e) => Process.Start("explorer.exe", $"\"{CanvasPaths.Root}\"");
        private void btnRefreshLib_Click(object sender, EventArgs e) => RefreshLibrary();

        private void btnAdd_Click(object sender, EventArgs e) => AddSelected();
        private void btnRemove_Click(object sender, EventArgs e) => RemoveSelected();
        private void btnUp_Click(object sender, EventArgs e) => MovePage(-1);
        private void btnDown_Click(object sender, EventArgs e) => MovePage(1);

        private void lstLibrary_DoubleClick(object sender, EventArgs e) => AddSelected();
        private void lstLibrary_SelectedIndexChanged(object sender, EventArgs e) => QueuePreview(lstLibrary.SelectedItem as string);
        // items stay "pages/xxx" (what config.json stores); only the display drops the prefix
        private void lstLibrary_Format(object sender, ListControlConvertEventArgs e) => e.Value = PageLabel(e.ListItem as string);

        // search: filters the list while typing (space = several words, all must match)
        private void txtSearch_TextChanged(object sender, EventArgs e) => FilterLibrary();

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down && lstLibrary.Items.Count > 0) { lstLibrary.Focus(); lstLibrary.SelectedIndex = 0; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter && lstLibrary.Items.Count > 0)
            {
                if (lstLibrary.SelectedIndex < 0) lstLibrary.SelectedIndex = 0;
                AddSelected();
                e.SuppressKeyPress = true;
            }
        }

        private void lstPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loading) ShowDetails();
            int i = lstPages.SelectedIndex;
            if (i >= 0 && i < _cfg.Pages.Count) QueuePreview(_cfg.Pages[i].Dir);
        }

        private void txtName_TextChanged(object sender, EventArgs e) => EditDetails();
        private void txtIcon_TextChanged(object sender, EventArgs e) => EditDetails();
        // list text only when leaving the box -- changing a ListBox item
        // drops the selection (and with it the text box's focus)
        private void txtName_Leave(object sender, EventArgs e) => UpdateListText();
        private void btnBrowseIcon_Click(object sender, EventArgs e) => BrowseIcon();

        private void btnPageView_Click(object sender, EventArgs e) => SetSettingsView(false);
        private void btnSetView_Click(object sender, EventArgs e) => SetSettingsView(true);
        private void btnReload_Click(object sender, EventArgs e) { UpdateDevice(); ShowPreview(_previewDir, true); }
        private void picSide_MouseClick(object sender, MouseEventArgs e) => SidebarClick(sender, e);
        private void previewTimer_Tick(object sender, EventArgs e) { previewTimer.Stop(); ShowPreview(_pendingPreview, false); }

        private void txtSetBg_TextChanged(object sender, EventArgs e) => EditLook();
        private void txtSetIcon_TextChanged(object sender, EventArgs e) => EditLook();
        private void btnSetBg_Click(object sender, EventArgs e) => BrowseInto(txtSetBg, "Settings background (scaled to 720 x 480)");
        private void btnSetIcon_Click(object sender, EventArgs e) => BrowseInto(txtSetIcon, "Gear icon (shown at 64 x 64)");

        // ------------------------------------------------------------------
        // Config files
        // ------------------------------------------------------------------

        private void RefreshConfigs(string select)
        {
            _loading = true;
            cboConfig.Items.Clear();
            foreach (var c in CanvasPaths.ListConfigs()) cboConfig.Items.Add(c);
            _loading = false;

            int i = select == null ? -1 : cboConfig.Items.IndexOf(select);
            if (i < 0 && cboConfig.Items.Count > 0) i = 0;
            if (i >= 0) cboConfig.SelectedIndex = i;   // -> OpenSelectedConfig
            else { _cfg = new CanvasConfig(); _cfgName = null; RefreshPages(); }
            UpdateActiveLabel();
        }

        private void OpenSelectedConfig()
        {
            if (!ConfirmDiscard()) { _loading = true; cboConfig.SelectedItem = _cfgName; _loading = false; return; }
            string name = cboConfig.SelectedItem as string;
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
            lblActive.Text = a == null ? "In use: (none)" : $"In use: {a}";
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // sizes after DPI scaling are known now, before the window shows
            lstPages.Height = lstPages.ItemHeight * CanvasConfig.MaxPages + 8;
            FitTopBand();
            PerformLayout();
            FitWindowToPreview();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateIconPreview();   // the hint needs the TextBox handle, which exists only now
            SetHint(txtSearch, $"Search pages... ({_libAll.Count} total)");
            txtSearch.Focus();
        }

        /// <summary>Default height: just enough for the 800 x 480 preview
        /// (fixed size in the designer, same as the other modes' previews),
        /// as long as the screen has room for it.</summary>
        private void FitWindowToPreview()
        {
            var area = panelPrevArea.ClientSize;
            if (area.Width <= 0) return;
            int wantH = panelPreviewBox.Bottom + 4;
            var wa = Screen.FromControl(this).WorkingArea;
            int h = Height + (wantH - area.Height);
            Height = Math.Max(MinimumSize.Height, Math.Min(h, wa.Height));
            Location = new Point(wa.Left + Math.Max(0, (wa.Width - Width) / 2), wa.Top + Math.Max(0, (wa.Height - Height) / 2));
        }

        /// <summary>Upper band = caption + 4-row sidebar list + Name/Icon
        /// details (the right column's natural height); the library on the
        /// left shares it and scrolls. Everything else goes to the preview.</summary>
        private void FitTopBand()
        {
            if (tableTop == null) return;
            var pad = tableTop.Padding;
            int right = lblPages.Height + lstPages.Height + tableDetails.Height;
            // Add / Remove / Up / Down must fit too (summed by hand: a docked
            // flow panel's preferred size follows its current height)
            int buttons = flowMid.Padding.Vertical;
            foreach (Control c in flowMid.Controls) buttons += c.Height + c.Margin.Vertical;
            tableTop.Height = Math.Max(right, buttons) + pad.Vertical + 4;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Windows shutdown / logoff: no Save? prompt (it would block
            // shutdown with nobody there to answer) -- unsaved changes are dropped.
            if (e.CloseReason != CloseReason.WindowsShutDown && !ConfirmDiscard()) e.Cancel = true;
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
            string sel = lstLibrary.SelectedItem as string;
            var words = txtSearch.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var hits = _libAll.Where(d => words.All(w => PageLabel(d).IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            lstLibrary.BeginUpdate();
            lstLibrary.Items.Clear();
            foreach (var d in hits) lstLibrary.Items.Add(d);
            lstLibrary.EndUpdate();
            if (sel != null && hits.Contains(sel)) lstLibrary.SelectedItem = sel;
            SetHint(txtSearch, $"Search pages... ({_libAll.Count} total)");
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
            previewTimer.Stop();
            previewTimer.Start();
        }

        // ------------------------------------------------------------------
        // ESP screen mock: sidebar + Settings page
        // ------------------------------------------------------------------

        private void SetSettingsView(bool on)
        {
            _showSettings = on;
            if (_web != null) _web.Visible = !on;
            picSettings.Visible = on;
            if (on) picSettings.BringToFront();
            lblPreview.Text = on ? "Preview: Settings (mock, fonts differ slightly from the device)" : (_previewDir != null ? $"Preview: {PageLabel(_previewDir)}" : "Preview: select a page on the left or above");
            UpdateDevice();
        }

        /// <summary>Redraws the sidebar and the Settings mock from the
        /// config being edited (call after any page / icon / look change).</summary>
        private void UpdateDevice()
        {
            var oldSide = picSide.Image;
            picSide.Image = RenderSidebar();
            oldSide?.Dispose();
            if (_showSettings)
            {
                var oldSet = picSettings.Image;
                picSettings.Image = RenderSettings();
                oldSet?.Dispose();
            }
            if (btnPageView != null)
            {
                btnPageView.BackColor = _showSettings ? PanelColor : ActiveBlue;
                btnSetView.BackColor = _showSettings ? ActiveBlue : PanelColor;
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

        private Bitmap RenderSidebar() =>
            CanvasSidebar.Render(_cfg, _showSettings ? -1 : _previewIndex, _showSettings);

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
            if (picSide.Height <= 0) return;
            int y = e.Y * 480 / picSide.Height;
            if (y >= GearY - 4) { SetSettingsView(true); return; }
            int i = (y - SbY0 + (SbStep - SbBtn) / 2) / SbStep;
            if (i < 0 || i >= _cfg.Pages.Count) return;
            lstPages.SelectedIndex = i;      // -> details + preview of that page
            SetSettingsView(false);
        }

        /// <summary>Zoom so the page's CSS viewport is exactly 720 x 480
        /// whatever the box size and monitor DPI.</summary>
        private void ApplyPreviewZoom()
        {
            if (_web?.CoreWebView2 == null || panelPreviewBox.Width < 10) return;
            double dpi = DeviceDpi / 96.0;
            _web.ZoomFactor = panelPreviewBox.Width / (720.0 * dpi);
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
                if (!_showSettings) lblPreview.Text = $"Preview: {PageLabel(dir)}";
            }
            catch (Exception ex)
            {
                lblPreview.Text = "Preview failed: " + ex.Message;
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
            panelPreviewBox.Controls.Add(_web);
            _web.Visible = !_showSettings;
            picSettings.BringToFront();
            await _web.EnsureCoreWebView2Async(env);
            var core = _web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.SetVirtualHostNameToFolderMapping(CanvasRenderer.HostName, CanvasPaths.Root,
                                                   CoreWebView2HostResourceAccessKind.Allow);
            // Read-only window.esdeck: pages show real stats / now playing
            // instead of their demo data; actions only return "preview: not run".
            if (_liveBridge != null)
            {
                core.AddHostObjectToScript("esdeck", _liveBridge.PreviewCopy());
                await core.AddScriptToExecuteOnDocumentCreatedAsync(CanvasRenderer.EsdeckScript);
            }
            await core.AddScriptToExecuteOnDocumentCreatedAsync(CanvasRenderer.TouchScreenScript);
            core.NavigationCompleted += (s, e) => ApplyPreviewZoom();
            ApplyPreviewZoom();
        }

        private void ShowLook()
        {
            _loading = true;
            txtSetBg.Text = _cfg.Settings?.BgImage ?? "";
            txtSetIcon.Text = _cfg.Settings?.SideIcon ?? "";
            _loading = false;
        }

        private void EditLook()
        {
            if (_loading) return;
            if (_cfg.Settings == null) _cfg.Settings = new CanvasSettingsLook();
            _cfg.Settings.BgImage = txtSetBg.Text.Trim();
            _cfg.Settings.SideIcon = txtSetIcon.Text.Trim();
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
            lstPages.Items.Clear();
            foreach (var p in _cfg.Pages)
                lstPages.Items.Add(PageText(p));
            if (select < 0 && lstPages.Items.Count > 0) select = 0;   // something to show in details / preview
            if (select >= 0 && select < lstPages.Items.Count) lstPages.SelectedIndex = select;
            ShowDetails();
            ShowLook();
            UpdateDevice();
        }

        private void AddSelected()
        {
            string dir = lstLibrary.SelectedItem as string;
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
            int i = lstPages.SelectedIndex;
            if (i < 0) return;
            _cfg.Pages.RemoveAt(i);
            _dirty = true;
            RefreshPages(Math.Min(i, _cfg.Pages.Count - 1));
        }

        private void MovePage(int delta)
        {
            int i = lstPages.SelectedIndex, j = i + delta;
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
            int i = lstPages.SelectedIndex;
            var old = picIcon.Image;
            picIcon.Image = null;
            old?.Dispose();
            if (i < 0 || i >= _cfg.Pages.Count) { SetHint(txtIcon, ""); return; }

            var p = _cfg.Pages[i];
            string def = Path.Combine(CanvasPaths.Resolve(p.Dir) ?? "", "icon.png");
            SetHint(txtIcon, File.Exists(def)
                ? $"Default: {PageLabel(p.Dir)}/icon.png"
                : "Default: page name as text");
            try { picIcon.Image = CanvasEncoder.RenderIcon(p.IconPath, p.Name); }
            catch { /* unreadable image: no preview */ }
        }

        private void ShowDetails()
        {
            int i = lstPages.SelectedIndex;
            _loading = true;
            txtName.Enabled = txtIcon.Enabled = i >= 0;
            txtName.Text = i >= 0 ? _cfg.Pages[i].Name : "";
            txtIcon.Text = i >= 0 ? _cfg.Pages[i].Icon : "";
            _loading = false;
            UpdateIconPreview();
        }

        private void EditDetails()
        {
            int i = lstPages.SelectedIndex;
            if (_loading || i < 0) return;
            _cfg.Pages[i].Name = txtName.Text.Trim();
            _cfg.Pages[i].Icon = txtIcon.Text.Trim();
            _dirty = true;
            UpdateIconPreview();
            UpdateDevice();
        }

        private void UpdateListText()
        {
            int i = lstPages.SelectedIndex;
            if (i < 0 || i >= _cfg.Pages.Count) return;
            string text = PageText(_cfg.Pages[i]);
            if ((string)lstPages.Items[i] == text) return;
            _loading = true;
            lstPages.Items[i] = text;
            lstPages.SelectedIndex = i;   // setting an item drops the selection
            _loading = false;
        }

        private void BrowseIcon()
        {
            int i = lstPages.SelectedIndex;
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
                txtIcon.Text = rel;
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
                MessageBox.Show(this, $"{name} already exists.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "index.html"), PageTemplate.Replace("{{NAME}}", name));
            RefreshLibrary();
            lstLibrary.SelectedItem = $"pages/{name}";
            SetStatus($"Created {name}");
        }

        /// <summary>Page folder as the user sees it: "pages/clock" -> "clock".</summary>
        private static string PageLabel(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return dir;
            string d = dir.Replace('\\', '/').Trim('/');
            return d.StartsWith("pages/", StringComparison.OrdinalIgnoreCase) ? d.Substring(6) : d;
        }

        private static string PageText(CanvasPageEntry p) =>
            string.IsNullOrWhiteSpace(p.Name) ? PageLabel(p.Dir) : $"{p.Name}   ({PageLabel(p.Dir)})";

        private static string SafeName(string name) =>
            Regex.Replace(name.Trim(), @"[\\/:*?""<>|\s]+", "_");

        private void SetStatus(string text) => lblStatus.Text = text;

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
