using System.Drawing;
using System.Windows.Forms;

namespace ESDeckPC
{
    public class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color BgDark = Color.FromArgb(45, 45, 48);
        private static readonly Color BgDarker = Color.FromArgb(30, 30, 30);
        private static readonly Color Hover = Color.FromArgb(62, 62, 66);
        private static readonly Color Pressed = Color.FromArgb(80, 80, 80);
        private static readonly Color Border = Color.FromArgb(80, 80, 80);
        private static readonly Color SepDark = Color.FromArgb(80, 80, 80);
        private static readonly Color SepLight = Color.FromArgb(60, 60, 60);

        // ToolStrip bar background
        public override Color ToolStripGradientBegin => BgDark;
        public override Color ToolStripGradientMiddle => BgDark;
        public override Color ToolStripGradientEnd => BgDark;

        // MenuStrip bar background
        public override Color MenuStripGradientBegin => BgDark;
        public override Color MenuStripGradientEnd => BgDark;

        // Drop-down panel background
        public override Color ToolStripDropDownBackground => BgDarker;
        public override Color ImageMarginGradientBegin => BgDarker;
        public override Color ImageMarginGradientMiddle => BgDarker;
        public override Color ImageMarginGradientEnd => BgDarker;

        // Drop-down border
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;

        // Menu item hover/selected
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;

        // Menu item pressed
        public override Color MenuItemPressedGradientBegin => Pressed;
        public override Color MenuItemPressedGradientMiddle => Pressed;
        public override Color MenuItemPressedGradientEnd => Pressed;

        // Button states
        public override Color ButtonSelectedHighlight => Hover;
        public override Color ButtonPressedHighlight => Pressed;
        public override Color ButtonCheckedHighlight => Hover;
        public override Color ButtonSelectedBorder => Border;

        // Separators
        public override Color SeparatorDark => SepDark;
        public override Color SeparatorLight => SepLight;
    }
}