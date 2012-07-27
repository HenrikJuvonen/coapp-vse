namespace CoApp.VSE
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using Controls;

    public partial class MainWindowNormal
    {
        public MainWindowNormal()
        {
            InitializeComponent();
        }

        public void ExecuteToggleConsole(object sender = null, EventArgs e = null)
        {
            Module.ToggleConsoleControl();

            if (ConsoleControl.IsVisible)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(
                    a => Dispatcher.Invoke(new Action(() => ConsoleControl.ConsoleBox.Focus())));
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
                if (item.Caption != "Add Filter..." && item.Caption != "Project" && item.Caption != "Feed URL")
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
