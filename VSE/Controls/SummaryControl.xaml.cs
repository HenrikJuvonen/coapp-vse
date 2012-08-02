namespace CoApp.VSE.Controls
{
    using System;
    using System.Linq;
    using System.Windows;

    public partial class SummaryControl
    {
        public SummaryControl()
        {
            InitializeComponent();

            ApplyButtonShield.UpdateShield(ApplyButton.IsEnabled);
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == ApplyButton && ApplyButtonShield != null)
                ApplyButtonShield.UpdateShield(ApplyButton.IsEnabled);
        }
        
        private void ExecuteCancel(object sender, EventArgs e)
        {
            Module.ShowMainControl();
        }

        private void ExecuteOk(object sender, EventArgs e)
        {
            Module.ShowProgressControl();
        }

        internal void Initialize()
        {
            RemoveDataGrid.ItemsSource = from package in Module.PackageManager.RemovePlan
                                         orderby package.CanonicalName
                                         select package;

            RemoveDataGrid.Visibility = RemoveDataGrid.HasItems ? Visibility.Visible : Visibility.Collapsed;

            InstallDataGrid.ItemsSource = from package in Module.PackageManager.InstallPlan
                                          orderby package.CanonicalName
                                          select package;

            InstallDataGrid.Visibility = InstallDataGrid.HasItems ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
