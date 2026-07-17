using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ESDeckPC
{
    /// <summary>
    /// Renders a preview bitmap for a Monitor data page (System/GPU/Storage/etc.),
    /// mirroring the firmware layout in ui_monitor.c (build_data_page / make_cell /
    /// update_data_pages). Values shown are placeholder "-" since live sensor data
    /// is not available on the PC editor; the layout, colors, and bar thresholds
    /// match the device exactly so the preview is visually representative.
    /// </summary>
    public static class MonitorPageRenderer
    {
        // Content area: 800x480 screen minus 80px sidebar (see ui.h SCREEN_W/H, SIDEBAR_W)
        private const int ContentW = 720;
        private const int ContentH = 480;

        private const int CellW = 320;
        private const int CellH = 200;
        private const int QuadW = ContentW / 2;
        private const int QuadH = ContentH / 2;
        private const int CellOffX = (QuadW - CellW) / 2;
        private const int CellOffY = (QuadH - CellH) / 2;

        private const int BarMaxUsage = 100;
        private const int BarMaxTemp = 105;

        // Five-step thresholds, percent of bar_max (ui_monitor.h MON_BAR_THR_*)
        private const float ThrLow = 40f;
        private const float ThrMid = 60f;
        private const float ThrHigh = 75f;
        private const float ThrCrit = 90f;

        private static readonly Color BarColLow = Color.FromArgb(0x00, 0x55, 0xCC);
        private static readonly Color BarColMid = Color.FromArgb(0x00, 0xAA, 0x44);
        private static readonly Color BarColHigh = Color.FromArgb(0xDD, 0xBB, 0x00);
        private static readonly Color BarColWarn = Color.FromArgb(0xFF, 0x77, 0x00);
        private static readonly Color BarColCrit = Color.FromArgb(0xFF, 0x22, 0x22);

        private sealed class CellMeta
        {
            public string Label;
            public string Unit;
            public int BarMax;
            public bool IsTemp;
            public bool Invert;
        }

        // Mirrors s_cell_meta in ui_monitor.c exactly (label/unit/bar_max/is_temp/invert).
        private static readonly CellMeta[] CellMetaTable =
        {
            new CellMeta { Label = "",          Unit = "",       BarMax = 0,           IsTemp = false, Invert = false }, // none
            new CellMeta { Label = "CPU Usage",  Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "CPU Temp",   Unit = " C",     BarMax = BarMaxTemp,  IsTemp = true,  Invert = false },
            new CellMeta { Label = "CPU Freq",   Unit = " GHz",   BarMax = 0,           IsTemp = false, Invert = false },
            new CellMeta { Label = "RAM Usage",  Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "GPU Usage",  Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "GPU Temp",   Unit = " C",     BarMax = BarMaxTemp,  IsTemp = true,  Invert = false },
            new CellMeta { Label = "VRAM",       Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "Net Up",     Unit = " MB/s",  BarMax = 0,           IsTemp = false, Invert = false },
            new CellMeta { Label = "Net Down",   Unit = " MB/s",  BarMax = 0,           IsTemp = false, Invert = false },
            new CellMeta { Label = "Disk",       Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "CPU Power",  Unit = " W",     BarMax = 0,           IsTemp = false, Invert = false },
            new CellMeta { Label = "GPU Power",  Unit = " % TDP", BarMax = BarMaxUsage, IsTemp = false, Invert = false },
            new CellMeta { Label = "SSD Life",   Unit = "%",      BarMax = BarMaxUsage, IsTemp = false, Invert = true  },
        };

        // Order must match firmware mon_cell_id_t (MON_CELL_NONE = index 0).
        private static readonly string[] CellIds =
        {
            "", "cpu_usage", "cpu_temp", "cpu_freq", "ram_usage",
            "gpu_usage", "gpu_temp", "gpu_vram", "net_up", "net_down",
            "disk_usage", "cpu_power", "gpu_power", "ssd_life",
        };

        private static int CellIndex(string cellId)
        {
            if (string.IsNullOrEmpty(cellId)) return 0;
            int idx = Array.IndexOf(CellIds, cellId);
            return idx >= 0 ? idx : 0;
        }

        /// <summary>
        /// Renders the given page (background + 2x2 cell grid) at preview
        /// placeholder values ("-", no bar fill), matching the no-data state
        /// shown on the device before live sensor data arrives.
        /// </summary>
        public static Bitmap Render(MonitorPageCfg page, Bitmap bgBitmap)
        {
            var bmp = new Bitmap(ContentW, ContentH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.FromArgb(18, 18, 18));

                DrawBackground(g, bgBitmap);

                if (page?.Cells != null)
                {
                    for (int j = 0; j < page.Cells.Length && j < 4; j++)
                    {
                        string cellId = page.Cells[j];
                        if (string.IsNullOrEmpty(cellId)) continue;

                        int col = j % 2;
                        int row = j / 2;
                        var meta = CellMetaTable[CellIndex(cellId)];
                        DrawCell(g, col, row, meta);
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Renders just the background (zoom-fill + 50% mask, same convention
        /// as Render()) with no cell grid -- used for the Settings entry,
        /// which only ever has a bg_image (no data cells in the schema).
        /// </summary>
        public static Bitmap RenderBackgroundOnly(Bitmap bgBitmap)
        {
            var bmp = new Bitmap(ContentW, ContentH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(18, 18, 18));
                DrawBackground(g, bgBitmap);
            }
            return bmp;
        }

        private static void DrawBackground(Graphics g, Bitmap bgBitmap)
        {
            if (bgBitmap != null)
            {
                // Zoom-fill: scale to cover ContentW x ContentH, center horizontally
                // (mirrors build_data_page's zoom_x/zoom_y/offset_x logic).
                float zoomX = (float)ContentW / bgBitmap.Width;
                float zoomY = (float)ContentH / bgBitmap.Height;
                float zoom = Math.Max(zoomX, zoomY);
                int scaledW = (int)(bgBitmap.Width * zoom);
                int scaledH = (int)(bgBitmap.Height * zoom);
                int offsetX = (ContentW - scaledW) / 2;

                g.DrawImage(bgBitmap, offsetX, 0, scaledW, scaledH);
            }

            // 50% black mask overlay (LV_OPA_50), drawn whether or not a bg image is set.
            using (var maskBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                g.FillRectangle(maskBrush, 0, 0, ContentW, ContentH);
        }

        private static void DrawCell(Graphics g, int col, int row, CellMeta meta)
        {
            int x = col * QuadW + CellOffX;
            int y = row * QuadH + CellOffY;
            var rect = new Rectangle(x, y, CellW, CellH);

            using (var bgBrush = new SolidBrush(Color.FromArgb(128, 30, 30, 30))) // 0x1e1e1e @ 50%
            using (var borderPen = new Pen(Color.FromArgb(0x33, 0x33, 0x33)))
            using (var path = RoundedRect(rect, 10))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);
            }

            // Title (top-left, with cell's internal 16px padding)
            using (var titleFont = new Font("Segoe UI", 9.5f))
            using (var titleBrush = new SolidBrush(Color.FromArgb(0xAA, 0xAA, 0xAA)))
                g.DrawString(meta.Label, titleFont, titleBrush, x + 16, y + 16);

            // Value label (placeholder "-", centered, slightly above middle)
            using (var valFont = new Font("Segoe UI", 22f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(Color.White))
            {
                string text = "-";
                var size = g.MeasureString(text, valFont);
                float vx = x + (CellW - size.Width) / 2f;
                float vy = y + (CellH - size.Height) / 2f - 10;
                g.DrawString(text, valFont, valBrush, vx, vy);
            }

            // Bar (bottom, only if this cell type has one; shown empty/at 0 -- no live data)
            if (meta.BarMax > 0)
            {
                int barW = CellW - 32;
                int barH = 8;
                int barX = x + 16;
                int barY = y + CellH - 16 - barH;

                using (var trackBrush = new SolidBrush(Color.FromArgb(0x33, 0x33, 0x33)))
                    g.FillRoundedRect(trackBrush, barX, barY, barW, barH, 4);

                // 0% fill in the no-data state; color shown is the "low" tier
                // since the preview cannot reflect live values.
                using (var fillBrush = new SolidBrush(BarColLow))
                    g.FillRoundedRect(fillBrush, barX, barY, 0, barH, 4);
            }
        }

        /// <summary>
        /// Picks the firmware's five-step bar color for a given value, for
        /// future use once live/simulated values are wired into the preview.
        /// </summary>
        public static Color BarColorFor(float value, CellPreviewMeta meta)
        {
            if (meta.BarMax <= 0) return BarColLow;
            float pct = value / meta.BarMax * 100f;
            if (meta.Invert) pct = 100f - pct;

            if (pct >= ThrCrit) return BarColCrit;
            if (pct >= ThrHigh) return BarColWarn;
            if (pct >= ThrMid) return BarColHigh;
            if (pct >= ThrLow) return BarColMid;
            return BarColLow;
        }

        public struct CellPreviewMeta
        {
            public int BarMax;
            public bool Invert;
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

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush brush, int x, int y, int w, int h, int radius)
        {
            if (w <= 0 || h <= 0) return;
            int d = Math.Min(radius * 2, Math.Min(w, h));
            using (var path = new GraphicsPath())
            {
                if (d <= 0)
                {
                    g.FillRectangle(brush, x, y, w, h);
                    return;
                }
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + w - d, y, d, d, 270, 90);
                path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
                path.AddArc(x, y + h - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
    }
}