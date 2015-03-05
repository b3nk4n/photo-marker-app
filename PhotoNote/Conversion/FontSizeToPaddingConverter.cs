using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PhotoNote.Conversion
{
    /// <summary>
    /// Converter for font size to padding
    /// <remarks>
    /// DEPRECATED: not called while rendering!
    /// </remarks>
    /// </summary>
    public class FontSizeToPaddingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                double doubleValue = (double)value;
                double h = 12 + doubleValue / 6;
                double v = 10 + doubleValue / 10;
                return new Thickness(h, v, h - 2, v);
            }

            return new Thickness();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
