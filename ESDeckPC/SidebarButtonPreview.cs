using System.Drawing;
using System.Drawing.Drawing2D;

namespace ESDeckPC
{
    /// <summary>
    /// Draws a mockup of the ESP32 firmware's deck/monitor sidebar page
    /// button (main/ui/ui_deck.c, main/ui/ui_monitor.c -- 64x56 lv_btn,
    /// bg 0x2a2a2a, radius 8, clip_corner enabled). Shared by every editor
    /// UI that lets the user pick a side_icon (deck's FormConfigEditor now,
    /// monitor's UC_PageSettings/UC_ClockSettings later) so the preview
    /// always matches what the real firmware button looks like.
    /// </summary>
    public static class SidebarButtonPreview
    {
        public const int ButtonW = 64;
        public const int ButtonH = 56;
        private const int RadiusPx = 8;

        public static readonly Color BgColor = Color.FromArgb(0x2a, 0x2a, 0x2a);
        public static readonly Color TextColor = Color.FromArgb(0xcc, 0xcc, 0xcc);

        /// <summary>
        /// Draws the mock button into the given bounds (any size -- scaled
        /// uniformly from the real 64x56, so pass e.g. a 128x112 box for a
        /// 2x preview). If icon is non-null it's drawn at native pixel size
        /// scaled by the same factor and centered -- exactly like
        /// lv_obj_center() on the ESP side, NOT stretched to fill, so an
        /// icon that isn't already cropped to 64x56 will show clipped or
        /// gapped here too, matching the real device. If icon is null,
        /// fallbackText is centered instead (the page/cell name shown on
        /// the real button when no side_icon is set).
        /// </summary>
        public static void Draw(Graphics g, Rectangle bounds, Bitmap icon, string fallbackText)
        {
            float scale = (float)bounds.Width / ButtonW;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using (var path = RoundedRect(bounds, RadiusPx * scale))
            {
                Region oldClip = g.Clip;
                g.SetClip(path, CombineMode.Replace);

                using (var brush = new SolidBrush(BgColor))
                    g.FillRectangle(brush, bounds);

                if (icon != null)
                {
                    int w = (int)(icon.Width * scale);
                    int h = (int)(icon.Height * scale);
                    int x = bounds.X + (bounds.Width - w) / 2;
                    int y = bounds.Y + (bounds.Height - h) / 2;
                    g.DrawImage(icon, x, y, w, h);
                }
                else if (!string.IsNullOrEmpty(fallbackText))
                {
                    using (var font = new Font("Segoe UI", 7f * scale, FontStyle.Regular))
                    using (var brush = new SolidBrush(TextColor))
                    using (var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                    })
                    {
                        g.DrawString(fallbackText, font, brush, bounds, sf);
                    }
                }

                g.Clip = oldClip;
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
