namespace ESDeckPC
{
    partial class FormCanvasEditor
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.flowTop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblConfig = new System.Windows.Forms.Label();
            this.cboConfig = new System.Windows.Forms.ComboBox();
            this.btnNewConfig = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnUse = new System.Windows.Forms.Button();
            this.lblActive = new System.Windows.Forms.Label();
            this.tableTop = new System.Windows.Forms.TableLayoutPanel();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.lstLibrary = new System.Windows.Forms.ListBox();
            this.panelSearch = new System.Windows.Forms.Panel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblLibrary = new System.Windows.Forms.Label();
            this.flowMid = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.panelRight = new System.Windows.Forms.Panel();
            this.tableDetails = new System.Windows.Forms.TableLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblIcon = new System.Windows.Forms.Label();
            this.txtIcon = new System.Windows.Forms.TextBox();
            this.btnBrowseIcon = new System.Windows.Forms.Button();
            this.picIcon = new System.Windows.Forms.PictureBox();
            this.lstPages = new System.Windows.Forms.ListBox();
            this.lblPages = new System.Windows.Forms.Label();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.panelPrevArea = new System.Windows.Forms.Panel();
            this.picSide = new System.Windows.Forms.PictureBox();
            this.panelPreviewBox = new System.Windows.Forms.Panel();
            this.picSettings = new System.Windows.Forms.PictureBox();
            this.flowPrevHead = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPageView = new System.Windows.Forms.Button();
            this.btnSetView = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.lblPreview = new System.Windows.Forms.Label();
            this.flowLook = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSetBg = new System.Windows.Forms.Label();
            this.txtSetBg = new System.Windows.Forms.TextBox();
            this.btnSetBg = new System.Windows.Forms.Button();
            this.lblSetIcon = new System.Windows.Forms.Label();
            this.txtSetIcon = new System.Windows.Forms.TextBox();
            this.btnSetIcon = new System.Windows.Forms.Button();
            this.flowBottom = new System.Windows.Forms.FlowLayoutPanel();
            this.btnNewPage = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnRefreshLib = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.previewTimer = new System.Windows.Forms.Timer(this.components);
            this.flowTop.SuspendLayout();
            this.tableTop.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.panelSearch.SuspendLayout();
            this.flowMid.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.tableDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.panelPreview.SuspendLayout();
            this.panelPrevArea.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSide)).BeginInit();
            this.panelPreviewBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSettings)).BeginInit();
            this.flowPrevHead.SuspendLayout();
            this.flowLook.SuspendLayout();
            this.flowBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowTop
            // 
            this.flowTop.Controls.Add(this.lblConfig);
            this.flowTop.Controls.Add(this.cboConfig);
            this.flowTop.Controls.Add(this.btnNewConfig);
            this.flowTop.Controls.Add(this.btnSave);
            this.flowTop.Controls.Add(this.btnUse);
            this.flowTop.Controls.Add(this.lblActive);
            this.flowTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowTop.Location = new System.Drawing.Point(0, 0);
            this.flowTop.Name = "flowTop";
            this.flowTop.Padding = new System.Windows.Forms.Padding(10, 8, 10, 0);
            this.flowTop.Size = new System.Drawing.Size(864, 44);
            this.flowTop.TabIndex = 0;
            this.flowTop.WrapContents = false;
            // 
            // lblConfig
            // 
            this.lblConfig.AutoSize = true;
            this.lblConfig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblConfig.Location = new System.Drawing.Point(10, 14);
            this.lblConfig.Margin = new System.Windows.Forms.Padding(0, 6, 4, 0);
            this.lblConfig.Name = "lblConfig";
            this.lblConfig.Size = new System.Drawing.Size(46, 15);
            this.lblConfig.TabIndex = 1;
            this.lblConfig.Text = "Config:";
            // 
            // cboConfig
            // 
            this.cboConfig.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.cboConfig.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboConfig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cboConfig.FormattingEnabled = true;
            this.cboConfig.Location = new System.Drawing.Point(63, 11);
            this.cboConfig.Name = "cboConfig";
            this.cboConfig.Size = new System.Drawing.Size(200, 23);
            this.cboConfig.TabIndex = 2;
            this.cboConfig.SelectedIndexChanged += new System.EventHandler(this.cboConfig_SelectedIndexChanged);
            // 
            // btnNewConfig
            // 
            this.btnNewConfig.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnNewConfig.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnNewConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewConfig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnNewConfig.Location = new System.Drawing.Point(269, 11);
            this.btnNewConfig.Name = "btnNewConfig";
            this.btnNewConfig.Size = new System.Drawing.Size(70, 28);
            this.btnNewConfig.TabIndex = 3;
            this.btnNewConfig.Text = "New...";
            this.btnNewConfig.UseVisualStyleBackColor = false;
            this.btnNewConfig.Click += new System.EventHandler(this.btnNewConfig_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSave.Location = new System.Drawing.Point(345, 11);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(70, 28);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnUse
            // 
            this.btnUse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnUse.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnUse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUse.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnUse.Location = new System.Drawing.Point(421, 11);
            this.btnUse.Name = "btnUse";
            this.btnUse.Size = new System.Drawing.Size(120, 28);
            this.btnUse.TabIndex = 5;
            this.btnUse.Text = "Use This Config";
            this.btnUse.UseVisualStyleBackColor = false;
            this.btnUse.Click += new System.EventHandler(this.btnUse_Click);
            // 
            // lblActive
            // 
            this.lblActive.AutoSize = true;
            this.lblActive.ForeColor = System.Drawing.Color.Gray;
            this.lblActive.Location = new System.Drawing.Point(554, 15);
            this.lblActive.Margin = new System.Windows.Forms.Padding(10, 7, 0, 0);
            this.lblActive.Name = "lblActive";
            this.lblActive.Size = new System.Drawing.Size(79, 15);
            this.lblActive.TabIndex = 6;
            this.lblActive.Text = "In use: (none)";
            // 
            // tableTop
            // 
            this.tableTop.ColumnCount = 3;
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tableTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableTop.Controls.Add(this.panelLeft, 0, 0);
            this.tableTop.Controls.Add(this.flowMid, 1, 0);
            this.tableTop.Controls.Add(this.panelRight, 2, 0);
            this.tableTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableTop.Location = new System.Drawing.Point(0, 44);
            this.tableTop.Name = "tableTop";
            this.tableTop.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.tableTop.RowCount = 1;
            this.tableTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableTop.Size = new System.Drawing.Size(864, 220);
            this.tableTop.TabIndex = 7;
            // 
            // panelLeft
            // 
            this.panelLeft.Controls.Add(this.lstLibrary);
            this.panelLeft.Controls.Add(this.panelSearch);
            this.panelLeft.Controls.Add(this.lblLibrary);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Location = new System.Drawing.Point(13, 7);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(324, 206);
            this.panelLeft.TabIndex = 8;
            // 
            // lstLibrary
            // 
            this.lstLibrary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lstLibrary.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstLibrary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lstLibrary.FormattingEnabled = true;
            this.lstLibrary.IntegralHeight = false;
            this.lstLibrary.ItemHeight = 15;
            this.lstLibrary.Location = new System.Drawing.Point(0, 45);
            this.lstLibrary.Name = "lstLibrary";
            this.lstLibrary.Size = new System.Drawing.Size(324, 161);
            this.lstLibrary.TabIndex = 9;
            this.lstLibrary.SelectedIndexChanged += new System.EventHandler(this.lstLibrary_SelectedIndexChanged);
            this.lstLibrary.DoubleClick += new System.EventHandler(this.lstLibrary_DoubleClick);
            // 
            // panelSearch
            // 
            this.panelSearch.Controls.Add(this.txtSearch);
            this.panelSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSearch.Location = new System.Drawing.Point(0, 15);
            this.panelSearch.Name = "panelSearch";
            this.panelSearch.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.panelSearch.Size = new System.Drawing.Size(324, 30);
            this.panelSearch.TabIndex = 10;
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtSearch.Location = new System.Drawing.Point(0, 2);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(324, 23);
            this.txtSearch.TabIndex = 11;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            // 
            // lblLibrary
            // 
            this.lblLibrary.AutoSize = true;
            this.lblLibrary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLibrary.ForeColor = System.Drawing.Color.Silver;
            this.lblLibrary.Location = new System.Drawing.Point(0, 0);
            this.lblLibrary.Name = "lblLibrary";
            this.lblLibrary.Size = new System.Drawing.Size(154, 15);
            this.lblLibrary.TabIndex = 12;
            this.lblLibrary.Text = "Page library (Canvas/pages)";
            // 
            // flowMid
            // 
            this.flowMid.Controls.Add(this.btnAdd);
            this.flowMid.Controls.Add(this.btnRemove);
            this.flowMid.Controls.Add(this.btnUp);
            this.flowMid.Controls.Add(this.btnDown);
            this.flowMid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMid.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMid.Location = new System.Drawing.Point(343, 7);
            this.flowMid.Name = "flowMid";
            this.flowMid.Padding = new System.Windows.Forms.Padding(8, 24, 0, 0);
            this.flowMid.Size = new System.Drawing.Size(104, 206);
            this.flowMid.TabIndex = 13;
            this.flowMid.WrapContents = false;
            // 
            // btnAdd
            // 
            this.btnAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnAdd.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnAdd.Location = new System.Drawing.Point(11, 27);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(90, 28);
            this.btnAdd.TabIndex = 14;
            this.btnAdd.Text = "Add >";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnRemove.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnRemove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemove.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnRemove.Location = new System.Drawing.Point(11, 61);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(90, 28);
            this.btnRemove.TabIndex = 15;
            this.btnRemove.Text = "< Remove";
            this.btnRemove.UseVisualStyleBackColor = false;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnUp
            // 
            this.btnUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnUp.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnUp.Location = new System.Drawing.Point(11, 95);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(90, 28);
            this.btnUp.TabIndex = 16;
            this.btnUp.Text = "Up";
            this.btnUp.UseVisualStyleBackColor = false;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnDown.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnDown.Location = new System.Drawing.Point(11, 129);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(90, 28);
            this.btnDown.TabIndex = 17;
            this.btnDown.Text = "Down";
            this.btnDown.UseVisualStyleBackColor = false;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // panelRight
            // 
            this.panelRight.Controls.Add(this.tableDetails);
            this.panelRight.Controls.Add(this.lstPages);
            this.panelRight.Controls.Add(this.lblPages);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(453, 7);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(398, 206);
            this.panelRight.TabIndex = 18;
            // 
            // tableDetails
            // 
            this.tableDetails.ColumnCount = 4;
            this.tableDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tableDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tableDetails.Controls.Add(this.lblName, 0, 0);
            this.tableDetails.Controls.Add(this.txtName, 1, 0);
            this.tableDetails.Controls.Add(this.lblIcon, 0, 1);
            this.tableDetails.Controls.Add(this.txtIcon, 1, 1);
            this.tableDetails.Controls.Add(this.btnBrowseIcon, 2, 1);
            this.tableDetails.Controls.Add(this.picIcon, 3, 0);
            this.tableDetails.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableDetails.Location = new System.Drawing.Point(0, 83);
            this.tableDetails.Name = "tableDetails";
            this.tableDetails.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.tableDetails.RowCount = 2;
            this.tableDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableDetails.Size = new System.Drawing.Size(398, 76);
            this.tableDetails.TabIndex = 19;
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblName.Location = new System.Drawing.Point(0, 12);
            this.lblName.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(39, 15);
            this.lblName.TabIndex = 20;
            this.lblName.Text = "Name";
            // 
            // txtName
            // 
            this.txtName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtName.Location = new System.Drawing.Point(47, 9);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(196, 23);
            this.txtName.TabIndex = 21;
            this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
            this.txtName.Leave += new System.EventHandler(this.txtName_Leave);
            // 
            // lblIcon
            // 
            this.lblIcon.AutoSize = true;
            this.lblIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblIcon.Location = new System.Drawing.Point(0, 47);
            this.lblIcon.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblIcon.Name = "lblIcon";
            this.lblIcon.Size = new System.Drawing.Size(30, 15);
            this.lblIcon.TabIndex = 22;
            this.lblIcon.Text = "Icon";
            // 
            // txtIcon
            // 
            this.txtIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtIcon.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtIcon.Location = new System.Drawing.Point(47, 44);
            this.txtIcon.Name = "txtIcon";
            this.txtIcon.Size = new System.Drawing.Size(196, 23);
            this.txtIcon.TabIndex = 23;
            this.txtIcon.TextChanged += new System.EventHandler(this.txtIcon_TextChanged);
            // 
            // btnBrowseIcon
            // 
            this.btnBrowseIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnBrowseIcon.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnBrowseIcon.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBrowseIcon.Location = new System.Drawing.Point(249, 44);
            this.btnBrowseIcon.Name = "btnBrowseIcon";
            this.btnBrowseIcon.Size = new System.Drawing.Size(74, 28);
            this.btnBrowseIcon.TabIndex = 24;
            this.btnBrowseIcon.Text = "Browse...";
            this.btnBrowseIcon.UseVisualStyleBackColor = false;
            this.btnBrowseIcon.Click += new System.EventHandler(this.btnBrowseIcon_Click);
            // 
            // picIcon
            // 
            this.picIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(42)))), ((int)(((byte)(42)))));
            this.picIcon.Location = new System.Drawing.Point(332, 6);
            this.picIcon.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.picIcon.Name = "picIcon";
            this.tableDetails.SetRowSpan(this.picIcon, 2);
            this.picIcon.Size = new System.Drawing.Size(64, 64);
            this.picIcon.TabIndex = 25;
            this.picIcon.TabStop = false;
            // 
            // lstPages
            // 
            this.lstPages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lstPages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstPages.Dock = System.Windows.Forms.DockStyle.Top;
            this.lstPages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lstPages.FormattingEnabled = true;
            this.lstPages.IntegralHeight = false;
            this.lstPages.ItemHeight = 15;
            this.lstPages.Location = new System.Drawing.Point(0, 15);
            this.lstPages.Name = "lstPages";
            this.lstPages.Size = new System.Drawing.Size(398, 68);
            this.lstPages.TabIndex = 25;
            this.lstPages.SelectedIndexChanged += new System.EventHandler(this.lstPages_SelectedIndexChanged);
            // 
            // lblPages
            // 
            this.lblPages.AutoSize = true;
            this.lblPages.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPages.ForeColor = System.Drawing.Color.Silver;
            this.lblPages.Location = new System.Drawing.Point(0, 0);
            this.lblPages.Name = "lblPages";
            this.lblPages.Size = new System.Drawing.Size(169, 15);
            this.lblPages.TabIndex = 26;
            this.lblPages.Text = "Sidebar pages (max 4, top first)";
            // 
            // panelPreview
            // 
            this.panelPreview.Controls.Add(this.panelPrevArea);
            this.panelPreview.Controls.Add(this.flowPrevHead);
            this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPreview.Location = new System.Drawing.Point(0, 264);
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Padding = new System.Windows.Forms.Padding(10, 0, 10, 4);
            this.panelPreview.Size = new System.Drawing.Size(864, 255);
            this.panelPreview.TabIndex = 27;
            // 
            // panelPrevArea
            // 
            this.panelPrevArea.Controls.Add(this.picSide);
            this.panelPrevArea.Controls.Add(this.panelPreviewBox);
            this.panelPrevArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPrevArea.Location = new System.Drawing.Point(10, 52);
            this.panelPrevArea.Name = "panelPrevArea";
            this.panelPrevArea.Size = new System.Drawing.Size(844, 199);
            this.panelPrevArea.TabIndex = 28;
            // 
            // picSide
            // 
            this.picSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.picSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picSide.Location = new System.Drawing.Point(0, 0);
            this.picSide.Name = "picSide";
            this.picSide.Size = new System.Drawing.Size(80, 480);
            this.picSide.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSide.TabIndex = 0;
            this.picSide.TabStop = false;
            this.picSide.MouseClick += new System.Windows.Forms.MouseEventHandler(this.picSide_MouseClick);
            // 
            // panelPreviewBox
            // 
            this.panelPreviewBox.BackColor = System.Drawing.Color.Black;
            this.panelPreviewBox.Controls.Add(this.picSettings);
            this.panelPreviewBox.Location = new System.Drawing.Point(80, 0);
            this.panelPreviewBox.Name = "panelPreviewBox";
            this.panelPreviewBox.Size = new System.Drawing.Size(720, 480);
            this.panelPreviewBox.TabIndex = 29;
            // 
            // picSettings
            // 
            this.picSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(34)))), ((int)(((byte)(34)))));
            this.picSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picSettings.Location = new System.Drawing.Point(0, 0);
            this.picSettings.Name = "picSettings";
            this.picSettings.Size = new System.Drawing.Size(720, 480);
            this.picSettings.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSettings.TabIndex = 0;
            this.picSettings.TabStop = false;
            this.picSettings.Visible = false;
            // 
            // flowPrevHead
            // 
            this.flowPrevHead.Controls.Add(this.btnPageView);
            this.flowPrevHead.Controls.Add(this.btnSetView);
            this.flowPrevHead.Controls.Add(this.btnReload);
            this.flowPrevHead.Controls.Add(this.lblPreview);
            this.flowPrevHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowPrevHead.Location = new System.Drawing.Point(10, 0);
            this.flowPrevHead.Name = "flowPrevHead";
            this.flowPrevHead.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.flowPrevHead.Size = new System.Drawing.Size(844, 52);
            this.flowPrevHead.TabIndex = 30;
            this.flowPrevHead.WrapContents = false;
            // 
            // btnPageView
            // 
            this.btnPageView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnPageView.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnPageView.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPageView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnPageView.Location = new System.Drawing.Point(3, 11);
            this.btnPageView.Name = "btnPageView";
            this.btnPageView.Size = new System.Drawing.Size(56, 28);
            this.btnPageView.TabIndex = 31;
            this.btnPageView.Text = "頁面";
            this.btnPageView.UseVisualStyleBackColor = false;
            this.btnPageView.Click += new System.EventHandler(this.btnPageView_Click);
            // 
            // btnSetView
            // 
            this.btnSetView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnSetView.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnSetView.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSetView.Location = new System.Drawing.Point(65, 11);
            this.btnSetView.Name = "btnSetView";
            this.btnSetView.Size = new System.Drawing.Size(56, 28);
            this.btnSetView.TabIndex = 32;
            this.btnSetView.Text = "設定";
            this.btnSetView.UseVisualStyleBackColor = false;
            this.btnSetView.Click += new System.EventHandler(this.btnSetView_Click);
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnReload.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnReload.Location = new System.Drawing.Point(127, 11);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(80, 28);
            this.btnReload.TabIndex = 33;
            this.btnReload.Text = "重新整理";
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // lblPreview
            // 
            this.lblPreview.AutoSize = true;
            this.lblPreview.ForeColor = System.Drawing.Color.Silver;
            this.lblPreview.Location = new System.Drawing.Point(224, 14);
            this.lblPreview.Margin = new System.Windows.Forms.Padding(14, 6, 0, 0);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Size = new System.Drawing.Size(176, 15);
            this.lblPreview.TabIndex = 34;
            this.lblPreview.Text = "預覽：點選左邊或上面的頁面";
            // 
            // flowLook
            // 
            this.flowLook.Controls.Add(this.lblSetBg);
            this.flowLook.Controls.Add(this.txtSetBg);
            this.flowLook.Controls.Add(this.btnSetBg);
            this.flowLook.Controls.Add(this.lblSetIcon);
            this.flowLook.Controls.Add(this.txtSetIcon);
            this.flowLook.Controls.Add(this.btnSetIcon);
            this.flowLook.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLook.Location = new System.Drawing.Point(0, 519);
            this.flowLook.Name = "flowLook";
            this.flowLook.Padding = new System.Windows.Forms.Padding(10, 6, 10, 0);
            this.flowLook.Size = new System.Drawing.Size(864, 40);
            this.flowLook.TabIndex = 35;
            this.flowLook.WrapContents = false;
            // 
            // lblSetBg
            // 
            this.lblSetBg.AutoSize = true;
            this.lblSetBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblSetBg.Location = new System.Drawing.Point(10, 13);
            this.lblSetBg.Margin = new System.Windows.Forms.Padding(0, 7, 4, 0);
            this.lblSetBg.Name = "lblSetBg";
            this.lblSetBg.Size = new System.Drawing.Size(81, 15);
            this.lblSetBg.TabIndex = 36;
            this.lblSetBg.Text = "Settings  背景";
            // 
            // txtSetBg
            // 
            this.txtSetBg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtSetBg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSetBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtSetBg.Location = new System.Drawing.Point(98, 9);
            this.txtSetBg.Name = "txtSetBg";
            this.txtSetBg.Size = new System.Drawing.Size(210, 23);
            this.txtSetBg.TabIndex = 37;
            this.txtSetBg.TextChanged += new System.EventHandler(this.txtSetBg_TextChanged);
            // 
            // btnSetBg
            // 
            this.btnSetBg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnSetBg.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnSetBg.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSetBg.Location = new System.Drawing.Point(314, 9);
            this.btnSetBg.Name = "btnSetBg";
            this.btnSetBg.Size = new System.Drawing.Size(30, 28);
            this.btnSetBg.TabIndex = 38;
            this.btnSetBg.Text = "...";
            this.btnSetBg.UseVisualStyleBackColor = false;
            this.btnSetBg.Click += new System.EventHandler(this.btnSetBg_Click);
            // 
            // lblSetIcon
            // 
            this.lblSetIcon.AutoSize = true;
            this.lblSetIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblSetIcon.Location = new System.Drawing.Point(361, 13);
            this.lblSetIcon.Margin = new System.Windows.Forms.Padding(14, 7, 4, 0);
            this.lblSetIcon.Name = "lblSetIcon";
            this.lblSetIcon.Size = new System.Drawing.Size(59, 15);
            this.lblSetIcon.TabIndex = 39;
            this.lblSetIcon.Text = "齒輪圖示";
            // 
            // txtSetIcon
            // 
            this.txtSetIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtSetIcon.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSetIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtSetIcon.Location = new System.Drawing.Point(427, 9);
            this.txtSetIcon.Name = "txtSetIcon";
            this.txtSetIcon.Size = new System.Drawing.Size(170, 23);
            this.txtSetIcon.TabIndex = 40;
            this.txtSetIcon.TextChanged += new System.EventHandler(this.txtSetIcon_TextChanged);
            // 
            // btnSetIcon
            // 
            this.btnSetIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnSetIcon.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnSetIcon.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSetIcon.Location = new System.Drawing.Point(603, 9);
            this.btnSetIcon.Name = "btnSetIcon";
            this.btnSetIcon.Size = new System.Drawing.Size(30, 28);
            this.btnSetIcon.TabIndex = 41;
            this.btnSetIcon.Text = "...";
            this.btnSetIcon.UseVisualStyleBackColor = false;
            this.btnSetIcon.Click += new System.EventHandler(this.btnSetIcon_Click);
            // 
            // flowBottom
            // 
            this.flowBottom.Controls.Add(this.btnNewPage);
            this.flowBottom.Controls.Add(this.btnOpenFolder);
            this.flowBottom.Controls.Add(this.btnRefreshLib);
            this.flowBottom.Controls.Add(this.lblStatus);
            this.flowBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowBottom.Location = new System.Drawing.Point(0, 559);
            this.flowBottom.Name = "flowBottom";
            this.flowBottom.Padding = new System.Windows.Forms.Padding(10, 6, 10, 0);
            this.flowBottom.Size = new System.Drawing.Size(864, 42);
            this.flowBottom.TabIndex = 42;
            this.flowBottom.WrapContents = false;
            // 
            // btnNewPage
            // 
            this.btnNewPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnNewPage.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnNewPage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnNewPage.Location = new System.Drawing.Point(13, 9);
            this.btnNewPage.Name = "btnNewPage";
            this.btnNewPage.Size = new System.Drawing.Size(100, 28);
            this.btnNewPage.TabIndex = 43;
            this.btnNewPage.Text = "New Page...";
            this.btnNewPage.UseVisualStyleBackColor = false;
            this.btnNewPage.Click += new System.EventHandler(this.btnNewPage_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnOpenFolder.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnOpenFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFolder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnOpenFolder.Location = new System.Drawing.Point(119, 9);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(140, 28);
            this.btnOpenFolder.TabIndex = 44;
            this.btnOpenFolder.Text = "Open Canvas Folder";
            this.btnOpenFolder.UseVisualStyleBackColor = false;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnRefreshLib
            // 
            this.btnRefreshLib.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnRefreshLib.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnRefreshLib.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshLib.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnRefreshLib.Location = new System.Drawing.Point(265, 9);
            this.btnRefreshLib.Name = "btnRefreshLib";
            this.btnRefreshLib.Size = new System.Drawing.Size(120, 28);
            this.btnRefreshLib.TabIndex = 45;
            this.btnRefreshLib.Text = "Refresh Library";
            this.btnRefreshLib.UseVisualStyleBackColor = false;
            this.btnRefreshLib.Click += new System.EventHandler(this.btnRefreshLib_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(398, 13);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(10, 7, 0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 15);
            this.lblStatus.TabIndex = 46;
            // 
            // previewTimer
            // 
            this.previewTimer.Interval = 250;
            this.previewTimer.Tick += new System.EventHandler(this.previewTimer_Tick);
            // 
            // FormCanvasEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(864, 601);
            this.Controls.Add(this.panelPreview);
            this.Controls.Add(this.tableTop);
            this.Controls.Add(this.flowLook);
            this.Controls.Add(this.flowBottom);
            this.Controls.Add(this.flowTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MinimumSize = new System.Drawing.Size(880, 640);
            this.Name = "FormCanvasEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Canvas Pages";
            this.flowTop.ResumeLayout(false);
            this.flowTop.PerformLayout();
            this.tableTop.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            this.panelSearch.ResumeLayout(false);
            this.panelSearch.PerformLayout();
            this.flowMid.ResumeLayout(false);
            this.panelRight.ResumeLayout(false);
            this.panelRight.PerformLayout();
            this.tableDetails.ResumeLayout(false);
            this.tableDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.panelPreview.ResumeLayout(false);
            this.panelPrevArea.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picSide)).EndInit();
            this.panelPreviewBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picSettings)).EndInit();
            this.flowPrevHead.ResumeLayout(false);
            this.flowPrevHead.PerformLayout();
            this.flowLook.ResumeLayout(false);
            this.flowLook.PerformLayout();
            this.flowBottom.ResumeLayout(false);
            this.flowBottom.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowTop;
        private System.Windows.Forms.Label lblConfig;
        private System.Windows.Forms.ComboBox cboConfig;
        private System.Windows.Forms.Button btnNewConfig;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnUse;
        private System.Windows.Forms.Label lblActive;
        private System.Windows.Forms.TableLayoutPanel tableTop;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.ListBox lstLibrary;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblLibrary;
        private System.Windows.Forms.FlowLayoutPanel flowMid;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.TableLayoutPanel tableDetails;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblIcon;
        private System.Windows.Forms.TextBox txtIcon;
        private System.Windows.Forms.Button btnBrowseIcon;
        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.ListBox lstPages;
        private System.Windows.Forms.Label lblPages;
        private System.Windows.Forms.Panel panelPreview;
        private System.Windows.Forms.Panel panelPrevArea;
        private System.Windows.Forms.PictureBox picSide;
        private System.Windows.Forms.Panel panelPreviewBox;
        private System.Windows.Forms.PictureBox picSettings;
        private System.Windows.Forms.FlowLayoutPanel flowPrevHead;
        private System.Windows.Forms.Button btnPageView;
        private System.Windows.Forms.Button btnSetView;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.FlowLayoutPanel flowLook;
        private System.Windows.Forms.Label lblSetBg;
        private System.Windows.Forms.TextBox txtSetBg;
        private System.Windows.Forms.Button btnSetBg;
        private System.Windows.Forms.Label lblSetIcon;
        private System.Windows.Forms.TextBox txtSetIcon;
        private System.Windows.Forms.Button btnSetIcon;
        private System.Windows.Forms.FlowLayoutPanel flowBottom;
        private System.Windows.Forms.Button btnNewPage;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnRefreshLib;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Timer previewTimer;
    }
}
