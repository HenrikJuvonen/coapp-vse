using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using CoApp.Toolkit.Extensions;

namespace CoApp.VSE.Core.Controls
{
    public partial class FilterItemsControl
    {
        private FilterMenu _filterMenu;

        public FilterItemsControl()
        {
            InitializeComponent();

            _filterMenu = new FilterMenu(this);

            FilterBox.Items.Clear();

            if (Module.PackageManager.Settings["#rememberFilters"].BoolValue)
            {
                var filters = Module.PackageManager.LoadFilters();

                foreach (var caption in filters.Keys)
                {
                    if (!Module.IsSolutionOpen && caption == "Projects")
                        continue;

                    if (!filters[caption].IsNullOrEmpty())
                    {
                        var details = filters[caption];

                        if (!Module.IsSolutionOpen)
                        {
                            if (details.Contains("Is Used In Projects"))
                                details.Remove("Is Used In Projects");

                            if (!details.Any())
                                continue;
                        }

                        foreach (var detail in details)
                        {
                            _filterMenu.SetFilterBlockVisibility(caption, detail, Visibility.Collapsed);
                        }

                        FilterBox.Items.Add(new FilterControl(this, caption, details));
                    }
                }
            }
            else
            {
                var details = new List<string> { "Is Installed" };

                if (Module.IsDTELoaded)
                {
                    details.Add("Is Development Package");
                }

                foreach (var detail in details)
                {
                    _filterMenu.SetFilterBlockVisibility("Boolean", detail, Visibility.Collapsed);
                }

                FilterBox.Items.Add(new FilterControl(this, "Boolean", details));
            }
        }

        private void OnAddFilterButtonClick(object sender, EventArgs e)
        {
            _filterMenu.PlacementTarget = AddFilterButton;
            _filterMenu.Placement = PlacementMode.Bottom;
            _filterMenu.IsOpen = true;
            _filterMenu.VerticalOffset = -4;
            _filterMenu.HorizontalOffset = -4;
            _filterMenu.StaysOpen = false;
            _filterMenu.Closed += (o, a) => AddFilterButton.IsChecked = false;
            _filterMenu.Opened += (o, a) => AddFilterButton.IsChecked = true;
        }

        private void OnClearButtonClick(object sender, EventArgs e)
        {
            Clear();
            Module.ReloadMainControl();
        }

        internal void Clear()
        {
            _filterMenu.IsOpen = false;
            _filterMenu = new FilterMenu(this);
            FilterBox.Items.Clear();
            Module.PackageManager.Filters.Clear();
        }

        internal void AddFilter(string caption, string detail = null)
        {
            _filterMenu.SetFilterBlockVisibility(caption, detail, Visibility.Collapsed);

            foreach (FilterControl filterControl in FilterBox.Items)
            {
                if (filterControl.Caption == caption)
                {
                    if (!filterControl.Details.Contains(detail))
                    {
                        filterControl.Details.Add(detail);
                        filterControl.Details.Sort();
                        filterControl.Refresh();
                        Module.ReloadMainControl();
                    }
                    return;
                }
            }

            FilterBox.Items.Add(new FilterControl(this, caption, detail == null ? new List<string>() : new List<string> { detail }));

            Module.ReloadMainControl();
        }

        internal void RemoveFilter(string caption, string detail = null)
        {
            var filterControl = FilterBox.Items.Cast<FilterControl>().FirstOrDefault(n => n.Caption == caption);

            if (filterControl == null)
                return;

            if (detail != null)
            {
                _filterMenu.SetFilterBlockVisibility(caption, detail, Visibility.Visible);

                filterControl.Details.Remove(detail);
                filterControl.Refresh();

                if (!filterControl.Details.Any())
                    FilterBox.Items.Remove(filterControl);
            }
            else
            {
                if (filterControl.Details != null)
                {
                    foreach (var d in filterControl.Details)
                        _filterMenu.SetFilterBlockVisibility(caption, d, Visibility.Visible);
                }

                FilterBox.Items.Remove(filterControl);
            }

            Module.ReloadMainControl();
        }

        internal void RemoveFilters(string caption)
        {
            foreach (FilterControl filterControl in FilterBox.Items)
            {
                if (filterControl.Caption == caption)
                {
                    FilterBox.Items.Remove(filterControl);
                    Module.ReloadMainControl();
                    return;
                }
            }
        }
    }
}
