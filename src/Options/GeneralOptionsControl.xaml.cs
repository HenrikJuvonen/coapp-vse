using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Options
{
    using OptionsResources = CoApp.VisualStudio.Options.Resources;

    /// <summary>
    /// Interaction logic for GeneralOptionsControl.xaml
    /// </summary>
    public partial class GeneralOptionsControl : WpfControl
    {
        private ISettings settings;

        private string _lastItemsOnPage;

        public GeneralOptionsControl()
        {
            InitializeBase();
            InitializeComponent();

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "coapp-vse");
            
            settings = new Settings(path);

            LoadSettings();
        }

        /// <summary>
        /// This gets called when users click OK button.
        /// </summary>
        internal void ApplyChangedSettings()
        {
            SaveUpdateComboBoxValue();
            SaveRestoreComboBoxValue();

            settings.SetValue("coapp", "rememberFilters", (RememberFiltersCheckBox.IsChecked == true).ToString());
            settings.SetValue("coapp", "itemsOnPage", ItemsOnPageTextBox.Text);
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            DirectoryInfo cache = new DirectoryInfo(@"C:\ProgramData\.cache\packages");

            try
            {
                foreach (FileInfo file in cache.GetFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in cache.GetDirectories())
                {
                    dir.Delete(true);
                }

                MessageHelper.ShowInfoMessage(OptionsResources.ShowInfo_ClearPackageCache, null);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowErrorMessage(ex, null);
            }
        }

        private void OnItemsOnPageChanged(object sender, EventArgs e)
        {
            string text = ItemsOnPageTextBox.Text;
            int result = 0;

            if (text.Length > 3)
            {
                text = text.Substring(0, 3);
            }

            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    result = int.Parse(text);
                }

                ItemsOnPageTextBox.Text = text;
                ItemsOnPageTextBox.CaretIndex = text.Length;

                _lastItemsOnPage = text;
            }
            catch
            {
                ItemsOnPageTextBox.Text = _lastItemsOnPage;
            }
        }

        private void OnBrowsePackageCacheClick(object sender, EventArgs e)
        {
            Process.Start(@"C:\ProgramData\.cache\packages");
        }

        private void OnUpdateComboBoxMouseEnter(object sender, EventArgs e)
        {
            UpdateComboBox.Focus();
        }

        private void OnRestoreComboBoxMouseEnter(object sender, EventArgs e)
        {
            RestoreComboBox.Focus();
        }

        private void LoadSettings()
        {
            string update = settings.GetValue("coapp", "update");
            string restore = settings.GetValue("coapp", "restore");
            string rememberFilters = settings.GetValue("coapp", "rememberFilters");
            string itemsOnPage = settings.GetValue("coapp", "itemsOnPage");

            switch (update)
            {
                case "automatic":
                    UpdateComboBox.SelectedIndex = 0;
                    break;
                case "notify":
                    UpdateComboBox.SelectedIndex = 1;
                    break;
                case "nothing":
                default:
                    UpdateComboBox.SelectedIndex = 2;
                    break;
            }

            switch (restore)
            {
                case "automatic":
                    RestoreComboBox.SelectedIndex = 0;
                    break;
                case "notify":
                    RestoreComboBox.SelectedIndex = 1;
                    break;
                case "nothing":
                default:
                    RestoreComboBox.SelectedIndex = 2;
                    break;
            }

            bool rememberFiltersChecked;
            bool.TryParse(rememberFilters, out rememberFiltersChecked);
            RememberFiltersCheckBox.IsChecked = rememberFiltersChecked;

            ItemsOnPageTextBox.Text = itemsOnPage ?? "8";
        }

        private void SaveUpdateComboBoxValue()
        {
            string value = "nothing";

            switch (UpdateComboBox.SelectedIndex)
            {
                case 0:
                    value = "automatic";
                    break;
                case 1:
                    value = "notify";
                    break;
            }

            settings.SetValue("coapp", "update", value);
        }

        private void SaveRestoreComboBoxValue()
        {
            string value = "nothing";

            switch (RestoreComboBox.SelectedIndex)
            {
                case 0:
                    value = "automatic";
                    break;
                case 1:
                    value = "notify";
                    break;
            }

            settings.SetValue("coapp", "restore", value);
        }
    }
}
