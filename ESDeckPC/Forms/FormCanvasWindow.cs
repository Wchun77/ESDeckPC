using System;
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
