namespace ESDeckPC
{
    partial class UC_ClockSettings
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.grpClockBg = new System.Windows.Forms.GroupBox();
            this.txtClockBgImage = new System.Windows.Forms.TextBox();
            this.btnClockBgBrowse = new System.Windows.Forms.Button();
            this.btnClockBgClear = new System.Windows.Forms.Button();
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
            this.grpOpacity = new System.Windows.Forms.GroupBox();
            this.lblOpaTime = new System.Windows.Forms.Label();
            this.lblOpaColon = new System.Windows.Forms.Label();
            this.lblOpaDate = new System.Windows.Forms.Label();
            this.lblOpaDay = new System.Windows.Forms.Label();
            this.lblOpaSec = new System.Windows.Forms.Label();
            this.nudOpaTime = new System.Windows.Forms.NumericUpDown();
            this.nudOpaColon = new System.Windows.Forms.NumericUpDown();
            this.nudOpaDate = new System.Windows.Forms.NumericUpDown();
            this.nudOpaDay = new System.Windows.Forms.NumericUpDown();
            this.nudOpaSec = new System.Windows.Forms.NumericUpDown();
            this.lblColonGap = new System.Windows.Forms.Label();
            this.nudColonGap = new System.Windows.Forms.NumericUpDown();
            this.grpClockBg.SuspendLayout();
            this.grpColors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSepWidth)).BeginInit();
            this.grpFonts.SuspendLayout();
            this.grpOpacity.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaColon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaDate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaDay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudColonGap)).BeginInit();
            this.SuspendLayout();
            // 
            // grpClockBg
            // 
            this.grpClockBg.Controls.Add(this.txtClockBgImage);
            this.grpClockBg.Controls.Add(this.btnClockBgBrowse);
            this.grpClockBg.Controls.Add(this.btnClockBgClear);
            this.grpClockBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpClockBg.Location = new System.Drawing.Point(14, 644);
            this.grpClockBg.Name = "grpClockBg";
            this.grpClockBg.Size = new System.Drawing.Size(234, 86);
            this.grpClockBg.TabIndex = 9;
            this.grpClockBg.TabStop = false;
            this.grpClockBg.Text = "Background image";
            // 
            // txtClockBgImage
            // 
            this.txtClockBgImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtClockBgImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtClockBgImage.Location = new System.Drawing.Point(8, 20);
            this.txtClockBgImage.Name = "txtClockBgImage";
            this.txtClockBgImage.ReadOnly = true;
            this.txtClockBgImage.Size = new System.Drawing.Size(218, 22);
            this.txtClockBgImage.TabIndex = 0;
            // 
            // btnClockBgBrowse
            // 
            this.btnClockBgBrowse.Location = new System.Drawing.Point(8, 46);
            this.btnClockBgBrowse.Name = "btnClockBgBrowse";
            this.btnClockBgBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnClockBgBrowse.TabIndex = 1;
            this.btnClockBgBrowse.Text = "Browse...";
            // 
            // btnClockBgClear
            // 
            this.btnClockBgClear.Location = new System.Drawing.Point(96, 46);
            this.btnClockBgClear.Name = "btnClockBgClear";
            this.btnClockBgClear.Size = new System.Drawing.Size(60, 24);
            this.btnClockBgClear.TabIndex = 2;
            this.btnClockBgClear.Text = "Clear";
            // 
            // grpColors
            // 
            this.grpColors.Controls.Add(this.lblColonGap);
            this.grpColors.Controls.Add(this.nudColonGap);
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
            this.grpColors.Location = new System.Drawing.Point(14, 210);
            this.grpColors.Name = "grpColors";
            this.grpColors.Size = new System.Drawing.Size(234, 254);
            this.grpColors.TabIndex = 10;
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
            // 
            // lblColColon
            // 
            this.lblColColon.Location = new System.Drawing.Point(8, 79);
            this.lblColColon.Name = "lblColColon";
            this.lblColColon.Size = new System.Drawing.Size(90, 18);
            this.lblColColon.TabIndex = 2;
            this.lblColColon.Text = "Colon";
            // 
            // btnColColon
            // 
            this.btnColColon.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColColon.Location = new System.Drawing.Point(104, 75);
            this.btnColColon.Name = "btnColColon";
            this.btnColColon.Size = new System.Drawing.Size(120, 22);
            this.btnColColon.TabIndex = 3;
            // 
            // lblColDate
            // 
            this.lblColDate.Location = new System.Drawing.Point(8, 107);
            this.lblColDate.Name = "lblColDate";
            this.lblColDate.Size = new System.Drawing.Size(90, 18);
            this.lblColDate.TabIndex = 4;
            this.lblColDate.Text = "Date";
            // 
            // btnColDate
            // 
            this.btnColDate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColDate.Location = new System.Drawing.Point(104, 103);
            this.btnColDate.Name = "btnColDate";
            this.btnColDate.Size = new System.Drawing.Size(120, 22);
            this.btnColDate.TabIndex = 5;
            // 
            // lblColDay
            // 
            this.lblColDay.Location = new System.Drawing.Point(8, 135);
            this.lblColDay.Name = "lblColDay";
            this.lblColDay.Size = new System.Drawing.Size(90, 18);
            this.lblColDay.TabIndex = 6;
            this.lblColDay.Text = "Day";
            // 
            // btnColDay
            // 
            this.btnColDay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColDay.Location = new System.Drawing.Point(104, 131);
            this.btnColDay.Name = "btnColDay";
            this.btnColDay.Size = new System.Drawing.Size(120, 22);
            this.btnColDay.TabIndex = 7;
            // 
            // lblColSec
            // 
            this.lblColSec.Location = new System.Drawing.Point(8, 163);
            this.lblColSec.Name = "lblColSec";
            this.lblColSec.Size = new System.Drawing.Size(90, 18);
            this.lblColSec.TabIndex = 8;
            this.lblColSec.Text = "Sec";
            // 
            // btnColSec
            // 
            this.btnColSec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColSec.Location = new System.Drawing.Point(104, 159);
            this.btnColSec.Name = "btnColSec";
            this.btnColSec.Size = new System.Drawing.Size(120, 22);
            this.btnColSec.TabIndex = 9;
            // 
            // lblColSep
            // 
            this.lblColSep.Location = new System.Drawing.Point(8, 191);
            this.lblColSep.Name = "lblColSep";
            this.lblColSep.Size = new System.Drawing.Size(90, 18);
            this.lblColSep.TabIndex = 10;
            this.lblColSep.Text = "Separator";
            // 
            // btnColSep
            // 
            this.btnColSep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnColSep.Location = new System.Drawing.Point(104, 187);
            this.btnColSep.Name = "btnColSep";
            this.btnColSep.Size = new System.Drawing.Size(120, 22);
            this.btnColSep.TabIndex = 11;
            // 
            // lblSepWidthLbl
            // 
            this.lblSepWidthLbl.Location = new System.Drawing.Point(8, 219);
            this.lblSepWidthLbl.Name = "lblSepWidthLbl";
            this.lblSepWidthLbl.Size = new System.Drawing.Size(90, 18);
            this.lblSepWidthLbl.TabIndex = 12;
            this.lblSepWidthLbl.Text = "Sep width";
            // 
            // nudSepWidth
            // 
            this.nudSepWidth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudSepWidth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudSepWidth.Location = new System.Drawing.Point(104, 215);
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
            this.grpFonts.Location = new System.Drawing.Point(14, 11);
            this.grpFonts.Name = "grpFonts";
            this.grpFonts.Size = new System.Drawing.Size(234, 197);
            this.grpFonts.TabIndex = 11;
            this.grpFonts.TabStop = false;
            this.grpFonts.Text = "Fonts (bin files)";
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
            // grpOpacity
            // 
            this.grpOpacity.Controls.Add(this.nudOpaSec);
            this.grpOpacity.Controls.Add(this.nudOpaDay);
            this.grpOpacity.Controls.Add(this.nudOpaDate);
            this.grpOpacity.Controls.Add(this.nudOpaColon);
            this.grpOpacity.Controls.Add(this.nudOpaTime);
            this.grpOpacity.Controls.Add(this.lblOpaTime);
            this.grpOpacity.Controls.Add(this.lblOpaColon);
            this.grpOpacity.Controls.Add(this.lblOpaDate);
            this.grpOpacity.Controls.Add(this.lblOpaDay);
            this.grpOpacity.Controls.Add(this.lblOpaSec);
            this.grpOpacity.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpOpacity.Location = new System.Drawing.Point(14, 470);
            this.grpOpacity.Name = "grpOpacity";
            this.grpOpacity.Size = new System.Drawing.Size(234, 168);
            this.grpOpacity.TabIndex = 12;
            this.grpOpacity.TabStop = false;
            this.grpOpacity.Text = "Opacity";
            // 
            // lblOpaTime
            // 
            this.lblOpaTime.Location = new System.Drawing.Point(8, 24);
            this.lblOpaTime.Name = "lblOpaTime";
            this.lblOpaTime.Size = new System.Drawing.Size(90, 18);
            this.lblOpaTime.TabIndex = 0;
            this.lblOpaTime.Text = "Time";
            // 
            // lblOpaColon
            // 
            this.lblOpaColon.Location = new System.Drawing.Point(8, 52);
            this.lblOpaColon.Name = "lblOpaColon";
            this.lblOpaColon.Size = new System.Drawing.Size(90, 18);
            this.lblOpaColon.TabIndex = 2;
            this.lblOpaColon.Text = "Colon";
            // 
            // lblOpaDate
            // 
            this.lblOpaDate.Location = new System.Drawing.Point(8, 80);
            this.lblOpaDate.Name = "lblOpaDate";
            this.lblOpaDate.Size = new System.Drawing.Size(90, 18);
            this.lblOpaDate.TabIndex = 4;
            this.lblOpaDate.Text = "Date";
            // 
            // lblOpaDay
            // 
            this.lblOpaDay.Location = new System.Drawing.Point(8, 108);
            this.lblOpaDay.Name = "lblOpaDay";
            this.lblOpaDay.Size = new System.Drawing.Size(90, 18);
            this.lblOpaDay.TabIndex = 6;
            this.lblOpaDay.Text = "Day";
            // 
            // lblOpaSec
            // 
            this.lblOpaSec.Location = new System.Drawing.Point(8, 136);
            this.lblOpaSec.Name = "lblOpaSec";
            this.lblOpaSec.Size = new System.Drawing.Size(90, 18);
            this.lblOpaSec.TabIndex = 8;
            this.lblOpaSec.Text = "Sec";
            // 
            // nudOpaTime
            // 
            this.nudOpaTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudOpaTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudOpaTime.Location = new System.Drawing.Point(104, 20);
            this.nudOpaTime.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudOpaTime.Name = "nudOpaTime";
            this.nudOpaTime.Size = new System.Drawing.Size(56, 22);
            this.nudOpaTime.TabIndex = 14;
            this.nudOpaTime.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudOpaColon
            // 
            this.nudOpaColon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudOpaColon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudOpaColon.Location = new System.Drawing.Point(104, 50);
            this.nudOpaColon.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudOpaColon.Name = "nudOpaColon";
            this.nudOpaColon.Size = new System.Drawing.Size(56, 22);
            this.nudOpaColon.TabIndex = 15;
            this.nudOpaColon.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudOpaDate
            // 
            this.nudOpaDate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudOpaDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudOpaDate.Location = new System.Drawing.Point(104, 78);
            this.nudOpaDate.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudOpaDate.Name = "nudOpaDate";
            this.nudOpaDate.Size = new System.Drawing.Size(56, 22);
            this.nudOpaDate.TabIndex = 16;
            this.nudOpaDate.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudOpaDay
            // 
            this.nudOpaDay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudOpaDay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudOpaDay.Location = new System.Drawing.Point(104, 106);
            this.nudOpaDay.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudOpaDay.Name = "nudOpaDay";
            this.nudOpaDay.Size = new System.Drawing.Size(56, 22);
            this.nudOpaDay.TabIndex = 17;
            this.nudOpaDay.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudOpaSec
            // 
            this.nudOpaSec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudOpaSec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudOpaSec.Location = new System.Drawing.Point(104, 134);
            this.nudOpaSec.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.nudOpaSec.Name = "nudOpaSec";
            this.nudOpaSec.Size = new System.Drawing.Size(56, 22);
            this.nudOpaSec.TabIndex = 18;
            this.nudOpaSec.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblColonGap
            // 
            this.lblColonGap.Location = new System.Drawing.Point(8, 52);
            this.lblColonGap.Name = "lblColonGap";
            this.lblColonGap.Size = new System.Drawing.Size(90, 18);
            this.lblColonGap.TabIndex = 14;
            this.lblColonGap.Text = "Colon gap";
            // 
            // nudColonGap
            // 
            this.nudColonGap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudColonGap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudColonGap.Location = new System.Drawing.Point(104, 48);
            this.nudColonGap.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudColonGap.Name = "nudColonGap";
            this.nudColonGap.Size = new System.Drawing.Size(56, 22);
            this.nudColonGap.TabIndex = 15;
            this.nudColonGap.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // UC_ClockSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.Controls.Add(this.grpOpacity);
            this.Controls.Add(this.grpClockBg);
            this.Controls.Add(this.grpColors);
            this.Controls.Add(this.grpFonts);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_ClockSettings";
            this.Size = new System.Drawing.Size(265, 742);
            this.grpClockBg.ResumeLayout(false);
            this.grpClockBg.PerformLayout();
            this.grpColors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudSepWidth)).EndInit();
            this.grpFonts.ResumeLayout(false);
            this.grpFonts.PerformLayout();
            this.grpOpacity.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaColon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaDate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaDay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpaSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudColonGap)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpClockBg;
        private System.Windows.Forms.TextBox txtClockBgImage;
        private System.Windows.Forms.Button btnClockBgBrowse;
        private System.Windows.Forms.Button btnClockBgClear;
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
        private System.Windows.Forms.GroupBox grpOpacity;
        private System.Windows.Forms.Label lblOpaTime;
        private System.Windows.Forms.Label lblOpaColon;
        private System.Windows.Forms.Label lblOpaDate;
        private System.Windows.Forms.Label lblOpaDay;
        private System.Windows.Forms.Label lblOpaSec;
        private System.Windows.Forms.NumericUpDown nudOpaSec;
        private System.Windows.Forms.NumericUpDown nudOpaDay;
        private System.Windows.Forms.NumericUpDown nudOpaDate;
        private System.Windows.Forms.NumericUpDown nudOpaColon;
        private System.Windows.Forms.NumericUpDown nudOpaTime;
        private System.Windows.Forms.Label lblColonGap;
        private System.Windows.Forms.NumericUpDown nudColonGap;
    }
}
