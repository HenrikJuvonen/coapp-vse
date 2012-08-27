using System;
using System.Windows.Controls;
using System.Windows;

namespace CoApp.VSE.Core.Controls.Options
{
    using Utility;

    public partial class OptionsControl
    {
        public OptionsControl()
        {
            InitializeComponent();

            if (Module.PackageManager != null)
            {
                if (Module.PackageManager.Settings["#theme"].StringValue == "Dark")
                    ThemeComboBox.SelectedIndex = 0;
                else
                    ThemeComboBox.SelectedIndex = 1;
            }
        }

        private void ExecuteBack(object sender, EventArgs e)
        {
            Module.ShowMainControl();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            var theme = ThemeComboBox.SelectedIndex == 1 ? MahApps.Metro.Theme.Light : MahApps.Metro.Theme.Dark;

            Module.PackageManager.Settings["#theme"].StringValue = ThemeComboBox.SelectedIndex == 1 ? "Light" : "Dark";

            ThemeManager.ChangeTheme(Module.MainWindow, theme);
        }
    }
}
