using System;
using System.Drawing;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class UC_DeckButton : UserControl
    {
        public event EventHandler EditClicked;

        private PcButton _button;
        private Color _originalBackColor;

        public UC_DeckButton()
        {
            InitializeComponent();
            btnEdit.Click += (s, e) => EditClicked?.Invoke(this, EventArgs.Empty);
            lblName.MouseUp += (s, e) => this.OnMouseUp(e);
            lblAction.MouseUp += (s, e) => this.OnMouseUp(e);

            _originalBackColor = this.BackColor;

            // trigger drag from label or the UC itself, but not from btnEdit
            lblName.MouseDown += UC_DeckButton_MouseDown;
            lblAction.MouseDown += UC_DeckButton_MouseDown;
            this.MouseDown += UC_DeckButton_MouseDown;
        }

        public void SetData(PcButton button)
        {
            _button = button;
            lblName.Text = string.IsNullOrEmpty(button?.Label) ? "—" : button.Label;
            lblAction.Text = string.IsNullOrEmpty(button?.Action) ? "—" : button.Action;
        }

        public PcButton Button => _button;

        private void UC_DeckButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ApplyDragVisual(true);
            this.DoDragDrop(this, DragDropEffects.Move);
            ApplyDragVisual(false);
        }

        public void ApplyDragVisual(bool apply)
        {
            this.BackColor = apply
                ? Color.FromArgb(80, 80, 90)
                : _originalBackColor;
            this.Invalidate();
        }
    }
}