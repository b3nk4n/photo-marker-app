using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PhotoNote.Conversion
{
    /// <summary>
    /// Converter for linear to quadratic conversion.
    /// </summary>
    public class LinearToQuadraticConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                var doubleValue = (double)value;
                double normalized = (doubleValue - 3) / 42;

                var resultNormalized = Math.Pow(normalized, 7.0 / 4);
                var result = (resultNormalized * 42) + 3;
                return result;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                var doubleValue = (double)value;
                double normalized = (doubleValue - 3) / 42;

                var resultNormalized = Math.Pow(normalized, 4.0 / 7);
                var result = (resultNormalized * 42) + 3;
                return result;
            }
            return value;
        }
    }
}
