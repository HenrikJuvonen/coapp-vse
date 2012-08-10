namespace CoApp.VSE.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using Controls;

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            InitHeader();
        }

        private void InitHeader()
        {
            var border = Header;
            var restoreOnMouseMove = false;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                    e.Handled = true;
                }
                else
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        restoreOnMouseMove = true;
                    }
                }
            };

            border.MouseMove += (s, e) =>
            {
                if (restoreOnMouseMove)
                {
                    restoreOnMouseMove = false;

                    var width = RestoreBounds.Width;
                    var x = e.GetPosition(this).X - width / 2;

                    if (x < 0)
                    {
                        x = 0;
                    }
                    else if (x + width > System.Windows.SystemParameters.PrimaryScreenWidth)
                    {
                        x = System.Windows.SystemParameters.PrimaryScreenWidth - width;
                    }

                    WindowState = WindowState.Normal;
                    Left = x;
                    Top = 0;

                    try
                    {
                        DragMove();
                    }
                    catch
                    {
                    }
                }
            };
        }

        public void ExecuteToggleConsole(object sender = null, EventArgs e = null)
        {
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
