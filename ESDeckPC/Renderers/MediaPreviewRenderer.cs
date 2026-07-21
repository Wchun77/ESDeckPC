using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ESDeckPC
{
    /// <summary>
    /// Renders a preview bitmap for the Media player card, mirroring the
    /// firmware layout in ui_media.c (build_player_card / build_bg). Cover,
    /// title/artist, progress bar and transport buttons are all placeholders
    /// -- live Now Playing data is not available on the PC editor -- but
    /// every position matches the device exactly (same LV_ALIGN_TOP_MID
    /// offsets used in ui_media.c) so the preview is visually representative.
    /// </summary>
    public static class MediaPreviewRenderer
    {
        // Content area: 800x480 screen minus 80px sidebar (see ui.h SCREEN_W/H, SIDEBAR_W)
        private const int ContentW = 720;
        private const int ContentH = 480;
        private const int CenterX = ContentW / 2;

        private static readonly Color ColCoverBg = Color.FromArgb(60, 60, 60);
        private static readonly Color ColText = Color.FromArgb(220, 220, 220);
        private static readonly Color ColSubText = Color.FromArgb(150, 150, 150);
        private static readonly Color ColTrack = Color.FromArgb(0x33, 0x33, 0x33);
        private static readonly Color ColBtnBg = Color.FromArgb(45, 45, 48);

        /// <summary>
        /// Renders the full player card mockup (background + placeholder
        /// cover/title/artist/progress/buttons) -- same "no real data yet"
        /// disabled look ui_media.c shows before any HID data arrives.
        /// </summary>
        public static Bitmap Render(Bitmap bgBitmap)
        {
            var bmp = new Bitmap(ContentW, ContentH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.FromArgb(0x22, 0x22, 0x22)); // s_page's flat fallback color

                DrawBackground(g, bgBitmap);
                DrawCover(g);
                DrawTitleArtist(g);
                DrawProgress(g);
                DrawButtons(g);
            }
            return bmp;
        }

        /// <summary>
        /// Cover-fit zoom + 50% black mask, only when a bg image is set --
        /// build_bg() in ui_media.c early-returns (no mask object at all)
        /// when bg_image is empty, leaving the flat clear color showing as-is.
        /// </summary>
        private static void DrawBackground(Graphics g, Bitmap bgBitmap)
        {
            if (bgBitmap == null) return;

            float zoomX = (float)ContentW / bgBitmap.Width;
            float zoomY = (float)ContentH / bgBitmap.Height;
            float zoom = Math.Max(zoomX, zoomY);
            int scaledW = (int)(bgBitmap.Width * zoom);
            int scaledH = (int)(bgBitmap.Height * zoom);
            int offsetX = (ContentW - scaledW) / 2;
            int offsetY = (ContentH - scaledH) / 2;
            g.DrawImage(bgBitmap, offsetX, offsetY, scaledW, scaledH);

            using (var maskBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                g.FillRectangle(maskBrush, 0, 0, ContentW, ContentH);
        }

        // 220x220, LV_ALIGN_TOP_MID offset (0, 36) -> top=36, center x=CenterX
        private static void DrawCover(Graphics g)
        {
            var rect = new Rectangle(CenterX - 110, 36, 220, 220);
            using (var brush = new SolidBrush(ColCoverBg))
            using (var path = RoundedRect(rect, 8))
                g.FillPath(brush, path);

            using (var font = new Font("Segoe UI", 32f))
            using (var brush = new SolidBrush(ColSubText))
            {
                const string note = "♪"; // no-cover-yet placeholder glyph
                var size = g.MeasureString(note, font);
                g.DrawString(note, font, brush,
                    rect.X + (rect.Width - size.Width) / 2f,
                    rect.Y + (rect.Height - size.Height) / 2f);
            }
        }

        // title: TOP_MID offset (0, 274); artist: TOP_MID offset (0, 308)
        private static void DrawTitleArtist(Graphics g)
        {
            DrawCenteredAt(g, "None", CenterX, 274, new Font("Segoe UI", 14f, FontStyle.Bold), ColText);
            DrawCenteredAt(g, "None", CenterX, 308, new Font("Segoe UI", 10.5f), ColSubText);
        }

        // bar: 480x8, TOP_MID offset (0, 356); labels: TOP_MID offset (+-240, 384)
        private static void DrawProgress(Graphics g)
        {
            int barX = CenterX - 240, barY = 356, barW = 480, barH = 8;
            using (var trackBrush = new SolidBrush(ColTrack))
                g.FillRoundedRect(trackBrush, barX, barY, barW, barH, 4);

            DrawCenteredAt(g, "-:--", CenterX - 240, 384, new Font("Segoe UI", 9f), ColSubText);
            DrawCenteredAt(g, "-:--", CenterX + 240, 384, new Font("Segoe UI", 9f), ColSubText);
        }

        // prev: 56x56 TOP_MID(-76,400); play: 64x64 TOP_MID(0,396); next: 56x56 TOP_MID(76,400)
        // Icons are hand-drawn polygons rather than font glyphs -- Segoe UI's
        // media-control glyphs (▶/⏮/⏭) measure with asymmetric side bearing,
        // so centering them via MeasureString reliably looks off-center
        // (the reported "play button looks crooked" issue). Polygons give
        // exact, font-independent centering.
        private static void DrawButtons(Graphics g)
        {
            DrawCircleBg(g, CenterX - 76, 400, 56);
            DrawSkipIcon(g, CenterX - 76, 400, 56, pointingRight: false);

            DrawCircleBg(g, CenterX, 396, 64);
            DrawPlayTriangle(g, CenterX, 396 + 32, 64);

            DrawCircleBg(g, CenterX + 76, 400, 56);
            DrawSkipIcon(g, CenterX + 76, 400, 56, pointingRight: true);
        }

        private static void DrawCircleBg(Graphics g, int centerX, int top, int size)
        {
            var rect = new Rectangle(centerX - size / 2, top, size, size);
            using (var brush = new SolidBrush(ColBtnBg))
                g.FillEllipse(brush, rect);
        }

        /// <summary>
        /// Solid right-pointing triangle, centered at (centerX, centerY).
        /// Sized to approximate ui_media.c's actual icon, not the 64px
        /// button -- s_play_icon_lbl is LV_SYMBOL_PLAY at lv_font_montserrat_24,
        /// a small glyph inside a big circle, not a triangle that fills it.
        /// </summary>
        private static void DrawPlayTriangle(Graphics g, int centerX, int centerY, int btnSize)
        {
            const float h = 9f; // half-height -- approximates a 24px glyph's ink height
            const float w = 8f;
            var pts = new[]
            {
                new PointF(centerX - w * 0.7f, centerY - h),
                new PointF(centerX - w * 0.7f, centerY + h),
                new PointF(centerX + w,        centerY),
            };
            using (var brush = new SolidBrush(ColText))
                g.FillPolygon(brush, pts);
        }

        /// <summary>
        /// Triangle + bar "skip" icon, centered at (centerX, top + btnSize/2).
        /// Sized to approximate LV_SYMBOL_PREV/NEXT at the firmware's default
        /// label font (lv_font_montserrat_14, since ui_media.c never overrides
        /// it for these two buttons) -- noticeably smaller than the play icon.
        /// </summary>
        private static void DrawSkipIcon(Graphics g, int centerX, int top, int btnSize, bool pointingRight)
        {
            int centerY = top + btnSize / 2;
            int dir = pointingRight ? 1 : -1;
            const float h = 5.5f;
            const float w = 5f;
            const float barW = 2.2f;

            // Triangle sits toward the leading edge, bar trails behind it.
            float triCenterX = centerX - dir * w * 0.55f;
            var tri = new[]
            {
                new PointF(triCenterX - dir * w * 0.6f, centerY - h),
                new PointF(triCenterX - dir * w * 0.6f, centerY + h),
                new PointF(triCenterX + dir * w * 0.7f,  centerY),
            };
            using (var brush = new SolidBrush(ColText))
            {
                g.FillPolygon(brush, tri);
                float barX = centerX + dir * w * 0.75f;
                g.FillRectangle(brush, barX - barW / 2f, centerY - h, barW, h * 2f);
            }
        }

        private static void DrawCenteredAt(Graphics g, string text, int centerX, int y, Font font, Color color)
        {
            using (font)
            using (var brush = new SolidBrush(color))
            {
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, brush, centerX - size.Width / 2f, y);
            }
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
