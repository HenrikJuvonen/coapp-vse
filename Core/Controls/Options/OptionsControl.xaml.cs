using System.Windows;

namespace CoApp.VSE.Core.Controls.Options
{
    using System;

    public partial class OptionsControl
    {
        public OptionsControl()
        {
            InitializeComponent();
        }

        private void ExecuteBack(object sender, EventArgs e)
        {
            Module.ShowMainControl();
        }
    }
}
