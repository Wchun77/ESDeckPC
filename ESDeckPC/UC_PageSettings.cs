using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class UC_PageSettings : UserControl
    {
        // ------------------------------------------------------------------
        // Cell value choices (must match firmware-side mon_cell_id_t names)
        // ------------------------------------------------------------------

        private static readonly string[] CellOptions =
        {
            "", // empty slot
            "cpu_usage", "cpu_temp", "cpu_freq",
            "ram_usage",
            "gpu_usage", "gpu_temp", "gpu_vram",
            "net_up", "net_down",
            "disk_usage",
            "cpu_power", "gpu_power",
            "ssd_life",
        };

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private Bitmap _bgBitmap = null;
        public Bitmap BgBitmap => _bgBitmap;

        /// <summary>
        /// The page currently bound to this control. Set by ApplyConfig.
        /// Exposed so the host form can identify which page is being edited
        /// (e.g. for rename / delete from the flpPages context menu).
        /// </summary>
        public MonitorPageCfg BoundPage { get; private set; }

        /// <summary>
        /// Raised whenever a change in this control should cause the host
        /// form to re-render the preview.
        /// </summary>
        public event EventHandler PreviewInvalidated;

        // True while ApplyConfig is in progress; see UC_ClockSettings for rationale.
        private bool _isApplying = false;

        public UC_PageSettings()
        {
            InitializeComponent();
            StyleDarkButtons();

            btnPageBgBrowse.Click += BtnPageBgBrowse_Click;
            btnPageBgClear.Click += BtnPageBgClear_Click;

            foreach (var cmb in CellCombos())
            {
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb.Items.Clear();
                cmb.Items.AddRange(CellOptions);
                cmb.SelectedIndexChanged += (s, e) => RaisePreviewInvalidated();
            }
        }

        private ComboBox[] CellCombos() => new[] { cmbCell0, cmbCell1, cmbCell2, cmbCell3 };

        // ------------------------------------------------------------------
        // Config <-> UI sync
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebinds this shared control to a different page. Call this every
        /// time the selected page button in flpPages changes.
        /// </summary>
        public void ApplyConfig(MonitorPageCfg page) => ApplyConfig(page, null);

        /// <summary>
        /// Rebinds this shared control to a different page, and additionally
        /// tries to resolve+load the background image from the given USB
        /// assets/backgrounds folder using the filename already stored in
        /// the JSON. Pass null to skip resolution (filename still shown as text).
        /// </summary>
        public void ApplyConfig(MonitorPageCfg page, string backgroundsDir)
        {
            _isApplying = true;
            try
            {
                BoundPage = page;

                txtPageName.Text = page.Name ?? "";
                txtPageBgImage.Text = page.BgImage ?? "";

                _bgBitmap?.Dispose();
                _bgBitmap = null;
                if (!string.IsNullOrEmpty(backgroundsDir) && !string.IsNullOrEmpty(page.BgImage))
                {
                    var path = Path.Combine(backgroundsDir, page.BgImage);
                    if (File.Exists(path))
                    {
                        try { _bgBitmap = new Bitmap(path); }
                        catch { _bgBitmap = null; }
                    }
                }

                var combos = CellCombos();
                for (int i = 0; i < combos.Length; i++)
                {
                    string val = (page.Cells != null && i < page.Cells.Length) ? page.Cells[i] : null;
                    SetCellCombo(combos[i], val);
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        public void ReadConfig(MonitorPageCfg page)
        {
            page.Name = txtPageName.Text;
            page.BgImage = txtPageBgImage.Text;

            var combos = CellCombos();
            if (page.Cells == null || page.Cells.Length != combos.Length)
                page.Cells = new string[combos.Length];

            for (int i = 0; i < combos.Length; i++)
            {
                string val = combos[i].SelectedItem as string;
                page.Cells[i] = string.IsNullOrEmpty(val) ? null : val;
            }
        }

        private static void SetCellCombo(ComboBox cmb, string val)
        {
            int idx = string.IsNullOrEmpty(val) ? 0 : cmb.Items.IndexOf(val);
            cmb.SelectedIndex = idx >= 0 ? idx : 0;
        }

        // ------------------------------------------------------------------
        // Background image
        // ------------------------------------------------------------------

        private void BtnPageBgBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select background image";
                dlg.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _bgBitmap?.Dispose();
                    _bgBitmap = new Bitmap(dlg.FileName);
                    txtPageBgImage.Text = Path.GetFileName(dlg.FileName);
                    RaisePreviewInvalidated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image:\n{ex.Message}",
                                   "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPageBgClear_Click(object sender, EventArgs e)
        {
            _bgBitmap?.Dispose();
            _bgBitmap = null;
            txtPageBgImage.Text = "";
            RaisePreviewInvalidated();
        }

        // ------------------------------------------------------------------
        // Dark styling (cannot go inside InitializeComponent)
        // ------------------------------------------------------------------

        private void StyleDarkButtons()
        {
            var darkBg = Color.FromArgb(55, 55, 58);
            var darkBorder = Color.FromArgb(80, 80, 80);
            var lightFg = Color.FromArgb(220, 220, 220);
            var font = new Font("Consolas", 8.5f);

            foreach (var btn in new[] { btnPageBgBrowse, btnPageBgClear })
            {
                btn.BackColor = darkBg;
                btn.ForeColor = lightFg;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = darkBorder;
                btn.Font = font;
            }

            foreach (var cmb in CellCombos())
            {
                cmb.BackColor = Color.FromArgb(45, 45, 48);
                cmb.ForeColor = lightFg;
                cmb.FlatStyle = FlatStyle.Flat;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void RaisePreviewInvalidated()
        {
            if (_isApplying) return; // suppress reentrancy during ApplyConfig
            PreviewInvalidated?.Invoke(this, EventArgs.Empty);
        }

        // ------------------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------------------

        /// <summary>
        /// Releases bitmap resources owned by this control.
        /// Call from the Designer-generated Dispose(bool) override,
        /// same pattern as UC_ClockSettings.DisposeOwnedResources().
        /// </summary>
        private void DisposeOwnedResources()
        {
            _bgBitmap?.Dispose();
        }
    }
}