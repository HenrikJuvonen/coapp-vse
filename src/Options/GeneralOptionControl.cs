using System;
using System.IO;
using System.Windows.Forms;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Options
{
    public partial class GeneralOptionControl : UserControl
    {
        public GeneralOptionControl()
        {
            InitializeComponent();
        }

        internal void OnActivated()
        {
        }

        internal void OnApply()
        {

        }

        internal void OnClosed()
        {
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo cache = new DirectoryInfo(@"C:\apps\.cache\packages");

            foreach (FileInfo file in cache.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in cache.GetDirectories())
            {
                dir.Delete(true);
            }

            MessageHelper.ShowInfoMessage(Resources.ShowInfo_ClearPackageCache, Resources.ShowWarning_Title);
        }

        private void OnBrowsePackageCacheClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"C:\apps\.cache\packages");
        }
    }
}