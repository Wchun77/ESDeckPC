using System;
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
    /// Sidebar: tap a page to switch, the gear opens Canvas Pages.
    /// "Always on Top" is in the window menu (right-click the title bar),
    /// remembered across runs.
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

        /// <summary>Panel the WebView is moved into (720 x 480).</summary>
        internal Control PageHost => panelPage;

        // ------------------------------------------------------------------
        // Always on Top (window menu)
        // ------------------------------------------------------------------

        [DllImport("user32.dll")] private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")] private static extern uint CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);

        private const int WM_SYSCOMMAND = 0x0112;
        private const uint MF_STRING = 0x0000, MF_SEPARATOR = 0x0800, MF_CHECKED = 0x0008, MF_UNCHECKED = 0x0000;
        private const uint IdTopMost = 0x1F10;   // below 0xF000 (system commands), low 4 bits 0

        private static readonly string PrefsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESDeckPC", "CanvasWindow.txt");

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            IntPtr menu = GetSystemMenu(Handle, false);
            AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            AppendMenu(menu, MF_STRING, (UIntPtr)IdTopMost, "Always on Top");
            SetTopMost(LoadTopMost(), false);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && ((uint)m.WParam.ToInt64() & 0xFFF0) == IdTopMost)
            {
                SetTopMost(!TopMost, true);
                return;
            }
            base.WndProc(ref m);
        }

        private void SetTopMost(bool on, bool save)
        {
            TopMost = on;
            CheckMenuItem(GetSystemMenu(Handle, false), IdTopMost, on ? MF_CHECKED : MF_UNCHECKED);
            if (!save) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath));
                File.WriteAllText(PrefsPath, on ? "topmost=1" : "topmost=0");
            }
            catch { /* not remembered: still applied for this window */ }
        }

        private static bool LoadTopMost()
        {
            try { return File.Exists(PrefsPath) && File.ReadAllText(PrefsPath).Trim() == "topmost=1"; }
            catch { return false; }
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
            ShowMessage("The active config has no pages.\nAdd some in Canvas Pages (the gear).");
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
                new FormCanvasEditor(_link?.Bridge).Show();
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
