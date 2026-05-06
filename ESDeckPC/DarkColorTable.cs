using System.Drawing;
using System.Windows.Forms;

namespace ESDeckPC
{
    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientMiddle => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color MenuStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color MenuStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color ButtonSelectedHighlight => Color.FromArgb(62, 62, 66);
        public override Color ButtonPressedHighlight => Color.FromArgb(80, 80, 80);
        public override Color ButtonCheckedHighlight => Color.FromArgb(62, 62, 66);
        public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
    }
}