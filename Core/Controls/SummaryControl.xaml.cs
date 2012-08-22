using System;
using System.Linq;
using System.Windows;

namespace CoApp.VSE.Core.Controls
{
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

            var removePlan = Module.PackageManager.RemovePlan;

            if (Module.PackageManager.Settings["#autoTrim"].BoolValue)
                removePlan = removePlan.Union(Module.PackageManager.TrimPlan);

            RemoveDataGrid.ItemsSource = from package in removePlan
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
