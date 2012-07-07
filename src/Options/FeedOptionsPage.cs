using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoApp.VisualStudio.Options
{
    [Guid("35DE739E-CE3D-45ED-A222-46755163AA93")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class FeedOptionsPage : OptionsPageBase
    {
        private FeedOptionsControl _control;

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            FeedsControl.InitializeOnActivated();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
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

        private FeedOptionsControl FeedsControl
        {
            get
            {
                if (_control == null)
                {
                    _control = new FeedOptionsControl();
                }

                return _control;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window
        {
            get
            {
                return new WinFormsControl(FeedsControl);
            }
        }
    }
}
