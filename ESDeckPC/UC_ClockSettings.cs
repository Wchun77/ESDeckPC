using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class UC_ClockSettings : UserControl
    {
        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private FontBinLoader _fontTime = null;
        private FontBinLoader _fontSec = null;
        private FontBinLoader _fontDate = null;
        private Bitmap _bgBitmap = null;

        public FontBinLoader FontTime => _fontTime;
        public FontBinLoader FontSec => _fontSec;
        public FontBinLoader FontDate => _fontDate;
        public Bitmap BgBitmap => _bgBitmap;

        /// <summary>
        /// Raised whenever a change in this control should cause the host
        /// form to re-render the preview (color/font/background changes).
        /// </summary>
        public event EventHandler PreviewInvalidated;

        // True while ApplyConfig is in progress. Suppresses PreviewInvalidated
        // so that ValueChanged events fired by programmatic assignment don't
        // trigger a premature ReadConfig() that would write back a
        // partially-applied (half old / half new) state into the config.
        private bool _isApplying = false;

        public UC_ClockSettings()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnClockBgBrowse.Click += BtnClockBgBrowse_Click;
            btnClockBgClear.Click += BtnClockBgClear_Click;

            btnFontTime.Click += (s, e) => BrowseBin(txtFontTime, ref _fontTime, lblFontTimeNote);
            btnFontSec.Click += (s, e) => BrowseBin(txtFontSec, ref _fontSec, lblFontSecNote);
            btnFontDate.Click += (s, e) => BrowseBin(txtFontDate, ref _fontDate, lblFontDateNote);

            btnColTime.Click += (s, e) => PickColor(btnColTime);
            btnColColon.Click += (s, e) => PickColor(btnColColon);
            btnColDate.Click += (s, e) => PickColor(btnColDate);
            btnColDay.Click += (s, e) => PickColor(btnColDay);
            btnColSec.Click += (s, e) => PickColor(btnColSec);
            btnColSep.Click += (s, e) => PickColor(btnColSep);

            nudSepWidth.ValueChanged += (s, e) => RaisePreviewInvalidated();
            nudColonGap.ValueChanged += (s, e) => RaisePreviewInvalidated();

            foreach (var nud in OpacityNuds())
            {
                nud.Minimum = 0;
                nud.Maximum = 255;
                nud.ValueChanged += (s, e) => RaisePreviewInvalidated();
            }
        }

        private NumericUpDown[] OpacityNuds() => new[]
        {
            nudOpaTime, nudOpaColon, nudOpaDate, nudOpaDay, nudOpaSec,
        };

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        public void ApplyConfig(MonitorClockCfg c) => ApplyConfig(c, null, null);

        /// <summary>
        /// Rebinds config, and additionally tries to resolve+load the
        /// background image and font .bin files from the given USB asset
        /// folders, using the filenames already stored in the JSON. Pass
        /// null for either folder to skip that resolution (filenames are
        /// still shown as text either way).
        /// </summary>
        public void ApplyConfig(MonitorClockCfg c, string backgroundsDir, string fontsDir)
        {
            _isApplying = true;
            try
            {
                txtClockBgImage.Text = c.BgImage ?? "";
                txtFontTime.Text = c.FontTime ?? "";
                txtFontDate.Text = c.FontDate ?? "";
                txtFontSec.Text = c.FontSec ?? "";

                SetColorButton(btnColTime, c.ColTime);
                SetColorButton(btnColColon, c.ColColon);
                SetColorButton(btnColDate, c.ColDate);
                SetColorButton(btnColDay, c.ColDay);
                SetColorButton(btnColSec, c.ColSec);
                SetColorButton(btnColSep, c.SepColor);
                nudSepWidth.Value = Math.Max(nudSepWidth.Minimum,
                                    Math.Min(nudSepWidth.Maximum, c.SepWidth));

                nudOpaTime.Value = Math.Max(nudOpaTime.Minimum, Math.Min(nudOpaTime.Maximum, c.OpaTime));
                nudOpaColon.Value = Math.Max(nudOpaColon.Minimum, Math.Min(nudOpaColon.Maximum, c.OpaColon));
                nudOpaDate.Value = Math.Max(nudOpaDate.Minimum, Math.Min(nudOpaDate.Maximum, c.OpaDate));
                nudOpaDay.Value = Math.Max(nudOpaDay.Minimum, Math.Min(nudOpaDay.Maximum, c.OpaDay));
                nudOpaSec.Value = Math.Max(nudOpaSec.Minimum, Math.Min(nudOpaSec.Maximum, c.OpaSec));
                nudColonGap.Value = Math.Max(nudColonGap.Minimum,
                                    Math.Min(nudColonGap.Maximum, c.ColonGap));

                ReloadAssetsFromUsb(c, backgroundsDir, fontsDir);
            }
            finally
            {
                _isApplying = false;
            }
        }

        /// <summary>
        /// Resolves c.BgImage / c.FontTime / c.FontSec / c.FontDate against
        /// the given folders and loads them if found. Failures are silent
        /// (missing file, wrong USB plugged in, etc.) -- the text fields
        /// still show the stored filename either way.
        /// </summary>
        private void ReloadAssetsFromUsb(MonitorClockCfg c, string backgroundsDir, string fontsDir)
        {
            _bgBitmap?.Dispose();
            _bgBitmap = null;
            if (!string.IsNullOrEmpty(backgroundsDir) && !string.IsNullOrEmpty(c.BgImage))
            {
                var path = Path.Combine(backgroundsDir, c.BgImage);
                if (File.Exists(path))
                {
                    try { _bgBitmap = new Bitmap(path); }
                    catch { _bgBitmap = null; }
                }
            }

            _fontTime?.Dispose(); _fontTime = null;
            _fontSec?.Dispose(); _fontSec = null;
            _fontDate?.Dispose(); _fontDate = null;
            if (!string.IsNullOrEmpty(fontsDir))
            {
                _fontTime = TryLoadFont(fontsDir, c.FontTime, lblFontTimeNote);
                _fontSec = TryLoadFont(fontsDir, c.FontSec, lblFontSecNote);
                _fontDate = TryLoadFont(fontsDir, c.FontDate, lblFontDateNote);
            }
            else
            {
                UpdateFontNote(lblFontTimeNote, null);
                UpdateFontNote(lblFontSecNote, null);
                UpdateFontNote(lblFontDateNote, null);
            }
        }

        private static FontBinLoader TryLoadFont(string fontsDir, string fileName, Label note)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                UpdateFontNote(note, null);
                return null;
            }
            var path = Path.Combine(fontsDir, fileName);
            if (!File.Exists(path))
            {
                UpdateFontNote(note, null);
                return null;
            }
            try
            {
                var loader = FontBinLoader.Load(path);
                UpdateFontNote(note, loader);
                return loader;
            }
            catch
            {
                UpdateFontNote(note, null);
                return null;
            }
        }

        public void ReadConfig(MonitorClockCfg c)
        {
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

            c.OpaTime = (byte)nudOpaTime.Value;
            c.OpaColon = (byte)nudOpaColon.Value;
            c.OpaDate = (byte)nudOpaDate.Value;
            c.OpaDay = (byte)nudOpaDay.Value;
            c.OpaSec = (byte)nudOpaSec.Value;
            c.ColonGap = (int)nudColonGap.Value;
        }

        // ------------------------------------------------------------------
        // Font .bin file pickers
        // ------------------------------------------------------------------

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
            RaisePreviewInvalidated();
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
                    RaisePreviewInvalidated();
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
            RaisePreviewInvalidated();
        }

        // ------------------------------------------------------------------
        // Colour pickers
        // ------------------------------------------------------------------

        private void PickColor(Button btn)
        {
            using (var dlg = new ColorDialog { Color = btn.BackColor, FullOpen = true })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                btn.BackColor = dlg.Color;
                btn.ForeColor = ContrastColor(dlg.Color);
                btn.Text = ColorToHex(dlg.Color);
                RaisePreviewInvalidated();
            }
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

            foreach (var btn in new[] { btnFontTime, btnFontSec, btnFontDate,
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

            var darkInputBg = Color.FromArgb(45, 45, 48);
            foreach (var nud in OpacityNuds().Concat(new[] { nudColonGap }))
            {
                nud.BackColor = darkInputBg;
                nud.ForeColor = lightFg;
            }

            if (lblOpaTime != null) lblOpaTime.ForeColor = Color.FromArgb(200, 200, 200);
            if (lblOpaColon != null) lblOpaColon.ForeColor = Color.FromArgb(200, 200, 200);
            if (lblOpaDate != null) lblOpaDate.ForeColor = Color.FromArgb(200, 200, 200);
            if (lblOpaDay != null) lblOpaDay.ForeColor = Color.FromArgb(200, 200, 200);
            if (lblOpaSec != null) lblOpaSec.ForeColor = Color.FromArgb(200, 200, 200);
            if (lblColonGap != null) lblColonGap.ForeColor = Color.FromArgb(200, 200, 200);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void RaisePreviewInvalidated()
        {
            if (_isApplying) return; // suppress reentrancy during ApplyConfig
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// Releases font/bitmap resources owned by this control.
        /// Called from the Designer-generated Dispose(bool) override.
        /// </summary>
        private void DisposeOwnedResources()
        {
            _fontTime?.Dispose();
            _fontSec?.Dispose();
            _fontDate?.Dispose();
            _bgBitmap?.Dispose();
        }
    }
}