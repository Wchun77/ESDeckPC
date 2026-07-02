namespace ESDeckPC
{
    partial class FormButtonEditor
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
            this.lblLabelTag = new System.Windows.Forms.Label();
            this.txtLabel = new System.Windows.Forms.TextBox();
            this.lblIconTag = new System.Windows.Forms.Label();
            this.txtIcon = new System.Windows.Forms.TextBox();
            this.lblActionTag = new System.Windows.Forms.Label();
            this.cmbAction = new System.Windows.Forms.ComboBox();
            this.pnlDynamic = new System.Windows.Forms.Panel();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblLabelTag
            // 
            this.lblLabelTag.AutoSize = true;
            this.lblLabelTag.ForeColor = System.Drawing.Color.Gray;
            this.lblLabelTag.Location = new System.Drawing.Point(16, 16);
            this.lblLabelTag.Name = "lblLabelTag";
            this.lblLabelTag.Size = new System.Drawing.Size(42, 14);
            this.lblLabelTag.TabIndex = 0;
            this.lblLabelTag.Text = "Label";
            // 
            // txtLabel
            // 
            this.txtLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtLabel.Location = new System.Drawing.Point(16, 32);
            this.txtLabel.Name = "txtLabel";
            this.txtLabel.Size = new System.Drawing.Size(352, 22);
            this.txtLabel.TabIndex = 0;
            // 
            // lblIconTag
            // 
            this.lblIconTag.AutoSize = true;
            this.lblIconTag.ForeColor = System.Drawing.Color.Gray;
            this.lblIconTag.Location = new System.Drawing.Point(16, 64);
            this.lblIconTag.Name = "lblIconTag";
            this.lblIconTag.Size = new System.Drawing.Size(112, 14);
            this.lblIconTag.TabIndex = 1;
            this.lblIconTag.Text = "Icon (filename)";
            // 
            // txtIcon
            // 
            this.txtIcon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtIcon.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtIcon.Location = new System.Drawing.Point(16, 80);
            this.txtIcon.Name = "txtIcon";
            this.txtIcon.Size = new System.Drawing.Size(352, 22);
            this.txtIcon.TabIndex = 1;
            // 
            // lblActionTag
            // 
            this.lblActionTag.AutoSize = true;
            this.lblActionTag.ForeColor = System.Drawing.Color.Gray;
            this.lblActionTag.Location = new System.Drawing.Point(16, 112);
            this.lblActionTag.Name = "lblActionTag";
            this.lblActionTag.Size = new System.Drawing.Size(49, 14);
            this.lblActionTag.TabIndex = 2;
            this.lblActionTag.Text = "Action";
            // 
            // cmbAction
            // 
            this.cmbAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAction.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAction.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cmbAction.Location = new System.Drawing.Point(16, 128);
            this.cmbAction.Name = "cmbAction";
            this.cmbAction.Size = new System.Drawing.Size(352, 22);
            this.cmbAction.TabIndex = 2;
            // 
            // pnlDynamic
            // 
            this.pnlDynamic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.pnlDynamic.Location = new System.Drawing.Point(16, 160);
            this.pnlDynamic.Name = "pnlDynamic";
            this.pnlDynamic.Size = new System.Drawing.Size(352, 100);
            this.pnlDynamic.TabIndex = 3;
            // 
            // pnlBottom
            // 
            this.pnlBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnOK);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 270);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.pnlBottom.Size = new System.Drawing.Size(384, 40);
            this.pnlBottom.TabIndex = 4;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Consolas", 9F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnCancel.Location = new System.Drawing.Point(296, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 26);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("Consolas", 9F);
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(210, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 26);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            // 
            // FormButtonEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(384, 310);
            this.Controls.Add(this.lblLabelTag);
            this.Controls.Add(this.txtLabel);
            this.Controls.Add(this.lblIconTag);
            this.Controls.Add(this.txtIcon);
            this.Controls.Add(this.lblActionTag);
            this.Controls.Add(this.cmbAction);
            this.Controls.Add(this.pnlDynamic);
            this.Controls.Add(this.pnlBottom);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormButtonEditor";
            this.Text = "Edit Button";
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLabelTag;
        private System.Windows.Forms.TextBox txtLabel;
        private System.Windows.Forms.Label lblIconTag;
        private System.Windows.Forms.TextBox txtIcon;
        private System.Windows.Forms.Label lblActionTag;
        private System.Windows.Forms.ComboBox cmbAction;
        private System.Windows.Forms.Panel pnlDynamic;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}