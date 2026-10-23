using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
        private readonly ListBox _lstPages = new ListBox { IntegralHeight = false };
        private readonly TextBox _txtName = new TextBox();
        private readonly TextBox _txtIcon = new TextBox();
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
            ClientSize = new Size(780, 510);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            ForeColor = Fg;
            Font = new Font("Segoe UI", 9f);

            BuildLayout();
            CanvasPaths.Ensure();
            RefreshLibrary();
            RefreshConfigs(CanvasPaths.ActiveConfigName);
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
            foreach (Control c in new Control[] { _cboConfig, _lstLibrary, _lstPages, _txtName, _txtIcon, _txtSetBg, _txtSetIcon }) Style(c);
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
            left.Controls.Add(_lstLibrary);
            left.Controls.Add(capLib);

            var mid = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8, 30, 0, 0) };
            mid.Controls.Add(MakeButton("Add >", (s, e) => AddSelected()));
            mid.Controls.Add(MakeButton("< Remove", (s, e) => RemoveSelected()));
            mid.Controls.Add(MakeButton("Up", (s, e) => MovePage(-1)));
            mid.Controls.Add(MakeButton("Down", (s, e) => MovePage(1)));

            var right = new Panel { Dock = DockStyle.Fill };
            var capPages = Caption($"Sidebar pages (max {CanvasConfig.MaxPages}, top first)");
            capPages.Dock = DockStyle.Top;
            _lstPages.Dock = DockStyle.Fill;
            _lstPages.SelectedIndexChanged += (s, e) => ShowDetails();

            var details = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 70, ColumnCount = 3, RowCount = 2, Padding = new Padding(0, 6, 0, 0) };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            _txtName.Dock = DockStyle.Fill;
            _txtIcon.Dock = DockStyle.Fill;
            _txtName.TextChanged += (s, e) => EditDetails();
            _txtIcon.TextChanged += (s, e) => EditDetails();
            details.Controls.Add(new Label { Text = "Name", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 0);
            details.Controls.Add(_txtName, 1, 0);
            details.Controls.Add(new Label { Text = "Icon", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 1);
            details.Controls.Add(_txtIcon, 1, 1);
            details.Controls.Add(MakeButton("Browse...", (s, e) => BrowseIcon(), 76), 2, 1);

            right.Controls.Add(_lstPages);
            right.Controls.Add(details);
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
            _lstLibrary.Items.Clear();
            foreach (var d in CanvasPaths.ListPageDirs()) _lstLibrary.Items.Add(d);
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

        private void ShowDetails()
        {
            int i = _lstPages.SelectedIndex;
            _loading = true;
            _txtName.Enabled = _txtIcon.Enabled = i >= 0;
            _txtName.Text = i >= 0 ? _cfg.Pages[i].Name : "";
            _txtIcon.Text = i >= 0 ? _cfg.Pages[i].Icon : "";
            _loading = false;
        }

        private void EditDetails()
        {
            int i = _lstPages.SelectedIndex;
            if (_loading || i < 0) return;
            _cfg.Pages[i].Name = _txtName.Text.Trim();
            _cfg.Pages[i].Icon = _txtIcon.Text.Trim();
            _dirty = true;
            _loading = true;
            _lstPages.Items[i] = string.IsNullOrWhiteSpace(_cfg.Pages[i].Name)
                ? _cfg.Pages[i].Dir : $"{_cfg.Pages[i].Name}   ({_cfg.Pages[i].Dir})";
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
