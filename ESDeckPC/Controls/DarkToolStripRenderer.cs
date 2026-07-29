using System.Drawing;
using System.Windows.Forms;

namespace ESDeckPC
{
    // ProfessionalColorTable has no arrow-color property, so
    // ToolStripProfessionalRenderer.OnRenderArrow falls back to its own
    // dark/system color regardless of DarkColorTable -- that's why the
    // submenu arrow (e.g. next to "Font") renders near-black against our
    // dark background while every other color follows the theme. Overriding
    // OnRenderArrow here is the only hook that actually controls it.
    public class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color ArrowColor = Color.FromArgb(220, 220, 220);

        public DarkToolStripRenderer(ProfessionalColorTable table) : base(table) { }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = ArrowColor;
            base.OnRenderArrow(e);
        }
    }
}
