using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace WPFMachine.Views
{
    internal class BitEnumerationConversion : DependencyObject, IValueConverter
    {
        

        public object TrueVal
        {
            get { return (object)GetValue(TrueValProperty); }
            set { SetValue(TrueValProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrueVal.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrueValProperty =
            DependencyProperty.Register("TrueVal", typeof(object), typeof(BitEnumerationConversion), new PropertyMetadata(null));



        public object FalseVal
        {
            get { return (object)GetValue(FalseValProperty); }
            set { SetValue(FalseValProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FalseVal.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FalseValProperty =
            DependencyProperty.Register("FalseVal", typeof(object), typeof(BitEnumerationConversion), new PropertyMetadata(null));




        public  object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumval && parameter is Enum param && param is not null && enumval is not null)
            {
                return enumval.HasFlag(param) ? TrueVal : FalseVal;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;

        }
    }
}
