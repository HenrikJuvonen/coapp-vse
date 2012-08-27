using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CoApp.VSE.Core
{
    using Controls;

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public string CurrentView
        {
            get
            {
                var view = ViewContainer.Children.OfType<UserControl>().FirstOrDefault(n => n.IsVisible);
                return view == null ? "" : (string)view.Tag;
            }
        }

        public void GoTo(string view)
        {
            foreach (UserControl child in ViewContainer.Children)
            {
                child.Visibility = (string)child.Tag == view ? Visibility.Visible : Visibility.Collapsed;
            }
            
            VisualStudioControl.PackagesDataGrid.ItemsSource = null;
            SummaryControl.RemoveDataGrid.ItemsSource = null;
            SummaryControl.InstallDataGrid.ItemsSource = null;
            ProgressControl.ProgressDataGrid.ItemsSource = null;
            ProgressControl.Packages = null;
            ProgressControl.Log.Document.Blocks.Clear();
            InfoControl.DataContext = null;
        }

        private void CanExecuteMainViewCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = MainControl.IsVisible;
        }
        
        private void ExecuteFocusSearch(object sender, ExecutedRoutedEventArgs e)
        {
            if (!MainControl.SearchBox.IsFocused)
                MainControl.SearchBox.Focus();
        }

        public void ExecuteToggleConsole(object sender = null, EventArgs e = null)
        {
            if (Module.PackageManager.Settings["#showConsole"].BoolValue)
                return;

            MainControl.ConsoleControl.Visibility = MainControl.ConsoleControl.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

            if (MainControl.ConsoleControl.IsVisible)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    a => Dispatcher.Invoke(new Action(() => MainControl.ConsoleControl.ConsoleBox.Focus())));
            }
            else
            {
                FocusManager.SetFocusedElement(this, this);
            }
        }

        private void ExecuteReload(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteReload(null, null);
        }

        private void ExecuteApply(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteApply(null, null);
        }

        private void ExecuteShowOptions(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteShowOptions(null, null);
        }

        private void ExecuteBrowse(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteBrowse(null, null);
        }

        private void ExecuteMarkUpdates(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteMarkUpdates(null, null);
        }

        private void ExecuteMoreInformation(object sender, ExecutedRoutedEventArgs e)
        {
            MainControl.ExecuteMoreInformation(null, null);
        }

        private void ExecuteCancel(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentView == "Options" || CurrentView == "Info")
            {
                GoTo("Main");
            }
        }

        public void ExecuteChangeTheme(object sender, EventArgs e)
        {
            if (Module.PackageManager.Settings["#theme"].StringValue == "Dark")
            {
                Module.PackageManager.Settings["#theme"].StringValue = "Light";
                Utility.ThemeManager.ChangeTheme(Module.MainWindow, MahApps.Metro.Theme.Light);
            }
            else
            {
                Module.PackageManager.Settings["#theme"].StringValue = "Dark";
                Utility.ThemeManager.ChangeTheme(Module.MainWindow, MahApps.Metro.Theme.Dark);
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            WindowState = WindowState.Normal;

            if (Module.IsApplying)
            {
                Module.TrayIcon.Visible = true;
                e.Cancel = true;
                Hide();

                return;
            }

            var filters = new Dictionary<string, List<string>>();

            foreach (FilterControl item in MainControl.FilterItemsControl.FilterBox.Items)
            {
                if (item.Caption != "Project" && item.Caption != "Feed URL")
                {
                    if (!filters.ContainsKey(item.Caption))
                        filters.Add(item.Caption, item.Details);
                    else
                        filters[item.Caption].AddRange(item.Details);
                }
            }

            Module.PackageManager.SaveFilters(filters);

            if (!Module.IsShutdownForced && Module.PackageManager.Settings["#closeToTray"].BoolValue)
            {
                if (!Module.PackageManager.Settings["#showTrayIcon"].BoolValue)
                {
                    Module.TrayIcon.Visible = true;
                }
                e.Cancel = true;
                Hide();
            }
            else
            {
                if (Module.IsDTELoaded)
                    Hide();
                else
                    Application.Current.Shutdown();

                if (Owner != null)
                {
                    Owner.Activate();
                }

                e.Cancel = true;
            }
        }
    }
}
