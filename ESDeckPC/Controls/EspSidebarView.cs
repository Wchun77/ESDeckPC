using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// The ESP's Deck / Monitor sidebar as an editing control: the page list
    /// of the editor, drawn the way the device shows it (EspSidebarRenderer).
    ///   left click      select a page / the gear (ItemClick)
    ///   double click    ItemDoubleClick (the editors rename)
    ///   right click     ItemMenu, with the screen point for a context menu
    ///   drag            reorder pages (ItemMoved); the first FixedLeading
    ///                   items (Monitor's Clock) stay put
    ///   "+" slot        after the last page while AllowAdd (AddClick)
    ///   wheel           scrolls when the pages don't fit (Deck)
    /// Index values: page index, or EspSidebarRenderer.HitGear for the gear.
    /// </summary>
    public class EspSidebarView : Control
    {
        private List<SidebarItem> _items = new List<SidebarItem>();
        private string _gearIcon;
        private int _selected = -1;
        private bool _gearSelected;
        private int _scroll;

        private int _downIdx = EspSidebarRenderer.HitNone;
        private Point _downPt;
        private bool _dragging;
        private int _dropAt = -1;   // insertion slot while dragging

        public int FixedLeading { get; set; }
        public bool AllowAdd { get; set; }

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemDoubleClick;
        public event EventHandler<SidebarMenuEventArgs> ItemMenu;
        public event EventHandler<SidebarMoveEventArgs> ItemMoved;
        public event EventHandler AddClick;

        public EspSidebarView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(0x11, 0x11, 0x11);
            Size = new Size(EspSidebarRenderer.Width, EspSidebarRenderer.Height);
        }

        private int SlotCount => _items.Count + (AllowAdd ? 1 : 0);

        /// <summary>Redraw with these pages; scrolls so the selected one is visible.</summary>
        public void SetItems(IList<SidebarItem> items, string gearIcon, int selected, bool gearSelected)
        {
            _items = new List<SidebarItem>(items);
            _gearIcon = gearIcon;
            _selected = selected;
            _gearSelected = gearSelected;
            _scroll = EspSidebarRenderer.ScrollToShow(_scroll, gearSelected ? -1 : selected, SlotCount);
            Invalidate();
        }

        // ------------------------------------------------------------------
        // Painting
        // ------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var bmp = EspSidebarRenderer.RenderPages(_items, _gearIcon, _selected, _gearSelected, _scroll))
                e.Graphics.DrawImageUnscaled(bmp, 0, 0);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.SetClip(new Rectangle(0, 0, Width, EspSidebarRenderer.ColumnH));
            if (AllowAdd)
            {
                var r = SlotRect(_items.Count);
                using (var pen = new Pen(Color.FromArgb(0x66, 0x66, 0x66), 2) { DashStyle = DashStyle.Dash })
                using (var path = Rounded(r, 8))
                    g.DrawPath(pen, path);
                using (var f = new Font("Segoe UI", 26f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(Color.FromArgb(0x88, 0x88, 0x88)))
                using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("+", f, b, r, fmt);
            }
            if (_dragging && _dropAt >= 0)
            {
                int y = EspSidebarRenderer.Pad + _dropAt * EspSidebarRenderer.Step - _scroll - EspSidebarRenderer.Gap / 2 - 1;
                using (var b = new SolidBrush(Color.FromArgb(0x00, 0xd4, 0xff)))
                    g.FillRectangle(b, 4, y, Width - 8, 3);
            }
            g.ResetClip();
        }

        private Rectangle SlotRect(int i) =>
            new Rectangle(EspSidebarRenderer.Pad, EspSidebarRenderer.Pad + i * EspSidebarRenderer.Step - _scroll,
                          EspSidebarRenderer.Btn, EspSidebarRenderer.Btn);

        // ------------------------------------------------------------------
        // Mouse
        // ------------------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();   // for the wheel
            _downIdx = EspSidebarRenderer.HitPages(e.Location, SlotCount, _scroll);
            _downPt = e.Location;
            _dragging = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button != MouseButtons.Left || !CanDrag(_downIdx)) return;
            if (!_dragging && Math.Abs(e.Y - _downPt.Y) + Math.Abs(e.X - _downPt.X) < 5) return;
            _dragging = true;

            // keep scrolling while the pointer sits near either end
            if (e.Y < 16) Scroll(-12);
            else if (e.Y > EspSidebarRenderer.ColumnH - 16 && e.Y < EspSidebarRenderer.ColumnH) Scroll(12);

            int slot = (e.Y + _scroll - EspSidebarRenderer.Pad + EspSidebarRenderer.Step / 2) / EspSidebarRenderer.Step;
            _dropAt = Math.Max(FixedLeading, Math.Min(_items.Count, slot));
            Cursor = Cursors.SizeNS;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int down = _downIdx;
            _downIdx = EspSidebarRenderer.HitNone;
            Cursor = Cursors.Default;

            if (_dragging)
            {
                _dragging = false;
                int to = _dropAt > down ? _dropAt - 1 : _dropAt;
                _dropAt = -1;
                Invalidate();
                if (to != down && to >= FixedLeading) ItemMoved?.Invoke(this, new SidebarMoveEventArgs(down, to));
                return;
            }

            int hit = EspSidebarRenderer.HitPages(e.Location, SlotCount, _scroll);
            if (hit != down || hit == EspSidebarRenderer.HitNone) return;
            bool add = AllowAdd && hit == _items.Count;
            if (e.Button == MouseButtons.Left)
            {
                if (add) AddClick?.Invoke(this, EventArgs.Empty);
                else ItemClick?.Invoke(this, hit);
            }
            else if (e.Button == MouseButtons.Right && !add)
                ItemMenu?.Invoke(this, new SidebarMenuEventArgs(hit, PointToScreen(e.Location)));
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            int hit = EspSidebarRenderer.HitPages(e.Location, _items.Count, _scroll);
            if (hit >= 0) ItemDoubleClick?.Invoke(this, hit);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Scroll(-e.Delta / 120 * EspSidebarRenderer.Step);
        }

        private bool CanDrag(int idx) => idx >= FixedLeading && idx < _items.Count && _items.Count - FixedLeading > 1;

        private void Scroll(int dy)
        {
            int max = Math.Max(0, EspSidebarRenderer.ColumnContentH(SlotCount) - EspSidebarRenderer.ColumnH);
            int s = Math.Max(0, Math.Min(max, _scroll + dy));
            if (s == _scroll) return;
            _scroll = s;
            Invalidate();
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
    }

    public sealed class SidebarMenuEventArgs : EventArgs
    {
        public int Index { get; }
        public Point ScreenPoint { get; }
        public SidebarMenuEventArgs(int index, Point screenPoint) { Index = index; ScreenPoint = screenPoint; }
    }

    public sealed class SidebarMoveEventArgs : EventArgs
    {
        public int From { get; }
        public int To { get; }
        public SidebarMoveEventArgs(int from, int to) { From = from; To = to; }
    }
}
