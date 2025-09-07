using System;
using System.Globalization;
using System.Windows.Data;

namespace IDS.PICMF.Forms
{
    public class MultiBooleansAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                var wpfBoolean = (bool) value;
                if (!wpfBoolean)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBooleansAndConverter is a OneWay converter.");
        }
    }
}
