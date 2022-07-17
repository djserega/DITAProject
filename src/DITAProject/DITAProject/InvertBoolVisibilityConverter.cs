using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ITAJira
{
    public class InvertBoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is bool)
                return (!(bool)value) ? Visibility.Visible : Visibility.Hidden;
            else
                throw new ArgumentException("Value must be of the type bool");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is bool)
                return (!(bool)value) ? Visibility.Visible : Visibility.Hidden;
            else
                throw new ArgumentException();
        }
    }
}
