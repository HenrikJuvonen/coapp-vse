using System;
using System.Windows.Documents;
using System.Windows.Input;

namespace CoApp.VSE.Core.Controls
{
    using Utility;

    public partial class InfoControl
    {
        public InfoControl()
        {
            InitializeComponent();
        }

        private void ExecuteBack(object sender, EventArgs e)
        {
            Module.ShowMainControl();
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            var hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                UriHelper.OpenExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }
    }
}
