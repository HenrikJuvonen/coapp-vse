using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms.Integration;

namespace CoApp.VisualStudio.Options
{
    /// <summary>
    /// WinForms-WPF interoperability
    /// </summary>
    public class WinFormsControl : System.Windows.Forms.UserControl
    {
        private ElementHost host;

        public WinFormsControl(UIElement control)
        {
            host = new System.Windows.Forms.Integration.ElementHost();
            SuspendLayout();

            host.Dock = System.Windows.Forms.DockStyle.Fill;
            host.Location = new System.Drawing.Point(0, 0);
            host.TabIndex = 0;
            host.Child = control;

            Controls.Add(host);
            ResumeLayout(false);
        }
    }

    /// <summary>
    /// Wpf-controls are read-only in Vs options. This class gets around that problem.
    /// </summary>
    public class WpfControl : System.Windows.Controls.UserControl
    {
        private const UInt32 DLGC_WANTARROWS = 0x0001;
        private const UInt32 DLGC_WANTTAB = 0x0002;
        private const UInt32 DLGC_WANTALLKEYS = 0x0004;
        private const UInt32 DLGC_HASSETSEL = 0x0008;
        private const UInt32 DLGC_WANTCHARS = 0x0080;
        private const UInt32 WM_GETDLGCODE = 0x0087;

        protected void InitializeBase()
        {
            Loaded += delegate
            {
                HwndSource s = HwndSource.FromVisual(this) as HwndSource;
                if (s != null)
                    s.AddHook(new HwndSourceHook(ChildHwndSourceHook));
            };
        }

        IntPtr ChildHwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETDLGCODE)
            {
                handled = true;
                return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
            }
            return IntPtr.Zero;
        }
    }
}
