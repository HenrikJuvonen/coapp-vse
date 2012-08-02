using System.Windows;

namespace CoApp.VSE.Controls.Options
{
    using System;

    public partial class OptionsControl
    {
        public OptionsControl()
        {
            InitializeComponent();
        }

        public void ExecuteBack(object sender = null, EventArgs e = null)
        {
            Module.ShowMainControl();
        }
    }
}
