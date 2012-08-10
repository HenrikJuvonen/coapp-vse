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
        private FilterMenu _filterPopup;

        public FilterItemsControl()
        {
            InitializeComponent();

            _filterPopup = new FilterMenu(this);

            FilterBox.Items.Clear();

            if (Module.PackageManager.Settings["#rememberFilters"].BoolValue)
            {
                var filters = Module.PackageManager.LoadFilters();

                foreach (var caption in filters.Keys)
                {
                    if (!filters[caption].IsNullOrEmpty())
                    {
                        var details = filters[caption];

                        if (!Module.IsDTELoaded)
                        {
                            if (details.Contains("Is Used In Projects"))
                                details.Remove("Is Used In Projects");
                            
                            if (!details.Any())
                                continue;
                        }

                        foreach (var detail in details)
                        {
                            _filterPopup.SetFilterBlockVisibility(caption, detail, Visibility.Collapsed);
                        }

                        FilterBox.Items.Add(new FilterControl(this, caption, details));
                    }
                }
            }
        }

        private void OnAddFilterButtonClick(object sender, EventArgs e)
        {
            _filterPopup.PlacementTarget = AddFilterButton;
            _filterPopup.Placement = PlacementMode.Bottom;
            _filterPopup.IsOpen = true;
            _filterPopup.StaysOpen = false;
        }

        private void OnClearButtonClick(object sender, EventArgs e)
        {
            Clear();
            Module.ReloadMainControl();
        }

        internal void Clear()
        {
            _filterPopup.IsOpen = false;
            _filterPopup = new FilterMenu(this);
            FilterBox.Items.Clear();
            Module.PackageManager.Filters.Clear();
        }

        internal void AddFilter(string caption, string detail = null)
        {
            _filterPopup.SetFilterBlockVisibility(caption, detail, Visibility.Collapsed);

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
                _filterPopup.SetFilterBlockVisibility(caption, detail, Visibility.Visible);

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
                        _filterPopup.SetFilterBlockVisibility(caption, d, Visibility.Visible);
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
