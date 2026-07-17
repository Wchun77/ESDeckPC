namespace ESDeckPC
{
    partial class UC_PageSettings
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.grpPageBg = new System.Windows.Forms.GroupBox();
            this.txtPageBgImage = new System.Windows.Forms.TextBox();
            this.btnPageBgBrowse = new System.Windows.Forms.Button();
            this.btnPageBgClear = new System.Windows.Forms.Button();
            this.grpCells = new System.Windows.Forms.GroupBox();
            this.cmbCell3 = new System.Windows.Forms.ComboBox();
            this.cmbCell2 = new System.Windows.Forms.ComboBox();
            this.cmbCell1 = new System.Windows.Forms.ComboBox();
            this.cmbCell0 = new System.Windows.Forms.ComboBox();
            this.txtPageName = new System.Windows.Forms.TextBox();
            this.grpPageBg.SuspendLayout();
            this.grpCells.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpPageBg
            // 
            this.grpPageBg.Controls.Add(this.txtPageBgImage);
            this.grpPageBg.Controls.Add(this.btnPageBgBrowse);
            this.grpPageBg.Controls.Add(this.btnPageBgClear);
            this.grpPageBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpPageBg.Location = new System.Drawing.Point(12, 175);
            this.grpPageBg.Name = "grpPageBg";
            this.grpPageBg.Size = new System.Drawing.Size(234, 86);
            this.grpPageBg.TabIndex = 10;
            this.grpPageBg.TabStop = false;
            this.grpPageBg.Text = "Background image";
            // 
            // txtPageBgImage
            // 
            this.txtPageBgImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtPageBgImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtPageBgImage.Location = new System.Drawing.Point(8, 20);
            this.txtPageBgImage.Name = "txtPageBgImage";
            this.txtPageBgImage.ReadOnly = true;
            this.txtPageBgImage.Size = new System.Drawing.Size(218, 22);
            this.txtPageBgImage.TabIndex = 0;
            // 
            // btnPageBgBrowse
            // 
            this.btnPageBgBrowse.Location = new System.Drawing.Point(8, 49);
            this.btnPageBgBrowse.Name = "btnPageBgBrowse";
            this.btnPageBgBrowse.Size = new System.Drawing.Size(80, 24);
            this.btnPageBgBrowse.TabIndex = 1;
            this.btnPageBgBrowse.Text = "Browse...";
            // 
            // btnPageBgClear
            // 
            this.btnPageBgClear.Location = new System.Drawing.Point(96, 49);
            this.btnPageBgClear.Name = "btnPageBgClear";
            this.btnPageBgClear.Size = new System.Drawing.Size(60, 24);
            this.btnPageBgClear.TabIndex = 2;
            this.btnPageBgClear.Text = "Clear";
            // 
            // grpCells
            // 
            this.grpCells.Controls.Add(this.cmbCell3);
            this.grpCells.Controls.Add(this.cmbCell2);
            this.grpCells.Controls.Add(this.cmbCell1);
            this.grpCells.Controls.Add(this.cmbCell0);
            this.grpCells.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(210)))));
            this.grpCells.Location = new System.Drawing.Point(12, 58);
            this.grpCells.Name = "grpCells";
            this.grpCells.Size = new System.Drawing.Size(234, 111);
            this.grpCells.TabIndex = 11;
            this.grpCells.TabStop = false;
            this.grpCells.Text = "Background image";
            // 
            // cmbCell3
            // 
            this.cmbCell3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbCell3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCell3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCell3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cmbCell3.Items.AddRange(new object[] {
            "launch",
            "hotkey",
            "media",
            "discord",
            "scroll",
            "sequence",
            "text"});
            this.cmbCell3.Location = new System.Drawing.Point(119, 71);
            this.cmbCell3.Name = "cmbCell3";
            this.cmbCell3.Size = new System.Drawing.Size(107, 22);
            this.cmbCell3.TabIndex = 6;
            // 
            // cmbCell2
            // 
            this.cmbCell2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbCell2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCell2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCell2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cmbCell2.Items.AddRange(new object[] {
            "launch",
            "hotkey",
            "media",
            "discord",
            "scroll",
            "sequence",
            "text"});
            this.cmbCell2.Location = new System.Drawing.Point(8, 71);
            this.cmbCell2.Name = "cmbCell2";
            this.cmbCell2.Size = new System.Drawing.Size(107, 22);
            this.cmbCell2.TabIndex = 5;
            // 
            // cmbCell1
            // 
            this.cmbCell1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbCell1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCell1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCell1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cmbCell1.Items.AddRange(new object[] {
            "launch",
            "hotkey",
            "media",
            "discord",
            "scroll",
            "sequence",
            "text"});
            this.cmbCell1.Location = new System.Drawing.Point(119, 31);
            this.cmbCell1.Name = "cmbCell1";
            this.cmbCell1.Size = new System.Drawing.Size(107, 22);
            this.cmbCell1.TabIndex = 4;
            // 
            // cmbCell0
            // 
            this.cmbCell0.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.cmbCell0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCell0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCell0.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.cmbCell0.Items.AddRange(new object[] {
            "launch",
            "hotkey",
            "media",
            "discord",
            "scroll",
            "sequence",
            "text"});
            this.cmbCell0.Location = new System.Drawing.Point(8, 31);
            this.cmbCell0.Name = "cmbCell0";
            this.cmbCell0.Size = new System.Drawing.Size(107, 22);
            this.cmbCell0.TabIndex = 3;
            // 
            // txtPageName
            // 
            this.txtPageName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.txtPageName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtPageName.Location = new System.Drawing.Point(12, 15);
            this.txtPageName.Name = "txtPageName";
            this.txtPageName.ReadOnly = true;
            this.txtPageName.Size = new System.Drawing.Size(234, 22);
            this.txtPageName.TabIndex = 12;
            // 
            // UC_PageSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.Controls.Add(this.txtPageName);
            this.Controls.Add(this.grpCells);
            this.Controls.Add(this.grpPageBg);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_PageSettings";
            this.Size = new System.Drawing.Size(265, 538);
            this.grpPageBg.ResumeLayout(false);
            this.grpPageBg.PerformLayout();
            this.grpCells.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpPageBg;
        private System.Windows.Forms.TextBox txtPageBgImage;
        private System.Windows.Forms.Button btnPageBgBrowse;
        private System.Windows.Forms.Button btnPageBgClear;
        private System.Windows.Forms.GroupBox grpCells;
        private System.Windows.Forms.TextBox txtPageName;
        private System.Windows.Forms.ComboBox cmbCell3;
        private System.Windows.Forms.ComboBox cmbCell2;
        private System.Windows.Forms.ComboBox cmbCell1;
        private System.Windows.Forms.ComboBox cmbCell0;
    }
}
