using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoApp.VSE.Core.Extensions;
using EnvDTE;

namespace CoApp.VSE.Core.Controls
{
    public partial class FilterPopup
    {
        private readonly FilterItemsControl _itemsControl;

        public FilterPopup(FilterItemsControl itemsControl)
        {
            InitializeComponent();

            _itemsControl = itemsControl;

            foreach (var feedLocation in Module.PackageManager.GetFeedLocations())
            {
                FilterFeedUrlComboBox.Items.Add(new ComboBoxItem { Content = feedLocation });
            }

            if (Module.IsDTELoaded)
            {
                Module.DTE.Events.SolutionEvents.Opened += UpdateProjects;
                Module.DTE.Events.SolutionEvents.AfterClosing += UpdateProjects;
                Module.DTE.Events.SolutionEvents.ProjectAdded += project => UpdateProjects();
                Module.DTE.Events.SolutionEvents.ProjectRemoved += project => UpdateProjects();
                Module.DTE.Events.SolutionEvents.ProjectRenamed += (project, name) => UpdateProjects();
            }
            else
            {
                // Remove "Is Development Package" and "Is Used In Projects"
                FilterBooleanComboBox.Items.RemoveAt(1);
                FilterBooleanComboBox.Items.RemoveAt(1);
            }
            
        }

        public void UpdateProjects()
        {
            _itemsControl.RemoveFilters("Project");
            FilterProjectComboBox.Items.Clear();
            FilterProjectComboBox.Items.Add(new ComboBoxItem { Content = "Project", Visibility = Visibility.Collapsed });
            FilterProjectComboBox.SelectedIndex = 0;

            var anyProjects = false;

            if (Module.IsSolutionOpen)
            {
                var projects = Module.DTE.Solution.Projects.OfType<Project>().Where(n => n.IsSupported());

                foreach (var project in projects)
                {
                    FilterProjectComboBox.Items.Add(new ComboBoxItem { Content = project.GetName() });
                }

                anyProjects = projects.Any();
            }

            foreach (ComboBoxItem item in FilterBooleanComboBox.Items)
            {
                var detail = (string) item.Content;
                if (detail == "Is Used In Projects" || detail == "Is Development Package")
                {
                    var detailFiltered = Module.PackageManager.Filters.ContainsKey("Boolean") && !Module.PackageManager.Filters["Boolean"].Contains(detail);
                    item.Visibility = anyProjects && detailFiltered ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            FilterProjectComboBox.Visibility = anyProjects ? Visibility.Visible : Visibility.Collapsed;
            FilterProjectComboBox.IsEnabled = anyProjects;
        }

        public void SetFilterBlockVisibility(string caption, string details, Visibility visibility)
        {
            foreach (var item in FilterBlockContainer.Children)
            {
                var comboBox = item as ComboBox;

                if (comboBox != null)
                {
                    foreach (var subitem in comboBox.Items)
                    {
                        var comboboxitem = subitem as ComboBoxItem;

                        if (comboboxitem != null && (string)comboboxitem.Content == details)
                        {
                            comboboxitem.Visibility = visibility;
                        }
                    }

                    comboBox.IsEnabled = !AreAllItemsCollapsed(comboBox);
                }
            }
        }

        private bool AreAllItemsCollapsed(ItemsControl o)
        {
            return o.Items.Cast<ComboBoxItem>().Skip(1).Aggregate(true, (current, item) => current && item.Visibility == Visibility.Collapsed);
        }

        private void OnFilterNameTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && FilterNameTextBox.IsFocused)
            {
                _itemsControl.AddFilter("Name", FilterNameTextBox.Text);

                FilterNameTextBox.Clear();
            }
        }

        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;

            if (!comboBox.IsDropDownOpen)
            {
                comboBox.SelectedIndex = 0;
                return;
            }

            if (e.AddedItems.Count == 0 || comboBox.SelectedIndex == 0)
                return;

            var item = (ComboBoxItem)e.AddedItems[0];

            if (item.Visibility == Visibility.Collapsed)
                return;

            _itemsControl.AddFilter((string)((ComboBoxItem)comboBox.Items[0]).Content, (string)item.Content);
            item.Visibility = Visibility.Collapsed;

            comboBox.SelectedIndex = 0;

            comboBox.IsEnabled = !AreAllItemsCollapsed(comboBox);
        }
    }
}
