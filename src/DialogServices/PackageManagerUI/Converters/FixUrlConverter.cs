using System;
using System.Windows.Data;

namespace CoApp.VisualStudio.Dialog.PackageManagerUI
{
    public class FixUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Uri source = (Uri)value;
            
            if (source == null || !source.IsAbsoluteUri || String.IsNullOrEmpty(source.OriginalString))
            {
                source = null;
            }

            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}