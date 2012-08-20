﻿namespace CoApp.VSE.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using Controls;
    using System.Runtime.InteropServices;
    using System.Windows.Interop;

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void ExecuteFocusSearch(object sender, ExecutedRoutedEventArgs e)
        {
            if (!MainControl.IsVisible)
                return;

            if (!MainControl.SearchBox.IsFocused)
                MainControl.SearchBox.Focus();
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

        public void ExecuteToggleConsole(object sender = null, EventArgs e = null)
        {
            if (Module.PackageManager.Settings["#showConsole"].BoolValue || !MainControl.IsVisible)
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

                e.Cancel = true;
            }
        }
    }
}
