using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CoApp.VSE.Core.Controls
{
    public partial class FilterControl
    {
        public string Caption { get; private set; }
        public List<string> Details { get; private set; }

        private readonly FilterItemsControl _itemsControl;

        public FilterControl(FilterItemsControl itemsControl, string caption, List<string> details = null)
        {
            InitializeComponent();

            _itemsControl = itemsControl;

            DataContext = this;
            Caption = caption;
            Details = details;
            FilterCaption.Content = Caption;
        }

        public void Refresh()
        {
            FilterDetails.Items.Refresh();
        }
        
        private void OnClick(object sender, RoutedEventArgs e)
        {
            var src = e.Source as FrameworkElement;

            if (src != null)
                _itemsControl.RemoveFilter(Caption, (string)src.Tag);
        }
    }
}
