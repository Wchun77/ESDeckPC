using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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

        private FontBinLoader _fontTime = null;
        private FontBinLoader _fontSec = null;
        private FontBinLoader _fontDate = null;
        private Bitmap _bgBitmap = null;
        private Bitmap _previewBmp = null;

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
            SetWindowTheme(pnlLeft.Handle, "DarkMode_Explorer", null);
        }

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public FormMonitorEditor()
        {
            InitializeComponent();
            StyleDarkButtons();
            this.splitMain.Panel2.Resize += (s, e) => CenterPreview();
            ApplyConfigToUi(_cfg);
            RefreshPreview();
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
                ApplyConfigToUi(_cfg);
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
                    ApplyConfigToUi(_cfg);
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
        // Font .bin file pickers
        // ------------------------------------------------------------------

        private void BtnFontTime_Click(object sender, EventArgs e)
            => BrowseBin(txtFontTime, ref _fontTime, lblFontTimeNote);

        private void BtnFontSec_Click(object sender, EventArgs e)
            => BrowseBin(txtFontSec, ref _fontSec, lblFontSecNote);

        private void BtnFontDate_Click(object sender, EventArgs e)
            => BrowseBin(txtFontDate, ref _fontDate, lblFontDateNote);

        private void BrowseBin(TextBox txt, ref FontBinLoader cache, Label note)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select LVGL font .bin file";
                dlg.Filter = "LVGL font (*.bin)|*.bin";
                if (!string.IsNullOrEmpty(txt.Text))
                    dlg.InitialDirectory = Path.GetDirectoryName(txt.Text);
                if (dlg.ShowDialog() != DialogResult.OK) return;
                txt.Text = dlg.FileName;
            }
            cache?.Dispose();
            cache = FontBinLoader.Load(txt.Text);
            UpdateFontNote(note, cache);
            RefreshPreview();
        }

        private static void UpdateFontNote(Label lbl, FontBinLoader f)
        {
            if (f == null)
            {
                lbl.Text = "";
                lbl.ForeColor = Color.Gray;
            }
            else
            {
                lbl.Text = $"ascent={f.Ascent}  descent={f.Descent}  lineH={f.LineHeight}";
                lbl.ForeColor = Color.FromArgb(100, 200, 100);
            }
        }

        // ------------------------------------------------------------------
        // Background image
        // ------------------------------------------------------------------

        private void BtnClockBgBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select background image";
                dlg.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _bgBitmap?.Dispose();
                    _bgBitmap = new Bitmap(dlg.FileName);
                    txtClockBgImage.Text = Path.GetFileName(dlg.FileName);
                    RefreshPreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnClockBgClear_Click(object sender, EventArgs e)
        {
            _bgBitmap?.Dispose();
            _bgBitmap = null;
            txtClockBgImage.Text = "";
            RefreshPreview();
        }

        private void BtnSystemBgBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select background image";
                dlg.Filter = "Image files (*.jpg;*.jpeg;)|*.jpg;*.jpeg;;";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    txtSystemBgImage.Text = Path.GetFileName(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSystemBgClear_Click(object sender, EventArgs e)
        {
            txtSystemBgImage.Text = "";
        }

        // ------------------------------------------------------------------
        // Colour pickers
        // ------------------------------------------------------------------

        private void BtnColTime_Click(object sender, EventArgs e) => PickColor(btnColTime);
        private void BtnColColon_Click(object sender, EventArgs e) => PickColor(btnColColon);
        private void BtnColDate_Click(object sender, EventArgs e) => PickColor(btnColDate);
        private void BtnColDay_Click(object sender, EventArgs e) => PickColor(btnColDay);
        private void BtnColSec_Click(object sender, EventArgs e) => PickColor(btnColSec);
        private void BtnColSep_Click(object sender, EventArgs e) => PickColor(btnColSep);

        private void PickColor(Button btn)
        {
            using (var dlg = new ColorDialog { Color = btn.BackColor, FullOpen = true })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                btn.BackColor = dlg.Color;
                btn.ForeColor = ContrastColor(dlg.Color);
                btn.Text = ColorToHex(dlg.Color);
                RefreshPreview();
            }
        }

        // ------------------------------------------------------------------
        // Sep width
        // ------------------------------------------------------------------

        private void NudSepWidth_ValueChanged(object sender, EventArgs e)
            => RefreshPreview();

        // ------------------------------------------------------------------
        // Preview
        // ------------------------------------------------------------------

        private void RefreshPreview()
        {
            ReadUiIntoConfig();
            var newBmp = MonitorClockRenderer.Render(
                _cfg.Clock, _fontTime, _fontSec, _fontDate, _bgBitmap);
            picPreview.Image = null;  // detach before dispose
            _previewBmp?.Dispose();
            _previewBmp = newBmp;
            picPreview.Image = _previewBmp;
            CenterPreview();

            string t = _fontTime != null ? $"time lineH={_fontTime.LineHeight} {_fontTime.DebugInfo()}" : "time=no font";
            string s = _fontSec != null ? $"sec lineH={_fontSec.LineHeight} {_fontSec.DebugInfo()}" : "sec=no font";
            string d = _fontDate != null ? $"date lineH={_fontDate.LineHeight} {_fontDate.DebugInfo()}" : "date=no font";
            SetStatus($"{t}  |  {s}  |  {d}");
        }

        private void CenterPreview()
        {
            var panel = splitMain.Panel2;
            int x = Math.Max(0, (panel.ClientSize.Width - picPreview.Width) / 2);
            int y = Math.Max(0, (panel.ClientSize.Height - picPreview.Height - lblStatus.Height) / 2);
            picPreview.Location = new Point(x, y);
        }

        private void SetStatus(string msg) => lblStatus.Text = msg;

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        private void ApplyConfigToUi(MonitorConfig cfg)
        {
            var c = cfg.Clock;
            txtClockBgImage.Text = c.BgImage ?? "";
            txtFontTime.Text = c.FontTime ?? "";
            txtFontDate.Text = c.FontDate ?? "";
            txtFontSec.Text = c.FontSec ?? "";

            var s = cfg.System;
            txtSystemBgImage.Text = s.BgImage ?? "";

            SetColorButton(btnColTime, c.ColTime);
            SetColorButton(btnColColon, c.ColColon);
            SetColorButton(btnColDate, c.ColDate);
            SetColorButton(btnColDay, c.ColDay);
            SetColorButton(btnColSec, c.ColSec);
            SetColorButton(btnColSep, c.SepColor);
            nudSepWidth.Value = Math.Max(nudSepWidth.Minimum,
                                Math.Min(nudSepWidth.Maximum, c.SepWidth)); // 這個會觸發 ReadUiIntoConfig 所以要最後
        }

        private void ReadUiIntoConfig()
        {
            var c = _cfg.Clock;
            if (!string.IsNullOrEmpty(txtFontTime.Text))
                c.FontTime = Path.GetFileName(txtFontTime.Text);
            if (!string.IsNullOrEmpty(txtFontSec.Text))
                c.FontSec = Path.GetFileName(txtFontSec.Text);
            if (!string.IsNullOrEmpty(txtFontDate.Text))
                c.FontDate = Path.GetFileName(txtFontDate.Text);

            c.ColTime = ColorToHex(btnColTime.BackColor);
            c.ColColon = ColorToHex(btnColColon.BackColor);
            c.ColDate = ColorToHex(btnColDate.BackColor);
            c.ColDay = ColorToHex(btnColDay.BackColor);
            c.ColSec = ColorToHex(btnColSec.BackColor);
            c.SepColor = ColorToHex(btnColSep.BackColor);
            c.SepWidth = (int)nudSepWidth.Value;
            c.BgImage = txtClockBgImage.Text;

            var s = _cfg.System;
            s.BgImage = txtSystemBgImage.Text;
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

            foreach (var btn in new[] { btnJsonNew, btnJsonOpen, btnJsonSave,
                                        btnFontTime, btnFontSec, btnFontDate,
                                        btnClockBgBrowse, btnClockBgClear })
            {
                btn.BackColor = darkBg;
                btn.ForeColor = lightFg;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.Font = font;
            }

            var defCol = Color.FromArgb(0xF0, 0xF2, 0xFF);
            foreach (var btn in new[] { btnColTime, btnColColon, btnColDate,
                                        btnColDay, btnColSec, btnColSep })
            {
                btn.BackColor = defCol;
                btn.ForeColor = ContrastColor(defCol);
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.Text = "F0F2FF";
            }

            foreach (var lbl in new[] { lblColTime, lblColColon, lblColDate,
                                        lblColDay, lblColSec, lblColSep, lblSepWidthLbl })
                lbl.ForeColor = Color.FromArgb(200, 200, 200);

            foreach (var lbl in new[] { lblFontTime, lblFontSec, lblFontDate })
                lbl.ForeColor = Color.FromArgb(180, 180, 180);
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

        private static void SetColorButton(Button btn, string hex)
        {
            var col = HexToColor(hex, Color.FromArgb(0xF0, 0xF2, 0xFF));
            btn.BackColor = col;
            btn.ForeColor = ContrastColor(col);
            btn.Text = hex ?? "";
        }

        private static Color HexToColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                try
                {
                    int v = Convert.ToInt32(hex, 16);
                    return Color.FromArgb(0xFF, (v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
                }
                catch { }
            }
            return fallback;
        }

        private static string ColorToHex(Color c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";

        private static Color ContrastColor(Color bg)
            => (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) > 128
               ? Color.Black : Color.White;

        // ------------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------------

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _fontTime?.Dispose();
            _fontSec?.Dispose();
            _fontDate?.Dispose();
            _bgBitmap?.Dispose();
            _previewBmp?.Dispose();
            base.OnFormClosed(e);
        }
    }
}