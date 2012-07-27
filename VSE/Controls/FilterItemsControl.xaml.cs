using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CoApp.Toolkit.Extensions;

namespace CoApp.VSE.Controls
{
    public partial class FilterItemsControl
    {
        private FilterControl _adder;

        public FilterItemsControl()
        {
            InitializeComponent();

            FilterBox.Items.Clear();
            _adder = new FilterControl(this);
            FilterBox.Items.Add(_adder);

            if (Module.PackageManager.Settings["#rememberFilters"].BoolValue)
            {
                var filters = Module.PackageManager.LoadFilters();

                foreach (var caption in filters.Keys)
                {
                    if (filters[caption].IsNullOrEmpty())
                    {
                        FilterBox.Items.Add(new FilterControl(this, caption));
                    }
                    else
                    {
                        var details = filters[caption];

                        if (!Module.IsDTELoaded)
                        {
                            if (details.Contains("In Solution"))
                                details.Remove("In Solution");

                            if (details.Contains("For Development"))
                                details.Remove("For Development");

                            if (!details.Any())
                                continue;
                        }
                        
                        FilterBox.Items.Add(new FilterControl(this, caption, details));
                    }
                }
            }
        }

        internal void Clear()
        {
            FilterBox.Items.Clear();
            _adder = new FilterControl(this);
            FilterBox.Items.Add(_adder);

            Module.PackageManager.Filters.Clear();
        }

        internal void AddFilter(string caption, string detail = null)
        {
            var adder = (FilterControl) FilterBox.Items[0];
            adder.SetFilterBlockVisibility(caption, detail, Visibility.Collapsed);

            foreach (FilterControl filterControl in FilterBox.Items)
            {
                if (filterControl.Caption == caption)
                {
                    filterControl.Details.Add(detail);
                    filterControl.Details.Sort();
                    filterControl.Refresh();
                    Module.MainWindow.MainControl.Reload();
                    return;
                }
            }

            FilterBox.Items.Add(new FilterControl(this, caption, detail == null ? new List<string>() : new List<string> { detail }));

            Module.MainWindow.MainControl.Reload();
        }

        internal void RemoveFilter(FilterControl filterControl, string detail = null)
        {
            var adder = (FilterControl)FilterBox.Items[0];

            if (detail != null)
            {
                adder.SetFilterBlockVisibility(filterControl.Caption, detail, Visibility.Visible);

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
                        adder.SetFilterBlockVisibility(filterControl.Caption, d, Visibility.Visible);
                }

                FilterBox.Items.Remove(filterControl);
            }

            Module.MainWindow.MainControl.Reload();
        }

        internal void RemoveFilters(string caption)
        {
            foreach (FilterControl filterControl in FilterBox.Items)
            {
                if (filterControl.Caption == caption)
                {
                    FilterBox.Items.Remove(filterControl);
                    Module.MainWindow.MainControl.Reload();
                    return;
                }
            }
        }
    }
}
