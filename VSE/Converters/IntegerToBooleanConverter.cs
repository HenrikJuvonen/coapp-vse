using System;
using System.Globalization;
using System.Windows.Data;

namespace CoApp.VSE.Converters
{
    public class IntegerToBooleanConverter : IValueConverter
    {
        public bool Inverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Inverted ? ((int) value != 0) : ((int) value == 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}