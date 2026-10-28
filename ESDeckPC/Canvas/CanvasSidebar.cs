using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ESDeckPC
{
    /// <summary>
    /// The ESP's Canvas sidebar drawn on the PC (80 x 480): page icons and
    /// the gear, laid out like ui_canvas.c / ui.c. Used by the editor
    /// preview and the Canvas window.
    /// </summary>
    internal static class CanvasSidebar
    {
        public const int Width = 80, Height = 480;
        // buttons 64 x 64 at x 8, first at y 8, 80 px apart; gear at (8, 408);
        // selected = 3 px blue outline (#0055cc); background #111111
        public const int Btn = 64, X = 8, Y0 = 8, Step = 80, GearY = 408;
        public const int HitGear = -2, HitNone = -1;

        /// <summary>selected = highlighted page (-1 none); gearSelected = the
        /// gear shows as selected (Settings open).</summary>
        public static Bitmap Render(CanvasConfig cfg, int selected, bool gearSelected)
        {
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(0x11, 0x11, 0x11));
                var blue = Color.FromArgb(0x00, 0x55, 0xcc);
                int count = cfg?.Pages.Count ?? 0;
                for (int i = 0; i < count && i < CanvasConfig.MaxPages; i++)
                {
                    var r = new Rectangle(X, Y0 + i * Step, Btn, Btn);
                    Bitmap icon = null;
                    try { icon = CanvasEncoder.RenderIcon(cfg.Pages[i].IconPath, cfg.Pages[i].Name); } catch { }
                    using (var path = Rounded(r, 8))
                    {
                        g.SetClip(path);
                        if (icon != null) g.DrawImage(icon, r);
                        else using (var b = new SolidBrush(Color.FromArgb(0x2a, 0x2a, 0x2a))) g.FillRectangle(b, r);
                        g.ResetClip();
                        if (!gearSelected && i == selected)
                            using (var pen = new Pen(blue, 3)) g.DrawPath(pen, Rounded(Rectangle.Inflate(r, 2, 2), 9));
                    }
                    icon?.Dispose();
                }
                // gear: the config's side_icon, or the built-in gear glyph
                var gr = new Rectangle(X, GearY, Btn, Btn);
                Bitmap gearImg = null;
                try { gearImg = CanvasEncoder.RenderIconFile(CanvasPaths.Resolve(cfg?.Settings?.SideIcon)); } catch { }
                using (var path = Rounded(gr, 8))
                {
                    bool glyphSel = gearSelected && gearImg == null;
                    using (var b = new SolidBrush(glyphSel ? blue : Color.FromArgb(0x2a, 0x2a, 0x2a))) g.FillPath(b, path);
                    if (gearImg != null)
                    {
                        g.SetClip(path); g.DrawImage(gearImg, gr); g.ResetClip();
                        if (gearSelected) using (var pen = new Pen(blue, 3)) g.DrawPath(pen, Rounded(Rectangle.Inflate(gr, 2, 2), 9));
                    }
                    else
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        using (var f = new Font("Segoe UI Symbol", 20f, FontStyle.Regular, GraphicsUnit.Pixel))
                        using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            g.DrawString("⚙", f, Brushes.White, gr, fmt);
                    }
                }
                gearImg?.Dispose();
            }
            return bmp;
        }

        /// <summary>Page index at sidebar y (0..479), HitGear, or HitNone.</summary>
        public static int HitTest(int y, int pageCount)
        {
            if (y >= GearY - 4) return HitGear;
            int i = (y - Y0 + (Step - Btn) / 2) / Step;
            return i >= 0 && i < pageCount && i < CanvasConfig.MaxPages ? i : HitNone;
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
        {
            var g = new GraphicsPath();
            int d = rad * 2;
            g.AddArc(r.X, r.Y, d, d, 180, 90);
            g.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            g.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            g.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            g.CloseFigure();
            return g;
        }
    }
}
