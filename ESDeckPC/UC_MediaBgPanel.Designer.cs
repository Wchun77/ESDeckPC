namespace ESDeckPC
{
    partial class UC_MediaBgPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeOwnedResources();
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.grpBg = new System.Windows.Forms.GroupBox();
            this.txtBgImage = new System.Windows.Forms.TextBox();
            this.btnBgBrowse = new System.Windows.Forms.Button();
            this.btnBgClear = new System.Windows.Forms.Button();
            this.grpBg.SuspendLayout();
            this.SuspendLayout();
            //
            // grpBg
            //
            this.grpBg.Controls.Add(this.txtBgImage);
            this.grpBg.Controls.Add(this.btnBgBrowse);
            this.grpBg.Controls.Add(this.btnBgClear);
            this.grpBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpBg.Location = new System.Drawing.Point(12, 12);
            this.grpBg.Name = "grpBg";
            this.grpBg.Size = new System.Drawing.Size(234, 86);
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
            this.btnBgBrowse.Location = new System.Drawing.Point(8, 49);
            this.btnBgBrowse.Name = "btnBgBrowse";
            this.btnBgBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnBgBrowse.TabIndex = 1;
            this.btnBgBrowse.Text = "Browse...";
            //
            // btnBgClear
            //
            this.btnBgClear.Location = new System.Drawing.Point(96, 49);
            this.btnBgClear.Name = "btnBgClear";
            this.btnBgClear.Size = new System.Drawing.Size(60, 24);
            this.btnBgClear.TabIndex = 2;
            this.btnBgClear.Text = "Clear";
            //
            // UC_MediaBgPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.Controls.Add(this.grpBg);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_MediaBgPanel";
            this.Size = new System.Drawing.Size(265, 110);
            this.grpBg.ResumeLayout(false);
            this.grpBg.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpBg;
        private System.Windows.Forms.TextBox txtBgImage;
        private System.Windows.Forms.Button btnBgBrowse;
        private System.Windows.Forms.Button btnBgClear;
    }
}
