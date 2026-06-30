using System;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// Small modal dialog used for both "new page" and "rename page".
    /// Returns the entered name via NameValue when DialogResult == OK.
    /// All controls are defined in FormTabNamePrompt_Designer.cs.
    /// </summary>
    public partial class FormTabNamePrompt : Form
    {
        public string NameValue => txtName.Text.Trim();

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
                                                        ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int v = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
        }

        public FormTabNamePrompt(string title, string initialValue)
        {
            InitializeComponent();
            Text = title;
            txtName.Text = initialValue ?? "";
            txtName.SelectAll();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Name cannot be empty.", "Invalid name",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Convenience helper: show the dialog and return the entered name,
        /// or null if the user cancelled.
        /// </summary>
        public static string Show(IWin32Window owner, string title, string initialValue = "")
        {
            using (var dlg = new FormTabNamePrompt(title, initialValue))
            {
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.NameValue : null;
            }
        }
    }
}