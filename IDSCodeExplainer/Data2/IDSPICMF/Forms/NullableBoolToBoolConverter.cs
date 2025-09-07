using System;
using System.Globalization;
using System.Windows.Data;

namespace IDS.PICMF.Forms
{
    // converts 3 states (nullable bool) to 2 states (bool)
    // this can be used to bind radio button's IsChecked property
    public class NullableBoolToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // return true if value == parameter, else, return false
            if (parameter == null)
            {
                return value == null;
            }

            var test = (bool?) value;
            var result = bool.Parse((string) parameter);

            return test == result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return parameter if value is true
            if (value is bool)
            {
                var test = (bool) value;
                if (test)
                {
                    if (parameter == null)
                    {
                        return null;
                    }

                    return bool.Parse((string) parameter);
                }

                return null;
            }
            return value;
        }

        #endregion
    }
}
