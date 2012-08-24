using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CoApp.VSE.Core.Converters
{
    public class WindowCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "pack://application:,,,/CoApp.VSE.Core;component/Resources/w" + value + ".png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
