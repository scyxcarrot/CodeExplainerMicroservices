using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using Media = System.Windows.Media;

namespace IDS.PICMF.Forms
{
    public class ColorToSolidColorBrushValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is Color))
            {
                return null;
            }

            var color = (Color)value;
            return new Media.SolidColorBrush(Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        #endregion
    }
}
