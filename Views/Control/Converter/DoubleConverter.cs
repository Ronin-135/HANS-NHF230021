using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WPFMachine.Views
{
    internal class DoubleConverter : DependencyObject, IValueConverter
    {


        public double Magnification
        {
            get { return (double)GetValue(MagnificationProperty); }
            set { SetValue(MagnificationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Magnification.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MagnificationProperty =
            DependencyProperty.Register("Magnification", typeof(double), typeof(DoubleConverter), new PropertyMetadata(1.0));



        public double HowMany
        {
            get { return (double)GetValue(HowManyProperty); }
            set { SetValue(HowManyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HowMany.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HowManyProperty =
            DependencyProperty.Register("HowMany", typeof(double), typeof(DoubleConverter), new PropertyMetadata(1.0));



        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double dou)
                return dou / Magnification * HowMany;

            throw new NotImplementedException();

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double dou) return null;
            return dou / HowMany * Magnification;
        }
    }
}
