using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Touchpad editor's "Sidebar" entry: what goes between the keyboard
    /// button and the gear on the device sidebar -- nothing, the scroll
    /// wheel, or up to TouchpadConfig.MaxHotkeys hotkey buttons.
    ///
    /// Hotkeys are stored in the touchpad JSON and executed by the ESP itself
    /// (sent as keyboard reports), not through Deck's page/button actions;
    /// only the key-name format ("ctrl, c") is shared with Deck's hotkey.
    /// Built in code (no designer file) -- it's a fixed, small form. Uses the
    /// same Font-based auto-scaling as the designer-built panels so text
    /// isn't clipped on Windows display scaling above 100%.
    /// </summary>
    public class UC_TouchpadSidebarPanel : UserControl
    {
        public event EventHandler PreviewInvalidated;

        private readonly ComboBox _cmbMode;
        private readonly Panel _pnlSlots;
        private readonly SlotRow[] _rows = new SlotRow[TouchpadConfig.MaxHotkeys];

        private string _sideIconsDir;
        private bool _isApplying;

        private static readonly Color ColBack = Color.FromArgb(28, 28, 28);
        private static readonly Color ColField = Color.FromArgb(45, 45, 48);
        private static readonly Color ColFore = Color.FromArgb(220, 220, 220);
        private static readonly Color ColGroup = Color.FromArgb(210, 210, 210);
        private static readonly Color ColCaption = Color.FromArgb(170, 170, 170);
        private static readonly Color ColBtn = Color.FromArgb(55, 55, 58);
        private static readonly Color ColBorder = Color.FromArgb(80, 80, 80);
        private static readonly Color ColBad = Color.FromArgb(220, 90, 90);

        private static readonly string[] ModeValues = { "none", "wheel", "hotkeys" };
        private static readonly string[] ModeNames = { "None (empty)", "Scroll wheel", "Hotkeys" };

        private sealed class SlotRow
        {
            public GroupBox Group;
            public TextBox Label;
            public TextBox Keys;
            public Label IconName;
            public string Icon = "";
        }

        public UC_TouchpadSidebarPanel()
        {
            SuspendLayout();
            BackColor = ColBack;
            ForeColor = ColFore;
            Font = new Font("Consolas", 9F);
            Size = new Size(265, 520);

            var grpMode = new GroupBox
            {
                Text = "Sidebar Middle",
                ForeColor = ColGroup,
                Location = new Point(12, 12),
                Size = new Size(234, 54),
            };
            _cmbMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColField,
                ForeColor = ColFore,
                Location = new Point(8, 20),
                Size = new Size(218, 22),
            };
            _cmbMode.Items.AddRange(ModeNames);
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += (s, e) => { UpdateSlotsEnabled(); Raise(); };
            grpMode.Controls.Add(_cmbMode);
            Controls.Add(grpMode);

            _pnlSlots = new Panel { Location = new Point(0, 72), Size = new Size(265, 448) };
            Controls.Add(_pnlSlots);

            for (int i = 0; i < _rows.Length; i++)
                _rows[i] = BuildRow(i);

            UpdateSlotsEnabled();

            // Same as the designer files (7x14 = Consolas 9 at 96 dpi): set last,
            // then ResumeLayout scales everything above to the current DPI.
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ResumeLayout(false);
        }

        private SlotRow BuildRow(int i)
        {
            var row = new SlotRow();
            row.Group = new GroupBox
            {
                Text = $"Hotkey {i + 1}",
                ForeColor = ColGroup,
                Location = new Point(12, i * 108),
                Size = new Size(234, 102),
            };

            row.Group.Controls.Add(MakeCaption("Label", 8, 22));
            row.Label = MakeTextBox(58, 19, 168);
            row.Label.MaxLength = 11;   // TP_HOTKEY_LABEL_LEN - 1 on the device
            row.Label.TextChanged += (s, e) => Raise();
            row.Group.Controls.Add(row.Label);

            row.Group.Controls.Add(MakeCaption("Keys", 8, 48));
            row.Keys = MakeTextBox(58, 45, 168);
            row.Keys.TextChanged += (s, e) => { ValidateRow(row); Raise(); };
            row.Group.Controls.Add(row.Keys);

            var btnIcon = MakeButton("Icon...", 8, 72, 74);
            btnIcon.Click += (s, e) =>
            {
                string name = FormSideIconPicker.Show(FindForm(), $"Hotkey {i + 1} Icon",
                    row.Icon, string.IsNullOrEmpty(row.Label.Text) ? "?" : row.Label.Text, _sideIconsDir);
                if (name == null) return;
                row.Icon = name;
                UpdateIconName(row);
                Raise();
            };
            row.Group.Controls.Add(btnIcon);

            var btnClear = MakeButton("Clear", 86, 72, 62);
            btnClear.Click += (s, e) =>
            {
                row.Label.Text = "";
                row.Keys.Text = "";
                row.Icon = "";
                UpdateIconName(row);
                Raise();
            };
            row.Group.Controls.Add(btnClear);

            row.IconName = new Label
            {
                ForeColor = ColCaption,
                Font = new Font("Consolas", 8F),
                Location = new Point(152, 77),
                Size = new Size(76, 16),
                AutoEllipsis = true,
            };
            row.Group.Controls.Add(row.IconName);
            UpdateIconName(row);

            _pnlSlots.Controls.Add(row.Group);
            return row;
        }

        // ------------------------------------------------------------------
        // Config <-> UI
        // ------------------------------------------------------------------

        public void ApplyConfig(TouchpadConfig cfg, string sideIconsDir)
        {
            _isApplying = true;
            try
            {
                _sideIconsDir = sideIconsDir;
                int mi = Array.IndexOf(ModeValues, (cfg.SidebarMode ?? "none").ToLowerInvariant());
                _cmbMode.SelectedIndex = mi < 0 ? 0 : mi;

                for (int i = 0; i < _rows.Length; i++)
                {
                    var hk = (cfg.Hotkeys != null && i < cfg.Hotkeys.Count) ? cfg.Hotkeys[i] : null;
                    _rows[i].Label.Text = hk?.Label ?? "";
                    _rows[i].Keys.Text = hk?.Keys != null ? string.Join(", ", hk.Keys) : "";
                    _rows[i].Icon = hk?.Icon ?? "";
                    UpdateIconName(_rows[i]);
                    ValidateRow(_rows[i]);
                }
                UpdateSlotsEnabled();
            }
            finally
            {
                _isApplying = false;
            }
        }

        /// <summary>
        /// Writes mode + hotkeys into cfg. Empty rows (no keys) are skipped,
        /// so filled rows are packed in order -- same as the device lays them
        /// out top-down.
        /// </summary>
        public void ReadInto(TouchpadConfig cfg)
        {
            cfg.SidebarMode = ModeValues[Math.Max(0, _cmbMode.SelectedIndex)];
            cfg.Hotkeys = new List<TouchpadHotkey>();
            foreach (var row in _rows)
            {
                var keys = TouchpadHotkeyKeys.Split(row.Keys.Text);
                if (keys.Count == 0) continue;
                cfg.Hotkeys.Add(new TouchpadHotkey
                {
                    Label = row.Label.Text.Trim(),
                    Icon = row.Icon ?? "",
                    Keys = keys,
                });
            }
        }

        /// <summary>
        /// Error text for the first invalid filled row, or null if all good.
        /// Only checked in Hotkeys mode (rows are ignored otherwise).
        /// </summary>
        public string Validate()
        {
            if (ModeValues[Math.Max(0, _cmbMode.SelectedIndex)] != "hotkeys") return null;
            for (int i = 0; i < _rows.Length; i++)
            {
                var keys = TouchpadHotkeyKeys.Split(_rows[i].Keys.Text);
                if (keys.Count == 0) continue;
                if (!TouchpadHotkeyKeys.IsValid(keys, out string err))
                    return $"Hotkey {i + 1}: {err}";
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void Raise()
        {
            if (!_isApplying) PreviewInvalidated?.Invoke(this, EventArgs.Empty);
        }

        // Hotkey rows only exist in Hotkeys mode -- hidden otherwise.
        private void UpdateSlotsEnabled()
        {
            _pnlSlots.Visible = ModeValues[Math.Max(0, _cmbMode.SelectedIndex)] == "hotkeys";
        }

        private static void UpdateIconName(SlotRow row)
        {
            row.IconName.Text = string.IsNullOrEmpty(row.Icon) ? "(no icon)" : row.Icon;
        }

        private static void ValidateRow(SlotRow row)
        {
            var keys = TouchpadHotkeyKeys.Split(row.Keys.Text);
            bool ok = keys.Count == 0 || TouchpadHotkeyKeys.IsValid(keys, out _);
            row.Keys.ForeColor = ok ? ColFore : ColBad;
        }

        private static Label MakeCaption(string text, int x, int y) => new Label
        {
            Text = text,
            ForeColor = ColCaption,
            Location = new Point(x, y),
            AutoSize = true,
        };

        private static TextBox MakeTextBox(int x, int y, int w) => new TextBox
        {
            BackColor = ColField,
            ForeColor = ColFore,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(x, y),
            Size = new Size(w, 22),
        };

        private static Button MakeButton(string text, int x, int y, int w)
        {
            var b = new Button
            {
                Text = text,
                BackColor = ColBtn,
                ForeColor = ColFore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 8.5f),
                Location = new Point(x, y),
                Size = new Size(w, 24),
                Padding = Padding.Empty,     // flat buttons' default padding was clipping the text
            };
            b.FlatAppearance.BorderColor = ColBorder;
            return b;
        }
    }
}
