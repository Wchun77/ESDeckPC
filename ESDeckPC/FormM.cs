using ESDeckPC.Properties;
using HidSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormM : Form
    {
        private const int VendorId = 0x303A;
        private const int ProductId = 0x4004;

        private HidReceiver _receiver;
        private MonitorSender _monitor;
        private bool _hidConnected = false;
        private PcConfig _config = null;
        private string _pcJsonPath = null;
        private bool _forceClose = false;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref value, sizeof(int));
        }

        public FormM()
        {
            InitializeComponent();

            Icon = Resources.playstation;

            _receiver = new HidReceiver();
            _receiver.OnButtonPressed += OnButtonPressed;
            _receiver.OnMonitorControl += OnMonitorControl;
            _receiver.OnModeReport += OnModeReport;

            _monitor = new MonitorSender(_receiver);
            _monitor.OnLog += msg => AppendLog(msg, Color.CornflowerBlue);

            tsBtnClearLog.Click += tsBtnClearLog_Click;

            notifyIcon.Icon = this.Icon ?? SystemIcons.Application;

            string lastPath = Properties.Settings.Default.LastPcJson;
            if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                LoadConfig(lastPath);

            DiscordRpcClient.Instance.ConnectionChanged +=
                connected => SetDiscordStatus(connected);
            DiscordRpcClient.Instance.LogMessage +=
                msg => AppendLog(msg, Color.Gray);
            _ = DiscordRpcClient.Instance.ConnectAsync();
        }

        // ------------------------------------------------------------------
        // HID poll timer
        // ------------------------------------------------------------------

        private void timerHid_Tick(object sender, EventArgs e)
        {
            var device = DeviceList.Local.GetHidDeviceOrNull(VendorId, ProductId);
            bool detected = device != null;

            if (detected && !_hidConnected)
            {
                if (_receiver.Open())
                {
                    _receiver.StartListening();
                    _hidConnected = true;
                    ssLblHid.Text = "HID: Connected";
                    ssLblHid.ForeColor = Color.Green;
                    AppendLog("HID connected", Color.LimeGreen);

                    /* Query ESP for current mode — reply arrives via OnModeReport */
                    _monitor.SendQuery();
                }
            }
            else if (!detected && _hidConnected)
            {
                _monitor.Unsubscribe();
                _receiver.Stop();
                _hidConnected = false;
                ssLblHid.Text = "HID: Disconnected";
                ssLblHid.ForeColor = Color.Red;
                AppendLog("HID disconnected", Color.OrangeRed);
            }
        }

        // ------------------------------------------------------------------
        // HID button event (fired from background thread)
        // ------------------------------------------------------------------

        private void OnButtonPressed(byte page, byte btn)
        {
            if (btn == 0x00) return;
            this.BeginInvoke((Action)(() =>
            {
                string result = ActionExecutor.Run(_config, page, btn);
                string label = GetButtonLabel(page, btn);
                AppendLog($"[{label}] {result}",
                          result.Contains("failed") ? Color.OrangeRed : Color.LimeGreen);
            }));
        }

        // ------------------------------------------------------------------
        // Monitor control event (page=0xFF from ESP, fired from background thread)
        // ------------------------------------------------------------------

        private void OnMonitorControl(byte cmd)
        {
            const byte SUBSCRIBE = 0x01;
            const byte UNSUBSCRIBE = 0x02;

            this.BeginInvoke((Action)(() =>
            {
                switch (cmd)
                {
                    case SUBSCRIBE:
                        _monitor.Subscribe();
                        AppendLog("Monitor: subscribed", Color.CornflowerBlue);
                        break;

                    case UNSUBSCRIBE:
                        _monitor.Unsubscribe();
                        AppendLog("Monitor: unsubscribed", Color.CornflowerBlue);
                        break;

                    default:
                        AppendLog($"Monitor: unknown cmd 0x{cmd:X2}", Color.Gray);
                        break;
                }
            }));
        }

        private void OnModeReport(bool inMonitor)
        {
            this.BeginInvoke((Action)(() =>
            {
                if (inMonitor)
                {
                    _monitor.Subscribe();
                    AppendLog("Monitor: ESP already in monitor mode, subscribed", Color.CornflowerBlue);
                }
                else
                {
                    AppendLog("Monitor: ESP in deck mode", Color.Gray);
                }
            }));
        }

        private string GetButtonLabel(byte page, byte btn)
        {
            if (_config == null) return $"p{page}b{btn}";
            int pi = page - 1, bi = btn - 1;
            if (pi < 0 || pi >= _config.Pages.Count) return $"p{page}b{btn}";
            var pg = _config.Pages[pi];
            if (bi < 0 || bi >= pg.Buttons.Count) return $"p{page}b{btn}";
            return pg.Buttons[bi].Label ?? $"p{page}b{btn}";
        }

        // ------------------------------------------------------------------
        // Toolbar handlers
        // ------------------------------------------------------------------

        private void tsMenuSettingsLoad_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open PC JSON";
                dlg.Filter = "JSON files (*.json)|*.json";

                string cfgDir = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath), "cfg");
                if (Directory.Exists(cfgDir))
                    dlg.InitialDirectory = cfgDir;

                if (dlg.ShowDialog() != DialogResult.OK) return;
                LoadConfig(dlg.FileName);
            }
        }

        private void tsBtnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void tsMenuSettingsEdit_Click(object sender, EventArgs e)
        {
            string pcPath = null;
            string espPath = null;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open PC JSON";
                dlg.Filter = "JSON files (*.json)|*.json";

                string cfgDir = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath), "cfg");
                if (Directory.Exists(cfgDir))
                    dlg.InitialDirectory = cfgDir;

                if (dlg.ShowDialog() != DialogResult.OK) return;
                pcPath = dlg.FileName;
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open ESP JSON (optional, cancel to skip)";
                dlg.Filter = "JSON files (*.json)|*.json";
                dlg.InitialDirectory = Path.GetDirectoryName(pcPath);

                if (dlg.ShowDialog() == DialogResult.OK)
                    espPath = dlg.FileName;
            }

            if (espPath == null)
            {
                var result = MessageBox.Show(
                    "No ESP JSON selected.\n\nSaving without an ESP JSON will overwrite " +
                    "the PC file only — the ESP file on the device will not be updated.\n\n" +
                    "Continue anyway?",
                    "ESP JSON not selected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No) return;
            }

            if (espPath != null &&
                string.Equals(Path.GetFullPath(pcPath), Path.GetFullPath(espPath),
                              StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "PC JSON and ESP JSON cannot be the same file.\n\n" +
                    "Saving would overwrite the ESP file with PC data.",
                    "Same file selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                var pcConfig = ConfigLoader.LoadPc(pcPath);
                var editor = new FormConfigEditor(pcConfig, pcPath, espPath);
                editor.ConfigSaved += (s, newPath) =>
                {
                    if (!string.IsNullOrEmpty(newPath))
                        LoadConfig(newPath);
                };
                editor.Show();
            }
            catch (Exception ex)
            {
                AppendLog($"Load failed: {ex.Message}", Color.OrangeRed);
            }
        }

        // ------------------------------------------------------------------
        // Settings menu handlers
        // ------------------------------------------------------------------

        private void tsMenuSettingsReload_Click(object sender, EventArgs e)
        {
            if (_pcJsonPath == null) return;
            LoadConfig(_pcJsonPath);
        }

        private void tsMenuSettingsOpenFolder_Click(object sender, EventArgs e)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string cfgFolder = Path.Combine(exeDir, "cfg");

            if (!Directory.Exists(cfgFolder))
            {
                MessageBox.Show("Folder 'cfg' not found.", "ESDeck PC",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Process.Start("explorer.exe", cfgFolder);
        }

        // ------------------------------------------------------------------
        // Discord menu handlers
        // ------------------------------------------------------------------

        private void tsMenuDiscordReconnect_Click(object sender, EventArgs e)
        {
            AppendLog("Discord: reconnecting...", Color.Gray);
            _ = DiscordRpcClient.Instance.ConnectAsync();
        }

        // ------------------------------------------------------------------
        // Monitor
        // ------------------------------------------------------------------
        private void tsMenuMonitorEdit_Click(object sender, EventArgs e)
        {
            new FormMonitorEditor().Show();
        }

        private void tsMenuFontBuilder_Click(object sender, EventArgs e)
        {
            new FormFontBuilder().Show();
        }

        private void tsMenuImageBg_Click(object sender, EventArgs e)
        {
            new FormBgImporter().Show();
        }

        private void tsMenuImageIco_Click(object sender, EventArgs e)
        {
            new FormIcoImporter().Show();
        }

        // ------------------------------------------------------------------
        // Discord status update
        // ------------------------------------------------------------------

        public void SetDiscordStatus(bool connected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((Action)(() => SetDiscordStatus(connected)));
                return;
            }

            if (connected)
            {
                ssLblDiscord.Text = "Discord: Connected";
                ssLblDiscord.ForeColor = Color.Green;
            }
            else
            {
                ssLblDiscord.Text = "Discord: Disconnected";
                ssLblDiscord.ForeColor = Color.Red;
            }
        }

        // ------------------------------------------------------------------
        // Tray handlers
        // ------------------------------------------------------------------

        private void FormM_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_forceClose) return;
            e.Cancel = true;
            this.Hide();
            notifyIcon.ShowBalloonTip(1000, "ESDeck PC",
                                      "Running in background", ToolTipIcon.Info);
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e) => ShowMainWindow();
        private void trayMenuOpen_Click(object sender, EventArgs e) => ShowMainWindow();

        private void trayMenuExit_Click(object sender, EventArgs e)
        {
            _forceClose = true;
            _monitor.Dispose();
            _receiver.Stop();
            DiscordRpcClient.Instance.Dispose();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        // ------------------------------------------------------------------
        // Config load
        // ------------------------------------------------------------------

        private void LoadConfig(string path)
        {
            try
            {
                _config = ConfigLoader.LoadPc(path);
                _pcJsonPath = path;
                Properties.Settings.Default.LastPcJson = path;
                Properties.Settings.Default.Save();
                lblPcName.Text = Path.GetFileName(path);
                tsMenuSettingsReload.Enabled = true;
                lstPages.Items.Clear();
                foreach (var pg in _config.Pages)
                    lstPages.Items.Add($"{pg.Name} ({pg.Buttons.Count})");
                ssLblJson.Text = "JSON: Loaded";
                ssLblPages.Text = $"{_config.Pages.Count} pages";
                AppendLog($"Loaded {Path.GetFileName(path)}", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                AppendLog($"Load failed: {ex.Message}", Color.OrangeRed);
            }
        }

        // ------------------------------------------------------------------
        // Log helper
        // ------------------------------------------------------------------

        private void AppendLog(string text, Color color)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke((Action)(() => AppendLog(text, color)));
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(line);
            txtLog.ScrollToCaret();
        }

    }
}