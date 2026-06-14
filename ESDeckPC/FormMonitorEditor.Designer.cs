namespace ESDeckPC
{
    partial class FormMonitorEditor
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
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.grpBg = new System.Windows.Forms.GroupBox();
            this.txtBgImage = new System.Windows.Forms.TextBox();
            this.btnBgBrowse = new System.Windows.Forms.Button();
            this.btnBgClear = new System.Windows.Forms.Button();
            this.grpColors = new System.Windows.Forms.GroupBox();
            this.lblColTime = new System.Windows.Forms.Label();
            this.btnColTime = new System.Windows.Forms.Button();
            this.lblColColon = new System.Windows.Forms.Label();
            this.btnColColon = new System.Windows.Forms.Button();
            this.lblColDate = new System.Windows.Forms.Label();
            this.btnColDate = new System.Windows.Forms.Button();
            this.lblColDay = new System.Windows.Forms.Label();
            this.btnColDay = new System.Windows.Forms.Button();
            this.lblColSec = new System.Windows.Forms.Label();
            this.btnColSec = new System.Windows.Forms.Button();
            this.lblColSep = new System.Windows.Forms.Label();
            this.btnColSep = new System.Windows.Forms.Button();
            this.lblSepWidthLbl = new System.Windows.Forms.Label();
            this.nudSepWidth = new System.Windows.Forms.NumericUpDown();
            this.grpFonts = new System.Windows.Forms.GroupBox();
            this.lblFontTime = new System.Windows.Forms.Label();
            this.txtFontTime = new System.Windows.Forms.TextBox();
            this.btnFontTime = new System.Windows.Forms.Button();
            this.lblFontTimeNote = new System.Windows.Forms.Label();
            this.lblFontSec = new System.Windows.Forms.Label();
            this.txtFontSec = new System.Windows.Forms.TextBox();
            this.btnFontSec = new System.Windows.Forms.Button();
            this.lblFontSecNote = new System.Windows.Forms.Label();
            this.lblFontDate = new System.Windows.Forms.Label();
            this.txtFontDate = new System.Windows.Forms.TextBox();
            this.btnFontDate = new System.Windows.Forms.Button();
            this.lblFontDateNote = new System.Windows.Forms.Label();
            this.grpJson = new System.Windows.Forms.GroupBox();
            this.lblJsonPath = new System.Windows.Forms.Label();
            this.btnJsonNew = new System.Windows.Forms.Button();
            this.btnJsonOpen = new System.Windows.Forms.Button();
            this.btnJsonSave = new System.Windows.Forms.Button();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.lblStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.grpBg.SuspendLayout();
            this.grpColors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSepWidth)).BeginInit();
            this.grpFonts.SuspendLayout();
            this.grpJson.SuspendLayout();
            this.pnlRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.pnlLeft);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.pnlRight);
            this.splitMain.Size = new System.Drawing.Size(1060, 560);
            this.splitMain.SplitterDistance = 271;
            this.splitMain.TabIndex = 0;
            // 
            // pnlLeft
            // 
            this.pnlLeft.AutoScroll = true;
            this.pnlLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.pnlLeft.Controls.Add(this.grpBg);
            this.pnlLeft.Controls.Add(this.grpColors);
            this.pnlLeft.Controls.Add(this.grpFonts);
            this.pnlLeft.Controls.Add(this.grpJson);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLeft.Location = new System.Drawing.Point(0, 0);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Padding = new System.Windows.Forms.Padding(10, 8, 6, 8);
            this.pnlLeft.Size = new System.Drawing.Size(271, 560);
            this.pnlLeft.TabIndex = 0;
            // 
            // grpBg
            // 
            this.grpBg.Controls.Add(this.txtBgImage);
            this.grpBg.Controls.Add(this.btnBgBrowse);
            this.grpBg.Controls.Add(this.btnBgClear);
            this.grpBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpBg.Location = new System.Drawing.Point(10, 541);
            this.grpBg.Name = "grpBg";
            this.grpBg.Size = new System.Drawing.Size(234, 78);
            this.grpBg.TabIndex = 0;
            this.grpBg.TabStop = false;
            this.grpBg.Text = "Background image";
            // 
            // txtBgImage
            // 
            this.txtBgImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtBgImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtBgImage.Location = new System.Drawing.Point(8, 20);
            this.txtBgImage.Name = "txtBgImage";
            this.txtBgImage.ReadOnly = true;
            this.txtBgImage.Size = new System.Drawing.Size(218, 22);
            this.txtBgImage.TabIndex = 0;
            // 
            // btnBgBrowse
            // 
            this.btnBgBrowse.Location = new System.Drawing.Point(8, 46);
            this.btnBgBrowse.Name = "btnBgBrowse";
            this.btnBgBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnBgBrowse.TabIndex = 1;
            this.btnBgBrowse.Text = "Browse...";
            this.btnBgBrowse.Click += new System.EventHandler(this.BtnBgBrowse_Click);
            // 
            // btnBgClear
            // 
            this.btnBgClear.Location = new System.Drawing.Point(96, 46);
            this.btnBgClear.Name = "btnBgClear";
            this.btnBgClear.Size = new System.Drawing.Size(60, 24);
            this.btnBgClear.TabIndex = 2;
            this.btnBgClear.Text = "Clear";
            this.btnBgClear.Click += new System.EventHandler(this.BtnBgClear_Click);
            // 
            // grpColors
            // 
            this.grpColors.Controls.Add(this.lblColTime);
            this.grpColors.Controls.Add(this.btnColTime);
            this.grpColors.Controls.Add(this.lblColColon);
            this.grpColors.Controls.Add(this.btnColColon);
            this.grpColors.Controls.Add(this.lblColDate);
            this.grpColors.Controls.Add(this.btnColDate);
            this.grpColors.Controls.Add(this.lblColDay);
            this.grpColors.Controls.Add(this.btnColDay);
            this.grpColors.Controls.Add(this.lblColSec);
            this.grpColors.Controls.Add(this.btnColSec);
            this.grpColors.Controls.Add(this.lblColSep);
            this.grpColors.Controls.Add(this.btnColSep);
            this.grpColors.Controls.Add(this.lblSepWidthLbl);
            this.grpColors.Controls.Add(this.nudSepWidth);
            this.grpColors.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpColors.Location = new System.Drawing.Point(10, 305);
            this.grpColors.Name = "grpColors";
            this.grpColors.Size = new System.Drawing.Size(234, 228);
            this.grpColors.TabIndex = 1;
            this.grpColors.TabStop = false;
            this.grpColors.Text = "Colors";
            // 
            // lblColTime
            // 
            this.lblColTime.Location = new System.Drawing.Point(8, 24);
            this.lblColTime.Name = "lblColTime";
            this.lblColTime.Size = new System.Drawing.Size(90, 18);
            this.lblColTime.TabIndex = 0;
            this.lblColTime.Text = "Time";
            // 
            // btnColTime
            // 
            this.btnColTime.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColTime.Location = new System.Drawing.Point(104, 20);
            this.btnColTime.Name = "btnColTime";
            this.btnColTime.Size = new System.Drawing.Size(120, 22);
            this.btnColTime.TabIndex = 1;
            this.btnColTime.Click += new System.EventHandler(this.BtnColTime_Click);
            // 
            // lblColColon
            // 
            this.lblColColon.Location = new System.Drawing.Point(8, 52);
            this.lblColColon.Name = "lblColColon";
            this.lblColColon.Size = new System.Drawing.Size(90, 18);
            this.lblColColon.TabIndex = 2;
            this.lblColColon.Text = "Colon";
            // 
            // btnColColon
            // 
            this.btnColColon.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColColon.Location = new System.Drawing.Point(104, 48);
            this.btnColColon.Name = "btnColColon";
            this.btnColColon.Size = new System.Drawing.Size(120, 22);
            this.btnColColon.TabIndex = 3;
            this.btnColColon.Click += new System.EventHandler(this.BtnColColon_Click);
            // 
            // lblColDate
            // 
            this.lblColDate.Location = new System.Drawing.Point(8, 80);
            this.lblColDate.Name = "lblColDate";
            this.lblColDate.Size = new System.Drawing.Size(90, 18);
            this.lblColDate.TabIndex = 4;
            this.lblColDate.Text = "Date";
            // 
            // btnColDate
            // 
            this.btnColDate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColDate.Location = new System.Drawing.Point(104, 76);
            this.btnColDate.Name = "btnColDate";
            this.btnColDate.Size = new System.Drawing.Size(120, 22);
            this.btnColDate.TabIndex = 5;
            this.btnColDate.Click += new System.EventHandler(this.BtnColDate_Click);
            // 
            // lblColDay
            // 
            this.lblColDay.Location = new System.Drawing.Point(8, 108);
            this.lblColDay.Name = "lblColDay";
            this.lblColDay.Size = new System.Drawing.Size(90, 18);
            this.lblColDay.TabIndex = 6;
            this.lblColDay.Text = "Day";
            // 
            // btnColDay
            // 
            this.btnColDay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColDay.Location = new System.Drawing.Point(104, 104);
            this.btnColDay.Name = "btnColDay";
            this.btnColDay.Size = new System.Drawing.Size(120, 22);
            this.btnColDay.TabIndex = 7;
            this.btnColDay.Click += new System.EventHandler(this.BtnColDay_Click);
            // 
            // lblColSec
            // 
            this.lblColSec.Location = new System.Drawing.Point(8, 136);
            this.lblColSec.Name = "lblColSec";
            this.lblColSec.Size = new System.Drawing.Size(90, 18);
            this.lblColSec.TabIndex = 8;
            this.lblColSec.Text = "Sec";
            // 
            // btnColSec
            // 
            this.btnColSec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColSec.Location = new System.Drawing.Point(104, 132);
            this.btnColSec.Name = "btnColSec";
            this.btnColSec.Size = new System.Drawing.Size(120, 22);
            this.btnColSec.TabIndex = 9;
            this.btnColSec.Click += new System.EventHandler(this.BtnColSec_Click);
            // 
            // lblColSep
            // 
            this.lblColSep.Location = new System.Drawing.Point(8, 164);
            this.lblColSep.Name = "lblColSep";
            this.lblColSep.Size = new System.Drawing.Size(90, 18);
            this.lblColSep.TabIndex = 10;
            this.lblColSep.Text = "Separator";
            // 
            // btnColSep
            // 
            this.btnColSep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColSep.Location = new System.Drawing.Point(104, 160);
            this.btnColSep.Name = "btnColSep";
            this.btnColSep.Size = new System.Drawing.Size(120, 22);
            this.btnColSep.TabIndex = 11;
            this.btnColSep.Click += new System.EventHandler(this.BtnColSep_Click);
            // 
            // lblSepWidthLbl
            // 
            this.lblSepWidthLbl.Location = new System.Drawing.Point(8, 192);
            this.lblSepWidthLbl.Name = "lblSepWidthLbl";
            this.lblSepWidthLbl.Size = new System.Drawing.Size(90, 18);
            this.lblSepWidthLbl.TabIndex = 12;
            this.lblSepWidthLbl.Text = "Sep width";
            // 
            // nudSepWidth
            // 
            this.nudSepWidth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudSepWidth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudSepWidth.Location = new System.Drawing.Point(104, 188);
            this.nudSepWidth.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudSepWidth.Name = "nudSepWidth";
            this.nudSepWidth.Size = new System.Drawing.Size(56, 22);
            this.nudSepWidth.TabIndex = 13;
            this.nudSepWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSepWidth.ValueChanged += new System.EventHandler(this.NudSepWidth_ValueChanged);
            // 
            // grpFonts
            // 
            this.grpFonts.Controls.Add(this.lblFontTime);
            this.grpFonts.Controls.Add(this.txtFontTime);
            this.grpFonts.Controls.Add(this.btnFontTime);
            this.grpFonts.Controls.Add(this.lblFontTimeNote);
            this.grpFonts.Controls.Add(this.lblFontSec);
            this.grpFonts.Controls.Add(this.txtFontSec);
            this.grpFonts.Controls.Add(this.btnFontSec);
            this.grpFonts.Controls.Add(this.lblFontSecNote);
            this.grpFonts.Controls.Add(this.lblFontDate);
            this.grpFonts.Controls.Add(this.txtFontDate);
            this.grpFonts.Controls.Add(this.btnFontDate);
            this.grpFonts.Controls.Add(this.lblFontDateNote);
            this.grpFonts.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpFonts.Location = new System.Drawing.Point(10, 106);
            this.grpFonts.Name = "grpFonts";
            this.grpFonts.Size = new System.Drawing.Size(234, 197);
            this.grpFonts.TabIndex = 2;
            this.grpFonts.TabStop = false;
            this.grpFonts.Text = "Fonts (dump folder)";
            // 
            // lblFontTime
            // 
            this.lblFontTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblFontTime.Location = new System.Drawing.Point(8, 18);
            this.lblFontTime.Name = "lblFontTime";
            this.lblFontTime.Size = new System.Drawing.Size(218, 16);
            this.lblFontTime.TabIndex = 0;
            this.lblFontTime.Text = "Time (HH:MM)";
            // 
            // txtFontTime
            // 
            this.txtFontTime.Location = new System.Drawing.Point(8, 36);
            this.txtFontTime.Name = "txtFontTime";
            this.txtFontTime.ReadOnly = true;
            this.txtFontTime.Size = new System.Drawing.Size(188, 22);
            this.txtFontTime.TabIndex = 1;
            // 
            // btnFontTime
            // 
            this.btnFontTime.Location = new System.Drawing.Point(200, 35);
            this.btnFontTime.Name = "btnFontTime";
            this.btnFontTime.Size = new System.Drawing.Size(28, 22);
            this.btnFontTime.TabIndex = 2;
            this.btnFontTime.Text = "...";
            this.btnFontTime.Click += new System.EventHandler(this.BtnFontTime_Click);
            // 
            // lblFontTimeNote
            // 
            this.lblFontTimeNote.Font = new System.Drawing.Font("Consolas", 7.5F);
            this.lblFontTimeNote.ForeColor = System.Drawing.Color.Gray;
            this.lblFontTimeNote.Location = new System.Drawing.Point(8, 58);
            this.lblFontTimeNote.Name = "lblFontTimeNote";
            this.lblFontTimeNote.Size = new System.Drawing.Size(218, 14);
            this.lblFontTimeNote.TabIndex = 3;
            // 
            // lblFontSec
            // 
            this.lblFontSec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblFontSec.Location = new System.Drawing.Point(8, 76);
            this.lblFontSec.Name = "lblFontSec";
            this.lblFontSec.Size = new System.Drawing.Size(218, 16);
            this.lblFontSec.TabIndex = 4;
            this.lblFontSec.Text = "Sec";
            // 
            // txtFontSec
            // 
            this.txtFontSec.Location = new System.Drawing.Point(8, 94);
            this.txtFontSec.Name = "txtFontSec";
            this.txtFontSec.ReadOnly = true;
            this.txtFontSec.Size = new System.Drawing.Size(188, 22);
            this.txtFontSec.TabIndex = 5;
            // 
            // btnFontSec
            // 
            this.btnFontSec.Location = new System.Drawing.Point(200, 93);
            this.btnFontSec.Name = "btnFontSec";
            this.btnFontSec.Size = new System.Drawing.Size(28, 22);
            this.btnFontSec.TabIndex = 6;
            this.btnFontSec.Text = "...";
            this.btnFontSec.Click += new System.EventHandler(this.BtnFontSec_Click);
            // 
            // lblFontSecNote
            // 
            this.lblFontSecNote.Font = new System.Drawing.Font("Consolas", 7.5F);
            this.lblFontSecNote.ForeColor = System.Drawing.Color.Gray;
            this.lblFontSecNote.Location = new System.Drawing.Point(8, 116);
            this.lblFontSecNote.Name = "lblFontSecNote";
            this.lblFontSecNote.Size = new System.Drawing.Size(218, 14);
            this.lblFontSecNote.TabIndex = 7;
            // 
            // lblFontDate
            // 
            this.lblFontDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblFontDate.Location = new System.Drawing.Point(8, 134);
            this.lblFontDate.Name = "lblFontDate";
            this.lblFontDate.Size = new System.Drawing.Size(218, 16);
            this.lblFontDate.TabIndex = 8;
            this.lblFontDate.Text = "Date";
            // 
            // txtFontDate
            // 
            this.txtFontDate.Location = new System.Drawing.Point(8, 152);
            this.txtFontDate.Name = "txtFontDate";
            this.txtFontDate.ReadOnly = true;
            this.txtFontDate.Size = new System.Drawing.Size(188, 22);
            this.txtFontDate.TabIndex = 9;
            // 
            // btnFontDate
            // 
            this.btnFontDate.Location = new System.Drawing.Point(200, 151);
            this.btnFontDate.Name = "btnFontDate";
            this.btnFontDate.Size = new System.Drawing.Size(28, 22);
            this.btnFontDate.TabIndex = 10;
            this.btnFontDate.Text = "...";
            this.btnFontDate.Click += new System.EventHandler(this.BtnFontDate_Click);
            // 
            // lblFontDateNote
            // 
            this.lblFontDateNote.Font = new System.Drawing.Font("Consolas", 7.5F);
            this.lblFontDateNote.ForeColor = System.Drawing.Color.Gray;
            this.lblFontDateNote.Location = new System.Drawing.Point(8, 174);
            this.lblFontDateNote.Name = "lblFontDateNote";
            this.lblFontDateNote.Size = new System.Drawing.Size(218, 14);
            this.lblFontDateNote.TabIndex = 11;
            // 
            // grpJson
            // 
            this.grpJson.Controls.Add(this.lblJsonPath);
            this.grpJson.Controls.Add(this.btnJsonNew);
            this.grpJson.Controls.Add(this.btnJsonOpen);
            this.grpJson.Controls.Add(this.btnJsonSave);
            this.grpJson.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpJson.Location = new System.Drawing.Point(10, 8);
            this.grpJson.Name = "grpJson";
            this.grpJson.Size = new System.Drawing.Size(234, 90);
            this.grpJson.TabIndex = 3;
            this.grpJson.TabStop = false;
            this.grpJson.Text = "Monitor JSON";
            // 
            // lblJsonPath
            // 
            this.lblJsonPath.Font = new System.Drawing.Font("Consolas", 8.5F);
            this.lblJsonPath.ForeColor = System.Drawing.Color.Gray;
            this.lblJsonPath.Location = new System.Drawing.Point(8, 20);
            this.lblJsonPath.Name = "lblJsonPath";
            this.lblJsonPath.Size = new System.Drawing.Size(218, 16);
            this.lblJsonPath.TabIndex = 0;
            this.lblJsonPath.Text = "(no file)";
            // 
            // btnJsonNew
            // 
            this.btnJsonNew.Location = new System.Drawing.Point(8, 42);
            this.btnJsonNew.Name = "btnJsonNew";
            this.btnJsonNew.Size = new System.Drawing.Size(60, 26);
            this.btnJsonNew.TabIndex = 1;
            this.btnJsonNew.Text = "New";
            this.btnJsonNew.Click += new System.EventHandler(this.BtnJsonNew_Click);
            // 
            // btnJsonOpen
            // 
            this.btnJsonOpen.Location = new System.Drawing.Point(74, 42);
            this.btnJsonOpen.Name = "btnJsonOpen";
            this.btnJsonOpen.Size = new System.Drawing.Size(60, 26);
            this.btnJsonOpen.TabIndex = 2;
            this.btnJsonOpen.Text = "Open";
            this.btnJsonOpen.Click += new System.EventHandler(this.BtnJsonOpen_Click);
            // 
            // btnJsonSave
            // 
            this.btnJsonSave.Location = new System.Drawing.Point(140, 42);
            this.btnJsonSave.Name = "btnJsonSave";
            this.btnJsonSave.Size = new System.Drawing.Size(60, 26);
            this.btnJsonSave.TabIndex = 3;
            this.btnJsonSave.Text = "Save";
            this.btnJsonSave.Click += new System.EventHandler(this.BtnJsonSave_Click);
            // 
            // pnlRight
            // 
            this.pnlRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlRight.Controls.Add(this.picPreview);
            this.pnlRight.Controls.Add(this.lblStatus);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Location = new System.Drawing.Point(0, 0);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(785, 560);
            this.pnlRight.TabIndex = 0;
            // 
            // picPreview
            // 
            this.picPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPreview.Location = new System.Drawing.Point(0, 0);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(720, 480);
            this.picPreview.TabIndex = 0;
            this.picPreview.TabStop = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(0, 540);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(785, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "No font loaded";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FormMonitorEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(1060, 560);
            this.Controls.Add(this.splitMain);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MinimumSize = new System.Drawing.Size(820, 580);
            this.Name = "FormMonitorEditor";
            this.Text = "Monitor Editor";
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.grpBg.ResumeLayout(false);
            this.grpBg.PerformLayout();
            this.grpColors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudSepWidth)).EndInit();
            this.grpFonts.ResumeLayout(false);
            this.grpFonts.PerformLayout();
            this.grpJson.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.GroupBox grpJson;
        private System.Windows.Forms.Label lblJsonPath;
        private System.Windows.Forms.Button btnJsonNew;
        private System.Windows.Forms.Button btnJsonOpen;
        private System.Windows.Forms.Button btnJsonSave;
        private System.Windows.Forms.GroupBox grpFonts;
        private System.Windows.Forms.Label lblFontTime;
        private System.Windows.Forms.TextBox txtFontTime;
        private System.Windows.Forms.Button btnFontTime;
        private System.Windows.Forms.Label lblFontTimeNote;
        private System.Windows.Forms.Label lblFontSec;
        private System.Windows.Forms.TextBox txtFontSec;
        private System.Windows.Forms.Button btnFontSec;
        private System.Windows.Forms.Label lblFontSecNote;
        private System.Windows.Forms.Label lblFontDate;
        private System.Windows.Forms.TextBox txtFontDate;
        private System.Windows.Forms.Button btnFontDate;
        private System.Windows.Forms.Label lblFontDateNote;
        private System.Windows.Forms.GroupBox grpColors;
        private System.Windows.Forms.Label lblColTime;
        private System.Windows.Forms.Button btnColTime;
        private System.Windows.Forms.Label lblColColon;
        private System.Windows.Forms.Button btnColColon;
        private System.Windows.Forms.Label lblColDate;
        private System.Windows.Forms.Button btnColDate;
        private System.Windows.Forms.Label lblColDay;
        private System.Windows.Forms.Button btnColDay;
        private System.Windows.Forms.Label lblColSec;
        private System.Windows.Forms.Button btnColSec;
        private System.Windows.Forms.Label lblColSep;
        private System.Windows.Forms.Button btnColSep;
        private System.Windows.Forms.Label lblSepWidthLbl;
        private System.Windows.Forms.NumericUpDown nudSepWidth;
        private System.Windows.Forms.GroupBox grpBg;
        private System.Windows.Forms.TextBox txtBgImage;
        private System.Windows.Forms.Button btnBgBrowse;
        private System.Windows.Forms.Button btnBgClear;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.PictureBox picPreview;
        private System.Windows.Forms.Label lblStatus;
    }
}