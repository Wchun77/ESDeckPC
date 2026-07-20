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
        private NowPlayingWatcher _nowPlaying;
        private NowPlayingSender _nowPlayingSender;
        private AudioLevelWatcher _audioLevel;
        private AudioLevelSender _audioLevelSender;
        private bool _hidConnected = false;
        private PcConfig _config = null;
        private string _pcJsonPath = null;
        private bool _forceClose = false;
        private bool _editorOpen = false;

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

            var darkRenderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            ToolStripManager.Renderer = darkRenderer;
            toolStrip.Renderer = darkRenderer;

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripDropDownButton btn)
                {
                    btn.DropDown.Renderer = darkRenderer;
                    btn.DropDown.Opening += (s, e) =>
                    {
                        if (s is ToolStripDropDown dd)
                            dd.Renderer = darkRenderer;
                    };
                    foreach (ToolStripItem sub in btn.DropDownItems)
                        sub.ForeColor = Color.FromArgb(220, 220, 220);
                }
            }

            trayMenu.Renderer = darkRenderer;
            foreach (ToolStripItem item in trayMenu.Items)
                item.ForeColor = Color.FromArgb(220, 220, 220);

            Icon = Resources.playstation;

            _receiver = new HidReceiver();
            _receiver.OnButtonPressed += OnButtonPressed;
            _receiver.OnMonitorControl += OnMonitorControl;
            _receiver.OnMediaControl += OnMediaControl;
            _receiver.OnMediaSeek += OnMediaSeek;
            _receiver.OnModeReport += OnModeReport;

            _monitor = new MonitorSender(_receiver);
            _monitor.OnLog += msg => AppendLog(msg, Color.CornflowerBlue);

            // Media mode Now Playing: watches Windows Media Session locally,
            // sender pushes position/duration/playing to the ESP over HID
            // (CMD_NOWPLAYING_PROGRESS=0x06) while the ESP is on the Media
            // page (subscribe/unsubscribe via OnMediaControl below).
            // Title/artist are not part of this protocol yet -- see
            // doc/ESDeck_Media模式開發筆記.md 第 4 節.
            _nowPlaying = new NowPlayingWatcher();
            _nowPlaying.OnLog += msg => AppendLog(msg, Color.MediumPurple);
            _nowPlaying.Start();

            _nowPlayingSender = new NowPlayingSender(_receiver, _nowPlaying);
            _nowPlayingSender.OnLog += msg => AppendLog(msg, Color.MediumPurple);

            // Send immediately on a real state change (play/pause/seek/
            // track change -- from the ESP's own buttons or anything else)
            // instead of waiting out the rest of the 1s cycle, so the ESP's
            // icon updates promptly rather than up to ~1s late.
            _nowPlaying.OnStateChanged += () => _nowPlayingSender.Nudge();

            // Media mode audio visualization: WASAPI loopback volume level,
            // sent to the ESP sidebar VU-meter bar (CMD_AUDIO_LEVEL=0x07)
            // while the ESP is on the Media page, same subscribe channel as
            // Now Playing. Single-value "簡單版" only -- see
            // doc/ESDeck_Media模式開發筆記.md 第 5 節.
            _audioLevel = new AudioLevelWatcher();
            _audioLevel.OnLog += msg => AppendLog(msg, Color.SkyBlue);
            _audioLevel.Start();

            _audioLevelSender = new AudioLevelSender(_receiver, _audioLevel);
            _audioLevelSender.OnLog += msg => AppendLog(msg, Color.SkyBlue);

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
                _nowPlayingSender.Unsubscribe();
                _audioLevelSender.Unsubscribe();
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

        // ------------------------------------------------------------------
        // Media control event (page=0xFE from ESP, fired from background thread)
        // ------------------------------------------------------------------

        private void OnMediaControl(byte cmd)
        {
            const byte SUBSCRIBE = 0x01;
            const byte UNSUBSCRIBE = 0x02;
            const byte PLAY_PAUSE = 0x03;
            const byte NEXT = 0x04;
            const byte PREV = 0x05;

            this.BeginInvoke((Action)(() =>
            {
                switch (cmd)
                {
                    case SUBSCRIBE:
                        _nowPlayingSender.Subscribe();
                        _audioLevelSender.Subscribe();
                        AppendLog("NowPlaying: subscribed", Color.MediumPurple);
                        break;

                    case UNSUBSCRIBE:
                        _nowPlayingSender.Unsubscribe();
                        _audioLevelSender.Unsubscribe();
                        AppendLog("NowPlaying: unsubscribed", Color.MediumPurple);
                        break;

                    case PLAY_PAUSE:
                        _nowPlaying.TogglePlayPause();
                        AppendLog("NowPlaying: play/pause (from ESP)", Color.MediumPurple);
                        break;

                    case NEXT:
                        _nowPlaying.Next();
                        AppendLog("NowPlaying: next (from ESP)", Color.MediumPurple);
                        break;

                    case PREV:
                        _nowPlaying.Previous();
                        AppendLog("NowPlaying: prev (from ESP)", Color.MediumPurple);
                        break;

                    default:
                        AppendLog($"NowPlaying: unknown cmd 0x{cmd:X2}", Color.Gray);
                        break;
                }
            }));
        }

        private void OnMediaSeek(uint positionMs)
        {
            this.BeginInvoke((Action)(() =>
            {
                _nowPlaying.SeekTo(TimeSpan.FromMilliseconds(positionMs));
                AppendLog($"NowPlaying: seek to {TimeSpan.FromMilliseconds(positionMs):hh\\:mm\\:ss} (from ESP)", Color.MediumPurple);
            }));
        }

        private void OnModeReport(HidReceiver.EspMode mode)
        {
            this.BeginInvoke((Action)(() =>
            {
                switch (mode)
                {
                    case HidReceiver.EspMode.Monitor:
                        _monitor.Subscribe();
                        AppendLog("Monitor: ESP already in monitor mode, subscribed", Color.CornflowerBlue);
                        break;

                    case HidReceiver.EspMode.Media:
                        // Covers the case where the ESP was already on the
                        // Media page before this app started (or restarted)
                        // -- ESP already sent its one-shot subscribe while
                        // nothing was listening, so without this the PC side
                        // would never start sending and the ESP would be
                        // stuck showing its own local fake-data fallback.
                        _nowPlayingSender.Subscribe();
                        _audioLevelSender.Subscribe();
                        AppendLog("NowPlaying: ESP already in media mode, subscribed", Color.MediumPurple);
                        break;

                    default:
                        AppendLog("Monitor: ESP in deck mode", Color.Gray);
                        break;
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
            if (_editorOpen)
            {
                MessageBox.Show("The deck editor is already open.",
                    "ESDeck PC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
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
                editor.FormClosed += (s, args) => _editorOpen = false;
                _editorOpen = true;
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

        private void tsMenuImageSideIcon_Click(object sender, EventArgs e)
        {
            new FormSideIconImporter().Show();
        }

        private void tsMenuImageBootAnim_Click(object sender, EventArgs e)
        {
            new FormBootAnimConverter().Show();
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
            _nowPlaying.Dispose();
            _nowPlayingSender.Dispose();
            _audioLevel.Dispose();
            _audioLevelSender.Dispose();
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