using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ESDeckPC
{
    /// <summary>
    /// Renders a 720x480 clock preview using LVGL-compatible glyph layout.
    ///
    /// Panel geometry mirrors ui_clock_widget.c:
    ///   DATE_PANEL : x=8,  y=16, w=200, h=88
    ///   TIME_PANEL : x=0,  y=(480-280)/2=100, w=720, h=280
    ///   SEC_PANEL  : x=596, y=408, w=124, h=60
    ///
    /// LVGL glyph positioning:
    ///   draw_x = pen_x + ofs_x
    ///   draw_y = baseline_y - ofs_y - box_h    (Y axis is down in screen coords)
    ///   pen advances by adv_w after each glyph
    ///
    /// LVGL CENTER alignment:
    ///   total advance width -> horizontal centering
    ///   max(ofs_y + box_h) across all glyphs = block height -> vertical centering
    ///   baseline_y = panel_top + (panel_h - block_h) / 2 + block_h
    ///
    /// LVGL BOTTOM_LEFT alignment:
    ///   baseline_y = panel_bottom  (bottom of bbox of tallest glyph at baseline)
    /// </summary>
    public static class MonitorClockRenderer
    {
        private const int CW = 720;
        private const int CH = 480;

        private const int DATE_X = 8, DATE_Y = 16, DATE_W = 200, DATE_H = 88;
        private const int TIME_X = 0, TIME_W = 720, TIME_H = 280;
        private const int SEC_W = 124, SEC_H = 60;
        private const int CLK_SEP_LEN = 160;

        // ------------------------------------------------------------------
        // Public entry point
        // ------------------------------------------------------------------

        public static Bitmap Render(MonitorClockCfg cfg,
                                    FontBinLoader fontTime,
                                    FontBinLoader fontSec,
                                    FontBinLoader fontDate,
                                    Bitmap background)
        {
            var bmp = new Bitmap(CW, CH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.None;

                if (background != null)
                    DrawBackground(g, background);
                else
                    g.Clear(Color.FromArgb(0x22, 0x22, 0x22));

                // Semi-transparent overlay (LV_OPA_50)
                using (var br = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                    g.FillRectangle(br, 0, 0, CW, CH);

                Color colTime = ApplyOpacity(ParseHex(cfg?.ColTime, 0xF0F2FF), cfg?.OpaTime ?? 255);
                Color colColon = ApplyOpacity(ParseHex(cfg?.ColTime, 0xF0F2FF), cfg?.OpaColon ?? 255);
                Color colDate = ApplyOpacity(ParseHex(cfg?.ColDate, 0xF0F2FF), cfg?.OpaDate ?? 255);
                Color colDay = ApplyOpacity(ParseHex(cfg?.ColDay, 0xF0F2FF), cfg?.OpaDay ?? 255);
                Color colSec = ApplyOpacity(ParseHex(cfg?.ColSec, 0xF0F2FF), cfg?.OpaSec ?? 255);
                Color colSep = ParseHex(cfg?.SepColor, 0xF0F2FF);
                int sepWidth = cfg?.SepWidth ?? 1;
                int colonGap = cfg?.ColonGap ?? 30;

                int timeY = (CH - TIME_H) / 2;
                int secX = CW - SEC_W - 4;
                int secY = CH - SEC_H - 12;

                DrawDatePanel(g, fontDate, colDate, colDay, colSep, sepWidth);
                DrawTimePanel(g, fontTime, colTime, colColon, timeY, colonGap);
                DrawSecPanel(g, fontSec, colSec, secX, secY);
            }
            return bmp;
        }

        // ------------------------------------------------------------------
        // Panel renderers
        // ------------------------------------------------------------------

        private static void DrawDatePanel(Graphics g,
                                          FontBinLoader font,
                                          Color colDate, Color colDay,
                                          Color colSep, int sepWidth)
        {
            // date_label: LV_ALIGN_TOP_LEFT
            DrawTextTopLeft(g, "00/00", font, colDate,
                            DATE_X, DATE_Y, DATE_W, DATE_H);

            // day_label: LV_ALIGN_BOTTOM_LEFT
            DrawTextBottomLeft(g, "XXX", font, colDay,
                               DATE_X, DATE_Y, DATE_W, DATE_H);

            // separator line at bottom of date panel
            if (sepWidth > 0)
            {
                int sepY = DATE_Y + DATE_H;
                using (var pen = new Pen(colSep, sepWidth))
                    g.DrawLine(pen, DATE_X, sepY, DATE_X + CLK_SEP_LEN, sepY);
            }
        }

        private static void DrawTimePanel(Graphics g,
                                          FontBinLoader font,
                                          Color colDigits, Color colColon,
                                          int panelY, int colonGap)
        {
            // Mirror ESP layout: 4 equal digit cells + colon fixed at center
            // [ h_tens | h_units |gap| : |gap| m_tens | m_units ]
            // <-180px->|<--150px->|  |   |  |<-150px->|<-180px->
            const int DIGIT_W = TIME_W / 4;  // 180px

            // h_tens: centered in full cell
            DrawSingleGlyph(g, '0', font, colDigits, TIME_X, panelY, DIGIT_W, TIME_H, 0);
            // h_units: right-aligned with colonGap margin on right
            DrawSingleGlyph(g, '0', font, colDigits, TIME_X + DIGIT_W, panelY, DIGIT_W, TIME_H, -colonGap);
            // colon: centered on full panel
            DrawSingleGlyph(g, ':', font, colColon, TIME_X, panelY, TIME_W, TIME_H, 0);
            // m_tens: left-aligned with colonGap margin on left
            DrawSingleGlyph(g, '0', font, colDigits, TIME_X + DIGIT_W * 2, panelY, DIGIT_W, TIME_H, +colonGap);
            // m_units: centered in full cell
            DrawSingleGlyph(g, '0', font, colDigits, TIME_X + DIGIT_W * 3, panelY, DIGIT_W, TIME_H, 0);
        }

        private static void DrawSecPanel(Graphics g,
                                         FontBinLoader font,
                                         Color col,
                                         int panelX, int panelY)
        {
            DrawTextCenter(g, "00", font, col,
                           panelX, panelY, SEC_W, SEC_H);
        }

        // ------------------------------------------------------------------
        // Single glyph centered in a cell (mirrors LV_TEXT_ALIGN_CENTER in fixed-width label)
        // penOffset: additional x offset from cell left (for cells with left padding)
        // ------------------------------------------------------------------

        // bias > 0: shift right (left-aligned with gap), bias < 0: shift left (right-aligned with gap)
        private static void DrawSingleGlyph(Graphics g, char c,
                                             FontBinLoader font, Color col,
                                             int cellX, int cellY, int cellW, int cellH,
                                             int bias = 0)
        {
            if (font == null) return;
            var gi = font.GetGlyph(c);
            if (gi == null) return;

            int penX;
            if (bias == 0)
            {
                // Centered
                penX = cellX + (cellW - gi.AdvW) / 2;
            }
            else if (bias > 0)
            {
                // Left-aligned with gap: pen starts at cellX + bias
                penX = cellX + bias;
            }
            else
            {
                // Right-aligned with gap: pen ends at cellX + cellW + bias
                penX = cellX + cellW + bias - gi.AdvW;
            }

            int blockH = gi.OfsY + gi.BoxH;
            int baselineY = cellY + (cellH - blockH) / 2 + blockH;

            if (gi.Image != null)
            {
                int drawX = penX + gi.OfsX;
                int drawY = baselineY - gi.OfsY - gi.BoxH;
                DrawTinted(g, gi.Image, drawX, drawY, col);
            }
        }

        // ------------------------------------------------------------------
        // Text layout (mirrors LVGL label alignment modes)
        // ------------------------------------------------------------------

        private static void DrawTextCenter(Graphics g, string text,
                                           FontBinLoader font, Color col,
                                           int px, int py, int pw, int ph)
        {
            if (font == null) { DrawFallback(g, text, col, px, py, pw, ph, ContentAlignment.MiddleCenter); return; }

            int totalAdv = MeasureAdv(text, font);
            int blockH = MeasureBlockH(text, font);

            int penX = px + (pw - totalAdv) / 2;
            int baselineY = py + (ph - blockH) / 2 + blockH;

            DrawGlyphs(g, text, font, col, penX, baselineY);
        }

        private static void DrawTextTopLeft(Graphics g, string text,
                                            FontBinLoader font, Color col,
                                            int px, int py, int pw, int ph)
        {
            if (font == null) { DrawFallback(g, text, col, px, py, pw, ph, ContentAlignment.TopLeft); return; }

            int baselineY = py + font.Ascent;
            DrawGlyphs(g, text, font, col, px, baselineY);
        }

        private static void DrawTextBottomLeft(Graphics g, string text,
                                               FontBinLoader font, Color col,
                                               int px, int py, int pw, int ph)
        {
            if (font == null) { DrawFallback(g, text, col, px, py, pw, ph, ContentAlignment.BottomLeft); return; }

            int baselineY = py + ph + font.Descent;
            DrawGlyphs(g, text, font, col, px, baselineY);
        }

        // ------------------------------------------------------------------
        // Core glyph rasteriser
        // ------------------------------------------------------------------

        private static void DrawGlyphs(Graphics g, string text,
                                       FontBinLoader font, Color tint,
                                       int penX, int baselineY)
        {
            foreach (char c in text)
            {
                var gi = font.GetGlyph(c);
                if (gi == null)
                {
                    // Unknown glyph: skip silently (no placeholder box)
                    continue;
                }

                if (gi.Image != null && gi.BoxW > 0 && gi.BoxH > 0)
                {
                    // LVGL: draw_x = pen_x + ofs_x
                    //       draw_y = baseline_y - ofs_y - box_h
                    int drawX = penX + gi.OfsX;
                    int drawY = baselineY - gi.OfsY - gi.BoxH;
                    DrawTinted(g, gi.Image, drawX, drawY, tint);
                }
                else if (gi.BoxW > 0 && gi.BoxH > 0)
                {
                    // Compressed glyph: draw a filled rect as placeholder
                    int drawX = penX + gi.OfsX;
                    int drawY = baselineY - gi.OfsY - gi.BoxH;
                    using (var br = new SolidBrush(Color.FromArgb(160, tint)))
                        g.FillRectangle(br, drawX, drawY, gi.BoxW, gi.BoxH);
                }

                penX += gi.AdvW;
            }
        }

        // ------------------------------------------------------------------
        // Tinted blit (white RGBA glyph -> user colour)
        // ------------------------------------------------------------------

        private static void DrawTinted(Graphics g, Bitmap src, int x, int y, Color tint)
        {
            float r = tint.R / 255f;
            float gv = tint.G / 255f;
            float b = tint.B / 255f;
            float a = tint.A / 255f;

            var cm = new ColorMatrix(new[]
            {
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  0, 0 },
                new float[] { 0,  0,  0,  a, 0 },
                new float[] { r, gv,  b,  0, 1 },
            });

            var ia = new ImageAttributes();
            ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(src,
                        new Rectangle(x, y, src.Width, src.Height),
                        0, 0, src.Width, src.Height,
                        GraphicsUnit.Pixel, ia);
            ia.Dispose();
        }

        // ------------------------------------------------------------------
        // Measurement helpers
        // ------------------------------------------------------------------

        // Total horizontal advance of a string
        private static int MeasureAdv(string text, FontBinLoader font)
        {
            int total = 0;
            foreach (char c in text)
            {
                var gi = font.GetGlyph(c);
                total += gi?.AdvW ?? (font.Ascent / 3);
            }
            return total;
        }

        // Block height = max(ofs_y + box_h) across glyphs in string
        // This is what LVGL uses to vertically center/place a text block
        private static int MeasureBlockH(string text, FontBinLoader font)
        {
            int max = font.Ascent; // fallback
            foreach (char c in text)
            {
                var gi = font.GetGlyph(c);
                if (gi == null) continue;
                int h = gi.OfsY + gi.BoxH;
                if (h > max) max = h;
            }
            return max;
        }

        // ------------------------------------------------------------------
        // Background zoom-fill
        // ------------------------------------------------------------------

        private static void DrawBackground(Graphics g, Bitmap bg)
        {
            float zx = (float)CW / bg.Width;
            float zy = (float)CH / bg.Height;
            float z = Math.Max(zx, zy);
            int sw = (int)(bg.Width * z);
            int sh = (int)(bg.Height * z);
            int ox = (CW - sw) / 2;
            int oy = (CH - sh) / 2;
            g.DrawImage(bg, ox, oy, sw, sh);
        }

        // ------------------------------------------------------------------
        // Fallback text (no font loaded)
        // ------------------------------------------------------------------

        private static void DrawFallback(Graphics g, string text, Color col,
                                         int px, int py, int pw, int ph,
                                         ContentAlignment align)
        {
            using (var font = new Font("Consolas", 12f))
            using (var br = new SolidBrush(Color.FromArgb(80, col)))
            {
                var sf = new StringFormat
                {
                    Alignment = align == ContentAlignment.MiddleCenter ? StringAlignment.Center : StringAlignment.Near,
                    LineAlignment = align == ContentAlignment.MiddleCenter ? StringAlignment.Center
                                  : align == ContentAlignment.BottomLeft ? StringAlignment.Far
                                  : StringAlignment.Near,
                };
                g.DrawString(text, font, br, new RectangleF(px, py, pw, ph), sf);
            }
        }

        // ------------------------------------------------------------------
        // Utilities
        // ------------------------------------------------------------------

        private static Color ApplyOpacity(Color baseColor, byte opacity)
            => Color.FromArgb(opacity, baseColor.R, baseColor.G, baseColor.B);

        private static Color ParseHex(string hex, uint fallback)
        {
            if (!string.IsNullOrEmpty(hex))
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    try
                    {
                        uint v = Convert.ToUInt32(hex, 16);
                        return Color.FromArgb(0xFF,
                            (int)((v >> 16) & 0xFF),
                            (int)((v >> 8) & 0xFF),
                            (int)(v & 0xFF));
                    }
                    catch { }
                }
            }
            return Color.FromArgb(0xFF,
                (int)((fallback >> 16) & 0xFF),
                (int)((fallback >> 8) & 0xFF),
                (int)(fallback & 0xFF));
        }
    }
}