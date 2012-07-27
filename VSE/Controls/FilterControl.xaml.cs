using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CoApp.VSE.Extensions;
using EnvDTE;

namespace CoApp.VSE.Controls
{
    public partial class FilterControl
    {
        private readonly bool _isAdded;
        private bool _isExpanded;

        public string Caption { get; private set; }
        public List<string> Details { get; private set; }

        private readonly FilterItemsControl _itemsControl;

        public FilterControl(FilterItemsControl itemsControl, string caption = null, List<string> details = null)
        {
            InitializeComponent();

            _itemsControl = itemsControl;

            if (caption == null)
            {
                Caption = "Add Filter...";
                FilterCaption.Content = Caption;
                FilterDetails.Visibility = Visibility.Collapsed;
                _isAdded = false;

                foreach (var feedLocation in Module.PackageManager.GetFeedLocations())
                {
                    FilterFeedUrlComboBox.Items.Add(new ComboBoxItem { Content = feedLocation });
                }

                if (Module.IsDTELoaded)
                {
                    Module.DTE.Events.SolutionEvents.Opened += UpdateProjects;
                    Module.DTE.Events.SolutionEvents.BeforeClosing += UpdateProjects;
                    Module.DTE.Events.SolutionEvents.ProjectAdded += project => UpdateProjects();
                    Module.DTE.Events.SolutionEvents.ProjectRemoved += project => UpdateProjects();
                    Module.DTE.Events.SolutionEvents.ProjectRenamed += (project, name) => UpdateProjects();
                }
                else
                {
                    // Remove "For Development" and "In Solution"
                    FilterBooleanComboBox.Items.RemoveAt(1);
                    FilterBooleanComboBox.Items.RemoveAt(1);
                }
            }
            else
            {
                DataContext = this;
                Caption = caption;
                Details = details;
                FilterCaption.Content = Caption;
                _isAdded = true;

                LabelButton.Content = "r";

                if (Details != null && Details.Any())
                    LabelButton.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateProjects()
        {
            _itemsControl.RemoveFilters("Project");
            FilterProjectComboBox.Items.Clear();
            FilterProjectComboBox.Items.Add(new ComboBoxItem {Content = "Project", Visibility = Visibility.Collapsed});
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
                if ((string)item.Content == "In Solution" || (string)item.Content == "For Development")
                {
                    item.Visibility = anyProjects ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            FilterProjectComboBox.Visibility = anyProjects ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Refresh()
        {
            FilterDetails.Items.Refresh();
        }
        
        private void CollapseAll()
        {
            _isExpanded = false;

            LabelButton.Content = "u";
            
            FilterBlockContainer.Visibility = Visibility.Collapsed;
        }
        
        private void SetFilterBlockHostVisibility(object o, Visibility visibility)
        {
            ((UIElement) o).Visibility = visibility;
        }

        public void SetFilterBlockVisibility(string caption, string details, Visibility visibility)
        {
            foreach (var item in FilterBlockContainer.Children)
            {
                var textblock = item as Label;

                if (textblock != null && (string) textblock.Content == caption && string.IsNullOrEmpty(details))
                {
                    textblock.Visibility = visibility;
                    continue;
                }

                var panel = item as StackPanel;

                if (panel != null)
                {
                    foreach (var subitem in panel.Children)
                    {
                        textblock = subitem as Label;

                        if (textblock != null && (string) textblock.Content == details)
                        {
                            textblock.Visibility = visibility;
                        }
                    }

                    SetFilterBlockHostVisibility(panel, AreAllChildrenCollapsed(panel) ? Visibility.Collapsed : Visibility.Visible);

                    continue;
                }

                var combobox = item as ComboBox;

                if (combobox != null)
                {
                    foreach (var subitem in combobox.Items)
                    {
                        var comboboxitem = subitem as ComboBoxItem;

                        if (comboboxitem != null && (string) comboboxitem.Content == details)
                        {
                            comboboxitem.Visibility = visibility;
                        }
                    }

                    SetFilterBlockHostVisibility(combobox, AreAllItemsCollapsed(combobox) ? Visibility.Collapsed : Visibility.Visible);
                }
            }
        }

        private bool AreAllChildrenCollapsed(Panel o)
        {
            return o.Children.Cast<Label>().Aggregate(true, (current, item) => current && item.Visibility == Visibility.Collapsed);
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
            var comboBox = (ComboBox) sender;

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

            comboBox.Visibility = AreAllItemsCollapsed(comboBox) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAdded)
            {
                var src = e.Source as Label;

                if (src != null && src.Name == "FilterDetailButton")
                {
                    _itemsControl.RemoveFilter(this, (string) src.ToolTip);
                }
                else
                {
                    _itemsControl.RemoveFilter(this);
                }

                return;
            }

            if (e.Source == LabelButton)
            {
                if (_isExpanded)
                {
                    CollapseAll();
                }
                else
                {
                    _isExpanded = true;

                    LabelButton.Content = "t";

                    FilterBlockContainer.Visibility = Visibility.Visible;
                }
            }
            else if (e.Source is Label)
            {
                var filterblock = (Label) e.Source;

                _itemsControl.AddFilter((string) filterblock.Content);

                filterblock.Visibility = Visibility.Collapsed;
            }
        }
    }

    public class Attach
    {
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.RegisterAttached("InputBindings", typeof(InputBindingCollection), typeof(Attach),
            new FrameworkPropertyMetadata(new InputBindingCollection(),
            (sender, e) =>
            {
                var element = sender as UIElement;
                if (element == null) return;
                element.InputBindings.Clear();
                element.InputBindings.AddRange((InputBindingCollection)e.NewValue);
            }));

        public static InputBindingCollection GetInputBindings(UIElement element)
        {
            return (InputBindingCollection)element.GetValue(InputBindingsProperty);
        }

        public static void SetInputBindings(UIElement element, InputBindingCollection inputBindings)
        {
            element.SetValue(InputBindingsProperty, inputBindings);
        }
    }
}
