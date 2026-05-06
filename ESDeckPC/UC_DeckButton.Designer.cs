namespace ESDeckPC
{
    partial class UC_DeckButton
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.lblAction = new System.Windows.Forms.Label();
            this.btnEdit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.lblName.Location = new System.Drawing.Point(4, 12);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(135, 18);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "—";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblAction
            // 
            this.lblAction.ForeColor = System.Drawing.Color.Gray;
            this.lblAction.Location = new System.Drawing.Point(4, 36);
            this.lblAction.Name = "lblAction";
            this.lblAction.Size = new System.Drawing.Size(135, 18);
            this.lblAction.TabIndex = 1;
            this.lblAction.Text = "—";
            this.lblAction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(22, 62);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(99, 26);
            this.btnEdit.TabIndex = 2;
            this.btnEdit.Text = "Edit";
            // 
            // UC_DeckButton
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblAction);
            this.Controls.Add(this.btnEdit);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UC_DeckButton";
            this.Size = new System.Drawing.Size(143, 100);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblAction;
        private System.Windows.Forms.Button btnEdit;
    }
}