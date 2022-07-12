using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ITAJira.Views.Reports
{
    public class ToolTipConverter : IValueConverter
    {
        internal static event Func<int, string, string>? FormatterToolTipReport;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FormatterToolTipReport?.Invoke((int)value, (string)parameter) ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
