namespace ESDeckPC
{
    partial class FormFontBuilder
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.chkTime = new System.Windows.Forms.CheckBox();
            this.nudTime = new System.Windows.Forms.NumericUpDown();
            this.chkSec = new System.Windows.Forms.CheckBox();
            this.nudSec = new System.Windows.Forms.NumericUpDown();
            this.chkDate = new System.Windows.Forms.CheckBox();
            this.nudDate = new System.Windows.Forms.NumericUpDown();
            this.lblTtf = new System.Windows.Forms.Label();
            this.txtTtf = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnBuild = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDate)).BeginInit();
            this.SuspendLayout();
            // 
            // chkTime
            // 
            this.chkTime.AutoSize = true;
            this.chkTime.Checked = true;
            this.chkTime.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.chkTime.Location = new System.Drawing.Point(12, 54);
            this.chkTime.Name = "chkTime";
            this.chkTime.Size = new System.Drawing.Size(89, 18);
            this.chkTime.TabIndex = 2;
            this.chkTime.Text = "Time size";
            this.chkTime.CheckedChanged += new System.EventHandler(this.chkTime_CheckedChanged);
            // 
            // nudTime
            // 
            this.nudTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudTime.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudTime.Location = new System.Drawing.Point(107, 53);
            this.nudTime.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudTime.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudTime.Name = "nudTime";
            this.nudTime.Size = new System.Drawing.Size(64, 22);
            this.nudTime.TabIndex = 3;
            this.nudTime.Value = new decimal(new int[] {
            270,
            0,
            0,
            0});
            // 
            // chkSec
            // 
            this.chkSec.AutoSize = true;
            this.chkSec.Checked = true;
            this.chkSec.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.chkSec.Location = new System.Drawing.Point(12, 88);
            this.chkSec.Name = "chkSec";
            this.chkSec.Size = new System.Drawing.Size(82, 18);
            this.chkSec.TabIndex = 4;
            this.chkSec.Text = "Sec size";
            this.chkSec.CheckedChanged += new System.EventHandler(this.chkSec_CheckedChanged);
            // 
            // nudSec
            // 
            this.nudSec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudSec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudSec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudSec.Location = new System.Drawing.Point(107, 87);
            this.nudSec.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudSec.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudSec.Name = "nudSec";
            this.nudSec.Size = new System.Drawing.Size(64, 22);
            this.nudSec.TabIndex = 5;
            this.nudSec.Value = new decimal(new int[] {
            48,
            0,
            0,
            0});
            // 
            // chkDate
            // 
            this.chkDate.AutoSize = true;
            this.chkDate.Checked = true;
            this.chkDate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.chkDate.Location = new System.Drawing.Point(12, 122);
            this.chkDate.Name = "chkDate";
            this.chkDate.Size = new System.Drawing.Size(89, 18);
            this.chkDate.TabIndex = 6;
            this.chkDate.Text = "Date size";
            this.chkDate.CheckedChanged += new System.EventHandler(this.chkDate_CheckedChanged);
            // 
            // nudDate
            // 
            this.nudDate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudDate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudDate.Location = new System.Drawing.Point(107, 121);
            this.nudDate.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudDate.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudDate.Name = "nudDate";
            this.nudDate.Size = new System.Drawing.Size(64, 22);
            this.nudDate.TabIndex = 7;
            this.nudDate.Value = new decimal(new int[] {
            36,
            0,
            0,
            0});
            // 
            // lblTtf
            // 
            this.lblTtf.AutoSize = true;
            this.lblTtf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblTtf.Location = new System.Drawing.Point(12, 18);
            this.lblTtf.Name = "lblTtf";
            this.lblTtf.Size = new System.Drawing.Size(63, 14);
            this.lblTtf.TabIndex = 0;
            this.lblTtf.Text = "TTF File";
            // 
            // txtTtf
            // 
            this.txtTtf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtTtf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTtf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtTtf.Location = new System.Drawing.Point(81, 14);
            this.txtTtf.Name = "txtTtf";
            this.txtTtf.ReadOnly = true;
            this.txtTtf.Size = new System.Drawing.Size(244, 22);
            this.txtTtf.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBrowse.Location = new System.Drawing.Point(332, 14);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(72, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnBuild
            // 
            this.btnBuild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBuild.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBuild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuild.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBuild.Location = new System.Drawing.Point(316, 117);
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Size = new System.Drawing.Size(88, 26);
            this.btnBuild.TabIndex = 8;
            this.btnBuild.Text = "Build";
            this.btnBuild.UseVisualStyleBackColor = false;
            this.btnBuild.Click += new System.EventHandler(this.btnBuild_Click);
            // 
            // FormFontBuilder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(416, 155);
            this.Controls.Add(this.lblTtf);
            this.Controls.Add(this.txtTtf);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.chkTime);
            this.Controls.Add(this.nudTime);
            this.Controls.Add(this.chkSec);
            this.Controls.Add(this.nudSec);
            this.Controls.Add(this.chkDate);
            this.Controls.Add(this.nudDate);
            this.Controls.Add(this.btnBuild);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormFontBuilder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Font Builder";
            ((System.ComponentModel.ISupportInitialize)(this.nudTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDate)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblTtf;
        private System.Windows.Forms.TextBox txtTtf;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chkTime;
        private System.Windows.Forms.NumericUpDown nudTime;
        private System.Windows.Forms.CheckBox chkSec;
        private System.Windows.Forms.NumericUpDown nudSec;
        private System.Windows.Forms.CheckBox chkDate;
        private System.Windows.Forms.NumericUpDown nudDate;
        private System.Windows.Forms.Button btnBuild;
    }
}