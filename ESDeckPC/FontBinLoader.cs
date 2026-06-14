using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ESDeckPC
{
    public class GlyphInfo
    {
        public int Code { get; set; }
        public int AdvW { get; set; }
        public int OfsX { get; set; }
        public int OfsY { get; set; }
        public int BoxW { get; set; }
        public int BoxH { get; set; }
        public Bitmap Image { get; set; }
    }

    /// <summary>
    /// Loads glyph metrics and bitmaps from a lv_font_conv --format bin file.
    /// Decompression is a direct C# port of lv_font_fmt_txt.c (LVGL v8).
    /// </summary>
    public class FontBinLoader : IDisposable
    {
        public int Ascent { get; private set; }
        public int Descent { get; private set; }
        public int LineHeight => Ascent - Descent;
        public int IdxLocFmt { get; private set; }
        public int XyBits { get; private set; }
        public int WhBits { get; private set; }
        public int AdvBits { get; private set; }

        private readonly Dictionary<int, GlyphInfo> _glyphs = new Dictionary<int, GlyphInfo>();
        private bool _disposed;

        // ------------------------------------------------------------------
        // Load
        // ------------------------------------------------------------------

        public static FontBinLoader Load(string binPath)
        {
            if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
                return null;
            try { return Parse(File.ReadAllBytes(binPath)); }
            catch { return null; }
        }

        private static FontBinLoader Parse(byte[] d)
        {
            var loader = new FontBinLoader();

            // Head offsets (verified against lv_font_conv source)
            int headSize = (int)RU32(d, 0);
            loader.Ascent = RU16(d, 16);
            loader.Descent = RS16(d, 18);
            int idxLocFmt = d[34];
            int advFmt = d[36];
            int bpp = d[37];
            int xyBits = d[38];
            int whBits = d[39];
            int advBits = d[40];
            int comprId = d[41];

            loader.IdxLocFmt = idxLocFmt;
            loader.XyBits = xyBits;
            loader.WhBits = whBits;
            loader.AdvBits = advBits;

            int nbitsHdr = advBits + 2 * xyBits + 2 * whBits;

            // cmap
            int cmapOff = headSize;
            int cmapSize = (int)RU32(d, cmapOff);
            int nSubs = (int)RU32(d, cmapOff + 8);

            var cmap = new Dictionary<int, int>();
            int shBase = cmapOff + 12;
            for (int si = 0; si < nSubs; si++)
            {
                int sh = shBase + si * 16;
                int dataOffset = (int)RU32(d, sh);
                int rangeStart = (int)RU32(d, sh + 4);
                int rangeLen = RU16(d, sh + 8);
                int glyphIdStart = RU16(d, sh + 10);
                int entriesCount = RU16(d, sh + 12);
                int fmtType = d[sh + 14];
                int listOff = cmapOff + dataOffset;

                if (fmtType == 0) // FORMAT0_FULL
                {
                    for (int i = 0; i < rangeLen; i++)
                        cmap[rangeStart + i] = glyphIdStart + d[listOff + i];
                }
                else if (fmtType == 1) // SPARSE_FULL
                {
                    for (int i = 0; i < entriesCount; i++)
                    {
                        int cd = RU16(d, listOff + i * 2);
                        int id = RU16(d, listOff + entriesCount * 2 + i * 2);
                        cmap[rangeStart + cd] = glyphIdStart + id;
                    }
                }
                else if (fmtType == 2) // FORMAT0_TINY
                {
                    for (int i = 0; i < rangeLen; i++)
                        cmap[rangeStart + i] = glyphIdStart + i;
                }
                else if (fmtType == 3) // SPARSE_TINY
                {
                    for (int i = 0; i < entriesCount; i++)
                    {
                        int cd = RU16(d, listOff + i * 2);
                        cmap[rangeStart + cd] = glyphIdStart + i;
                    }
                }
            }

            // loca
            int locaOff = cmapOff + cmapSize;
            int locaSize = (int)RU32(d, locaOff);
            int nGlyphs = (int)RU32(d, locaOff + 8);
            var loca = new int[nGlyphs];
            for (int i = 0; i < nGlyphs; i++)
                loca[i] = idxLocFmt == 0
                    ? RU16(d, locaOff + 12 + i * 2)
                    : (int)RU32(d, locaOff + 12 + i * 4);

            // glyf
            int glyfOff = locaOff + locaSize;
            int glyfSize = (int)RU32(d, glyfOff) - 8; // content size

            foreach (var kvp in cmap)
            {
                int cp = kvp.Key;
                int gid = kvp.Value;
                if (gid == 0 || gid >= nGlyphs) continue;

                int gOff = glyfOff + loca[gid];
                int bitPos = gOff * 8;

                int advW = (int)BitsFromBuf(d, bitPos, advBits); bitPos += advBits;
                // adv_w_format 0=pixel, 1=FP4 (stored as pixel*16, divide to get pixel)
                if (advFmt == 1) advW = (advW + 8) >> 4;
                int ofsX = BitsFromBufSigned(d, bitPos, xyBits); bitPos += xyBits;
                int ofsY = BitsFromBufSigned(d, bitPos, xyBits); bitPos += xyBits;
                int boxW = (int)BitsFromBuf(d, bitPos, whBits); bitPos += whBits;
                int boxH = (int)BitsFromBuf(d, bitPos, whBits);

                var gi = new GlyphInfo { Code = cp, AdvW = advW, OfsX = ofsX, OfsY = ofsY, BoxW = boxW, BoxH = boxH };

                if (boxW > 0 && boxH > 0)
                {
                    int nextOff = (gid + 1 < nGlyphs) ? loca[gid + 1] : glyfSize + 8;
                    int bmpSize = nextOff - loca[gid] - nbitsHdr / 8;
                    byte[] bmpData = ExtractBmpBytes(d, gOff, nbitsHdr, bmpSize);
                    gi.Image = RenderGlyph(bmpData, boxW, boxH, bpp, comprId == 1);
                }

                loader._glyphs[cp] = gi;
            }

            return loader;
        }

        // ------------------------------------------------------------------
        // Bitmap extraction
        //
        // After the bit-packed glyph header (nbitsHdr bits), the bitmap data
        // follows in the bit stream. LVGL loader reads it byte-by-byte across
        // byte boundaries, with the last byte left-shifted by (nbitsHdr % 8).
        // ------------------------------------------------------------------

        private static byte[] ExtractBmpBytes(byte[] d, int glyphByteOff, int nbitsHdr, int bmpSize)
        {
            if (bmpSize <= 0) return new byte[0];
            int startBit = glyphByteOff * 8 + nbitsHdr;
            int tail = nbitsHdr % 8;
            var bmp = new byte[bmpSize];

            int bit = startBit;
            for (int k = 0; k < bmpSize - 1; k++)
            {
                bmp[k] = (byte)BitsFromBuf(d, bit, 8);
                bit += 8;
            }
            int lastLen = (tail == 0) ? 8 : (8 - tail);
            bmp[bmpSize - 1] = (byte)((uint)BitsFromBuf(d, bit, lastLen) << tail);

            return bmp;
        }

        // ------------------------------------------------------------------
        // Decompression: direct port of lv_font_fmt_txt.c decompress() + rle_next()
        // ------------------------------------------------------------------

        private enum RleState { Single, Repeat, Counter }

        private static Bitmap RenderGlyph(byte[] bmp, int w, int h, int bpp, bool prefilter)
        {
            RleState state = RleState.Single;
            uint rdp = 0;
            byte prevV = 0;
            int cnt = 0;

            byte RleNext()
            {
                byte ret;
                if (state == RleState.Single)
                {
                    ret = (byte)BmpBits(bmp, rdp, bpp);
                    if (rdp != 0 && prevV == ret) { cnt = 0; state = RleState.Repeat; }
                    prevV = ret; rdp += (uint)bpp;
                }
                else if (state == RleState.Repeat)
                {
                    uint v = BmpBits(bmp, rdp, 1); cnt++; rdp++;
                    if (v == 1)
                    {
                        ret = prevV;
                        if (cnt == 11)
                        {
                            cnt = (int)BmpBits(bmp, rdp, 6); rdp += 6;
                            if (cnt != 0) { state = RleState.Counter; }
                            else
                            {
                                ret = (byte)BmpBits(bmp, rdp, bpp);
                                prevV = ret; rdp += (uint)bpp; state = RleState.Single;
                            }
                        }
                    }
                    else
                    {
                        ret = (byte)BmpBits(bmp, rdp, bpp);
                        prevV = ret; rdp += (uint)bpp; state = RleState.Single;
                    }
                }
                else // Counter
                {
                    ret = prevV; cnt--;
                    if (cnt == 0)
                    {
                        ret = (byte)BmpBits(bmp, rdp, bpp);
                        prevV = ret; rdp += (uint)bpp; state = RleState.Single;
                    }
                }
                return ret;
            }

            byte[] line1 = new byte[w];
            byte[] line2 = new byte[w];
            int[] pixels = new int[w * h];

            for (int x = 0; x < w; x++) line1[x] = RleNext();
            for (int x = 0; x < w; x++) pixels[x] = line1[x];

            for (int y = 1; y < h; y++)
            {
                for (int x = 0; x < w; x++) line2[x] = RleNext();
                if (prefilter)
                {
                    for (int x = 0; x < w; x++)
                    {
                        line1[x] = (byte)(line2[x] ^ line1[x]);
                        pixels[y * w + x] = line1[x];
                    }
                }
                else
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixels[y * w + x] = line2[x];
                        line1[x] = line2[x];
                    }
                }
            }

            return PixelsToRgba(pixels, w, h, bpp);
        }

        // get_bits for bmp byte array (lv_font_fmt_txt.c get_bits)
        private static uint BmpBits(byte[] bmp, uint bitPos, int length)
        {
            int bytePos = (int)(bitPos >> 3);
            int bp2 = (int)(bitPos & 0x7);
            uint mask = (uint)((1 << length) - 1);
            if (bytePos >= bmp.Length) return 0;
            if (bp2 + length >= 8)
            {
                uint in16 = (uint)(bmp[bytePos] << 8);
                if (bytePos + 1 < bmp.Length) in16 += bmp[bytePos + 1];
                return (in16 >> (16 - bp2 - length)) & mask;
            }
            return ((uint)bmp[bytePos] >> (8 - bp2 - length)) & mask;
        }

        private static Bitmap PixelsToRgba(int[] pixels, int w, int h, int bpp)
        {
            int maxVal = (1 << bpp) - 1;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var bd = bmp.LockBits(new Rectangle(0, 0, w, h),
                                       ImageLockMode.WriteOnly,
                                       PixelFormat.Format32bppArgb);
            int stride = bd.Stride;
            byte[] buf = new byte[Math.Abs(stride) * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte a = (byte)(pixels[y * w + x] * 255 / maxVal);
                    int i = y * stride + x * 4;
                    buf[i] = 255; buf[i + 1] = 255; buf[i + 2] = 255; buf[i + 3] = a;
                }
            Marshal.Copy(buf, 0, bd.Scan0, buf.Length);
            bmp.UnlockBits(bd);
            return bmp;
        }

        // ------------------------------------------------------------------
        // Query
        // ------------------------------------------------------------------

        public bool HasGlyph(char c) => _glyphs.ContainsKey((int)c);
        public GlyphInfo GetGlyph(char c) { _glyphs.TryGetValue((int)c, out var g); return g; }

        // Debug info: first glyph '0' metrics for status display
        public string DebugInfo()
        {
            string hdr = $"[locFmt={IdxLocFmt} xy={XyBits} wh={WhBits} adv={AdvBits}]";
            if (_glyphs.TryGetValue(0x30, out var g))
                return $"{hdr} '0': adv={g.AdvW} ofs=({g.OfsX},{g.OfsY}) box={g.BoxW}x{g.BoxH}";
            return $"{hdr} no '0' glyph";
        }

        // ------------------------------------------------------------------
        // Bit reading helpers (MSB first = big-endian, for glyph header fields)
        // ------------------------------------------------------------------

        private static uint BitsFromBuf(byte[] d, int bitPos, int nbits)
        {
            uint val = 0;
            for (int i = 0; i < nbits; i++)
            {
                int by = (bitPos + i) / 8;
                int bi = 7 - ((bitPos + i) % 8);
                if (by < d.Length)
                    val = (val << 1) | ((uint)(d[by] >> bi) & 1u);
                else
                    val <<= 1;
            }
            return val;
        }

        private static int BitsFromBufSigned(byte[] d, int bitPos, int nbits)
        {
            int v = (int)BitsFromBuf(d, bitPos, nbits);
            if (nbits > 0 && v >= (1 << (nbits - 1))) v -= (1 << nbits);
            return v;
        }

        // Byte-level helpers
        private static int RU16(byte[] d, int o) => d[o] | (d[o + 1] << 8);
        private static int RS16(byte[] d, int o) { int v = RU16(d, o); return v >= 0x8000 ? v - 0x10000 : v; }
        private static uint RU32(byte[] d, int o) => (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));

        // ------------------------------------------------------------------
        // IDisposable
        // ------------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var g in _glyphs.Values) g.Image?.Dispose();
            _glyphs.Clear();
        }
    }
}