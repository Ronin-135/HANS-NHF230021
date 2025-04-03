using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WPFMachine.Views
{
    class GetIndexMultiValueConverter
        : DependencyObject, IMultiValueConverter
    {



        public int Shifting
        {
            get { return (int)GetValue(ShiftingProperty); }
            set { SetValue(ShiftingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Shifting.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShiftingProperty =
            DependencyProperty.Register("Shifting", typeof(int), typeof(GetIndexMultiValueConverter), new System.Windows.PropertyMetadata(0));




        public bool InReverseOrder
        {
            get { return (bool)GetValue(InReverseOrderProperty); }
            set { SetValue(InReverseOrderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InReverseOrder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InReverseOrderProperty =
            DependencyProperty.Register("InReverseOrder", typeof(bool), typeof(GetIndexMultiValueConverter), new System.Windows.PropertyMetadata(false));











        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
          
            if (values.Length > 1 && values[1] is not null && values[1] is IEnumerable list)
            {
                var index = list.OfType<object>().Select<object, int?>((s, i) => s.Equals(values[0]) ? i : null).FirstOrDefault(i => i != null);
                if(index!=null)
                 return InReverseOrder ? list.OfType<object>().Count() - index.Value + Shifting : index.Value + Shifting;

            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
