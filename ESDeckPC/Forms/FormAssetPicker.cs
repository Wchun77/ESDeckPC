using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Thumbnail picker for one assets folder (side_icons, backgrounds,
    /// icons): every image shown, filter by name, double-click or OK to pick.
    /// "Other file..." still opens a normal file dialog for a file elsewhere.
    /// Use Pick(); it falls back to the file dialog when the folder isn't
    /// known (no config opened from the SD card yet).
    /// </summary>
    public partial class FormAssetPicker : Form
    {
        private readonly string _dir;
        private readonly string _filter;          // OpenFileDialog filter for "Other file..."
        private readonly List<string> _names = new List<string>();
        private string _result;

        private readonly Size _thumb;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int LVM_SETICONSPACING = 0x1000 + 53;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(Handle, 20, ref v, sizeof(int));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetWindowTheme(lvAssets.Handle, "DarkMode_Explorer", null);   // dark scroll bar, themed selection
            FitColumns();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            lvAssets.Focus();   // selection drawn as active, not the gray "no focus" look
        }

        /// <summary>
        /// Spread the columns over the whole width: as many cells of at least
        /// the thumbnail + room for the name as fit, then share out what's left
        /// (LargeIcon's fixed spacing otherwise leaves a strip on the right).
        /// </summary>
        private void FitColumns()
        {
            if (!lvAssets.IsHandleCreated) return;
            int avail = lvAssets.ClientSize.Width;
            int min = Math.Max(_thumb.Width + 28, 110);
            int cols = Math.Max(1, avail / min);
            int cx = avail / cols, cy = _thumb.Height + 44;
            SendMessage(lvAssets.Handle, LVM_SETICONSPACING, IntPtr.Zero, (IntPtr)((cy << 16) | (cx & 0xFFFF)));
            lvAssets.ArrangeIcons();
        }

        private FormAssetPicker(string title, string dir, string[] patterns, string fileFilter, Size thumb, string current)
        {
            InitializeComponent();
            StyleDarkButtons();
            Text = title;
            _dir = dir;
            _filter = fileFilter;
            _thumb = thumb;
            lblFolder.Text = dir;

            var images = new ImageList { ImageSize = thumb, ColorDepth = ColorDepth.Depth32Bit };
            lvAssets.LargeImageList = images;
            foreach (var path in patterns.SelectMany(p => Directory.GetFiles(dir, p)).Distinct(StringComparer.OrdinalIgnoreCase)
                                         .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(path);
                images.Images.Add(name, Thumbnail(path, thumb));
                _names.Add(name);
            }
            Fill("");
            SelectName(current);

            txtFilter.TextChanged += (s, e) => Fill(txtFilter.Text.Trim());
            lvAssets.ItemActivate += (s, e) => Accept();
            lvAssets.ClientSizeChanged += (s, e) => FitColumns();
            lvAssets.SelectedIndexChanged += (s, e) => btnOk.Enabled = lvAssets.SelectedItems.Count > 0;
            btnOk.Click += (s, e) => Accept();
            btnOther.Click += BtnOther_Click;
            btnOk.Enabled = lvAssets.SelectedItems.Count > 0;
        }

        private void Fill(string filter)
        {
            lvAssets.BeginUpdate();
            lvAssets.Items.Clear();
            foreach (var n in _names)
                if (filter.Length == 0 || n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    lvAssets.Items.Add(new ListViewItem(n, n) { Name = n });
            lvAssets.EndUpdate();
            FitColumns();
            lblCount.Text = $"{lvAssets.Items.Count} / {_names.Count}";
        }

        private void SelectName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var it = lvAssets.Items[name];
            if (it == null) return;
            it.Selected = true;
            it.Focused = true;
            it.EnsureVisible();
        }

        private void Accept()
        {
            if (lvAssets.SelectedItems.Count == 0) return;
            _result = Path.Combine(_dir, lvAssets.SelectedItems[0].Name);
            DialogResult = DialogResult.OK;
        }

        private void BtnOther_Click(object sender, EventArgs e)
        {
            string picked = BrowseFile(this, Text, _filter, _dir);
            if (picked == null) return;
            _result = picked;
            DialogResult = DialogResult.OK;
        }

        /// <summary>The image fitted (aspect kept) on a dark tile the size of the thumbnail.</summary>
        private static Bitmap Thumbnail(string path, Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0x2a, 0x2a, 0x2a));
                try
                {
                    using (var ms = new MemoryStream(File.ReadAllBytes(path)))
                    using (var img = Image.FromStream(ms))
                    {
                        float k = Math.Min(1f, Math.Min((float)size.Width / img.Width, (float)size.Height / img.Height));
                        int w = Math.Max(1, (int)(img.Width * k)), h = Math.Max(1, (int)(img.Height * k));
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(img, (size.Width - w) / 2, (size.Height - h) / 2, w, h);
                    }
                }
                catch
                {
                    using (var pen = new Pen(Color.OrangeRed, 2))
                    {
                        g.DrawLine(pen, 8, 8, size.Width - 8, size.Height - 8);
                        g.DrawLine(pen, size.Width - 8, 8, 8, size.Height - 8);
                    }
                }
            }
            return bmp;
        }

        private static string BrowseFile(IWin32Window owner, string title, string filter, string dir)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = title;
                dlg.Filter = filter;
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) dlg.InitialDirectory = dir;
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.FileName : null;
            }
        }

        private void StyleDarkButtons()
        {
            foreach (var btn in new[] { btnOk, btnCancel, btnOther })
            {
                btn.BackColor = Color.FromArgb(55, 55, 58);
                btn.ForeColor = Color.FromArgb(220, 220, 220);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                btn.Font = new Font("Consolas", 8.5f);
            }
        }

        // ------------------------------------------------------------------
        // Entry points
        // ------------------------------------------------------------------

        /// <summary>
        /// Full path of the picked file, or null if cancelled. dir = the
        /// assets folder (null / missing: plain file dialog instead).
        /// patterns e.g. "*.png" or "*.jpg;*.jpeg".
        /// </summary>
        public static string Pick(IWin32Window owner, string title, string dir, string patterns, Size thumb, string current)
        {
            var pats = patterns.Split(';');
            string filter = $"Image ({patterns})|{patterns}";
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return BrowseFile(owner, title, filter, null);
            using (var dlg = new FormAssetPicker(title, dir, pats, filter, thumb, current))
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg._result : null;
        }

        public static readonly Size SideIconThumb = new Size(64, 64);
        public static readonly Size BackgroundThumb = new Size(150, 100);
        public static readonly Size IconThumb = new Size(80, 80);
    }
}
