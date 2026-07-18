using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Dual-thumb range slider with a thumbnail-strip background, iOS-style
    /// video trim control. The host decodes and supplies thumbnails via
    /// SetThumbnails(); this control only lays them out and handles
    /// thumb/pan dragging. See "ESDeck開機動畫轉檔工具-規劃.md" section 2.
    /// </summary>
    public class DualRangeSlider : Control
    {
        private const int ThumbW = 12;
        private const int TrackPad = 4;

        private double _totalSec = 1.0;
        private double _startSec = 0.0;
        private double _endSec = 1.0;

        private readonly List<Bitmap> _thumbs = new List<Bitmap>();

        private enum DragMode { None, Start, End, Pan, Playhead }
        private DragMode _drag = DragMode.None;
        private int _dragStartX;
        private double _panStartStart, _panStartEnd;

        private enum SelectedThumb { Start, End }

        // Which thumb keyboard nudging (arrow keys) applies to -- 'A'
        // selects Start, 'D' selects End, dragging a thumb directly also
        // selects it. Defaults to Start so arrow keys work immediately
        // without requiring an A/D press first.
        private SelectedThumb _selectedThumb = SelectedThumb.Start;

        public double TotalDurationSec
        {
            get => _totalSec;
            set { _totalSec = Math.Max(0.01, value); ClampRange(); Invalidate(); }
        }

        public double StartSec => _startSec;
        public double EndSec => _endSec;

        /// <summary>True if the End thumb is the one keyboard/last-drag actions target.</summary>
        public bool IsEndSelected => _selectedThumb == SelectedThumb.End;

        private double _currentSec = -1;

        /// <summary>
        /// Current playback position, drawn as a vertical playhead line.
        /// Set to a negative value (default) to hide it.
        /// </summary>
        public double CurrentSec
        {
            get => _currentSec;
            set { _currentSec = value; Invalidate(); }
        }

        /// <summary>Fired while dragging (every mouse move) and once more on mouse-up.</summary>
        public event EventHandler RangeChanged;

        /// <summary>
        /// Fired continuously while the playhead is being dragged (see
        /// OnMouseDown/OnMouseMove), carrying the requested position in
        /// seconds -- the host should seek its preview player to follow it
        /// live, like a normal video scrubber.
        /// </summary>
        public event EventHandler<double> SeekRequested;

        public DualRangeSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(24, 24, 24);
            Height = 72;
            TabStop = true;
        }

        // --------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------

        public void SetRange(double startSec, double endSec)
        {
            _startSec = Math.Max(0, startSec);
            _endSec = Math.Min(_totalSec, endSec);
            if (_endSec < _startSec) _endSec = _startSec;
            Invalidate();
        }

        /// <summary>
        /// Moves the right (end) thumb to the current playback position
        /// (CurrentSec) -- lets the host wire up a "set out-point here"
        /// button/shortcut for whatever's currently playing, instead of
        /// having to drag the thumb back to match afterwards. No-op if
        /// CurrentSec hasn't been set yet (nothing has played).
        /// </summary>
        public void SetEndToCurrent()
        {
            if (_currentSec < 0) return;
            _endSec = Math.Max(_startSec, Math.Min(_totalSec, _currentSec));
            _selectedThumb = SelectedThumb.End;
            Invalidate();
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Moves the playhead to the given position and asks the host to
        /// seek there via SeekRequested. Used by the draggable playhead
        /// (see OnMouseDown/OnMouseMove) -- unlike SetRange/nudging, this
        /// never touches Start/End, so it doesn't fire RangeChanged.
        /// </summary>
        private void SeekTo(double sec)
        {
            _currentSec = Math.Max(0, Math.Min(_totalSec, sec));
            Invalidate();
            SeekRequested?.Invoke(this, _currentSec);
        }

        /// <summary>
        /// Replaces the thumbnail strip. Ownership transfers to this
        /// control -- previous bitmaps are disposed here.
        /// </summary>
        public void SetThumbnails(IEnumerable<Bitmap> thumbs)
        {
            foreach (var b in _thumbs) b?.Dispose();
            _thumbs.Clear();
            if (thumbs != null) _thumbs.AddRange(thumbs);
            Invalidate();
        }

        // --------------------------------------------------------------
        // Layout helpers
        // --------------------------------------------------------------

        private int TrackLeft => ThumbW / 2;
        private int TrackRight => Width - ThumbW / 2;
        private int TrackWidth => Math.Max(1, TrackRight - TrackLeft);

        private double SecToX(double sec) => TrackLeft + (sec / _totalSec) * TrackWidth;
        private double XToSec(double x) => Math.Max(0, Math.Min(_totalSec, (x - TrackLeft) / TrackWidth * _totalSec));

        private void ClampRange()
        {
            if (_endSec > _totalSec) _endSec = _totalSec;
            if (_startSec > _endSec) _startSec = _endSec;
        }

        // --------------------------------------------------------------
        // Painting
        // --------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(BackColor);

            var trackRect = new Rectangle(TrackLeft, TrackPad, TrackWidth, Height - TrackPad * 2);

            if (_thumbs.Count > 0)
            {
                int segW = Math.Max(1, trackRect.Width / _thumbs.Count);
                for (int i = 0; i < _thumbs.Count; i++)
                {
                    var thumb = _thumbs[i];
                    if (thumb == null) continue;
                    int x = trackRect.X + i * segW;
                    int w = (i == _thumbs.Count - 1) ? Math.Max(1, trackRect.Right - x) : segW;
                    g.DrawImage(thumb, new Rectangle(x, trackRect.Y, w, trackRect.Height));
                }
            }
            else
            {
                using (var br = new SolidBrush(Color.FromArgb(45, 45, 48)))
                    g.FillRectangle(br, trackRect);
            }

            int startX = (int)SecToX(_startSec);
            int endX = (int)SecToX(_endSec);

            // Darken outside the selected range (same 50%-black-mask
            // language used elsewhere in this app's dark theme).
            using (var dim = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                if (startX > trackRect.X)
                    g.FillRectangle(dim, trackRect.X, trackRect.Y, startX - trackRect.X, trackRect.Height);
                if (endX < trackRect.Right)
                    g.FillRectangle(dim, endX, trackRect.Y, trackRect.Right - endX, trackRect.Height);
            }

            using (var pen = new Pen(Color.FromArgb(0, 150, 255), 2))
                g.DrawRectangle(pen, startX, trackRect.Y, Math.Max(1, endX - startX), trackRect.Height);

            DrawThumb(g, startX, Focused && _selectedThumb == SelectedThumb.Start);
            DrawThumb(g, endX, Focused && _selectedThumb == SelectedThumb.End);

            if (_currentSec >= 0 && _currentSec <= _totalSec)
            {
                int px = (int)SecToX(_currentSec);
                using (var playPen = new Pen(Color.Yellow, 2))
                    g.DrawLine(playPen, px, 0, px, Height);
                using (var br = new SolidBrush(Color.Yellow))
                {
                    var tri = new[]
                    {
                        new Point(px - 4, 0),
                        new Point(px + 4, 0),
                        new Point(px, 6),
                    };
                    g.FillPolygon(br, tri);
                }
            }
        }

        private void DrawThumb(Graphics g, int x, bool selected)
        {
            var rect = new Rectangle(x - ThumbW / 2, 0, ThumbW, Height);
            using (var br = new SolidBrush(Color.FromArgb(0, 150, 255)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(selected ? Color.Yellow : Color.White, selected ? 2 : 1))
                g.DrawRectangle(pen, rect);
        }

        // --------------------------------------------------------------
        // Mouse
        // --------------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus(); // grab keyboard focus so A/D/arrow-key nudging works right after a click

            if (e.Button != MouseButtons.Left) return;

            int startX = (int)SecToX(_startSec);
            int endX = (int)SecToX(_endSec);
            bool hasPlayhead = _currentSec >= 0;
            int playheadX = hasPlayhead ? (int)SecToX(_currentSec) : int.MinValue;

            if (Math.Abs(e.X - startX) <= ThumbW)
            {
                _drag = DragMode.Start;
                _selectedThumb = SelectedThumb.Start;
                Invalidate();
            }
            else if (Math.Abs(e.X - endX) <= ThumbW)
            {
                _drag = DragMode.End;
                _selectedThumb = SelectedThumb.End;
                Invalidate();
            }
            else if (hasPlayhead && Math.Abs(e.X - playheadX) <= ThumbW)
            {
                // Grabbing the playhead line itself -- pure scrub, doesn't
                // touch the trim range at all.
                _drag = DragMode.Playhead;
                SeekTo(XToSec(e.X));
            }
            else if (e.X > startX && e.X < endX)
            {
                // Clicking anywhere else inside the highlighted range also
                // scrubs the playhead there (like dragging the middle of a
                // normal video-editor timeline), while a full Pan (moving
                // the trim range while keeping its width) still happens as
                // you drag from here.
                _drag = DragMode.Pan;
                _dragStartX = e.X;
                _panStartStart = _startSec;
                _panStartEnd = _endSec;
                SeekTo(XToSec(e.X));
            }
            else
            {
                _drag = DragMode.None;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag == DragMode.None) return;

            switch (_drag)
            {
                case DragMode.Start:
                    _startSec = Math.Min(XToSec(e.X), _endSec);
                    break;
                case DragMode.End:
                    _endSec = Math.Max(XToSec(e.X), _startSec);
                    break;
                case DragMode.Pan:
                    double deltaSec = XToSec(e.X) - XToSec(_dragStartX);
                    double span = _panStartEnd - _panStartStart;
                    double newStart = Math.Max(0, Math.Min(_totalSec - span, _panStartStart + deltaSec));
                    _startSec = newStart;
                    _endSec = newStart + span;
                    SeekTo(XToSec(e.X)); // video follows the cursor live while panning, like a scrubber
                    break;
                case DragMode.Playhead:
                    SeekTo(XToSec(e.X));
                    return; // pure scrub -- Start/End didn't change, no RangeChanged
            }

            Invalidate();
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_drag == DragMode.Playhead)
            {
                _drag = DragMode.None;
                return;
            }
            if (_drag != DragMode.None)
            {
                _drag = DragMode.None;
                RangeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var b in _thumbs) b?.Dispose();
                _thumbs.Clear();
            }
            base.Dispose(disposing);
        }

        // --------------------------------------------------------------
        // Keyboard -- long videos map to huge pixel-per-second ratios,
        // making pixel-drag precision impractical (0.1s is well under a
        // pixel on a long track). Scheme:
        //   A / D     -- select the left / right thumb (highlight only)
        //   Left/Right-- nudge the selected thumb by 1s (Shift = 5s)
        //   Z / X     -- nudge the LEFT thumb by -0.1s / +0.1s directly
        //   C / V     -- nudge the RIGHT thumb by -0.1s / +0.1s directly
        // Z/X/C/V also update the selection/highlight to match, so a
        // follow-up arrow-key nudge continues on the same thumb.
        // --------------------------------------------------------------

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.A:
                    _selectedThumb = SelectedThumb.Start;
                    Invalidate();
                    break;
                case Keys.D:
                    _selectedThumb = SelectedThumb.End;
                    Invalidate();
                    break;
                case Keys.Z:
                    NudgeThumb(SelectedThumb.Start, -0.1);
                    break;
                case Keys.X:
                    NudgeThumb(SelectedThumb.Start, 0.1);
                    break;
                case Keys.C:
                    NudgeThumb(SelectedThumb.End, -0.1);
                    break;
                case Keys.V:
                    NudgeThumb(SelectedThumb.End, 0.1);
                    break;
                case Keys.Left:
                    NudgeThumb(_selectedThumb, e.Shift ? -5.0 : -1.0);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    NudgeThumb(_selectedThumb, e.Shift ? 5.0 : 1.0);
                    e.Handled = true;
                    break;
            }
        }

        private void NudgeThumb(SelectedThumb thumb, double deltaSec)
        {
            _selectedThumb = thumb;

            if (thumb == SelectedThumb.Start)
                _startSec = Math.Max(0, Math.Min(_endSec, _startSec + deltaSec));
            else
                _endSec = Math.Min(_totalSec, Math.Max(_startSec, _endSec + deltaSec));

            Invalidate();
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate(); // show the selected-thumb highlight now that we have focus
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate(); // hide the selected-thumb highlight
        }
    }
}
