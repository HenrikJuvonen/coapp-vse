using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CoApp.Packaging.Common;

namespace CoApp.VSE.Controls.Options
{
    public partial class GeneralOptionsControl
    {
        private bool _isLoaded;

        public GeneralOptionsControl()
        {
            InitializeComponent();
            LoadSettings();

            if (!Module.IsDTELoaded)
            {
                CloseToTrayCheckBox.Visibility = Visibility.Visible;
                StartInTrayCheckBox.Visibility = Visibility.Visible;
                RestorePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            var cacheDirectory = new DirectoryInfo(PackageManagerSettings.CoAppCacheDirectory + "\\packages\\");

            CacheClearStatusLabel.Content = "Please wait...";
            CacheClearButton.IsEnabled = false;

            var worker = new BackgroundWorker();

            worker.DoWork += (o, args) => 
            {
                var installedPackages = Module.PackageManager.PackagesViewModel.Packages.Where(n => n.PackageIdentity.IsInstalled);

                foreach (var file in cacheDirectory.GetFiles())
                {
                    if (installedPackages.All(n => n.PackageIdentity.LocalPackagePath != file.FullName))
                        file.Delete();
                }
            };

            worker.RunWorkerCompleted += (o, args) =>
            {
                CacheClearStatusLabel.Content = args.Error != null ? args.Error.Message : "Cleared.";
                CacheClearButton.IsEnabled = true;

                var dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += (ob, a) =>
                {
                    CacheClearStatusLabel.Content = string.Empty;
                    dispatcherTimer.Stop();
                };
                dispatcherTimer.Interval = new TimeSpan(0,0,4);
                dispatcherTimer.Start();
            };

            worker.RunWorkerAsync();
        }

        private void SaveSettings()
        {
            if (!_isLoaded)
                return;

            Module.PackageManager.Settings["#rememberFilters"].BoolValue = (RememberFiltersCheckBox.IsChecked == true);
            Module.PackageManager.SetTelemetry(TelemetryCheckBox.IsChecked == true);
            Module.PackageManager.Settings["#autoEnd"].BoolValue = (AutoEndCheckBox.IsChecked == true);

            Module.PackageManager.Settings["#closeToTray"].BoolValue = (CloseToTrayCheckBox.IsChecked == true);
            Module.PackageManager.Settings["#showTrayIcon"].BoolValue = (ShowTrayIconCheckBox.IsChecked == true);
            Module.PackageManager.Settings["#showNotifications"].BoolValue = (ShowNotificationsInTrayCheckBox.IsChecked == true);
            Module.PackageManager.Settings["#startInTray"].BoolValue = (StartInTrayCheckBox.IsChecked == true);

            Module.PackageManager.Settings["#update"].IntValue = UpdateComboBox.SelectedIndex;
            Module.PackageManager.Settings["#restore"].IntValue = RestoreComboBox.SelectedIndex;
        }

        private void LoadSettings()
        {
            RememberFiltersCheckBox.IsChecked = Module.PackageManager.Settings["#rememberFilters"].BoolValue;
            TelemetryCheckBox.IsChecked = Module.PackageManager.GetTelemetry();
            AutoEndCheckBox.IsChecked = Module.PackageManager.Settings["#autoEnd"].BoolValue;

            ShowTrayIconCheckBox.IsChecked = Module.PackageManager.Settings["#showTrayIcon"].BoolValue;
            ShowNotificationsInTrayCheckBox.IsChecked = Module.PackageManager.Settings["#showNotifications"].BoolValue;
            CloseToTrayCheckBox.IsChecked = Module.PackageManager.Settings["#closeToTray"].BoolValue;
            StartInTrayCheckBox.IsChecked = Module.PackageManager.Settings["#startInTray"].BoolValue;

            var update = Module.PackageManager.Settings["#update"].IntValue;
            UpdateComboBox.SelectedIndex = update >= 0 || update <= 2 ? update : 2;
            
            var restore = Module.PackageManager.Settings["#restore"].IntValue;
            RestoreComboBox.SelectedIndex = restore >= 0 || restore <= 2 ? restore : 2;

            StartInTrayCheckBox.IsEnabled = (ShowTrayIconCheckBox.IsChecked == true);

            _isLoaded = true;
        }

        private void OnShowTrayIconChecked(object sender, EventArgs e)
        {
            StartInTrayCheckBox.IsEnabled = (ShowTrayIconCheckBox.IsChecked == true);
            Module.TrayIcon.Visible = (ShowTrayIconCheckBox.IsChecked == true);
            
            OnChanged(sender, e);
        }

        private void OnChanged(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
