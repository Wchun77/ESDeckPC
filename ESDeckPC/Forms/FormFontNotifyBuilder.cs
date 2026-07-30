using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormFontNotifyBuilder : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
        }

        // Preview state -- disposed/replaced whenever a new .bin is loaded.
        private FontBinLoader _previewFont;

        public FormFontNotifyBuilder()
        {
            InitializeComponent();

            // Default the hanzi list to the one bundled next to the exe
            // (Tools\common_hanzi.txt, see ESDeckPC.csproj's
            // CopyToOutputDirectory entry) so the common case -- just
            // pick a ttf and build -- needs no extra browsing. Only
            // pre-fills the box if that file actually exists; an empty
            // box still makes it obvious a hanzi list needs picking if
            // the bundled one is missing for some reason.
            string defaultHanzi = GetDefaultHanziPath();
            if (File.Exists(defaultHanzi))
                txtHanzi.Text = defaultHanzi;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _previewFont?.Dispose();
            _previewFont = null;
            base.OnFormClosed(e);
        }

        private static string GetDefaultHanziPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "common_hanzi.txt");
        }

        // ------------------------------------------------------------------
        // Generate
        // ------------------------------------------------------------------

        private void btnBrowseTtf_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select TTF Font File";
                dlg.Filter = "TrueType Font (*.ttf)|*.ttf";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtTtf.Text = dlg.FileName;
            }
        }

        private void btnBrowseHanzi_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Hanzi List (.txt)";
                dlg.Filter = "Text File (*.txt)|*.txt";

                // Default to wherever the current hanzi list is (the
                // bundled Tools\common_hanzi.txt if nothing picked yet)
                // instead of whatever folder the dialog last remembered --
                // otherwise this opens somewhere unrelated (e.g. Documents)
                // the first time, which isn't where the actual list lives.
                string current = txtHanzi.Text.Trim();
                string dir = !string.IsNullOrEmpty(current) && File.Exists(current)
                    ? Path.GetDirectoryName(current)
                    : Path.GetDirectoryName(GetDefaultHanziPath());
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    dlg.InitialDirectory = dir;

                if (dlg.ShowDialog() == DialogResult.OK)
                    txtHanzi.Text = dlg.FileName;
            }
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            string ttfPath = txtTtf.Text.Trim();
            if (string.IsNullOrEmpty(ttfPath) || !File.Exists(ttfPath))
            {
                MessageBox.Show("Please select a valid TTF file.", "Notify Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hanziPath = txtHanzi.Text.Trim();
            if (string.IsNullOrEmpty(hanziPath) || !File.Exists(hanziPath))
            {
                MessageBox.Show("Please select a valid hanzi list (.txt).", "Notify Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string symbols;
            try
            {
                // lv_font_conv has no --symbols-file -- only --symbols
                // <characters>, taken inline, not as a path. Strip
                // whitespace/newlines so the list file can be formatted
                // however's convenient (one char per line, wrapped, etc.)
                // without those characters themselves getting requested
                // as glyphs.
                string raw = File.ReadAllText(hanziPath, System.Text.Encoding.UTF8);
                var sb = new System.Text.StringBuilder(raw.Length);
                foreach (char c in raw)
                    if (!char.IsWhiteSpace(c)) sb.Append(c);
                symbols = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read hanzi list:\n{ex.Message}", "Notify Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (symbols.Length == 0)
            {
                MessageBox.Show("Hanzi list is empty.", "Notify Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int size = (int)nudSize.Value;
            string dir2 = Path.GetDirectoryName(ttfPath);
            string font = Path.GetFileName(ttfPath);
            string bin = "notify.bin";

            if (!RunLvFontConvNotify(dir2, font, size, symbols, bin, out string err))
            {
                ShowBuildError(bin, err);
                return;
            }

            MessageBox.Show(
                $"Build complete.\n\n{bin}\n\nOutput folder:\n{dir2}",
                "Notify Font Builder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ShowBuildError(string bin, string err)
        {
            MessageBox.Show(
                $"Build failed for: {bin}\n\n{err}\n\nMake sure lv_font_conv is installed:\nnpm install -g lv_font_conv",
                "Notify Font Builder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Deliberately NOT routed through "cmd.exe /c" like the Clock
        // builder's RunLvFontConv -- cmd.exe caps its own /c command line
        // at ~8191 characters, and the --symbols value here is the whole
        // hanzi list inline (the bundled common_hanzi.txt alone is
        // already ~5500 characters), so it's realistic to blow past that
        // limit once the font/output path and other flags are added,
        // silently truncating the symbol list. powershell.exe's command
        // line instead rides on the normal ~32767-character Win32
        // process command-line limit, and -EncodedCommand (a base64 blob
        // of the actual script, decoded internally by PowerShell) sails
        // past all cmd-style argument-quoting/escaping pitfalls for
        // arbitrary CJK content -- same fix already applied to
        // convert_notify_font.bat for the same underlying reason.
        //
        // -r 0x20-0x7E       : ASCII (digits, upper/lower letters, half-width punctuation)
        // -r 0x3000-0x303F   : CJK Symbols and Punctuation (、。「」『』...)
        // -r 0xFF00-0xFFEF   : Halfwidth and Fullwidth Forms (，：full-width digits/letters, etc.)
        private static bool RunLvFontConvNotify(string workingDir, string ttfFileName, int size,
                                                 string symbols, string outBin, out string err)
        {
            err = string.Empty;
            try
            {
                string psScript =
                    $"& lv_font_conv --font {PsSingleQuote(ttfFileName)} --size {size} --bpp 4 " +
                    $"--symbols {PsSingleQuote(symbols)} " +
                    "-r 0x20-0x7E -r 0x3000-0x303F -r 0xFF00-0xFFEF " +
                    $"--format bin -o {PsSingleQuote(outBin)}";

                string encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(psScript));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    // -ExecutionPolicy Bypass: npm's global bin puts three
                    // shims next to lv_font_conv.js (no-ext, .cmd, .ps1);
                    // PowerShell resolves the bare name to the .ps1 one,
                    // which is a script and therefore subject to whatever
                    // execution policy applies to *this* process -- which
                    // isn't necessarily the same policy the user's own
                    // interactive shell has (e.g. a 32-bit ESDeckPC.exe
                    // spawning powershell.exe gets WOW64-redirected to the
                    // SysWOW64 host, which reads execution policy from a
                    // separate registry view than a 64-bit shell would).
                    // Bypass only affects this one child process, not any
                    // persistent user/machine setting.
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        err = p.StandardError.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(err))
                            err = p.StandardOutput.ReadToEnd();
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        // PowerShell single-quoted strings are fully literal (no $var
        // expansion, no backtick escapes to worry about) -- only a
        // literal ' needs doubling. Used for every value spliced into
        // the script above (ttf filename, symbols, output name), since
        // any of them could in principle contain one.
        private static string PsSingleQuote(string s)
        {
            return "'" + s.Replace("'", "''") + "'";
        }

        // ------------------------------------------------------------------
        // Preview
        // ------------------------------------------------------------------

        private void btnBrowseBin_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select notify.bin";
                dlg.Filter = "LVGL Bin Font (*.bin)|*.bin";

                string current = txtTtf.Text.Trim();
                string dir = !string.IsNullOrEmpty(current) ? Path.GetDirectoryName(current) : null;
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    dlg.InitialDirectory = dir;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                var loaded = FontBinLoader.Load(dlg.FileName);
                if (loaded == null)
                {
                    MessageBox.Show("Failed to parse this .bin file.", "Notify Font Builder",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _previewFont?.Dispose();
                _previewFont = loaded;
                txtBin.Text = dlg.FileName;
                panelPreview.Invalidate();
            }
        }

        private void txtChar_TextChanged(object sender, EventArgs e)
        {
            panelPreview.Invalidate();
        }

        // Single glyph centered in panelPreview. Mirrors
        // MonitorClockRenderer.DrawSingleGlyph's math (LVGL glyph layout:
        // draw_x = pen_x + ofs_x, draw_y = baseline_y - ofs_y - box_h) --
        // font.GetGlyph() returning null (character not in this .bin) is
        // the normal "not supported" case, not an error, so it's handled
        // by simply not drawing anything, per the "不支援就不顯示" spec.
        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_previewFont == null) return;

            string text = txtChar.Text;
            if (text.Length == 0) return;
            char c = text[0];

            var gi = _previewFont.GetGlyph(c);
            if (gi == null) return;              // not supported by this font -- draw nothing
            if (gi.Image == null) return;         // zero-width glyph (e.g. space) -- nothing to draw

            int cellW = panelPreview.ClientSize.Width;
            int cellH = panelPreview.ClientSize.Height;

            int penX = (cellW - gi.AdvW) / 2;
            int blockH = gi.OfsY + gi.BoxH;
            int baselineY = (cellH - blockH) / 2 + blockH;

            int drawX = penX + gi.OfsX;
            int drawY = baselineY - gi.OfsY - gi.BoxH;

            DrawTinted(e.Graphics, gi.Image, drawX, drawY, Color.FromArgb(220, 220, 220));
        }

        // Same tinting trick as MonitorClockRenderer.DrawTinted: the
        // glyph bitmap is a white RGBA mask (alpha = coverage), so a
        // ColorMatrix that zeroes RGB and forces it to the tint colour
        // (keeping the source alpha) recolors it without touching pixels
        // one at a time.
        private static void DrawTinted(Graphics g, Bitmap src, int x, int y, Color tint)
        {
            float r = tint.R / 255f;
            float gv = tint.G / 255f;
            float b = tint.B / 255f;
            float a = tint.A / 255f;

            var cm = new ColorMatrix(new[]
            {
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  a, 0 },
                new float[] { r, gv,  b,  0, 1 },
            });

            var ia = new ImageAttributes();
            ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(src,
                        new Rectangle(x, y, src.Width, src.Height),
                        0, 0, src.Width, src.Height,
                        GraphicsUnit.Pixel, ia);
            ia.Dispose();
        }
    }
}
