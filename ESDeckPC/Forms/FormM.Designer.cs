namespace ESDeckPC
{
    partial class FormM
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsMenuDeck = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuDeckEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuMonitor = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuMonitorEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuMeida = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuMediaEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuTouchpad = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuTouchpadEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuDiscord = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuDiscordReconnect = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssets = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuAssetsBg = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsIco = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsSideIcon = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsFont = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsFontClock = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsFontNotify = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuAssetsBootAnim = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnClearLog = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.ssLblHid = new System.Windows.Forms.ToolStripStatusLabel();
            this.ssLblDiscord = new System.Windows.Forms.ToolStripStatusLabel();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.timerHid = new System.Windows.Forms.Timer(this.components);
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayMenuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.trayMenuSep = new System.Windows.Forms.ToolStripSeparator();
            this.trayMenuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.trayMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.toolStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuDeck,
            this.tsMenuMonitor,
            this.tsMenuMeida,
            this.tsMenuTouchpad,
            this.tsMenuDiscord,
            this.tsMenuAssets,
            this.tsSep1,
            this.tsBtnClearLog});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(860, 25);
            this.toolStrip.TabIndex = 0;
            // 
            // tsMenuDeck
            // 
            this.tsMenuDeck.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuDeckEdit});
            this.tsMenuDeck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuDeck.Name = "tsMenuDeck";
            this.tsMenuDeck.ShowDropDownArrow = false;
            this.tsMenuDeck.Size = new System.Drawing.Size(39, 22);
            this.tsMenuDeck.Text = "Deck";
            // 
            // tsMenuDeckEdit
            // 
            this.tsMenuDeckEdit.Name = "tsMenuDeckEdit";
            this.tsMenuDeckEdit.Size = new System.Drawing.Size(145, 22);
            this.tsMenuDeckEdit.Text = "Edit";
            this.tsMenuDeckEdit.Click += new System.EventHandler(this.tsMenuDeckEdit_Click);
            // 
            // tsMenuMonitor
            // 
            this.tsMenuMonitor.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuMonitorEdit});
            this.tsMenuMonitor.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuMonitor.Name = "tsMenuMonitor";
            this.tsMenuMonitor.ShowDropDownArrow = false;
            this.tsMenuMonitor.Size = new System.Drawing.Size(57, 22);
            this.tsMenuMonitor.Text = "Monitor";
            // 
            // tsMenuMonitorEdit
            // 
            this.tsMenuMonitorEdit.Name = "tsMenuMonitorEdit";
            this.tsMenuMonitorEdit.Size = new System.Drawing.Size(96, 22);
            this.tsMenuMonitorEdit.Text = "Edit";
            this.tsMenuMonitorEdit.Click += new System.EventHandler(this.tsMenuMonitorEdit_Click);
            // 
            // tsMenuMeida
            // 
            this.tsMenuMeida.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuMediaEdit});
            this.tsMenuMeida.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuMeida.Name = "tsMenuMeida";
            this.tsMenuMeida.ShowDropDownArrow = false;
            this.tsMenuMeida.Size = new System.Drawing.Size(48, 22);
            this.tsMenuMeida.Text = "Media";
            // 
            // tsMenuMediaEdit
            // 
            this.tsMenuMediaEdit.Name = "tsMenuMediaEdit";
            this.tsMenuMediaEdit.Size = new System.Drawing.Size(96, 22);
            this.tsMenuMediaEdit.Text = "Edit";
            this.tsMenuMediaEdit.Click += new System.EventHandler(this.tsMenuMediaEdit_Click);
            // 
            // tsMenuTouchpad
            // 
            this.tsMenuTouchpad.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuTouchpadEdit});
            this.tsMenuTouchpad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuTouchpad.Name = "tsMenuTouchpad";
            this.tsMenuTouchpad.ShowDropDownArrow = false;
            this.tsMenuTouchpad.Size = new System.Drawing.Size(66, 22);
            this.tsMenuTouchpad.Text = "TouchPad";
            // 
            // tsMenuTouchpadEdit
            // 
            this.tsMenuTouchpadEdit.Name = "tsMenuTouchpadEdit";
            this.tsMenuTouchpadEdit.Size = new System.Drawing.Size(96, 22);
            this.tsMenuTouchpadEdit.Text = "Edit";
            this.tsMenuTouchpadEdit.Click += new System.EventHandler(this.tsMenuTouchpadEdit_Click);
            // 
            // tsMenuDiscord
            // 
            this.tsMenuDiscord.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuDiscordReconnect});
            this.tsMenuDiscord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuDiscord.Name = "tsMenuDiscord";
            this.tsMenuDiscord.ShowDropDownArrow = false;
            this.tsMenuDiscord.Size = new System.Drawing.Size(54, 22);
            this.tsMenuDiscord.Text = "Discord";
            // 
            // tsMenuDiscordReconnect
            // 
            this.tsMenuDiscordReconnect.Name = "tsMenuDiscordReconnect";
            this.tsMenuDiscordReconnect.Size = new System.Drawing.Size(134, 22);
            this.tsMenuDiscordReconnect.Text = "Reconnect";
            this.tsMenuDiscordReconnect.Click += new System.EventHandler(this.tsMenuDiscordReconnect_Click);
            // 
            // tsMenuAssets
            // 
            this.tsMenuAssets.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuAssetsBg,
            this.tsMenuAssetsIco,
            this.tsMenuAssetsSideIcon,
            this.tsMenuAssetsFont,
            this.tsMenuAssetsBootAnim});
            this.tsMenuAssets.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuAssets.Name = "tsMenuAssets";
            this.tsMenuAssets.ShowDropDownArrow = false;
            this.tsMenuAssets.Size = new System.Drawing.Size(45, 22);
            this.tsMenuAssets.Text = "Assets";
            // 
            // tsMenuAssetsBg
            // 
            this.tsMenuAssetsBg.Name = "tsMenuAssetsBg";
            this.tsMenuAssetsBg.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsBg.Text = "Background";
            this.tsMenuAssetsBg.Click += new System.EventHandler(this.tsMenuAssetsBg_Click);
            // 
            // tsMenuAssetsIco
            // 
            this.tsMenuAssetsIco.Name = "tsMenuAssetsIco";
            this.tsMenuAssetsIco.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsIco.Text = "Icon";
            this.tsMenuAssetsIco.Click += new System.EventHandler(this.tsMenuAssetsIco_Click);
            // 
            // tsMenuAssetsSideIcon
            // 
            this.tsMenuAssetsSideIcon.Name = "tsMenuAssetsSideIcon";
            this.tsMenuAssetsSideIcon.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsSideIcon.Text = "Side Icon";
            this.tsMenuAssetsSideIcon.Click += new System.EventHandler(this.tsMenuAssetsSideIcon_Click);
            // 
            // tsMenuAssetsFont
            // 
            this.tsMenuAssetsFont.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuAssetsFontClock,
            this.tsMenuAssetsFontNotify});
            this.tsMenuAssetsFont.Name = "tsMenuAssetsFont";
            this.tsMenuAssetsFont.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsFont.Text = "Font";
            // 
            // tsMenuAssetsFontClock
            // 
            this.tsMenuAssetsFontClock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuAssetsFontClock.Name = "tsMenuAssetsFontClock";
            this.tsMenuAssetsFontClock.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsFontClock.Text = "Clock";
            this.tsMenuAssetsFontClock.Click += new System.EventHandler(this.tsMenuAssetsFontClock_Click);
            //
            // tsMenuAssetsFontNotify
            //
            this.tsMenuAssetsFontNotify.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuAssetsFontNotify.Name = "tsMenuAssetsFontNotify";
            this.tsMenuAssetsFontNotify.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsFontNotify.Text = "Notify";
            this.tsMenuAssetsFontNotify.Click += new System.EventHandler(this.tsMenuAssetsFontNotify_Click);
            //
            // tsMenuAssetsBootAnim
            // 
            this.tsMenuAssetsBootAnim.Name = "tsMenuAssetsBootAnim";
            this.tsMenuAssetsBootAnim.Size = new System.Drawing.Size(180, 22);
            this.tsMenuAssetsBootAnim.Text = "Boot Animation";
            this.tsMenuAssetsBootAnim.Click += new System.EventHandler(this.tsMenuAssetsBootAnim_Click);
            // 
            // tsSep1
            // 
            this.tsSep1.Name = "tsSep1";
            this.tsSep1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsBtnClearLog
            // 
            this.tsBtnClearLog.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsBtnClearLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsBtnClearLog.Name = "tsBtnClearLog";
            this.tsBtnClearLog.Size = new System.Drawing.Size(65, 22);
            this.tsBtnClearLog.Text = "Clear Log";
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.statusStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ssLblHid,
            this.ssLblDiscord});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(860, 24);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 1;
            // 
            // ssLblHid
            // 
            this.ssLblHid.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ssLblHid.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ssLblHid.Name = "ssLblHid";
            this.ssLblHid.Size = new System.Drawing.Size(115, 19);
            this.ssLblHid.Text = "HID: Disconnected";
            // 
            // ssLblDiscord
            // 
            this.ssLblDiscord.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ssLblDiscord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ssLblDiscord.Name = "ssLblDiscord";
            this.ssLblDiscord.Size = new System.Drawing.Size(137, 19);
            this.ssLblDiscord.Text = "Discord: Disconnected";
            // 
            // grpLog
            // 
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpLog.Location = new System.Drawing.Point(0, 25);
            this.grpLog.Name = "grpLog";
            this.grpLog.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpLog.Size = new System.Drawing.Size(860, 403);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Log";
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.txtLog.Location = new System.Drawing.Point(8, 19);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(844, 376);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            // 
            // timerHid
            // 
            this.timerHid.Enabled = true;
            this.timerHid.Interval = 2000;
            this.timerHid.Tick += new System.EventHandler(this.timerHid_Tick);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.trayMenu;
            this.notifyIcon.Text = "ESDeck PC";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            // 
            // trayMenu
            // 
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trayMenuOpen,
            this.trayMenuSep,
            this.trayMenuExit});
            this.trayMenu.Name = "trayMenu";
            this.trayMenu.Size = new System.Drawing.Size(103, 54);
            // 
            // trayMenuOpen
            // 
            this.trayMenuOpen.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.trayMenuOpen.Name = "trayMenuOpen";
            this.trayMenuOpen.Size = new System.Drawing.Size(102, 22);
            this.trayMenuOpen.Text = "Open";
            this.trayMenuOpen.Click += new System.EventHandler(this.trayMenuOpen_Click);
            // 
            // trayMenuSep
            // 
            this.trayMenuSep.Name = "trayMenuSep";
            this.trayMenuSep.Size = new System.Drawing.Size(99, 6);
            // 
            // trayMenuExit
            // 
            this.trayMenuExit.Name = "trayMenuExit";
            this.trayMenuExit.Size = new System.Drawing.Size(102, 22);
            this.trayMenuExit.Text = "Exit";
            this.trayMenuExit.Click += new System.EventHandler(this.trayMenuExit_Click);
            // 
            // FormM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(860, 452);
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "FormM";
            this.Text = "ESDeck PC";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormM_FormClosing);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.trayMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuDiscord;
        private System.Windows.Forms.ToolStripMenuItem tsMenuDiscordReconnect;
        private System.Windows.Forms.ToolStripSeparator tsSep1;
        private System.Windows.Forms.ToolStripButton tsBtnClearLog;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel ssLblHid;
        private System.Windows.Forms.ToolStripStatusLabel ssLblDiscord;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Timer timerHid;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem trayMenuOpen;
        private System.Windows.Forms.ToolStripSeparator trayMenuSep;
        private System.Windows.Forms.ToolStripMenuItem trayMenuExit;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuMonitor;
        private System.Windows.Forms.ToolStripMenuItem tsMenuMonitorEdit;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuDeck;
        private System.Windows.Forms.ToolStripMenuItem tsMenuDeckEdit;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuAssets;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsBg;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsIco;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsSideIcon;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsBootAnim;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsFont;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuMeida;
        private System.Windows.Forms.ToolStripMenuItem tsMenuMediaEdit;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuTouchpad;
        private System.Windows.Forms.ToolStripMenuItem tsMenuTouchpadEdit;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsFontClock;
        private System.Windows.Forms.ToolStripMenuItem tsMenuAssetsFontNotify;
    }
}