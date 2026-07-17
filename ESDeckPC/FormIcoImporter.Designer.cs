namespace ESDeckPC
{
    partial class FormIcoImporter
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _source?.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.rbFill = new System.Windows.Forms.RadioButton();
            this.rbFit = new System.Windows.Forms.RadioButton();
            this.lblFile = new System.Windows.Forms.Label();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.lblHint = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnLoad.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnLoad.Location = new System.Drawing.Point(12, 10);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(90, 26);
            this.btnLoad.TabIndex = 0;
            this.btnLoad.Text = "Load Image";
            this.btnLoad.UseVisualStyleBackColor = false;
            this.btnLoad.Click += new System.EventHandler(this.BtnLoad_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnSave.Enabled = false;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSave.Location = new System.Drawing.Point(108, 10);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 26);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save PNG";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // rbFill
            // 
            this.rbFill.AutoSize = true;
            this.rbFill.Checked = true;
            this.rbFill.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.rbFill.Location = new System.Drawing.Point(24, 95);
            this.rbFill.Name = "rbFill";
            this.rbFill.Size = new System.Drawing.Size(53, 18);
            this.rbFill.TabIndex = 2;
            this.rbFill.TabStop = true;
            this.rbFill.Text = "Fill";
            this.rbFill.UseVisualStyleBackColor = true;
            this.rbFill.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            // 
            // rbFit
            // 
            this.rbFit.AutoSize = true;
            this.rbFit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.rbFit.Location = new System.Drawing.Point(24, 130);
            this.rbFit.Name = "rbFit";
            this.rbFit.Size = new System.Drawing.Size(46, 18);
            this.rbFit.TabIndex = 3;
            this.rbFit.Text = "Fit";
            this.rbFit.UseVisualStyleBackColor = true;
            this.rbFit.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            // 
            // lblFile
            // 
            this.lblFile.AutoEllipsis = true;
            this.lblFile.ForeColor = System.Drawing.Color.Gray;
            this.lblFile.Location = new System.Drawing.Point(12, 49);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(206, 18);
            this.lblFile.TabIndex = 4;
            this.lblFile.Text = "(no image loaded)";
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(18)))));
            this.canvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvas.Cursor = System.Windows.Forms.Cursors.Hand;
            this.canvas.Location = new System.Drawing.Point(98, 72);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(100, 100);
            this.canvas.TabIndex = 5;
            this.canvas.TabStop = false;
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.Canvas_Paint);
            this.canvas.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseDown);
            this.canvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseMove);
            this.canvas.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseUp);
            this.canvas.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseWheel);
            // 
            // lblHint
            // 
            this.lblHint.ForeColor = System.Drawing.Color.Gray;
            this.lblHint.Location = new System.Drawing.Point(12, 182);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(186, 32);
            this.lblHint.TabIndex = 6;
            this.lblHint.Text = "Drag/arrows to pan, scroll to zoom\r\nOutput: 100 x 100 PNG";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FormIcoImporter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ClientSize = new System.Drawing.Size(214, 226);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.lblFile);
            this.Controls.Add(this.rbFit);
            this.Controls.Add(this.rbFill);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnLoad);
            this.Font = new System.Drawing.Font("Consolas", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormIcoImporter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Icon Importer";
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.RadioButton rbFill;
        private System.Windows.Forms.RadioButton rbFit;
        private System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.PictureBox canvas;
        private System.Windows.Forms.Label lblHint;
    }
}
