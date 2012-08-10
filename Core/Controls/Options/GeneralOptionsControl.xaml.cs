using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using CoApp.Packaging.Common;

namespace CoApp.VSE.Core.Controls.Options
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

            if (!Toolkit.Win32.AdminPrivilege.IsRunAsAdmin)
                CacheClearButton.IsEnabled = false;

            TelemetryShield.UpdateShield(ApplyTelemetryButton.IsEnabled);
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender == ApplyTelemetryButton && TelemetryShield != null)
                TelemetryShield.UpdateShield(ApplyTelemetryButton.IsEnabled);
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            var cacheDirectory = new DirectoryInfo(PackageManagerSettings.CoAppCacheDirectory + "\\packages\\");

            CacheClearStatusLabel.Text = "Please wait...";
            CacheClearStatusLabel.Visibility = Visibility.Visible;
            CacheClearButton.IsEnabled = false;

            var worker = new BackgroundWorker();

            worker.DoWork += (o, args) => 
            {
                var installedPackages = Module.PackageManager.PackagesViewModel.Packages.Where(n => n.PackageIdentity.IsInstalled);
                var feedLocations = Module.PackageManager.GetFeedLocations().Select(n => n.Replace("://", "-").Replace("/", "-"));

                foreach (var file in cacheDirectory.GetFiles())
                {
                    if (installedPackages.All(n => n.PackageIdentity.LocalPackagePath != file.FullName) && !feedLocations.Contains(file.Name))
                        file.Delete();
                }
            };

            worker.RunWorkerCompleted += (o, args) =>
            {
                CacheClearStatusLabel.Text = args.Error != null ? args.Error.Message : "Cleared.";
                CacheClearButton.IsEnabled = true;

                var dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += (ob, a) =>
                {
                    CacheClearStatusLabel.Text = string.Empty;
                    CacheClearStatusLabel.Visibility = Visibility.Collapsed;
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

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Module.PackageManager.Elevated -= SaveSettings;
                
                Module.PackageManager.Settings["#rememberFilters"].BoolValue = (RememberFiltersCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#showConsole"].BoolValue = (ShowConsoleCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#autoEnd"].BoolValue = (AutoEndCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#autoTrim"].BoolValue = (AutoTrimCheckBox.IsChecked == true);
                
                Module.PackageManager.Settings["#closeToTray"].BoolValue = (CloseToTrayCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#showTrayIcon"].BoolValue = (ShowTrayIconCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#showNotifications"].BoolValue = (ShowNotificationsInTrayCheckBox.IsChecked == true);
                Module.PackageManager.Settings["#startInTray"].BoolValue = (StartInTrayCheckBox.IsChecked == true);

                Module.PackageManager.Settings["#update"].IntValue = UpdateComboBox.SelectedIndex;
                Module.PackageManager.Settings["#restore"].IntValue = RestoreComboBox.SelectedIndex;
                
                Module.PackageManager.SetTelemetry(TelemetryRadio1.IsChecked == true);

                if (!Module.MainWindow.MainControl.ConsoleControl.IsVisible && Module.PackageManager.Settings["#showConsole"].BoolValue)
                {
                    Module.MainWindow.MainControl.ConsoleControl.Visibility = Visibility.Visible;
                }
            }));
        }

        private void LoadSettings()
        {
            RememberFiltersCheckBox.IsChecked = Module.PackageManager.Settings["#rememberFilters"].BoolValue;
            ShowConsoleCheckBox.IsChecked = Module.PackageManager.Settings["#showConsole"].BoolValue;
            AutoEndCheckBox.IsChecked = Module.PackageManager.Settings["#autoEnd"].BoolValue;
            AutoTrimCheckBox.IsChecked = Module.PackageManager.Settings["#autoTrim"].BoolValue;

            ShowTrayIconCheckBox.IsChecked = Module.PackageManager.Settings["#showTrayIcon"].BoolValue;
            ShowNotificationsInTrayCheckBox.IsChecked = Module.PackageManager.Settings["#showNotifications"].BoolValue;
            CloseToTrayCheckBox.IsChecked = Module.PackageManager.Settings["#closeToTray"].BoolValue;
            StartInTrayCheckBox.IsChecked = Module.PackageManager.Settings["#startInTray"].BoolValue;

            var update = Module.PackageManager.Settings["#update"].IntValue;
            UpdateComboBox.SelectedIndex = update >= 0 || update <= 2 ? update : 2;
            
            var restore = Module.PackageManager.Settings["#restore"].IntValue;
            RestoreComboBox.SelectedIndex = restore >= 0 || restore <= 2 ? restore : 2;

            StartInTrayCheckBox.IsEnabled = (ShowTrayIconCheckBox.IsChecked == true);

            TelemetryRadio1.IsChecked = Module.PackageManager.GetTelemetry();
            TelemetryRadio2.IsChecked = !TelemetryRadio1.IsChecked;

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
            if (sender == ApplyTelemetryButton && _isLoaded)
            {
                Module.PackageManager.Elevated += SaveSettings;
                Module.PackageManager.Elevate();
                return;
            }

            SaveSettings();
        }
    }
}
