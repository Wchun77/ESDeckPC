namespace ESDeckPC
{
    partial class FormConfigEditor
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
            this.grpPages = new System.Windows.Forms.GroupBox();
            this.lstPages = new System.Windows.Forms.ListBox();
            this.btnSettingsPage = new System.Windows.Forms.Button();
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.picDeckPreview = new System.Windows.Forms.PictureBox();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnDiscard = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.grpPages.SuspendLayout();
            this.grpPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picDeckPreview)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.splitMain.Panel1.Controls.Add(this.grpPages);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.splitMain.Panel2.Controls.Add(this.grpPreview);
            this.splitMain.Size = new System.Drawing.Size(871, 512);
            this.splitMain.SplitterDistance = 121;
            this.splitMain.TabIndex = 0;
            //
            // grpPages
            //
            this.grpPages.Controls.Add(this.lstPages);
            this.grpPages.Controls.Add(this.btnSettingsPage);
            this.grpPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpPages.Location = new System.Drawing.Point(0, 0);
            this.grpPages.Name = "grpPages";
            this.grpPages.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpPages.Size = new System.Drawing.Size(121, 512);
            this.grpPages.TabIndex = 0;
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
            this.lstPages.Size = new System.Drawing.Size(105, 449);
            this.lstPages.TabIndex = 0;
            //
            // btnSettingsPage
            //
            this.btnSettingsPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnSettingsPage.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSettingsPage.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnSettingsPage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettingsPage.Font = new System.Drawing.Font("Consolas", 9F);
            this.btnSettingsPage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSettingsPage.Location = new System.Drawing.Point(8, 468);
            this.btnSettingsPage.Name = "btnSettingsPage";
            this.btnSettingsPage.Size = new System.Drawing.Size(105, 36);
            this.btnSettingsPage.TabIndex = 1;
            this.btnSettingsPage.Text = "Settings";
            this.btnSettingsPage.UseVisualStyleBackColor = false;
            //
            // grpPreview
            // 
            this.grpPreview.Controls.Add(this.picDeckPreview);
            this.grpPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpPreview.Location = new System.Drawing.Point(0, 0);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpPreview.Size = new System.Drawing.Size(746, 512);
            this.grpPreview.TabIndex = 0;
            this.grpPreview.TabStop = false;
            this.grpPreview.Text = "Preview";
            // 
            // picDeckPreview
            // 
            this.picDeckPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.picDeckPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picDeckPreview.Location = new System.Drawing.Point(11, 19);
            this.picDeckPreview.Name = "picDeckPreview";
            this.picDeckPreview.Size = new System.Drawing.Size(720, 480);
            this.picDeckPreview.TabIndex = 1;
            this.picDeckPreview.TabStop = false;
            // 
            // pnlBottom
            // 
            this.pnlBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlBottom.Controls.Add(this.btnDiscard);
            this.pnlBottom.Controls.Add(this.btnSave);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 512);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.pnlBottom.Size = new System.Drawing.Size(871, 40);
            this.pnlBottom.TabIndex = 1;
            // 
            // btnDiscard
            // 
            this.btnDiscard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDiscard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.btnDiscard.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnDiscard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDiscard.Font = new System.Drawing.Font("Consolas", 9F);
            this.btnDiscard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnDiscard.Location = new System.Drawing.Point(779, 6);
            this.btnDiscard.Name = "btnDiscard";
            this.btnDiscard.Size = new System.Drawing.Size(80, 26);
            this.btnDiscard.TabIndex = 1;
            this.btnDiscard.Text = "Discard";
            this.btnDiscard.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Consolas", 9F);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(693, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 26);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            // 
            // FormConfigEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(871, 552);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlBottom);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 300);
            this.Name = "FormConfigEditor";
            this.Text = "Config Editor";
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.grpPages.ResumeLayout(false);
            this.grpPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picDeckPreview)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.GroupBox grpPages;
        private System.Windows.Forms.ListBox lstPages;
        private System.Windows.Forms.Button btnSettingsPage;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnDiscard;
        private System.Windows.Forms.PictureBox picDeckPreview;
    }
}