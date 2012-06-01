using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace CoApp.VisualStudio.Options
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class FeedOptionsPage : OptionsPageBase
    {
        private FeedsOptionsControl _optionsWindow;

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            FeedsControl.Font = VsShellUtilities.GetEnvironmentFont(this);
            FeedsControl.InitializeOnActivated();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            // Do not need to call base.OnApply() here.
            bool wasApplied = FeedsControl.ApplyChangedSettings();
            if (!wasApplied)
            {
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            FeedsControl.ClearSettings();
            base.OnClosed(e);
        }

        private FeedsOptionsControl FeedsControl
        {
            get
            {
                if (_optionsWindow == null)
                {
                    _optionsWindow = new FeedsOptionsControl();
                    _optionsWindow.Location = new Point(0, 0);
                }

                return _optionsWindow;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window
        {
            get
            {
                return FeedsControl;
            }
        }
    }
}
