using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WPFMachine.Views
{
    public class BoolToConverter : DependencyObject, IValueConverter
    {

        


        public object TrueValue
        {
            get { return (object)GetValue(TrueValueProperty); }
            set { SetValue(TrueValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrueValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrueValueProperty =
            DependencyProperty.Register("TrueValue", typeof(object), typeof(BoolToConverter), new PropertyMetadata(null));  



        public object FalseValue
        {
            get { return (object)GetValue(FalseValueProperty); }
            set { SetValue(FalseValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FalseValueProperty =
            DependencyProperty.Register("FalseValue", typeof(object), typeof(BoolToConverter), new PropertyMetadata(null));

        public BoolToConverter()
        {

        }




        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.Equals(true) == true)
            {
                return TrueValue;
            }
            else
            {
                return FalseValue;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
