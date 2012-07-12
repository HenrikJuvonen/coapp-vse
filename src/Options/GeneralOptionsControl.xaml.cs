using System;
using System.IO;
using System.Diagnostics;
using CoApp.VisualStudio.VsCore;

namespace CoApp.VisualStudio.Options
{
    using OptionsResources = Resources;

    /// <summary>
    /// Interaction logic for GeneralOptionsControl.xaml
    /// </summary>
    public partial class GeneralOptionsControl
    {
        private string _lastItemsOnPage;

        public GeneralOptionsControl()
        {
            InitializeBase();
            InitializeComponent();

            LoadSettings();
        }

        /// <summary>
        /// This gets called when users click OK button.
        /// </summary>
        internal void ApplyChangedSettings()
        {
            CoAppWrapper.Settings["#update"].IntValue = UpdateComboBox.SelectedIndex;
            CoAppWrapper.Settings["#restore"].IntValue = RestoreComboBox.SelectedIndex;

            CoAppWrapper.Settings["#rememberFilters"].BoolValue = (RememberFiltersCheckBox.IsChecked == true);
            CoAppWrapper.Settings["#itemsOnPage"].IntValue = string.IsNullOrEmpty(ItemsOnPageTextBox.Text) ? 8 : int.Parse(ItemsOnPageTextBox.Text);

            CoAppWrapper.SetTelemetry(TelemetryCheckBox.IsChecked == true);
        }

        private void OnClearPackageCacheClick(object sender, EventArgs e)
        {
            var cache = new DirectoryInfo(@"C:\ProgramData\.cache\packages");

            try
            {
                foreach (var file in cache.GetFiles())
                {
                    file.Delete();
                }

                foreach (var dir in cache.GetDirectories())
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

            if (text.Length > 3)
            {
                text = text.Substring(0, 3);
            }

            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    int result = int.Parse(text);
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

        private void OnMouseEnter(object sender, EventArgs e)
        {
            ((System.Windows.UIElement)sender).Focus();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            Dummy.Focus();
        }

        private void LoadSettings()
        {
            int update = CoAppWrapper.Settings["#update"].IntValue;
            int restore = CoAppWrapper.Settings["#restore"].IntValue;
            bool rememberFilters = CoAppWrapper.Settings["#rememberFilters"].BoolValue;
            int itemsOnPage = CoAppWrapper.Settings["#itemsOnPage"].IntValue;

            UpdateComboBox.SelectedIndex = update == 0 || update == 1 ? update : 1;
            RestoreComboBox.SelectedIndex = restore >= 0 && restore <= 2 ? restore : 2;

            RememberFiltersCheckBox.IsChecked = rememberFilters;

            ItemsOnPageTextBox.Text = itemsOnPage < 5 || itemsOnPage > 1000 ? "8" : itemsOnPage.ToString();

            TelemetryCheckBox.IsChecked = CoAppWrapper.GetTelemetry();
        }
    }
}
