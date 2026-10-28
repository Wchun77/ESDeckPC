using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Canvas on the PC: the ESP's whole Canvas screen (sidebar + page,
    /// 800 x 480) in a window, for when the ESP is in another mode.
    /// Canvas runs on one side at a time (see CanvasLink): while the ESP is
    /// in Canvas mode this only says so, and it picks the page back up as
    /// soon as the ESP leaves. The page is the same WebView that renders
    /// for the ESP, moved in here, so its state carries over both ways.
    /// Sidebar: tap a page to switch, the gear opens the Canvas Editor.
    /// "Always on Top" is in the window menu (right-click the title bar).
    /// Resizable down from 800 x 480, kept at that ratio; sidebar and page
    /// scale together (the page layout stays 720 x 480). Both remembered.
    /// </summary>
    public partial class FormCanvasWindow : Form
    {
        private readonly CanvasLink _link;
        private CanvasConfig _cfg;
        private bool _pcOwned;
        private int _selected = -1;

        public FormCanvasWindow()
        {
            InitializeComponent();
        }

        public FormCanvasWindow(CanvasLink link) : this()
        {
            _link = link;
        }

        /// <summary>Panel the WebView is moved into (3:2, scaled with the window).</summary>
        internal Control PageHost => panelPage;

        // ------------------------------------------------------------------
        // Always on Top (window menu)
        // ------------------------------------------------------------------

        [DllImport("user32.dll")] private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")] private static extern uint CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private const int WM_SYSCOMMAND = 0x0112;
        private const uint MF_STRING = 0x0000, MF_SEPARATOR = 0x0800, MF_CHECKED = 0x0008, MF_UNCHECKED = 0x0000;
        private const uint IdTopMost = 0x1F10;   // below 0xF000 (system commands), low 4 bits 0

        private static readonly string PrefsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESDeckPC", "CanvasWindow.txt");

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int dark = 1;   // dark title bar, same as the other editors
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            IntPtr menu = GetSystemMenu(Handle, false);
            AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            AppendMenu(menu, MF_STRING, (UIntPtr)IdTopMost, "Always on Top");
            SetTopMost(LoadPref("topmost") == "1", false);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && ((uint)m.WParam.ToInt64() & 0xFFF0) == IdTopMost)
            {
                SetTopMost(!TopMost, true);
                return;
            }
            if (m.Msg == WM_SIZING)
            {
                KeepRatio(m.WParam.ToInt32(), m.LParam);
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        private void SetTopMost(bool on, bool save)
        {
            TopMost = on;
            CheckMenuItem(GetSystemMenu(Handle, false), IdTopMost, on ? MF_CHECKED : MF_UNCHECKED);
            if (save) SavePref("topmost", on ? "1" : "0");
        }

        // ------------------------------------------------------------------
        // Size: 800:480, width MinWidth..800 -- the page (emulated 720 x 480
        // viewport) can be drawn smaller but not larger. No Maximize: a
        // maximized window can't keep the ratio.
        // ------------------------------------------------------------------

        private const int BaseW = 800, BaseH = 480, MinWidth = 400;
        private const int WM_SIZING = 0x0214;
        private const int WMSZ_LEFT = 1, WMSZ_TOP = 3, WMSZ_TOPLEFT = 4, WMSZ_TOPRIGHT = 5, WMSZ_BOTTOM = 6, WMSZ_BOTTOMLEFT = 7;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        /// <summary>WM_SIZING: bend the dragged rectangle so the client area
        /// stays 800:480 (the edge being dragged decides which side moves).</summary>
        private void KeepRatio(int edge, IntPtr lParam)
        {
            var r = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
            int ncW = Width - ClientSize.Width, ncH = Height - ClientSize.Height;
            int cw = r.Right - r.Left - ncW, ch = r.Bottom - r.Top - ncH;
            if (edge == WMSZ_TOP || edge == WMSZ_BOTTOM) cw = ch * BaseW / BaseH;
            cw = Math.Max(MinWidth, Math.Min(BaseW, cw));   // WebView2 only scales the page down
            ch = cw * BaseH / BaseW;

            if (edge == WMSZ_LEFT || edge == WMSZ_TOPLEFT || edge == WMSZ_BOTTOMLEFT) r.Left = r.Right - (cw + ncW);
            else r.Right = r.Left + cw + ncW;
            if (edge == WMSZ_TOP || edge == WMSZ_TOPLEFT || edge == WMSZ_TOPRIGHT) r.Top = r.Bottom - (ch + ncH);
            else r.Bottom = r.Top + ch + ncH;
            Marshal.StructureToPtr(r, lParam, false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int w = int.TryParse(LoadPref("width"), out int saved) ? saved : BaseW;
            var wa = Screen.FromControl(this).WorkingArea;
            int maxW = Math.Max(MinWidth, Math.Min(BaseW, wa.Width - (Width - ClientSize.Width)));
            maxW = Math.Max(MinWidth, Math.Min(maxW,
                                       (wa.Height - (Height - ClientSize.Height)) * BaseW / BaseH));
            w = Math.Max(MinWidth, Math.Min(w, maxW));
            MaximumSize = SizeFromClientSize(new Size(BaseW, BaseH));   // also against Aero Snap, which skips WM_SIZING
            ClientSize = new Size(w, w * BaseH / BaseW);
            CenterToScreen();
            LayoutScaled();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState != FormWindowState.Minimized) LayoutScaled();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            SavePref("width", ClientSize.Width.ToString());
        }

        /// <summary>Sidebar 80 + page 720 of every 800 px of width.</summary>
        private void LayoutScaled()
        {
            if (picSide == null || ClientSize.Width < 1) return;
            int w = ClientSize.Width, h = ClientSize.Height;
            int side = (int)Math.Round(w * (double)CanvasSidebar.Width / BaseW);
            picSide.Bounds = new Rectangle(0, 0, side, h);
            panelPage.Bounds = new Rectangle(side, 0, w - side, h);
            _link?.PcResized(this);
        }

        // ------------------------------------------------------------------
        // Prefs: key=value lines in %LOCALAPPDATA%\ESDeckPC\CanvasWindow.txt
        // ------------------------------------------------------------------

        private static string LoadPref(string key)
        {
            try
            {
                if (!File.Exists(PrefsPath)) return null;
                foreach (string line in File.ReadAllLines(PrefsPath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0 && line.Substring(0, eq).Trim() == key) return line.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return null;
        }

        private static void SavePref(string key, string value)
        {
            try
            {
                var lines = File.Exists(PrefsPath) ? new System.Collections.Generic.List<string>(File.ReadAllLines(PrefsPath))
                                                   : new System.Collections.Generic.List<string>();
                lines.RemoveAll(l => l.StartsWith(key + "="));
                lines.Add(key + "=" + value);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath));
                File.WriteAllLines(PrefsPath, lines);
            }
            catch { /* not remembered: still applied for this window */ }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            DrawSidebar();
            _link?.OpenWindow(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) _link?.CloseWindow(this);   // before the panel's handle goes away
        }

        // ------------------------------------------------------------------
        // States (called by CanvasLink)
        // ------------------------------------------------------------------

        internal void ShowPcOwned(CanvasConfig cfg, int page)
        {
            _pcOwned = true;
            _cfg = cfg;
            _selected = page;
            lblMessage.Visible = false;
            DrawSidebar();
        }

        internal void ShowEspOwned(CanvasConfig cfg)
        {
            _pcOwned = false;
            _cfg = cfg;
            _selected = -1;
            ShowMessage("Canvas is on the ESDeck right now.\n" +
                        "It continues here when the ESDeck switches to another mode.");
        }

        internal void ShowEmpty(CanvasConfig cfg)
        {
            _pcOwned = false;
            _cfg = cfg;
            _selected = -1;
            ShowMessage("The active config has no pages.\nAdd some in the Canvas Editor (the gear).");
        }

        private void ShowMessage(string text)
        {
            lblMessage.Text = text;
            lblMessage.Visible = true;
            DrawSidebar();
        }

        private void DrawSidebar()
        {
            var old = picSide.Image;
            picSide.Image = CanvasSidebar.Render(_cfg, _selected, false);
            old?.Dispose();
        }

        // ------------------------------------------------------------------
        // Sidebar
        // ------------------------------------------------------------------

        private void picSide_MouseClick(object sender, MouseEventArgs e)
        {
            if (picSide.Height <= 0) return;
            int y = e.Y * CanvasSidebar.Height / picSide.Height;
            int hit = CanvasSidebar.HitTest(y, _cfg?.Pages.Count ?? 0);
            if (hit == CanvasSidebar.HitGear)
                SingleWindow.Show(() => new FormCanvasEditor(_link?.Bridge));
            else if (hit >= 0 && _pcOwned && hit != _selected)
                _link?.PcSelect(hit);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picSide.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
