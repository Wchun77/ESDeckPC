using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDeckPC
{
    public partial class FormButtonEditor : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int value = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }

        private PcButton _button;
        private readonly string _iconsDir;

        // Dynamic controls
        private Label _dynLabel;
        private TextBox _dynTxtTarget;    // launch / media
        private TextBox _dynTxtKeys;      // hotkey
        private ComboBox _dynCmbTarget;   // discord command
        private Label _dynLblChannelId;   // discord join_channel
        private TextBox _dynTxtChannelId; // discord join_channel

        // scroll
        private ComboBox _dynCmbScrollTarget;
        private Label _dynLblAmount;
        private TextBox _dynTxtAmount;

        // sequence
        private Label _dynLblSequenceKeys;
        private TextBox _dynTxtSequenceKeys;

        // mouse_click
        private ComboBox _dynCmbClickType;

        public FormButtonEditor(PcButton button, bool isNew = false, string iconsDir = null)
        {
            InitializeComponent();

            _button = button;
            _iconsDir = iconsDir;

            this.Text = isNew ? "Add Button" : "Edit Button";

            txtLabel.Text = button.Label ?? "";
            txtIcon.Text = button.Icon ?? "";

            // Populate action list here so adding new actions only requires
            // editing this file, not the Designer.
            cmbAction.Items.Clear();
            cmbAction.Items.AddRange(new object[]
            {
                "launch", "hotkey", "media", "discord",
                "scroll", "sequence", "text", "mouse_click",
            });

            cmbAction.SelectedItem = button.Action ?? "launch";
            if (cmbAction.SelectedIndex < 0) cmbAction.SelectedIndex = 0;
            cmbAction.SelectedIndexChanged += cmbAction_SelectedIndexChanged;

            btnIconBrowse.Click += BtnIconBrowse_Click;
            btnIconClear.Click += (s, e) => txtIcon.Text = "";

            btnOK.Click += btnOK_Click;
            btnCancel.Click += btnCancel_Click;

            BuildDynamicPanel(button.Action ?? "launch");
        }

        // ------------------------------------------------------------------
        // Icon browse
        // ------------------------------------------------------------------

        private void BtnIconBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select icon";
                ofd.Filter = "PNG image (*.png)|*.png";
                if (!string.IsNullOrEmpty(_iconsDir) && System.IO.Directory.Exists(_iconsDir))
                    ofd.InitialDirectory = _iconsDir;

                if (ofd.ShowDialog() == DialogResult.OK)
                    txtIcon.Text = System.IO.Path.GetFileName(ofd.FileName);
            }
        }

        // ------------------------------------------------------------------
        // Dynamic panel
        // ------------------------------------------------------------------

        private void cmbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildDynamicPanel(cmbAction.SelectedItem?.ToString());
        }

        private void BuildDynamicPanel(string action)
        {
            pnlDynamic.Controls.Clear();
            _dynLabel = null;
            _dynTxtTarget = null;
            _dynTxtKeys = null;
            _dynCmbTarget = null;
            _dynLblChannelId = null;
            _dynTxtChannelId = null;
            _dynCmbScrollTarget = null;
            _dynLblAmount = null;
            _dynTxtAmount = null;
            _dynLblSequenceKeys = null;
            _dynTxtSequenceKeys = null;
            _dynCmbClickType = null;

            switch (action?.ToLower())
            {
                case "launch":
                case "media":
                    _dynLabel = MakeLabel("Target");
                    _dynLabel.Location = new Point(0, 0);

                    _dynTxtTarget = MakeTextBox();
                    _dynTxtTarget.Location = new Point(0, 16);
                    _dynTxtTarget.Size = new Size(352, 22);

                    if (action == "launch")
                        _dynTxtTarget.Text = _button.Target ?? "";
                    else
                        _dynTxtTarget.Text = _button.Target ?? "";

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynTxtTarget);
                    break;

                case "hotkey":
                    _dynLabel = MakeLabel("Keys  (e.g. ctrl, shift, s)");
                    _dynLabel.Location = new Point(0, 0);

                    _dynTxtKeys = MakeTextBox();
                    _dynTxtKeys.Location = new Point(0, 16);
                    _dynTxtKeys.Size = new Size(352, 22);
                    _dynTxtKeys.Text = _button.Keys != null
                        ? string.Join(", ", _button.Keys) : "";

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynTxtKeys);
                    break;

                case "discord":
                    _dynLabel = MakeLabel("Command");
                    _dynLabel.Location = new Point(0, 0);

                    _dynCmbTarget = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Location = new Point(0, 16),
                        Size = new Size(352, 22),
                        BackColor = System.Drawing.Color.FromArgb(45, 45, 48),
                        ForeColor = System.Drawing.Color.FromArgb(220, 220, 220),
                        FlatStyle = FlatStyle.Flat,
                        Font = this.Font,
                    };
                    _dynCmbTarget.Items.AddRange(new object[]
                    {
                        "mute", "unmute", "deafen", "undeafen",
                        "join_channel", "leave_channel"
                    });
                    _dynCmbTarget.SelectedItem = _button.Target ?? "mute";
                    if (_dynCmbTarget.SelectedIndex < 0) _dynCmbTarget.SelectedIndex = 0;
                    _dynCmbTarget.SelectedIndexChanged += (s, ev) => UpdateDiscordChannelVisibility();

                    _dynLblChannelId = MakeLabel("Channel ID");
                    _dynLblChannelId.Location = new Point(0, 48);

                    _dynTxtChannelId = MakeTextBox();
                    _dynTxtChannelId.Location = new Point(0, 64);
                    _dynTxtChannelId.Size = new Size(352, 22);
                    _dynTxtChannelId.Text = _button.ChannelId ?? "";

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynCmbTarget);
                    pnlDynamic.Controls.Add(_dynLblChannelId);
                    pnlDynamic.Controls.Add(_dynTxtChannelId);

                    UpdateDiscordChannelVisibility();
                    break;

                case "scroll":
                    _dynLabel = MakeLabel("Direction");
                    _dynLabel.Location = new Point(0, 0);

                    _dynCmbScrollTarget = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Location = new Point(0, 16),
                        Size = new Size(352, 22),
                        BackColor = System.Drawing.Color.FromArgb(45, 45, 48),
                        ForeColor = System.Drawing.Color.FromArgb(220, 220, 220),
                        FlatStyle = FlatStyle.Flat,
                        Font = this.Font,
                    };
                    _dynCmbScrollTarget.Items.AddRange(new object[] { "up", "down", "left", "right" });
                    _dynCmbScrollTarget.SelectedItem = _button.Target ?? "up";
                    if (_dynCmbScrollTarget.SelectedIndex < 0) _dynCmbScrollTarget.SelectedIndex = 0;

                    _dynLblAmount = MakeLabel("Amount  (wheel delta, default 120)");
                    _dynLblAmount.Location = new Point(0, 48);

                    _dynTxtAmount = MakeTextBox();
                    _dynTxtAmount.Location = new Point(0, 64);
                    _dynTxtAmount.Size = new Size(120, 22);
                    _dynTxtAmount.Text = _button.Amount.HasValue
                        ? _button.Amount.Value.ToString() : "";

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynCmbScrollTarget);
                    pnlDynamic.Controls.Add(_dynLblAmount);
                    pnlDynamic.Controls.Add(_dynTxtAmount);
                    break;

                case "sequence":
                    _dynLblSequenceKeys = MakeLabel("Key combos  (one per line, e.g. ctrl, f)");
                    _dynLblSequenceKeys.Location = new Point(0, 0);

                    _dynTxtSequenceKeys = MakeTextBox();
                    _dynTxtSequenceKeys.Location = new Point(0, 16);
                    _dynTxtSequenceKeys.Size = new Size(352, 80);
                    _dynTxtSequenceKeys.Multiline = true;
                    _dynTxtSequenceKeys.ScrollBars = ScrollBars.Vertical;
                    _dynTxtSequenceKeys.Text = _button.Keys != null
                        ? string.Join(Environment.NewLine, _button.Keys) : "";

                    pnlDynamic.Controls.Add(_dynLblSequenceKeys);
                    pnlDynamic.Controls.Add(_dynTxtSequenceKeys);
                    break;

                case "text":
                    _dynLabel = MakeLabel("Text");
                    _dynLabel.Location = new Point(0, 0);

                    _dynTxtTarget = MakeTextBox();
                    _dynTxtTarget.Location = new Point(0, 16);
                    _dynTxtTarget.Size = new Size(352, 22);
                    _dynTxtTarget.Text = _button.Target ?? "";

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynTxtTarget);
                    break;

                case "mouse_click":
                    _dynLabel = MakeLabel("Click type");
                    _dynLabel.Location = new Point(0, 0);

                    _dynCmbClickType = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Location = new Point(0, 16),
                        Size = new Size(352, 22),
                        BackColor = System.Drawing.Color.FromArgb(45, 45, 48),
                        ForeColor = System.Drawing.Color.FromArgb(220, 220, 220),
                        FlatStyle = FlatStyle.Flat,
                        Font = this.Font,
                    };
                    _dynCmbClickType.Items.AddRange(new object[] { "single", "double" });
                    _dynCmbClickType.SelectedItem = _button.Target ?? "single";
                    if (_dynCmbClickType.SelectedIndex < 0) _dynCmbClickType.SelectedIndex = 0;

                    pnlDynamic.Controls.Add(_dynLabel);
                    pnlDynamic.Controls.Add(_dynCmbClickType);
                    break;
            }
        }

        private void UpdateDiscordChannelVisibility()
        {
            if (_dynLblChannelId == null || _dynTxtChannelId == null) return;
            bool show = _dynCmbTarget?.SelectedItem?.ToString() == "join_channel";
            _dynLblChannelId.Visible = show;
            _dynTxtChannelId.Visible = show;
        }

        // ------------------------------------------------------------------
        // OK / Cancel
        // ------------------------------------------------------------------

        private void btnOK_Click(object sender, EventArgs e)
        {
            _button.Label = txtLabel.Text.Trim();
            _button.Icon = txtIcon.Text.Trim();
            _button.Action = cmbAction.SelectedItem?.ToString();

            string action = _button.Action?.ToLower();

            if (action == "launch" || action == "media")
            {
                _button.Target = _dynTxtTarget?.Text.Trim();
                _button.Keys = null;
            }
            else if (action == "hotkey")
            {
                _button.Target = null;
                _button.ChannelId = null;
                string raw = _dynTxtKeys?.Text ?? "";
                var parts = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                _button.Keys = new List<string>();
                foreach (var p in parts)
                    _button.Keys.Add(p.Trim());
            }
            else if (action == "discord")
            {
                _button.Keys = null;
                _button.Target = _dynCmbTarget?.SelectedItem?.ToString();
                _button.ChannelId = _button.Target == "join_channel"
                    ? _dynTxtChannelId?.Text.Trim()
                    : null;
            }
            else if (action == "scroll")
            {
                _button.Keys = null;
                _button.ChannelId = null;
                _button.Target = _dynCmbScrollTarget?.SelectedItem?.ToString();
                string amtTxt = _dynTxtAmount?.Text.Trim();
                _button.Amount = int.TryParse(amtTxt, out int amt) ? (int?)amt : null;
            }
            else if (action == "sequence")
            {
                _button.Target = null;
                _button.ChannelId = null;
                _button.Amount = null;
                string raw = _dynTxtSequenceKeys?.Text ?? "";
                var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                _button.Keys = new List<string>();
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0)
                        _button.Keys.Add(trimmed);
                }
            }
            else if (action == "text")
            {
                _button.Keys = null;
                _button.ChannelId = null;
                _button.Amount = null;
                _button.Target = _dynTxtTarget?.Text ?? "";
            }
            else if (action == "mouse_click")
            {
                _button.Keys = null;
                _button.ChannelId = null;
                _button.Amount = null;
                _button.Target = _dynCmbClickType?.SelectedItem?.ToString() ?? "single";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private Label MakeLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                ForeColor = Color.Gray,
            };
        }

        private TextBox MakeTextBox()
        {
            return new TextBox
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle,
                Font = this.Font,
            };
        }
    }
}