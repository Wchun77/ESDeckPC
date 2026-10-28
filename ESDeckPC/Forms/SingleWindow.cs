using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ESDeckPC
{
    /// <summary>
    /// One window per editor / tool: opening it again brings the open one to
    /// the front instead of a second copy (two editors on the same config
    /// would overwrite each other). Non-modal, so other windows stay usable.
    /// UI thread only.
    /// </summary>
    internal static class SingleWindow
    {
        private static readonly Dictionary<Type, Form> Open = new Dictionary<Type, Form>();

        public static T Show<T>(Func<T> create) where T : Form
        {
            if (Open.TryGetValue(typeof(T), out Form f) && !f.IsDisposed)
            {
                if (f.WindowState == FormWindowState.Minimized) f.WindowState = FormWindowState.Normal;
                f.Activate();
                return (T)f;
            }
            T form = create();
            Open[typeof(T)] = form;
            form.FormClosed += (s, e) =>
            {
                if (Open.TryGetValue(typeof(T), out Form cur) && cur == form) Open.Remove(typeof(T));
            };
            form.Show();
            return form;
        }
    }
}
