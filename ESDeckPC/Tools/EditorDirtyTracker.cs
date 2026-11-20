using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// Unsaved-changes tracking for the config editors: compares the config,
    /// serialized, with a snapshot taken when it was last opened / saved.
    /// Keeps " *" on the window title while they differ (checked every
    /// 500 ms -- the configs are small), and asks Save / Don't Save / Cancel
    /// before the config is replaced or the window closes.
    /// </summary>
    internal sealed class EditorDirtyTracker : IDisposable
    {
        private readonly Form _form;
        private readonly Func<object> _config;
        private readonly Func<string> _title;
        private readonly Action _readUi;
        private readonly Timer _timer;
        private string _saved;

        /// <param name="config">the config object being edited (may be replaced; read each time)</param>
        /// <param name="title">window title without the " *"</param>
        /// <param name="readUi">copies panel fields into the config first (null if the editor writes straight through)</param>
        public EditorDirtyTracker(Form form, Func<object> config, Func<string> title, Action readUi = null)
        {
            _form = form;
            _config = config;
            _title = title;
            _readUi = readUi;
            _timer = new Timer { Interval = 500 };
            _timer.Tick += (s, e) => RefreshTitle();
            _timer.Start();
            form.FormClosed += (s, e) => Dispose();
        }

        private string Snapshot() => JsonConvert.SerializeObject(_config());

        public bool IsDirty => _saved != null && Snapshot() != _saved;

        /// <summary>The config as it is now counts as saved (after open / new / save).</summary>
        public void MarkSaved()
        {
            _readUi?.Invoke();
            _saved = Snapshot();
            RefreshTitle();
        }

        public void RefreshTitle()
        {
            string t = _title() + (IsDirty ? " *" : "");
            if (_form.Text != t) _form.Text = t;
        }

        /// <summary>
        /// Before throwing the current config away: true to go on (nothing
        /// unsaved, saved now, or Don't Save), false to stay (Cancel, or the
        /// save failed / was cancelled).
        /// </summary>
        public bool ConfirmDiscard(Func<bool> save)
        {
            _readUi?.Invoke();
            if (!IsDirty) return true;
            var r = MessageBox.Show(_form, "There are unsaved changes. Save them first?", _title(),
                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.No) return true;
            return save();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
