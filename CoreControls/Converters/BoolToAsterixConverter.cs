using System;
using System.Globalization;
using System.Windows.Data;

namespace CoreControls.Converters
{
    public class BoolToAsterixConverter : IValueConverter
    {
        private static BoolToAsterixConverter instance = new BoolToAsterixConverter();
        public static BoolToAsterixConverter Instance
        {
            get
            {
                return instance;
            }
        }

        public object Convert(object o, Type type,
            object parameter, CultureInfo culture)
        {
            return ((bool)o) ? "*" : "";
        }

        public object ConvertBack(object o, Type type,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
