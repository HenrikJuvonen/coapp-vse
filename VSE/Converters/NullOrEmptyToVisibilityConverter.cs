using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CoApp.Packaging.Common.Model;

namespace CoApp.VSE.Converters
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null
                || (value is string && string.IsNullOrEmpty(value as string))
                || (value is IEnumerable<string> && !((IEnumerable<string>)value).Any())
                || (value is IEnumerable<License> && !((IEnumerable<License>)value).Any()))
                ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
