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
            this.tsMenuSettingsLoad = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuSettingsEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuSettingsReload = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuSettingsOpenFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuMonitor = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuMonitorEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuFontBuilder = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuDiscord = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuDiscordReconnect = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuImage = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMenuImageBg = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuImageIco = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuImageSideIcon = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMenuImageBootAnim = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnClearLog = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.ssLblHid = new System.Windows.Forms.ToolStripStatusLabel();
            this.ssLblJson = new System.Windows.Forms.ToolStripStatusLabel();
            this.ssLblDiscord = new System.Windows.Forms.ToolStripStatusLabel();
            this.ssLblPages = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.grpPages = new System.Windows.Forms.GroupBox();
            this.lstPages = new System.Windows.Forms.ListBox();
            this.grpFiles = new System.Windows.Forms.GroupBox();
            this.lblPcTag = new System.Windows.Forms.Label();
            this.lblPcName = new System.Windows.Forms.Label();
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
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpPages.SuspendLayout();
            this.grpFiles.SuspendLayout();
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
            this.tsMenuDiscord,
            this.tsMenuImage,
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
            this.tsMenuSettingsLoad,
            this.tsMenuSettingsEdit,
            this.tsMenuSettingsReload,
            this.tsMenuSettingsOpenFolder});
            this.tsMenuDeck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuDeck.Name = "tsMenuDeck";
            this.tsMenuDeck.ShowDropDownArrow = false;
            this.tsMenuDeck.Size = new System.Drawing.Size(39, 22);
            this.tsMenuDeck.Text = "Deck";
            // 
            // tsMenuSettingsLoad
            // 
            this.tsMenuSettingsLoad.Name = "tsMenuSettingsLoad";
            this.tsMenuSettingsLoad.Size = new System.Drawing.Size(145, 22);
            this.tsMenuSettingsLoad.Text = "Load";
            this.tsMenuSettingsLoad.Click += new System.EventHandler(this.tsMenuSettingsLoad_Click);
            // 
            // tsMenuSettingsEdit
            // 
            this.tsMenuSettingsEdit.Name = "tsMenuSettingsEdit";
            this.tsMenuSettingsEdit.Size = new System.Drawing.Size(145, 22);
            this.tsMenuSettingsEdit.Text = "Edit";
            this.tsMenuSettingsEdit.Click += new System.EventHandler(this.tsMenuSettingsEdit_Click);
            // 
            // tsMenuSettingsReload
            // 
            this.tsMenuSettingsReload.Enabled = false;
            this.tsMenuSettingsReload.Name = "tsMenuSettingsReload";
            this.tsMenuSettingsReload.Size = new System.Drawing.Size(145, 22);
            this.tsMenuSettingsReload.Text = "Reload";
            this.tsMenuSettingsReload.Click += new System.EventHandler(this.tsMenuSettingsReload_Click);
            // 
            // tsMenuSettingsOpenFolder
            // 
            this.tsMenuSettingsOpenFolder.Name = "tsMenuSettingsOpenFolder";
            this.tsMenuSettingsOpenFolder.Size = new System.Drawing.Size(145, 22);
            this.tsMenuSettingsOpenFolder.Text = "Open Folder";
            this.tsMenuSettingsOpenFolder.Click += new System.EventHandler(this.tsMenuSettingsOpenFolder_Click);
            // 
            // tsMenuMonitor
            // 
            this.tsMenuMonitor.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuMonitorEdit,
            this.tsMenuFontBuilder});
            this.tsMenuMonitor.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuMonitor.Name = "tsMenuMonitor";
            this.tsMenuMonitor.ShowDropDownArrow = false;
            this.tsMenuMonitor.Size = new System.Drawing.Size(57, 22);
            this.tsMenuMonitor.Text = "Monitor";
            // 
            // tsMenuMonitorEdit
            // 
            this.tsMenuMonitorEdit.Name = "tsMenuMonitorEdit";
            this.tsMenuMonitorEdit.Size = new System.Drawing.Size(186, 22);
            this.tsMenuMonitorEdit.Text = "Edit Monitor Config";
            this.tsMenuMonitorEdit.Click += new System.EventHandler(this.tsMenuMonitorEdit_Click);
            // 
            // tsMenuFontBuilder
            // 
            this.tsMenuFontBuilder.Name = "tsMenuFontBuilder";
            this.tsMenuFontBuilder.Size = new System.Drawing.Size(186, 22);
            this.tsMenuFontBuilder.Text = "Font Builder...";
            this.tsMenuFontBuilder.Click += new System.EventHandler(this.tsMenuFontBuilder_Click);
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
            this.tsMenuDiscordReconnect.Size = new System.Drawing.Size(180, 22);
            this.tsMenuDiscordReconnect.Text = "Reconnect";
            this.tsMenuDiscordReconnect.Click += new System.EventHandler(this.tsMenuDiscordReconnect_Click);
            // 
            // tsMenuImage
            // 
            this.tsMenuImage.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMenuImageBg,
            this.tsMenuImageIco,
            this.tsMenuImageSideIcon,
            this.tsMenuImageBootAnim});
            this.tsMenuImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tsMenuImage.Name = "tsMenuImage";
            this.tsMenuImage.ShowDropDownArrow = false;
            this.tsMenuImage.Size = new System.Drawing.Size(47, 22);
            this.tsMenuImage.Text = "Image";
            // 
            // tsMenuImageBg
            // 
            this.tsMenuImageBg.Name = "tsMenuImageBg";
            this.tsMenuImageBg.Size = new System.Drawing.Size(180, 22);
            this.tsMenuImageBg.Text = "Background";
            this.tsMenuImageBg.Click += new System.EventHandler(this.tsMenuImageBg_Click);
            // 
            // tsMenuImageIco
            // 
            this.tsMenuImageIco.Name = "tsMenuImageIco";
            this.tsMenuImageIco.Size = new System.Drawing.Size(180, 22);
            this.tsMenuImageIco.Text = "Icon";
            this.tsMenuImageIco.Click += new System.EventHandler(this.tsMenuImageIco_Click);
            // 
            // tsMenuImageSideIcon
            // 
            this.tsMenuImageSideIcon.Name = "tsMenuImageSideIcon";
            this.tsMenuImageSideIcon.Size = new System.Drawing.Size(180, 22);
            this.tsMenuImageSideIcon.Text = "Side Icon";
            this.tsMenuImageSideIcon.Click += new System.EventHandler(this.tsMenuImageSideIcon_Click);
            //
            // tsMenuImageBootAnim
            //
            this.tsMenuImageBootAnim.Name = "tsMenuImageBootAnim";
            this.tsMenuImageBootAnim.Size = new System.Drawing.Size(180, 22);
            this.tsMenuImageBootAnim.Text = "Boot Animation";
            this.tsMenuImageBootAnim.Click += new System.EventHandler(this.tsMenuImageBootAnim_Click);
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
            this.ssLblJson,
            this.ssLblDiscord,
            this.ssLblPages});
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
            // ssLblJson
            // 
            this.ssLblJson.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ssLblJson.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ssLblJson.Name = "ssLblJson";
            this.ssLblJson.Size = new System.Drawing.Size(115, 19);
            this.ssLblJson.Text = "JSON: Not loaded";
            // 
            // ssLblDiscord
            // 
            this.ssLblDiscord.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.ssLblDiscord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ssLblDiscord.Name = "ssLblDiscord";
            this.ssLblDiscord.Size = new System.Drawing.Size(137, 19);
            this.ssLblDiscord.Text = "Discord: Disconnected";
            // 
            // ssLblPages
            // 
            this.ssLblPages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ssLblPages.Name = "ssLblPages";
            this.ssLblPages.Size = new System.Drawing.Size(478, 19);
            this.ssLblPages.Spring = true;
            this.ssLblPages.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // splitMain
            // 
            this.splitMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 25);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.splitMain.Panel1.Controls.Add(this.grpPages);
            this.splitMain.Panel1.Controls.Add(this.grpFiles);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.splitMain.Panel2.Controls.Add(this.grpLog);
            this.splitMain.Size = new System.Drawing.Size(860, 403);
            this.splitMain.SplitterDistance = 121;
            this.splitMain.TabIndex = 2;
            // 
            // grpPages
            // 
            this.grpPages.Controls.Add(this.lstPages);
            this.grpPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpPages.Location = new System.Drawing.Point(0, 61);
            this.grpPages.Name = "grpPages";
            this.grpPages.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpPages.Size = new System.Drawing.Size(121, 342);
            this.grpPages.TabIndex = 1;
            this.grpPages.TabStop = false;
            this.grpPages.Text = "Pages";
            // 
            // lstPages
            // 
            this.lstPages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.lstPages.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPages.Font = new System.Drawing.Font("Consolas", 9F);
            this.lstPages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lstPages.ItemHeight = 14;
            this.lstPages.Location = new System.Drawing.Point(8, 19);
            this.lstPages.Name = "lstPages";
            this.lstPages.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.lstPages.Size = new System.Drawing.Size(105, 315);
            this.lstPages.TabIndex = 0;
            // 
            // grpFiles
            // 
            this.grpFiles.Controls.Add(this.lblPcTag);
            this.grpFiles.Controls.Add(this.lblPcName);
            this.grpFiles.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpFiles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpFiles.Location = new System.Drawing.Point(0, 0);
            this.grpFiles.Name = "grpFiles";
            this.grpFiles.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpFiles.Size = new System.Drawing.Size(121, 61);
            this.grpFiles.TabIndex = 0;
            this.grpFiles.TabStop = false;
            this.grpFiles.Text = "JSON";
            // 
            // lblPcTag
            // 
            this.lblPcTag.AutoSize = true;
            this.lblPcTag.ForeColor = System.Drawing.Color.Gray;
            this.lblPcTag.Location = new System.Drawing.Point(10, 20);
            this.lblPcTag.Name = "lblPcTag";
            this.lblPcTag.Size = new System.Drawing.Size(21, 14);
            this.lblPcTag.TabIndex = 3;
            this.lblPcTag.Text = "PC";
            // 
            // lblPcName
            // 
            this.lblPcName.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblPcName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblPcName.Location = new System.Drawing.Point(10, 36);
            this.lblPcName.Name = "lblPcName";
            this.lblPcName.Size = new System.Drawing.Size(103, 16);
            this.lblPcName.TabIndex = 4;
            this.lblPcName.Text = "—";
            // 
            // grpLog
            // 
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpLog.Location = new System.Drawing.Point(0, 0);
            this.grpLog.Name = "grpLog";
            this.grpLog.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpLog.Size = new System.Drawing.Size(735, 403);
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
            this.txtLog.Size = new System.Drawing.Size(719, 376);
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
            this.Controls.Add(this.splitMain);
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
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpPages.ResumeLayout(false);
            this.grpFiles.ResumeLayout(false);
            this.grpFiles.PerformLayout();
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
        private System.Windows.Forms.ToolStripStatusLabel ssLblJson;
        private System.Windows.Forms.ToolStripStatusLabel ssLblDiscord;
        private System.Windows.Forms.ToolStripStatusLabel ssLblPages;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpFiles;
        private System.Windows.Forms.Label lblPcTag;
        private System.Windows.Forms.Label lblPcName;
        private System.Windows.Forms.GroupBox grpPages;
        private System.Windows.Forms.ListBox lstPages;
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
        private System.Windows.Forms.ToolStripMenuItem tsMenuFontBuilder;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuDeck;
        private System.Windows.Forms.ToolStripMenuItem tsMenuSettingsLoad;
        private System.Windows.Forms.ToolStripMenuItem tsMenuSettingsEdit;
        private System.Windows.Forms.ToolStripMenuItem tsMenuSettingsReload;
        private System.Windows.Forms.ToolStripMenuItem tsMenuSettingsOpenFolder;
        private System.Windows.Forms.ToolStripDropDownButton tsMenuImage;
        private System.Windows.Forms.ToolStripMenuItem tsMenuImageBg;
        private System.Windows.Forms.ToolStripMenuItem tsMenuImageIco;
        private System.Windows.Forms.ToolStripMenuItem tsMenuImageSideIcon;
        private System.Windows.Forms.ToolStripMenuItem tsMenuImageBootAnim;
    }
}