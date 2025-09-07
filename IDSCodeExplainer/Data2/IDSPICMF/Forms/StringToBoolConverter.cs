using System;
using System.Globalization;
using System.Windows.Data;

namespace IDS.PICMF.Forms
{
    public class StringToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // return true if value == parameter
            if (value == null)
            {
                return false;
            }

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return parameter if value is true
            if (value is bool)
            {
                var test = (bool) value;
                return test ? parameter : value;
            }
            return value;
        }

        #endregion
    }
}
