using System;
using System.Globalization;
using System.Windows.Data;

namespace CoreControls.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        private static IntToBoolConverter instance = new IntToBoolConverter();
        public static IntToBoolConverter Instance
        {
            get
            {
                return instance;
            }
        }

        public object Convert(object o, Type type,
            object parameter, CultureInfo culture)
        {
            return ((int)o) > 0; 
        }

        public object ConvertBack(object o, Type type,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
