using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WPFMachine.Views
{
    internal class ListReverseConverter : IValueConverter
    {
        public static ListReverseConverter instance;
        public static ListReverseConverter Instance => instance ??= new ListReverseConverter() ;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable values)
            {
                return values.OfType<object>().Reverse().ToList();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable values)
            {
                return values.OfType<object>().Reverse().ToList();
            }
            return value;
        }
    }
}
