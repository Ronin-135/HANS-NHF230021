using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using System.Windows.Markup;

namespace WPFMachine.Views
{
    class EnumToConverter : DependencyObject, IValueConverter
    {



        public List<EnumItem> EnumItems
        {
            get { return (List<EnumItem>)GetValue(EnumItemsProperty); }
            set { SetValue(EnumItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnumItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnumItemsProperty =
                DependencyProperty.Register("EnumItems", typeof(List<EnumItem>), typeof(EnumToConverter),
    new FrameworkPropertyMetadata(new List<EnumItem>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public EnumToConverter()
        {
            EnumItems = new();

        }



        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var items = EnumItems.FirstOrDefault(val => value.ToString() == val.Key);
            return items?.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty("Value")]
    class EnumItem : DependencyObject
    {


        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Key.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(string), typeof(EnumItem), new PropertyMetadata(""));




        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(EnumItem), new PropertyMetadata(""));




    }
}
