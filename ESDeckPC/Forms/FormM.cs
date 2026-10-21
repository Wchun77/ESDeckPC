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

        private HidReceiver _receiver;
        private MonitorSender _monitor;
        private NowPlayingWatcher _nowPlaying;
        private NowPlayingSender _nowPlayingSender;
        private NowPlayingImageSender _nowPlayingImageSender;
        private AudioLevelWatcher _audioLevel;
        private AudioLevelSender _audioLevelSender;
        private bool _hidConnected = false;
        private readonly TouchpadInput _touchpad = new TouchpadInput();
        private readonly KeyboardInput _keyboard = new KeyboardInput();
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

            var darkRenderer = new DarkToolStripRenderer(new DarkColorTable());
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
            _receiver.OnActionReceived += OnActionReceived;
            _receiver.OnMonitorControl += OnMonitorControl;
            _receiver.OnMediaControl += OnMediaControl;
            _receiver.OnMediaSeek += OnMediaSeek;
            _receiver.OnModeReport += OnModeReport;
            // Touchpad mode: handled on the HID read thread, no UI hop.
            _receiver.OnTouchpadReport += _touchpad.Process;
            // Mini keyboard: same, VK-based SendInput so the IME works.
            _receiver.OnKeyboardReport += _keyboard.Process;

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

            // Cover art + PC-rendered title/artist strip (CJK text, no
            // on-device font) -- sent on track change only, not a fixed
            // cadence like the senders above. Shares the same Subscribe()/
            // Unsubscribe() gating as Now Playing (see OnMediaControl,
            // OnModeReport below).
            _nowPlayingImageSender = new NowPlayingImageSender(_receiver, _nowPlaying);
            _nowPlayingImageSender.OnLog += msg => AppendLog(msg, Color.MediumPurple);

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
            var device = HidReceiver.FindVendorDevice();
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

                    /* Send current time immediately on connect so the ESP
                     * has a valid clock right away, then keep correcting
                     * it in the background below every poll tick
                     * regardless of mode. */
                    _monitor.SendTimeNow();

                    /* Query ESP for current mode — reply arrives via OnModeReport */
                    _monitor.SendQuery();
                }
            }
            else if (detected && _hidConnected)
            {
                /* Background time correction, independent of mode/subscribe
                 * state — the ESP free-runs its own clock between syncs
                 * (sys_clock.c) but this keeps it honest even outside
                 * Monitor mode, which already gets its own once-a-second
                 * stream from MonitorSender's worker loop while subscribed. */
                _monitor.SendTimeNow();
            }
            else if (!detected && _hidConnected)
            {
                _monitor.Unsubscribe();
                _nowPlayingSender.Unsubscribe();
                _audioLevelSender.Unsubscribe();
                _nowPlayingImageSender.Unsubscribe();
                _receiver.Stop();
                _touchpad.ReleaseAll();   // unplugged mid-drag: don't leave a button held
                _keyboard.ReleaseAll();   // ...or a key / modifier
                _hidConnected = false;
                ssLblHid.Text = "HID: Disconnected";
                ssLblHid.ForeColor = Color.Red;
                AppendLog("HID disconnected", Color.OrangeRed);
            }
        }

        // ------------------------------------------------------------------
        // Deck action (fired from the HID read thread). The ESP carries the
        // deck config and sends the pressed button as JSON -- label +
        // action fields -- so nothing is looked up on the PC side.
        // ------------------------------------------------------------------

        private void OnActionReceived(string json)
        {
            this.BeginInvoke((Action)(() =>
            {
                PcButton button;
                try
                {
                    button = Newtonsoft.Json.JsonConvert.DeserializeObject<PcButton>(json);
                }
                catch (Exception ex)
                {
                    AppendLog($"Deck action: bad JSON ({ex.Message})", Color.OrangeRed);
                    return;
                }
                if (button == null) return;

                string result = ActionExecutor.Run(button);
                string label = string.IsNullOrEmpty(button.Label) ? "?" : button.Label;
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
                        _nowPlayingImageSender.Subscribe();
                        AppendLog("NowPlaying: subscribed", Color.MediumPurple);
                        break;

                    case UNSUBSCRIBE:
                        _nowPlayingSender.Unsubscribe();
                        _audioLevelSender.Unsubscribe();
                        _nowPlayingImageSender.Unsubscribe();
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
                        _nowPlayingImageSender.Subscribe();
                        AppendLog("NowPlaying: ESP already in media mode, subscribed", Color.MediumPurple);
                        break;

                    case HidReceiver.EspMode.Touchpad:
                        AppendLog("Touchpad: ESP in touchpad mode", Color.Gray);
                        break;

                    default:
                        AppendLog("Monitor: ESP in deck mode", Color.Gray);
                        break;
                }
            }));
        }

        // ------------------------------------------------------------------
        // Toolbar handlers
        // ------------------------------------------------------------------

        private void tsBtnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void tsMenuDeckEdit_Click(object sender, EventArgs e)
        {
            if (_editorOpen)
            {
                MessageBox.Show("The deck editor is already open.",
                    "ESDeck PC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var editor = new FormConfigEditor();
            editor.FormClosed += (s, args) => _editorOpen = false;
            _editorOpen = true;
            editor.Show();
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

        // ------------------------------------------------------------------
        // Media
        // ------------------------------------------------------------------
        private void tsMenuMediaEdit_Click(object sender, EventArgs e)
        {
            new FormMediaEditor().Show();
        }

        // ------------------------------------------------------------------
        // Touchpad
        // ------------------------------------------------------------------
        private void tsMenuTouchpadEdit_Click(object sender, EventArgs e)
        {
            new FormTouchpadEditor().Show();
        }

        // ------------------------------------------------------------------
        // Assets
        // ------------------------------------------------------------------

        private void tsMenuAssetsFontClock_Click(object sender, EventArgs e)
        {
            new FormFontClockBuilder().Show();
        }

        private void tsMenuAssetsFontNotify_Click(object sender, EventArgs e)
        {
            new FormFontNotifyBuilder().Show();
        }

        private void tsMenuAssetsBg_Click(object sender, EventArgs e)
        {
            new FormBgImporter().Show();
        }

        private void tsMenuAssetsIco_Click(object sender, EventArgs e)
        {
            new FormIcoImporter().Show();
        }

        private void tsMenuAssetsSideIcon_Click(object sender, EventArgs e)
        {
            new FormSideIconImporter().Show();
        }

        private void tsMenuAssetsBootAnim_Click(object sender, EventArgs e)
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
            _nowPlayingImageSender.Dispose();
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