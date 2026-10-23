using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ESDeckPC
{
    /// <summary>
    /// Turns captured frames into what the ESP draws: changed rectangles
    /// (16 px grid) encoded as RLE (lossless, flat UI / text) or JPEG
    /// (photos, gradients -- whichever is clearly smaller). Pixels are
    /// RGB565 little-endian, as LVGL uses them on the ESP. Mirrors
    /// HID_CANVAS_ENC_* in ESDeck's usb_hid.h.
    /// </summary>
    public static class CanvasEncoder
    {
        public const byte EncRaw = 0;
        public const byte EncRle = 1;
        public const byte EncJpeg = 2;

        public const int Tile = 16;          // diff grid; rects are multiples of this (JPEG MCU friendly)
        public const int MaxRectH = 96;      // keeps single records small
        public const int IconSize = 64;
        private const long JpegQuality = 90;

        public sealed class Rect
        {
            public Rectangle Area;
            public byte Enc;
            public byte[] Data;
        }

        // ------------------------------------------------------------------
        // Pixels
        // ------------------------------------------------------------------

        public static int[] ToPixels(Bitmap bmp)
        {
            var px = new int[bmp.Width * bmp.Height];
            var bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                  ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < bmp.Height; y++)
                    Marshal.Copy(bd.Scan0 + y * bd.Stride, px, y * bmp.Width, bmp.Width);
            }
            finally { bmp.UnlockBits(bd); }
            return px;
        }

        private static ushort To565(int c)
        {
            int r = (c >> 16) & 0xFF, g = (c >> 8) & 0xFF, b = c & 0xFF;
            return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        }

        // ------------------------------------------------------------------
        // Diff: changed 16x16 tiles -> rectangles
        // ------------------------------------------------------------------

        /// <summary>Changed areas between two frames (w x h, both multiples
        /// of Tile). Tiles are merged into horizontal runs per tile row, then
        /// runs with the same span stacked vertically (up to MaxRectH).</summary>
        public static List<Rectangle> Diff(int[] prev, int[] cur, int w, int h)
        {
            int tw = w / Tile, th = h / Tile;
            var changed = new bool[tw, th];
            for (int ty = 0; ty < th; ty++)
                for (int tx = 0; tx < tw; tx++)
                    changed[tx, ty] = TileDiffers(prev, cur, w, tx * Tile, ty * Tile);

            // horizontal runs per tile row
            var runs = new List<Rectangle>();
            for (int ty = 0; ty < th; ty++)
            {
                int tx = 0;
                while (tx < tw)
                {
                    if (!changed[tx, ty]) { tx++; continue; }
                    int start = tx;
                    while (tx < tw && changed[tx, ty]) tx++;
                    runs.Add(new Rectangle(start * Tile, ty * Tile, (tx - start) * Tile, Tile));
                }
            }

            // stack runs with identical x span on consecutive rows
            var rects = new List<Rectangle>();
            foreach (var r in runs)
            {
                int i = rects.FindIndex(q => q.X == r.X && q.Width == r.Width &&
                                             q.Bottom == r.Y && q.Height + r.Height <= MaxRectH);
                if (i >= 0)
                    rects[i] = new Rectangle(rects[i].X, rects[i].Y, rects[i].Width, rects[i].Height + r.Height);
                else
                    rects.Add(r);
            }
            return rects;
        }

        private static bool TileDiffers(int[] a, int[] b, int w, int x0, int y0)
        {
            for (int y = y0; y < y0 + Tile; y++)
            {
                int i = y * w + x0;
                for (int x = 0; x < Tile; x++, i++)
                    if (((a[i] ^ b[i]) & 0x00F8FCF8) != 0)   // only bits that survive RGB565
                        return true;
            }
            return false;
        }

        /// <summary>The whole frame as horizontal strips.</summary>
        public static List<Rectangle> Strips(int w, int h)
        {
            var list = new List<Rectangle>();
            for (int y = 0; y < h; y += MaxRectH)
                list.Add(new Rectangle(0, y, w, Math.Min(MaxRectH, h - y)));
            return list;
        }

        // ------------------------------------------------------------------
        // Encoding
        // ------------------------------------------------------------------

        /// <summary>RLE (see HID_CANVAS_ENC_RLE): 0x80|n = run of n+1 of
        /// the next pixel, n = n+1 literal pixels follow.</summary>
        public static byte[] Rle(int[] px, int stride, Rectangle r)
        {
            var o = new MemoryStream(r.Width * r.Height);
            var lit = new List<ushort>(128);

            void flushLit()
            {
                if (lit.Count == 0) return;
                o.WriteByte((byte)(lit.Count - 1));
                foreach (var v in lit) { o.WriteByte((byte)v); o.WriteByte((byte)(v >> 8)); }
                lit.Clear();
            }

            int n = r.Width * r.Height, k = 0;
            ushort at(int idx) => To565(px[(r.Y + idx / r.Width) * stride + r.X + idx % r.Width]);

            while (k < n)
            {
                ushort c = at(k);
                int run = 1;
                while (k + run < n && run < 128 && at(k + run) == c) run++;
                if (run >= 3)
                {
                    flushLit();
                    o.WriteByte((byte)(0x80 | (run - 1)));
                    o.WriteByte((byte)c); o.WriteByte((byte)(c >> 8));
                    k += run;
                }
                else
                {
                    lit.Add(c);
                    k++;
                    if (lit.Count == 128) flushLit();
                }
            }
            flushLit();
            return o.ToArray();
        }

        private static readonly ImageCodecInfo JpegCodec =
            ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

        public static byte[] Jpeg(Bitmap src, Rectangle r)
        {
            using (var part = new Bitmap(r.Width, r.Height, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(part))
                    g.DrawImage(src, new Rectangle(0, 0, r.Width, r.Height), r, GraphicsUnit.Pixel);
                var ep = new EncoderParameters(1);
                ep.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
                using (var ms = new MemoryStream())
                {
                    part.Save(ms, JpegCodec, ep);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>RLE unless JPEG is a lot smaller (photos, gradients) --
        /// text stays sharp. preferSmall: just take the smaller one (big
        /// changes like scrolling, where keeping up matters more).</summary>
        public static Rect Encode(Bitmap bmp, int[] px, Rectangle r, bool preferSmall = false)
        {
            byte[] rle = Rle(px, bmp.Width, r);
            int raw = r.Width * r.Height * 2;
            if (rle.Length <= raw / 4)
                return new Rect { Area = r, Enc = EncRle, Data = rle };

            byte[] jpg = Jpeg(bmp, r);
            bool useJpeg = preferSmall ? jpg.Length < rle.Length : jpg.Length * 2 < rle.Length;
            return useJpeg
                ? new Rect { Area = r, Enc = EncJpeg, Data = jpg }
                : new Rect { Area = r, Enc = EncRle, Data = rle };
        }

        // ------------------------------------------------------------------
        // Sidebar icons
        // ------------------------------------------------------------------

        /// <summary>64 x 64 sidebar button face: the icon image fitted on
        /// the button color, or the page name as text.</summary>
        public static Bitmap RenderIcon(string iconPath, string name)
        {
            var bmp = new Bitmap(IconSize, IconSize, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0x2a, 0x2a, 0x2a));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                Image img = null;
                try { if (iconPath != null && File.Exists(iconPath)) img = LoadUnlocked(iconPath); }
                catch { img = null; }

                if (img != null)
                {
                    using (img)
                    {
                        float k = Math.Min((float)IconSize / img.Width, (float)IconSize / img.Height);
                        int w = (int)(img.Width * k), h = (int)(img.Height * k);
                        g.DrawImage(img, (IconSize - w) / 2, (IconSize - h) / 2, w, h);
                    }
                }
                else
                {
                    string text = string.IsNullOrWhiteSpace(name) ? "?" : name;
                    using (var font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point))
                    using (var brush = new SolidBrush(Color.FromArgb(0xcc, 0xcc, 0xcc)))
                    using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(text, font, brush, new RectangleF(2, 2, IconSize - 4, IconSize - 4), fmt);
                }
            }
            return bmp;
        }

        // Image.FromFile keeps the file locked; copy it out of a stream instead.
        private static Image LoadUnlocked(string path)
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(path)))
            using (var tmp = Image.FromStream(ms))
                return new Bitmap(tmp);
        }
    }
}
