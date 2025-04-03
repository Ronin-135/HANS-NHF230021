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
    class IndexMapConversion : IMultiValueConverter
    {
        public IValueConverter Convertible { get; set; }

        public object Default { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is IList TheSource && values[2] is IList TheTarget && TheSource is not null && TheTarget is not null)
            {
                if (TheSource.Count != TheTarget.Count) throw new Exception("长度不相等");
                var index = TheSource.IndexOf(values[0]);

                return Convertible != null ? Convertible.Convert( TheTarget[index], targetType, parameter, culture) : TheTarget[index];
            }
            return Default != null ? Default : Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
}
