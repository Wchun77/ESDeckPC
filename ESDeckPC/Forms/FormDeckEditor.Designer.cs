namespace ESDeckPC
{
    partial class FormDeckEditor
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
            this.TLPanel_FULL = new System.Windows.Forms.TableLayoutPanel();
            this.paneltop = new System.Windows.Forms.Panel();
            this.grpJson = new System.Windows.Forms.GroupBox();
            this.btnJsonSaveAs = new System.Windows.Forms.Button();
            this.lblJsonPath = new System.Windows.Forms.Label();
            this.btnJsonNew = new System.Windows.Forms.Button();
            this.btnJsonOpen = new System.Windows.Forms.Button();
            this.btnJsonSave = new System.Windows.Forms.Button();
            this.TLPanel_BOTTOM = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSettingsHost = new System.Windows.Forms.Panel();
            this.ucBg = new ESDeckPC.UC_MediaBgPanel();
            this.ucSideIcon = new ESDeckPC.UC_SideIconField();
            this.pnlName = new System.Windows.Forms.Panel();
            this.grpName = new System.Windows.Forms.GroupBox();
            this.txtPageName = new System.Windows.Forms.TextBox();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.sideView = new ESDeckPC.EspSidebarView();
            this.lblStatus = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.TLPanel_FULL.SuspendLayout();
            this.paneltop.SuspendLayout();
            this.grpJson.SuspendLayout();
            this.TLPanel_BOTTOM.SuspendLayout();
            this.pnlSettingsHost.SuspendLayout();
            this.pnlName.SuspendLayout();
            this.grpName.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // TLPanel_FULL
            // 
            this.TLPanel_FULL.ColumnCount = 1;
            this.TLPanel_FULL.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLPanel_FULL.Controls.Add(this.paneltop, 0, 0);
            this.TLPanel_FULL.Controls.Add(this.TLPanel_BOTTOM, 0, 1);
            this.TLPanel_FULL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TLPanel_FULL.Location = new System.Drawing.Point(0, 0);
            this.TLPanel_FULL.Name = "TLPanel_FULL";
            this.TLPanel_FULL.RowCount = 2;
            this.TLPanel_FULL.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLPanel_FULL.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 550F));
            this.TLPanel_FULL.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TLPanel_FULL.Size = new System.Drawing.Size(1126, 648);
            this.TLPanel_FULL.TabIndex = 0;
            // 
            // paneltop
            // 
            this.paneltop.Controls.Add(this.grpJson);
            this.paneltop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paneltop.Location = new System.Drawing.Point(3, 3);
            this.paneltop.Name = "paneltop";
            this.paneltop.Size = new System.Drawing.Size(1040, 92);
            this.paneltop.TabIndex = 0;
            // 
            // grpJson
            // 
            this.grpJson.Controls.Add(this.btnJsonSaveAs);
            this.grpJson.Controls.Add(this.lblJsonPath);
            this.grpJson.Controls.Add(this.btnJsonNew);
            this.grpJson.Controls.Add(this.btnJsonOpen);
            this.grpJson.Controls.Add(this.btnJsonSave);
            this.grpJson.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpJson.Location = new System.Drawing.Point(9, 9);
            this.grpJson.Name = "grpJson";
            this.grpJson.Size = new System.Drawing.Size(298, 76);
            this.grpJson.TabIndex = 3;
            this.grpJson.TabStop = false;
            this.grpJson.Text = "JSON";
            // 
            // btnJsonSaveAs
            // 
            this.btnJsonSaveAs.Location = new System.Drawing.Point(206, 42);
            this.btnJsonSaveAs.Name = "btnJsonSaveAs";
            this.btnJsonSaveAs.Size = new System.Drawing.Size(84, 26);
            this.btnJsonSaveAs.TabIndex = 4;
            this.btnJsonSaveAs.Text = "Save As";
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
            // 
            // btnJsonOpen
            // 
            this.btnJsonOpen.Location = new System.Drawing.Point(74, 42);
            this.btnJsonOpen.Name = "btnJsonOpen";
            this.btnJsonOpen.Size = new System.Drawing.Size(60, 26);
            this.btnJsonOpen.TabIndex = 2;
            this.btnJsonOpen.Text = "Open";
            // 
            // btnJsonSave
            // 
            this.btnJsonSave.Location = new System.Drawing.Point(140, 42);
            this.btnJsonSave.Name = "btnJsonSave";
            this.btnJsonSave.Size = new System.Drawing.Size(60, 26);
            this.btnJsonSave.TabIndex = 3;
            this.btnJsonSave.Text = "Save";
            // 
            // TLPanel_BOTTOM
            // 
            this.TLPanel_BOTTOM.ColumnCount = 2;
            this.TLPanel_BOTTOM.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.26786F));
            this.TLPanel_BOTTOM.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 74.73214F));
            this.TLPanel_BOTTOM.Controls.Add(this.pnlSettingsHost, 0, 0);
            this.TLPanel_BOTTOM.Controls.Add(this.pnlPreview, 1, 0);
            this.TLPanel_BOTTOM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TLPanel_BOTTOM.Location = new System.Drawing.Point(3, 101);
            this.TLPanel_BOTTOM.Name = "TLPanel_BOTTOM";
            this.TLPanel_BOTTOM.RowCount = 1;
            this.TLPanel_BOTTOM.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLPanel_BOTTOM.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 550F));
            this.TLPanel_BOTTOM.Size = new System.Drawing.Size(1120, 544);
            this.TLPanel_BOTTOM.TabIndex = 2;
            // 
            // pnlSettingsHost
            // 
            this.pnlSettingsHost.AutoScroll = true;
            this.pnlSettingsHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlSettingsHost.Controls.Add(this.ucBg);
            this.pnlSettingsHost.Controls.Add(this.ucSideIcon);
            this.pnlSettingsHost.Controls.Add(this.pnlName);
            this.pnlSettingsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSettingsHost.Location = new System.Drawing.Point(3, 3);
            this.pnlSettingsHost.Name = "pnlSettingsHost";
            this.pnlSettingsHost.Size = new System.Drawing.Size(277, 538);
            this.pnlSettingsHost.TabIndex = 3;
            // 
            // ucBg
            // 
            this.ucBg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ucBg.Dock = System.Windows.Forms.DockStyle.Top;
            this.ucBg.Font = new System.Drawing.Font("Consolas", 9F);
            this.ucBg.Location = new System.Drawing.Point(0, 196);
            this.ucBg.Margin = new System.Windows.Forms.Padding(4);
            this.ucBg.Name = "ucBg";
            this.ucBg.Size = new System.Drawing.Size(277, 110);
            this.ucBg.TabIndex = 2;
            // 
            // ucSideIcon
            // 
            this.ucSideIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ucSideIcon.Dock = System.Windows.Forms.DockStyle.Top;
            this.ucSideIcon.Font = new System.Drawing.Font("Consolas", 9F);
            this.ucSideIcon.Location = new System.Drawing.Point(0, 70);
            this.ucSideIcon.Margin = new System.Windows.Forms.Padding(4);
            this.ucSideIcon.Name = "ucSideIcon";
            this.ucSideIcon.Size = new System.Drawing.Size(277, 126);
            this.ucSideIcon.TabIndex = 1;
            // 
            // pnlName
            // 
            this.pnlName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.pnlName.Controls.Add(this.grpName);
            this.pnlName.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlName.Location = new System.Drawing.Point(0, 0);
            this.pnlName.Name = "pnlName";
            this.pnlName.Size = new System.Drawing.Size(277, 70);
            this.pnlName.TabIndex = 0;
            // 
            // grpName
            // 
            this.grpName.Controls.Add(this.txtPageName);
            this.grpName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpName.Location = new System.Drawing.Point(12, 12);
            this.grpName.Name = "grpName";
            this.grpName.Size = new System.Drawing.Size(234, 54);
            this.grpName.TabIndex = 0;
            this.grpName.TabStop = false;
            this.grpName.Text = "Page name";
            // 
            // txtPageName
            // 
            this.txtPageName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtPageName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPageName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtPageName.Location = new System.Drawing.Point(8, 21);
            this.txtPageName.Name = "txtPageName";
            this.txtPageName.Size = new System.Drawing.Size(218, 22);
            this.txtPageName.TabIndex = 0;
            // 
            // pnlPreview
            // 
            this.pnlPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlPreview.Controls.Add(this.sideView);
            this.pnlPreview.Controls.Add(this.picPreview);
            this.pnlPreview.Controls.Add(this.lblStatus);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(286, 3);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(831, 538);
            this.pnlPreview.TabIndex = 2;
            // 
            // picPreview
            // 
            this.picPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPreview.Location = new System.Drawing.Point(93, 11);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(720, 480);
            this.picPreview.TabIndex = 0;
            this.picPreview.TabStop = false;
            // 
            // sideView
            // 
            this.sideView.AllowAdd = true;
            this.sideView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.sideView.FixedLeading = 0;
            this.sideView.Location = new System.Drawing.Point(13, 11);
            this.sideView.Name = "sideView";
            this.sideView.Size = new System.Drawing.Size(80, 480);
            this.sideView.TabIndex = 4;
            this.toolTip.SetToolTip(this.sideView, "Click: select    Drag: reorder pages    Wheel: scroll\r\n+: add a page    Right-click: delete");
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(0, 518);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(831, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FormDeckEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(1126, 648);
            this.Controls.Add(this.TLPanel_FULL);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 580);
            this.Name = "FormDeckEditor";
            this.ShowIcon = false;
            this.Text = "Deck Editor";
            this.TLPanel_FULL.ResumeLayout(false);
            this.paneltop.ResumeLayout(false);
            this.grpJson.ResumeLayout(false);
            this.TLPanel_BOTTOM.ResumeLayout(false);
            this.pnlSettingsHost.ResumeLayout(false);
            this.pnlName.ResumeLayout(false);
            this.grpName.ResumeLayout(false);
            this.grpName.PerformLayout();
            this.pnlPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TLPanel_FULL;
        private System.Windows.Forms.Panel paneltop;
        private System.Windows.Forms.TableLayoutPanel TLPanel_BOTTOM;
        private System.Windows.Forms.GroupBox grpJson;
        private System.Windows.Forms.Label lblJsonPath;
        private System.Windows.Forms.Button btnJsonNew;
        private System.Windows.Forms.Button btnJsonOpen;
        private System.Windows.Forms.Button btnJsonSave;
        private System.Windows.Forms.Panel pnlSettingsHost;
        private ESDeckPC.UC_MediaBgPanel ucBg;
        private ESDeckPC.UC_SideIconField ucSideIcon;
        private System.Windows.Forms.Panel pnlName;
        private System.Windows.Forms.GroupBox grpName;
        private System.Windows.Forms.TextBox txtPageName;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.PictureBox picPreview;
        private ESDeckPC.EspSidebarView sideView;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnJsonSaveAs;
    }
}