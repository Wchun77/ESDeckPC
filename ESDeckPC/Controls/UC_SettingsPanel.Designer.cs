namespace ESDeckPC
{
    partial class UC_SettingsPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.grpSettingsBg = new System.Windows.Forms.GroupBox();
            this.txtSettingsBgImage = new System.Windows.Forms.TextBox();
            this.btnSettingsBgBrowse = new System.Windows.Forms.Button();
            this.btnSettingsBgClear = new System.Windows.Forms.Button();
            this.grpSettingsBg.SuspendLayout();
            this.SuspendLayout();
            //
            // grpSettingsBg
            //
            this.grpSettingsBg.Controls.Add(this.txtSettingsBgImage);
            this.grpSettingsBg.Controls.Add(this.btnSettingsBgBrowse);
            this.grpSettingsBg.Controls.Add(this.btnSettingsBgClear);
            this.grpSettingsBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpSettingsBg.Location = new System.Drawing.Point(12, 12);
            this.grpSettingsBg.Name = "grpSettingsBg";
            this.grpSettingsBg.Size = new System.Drawing.Size(234, 86);
            this.grpSettingsBg.TabIndex = 0;
            this.grpSettingsBg.TabStop = false;
            this.grpSettingsBg.Text = "Background image";
            //
            // txtSettingsBgImage
            //
            this.txtSettingsBgImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtSettingsBgImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtSettingsBgImage.Location = new System.Drawing.Point(8, 20);
            this.txtSettingsBgImage.Name = "txtSettingsBgImage";
            this.txtSettingsBgImage.ReadOnly = true;
            this.txtSettingsBgImage.Size = new System.Drawing.Size(218, 22);
            this.txtSettingsBgImage.TabIndex = 0;
            //
            // btnSettingsBgBrowse
            //
            this.btnSettingsBgBrowse.Location = new System.Drawing.Point(8, 49);
            this.btnSettingsBgBrowse.Name = "btnSettingsBgBrowse";
            this.btnSettingsBgBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnSettingsBgBrowse.TabIndex = 1;
            this.btnSettingsBgBrowse.Text = "Browse...";
            //
            // btnSettingsBgClear
            //
            this.btnSettingsBgClear.Location = new System.Drawing.Point(96, 49);
            this.btnSettingsBgClear.Name = "btnSettingsBgClear";
            this.btnSettingsBgClear.Size = new System.Drawing.Size(60, 24);
            this.btnSettingsBgClear.TabIndex = 2;
            this.btnSettingsBgClear.Text = "Clear";
            //
            // UC_SettingsPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.Controls.Add(this.grpSettingsBg);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_SettingsPanel";
            this.Size = new System.Drawing.Size(265, 110);
            this.grpSettingsBg.ResumeLayout(false);
            this.grpSettingsBg.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpSettingsBg;
        private System.Windows.Forms.TextBox txtSettingsBgImage;
        private System.Windows.Forms.Button btnSettingsBgBrowse;
        private System.Windows.Forms.Button btnSettingsBgClear;
    }
}
