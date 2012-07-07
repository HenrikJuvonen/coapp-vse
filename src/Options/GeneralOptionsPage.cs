using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CoApp.VisualStudio.Options
{
    [Guid("EB451205-CEDF-4EC2-AE48-8A6310601356")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class GeneralOptionsPage : OptionsPageBase
    {
        private GeneralOptionsControl _control;

        protected override void OnApply(PageApplyEventArgs e)
        {
            GeneralControl.ApplyChangedSettings();
        }

        private GeneralOptionsControl GeneralControl
        {
            get
            {
                if (_control == null)
                {
                    _control = new GeneralOptionsControl();
                }

                return _control;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override System.Windows.Forms.IWin32Window Window
        {
            get
            {
                return new WinFormsControl(GeneralControl);
            }
        }
    }
}