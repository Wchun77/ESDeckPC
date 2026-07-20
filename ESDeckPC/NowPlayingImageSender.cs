using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace ESDeckPC
{
    /// <summary>
    /// Sends cover art + a PC-rendered title/artist strip to the ESP over
    /// HID (CMD_NOWPLAYING_IMG_START/CHUNK/END = 0x08-0x0A, same page=0xFE
    /// channel as NowPlayingSender/AudioLevelSender). Rendering the
    /// title/artist as an image (rather than sending text) sidesteps the
    /// ESP having no CJK font on-device -- GDI+ handles Unicode text and
    /// font fallback for us here; the ESP just displays whatever bitmap
    /// arrives.
    ///
    /// Triggered by NowPlayingWatcher.OnMediaPropertiesChanged (fires
    /// whenever the OS reports new media properties for the focused
    /// session, i.e. on track change). Only active while Subscribe()d
    /// (mirrors NowPlayingSender's lifecycle) -- no point spending
    /// CPU/USB bandwidth rendering images the ESP isn't displaying.
    ///
    /// OUT report layout (65 bytes: Report ID + 64 payload), all three
    /// share generation(1B) + kind(1B) right after the cmd byte:
    ///   CMD_IMG_START (0x08): [2]=generation [3]=kind [4..5]=total_size(u16 LE)
    ///   CMD_IMG_CHUNK (0x09): [2]=generation [3]=kind [4..]=raw encoded bytes (<=61B/report)
    ///   CMD_IMG_END   (0x0A): [2]=generation [3]=kind
    /// Encoding differs per kind -- COVER is JPEG (photographic thumbnail),
    /// INFO is PNG (flat background + sharp text edges showed visible JPEG
    /// blocking artifacts around the glyphs; ESP decodes accordingly, see
    /// usb_hid.c's img_recv_end()).
    /// generation increments per track change so the ESP can tell a chunk
    /// from a superseded transfer apart from the current one -- see
    /// usb_hid.c's img_recv_* on the ESP side. No chunk index: reports go
    /// out over a single blocking control pipe from one thread here, so
    /// ordering is already guaranteed.
    /// </summary>
    public class NowPlayingImageSender : IDisposable
    {
        private const int REPORT_SIZE = 65;
        private const byte CMD_IMG_START = 0x08;
        private const byte CMD_IMG_CHUNK = 0x09;
        private const byte CMD_IMG_END   = 0x0A;
        private const byte KIND_COVER    = 0;
        private const byte KIND_INFO     = 1;

        private const int COVER_W = 220, COVER_H = 220;
        private const int INFO_W  = 480, INFO_H  = 70;
        private const int CHUNK_DATA_SIZE = 61;   // REPORT_SIZE - ID(1) - cmd(1) - gen(1) - kind(1)
        private const int MAX_JPEG_SIZE   = 60000; // must stay under ESP's HID_IMG_MAX_JPEG_SIZE (60KB) and fit a uint16 total_size

        private readonly HidReceiver _receiver;
        private readonly NowPlayingWatcher _watcher;

        private volatile bool _subscribed = false;
        private byte _generation = 0;

        public event Action<string> OnLog;

        public NowPlayingImageSender(HidReceiver receiver, NowPlayingWatcher watcher)
        {
            _receiver = receiver;
            _watcher = watcher;
            _watcher.OnMediaPropertiesChanged += OnMediaPropertiesChanged;
        }

        // ------------------------------------------------------------------
        // Subscribe / Unsubscribe -- no background thread needed here (this
        // only fires on track change, not on a fixed cadence like the other
        // senders), so this is just a gate on OnMediaPropertiesChanged.
        // ------------------------------------------------------------------

        public void Subscribe()
        {
            _subscribed = true;
            // Send whatever's already playing right away instead of
            // waiting for the next track change.
            _ = SendCurrentAsync();
        }

        public void Unsubscribe()
        {
            _subscribed = false;
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSessionMediaProperties props)
        {
            if (!_subscribed) return;

            if (props == null)
            {
                // Focus/session lost -- nothing to show anymore. Without
                // this the ESP would keep displaying whatever was playing
                // last instead of falling back to the placeholder/"None"
                // state, since it has no other signal that the info went
                // stale (unlike progress, which has its own ~3s timeout).
                SendClear(KIND_COVER);
                SendClear(KIND_INFO);
                Log("NowPlayingImg: focus lost, cleared cover/info");
                return;
            }

            _ = SendCurrentAsync();
        }

        private async Task SendCurrentAsync()
        {
            var props = _watcher.CurrentProperties;
            if (props == null) return;

            byte gen = unchecked(++_generation);

            try
            {
                // PNG, not JPEG: this is a flat background with sharp text
                // edges, and JPEG's lossy chroma subsampling showed visible
                // blocking artifacts around the glyphs. PNG is lossless and
                // this content (mostly one flat color) compresses to almost
                // nothing anyway, so there's no size-budget reason to
                // prefer JPEG here like there is for the photographic cover.
                using (Bitmap info = RenderInfoStrip(props.Title, props.Artist))
                {
                    SendImage(KIND_INFO, gen, info, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                Log($"NowPlayingImg: info render/send failed ({ex.Message})");
            }

            try
            {
                using (Bitmap cover = await LoadCoverAsync(props))
                {
                    if (cover != null)
                        SendImage(KIND_COVER, gen, cover, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Log($"NowPlayingImg: cover fetch/send failed ({ex.Message})");
            }
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        /// <summary>
        /// Draws title (top) + artist (bottom) onto a *transparent* strip
        /// (real alpha channel, saved as PNG) rather than a flat fill color
        /// -- Media will eventually have a real background image behind
        /// this, and a solid rectangle behind the text would look wrong
        /// sitting on top of it. LVGL alpha-blends this against whatever's
        /// actually behind it at render time (see ui_media.c's
        /// apply_info_image()). "Microsoft JhengHei UI" is used for CJK
        /// coverage; GDI+ falls back to the system default if it's somehow
        /// missing rather than throwing. AntiAlias (not ClearTypeGridFit)
        /// for the text -- ClearType's subpixel color fringing assumes an
        /// opaque backdrop and looks wrong composited over transparency.
        /// </summary>
        private static Bitmap RenderInfoStrip(string title, string artist)
        {
            var bmp = new Bitmap(INFO_W, INFO_H, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                string titleText = string.IsNullOrEmpty(title) ? "None" : title;
                string artistText = artist ?? "";

                using (var titleFont = new Font("Microsoft JhengHei UI", 17f, System.Drawing.FontStyle.Bold))
                using (var artistFont = new Font("Microsoft JhengHei UI", 12f))
                using (var titleBrush = new SolidBrush(Color.White))
                using (var artistBrush = new SolidBrush(Color.FromArgb(0x99, 0x99, 0x99)))
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                })
                {
                    var titleRect = new RectangleF(0, 2, INFO_W, 32);
                    var artistRect = new RectangleF(0, 38, INFO_W, 24);
                    g.DrawString(titleText, titleFont, titleBrush, titleRect, format);
                    if (artistText.Length > 0)
                        g.DrawString(artistText, artistFont, artistBrush, artistRect, format);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Pulls the session's thumbnail (Windows Media Session API gives
        /// this directly -- GlobalSystemMediaTransportControlsSessionMedia
        /// Properties.Thumbnail -- no per-app scraping needed, works the
        /// same for Spotify, a browser tab playing YouTube, etc, since it's
        /// whatever that app registered with the OS) and center-crops it to
        /// a square so non-square art doesn't look squished. Returns null
        /// if the session has no thumbnail.
        /// </summary>
        private static async Task<Bitmap> LoadCoverAsync(GlobalSystemMediaTransportControlsSessionMediaProperties props)
        {
            var thumbRef = props.Thumbnail;
            if (thumbRef == null) return null;

            byte[] bytes;
            using (var stream = await thumbRef.OpenReadAsync())
            {
                // DataReader (Windows.Storage.Streams) instead of the more
                // usual AsStreamForRead() extension -- that extension lives
                // in System.Runtime.WindowsRuntime.dll, which this project
                // doesn't explicitly reference (Windows.Media.Control comes
                // in via the Dubya.WindowsMediaController NuGet package's
                // own WinRT projection setup, not a manual winmd reference).
                // DataReader is part of the same Windows.Storage.Streams
                // namespace the stream itself came from, so no extra
                // reference is needed.
                var reader = new DataReader(stream);
                await reader.LoadAsync((uint)stream.Size);
                bytes = new byte[stream.Size];
                reader.ReadBytes(bytes);
            }

            using (var ms = new MemoryStream(bytes))
            using (var src = new Bitmap(ms))
            {
                var dst = new Bitmap(COVER_W, COVER_H, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(dst))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.FromArgb(0x2a, 0x2a, 0x2a));

                    float srcSize = Math.Min(src.Width, src.Height);
                    float srcX = (src.Width - srcSize) / 2f;
                    float srcY = (src.Height - srcSize) / 2f;
                    g.DrawImage(src, new RectangleF(0, 0, COVER_W, COVER_H),
                                new RectangleF(srcX, srcY, srcSize, srcSize), GraphicsUnit.Pixel);
                }
                return dst;
            }
        }

        // ------------------------------------------------------------------
        // HID transport -- START/CHUNK*/END
        // ------------------------------------------------------------------

        /// <summary>
        /// Sends CMD_IMG_START with total_size=0 -- the ESP-side sentinel
        /// for "clear whatever's currently shown for this kind, don't wait
        /// for a transfer" (see usb_hid.c's img_recv_start()).
        /// </summary>
        private void SendClear(byte kind)
        {
            byte gen = unchecked(++_generation);
            var start = new byte[REPORT_SIZE];
            start[1] = CMD_IMG_START;
            start[2] = gen;
            start[3] = kind;
            start[4] = 0;
            start[5] = 0;
            _receiver.WriteReport(start);
        }

        private void SendImage(byte kind, byte generation, Bitmap bmp, ImageFormat format)
        {
            byte[] encoded = format == ImageFormat.Png ? EncodePng(bmp) : EncodeJpeg(bmp, 80L);
            if (encoded.Length > MAX_JPEG_SIZE)
            {
                Log($"NowPlayingImg: encoded image too large ({encoded.Length} bytes), skipping kind {kind}");
                return;
            }

            var start = new byte[REPORT_SIZE];
            start[1] = CMD_IMG_START;
            start[2] = generation;
            start[3] = kind;
            start[4] = (byte)(encoded.Length & 0xFF);
            start[5] = (byte)((encoded.Length >> 8) & 0xFF);
            if (!_receiver.WriteReport(start))
            {
                Log("NowPlayingImg: start write failed");
                return;
            }

            int offset = 0;
            while (offset < encoded.Length)
            {
                int n = Math.Min(CHUNK_DATA_SIZE, encoded.Length - offset);
                var chunk = new byte[REPORT_SIZE];
                chunk[1] = CMD_IMG_CHUNK;
                chunk[2] = generation;
                chunk[3] = kind;
                System.Buffer.BlockCopy(encoded, offset, chunk, 4, n);
                if (!_receiver.WriteReport(chunk))
                {
                    Log("NowPlayingImg: chunk write failed, aborting transfer");
                    return;
                }
                offset += n;
            }

            var end = new byte[REPORT_SIZE];
            end[1] = CMD_IMG_END;
            end[2] = generation;
            end[3] = kind;
            _receiver.WriteReport(end);

            int chunkCount = (encoded.Length + CHUNK_DATA_SIZE - 1) / CHUNK_DATA_SIZE;
            Log($"NowPlayingImg: sent kind {kind}, {encoded.Length} bytes ({format.ToString()}) in {chunkCount} chunks");
        }

        private static byte[] EncodePng(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private static byte[] EncodeJpeg(Bitmap bmp, long quality)
        {
            var codec = GetEncoder(ImageFormat.Jpeg);
            var parms = new EncoderParameters(1);
            parms.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, codec, parms);
                return ms.ToArray();
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageDecoders())
                if (codec.FormatID == format.Guid) return codec;
            return null;
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        // ------------------------------------------------------------------
        // IDisposable
        // ------------------------------------------------------------------

        public void Dispose()
        {
            _watcher.OnMediaPropertiesChanged -= OnMediaPropertiesChanged;
        }
    }
}
