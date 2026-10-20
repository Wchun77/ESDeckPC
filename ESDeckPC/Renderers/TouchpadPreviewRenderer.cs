using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ESDeckPC
{
    /// <summary>
    /// Renders a preview bitmap of the Touchpad mode content area, mirroring
    /// ui_touchpad.c (build_page / build_bg): Left/Right buttons across the
    /// top, touchpad surface below. With a bg image, the image is cover-fit
    /// under a 50% black mask and the surfaces go translucent, same values as
    /// the firmware (OPA_BTN_ON_BG / OPA_SURFACE_ON_BG).
    /// </summary>
    public static class TouchpadPreviewRenderer
    {
        // Content area: 800x480 screen minus 80px sidebar (ui.h SCREEN_W/H, SIDEBAR_W)
        private const int ContentW = 720;
        private const int ContentH = 480;

        // ui_touchpad.c layout constants
        private const int Margin = 12;                              // TP_MARGIN
        private const int BtnBarH = 100;                            // TP_BTN_BAR_H
        private const int BtnH = BtnBarH - Margin * 2;              // TP_BTN_H
        private const int BtnW = (ContentW - Margin * 3) / 2;       // TP_BTN_W
        private const int PadX = Margin;                            // TP_PAD_X
        private const int PadY = BtnBarH;                           // TP_PAD_Y
        private const int PadW = ContentW - Margin * 2;             // TP_PAD_W
        private const int PadH = ContentH - BtnBarH - Margin;       // TP_PAD_H
        private const int Radius = 12;

        private static readonly Color ColPageBg = Color.FromArgb(0x22, 0x22, 0x22); // COL_PAGE_BG
        private static readonly Color ColSurface = Color.FromArgb(0x1a, 0x1a, 0x1a); // COL_SURFACE
        private static readonly Color ColBtn = Color.FromArgb(0x2a, 0x2a, 0x2a);     // COL_BTN
        private static readonly Color ColBorder = Color.FromArgb(0x33, 0x33, 0x33);  // COL_BORDER
        private static readonly Color ColText = Color.FromArgb(0xcc, 0xcc, 0xcc);    // COL_TEXT
        private static readonly Color ColHint = Color.FromArgb(0x44, 0x44, 0x44);    // COL_HINT
        private static readonly Color ColHintOnBg = Color.FromArgb(0x77, 0x77, 0x77);

        // LV_OPA_70 / LV_OPA_50
        private const int OpaBtnOnBg = 178;
        private const int OpaSurfaceOnBg = 127;

        public static Bitmap Render(Bitmap bgBitmap)
        {
            var bmp = new Bitmap(ContentW, ContentH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(ColPageBg);

                bool hasBg = DrawBackground(g, bgBitmap);

                DrawButton(g, Margin, "Left", hasBg);
                DrawButton(g, Margin * 2 + BtnW, "Right", hasBg);
                DrawPad(g, hasBg);
            }
            return bmp;
        }

        /// <summary>Cover-fit zoom + 50% black mask; no-op without an image.</summary>
        private static bool DrawBackground(Graphics g, Bitmap bgBitmap)
        {
            if (bgBitmap == null) return false;

            float zoom = Math.Max((float)ContentW / bgBitmap.Width, (float)ContentH / bgBitmap.Height);
            int scaledW = (int)(bgBitmap.Width * zoom);
            int scaledH = (int)(bgBitmap.Height * zoom);
            g.DrawImage(bgBitmap, (ContentW - scaledW) / 2, (ContentH - scaledH) / 2, scaledW, scaledH);

            using (var mask = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                g.FillRectangle(mask, 0, 0, ContentW, ContentH);
            return true;
        }

        private static void DrawButton(Graphics g, int x, string text, bool hasBg)
        {
            var rect = new Rectangle(x, Margin, BtnW, BtnH);
            using (var brush = new SolidBrush(hasBg ? Color.FromArgb(OpaBtnOnBg, ColBtn) : ColBtn))
            using (var path = RoundedRect(rect, Radius))
                g.FillPath(brush, path);

            DrawCentered(g, text, rect, new Font("Segoe UI", 14f), ColText);
        }

        private static void DrawPad(Graphics g, bool hasBg)
        {
            var rect = new Rectangle(PadX, PadY, PadW, PadH);
            using (var path = RoundedRect(rect, Radius))
            {
                using (var brush = new SolidBrush(hasBg ? Color.FromArgb(OpaSurfaceOnBg, ColSurface) : ColSurface))
                    g.FillPath(brush, path);
                using (var pen = new Pen(ColBorder, 1f))
                    g.DrawPath(pen, path);
            }

            DrawCentered(g, "Touchpad", rect, new Font("Segoe UI", 14f), hasBg ? ColHintOnBg : ColHint);
        }

        private static void DrawCentered(Graphics g, string text, Rectangle rect, Font font, Color color)
        {
            using (font)
            using (var brush = new SolidBrush(color))
            {
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, brush,
                    rect.X + (rect.Width - size.Width) / 2f,
                    rect.Y + (rect.Height - size.Height) / 2f);
            }
        }

        /// <summary>
        /// Placeholder for editor entries that have no content-area preview
        /// (Sidebar -- the 720x480 preview box only covers the content area).
        /// </summary>
        public static Bitmap RenderNoPreview(string text = "No preview")
        {
            var bmp = new Bitmap(ContentW, ContentH);
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font("Segoe UI", 14f))
            using (var brush = new SolidBrush(ColHint))
            using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.FromArgb(18, 18, 18));
                g.DrawString(text, font, brush, new RectangleF(0, 0, ContentW, ContentH), fmt);
            }
            return bmp;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
