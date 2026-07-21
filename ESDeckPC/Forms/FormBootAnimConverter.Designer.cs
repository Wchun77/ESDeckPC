namespace ESDeckPC
{
    partial class FormBootAnimConverter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // --------------------------------------------------------------
        // NOTE: written by hand in the classic VS-designer-generated
        // style (every control is a field, InitializeComponent is flat
        // "this.x = new T(); this.x.Prop = value;" statements only --
        // no object initializers, lambdas, LINQ, or helper factory
        // methods) so the Windows Forms designer can actually parse and
        // preview this file. Those C#3+ shortcuts compile and run fine
        // standalone, but the designer's CodeDom-based loader cannot
        // parse them and refuses to open the design surface at all.
        // --------------------------------------------------------------

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBootAnimConverter));
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.grpVideo = new System.Windows.Forms.GroupBox();
            this.videoRow = new System.Windows.Forms.TableLayoutPanel();
            this._txtVideoPath = new System.Windows.Forms.TextBox();
            this._btnBrowseVideo = new System.Windows.Forms.Button();
            this.previewPanel = new System.Windows.Forms.TableLayoutPanel();
            this.videoHost = new System.Windows.Forms.Panel();
            this._wmp = new AxWMPLib.AxWindowsMediaPlayer();
            this._previewImage = new System.Windows.Forms.PictureBox();
            this.previewCtlRow = new System.Windows.Forms.FlowLayoutPanel();
            this._btnPlayPause = new System.Windows.Forms.Button();
            this._btnSetEndHere = new System.Windows.Forms.Button();
            this._lblRange = new System.Windows.Forms.Label();
            this._slider = new ESDeckPC.DualRangeSlider();
            this.grpParams = new System.Windows.Forms.GroupBox();
            this.lblFps = new System.Windows.Forms.Label();
            this._numFps = new System.Windows.Forms.NumericUpDown();
            this.lblQuality = new System.Windows.Forms.Label();
            this._numQuality = new System.Windows.Forms.NumericUpDown();
            this.lblAspect = new System.Windows.Forms.Label();
            this._cmbAspect = new System.Windows.Forms.ComboBox();
            this.lblEstFramesCaption = new System.Windows.Forms.Label();
            this._lblEstFrames = new System.Windows.Forms.Label();
            this.grpOut = new System.Windows.Forms.GroupBox();
            this.outLayout = new System.Windows.Forms.TableLayoutPanel();
            this._txtOutputDir = new System.Windows.Forms.TextBox();
            this._btnBrowseOutput = new System.Windows.Forms.Button();
            this.actionRow = new System.Windows.Forms.TableLayoutPanel();
            this._btnConvert = new System.Windows.Forms.Button();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this.lblLogHeader = new System.Windows.Forms.Label();
            this._txtLog = new System.Windows.Forms.TextBox();
            this.root.SuspendLayout();
            this.grpVideo.SuspendLayout();
            this.videoRow.SuspendLayout();
            this.previewPanel.SuspendLayout();
            this.videoHost.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._wmp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._previewImage)).BeginInit();
            this.previewCtlRow.SuspendLayout();
            this.grpParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numFps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numQuality)).BeginInit();
            this.grpOut.SuspendLayout();
            this.outLayout.SuspendLayout();
            this.actionRow.SuspendLayout();
            this.SuspendLayout();
            // 
            // root
            // 
            this.root.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.grpVideo, 0, 0);
            this.root.Controls.Add(this.previewPanel, 0, 1);
            this.root.Controls.Add(this.grpParams, 0, 2);
            this.root.Controls.Add(this.grpOut, 0, 3);
            this.root.Controls.Add(this.actionRow, 0, 4);
            this.root.Controls.Add(this.lblLogHeader, 0, 5);
            this.root.Controls.Add(this._txtLog, 0, 6);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(0, 0);
            this.root.Name = "root";
            this.root.Padding = new System.Windows.Forms.Padding(10);
            this.root.RowCount = 7;
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 258F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 68F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Size = new System.Drawing.Size(760, 760);
            this.root.TabIndex = 0;
            // 
            // grpVideo
            // 
            this.grpVideo.Controls.Add(this.videoRow);
            this.grpVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVideo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpVideo.Location = new System.Drawing.Point(10, 10);
            this.grpVideo.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpVideo.Name = "grpVideo";
            this.grpVideo.Padding = new System.Windows.Forms.Padding(8);
            this.grpVideo.Size = new System.Drawing.Size(740, 58);
            this.grpVideo.TabIndex = 0;
            this.grpVideo.TabStop = false;
            this.grpVideo.Text = "Input Video";
            // 
            // videoRow
            // 
            this.videoRow.ColumnCount = 2;
            this.videoRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.videoRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.videoRow.Controls.Add(this._txtVideoPath, 0, 0);
            this.videoRow.Controls.Add(this._btnBrowseVideo, 1, 0);
            this.videoRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoRow.Location = new System.Drawing.Point(8, 23);
            this.videoRow.Name = "videoRow";
            this.videoRow.RowCount = 1;
            this.videoRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.videoRow.Size = new System.Drawing.Size(724, 27);
            this.videoRow.TabIndex = 0;
            // 
            // _txtVideoPath
            // 
            this._txtVideoPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._txtVideoPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtVideoPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtVideoPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._txtVideoPath.Location = new System.Drawing.Point(3, 3);
            this._txtVideoPath.Name = "_txtVideoPath";
            this._txtVideoPath.ReadOnly = true;
            this._txtVideoPath.Size = new System.Drawing.Size(622, 22);
            this._txtVideoPath.TabIndex = 0;
            // 
            // _btnBrowseVideo
            // 
            this._btnBrowseVideo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._btnBrowseVideo.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnBrowseVideo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this._btnBrowseVideo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBrowseVideo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._btnBrowseVideo.Location = new System.Drawing.Point(631, 3);
            this._btnBrowseVideo.Name = "_btnBrowseVideo";
            this._btnBrowseVideo.Size = new System.Drawing.Size(90, 24);
            this._btnBrowseVideo.TabIndex = 1;
            this._btnBrowseVideo.Text = "Browse...";
            this._btnBrowseVideo.UseVisualStyleBackColor = false;
            this._btnBrowseVideo.Click += new System.EventHandler(this.BtnBrowseVideo_Click);
            // 
            // previewPanel
            // 
            this.previewPanel.ColumnCount = 1;
            this.previewPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewPanel.Controls.Add(this.videoHost, 0, 0);
            this.previewPanel.Controls.Add(this.previewCtlRow, 0, 1);
            this.previewPanel.Controls.Add(this._slider, 0, 2);
            this.previewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewPanel.Location = new System.Drawing.Point(13, 79);
            this.previewPanel.Name = "previewPanel";
            this.previewPanel.RowCount = 3;
            this.previewPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.previewPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.previewPanel.Size = new System.Drawing.Size(734, 252);
            this.previewPanel.TabIndex = 1;
            // 
            // videoHost
            // 
            this.videoHost.Controls.Add(this._wmp);
            this.videoHost.Controls.Add(this._previewImage);
            this.videoHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoHost.Location = new System.Drawing.Point(3, 3);
            this.videoHost.Name = "videoHost";
            this.videoHost.Size = new System.Drawing.Size(728, 142);
            this.videoHost.TabIndex = 0;
            // 
            // _wmp
            // 
            this._wmp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._wmp.Enabled = true;
            this._wmp.Location = new System.Drawing.Point(0, 0);
            this._wmp.Name = "_wmp";
            this._wmp.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("_wmp.OcxState")));
            this._wmp.Size = new System.Drawing.Size(728, 142);
            this._wmp.TabIndex = 0;
            this._wmp.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(this.Wmp_PlayStateChange);
            // 
            // _previewImage
            // 
            this._previewImage.BackColor = System.Drawing.Color.Black;
            this._previewImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this._previewImage.Location = new System.Drawing.Point(0, 0);
            this._previewImage.Name = "_previewImage";
            this._previewImage.Size = new System.Drawing.Size(728, 142);
            this._previewImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._previewImage.TabIndex = 1;
            this._previewImage.TabStop = false;
            // 
            // previewCtlRow
            // 
            this.previewCtlRow.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.previewCtlRow.Controls.Add(this._btnPlayPause);
            this.previewCtlRow.Controls.Add(this._btnSetEndHere);
            this.previewCtlRow.Controls.Add(this._lblRange);
            this.previewCtlRow.Location = new System.Drawing.Point(3, 151);
            this.previewCtlRow.Name = "previewCtlRow";
            this.previewCtlRow.Size = new System.Drawing.Size(728, 33);
            this.previewCtlRow.TabIndex = 1;
            // 
            // _btnPlayPause
            // 
            this._btnPlayPause.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._btnPlayPause.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnPlayPause.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this._btnPlayPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnPlayPause.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._btnPlayPause.Location = new System.Drawing.Point(3, 3);
            this._btnPlayPause.Name = "_btnPlayPause";
            this._btnPlayPause.Size = new System.Drawing.Size(90, 26);
            this._btnPlayPause.TabIndex = 0;
            this._btnPlayPause.Text = "Play";
            this._btnPlayPause.UseVisualStyleBackColor = false;
            this._btnPlayPause.Click += new System.EventHandler(this.BtnPlayPause_Click);
            // 
            // _btnSetEndHere
            // 
            this._btnSetEndHere.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._btnSetEndHere.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnSetEndHere.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this._btnSetEndHere.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSetEndHere.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._btnSetEndHere.Location = new System.Drawing.Point(99, 3);
            this._btnSetEndHere.Name = "_btnSetEndHere";
            this._btnSetEndHere.Size = new System.Drawing.Size(140, 26);
            this._btnSetEndHere.TabIndex = 1;
            this._btnSetEndHere.Text = "Set End Here (S)";
            this._btnSetEndHere.UseVisualStyleBackColor = false;
            this._btnSetEndHere.Click += new System.EventHandler(this.BtnSetEndHere_Click);
            // 
            // _lblRange
            // 
            this._lblRange.AutoSize = true;
            this._lblRange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._lblRange.Location = new System.Drawing.Point(252, 8);
            this._lblRange.Margin = new System.Windows.Forms.Padding(10, 8, 0, 0);
            this._lblRange.Name = "_lblRange";
            this._lblRange.Size = new System.Drawing.Size(126, 14);
            this._lblRange.TabIndex = 2;
            this._lblRange.Text = "00:00.0 ~ 00:00.0";
            // 
            // _slider
            // 
            this._slider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(24)))), ((int)(((byte)(24)))));
            this._slider.CurrentSec = -1D;
            this._slider.Dock = System.Windows.Forms.DockStyle.Fill;
            this._slider.Location = new System.Drawing.Point(3, 190);
            this._slider.Name = "_slider";
            this._slider.Size = new System.Drawing.Size(728, 59);
            this._slider.TabIndex = 2;
            this._slider.TotalDurationSec = 1D;
            this._slider.RangeChanged += new System.EventHandler(this.Slider_RangeChanged);
            this._slider.SeekRequested += new System.EventHandler<double>(this.Slider_SeekRequested);
            // 
            // grpParams
            // 
            this.grpParams.Controls.Add(this.lblFps);
            this.grpParams.Controls.Add(this._numFps);
            this.grpParams.Controls.Add(this.lblQuality);
            this.grpParams.Controls.Add(this._numQuality);
            this.grpParams.Controls.Add(this.lblAspect);
            this.grpParams.Controls.Add(this._cmbAspect);
            this.grpParams.Controls.Add(this.lblEstFramesCaption);
            this.grpParams.Controls.Add(this._lblEstFrames);
            this.grpParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpParams.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpParams.Location = new System.Drawing.Point(10, 334);
            this.grpParams.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpParams.Name = "grpParams";
            this.grpParams.Padding = new System.Windows.Forms.Padding(8);
            this.grpParams.Size = new System.Drawing.Size(740, 100);
            this.grpParams.TabIndex = 2;
            this.grpParams.TabStop = false;
            this.grpParams.Text = "Conversion Parameters";
            // 
            // lblFps
            // 
            this.lblFps.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFps.AutoSize = true;
            this.lblFps.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblFps.Location = new System.Drawing.Point(29, 34);
            this.lblFps.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(126, 14);
            this.lblFps.TabIndex = 8;
            this.lblFps.Text = "Frame Rate (fps):";
            // 
            // _numFps
            // 
            this._numFps.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._numFps.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._numFps.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._numFps.Location = new System.Drawing.Point(189, 32);
            this._numFps.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this._numFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numFps.Name = "_numFps";
            this._numFps.Size = new System.Drawing.Size(70, 22);
            this._numFps.TabIndex = 9;
            this._numFps.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            // 
            // lblQuality
            // 
            this.lblQuality.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblQuality.AutoSize = true;
            this.lblQuality.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblQuality.Location = new System.Drawing.Point(391, 34);
            this.lblQuality.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblQuality.Name = "lblQuality";
            this.lblQuality.Size = new System.Drawing.Size(245, 14);
            this.lblQuality.TabIndex = 10;
            this.lblQuality.Text = "JPEG Quality (2=best/20=smallest):";
            // 
            // _numQuality
            // 
            this._numQuality.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._numQuality.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._numQuality.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._numQuality.Location = new System.Drawing.Point(642, 32);
            this._numQuality.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this._numQuality.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this._numQuality.Name = "_numQuality";
            this._numQuality.Size = new System.Drawing.Size(70, 22);
            this._numQuality.TabIndex = 11;
            this._numQuality.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblAspect
            // 
            this.lblAspect.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAspect.AutoSize = true;
            this.lblAspect.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblAspect.Location = new System.Drawing.Point(29, 69);
            this.lblAspect.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblAspect.Name = "lblAspect";
            this.lblAspect.Size = new System.Drawing.Size(154, 14);
            this.lblAspect.TabIndex = 12;
            this.lblAspect.Text = "Scale Mode (800x480):";
            // 
            // _cmbAspect
            // 
            this._cmbAspect.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._cmbAspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._cmbAspect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbAspect.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._cmbAspect.Items.AddRange(new object[] {
            "Crop to Fill",
            "Pad (Letterbox)",
            "Stretch to Fill"});
            this._cmbAspect.Location = new System.Drawing.Point(189, 66);
            this._cmbAspect.Name = "_cmbAspect";
            this._cmbAspect.Size = new System.Drawing.Size(175, 22);
            this._cmbAspect.TabIndex = 13;
            // 
            // lblEstFramesCaption
            // 
            this.lblEstFramesCaption.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEstFramesCaption.AutoSize = true;
            this.lblEstFramesCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblEstFramesCaption.Location = new System.Drawing.Point(391, 69);
            this.lblEstFramesCaption.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblEstFramesCaption.Name = "lblEstFramesCaption";
            this.lblEstFramesCaption.Size = new System.Drawing.Size(126, 14);
            this.lblEstFramesCaption.TabIndex = 14;
            this.lblEstFramesCaption.Text = "Estimated Frames:";
            // 
            // _lblEstFrames
            // 
            this._lblEstFrames.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblEstFrames.AutoSize = true;
            this._lblEstFrames.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this._lblEstFrames.Location = new System.Drawing.Point(525, 69);
            this._lblEstFrames.Name = "_lblEstFrames";
            this._lblEstFrames.Size = new System.Drawing.Size(70, 14);
            this._lblEstFrames.TabIndex = 15;
            this._lblEstFrames.Text = "36 frames";
            // 
            // grpOut
            // 
            this.grpOut.Controls.Add(this.outLayout);
            this.grpOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOut.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpOut.Location = new System.Drawing.Point(10, 442);
            this.grpOut.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpOut.Name = "grpOut";
            this.grpOut.Padding = new System.Windows.Forms.Padding(8);
            this.grpOut.Size = new System.Drawing.Size(740, 60);
            this.grpOut.TabIndex = 3;
            this.grpOut.TabStop = false;
            this.grpOut.Text = "Output Folder";
            // 
            // outLayout
            // 
            this.outLayout.ColumnCount = 2;
            this.outLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.outLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.outLayout.Controls.Add(this._txtOutputDir, 0, 0);
            this.outLayout.Controls.Add(this._btnBrowseOutput, 1, 0);
            this.outLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outLayout.Location = new System.Drawing.Point(8, 23);
            this.outLayout.Name = "outLayout";
            this.outLayout.RowCount = 1;
            this.outLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.outLayout.Size = new System.Drawing.Size(724, 29);
            this.outLayout.TabIndex = 0;
            // 
            // _txtOutputDir
            // 
            this._txtOutputDir.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._txtOutputDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._txtOutputDir.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtOutputDir.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._txtOutputDir.Location = new System.Drawing.Point(3, 4);
            this._txtOutputDir.Name = "_txtOutputDir";
            this._txtOutputDir.ReadOnly = true;
            this._txtOutputDir.Size = new System.Drawing.Size(578, 22);
            this._txtOutputDir.TabIndex = 0;
            // 
            // _btnBrowseOutput
            // 
            this._btnBrowseOutput.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._btnBrowseOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._btnBrowseOutput.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnBrowseOutput.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this._btnBrowseOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnBrowseOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._btnBrowseOutput.Location = new System.Drawing.Point(587, 3);
            this._btnBrowseOutput.Name = "_btnBrowseOutput";
            this._btnBrowseOutput.Size = new System.Drawing.Size(134, 24);
            this._btnBrowseOutput.TabIndex = 1;
            this._btnBrowseOutput.Text = "Browse...";
            this._btnBrowseOutput.UseVisualStyleBackColor = false;
            this._btnBrowseOutput.Click += new System.EventHandler(this.BtnBrowseOutput_Click);
            // 
            // actionRow
            // 
            this.actionRow.ColumnCount = 2;
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionRow.Controls.Add(this._btnConvert, 0, 0);
            this.actionRow.Controls.Add(this._progressBar, 1, 0);
            this.actionRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionRow.Location = new System.Drawing.Point(13, 513);
            this.actionRow.Name = "actionRow";
            this.actionRow.RowCount = 1;
            this.actionRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.actionRow.Size = new System.Drawing.Size(734, 32);
            this.actionRow.TabIndex = 4;
            // 
            // _btnConvert
            // 
            this._btnConvert.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._btnConvert.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._btnConvert.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._btnConvert.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this._btnConvert.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConvert.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this._btnConvert.Location = new System.Drawing.Point(3, 3);
            this._btnConvert.Name = "_btnConvert";
            this._btnConvert.Size = new System.Drawing.Size(134, 25);
            this._btnConvert.TabIndex = 0;
            this._btnConvert.Text = "Start Conversion";
            this._btnConvert.UseVisualStyleBackColor = false;
            this._btnConvert.Click += new System.EventHandler(this.BtnConvert_Click);
            // 
            // _progressBar
            // 
            this._progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._progressBar.Location = new System.Drawing.Point(148, 4);
            this._progressBar.Margin = new System.Windows.Forms.Padding(8, 4, 0, 4);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(586, 24);
            this._progressBar.TabIndex = 1;
            // 
            // lblLogHeader
            // 
            this.lblLogHeader.AutoSize = true;
            this.lblLogHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblLogHeader.Location = new System.Drawing.Point(10, 554);
            this.lblLogHeader.Margin = new System.Windows.Forms.Padding(0, 6, 0, 2);
            this.lblLogHeader.Name = "lblLogHeader";
            this.lblLogHeader.Size = new System.Drawing.Size(28, 14);
            this.lblLogHeader.TabIndex = 5;
            this.lblLogHeader.Text = "Log";
            // 
            // _txtLog
            // 
            this._txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this._txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtLog.Font = new System.Drawing.Font("Consolas", 8.5F);
            this._txtLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this._txtLog.Location = new System.Drawing.Point(13, 573);
            this._txtLog.Multiline = true;
            this._txtLog.Name = "_txtLog";
            this._txtLog.ReadOnly = true;
            this._txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtLog.Size = new System.Drawing.Size(734, 174);
            this._txtLog.TabIndex = 6;
            // 
            // FormBootAnimConverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(760, 760);
            this.Controls.Add(this.root);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormBootAnimConverter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Boot Animation Converter";
            this.root.ResumeLayout(false);
            this.root.PerformLayout();
            this.grpVideo.ResumeLayout(false);
            this.videoRow.ResumeLayout(false);
            this.videoRow.PerformLayout();
            this.previewPanel.ResumeLayout(false);
            this.videoHost.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._wmp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._previewImage)).EndInit();
            this.previewCtlRow.ResumeLayout(false);
            this.previewCtlRow.PerformLayout();
            this.grpParams.ResumeLayout(false);
            this.grpParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numFps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numQuality)).EndInit();
            this.grpOut.ResumeLayout(false);
            this.outLayout.ResumeLayout(false);
            this.outLayout.PerformLayout();
            this.actionRow.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        // --------------------------------------------------------------
        // Fields
        // --------------------------------------------------------------

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.GroupBox grpVideo;
        private System.Windows.Forms.TableLayoutPanel videoRow;
        private System.Windows.Forms.TextBox _txtVideoPath;
        private System.Windows.Forms.Button _btnBrowseVideo;
        private System.Windows.Forms.TableLayoutPanel previewPanel;
        private System.Windows.Forms.Panel videoHost;
        private AxWMPLib.AxWindowsMediaPlayer _wmp;
        private System.Windows.Forms.PictureBox _previewImage;
        private System.Windows.Forms.FlowLayoutPanel previewCtlRow;
        private System.Windows.Forms.Button _btnPlayPause;
        private System.Windows.Forms.Button _btnSetEndHere;
        private System.Windows.Forms.Label _lblRange;
        private ESDeckPC.DualRangeSlider _slider;
        private System.Windows.Forms.GroupBox grpParams;
        private System.Windows.Forms.GroupBox grpOut;
        private System.Windows.Forms.TableLayoutPanel outLayout;
        private System.Windows.Forms.TextBox _txtOutputDir;
        private System.Windows.Forms.Button _btnBrowseOutput;
        private System.Windows.Forms.TableLayoutPanel actionRow;
        private System.Windows.Forms.Button _btnConvert;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Label lblLogHeader;
        private System.Windows.Forms.TextBox _txtLog;
        private System.Windows.Forms.Label lblFps;
        private System.Windows.Forms.NumericUpDown _numFps;
        private System.Windows.Forms.Label lblQuality;
        private System.Windows.Forms.NumericUpDown _numQuality;
        private System.Windows.Forms.Label lblAspect;
        private System.Windows.Forms.ComboBox _cmbAspect;
        private System.Windows.Forms.Label lblEstFramesCaption;
        private System.Windows.Forms.Label _lblEstFrames;
    }
}
