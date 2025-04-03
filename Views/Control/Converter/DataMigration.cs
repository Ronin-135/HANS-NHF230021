using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WPFMachine.Views
{
    internal class DataMigration : DependencyObject, IValueConverter



    {



        public double Migration
        {
            get { return (double)GetValue(MigrationProperty); }
            set { SetValue(MigrationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Migration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MigrationProperty =
            DependencyProperty.Register("Migration", typeof(double), typeof(DataMigration), new PropertyMetadata(0.0));





        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = System.Convert.ToDouble(value);
            val += Migration;
            return System.Convert.ChangeType(val, value.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = System.Convert.ToDouble(value);
            val -= Migration;
            return System.Convert.ChangeType(val, targetType);
        }
    }
}
