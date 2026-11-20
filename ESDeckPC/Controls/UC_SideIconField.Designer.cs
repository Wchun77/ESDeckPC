namespace ESDeckPC
{
    partial class UC_SideIconField
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
            this.grpIcon = new System.Windows.Forms.GroupBox();
            this.picThumb = new System.Windows.Forms.Panel();
            this.lblName = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.lblWarn = new System.Windows.Forms.Label();
            this.grpIcon.SuspendLayout();
            this.SuspendLayout();
            //
            // grpIcon
            //
            this.grpIcon.Controls.Add(this.picThumb);
            this.grpIcon.Controls.Add(this.lblName);
            this.grpIcon.Controls.Add(this.btnBrowse);
            this.grpIcon.Controls.Add(this.btnClear);
            this.grpIcon.Controls.Add(this.lblWarn);
            this.grpIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpIcon.Location = new System.Drawing.Point(12, 12);
            this.grpIcon.Name = "grpIcon";
            this.grpIcon.Size = new System.Drawing.Size(234, 108);
            this.grpIcon.TabIndex = 0;
            this.grpIcon.TabStop = false;
            this.grpIcon.Text = "Side Icon";
            //
            // picThumb
            //
            this.picThumb.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.picThumb.Location = new System.Drawing.Point(8, 20);
            this.picThumb.Name = "picThumb";
            this.picThumb.Size = new System.Drawing.Size(64, 64);
            this.picThumb.TabIndex = 0;
            //
            // lblName
            //
            this.lblName.AutoEllipsis = true;
            this.lblName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblName.Location = new System.Drawing.Point(80, 22);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(146, 18);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "(none)";
            //
            // btnBrowse
            //
            this.btnBrowse.Location = new System.Drawing.Point(80, 48);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse...";
            //
            // btnClear
            //
            this.btnClear.Location = new System.Drawing.Point(166, 48);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(60, 24);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear";
            //
            // lblWarn
            //
            this.lblWarn.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblWarn.ForeColor = System.Drawing.Color.Orange;
            this.lblWarn.Location = new System.Drawing.Point(8, 88);
            this.lblWarn.Name = "lblWarn";
            this.lblWarn.Size = new System.Drawing.Size(218, 16);
            this.lblWarn.TabIndex = 4;
            //
            // UC_SideIconField
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.Controls.Add(this.grpIcon);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_SideIconField";
            this.Size = new System.Drawing.Size(265, 126);
            this.grpIcon.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpIcon;
        private System.Windows.Forms.Panel picThumb;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Label lblWarn;
    }
}
