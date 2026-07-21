using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Self-drawing deck preview control. Mirrors ui_deck.c layout:
    ///   - Background image: zoom-fill + 50% black mask
    ///   - Buttons: LV_FLEX_FLOW_ROW_WRAP, 160x150 each,
    ///              pad_all=8, pad_row=10, pad_col=10, radius=10, bg_opa=50%
    ///   - Icon: 100x100 centered, label below
    ///   - Right-click: context menu (Edit / Add Button / Clear Button)
    ///   - Left drag: reorder buttons
    /// </summary>
    public class DeckPreviewPanel : Control
    {
        // ------------------------------------------------------------------
        // Firmware constants (mirrored from ui_deck.c)
        // ------------------------------------------------------------------

        private const int PanelW = 720;
        private const int PanelH = 480;
        private const int BtnW = 160;
        private const int BtnH = 150;
        private const int PadAll = 8;
        private const int PadRow = 10;
        private const int PadCol = 10;
        private const int BtnRadius = 10;
        private const int IconSize = 100;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private PcPage _page = null;
        private Bitmap _bgBitmap = null;
        private string _assetsIconsDir = null;

        /// <summary>
        /// When false, the button grid/drag/context-menu-"Add Button" affordances
        /// are suppressed -- used for the Settings entry, which only ever has a
        /// background image (no buttons in the schema). SetPage() itself doesn't
        /// need to change: passing a page with an empty Buttons list already
        /// draws background-only; this flag just stops the user from adding one.
        /// </summary>
        public bool AllowButtons { get; set; } = true;

        // Icon bitmap cache: key = icon filename
        private readonly Dictionary<string, Bitmap> _iconCache = new Dictionary<string, Bitmap>();

        // Drag state
        private int _dragIndex = -1;
        private Point _dragStartPoint = Point.Empty;
        private int _dropInsertIndex = -1; // insertion point (0 = before first, Count = after last)

        // ------------------------------------------------------------------
        // Events
        // ------------------------------------------------------------------

        /// <summary>Fired when the user right-clicks Edit on a button.</summary>
        public event EventHandler<int> EditButtonRequested;

        /// <summary>Fired when the user right-clicks Add Button.</summary>
        public event EventHandler AddButtonRequested;

        /// <summary>Fired when the user right-clicks Clear Button on a button.</summary>
        public event EventHandler<int> ClearButtonRequested;

        /// <summary>Fired after a drag-reorder completes; buttons list already reordered.</summary>
        public event EventHandler ReorderCompleted;

        /// <summary>Fired when the user left-clicks a button (for preview action execution).</summary>
        public event EventHandler<int> ButtonClicked;

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------

        public DeckPreviewPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Size = new Size(PanelW, PanelH);
            BackColor = Color.FromArgb(0x22, 0x22, 0x22);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Binds a page and redraws. backgroundsDir and iconsDir may be null
        /// (filenames still shown as placeholder if bitmaps cannot be resolved).
        /// </summary>
        public void SetPage(PcPage page, string backgroundsDir, string iconsDir)
        {
            _page = page;
            _assetsIconsDir = iconsDir;

            _bgBitmap?.Dispose();
            _bgBitmap = null;

            ClearIconCache();

            if (!string.IsNullOrEmpty(backgroundsDir) && page != null &&
                !string.IsNullOrEmpty(page.BgImage))
            {
                var path = Path.Combine(backgroundsDir, page.BgImage);
                if (File.Exists(path))
                {
                    try { _bgBitmap = new Bitmap(path); }
                    catch { _bgBitmap = null; }
                }
            }

            Invalidate();
        }

        // ------------------------------------------------------------------
        // Painting
        // ------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            DrawBackground(g);

            if (_page?.Buttons == null) return;

            for (int i = 0; i < _page.Buttons.Count; i++)
            {
                var rect = ButtonRect(i);
                bool isDragging = (i == _dragIndex);

                // Insertion markers: left/right neighbours of drop point,
                // but never override the dragged button's own highlight.
                bool isInsertLeft = !isDragging
                    && _dropInsertIndex > 0
                    && i == _dropInsertIndex - 1;
                bool isInsertRight = !isDragging
                    && _dropInsertIndex >= 0
                    && i == _dropInsertIndex
                    && _dropInsertIndex < _page.Buttons.Count;

                DrawButton(g, rect, _page.Buttons[i], isDragging, isInsertLeft, isInsertRight);
            }
        }

        private void DrawBackground(Graphics g)
        {
            if (_bgBitmap != null)
            {
                float zx = (float)PanelW / _bgBitmap.Width;
                float zy = (float)PanelH / _bgBitmap.Height;
                float z = Math.Max(zx, zy);
                int sw = (int)(_bgBitmap.Width * z);
                int sh = (int)(_bgBitmap.Height * z);
                int ox = (PanelW - sw) / 2;
                int oy = (PanelH - sh) / 2;
                g.DrawImage(_bgBitmap, ox, oy, sw, sh);

                // 50% black mask only when a background image is present (LV_OPA_50)
                using (var br = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                    g.FillRectangle(br, 0, 0, PanelW, PanelH);
            }
            else
            {
                // No background: plain #222222, no mask (mirrors ESP behavior)
                g.Clear(Color.FromArgb(0x22, 0x22, 0x22));
            }
        }

        private void DrawButton(Graphics g, Rectangle rect, PcButton btn,
                                 bool isDragging, bool isInsertLeft, bool isInsertRight)
        {
            // Button background: #2d2d2d @ 50% (LV_OPA_50), dimmer when dragging
            byte bgAlpha = isDragging ? (byte)60 : (byte)128;
            using (var br = new SolidBrush(Color.FromArgb(bgAlpha, 0x2d, 0x2d, 0x2d)))
            using (var path = RoundedRect(rect, BtnRadius))
                g.FillPath(br, path);

            // Thin border: LVGL default theme adds a grey border to lv_btn
            using (var path = RoundedRect(rect, BtnRadius))
            using (var pen = new Pen(Color.FromArgb(80, 0x9E, 0x9E, 0x9E), 1))
                g.DrawPath(pen, path);

            // Dragged button: red border
            if (isDragging)
            {
                using (var path = RoundedRect(rect, BtnRadius))
                using (var pen = new Pen(Color.FromArgb(200, 50, 50), 2))
                    g.DrawPath(pen, path);
            }

            // Insertion point neighbours: blue border
            if (isInsertLeft || isInsertRight)
            {
                using (var path = RoundedRect(rect, BtnRadius))
                using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
                    g.DrawPath(pen, path);
            }

            bool hasIcon = !string.IsNullOrEmpty(btn?.Icon);
            Bitmap iconBmp = hasIcon ? ResolveIcon(btn.Icon) : null;

            if (iconBmp != null)
            {
                // Scale proportionally to fit within IconSize x IconSize
                float scale = Math.Min((float)IconSize / iconBmp.Width,
                                       (float)IconSize / iconBmp.Height);
                int drawW = (int)(iconBmp.Width * scale);
                int drawH = (int)(iconBmp.Height * scale);
                int iconX = rect.X + (BtnW - drawW) / 2;
                int iconY = rect.Y + (BtnH - drawH) / 2 - 14;
                g.DrawImage(iconBmp, iconX, iconY, drawW, drawH);
            }

            // Label (#cccccc)
            string label = btn?.Label ?? "";
            using (var font = hasIcon
                ? new Font("Segoe UI", 8.5f)
                : new Font("Segoe UI", 13f))
            using (var br = new SolidBrush(Color.FromArgb(0xCC, 0xCC, 0xCC)))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = hasIcon ? StringAlignment.Far : StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                };
                var textRect = new RectangleF(rect.X + 4, rect.Y + 4, BtnW - 8, BtnH - 8);
                g.DrawString(label, font, br, textRect, sf);
            }
        }

        // ------------------------------------------------------------------
        // Mouse events
        // ------------------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int idx = HitTest(e.Location);
            if (idx < 0) return;

            _dragIndex = idx;
            _dragStartPoint = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button != MouseButtons.Left || _dragIndex < 0) return;

            int dx = Math.Abs(e.X - _dragStartPoint.X);
            int dy = Math.Abs(e.Y - _dragStartPoint.Y);
            if (dx < SystemInformation.DragSize.Width &&
                dy < SystemInformation.DragSize.Height) return;

            // Start drag
            int dropIdx = FindDropIndex(e.Location);
            if (dropIdx != _dropInsertIndex)
            {
                _dropInsertIndex = dropIdx;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left && _dragIndex >= 0)
            {
                int dx = Math.Abs(e.X - _dragStartPoint.X);
                int dy = Math.Abs(e.Y - _dragStartPoint.Y);
                bool wasDrag = dx >= SystemInformation.DragSize.Width ||
                               dy >= SystemInformation.DragSize.Height;

                if (!wasDrag)
                {
                    // Short click, not a drag: fire ButtonClicked
                    int idx = HitTest(e.Location);
                    if (idx >= 0)
                        ButtonClicked?.Invoke(this, idx);

                    _dragIndex = -1;
                    _dropInsertIndex = -1;
                    Invalidate();
                    return;
                }

                int insertIdx = FindDropIndex(e.Location);
                if (insertIdx >= 0 && _page?.Buttons != null)
                {
                    int adjustedInsert = insertIdx > _dragIndex ? insertIdx - 1 : insertIdx;
                    adjustedInsert = Math.Max(0, Math.Min(adjustedInsert, _page.Buttons.Count - 1));

                    if (adjustedInsert != _dragIndex)
                    {
                        var btn = _page.Buttons[_dragIndex];
                        _page.Buttons.RemoveAt(_dragIndex);
                        _page.Buttons.Insert(adjustedInsert, btn);
                        ReorderCompleted?.Invoke(this, EventArgs.Empty);
                    }
                }

                _dragIndex = -1;
                _dropInsertIndex = -1;
                Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                int idx = HitTest(e.Location);
                ShowContextMenu(idx, e.Location);
            }
        }

        // ------------------------------------------------------------------
        // Context menu
        // ------------------------------------------------------------------

        private void ShowContextMenu(int btnIdx, Point location)
        {
            var darkBg = Color.FromArgb(45, 45, 48);
            var darkFg = Color.FromArgb(220, 220, 220);

            var cms = new ContextMenuStrip
            {
                BackColor = darkBg,
                ForeColor = darkFg,
                Renderer = new ToolStripProfessionalRenderer(new DeckDarkMenuColors()),
                ShowImageMargin = false,
            };

            if (btnIdx >= 0)
            {
                var miEdit = new ToolStripMenuItem("Edit") { ForeColor = darkFg };
                miEdit.Click += (s, e) => EditButtonRequested?.Invoke(this, btnIdx);
                cms.Items.Add(miEdit);

                var miClear = new ToolStripMenuItem("Clear Button") { ForeColor = Color.FromArgb(220, 80, 80) };
                miClear.Click += (s, e) => ClearButtonRequested?.Invoke(this, btnIdx);
                cms.Items.Add(miClear);

                cms.Items.Add(new ToolStripSeparator());
            }

            if (AllowButtons)
            {
                var miAdd = new ToolStripMenuItem("Add Button") { ForeColor = darkFg };
                miAdd.Click += (s, e) => AddButtonRequested?.Invoke(this, EventArgs.Empty);
                cms.Items.Add(miAdd);
            }

            if (cms.Items.Count == 0) return;   // nothing applicable (e.g. Settings preview, empty area)

            cms.Show(this, location);
        }

        private sealed class DeckDarkMenuColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(60, 60, 64);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 64);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 64);
            public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
            public override Color MenuBorder => Color.FromArgb(80, 80, 80);
            public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
        }

        // ------------------------------------------------------------------
        // Layout helpers
        // ------------------------------------------------------------------

        private Rectangle ButtonRect(int index)
        {
            // Mirrors LV_FLEX_FLOW_ROW_WRAP with pad_all=8, pad_col=10, pad_row=10
            int cols = Math.Max(1, (PanelW - PadAll * 2 + PadCol) / (BtnW + PadCol));
            int col = index % cols;
            int row = index / cols;
            int x = PadAll + col * (BtnW + PadCol);
            int y = PadAll + row * (BtnH + PadRow);
            return new Rectangle(x, y, BtnW, BtnH);
        }

        private int HitTest(Point p)
        {
            if (_page?.Buttons == null) return -1;
            for (int i = 0; i < _page.Buttons.Count; i++)
            {
                if (ButtonRect(i).Contains(p)) return i;
            }
            return -1;
        }

        private int FindDropIndex(Point p)
        {
            if (_page?.Buttons == null || _page.Buttons.Count == 0) return 0;

            int cols = Math.Max(1, (PanelW - PadAll * 2 + PadCol) / (BtnW + PadCol));

            for (int i = 0; i < _page.Buttons.Count; i++)
            {
                var r = ButtonRect(i);
                int midX = r.X + r.Width / 2;
                int midY = r.Y + r.Height / 2;

                int col = i % cols;
                int row = i / cols;

                // If on same row and to the left of midpoint, insert before this
                int btnRow = (p.Y - PadAll) / (BtnH + PadRow);
                if (btnRow == row && p.X < midX)
                    return i;
            }
            return _page.Buttons.Count;
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

        // ------------------------------------------------------------------
        // Icon cache
        // ------------------------------------------------------------------

        private Bitmap ResolveIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            if (_iconCache.TryGetValue(fileName, out var cached)) return cached;

            if (!string.IsNullOrEmpty(_assetsIconsDir))
            {
                var path = Path.Combine(_assetsIconsDir, fileName);
                if (File.Exists(path))
                {
                    try
                    {
                        var bmp = new Bitmap(path);
                        _iconCache[fileName] = bmp;
                        return bmp;
                    }
                    catch { }
                }
            }

            _iconCache[fileName] = null; // cache miss to avoid repeated disk access
            return null;
        }

        private void ClearIconCache()
        {
            foreach (var bmp in _iconCache.Values)
                bmp?.Dispose();
            _iconCache.Clear();
        }

        // ------------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bgBitmap?.Dispose();
                ClearIconCache();
            }
            base.Dispose(disposing);
        }
    }
}