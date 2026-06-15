using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormFontBuilder : Form
    {
        public FormFontBuilder()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select TTF Font File";
                dlg.Filter = "TrueType Font (*.ttf)|*.ttf";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtTtf.Text = dlg.FileName;
            }
        }

        private void chkTime_CheckedChanged(object sender, EventArgs e) => nudTime.Enabled = chkTime.Checked;
        private void chkSec_CheckedChanged(object sender, EventArgs e) => nudSec.Enabled = chkSec.Checked;
        private void chkDate_CheckedChanged(object sender, EventArgs e) => nudDate.Enabled = chkDate.Checked;

        private void btnBuild_Click(object sender, EventArgs e)
        {
            if (!chkTime.Checked && !chkSec.Checked && !chkDate.Checked)
            {
                MessageBox.Show("Select at least one font to build.", "Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ttfPath = txtTtf.Text.Trim();
            if (string.IsNullOrEmpty(ttfPath) || !File.Exists(ttfPath))
            {
                MessageBox.Show("Please select a valid TTF file.", "Font Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dir = Path.GetDirectoryName(ttfPath);
            string font = Path.GetFileName(ttfPath);

            var built = new System.Collections.Generic.List<string>();

            if (chkTime.Checked)
            {
                int size = (int)nudTime.Value;
                string bin = $"font_time_{size}.bin";
                string args = $"--font \"{font}\" --size {size} --bpp 4 --range 0x30-0x3A --format bin -o \"{bin}\"";
                if (!RunLvFontConv(dir, args, out string err))
                {
                    ShowBuildError(bin, err);
                    return;
                }
                built.Add(bin);
            }

            if (chkSec.Checked)
            {
                int size = (int)nudSec.Value;
                string bin = $"font_sec_{size}.bin";
                string args = $"--font \"{font}\" --size {size} --bpp 4 --range 0x30-0x39 --format bin -o \"{bin}\"";
                if (!RunLvFontConv(dir, args, out string err))
                {
                    ShowBuildError(bin, err);
                    return;
                }
                built.Add(bin);
            }

            if (chkDate.Checked)
            {
                int size = (int)nudDate.Value;
                string bin = $"font_date_{size}.bin";
                string args = $"--font \"{font}\" --size {size} --bpp 4 -r 0x2F -r 0x30-0x39 -r 0x41-0x5A --format bin -o \"{bin}\"";
                if (!RunLvFontConv(dir, args, out string err))
                {
                    ShowBuildError(bin, err);
                    return;
                }
                built.Add(bin);
            }

            MessageBox.Show(
                $"Build complete.\n\n{string.Join("\n", built)}\n\nOutput folder:\n{dir}",
                "Font Builder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ShowBuildError(string bin, string err)
        {
            MessageBox.Show(
                $"Build failed for: {bin}\n\n{err}\n\nMake sure lv_font_conv is installed:\nnpm install -g lv_font_conv",
                "Font Builder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool RunLvFontConv(string workingDir, string args, out string err)
        {
            err = string.Empty;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c lv_font_conv {args}",
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
    }
}