using System;
using System.Windows;
using System.Windows.Data;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                return string.IsNullOrEmpty(value.ToString()) ? Visibility.Collapsed : Visibility.Visible;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
