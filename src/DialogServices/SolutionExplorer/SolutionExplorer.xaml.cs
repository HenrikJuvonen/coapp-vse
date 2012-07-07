using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace CoApp.VisualStudio.Dialog
{
    public partial class SolutionExplorer
    {
        public SolutionExplorer()
        {
            InitializeComponent();
        }

        private void OnOkButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}