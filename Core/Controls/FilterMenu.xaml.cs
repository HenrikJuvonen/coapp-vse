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
    public partial class FilterMenu
    {
        private readonly FilterItemsControl _itemsControl;

        public FilterMenu(FilterItemsControl itemsControl)
        {
            InitializeComponent();

            _itemsControl = itemsControl;

            foreach (var feedLocation in Module.PackageManager.GetFeedLocations())
            {
                var menuitem = new MenuItem { Header = feedLocation };
                menuitem.Click += OnMenuItemClick;
                FilterFeedUrlMenuItem.Items.Add(menuitem);
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
                // Remove "Is Used In Projects"
                FilterBooleanMenuItem.Items.RemoveAt(7);
            }
            
        }

        public void UpdateProjects()
        {
            _itemsControl.RemoveFilters("Project");
            FilterProjectMenuItem.Items.Clear();

            var anyProjects = false;

            if (Module.IsSolutionOpen)
            {
                var projects = Module.DTE.Solution.Projects.OfType<Project>().Where(n => n.IsSupported());

                foreach (var project in projects)
                {
                    var menuitem = new MenuItem { Header = project.GetName() };
                    menuitem.Click += OnMenuItemClick;
                    FilterProjectMenuItem.Items.Add(menuitem);
                }

                anyProjects = projects.Any();
            }

            foreach (MenuItem item in FilterBooleanMenuItem.Items)
            {
                var detail = (string) item.Header;
                if (detail == "Is Used In Projects")
                {
                    var detailFiltered = Module.PackageManager.Filters.ContainsKey("Boolean") && !Module.PackageManager.Filters["Boolean"].Contains(detail);
                    item.Visibility = anyProjects && detailFiltered ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            FilterProjectMenuItem.Visibility = anyProjects ? Visibility.Visible : Visibility.Collapsed;
            FilterProjectMenuItem.IsEnabled = anyProjects;
        }

        public void SetFilterBlockVisibility(string caption, string details, Visibility visibility)
        {
            foreach (var item in Items)
            {
                var menuitem = item as MenuItem;

                if (menuitem != null)
                {
                    foreach (var subitem in menuitem.Items)
                    {
                        var submenuitem = subitem as MenuItem;

                        if (submenuitem != null && (string)submenuitem.Header == details)
                        {
                            submenuitem.Visibility = visibility;
                        }
                    }

                    menuitem.IsEnabled = !AreAllItemsCollapsed(menuitem);
                }
            }
        }

        private bool AreAllItemsCollapsed(ItemsControl o)
        {
            return o.Items.Cast<MenuItem>().Aggregate(true, (current, item) => current && item.Visibility == Visibility.Collapsed);
        }

        private void OnFilterNameTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && FilterNameTextBox.IsFocused)
            {
                _itemsControl.AddFilter("Name", FilterNameTextBox.Text);

                FilterNameTextBox.Clear();
            }
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender == FilterNameMenuItem)
            {
                e.Handled = true;
                IsOpen = true;
                return;
            }

            var item = (MenuItem)sender;
            var parent = (MenuItem)item.Parent;

            _itemsControl.AddFilter((string)parent.Header, (string)item.Header);
            item.Visibility = Visibility.Collapsed;

            parent.IsEnabled = !AreAllItemsCollapsed(parent);
        }

        private void OnOpened(object sender, EventArgs e)
        {
            FilterNameMenuItem.IsEnabled = true;
        }
    }
}
