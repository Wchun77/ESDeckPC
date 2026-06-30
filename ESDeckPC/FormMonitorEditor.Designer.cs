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
            this.TLPanel_FULL = new System.Windows.Forms.TableLayoutPanel();
            this.paneltop = new System.Windows.Forms.Panel();
            this.btnClockPage = new System.Windows.Forms.Button();
            this.flpPages = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.grpJson = new System.Windows.Forms.GroupBox();
            this.lblJsonPath = new System.Windows.Forms.Label();
            this.btnJsonNew = new System.Windows.Forms.Button();
            this.btnJsonOpen = new System.Windows.Forms.Button();
            this.btnJsonSave = new System.Windows.Forms.Button();
            this.TLPanel_BOTTOM = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSettingsHost = new System.Windows.Forms.Panel();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnJsonSaveAs = new System.Windows.Forms.Button();
            this.TLPanel_FULL.SuspendLayout();
            this.paneltop.SuspendLayout();
            this.flpPages.SuspendLayout();
            this.grpJson.SuspendLayout();
            this.TLPanel_BOTTOM.SuspendLayout();
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
            this.TLPanel_FULL.Size = new System.Drawing.Size(1046, 648);
            this.TLPanel_FULL.TabIndex = 0;
            // 
            // paneltop
            // 
            this.paneltop.Controls.Add(this.btnClockPage);
            this.paneltop.Controls.Add(this.flpPages);
            this.paneltop.Controls.Add(this.grpJson);
            this.paneltop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paneltop.Location = new System.Drawing.Point(3, 3);
            this.paneltop.Name = "paneltop";
            this.paneltop.Size = new System.Drawing.Size(1040, 92);
            this.paneltop.TabIndex = 0;
            // 
            // btnClockPage
            // 
            this.btnClockPage.Location = new System.Drawing.Point(357, 25);
            this.btnClockPage.Name = "btnClockPage";
            this.btnClockPage.Size = new System.Drawing.Size(79, 55);
            this.btnClockPage.TabIndex = 4;
            this.btnClockPage.Text = "Clock";
            // 
            // flpPages
            // 
            this.flpPages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpPages.Controls.Add(this.button1);
            this.flpPages.Location = new System.Drawing.Point(446, 21);
            this.flpPages.Name = "flpPages";
            this.flpPages.Size = new System.Drawing.Size(332, 64);
            this.flpPages.TabIndex = 5;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(79, 55);
            this.button1.TabIndex = 5;
            this.button1.Text = "TestUse";
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
            this.TLPanel_BOTTOM.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.21154F));
            this.TLPanel_BOTTOM.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 72.78846F));
            this.TLPanel_BOTTOM.Controls.Add(this.pnlSettingsHost, 0, 0);
            this.TLPanel_BOTTOM.Controls.Add(this.pnlPreview, 1, 0);
            this.TLPanel_BOTTOM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TLPanel_BOTTOM.Location = new System.Drawing.Point(3, 101);
            this.TLPanel_BOTTOM.Name = "TLPanel_BOTTOM";
            this.TLPanel_BOTTOM.RowCount = 1;
            this.TLPanel_BOTTOM.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLPanel_BOTTOM.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 550F));
            this.TLPanel_BOTTOM.Size = new System.Drawing.Size(1040, 544);
            this.TLPanel_BOTTOM.TabIndex = 2;
            // 
            // pnlSettingsHost
            // 
            this.pnlSettingsHost.AutoScroll = true;
            this.pnlSettingsHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlSettingsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSettingsHost.Location = new System.Drawing.Point(3, 3);
            this.pnlSettingsHost.Name = "pnlSettingsHost";
            this.pnlSettingsHost.Size = new System.Drawing.Size(277, 538);
            this.pnlSettingsHost.TabIndex = 3;
            // 
            // pnlPreview
            // 
            this.pnlPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.pnlPreview.Controls.Add(this.picPreview);
            this.pnlPreview.Controls.Add(this.lblStatus);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(286, 3);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(751, 538);
            this.pnlPreview.TabIndex = 2;
            // 
            // picPreview
            // 
            this.picPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPreview.Location = new System.Drawing.Point(13, 11);
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
            this.lblStatus.Location = new System.Drawing.Point(0, 518);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(751, 20);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "No font loaded";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnJsonSaveAs
            // 
            this.btnJsonSaveAs.Location = new System.Drawing.Point(206, 42);
            this.btnJsonSaveAs.Name = "btnJsonSaveAs";
            this.btnJsonSaveAs.Size = new System.Drawing.Size(84, 26);
            this.btnJsonSaveAs.TabIndex = 4;
            this.btnJsonSaveAs.Text = "Save As";
            // 
            // FormMonitorEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(1046, 648);
            this.Controls.Add(this.TLPanel_FULL);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MinimumSize = new System.Drawing.Size(820, 580);
            this.Name = "FormMonitorEditor";
            this.Text = "Monitor Editor";
            this.TLPanel_FULL.ResumeLayout(false);
            this.paneltop.ResumeLayout(false);
            this.flpPages.ResumeLayout(false);
            this.grpJson.ResumeLayout(false);
            this.TLPanel_BOTTOM.ResumeLayout(false);
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
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.PictureBox picPreview;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnClockPage;
        private System.Windows.Forms.FlowLayoutPanel flpPages;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnJsonSaveAs;
    }
}