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
        private readonly ElementHost _host = new System.Windows.Forms.Integration.ElementHost();

        public WinFormsControl(UIElement control)
        {
            SuspendLayout();

            _host.Dock = System.Windows.Forms.DockStyle.Fill;
            _host.Location = new System.Drawing.Point(0, 0);
            _host.TabIndex = 0;
            _host.Child = control;

            Controls.Add(_host);
            ResumeLayout(false);
        }
    }

    /// <summary>
    /// Wpf-controls are read-only in Vs options. This class gets around that problem.
    /// </summary>
    public class WpfControl : System.Windows.Controls.UserControl
    {
        private const UInt32 DLGC_WANTARROWS = 0x0001;
        private const UInt32 DLGC_HASSETSEL = 0x0008;
        private const UInt32 DLGC_WANTCHARS = 0x0080;
        private const UInt32 WM_GETDLGCODE = 0x0087;

        protected void InitializeBase()
        {
            Loaded += delegate
            {
                var s = HwndSource.FromVisual(this) as HwndSource;
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
