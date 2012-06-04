using System;
using System.IO;
using System.Diagnostics;
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
            DirectoryInfo cache = new DirectoryInfo(@"C:\ProgramData\.cache\packages");

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
            Process.Start(@"C:\ProgramData\.cache\packages");
        }
    }
}