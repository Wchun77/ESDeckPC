namespace ESDeckPC
{
    partial class FormCanvasWindow
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
            this.picSide = new System.Windows.Forms.PictureBox();
            this.panelPage = new System.Windows.Forms.Panel();
            this.lblMessage = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picSide)).BeginInit();
            this.panelPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // picSide
            // 
            this.picSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.picSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picSide.Location = new System.Drawing.Point(0, 0);
            this.picSide.Name = "picSide";
            this.picSide.Size = new System.Drawing.Size(80, 480);
            this.picSide.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSide.TabIndex = 0;
            this.picSide.TabStop = false;
            this.picSide.MouseClick += new System.Windows.Forms.MouseEventHandler(this.picSide_MouseClick);
            // 
            // panelPage
            // 
            this.panelPage.BackColor = System.Drawing.Color.Black;
            this.panelPage.Controls.Add(this.lblMessage);
            this.panelPage.Location = new System.Drawing.Point(80, 0);
            this.panelPage.Name = "panelPage";
            this.panelPage.Size = new System.Drawing.Size(720, 480);
            this.panelPage.TabIndex = 1;
            // 
            // lblMessage
            // 
            this.lblMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMessage.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.lblMessage.Location = new System.Drawing.Point(0, 0);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(720, 480);
            this.lblMessage.TabIndex = 0;
            this.lblMessage.Text = "Starting...";
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FormCanvasWindow
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 480);
            this.Controls.Add(this.panelPage);
            this.Controls.Add(this.picSide);
            this.MaximizeBox = false;
            this.Name = "FormCanvasWindow";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Canvas";
            ((System.ComponentModel.ISupportInitialize)(this.picSide)).EndInit();
            this.panelPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picSide;
        private System.Windows.Forms.Panel panelPage;
        private System.Windows.Forms.Label lblMessage;
    }
}
