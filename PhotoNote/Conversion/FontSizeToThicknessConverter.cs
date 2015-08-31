using System;
using System.Windows.Data;

namespace PhotoNote.Conversion
{
    /// <summary>
    /// Converts a font size to a border thickness.
    /// </summary>
    public class FontSizeToDoubleThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                double doubleValue = (double)value;
                return 1 + doubleValue / 12;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
