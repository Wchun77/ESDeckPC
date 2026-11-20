using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace ESDeckPC
{
    /// <summary>One sidebar button: icon file (full path, may be null/missing) and the text shown without it.</summary>
    public sealed class SidebarItem
    {
        public string IconPath;
        public string Label;
        public SidebarItem(string iconPath, string label) { IconPath = iconPath; Label = label; }
    }

    /// <summary>
    /// The ESP's 80 x 480 sidebar drawn on the PC for the Deck / Monitor /
    /// Media / Touchpad editors, so the preview shows the whole 800 x 480
    /// screen. Layouts follow the firmware:
    ///   Deck / Monitor  ui_deck.c / ui_monitor.c: 64x64 page buttons from
    ///                   (8, 8), 6 px apart, inside a 400 px tall column
    ///                   (Deck scrolls when there are more than fit)
    ///   Media           ui_media.c: 28 px level bar, bottom-aligned, 380 tall
    ///   Touchpad        ui_touchpad.c: keyboard button cell on top, hotkeys
    ///                   in 80 px cells below it (Sidebar = Hotkeys)
    ///   all             ui.c: settings gear at (8, 408)
    /// Buttons: #2a2a2a, radius 8, image centered at native size. Selected =
    /// 3 px #0055cc outline when it has an image, blue fill otherwise.
    /// </summary>
    public static class EspSidebarRenderer
    {
        public const int Width = 80, Height = 480;
        public const int Btn = 64, Pad = 8, Gap = 6, Step = Btn + Gap;
        public const int ColumnH = Height - 80;      // page column above the gear cell
        public const int GearY = Height - 80 + Pad;  // 408
        public const int Cell = 80;                  // touchpad cells

        public const int HitNone = -1, HitGear = -2, HitKeyboard = -3, HitContent = -4;

        private static readonly Color ColBg = Color.FromArgb(0x11, 0x11, 0x11);
        private static readonly Color ColBtn = Color.FromArgb(0x2a, 0x2a, 0x2a);
        private static readonly Color ColSel = Color.FromArgb(0x00, 0x55, 0xcc);
        private static readonly Color ColText = Color.FromArgb(0xcc, 0xcc, 0xcc);

        // ------------------------------------------------------------------
        // Deck / Monitor
        // ------------------------------------------------------------------

        /// <summary>Total height of the page column's content (pad + buttons + gaps + pad).</summary>
        public static int ColumnContentH(int count) => count <= 0 ? 0 : Pad * 2 + count * Btn + (count - 1) * Gap;

        /// <summary>Scroll offset that keeps page idx fully visible, clamped to the content.</summary>
        public static int ScrollToShow(int scroll, int idx, int count)
        {
            int max = Math.Max(0, ColumnContentH(count) - ColumnH);
            if (idx >= 0)
            {
                int top = Pad + idx * Step, bottom = top + Btn + Pad;
                if (top - Pad < scroll) scroll = top - Pad;
                if (bottom > scroll + ColumnH) scroll = bottom - ColumnH;
            }
            return Math.Max(0, Math.Min(max, scroll));
        }

        public static Bitmap RenderPages(IList<SidebarItem> pages, string gearIcon, int selected, bool gearSelected, int scroll)
        {
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Prepare(g);
                g.SetClip(new Rectangle(0, 0, Width, ColumnH));
                for (int i = 0; i < pages.Count; i++)
                {
                    var r = new Rectangle(Pad, Pad + i * Step - scroll, Btn, Btn);
                    if (r.Bottom < 0 || r.Top > ColumnH) continue;
                    DrawButton(g, r, pages[i].IconPath, pages[i].Label, 12f, !gearSelected && i == selected, false);
                }
                g.ResetClip();
                DrawGear(g, gearIcon, gearSelected);
            }
            return bmp;
        }

        /// <summary>Page index under p, HitGear, or HitNone.</summary>
        public static int HitPages(Point p, int count, int scroll)
        {
            if (p.Y >= ColumnH) return new Rectangle(Pad, GearY, Btn, Btn).Contains(p) ? HitGear : HitNone;
            int y = p.Y + scroll - Pad;
            if (y < 0 || p.X < Pad || p.X >= Pad + Btn) return HitNone;
            int i = y / Step;
            return i < count && y - i * Step < Btn ? i : HitNone;
        }

        // ------------------------------------------------------------------
        // Media
        // ------------------------------------------------------------------

        /// <summary>level 0..100 is only for show (the real bar follows PC audio).</summary>
        public static Bitmap RenderMedia(string gearIcon, bool gearSelected, int level)
        {
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Prepare(g);
                const int barW = 28, barH = ColumnH - 20;
                var bar = new Rectangle((Width - barW) / 2, ColumnH - barH, barW, barH);
                using (var b = new SolidBrush(Color.FromArgb(0x1a, 0x1a, 0x1a))) g.FillRectangle(b, bar);
                int h = barH * Math.Max(0, Math.Min(100, level)) / 100;
                using (var b = new SolidBrush(Color.FromArgb(0x00, 0xd4, 0xff)))
                    g.FillRectangle(b, bar.X, bar.Bottom - h, barW, h);
                DrawGear(g, gearIcon, gearSelected);
            }
            return bmp;
        }

        /// <summary>HitGear, or HitContent for the level bar column (the player page).</summary>
        public static int HitMedia(Point p) =>
            p.Y >= ColumnH ? (new Rectangle(Pad, GearY, Btn, Btn).Contains(p) ? HitGear : HitNone) : HitContent;

        // ------------------------------------------------------------------
        // Touchpad
        // ------------------------------------------------------------------

        public static Bitmap RenderTouchpad(string keyboardIcon, IList<SidebarItem> hotkeys, string gearIcon, bool gearSelected)
        {
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Prepare(g);
                var kb = new Rectangle(Pad, Pad, Btn, Btn);
                if (Load(keyboardIcon) != null) DrawButton(g, kb, keyboardIcon, null, 14f, false, true);
                else DrawGlyph(g, kb, "⌨", ColBtn);   // keyboard glyph, like LV_SYMBOL_KEYBOARD
                for (int i = 0; i < hotkeys.Count; i++)
                {
                    var r = new Rectangle(Pad, Cell + i * Cell + Pad, Btn, Btn);
                    if (r.Bottom > ColumnH) break;
                    DrawButton(g, r, hotkeys[i].IconPath, hotkeys[i].Label, 14f, false, true);
                }
                DrawGear(g, gearIcon, gearSelected);
            }
            return bmp;
        }

        /// <summary>HitKeyboard, a hotkey index, HitGear, or HitNone.</summary>
        public static int HitTouchpad(Point p, int hotkeyCount)
        {
            if (p.X < Pad || p.X >= Pad + Btn) return HitNone;
            if (p.Y < Cell) return p.Y >= Pad && p.Y < Pad + Btn ? HitKeyboard : HitNone;
            if (p.Y >= ColumnH) return p.Y >= GearY && p.Y < GearY + Btn ? HitGear : HitNone;
            int i = (p.Y - Cell) / Cell, y = (p.Y - Cell) % Cell;
            return i < hotkeyCount && y >= Pad && y < Pad + Btn ? i : HitNone;
        }

        // ------------------------------------------------------------------
        // Drawing helpers
        // ------------------------------------------------------------------

        private static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(ColBg);
        }

        // wrap: touchpad hotkeys wrap (LV_LABEL_LONG_WRAP), page names are one clipped line (LONG_CLIP)
        private static void DrawButton(Graphics g, Rectangle r, string iconPath, string label, float textPx, bool selected, bool wrap)
        {
            Bitmap icon = Load(iconPath);
            using (var path = Rounded(r, 8))
            {
                using (var b = new SolidBrush(selected && icon == null ? ColSel : ColBtn)) g.FillPath(b, path);
                Region old = g.Clip;
                g.SetClip(path, CombineMode.Intersect);
                if (icon != null)
                    g.DrawImage(icon, r.X + (r.Width - icon.Width) / 2, r.Y + (r.Height - icon.Height) / 2, icon.Width, icon.Height);
                else if (!string.IsNullOrEmpty(label))
                    using (var f = new Font("Segoe UI", textPx, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var b = new SolidBrush(ColText))
                    using (var fmt = new StringFormat(wrap ? 0 : StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.None })
                        g.DrawString(label, f, b, new RectangleF(r.X + 2, r.Y, r.Width - 4, r.Height), fmt);
                g.Clip = old;
            }
            if (selected && icon != null)
                using (var pen = new Pen(ColSel, 3))
                using (var ring = Rounded(Rectangle.Inflate(r, 2, 2), 10))
                    g.DrawPath(pen, ring);
        }

        private static void DrawGear(Graphics g, string gearIcon, bool selected)
        {
            var r = new Rectangle(Pad, GearY, Btn, Btn);
            if (Load(gearIcon) != null) DrawButton(g, r, gearIcon, null, 12f, selected, false);
            else DrawGlyph(g, r, "⚙", selected ? ColSel : ColBtn);
        }

        private static void DrawGlyph(Graphics g, Rectangle r, string glyph, Color fill)
        {
            using (var path = Rounded(r, 8))
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            using (var f = new Font("Segoe UI Symbol", 22f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(glyph, f, Brushes.White, r, fmt);
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ------------------------------------------------------------------
        // Icon cache: path + last write time, so the sidebar can be redrawn
        // on every preview refresh without re-reading files, and a replaced
        // file is picked up. Loaded through a copy so the file isn't locked.
        // ------------------------------------------------------------------

        private static readonly Dictionary<string, Tuple<DateTime, Bitmap>> _cache =
            new Dictionary<string, Tuple<DateTime, Bitmap>>(StringComparer.OrdinalIgnoreCase);

        private static Bitmap Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (!File.Exists(path)) return null;
                DateTime t = File.GetLastWriteTimeUtc(path);
                if (_cache.TryGetValue(path, out var hit) && hit.Item1 == t) return hit.Item2;
                Bitmap bmp;
                using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                using (var img = Image.FromStream(ms))
                    bmp = new Bitmap(img);
                if (hit != null) hit.Item2.Dispose();
                _cache[path] = Tuple.Create(t, bmp);
                return bmp;
            }
            catch { return null; }
        }

        /// <summary>assets\side_icons\name, or null when either is empty.</summary>
        public static string Resolve(string sideIconsDir, string name) =>
            string.IsNullOrEmpty(sideIconsDir) || string.IsNullOrEmpty(name) ? null : Path.Combine(sideIconsDir, name);
    }
}
