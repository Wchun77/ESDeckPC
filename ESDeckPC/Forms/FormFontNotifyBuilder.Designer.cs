namespace ESDeckPC
{
    partial class FormFontNotifyBuilder
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
            this.grpGenerate = new System.Windows.Forms.GroupBox();
            this.lblTtf = new System.Windows.Forms.Label();
            this.txtTtf = new System.Windows.Forms.TextBox();
            this.btnBrowseTtf = new System.Windows.Forms.Button();
            this.lblHanzi = new System.Windows.Forms.Label();
            this.txtHanzi = new System.Windows.Forms.TextBox();
            this.btnBrowseHanzi = new System.Windows.Forms.Button();
            this.lblSize = new System.Windows.Forms.Label();
            this.nudSize = new System.Windows.Forms.NumericUpDown();
            this.btnBuild = new System.Windows.Forms.Button();
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.lblBin = new System.Windows.Forms.Label();
            this.txtBin = new System.Windows.Forms.TextBox();
            this.btnBrowseBin = new System.Windows.Forms.Button();
            this.lblChar = new System.Windows.Forms.Label();
            this.txtChar = new System.Windows.Forms.TextBox();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.grpGenerate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSize)).BeginInit();
            this.grpPreview.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpGenerate
            // 
            this.grpGenerate.Controls.Add(this.lblTtf);
            this.grpGenerate.Controls.Add(this.txtTtf);
            this.grpGenerate.Controls.Add(this.btnBrowseTtf);
            this.grpGenerate.Controls.Add(this.lblHanzi);
            this.grpGenerate.Controls.Add(this.txtHanzi);
            this.grpGenerate.Controls.Add(this.btnBrowseHanzi);
            this.grpGenerate.Controls.Add(this.lblSize);
            this.grpGenerate.Controls.Add(this.nudSize);
            this.grpGenerate.Controls.Add(this.btnBuild);
            this.grpGenerate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpGenerate.Location = new System.Drawing.Point(10, 10);
            this.grpGenerate.Name = "grpGenerate";
            this.grpGenerate.Padding = new System.Windows.Forms.Padding(8);
            this.grpGenerate.Size = new System.Drawing.Size(430, 135);
            this.grpGenerate.TabIndex = 0;
            this.grpGenerate.TabStop = false;
            this.grpGenerate.Text = "Generate";
            // 
            // lblTtf
            // 
            this.lblTtf.AutoSize = true;
            this.lblTtf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblTtf.Location = new System.Drawing.Point(10, 30);
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
            this.txtTtf.Location = new System.Drawing.Point(100, 26);
            this.txtTtf.Name = "txtTtf";
            this.txtTtf.ReadOnly = true;
            this.txtTtf.Size = new System.Drawing.Size(233, 22);
            this.txtTtf.TabIndex = 1;
            // 
            // btnBrowseTtf
            // 
            this.btnBrowseTtf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBrowseTtf.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBrowseTtf.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseTtf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBrowseTtf.Location = new System.Drawing.Point(339, 25);
            this.btnBrowseTtf.Name = "btnBrowseTtf";
            this.btnBrowseTtf.Size = new System.Drawing.Size(76, 23);
            this.btnBrowseTtf.TabIndex = 2;
            this.btnBrowseTtf.Text = "Browse";
            this.btnBrowseTtf.UseVisualStyleBackColor = false;
            this.btnBrowseTtf.Click += new System.EventHandler(this.btnBrowseTtf_Click);
            // 
            // lblHanzi
            // 
            this.lblHanzi.AutoSize = true;
            this.lblHanzi.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblHanzi.Location = new System.Drawing.Point(10, 64);
            this.lblHanzi.Name = "lblHanzi";
            this.lblHanzi.Size = new System.Drawing.Size(77, 14);
            this.lblHanzi.TabIndex = 3;
            this.lblHanzi.Text = "Hanzi List";
            // 
            // txtHanzi
            // 
            this.txtHanzi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtHanzi.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtHanzi.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtHanzi.Location = new System.Drawing.Point(100, 60);
            this.txtHanzi.Name = "txtHanzi";
            this.txtHanzi.ReadOnly = true;
            this.txtHanzi.Size = new System.Drawing.Size(233, 22);
            this.txtHanzi.TabIndex = 4;
            // 
            // btnBrowseHanzi
            // 
            this.btnBrowseHanzi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBrowseHanzi.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBrowseHanzi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseHanzi.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBrowseHanzi.Location = new System.Drawing.Point(339, 59);
            this.btnBrowseHanzi.Name = "btnBrowseHanzi";
            this.btnBrowseHanzi.Size = new System.Drawing.Size(76, 23);
            this.btnBrowseHanzi.TabIndex = 5;
            this.btnBrowseHanzi.Text = "Browse";
            this.btnBrowseHanzi.UseVisualStyleBackColor = false;
            this.btnBrowseHanzi.Click += new System.EventHandler(this.btnBrowseHanzi_Click);
            // 
            // lblSize
            // 
            this.lblSize.AutoSize = true;
            this.lblSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblSize.Location = new System.Drawing.Point(10, 98);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(35, 14);
            this.lblSize.TabIndex = 6;
            this.lblSize.Text = "Size";
            // 
            // nudSize
            // 
            this.nudSize.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.nudSize.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nudSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.nudSize.Location = new System.Drawing.Point(100, 96);
            this.nudSize.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.nudSize.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudSize.Name = "nudSize";
            this.nudSize.Size = new System.Drawing.Size(64, 22);
            this.nudSize.TabIndex = 7;
            this.nudSize.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            // 
            // btnBuild
            // 
            this.btnBuild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBuild.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBuild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuild.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBuild.Location = new System.Drawing.Point(339, 93);
            this.btnBuild.Name = "btnBuild";
            this.btnBuild.Size = new System.Drawing.Size(76, 26);
            this.btnBuild.TabIndex = 8;
            this.btnBuild.Text = "Build";
            this.btnBuild.UseVisualStyleBackColor = false;
            this.btnBuild.Click += new System.EventHandler(this.btnBuild_Click);
            // 
            // grpPreview
            // 
            this.grpPreview.Controls.Add(this.lblBin);
            this.grpPreview.Controls.Add(this.txtBin);
            this.grpPreview.Controls.Add(this.btnBrowseBin);
            this.grpPreview.Controls.Add(this.lblChar);
            this.grpPreview.Controls.Add(this.txtChar);
            this.grpPreview.Controls.Add(this.panelPreview);
            this.grpPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.grpPreview.Location = new System.Drawing.Point(10, 151);
            this.grpPreview.Name = "grpPreview";
            this.grpPreview.Padding = new System.Windows.Forms.Padding(8);
            this.grpPreview.Size = new System.Drawing.Size(430, 270);
            this.grpPreview.TabIndex = 1;
            this.grpPreview.TabStop = false;
            this.grpPreview.Text = "Preview";
            // 
            // lblBin
            // 
            this.lblBin.AutoSize = true;
            this.lblBin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblBin.Location = new System.Drawing.Point(10, 30);
            this.lblBin.Name = "lblBin";
            this.lblBin.Size = new System.Drawing.Size(70, 14);
            this.lblBin.TabIndex = 0;
            this.lblBin.Text = ".bin File";
            // 
            // txtBin
            // 
            this.txtBin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtBin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtBin.Location = new System.Drawing.Point(100, 26);
            this.txtBin.Name = "txtBin";
            this.txtBin.ReadOnly = true;
            this.txtBin.Size = new System.Drawing.Size(233, 22);
            this.txtBin.TabIndex = 1;
            // 
            // btnBrowseBin
            // 
            this.btnBrowseBin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(64)))));
            this.btnBrowseBin.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(84)))));
            this.btnBrowseBin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseBin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnBrowseBin.Location = new System.Drawing.Point(339, 25);
            this.btnBrowseBin.Name = "btnBrowseBin";
            this.btnBrowseBin.Size = new System.Drawing.Size(76, 23);
            this.btnBrowseBin.TabIndex = 2;
            this.btnBrowseBin.Text = "Browse";
            this.btnBrowseBin.UseVisualStyleBackColor = false;
            this.btnBrowseBin.Click += new System.EventHandler(this.btnBrowseBin_Click);
            // 
            // lblChar
            // 
            this.lblChar.AutoSize = true;
            this.lblChar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.lblChar.Location = new System.Drawing.Point(10, 64);
            this.lblChar.Name = "lblChar";
            this.lblChar.Size = new System.Drawing.Size(70, 14);
            this.lblChar.TabIndex = 3;
            this.lblChar.Text = "Character";
            // 
            // txtChar
            // 
            this.txtChar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtChar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtChar.Font = new System.Drawing.Font("Consolas", 14F);
            this.txtChar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtChar.Location = new System.Drawing.Point(100, 58);
            this.txtChar.MaxLength = 1;
            this.txtChar.Name = "txtChar";
            this.txtChar.Size = new System.Drawing.Size(41, 29);
            this.txtChar.TabIndex = 4;
            this.txtChar.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtChar.TextChanged += new System.EventHandler(this.txtChar_TextChanged);
            // 
            // panelPreview
            // 
            this.panelPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.panelPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPreview.Location = new System.Drawing.Point(10, 100);
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Size = new System.Drawing.Size(405, 155);
            this.panelPreview.TabIndex = 5;
            this.panelPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint);
            // 
            // FormFontNotifyBuilder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(450, 430);
            this.Controls.Add(this.grpGenerate);
            this.Controls.Add(this.grpPreview);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormFontNotifyBuilder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Notify Font Builder";
            this.grpGenerate.ResumeLayout(false);
            this.grpGenerate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSize)).EndInit();
            this.grpPreview.ResumeLayout(false);
            this.grpPreview.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.GroupBox grpGenerate;
        private System.Windows.Forms.Label lblTtf;
        private System.Windows.Forms.TextBox txtTtf;
        private System.Windows.Forms.Button btnBrowseTtf;
        private System.Windows.Forms.Label lblHanzi;
        private System.Windows.Forms.TextBox txtHanzi;
        private System.Windows.Forms.Button btnBrowseHanzi;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.NumericUpDown nudSize;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Label lblBin;
        private System.Windows.Forms.TextBox txtBin;
        private System.Windows.Forms.Button btnBrowseBin;
        private System.Windows.Forms.Label lblChar;
        private System.Windows.Forms.TextBox txtChar;
        private System.Windows.Forms.Panel panelPreview;
    }
}
