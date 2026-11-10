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
        // Log colours, one per category so a glance tells them apart (txtLog's
        // background is #141414). Errors from FormM itself are LogError.
        private static readonly Color LogHidOk     = Color.LimeGreen;       // HID connected
        private static readonly Color LogError     = Color.OrangeRed;       // HID disconnected, failed actions
        private static readonly Color LogDeck      = Color.Khaki;           // Deck actions, Deck mode
        private static readonly Color LogTouchpad  = Color.WhiteSmoke;      // Touchpad mode
        private static readonly Color LogMonitor   = Color.CornflowerBlue;  // Monitor mode (+ mode query)
        private static readonly Color LogMedia     = Color.MediumPurple;    // the ESP's Media mode
        private static readonly Color LogNowPlaying= Color.Plum;            // what the PC itself is playing
        private static readonly Color LogAudio     = Color.SkyBlue;         // PC audio capture
        private static readonly Color LogCanvas    = Color.Turquoise;       // Canvas mode / pages
        private static readonly Color LogOta       = Color.Orange;          // firmware update
        private static readonly Color LogDiscord   = Color.LightSlateGray;  // Discord RPC
        private static readonly Color LogMisc      = Color.Gray;            // unknown commands / modes

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
        private OtaUploader _ota;
        private ToolStripMenuItem _tsMenuFirmware;
        private ToolStripMenuItem _trayMenuFirmware;
        private Icon _trayIconOn;    // normal app icon
        private Icon _trayIconOff;   // grayed copy, shown while ESDeck isn't connected
        private bool _otaRunning = false;
        private CanvasLink _canvas;
        private FormCanvasWindow _canvasWindow;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        // Log box scrollbar: dark instead of the default white one (same
        // trick as FormMediaEditor's settings panel). Windows 10 1809+.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetWindowTheme(txtLog.Handle, "DarkMode_Explorer", null);
        }

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

            // Device > Firmware Update... (built here, not in the designer)
            var tsMenuDevice = new ToolStripDropDownButton("Device")
            {
                ShowDropDownArrow = false,
                ForeColor = Color.FromArgb(220, 220, 220),
            };
            _tsMenuFirmware = new ToolStripMenuItem("Firmware Update...");
            _tsMenuFirmware.Click += tsMenuFirmware_Click;
            tsMenuDevice.DropDownItems.Add(_tsMenuFirmware);
            toolStrip.Items.Insert(toolStrip.Items.IndexOf(tsSep1), tsMenuDevice);

            // Modes: every mode's editor in one menu (the designer's
            // Deck / Monitor / Media / TouchPad buttons are folded in here),
            // plus Canvas, which is set up entirely on the PC.
            var tsMenuModes = new ToolStripDropDownButton("Modes")
            {
                ShowDropDownArrow = false,
                ForeColor = Color.FromArgb(220, 220, 220),
            };
            void foldIn(ToolStripDropDownButton oldBtn, ToolStripMenuItem edit, string text)
            {
                edit.Text = text;
                tsMenuModes.DropDownItems.Add(edit);   // moves it out of the old button
                toolStrip.Items.Remove(oldBtn);
            }
            foldIn(tsMenuDeck, tsMenuDeckEdit, "Deck Editor...");
            foldIn(tsMenuMonitor, tsMenuMonitorEdit, "Monitor Editor...");
            foldIn(tsMenuMeida, tsMenuMediaEdit, "Media Editor...");
            foldIn(tsMenuTouchpad, tsMenuTouchpadEdit, "Touchpad Editor...");
            tsMenuModes.DropDownItems.Add("Canvas Editor...", null, (s, e) => SingleWindow.Show(() => new FormCanvasEditor(_canvas?.Bridge)));
            tsMenuModes.DropDownItems.Add(new ToolStripSeparator());
            tsMenuModes.DropDownItems.Add("Canvas Window...", null, (s, e) => ShowCanvasWindow());
            tsMenuModes.DropDownItems.Add(new ToolStripSeparator());   // page-making tools below
            tsMenuModes.DropDownItems.Add("Open Canvas Folder", null, (s, e) =>
            {
                CanvasPaths.Ensure();
                Process.Start("explorer.exe", $"\"{CanvasPaths.Root}\"");
            });
            tsMenuModes.DropDownItems.Add("Canvas DevTools", null, (s, e) => _canvas.OpenDevTools());
            toolStrip.Items.Insert(0, tsMenuModes);

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
            _ota = new OtaUploader(_receiver);
            _ota.OnLog += msg => AppendLog(msg, LogOta);
            int lastLoggedPct = -10;
            _ota.OnProgress += pct =>
            {
                if (pct >= lastLoggedPct + 10 || pct == 100)
                {
                    lastLoggedPct = pct - pct % 10;
                    AppendLog($"OTA: {pct}%", LogOta);
                }
                if (pct == 0) lastLoggedPct = 0;
            };
            _receiver.OnOtaStatus += _ota.HandleStatus;
            _receiver.OnMonitorControl += OnMonitorControl;
            _receiver.OnMediaControl += OnMediaControl;
            _receiver.OnMediaSeek += OnMediaSeek;
            _receiver.OnModeReport += OnModeReport;
            // Touchpad mode: handled on the HID read thread, no UI hop.
            _receiver.OnTouchpadReport += _touchpad.Process;
            // Mini keyboard: same, VK-based SendInput so the IME works.
            _receiver.OnKeyboardReport += _keyboard.Process;

            // Canvas mode: HTML pages rendered here, shown on the ESP.
            _canvas = new CanvasLink(_receiver, this);
            _canvas.OnLog += msg => AppendLog(msg, LogCanvas);
            _receiver.OnCanvasReport += _canvas.HandleReport;

            _monitor = new MonitorSender(_receiver);
            _monitor.OnLog += msg => AppendLog(msg, LogMonitor);

            // Media mode Now Playing: watches Windows Media Session locally,
            // sender pushes position/duration/playing to the ESP over HID
            // (CMD_NOWPLAYING_PROGRESS=0x06) while the ESP is on the Media
            // page (subscribe/unsubscribe via OnMediaControl below).
            // Title/artist are not part of this protocol yet -- see
            // doc/ESDeck_Media模式開發筆記.md 第 4 節.
            _nowPlaying = new NowPlayingWatcher();
            _nowPlaying.OnLog += msg => AppendLog(msg, LogNowPlaying);   // the PC's own playback (also feeds Canvas)
            _nowPlaying.Start();
            _canvas.Bridge.Attach(_monitor, _nowPlaying);   // esdeck.stats() / esdeck.media() for Canvas pages

            _nowPlayingSender = new NowPlayingSender(_receiver, _nowPlaying);
            _nowPlayingSender.OnLog += msg => AppendLog(msg, LogMedia);

            // Cover art + PC-rendered title/artist strip (CJK text, no
            // on-device font) -- sent on track change only, not a fixed
            // cadence like the senders above. Shares the same Subscribe()/
            // Unsubscribe() gating as Now Playing (see OnMediaControl,
            // OnModeReport below).
            _nowPlayingImageSender = new NowPlayingImageSender(_receiver, _nowPlaying);
            _nowPlayingImageSender.OnLog += msg => AppendLog(msg, LogMedia);

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
            _audioLevel.OnLog += msg => AppendLog(msg, LogAudio);
            _audioLevel.Start();

            _audioLevelSender = new AudioLevelSender(_receiver, _audioLevel);
            _audioLevelSender.OnLog += msg => AppendLog(msg, LogMedia);   // part of the ESP's Media mode

            tsBtnClearLog.Click += tsBtnClearLog_Click;

            // Tray: icon shows the connection (gray = not connected), and
            // Firmware Update... is reachable without opening the window.
            _trayIconOn  = this.Icon ?? SystemIcons.Application;
            _trayIconOff = MakeGrayIcon(_trayIconOn);
            _trayMenuFirmware = new ToolStripMenuItem("Firmware Update...")
            {
                ForeColor = Color.FromArgb(220, 220, 220),   // added after the dark-theme loop above
            };
            _trayMenuFirmware.Click += tsMenuFirmware_Click;
            trayMenu.Items.Insert(trayMenu.Items.IndexOf(trayMenuSep), _trayMenuFirmware);
            var trayCanvas = new ToolStripMenuItem("Canvas Window")
            {
                ForeColor = Color.FromArgb(220, 220, 220),
            };
            trayCanvas.Click += (s, e) => ShowCanvasWindow();
            trayMenu.Items.Insert(trayMenu.Items.IndexOf(trayMenuSep), trayCanvas);
            SetTrayConnected(false);

            DiscordRpcClient.Instance.ConnectionChanged +=
                connected => SetDiscordStatus(connected);
            DiscordRpcClient.Instance.LogMessage +=
                msg => AppendLog(msg, LogDiscord);
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
                    SetTrayConnected(true);
                    AppendLog("HID connected", LogHidOk);

                    /* Send current time immediately on connect so the ESP
                     * has a valid clock right away, then keep correcting
                     * it in the background below every poll tick
                     * regardless of mode. */
                    _monitor.SendTimeNow();

                    /* Query ESP for current mode — reply arrives via OnModeReport */
                    _receiver.SendModeQuery();
                    AppendLog("HID: sent mode query", LogMisc);
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
                _canvas.OnDisconnected();
                _touchpad.ReleaseAll();   // unplugged mid-drag: don't leave a button held
                _keyboard.ReleaseAll();   // ...or a key / modifier
                _hidConnected = false;
                ssLblHid.Text = "HID: Disconnected";
                ssLblHid.ForeColor = Color.Red;
                SetTrayConnected(false);
                AppendLog("HID disconnected", LogError);
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
                _canvas.Deactivate("ESP switched to Deck");   // in case its "leave" got lost
                DeckButton button;
                try
                {
                    button = Newtonsoft.Json.JsonConvert.DeserializeObject<DeckButton>(json);
                }
                catch (Exception ex)
                {
                    AppendLog($"Deck: bad action JSON ({ex.Message})", LogError);
                    return;
                }
                if (button == null) return;

                string result = ActionExecutor.Run(button);
                string label = string.IsNullOrEmpty(button.Label) ? "?" : button.Label;
                AppendLog($"Deck: [{label}] {result}",
                          result.Contains("failed") ? LogError : LogDeck);
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
                        _canvas.Deactivate("ESP switched to Monitor");   // in case its "leave" got lost
                        AppendLog("Monitor: subscribed", LogMonitor);
                        _monitor.Subscribe();
                        break;

                    case UNSUBSCRIBE:
                        AppendLog("Monitor: unsubscribed", LogMonitor);
                        _monitor.Unsubscribe();
                        break;

                    default:
                        AppendLog($"Monitor: unknown cmd 0x{cmd:X2}", LogMisc);
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
                        _canvas.Deactivate("ESP switched to Media");   // in case its "leave" got lost
                        AppendLog("Media: subscribed", LogMedia);
                        _nowPlayingSender.Subscribe();
                        _audioLevelSender.Subscribe();
                        _nowPlayingImageSender.Subscribe();
                        break;

                    case UNSUBSCRIBE:
                        AppendLog("Media: unsubscribed", LogMedia);
                        _nowPlayingSender.Unsubscribe();
                        _audioLevelSender.Unsubscribe();
                        _nowPlayingImageSender.Unsubscribe();
                        break;

                    case PLAY_PAUSE:
                        _nowPlaying.TogglePlayPause();
                        AppendLog("Media: play/pause (from ESP)", LogMedia);
                        break;

                    case NEXT:
                        _nowPlaying.Next();
                        AppendLog("Media: next (from ESP)", LogMedia);
                        break;

                    case PREV:
                        _nowPlaying.Previous();
                        AppendLog("Media: prev (from ESP)", LogMedia);
                        break;

                    default:
                        AppendLog($"Media: unknown cmd 0x{cmd:X2}", LogMisc);
                        break;
                }
            }));
        }

        private void OnMediaSeek(uint positionMs)
        {
            this.BeginInvoke((Action)(() =>
            {
                _nowPlaying.SeekTo(TimeSpan.FromMilliseconds(positionMs));
                AppendLog($"Media: seek to {TimeSpan.FromMilliseconds(positionMs):hh\\:mm\\:ss} (from ESP)", LogMedia);
            }));
        }

        private void OnModeReport(HidReceiver.EspMode mode)
        {
            this.BeginInvoke((Action)(() =>
            {
                _canvas.OnModeReport(mode);
                switch (mode)
                {
                    case HidReceiver.EspMode.Canvas:
                        AppendLog("Canvas: ESP in canvas mode", LogCanvas);
                        break;

                    case HidReceiver.EspMode.Monitor:
                        AppendLog("Monitor: ESP in monitor mode, subscribed", LogMonitor);
                        _monitor.Subscribe();
                        break;

                    case HidReceiver.EspMode.Media:
                        // Covers the case where the ESP was already on the
                        // Media page before this app started (or restarted)
                        // -- ESP already sent its one-shot subscribe while
                        // nothing was listening, so without this the PC side
                        // would never start sending and the ESP would be
                        // stuck showing its own local fake-data fallback.
                        AppendLog("Media: ESP in media mode, subscribed", LogMedia);
                        _nowPlayingSender.Subscribe();
                        _audioLevelSender.Subscribe();
                        _nowPlayingImageSender.Subscribe();
                        break;

                    case HidReceiver.EspMode.Touchpad:
                        AppendLog("Touchpad: ESP in touchpad mode", LogTouchpad);
                        break;

                    case HidReceiver.EspMode.Deck:
                        AppendLog("Deck: ESP in deck mode", LogDeck);
                        break;

                    default:
                        // a mode this build doesn't know yet (newer firmware)
                        AppendLog($"Mode: ESP reported unknown mode {(int)mode}", LogMisc);
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
            SingleWindow.Show(() => new FormDeckEditor());   // already open: brought to the front
        }

        // ------------------------------------------------------------------
        // Device menu -- firmware update over HID (no Storage Mode / SD card)
        // ------------------------------------------------------------------

        private void tsMenuFirmware_Click(object sender, EventArgs e)
        {
            if (_otaRunning) return;
            if (!_hidConnected)
            {
                MessageBox.Show("ESDeck is not connected.", "Firmware Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string path;
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select ESDeck firmware (.bin)";
                dlg.Filter = "Firmware image (*.bin)|*.bin";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                path = dlg.FileName;
            }

            byte[] image;
            try { image = File.ReadAllBytes(path); }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot read file:\n{ex.Message}", "Firmware Update",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string err = OtaUploader.Validate(image, out string project, out string version);
            if (err != null)
            {
                MessageBox.Show(err, "Firmware Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(
                $"Update ESDeck firmware?\n\n{Path.GetFileName(path)}\n" +
                $"Project: {project}\nVersion: {version}\nSize: {image.Length / 1024} KB\n\n" +
                "Don't unplug the device until it reboots.",
                "Firmware Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _otaRunning = true;
            _canvas.Deactivate("firmware update");
            _tsMenuFirmware.Enabled = false;
            _trayMenuFirmware.Enabled = false;

            // Keep the HID channel for the update: stop streaming Monitor /
            // Media data (the device ignores it during the update anyway).
            // Time sync and mode query keep running.
            _monitor.Unsubscribe();
            _nowPlayingSender.Unsubscribe();
            _audioLevelSender.Unsubscribe();
            _nowPlayingImageSender.Unsubscribe();

            System.Threading.Tasks.Task.Run(() =>
            {
                bool ok = _ota.Run(image);
                BeginInvoke((Action)(() =>
                {
                    _otaRunning = false;
                    _tsMenuFirmware.Enabled = true;
                    _trayMenuFirmware.Enabled = true;
                    if (!ok)
                    {
                        // Device stays on the old firmware in whatever mode it
                        // was in -- ask for it, OnModeReport re-subscribes.
                        _receiver.SendModeQuery();
                        AppendLog("HID: sent mode query", LogMisc);
                        MessageBox.Show("Firmware update failed -- see the log. The device keeps its current firmware.",
                            "Firmware Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    // On success the device reboots; the reconnect path
                    // (timerHid_Tick) queries the mode as usual.
                }));
            });
        }

        // ------------------------------------------------------------------
        // Discord menu handlers
        // ------------------------------------------------------------------

        private void tsMenuDiscordReconnect_Click(object sender, EventArgs e)
        {
            AppendLog("Discord: reconnecting...", LogDiscord);
            _ = DiscordRpcClient.Instance.ConnectAsync();
        }

        // ------------------------------------------------------------------
        // Monitor
        // ------------------------------------------------------------------
        private void tsMenuMonitorEdit_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormMonitorEditor());
        }

        // ------------------------------------------------------------------
        // Media
        // ------------------------------------------------------------------
        private void tsMenuMediaEdit_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormMediaEditor());
        }

        // ------------------------------------------------------------------
        // Touchpad
        // ------------------------------------------------------------------
        private void tsMenuTouchpadEdit_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormTouchpadEditor());
        }

        // ------------------------------------------------------------------
        // Assets
        // ------------------------------------------------------------------

        private void tsMenuAssetsFontClock_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormFontClockBuilder());
        }

        private void tsMenuAssetsFontNotify_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormFontNotifyBuilder());
        }

        private void tsMenuAssetsBg_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormBgImporter());
        }

        private void tsMenuAssetsIco_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormIcoImporter());
        }

        private void tsMenuAssetsSideIcon_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormSideIconImporter());
        }

        private void tsMenuAssetsBootAnim_Click(object sender, EventArgs e)
        {
            SingleWindow.Show(() => new FormBootAnimConverter());
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
            if (e.CloseReason != CloseReason.UserClosing)
            {
                // Windows shutdown / logoff, Task Manager, Application.Exit:
                // really quit. Cancelling here would block shutdown (Windows
                // counts the veto as soon as any window of ours is visible,
                // e.g. the off-screen Canvas renderer).
                Shutdown();
                Program.ReleaseLock();   // the process may be killed before Main's finally
                return;
            }
            // X button: keep running in the tray
            e.Cancel = true;
            this.Hide();
            notifyIcon.ShowBalloonTip(1000, "ESDeck PC",
                                      "Running in background", ToolTipIcon.Info);
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e) => ShowMainWindow();
        private void trayMenuOpen_Click(object sender, EventArgs e) => ShowMainWindow();

        private void trayMenuExit_Click(object sender, EventArgs e)
        {
            Shutdown();
            Application.Exit();
        }

        /// <summary>Stops everything (tray Exit, Windows shutdown). Once only.</summary>
        private void Shutdown()
        {
            if (_forceClose) return;
            _forceClose = true;
            _monitor.Dispose();
            _nowPlaying.Dispose();
            _nowPlayingSender.Dispose();
            _nowPlayingImageSender.Dispose();
            _audioLevel.Dispose();
            _audioLevelSender.Dispose();
            _canvas.Dispose();
            _receiver.Stop();
            DiscordRpcClient.Instance.Dispose();
            notifyIcon.Visible = false;
        }

        private void SetTrayConnected(bool connected)
        {
            notifyIcon.Icon = connected ? _trayIconOn : _trayIconOff;
            notifyIcon.Text = connected ? "ESDeck PC - Connected" : "ESDeck PC - Disconnected";
        }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // Grayed copy of the app icon (same look as a disabled toolbar image).
        // Made once at startup; the GDI handle is copied into a managed Icon
        // and freed right away.
        private static Icon MakeGrayIcon(Icon src)
        {
            using (var bmp = src.ToBitmap())
            using (var gray = (Bitmap)ToolStripRenderer.CreateDisabledImage(bmp))
            {
                IntPtr h = gray.GetHicon();
                try
                {
                    using (var tmp = Icon.FromHandle(h))
                        return (Icon)tmp.Clone();
                }
                finally { DestroyIcon(h); }
            }
        }

        /// <summary>Canvas on the PC (one window; shows Canvas while the ESP
        /// is in another mode, see FormCanvasWindow).</summary>
        private void ShowCanvasWindow()
        {
            if (_canvas == null) return;
            if (_canvasWindow == null || _canvasWindow.IsDisposed)
            {
                _canvasWindow = new FormCanvasWindow(_canvas);
                _canvasWindow.FormClosed += (s, e) => _canvasWindow = null;
                _canvasWindow.Show();
            }
            else
            {
                if (_canvasWindow.WindowState == FormWindowState.Minimized)
                    _canvasWindow.WindowState = FormWindowState.Normal;
                _canvasWindow.Activate();
            }
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