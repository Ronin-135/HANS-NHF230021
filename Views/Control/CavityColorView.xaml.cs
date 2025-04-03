using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFMachine.Views.Control
{
    /// <summary>
    /// CavityColorView.xaml 的交互逻辑
    /// </summary>
    public partial class CavityColorView : UserControl
    {
        public CavityColorView()
        {
            InitializeComponent();
        }

        public IEnumerable<object> Plts
        {
            get { return (IEnumerable<object>)GetValue(PltsProperty); }
            set
            {
                SetValue(PltsProperty, value);
            }
        }

        public void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            App.Current.Dispatcher.Invoke((Action)delegate ()
            {
                if (SelectPath != e.PropertyName) return;
                SelectValue = Plts.FirstOrDefault(plt => !(bool)(plt.GetType().GetProperty(SelectPath).GetValue(plt)));
            });


        }

        // Using a DependencyProperty as the backing store for Plts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PltsProperty =
            DependencyProperty.Register("Plts", typeof(IEnumerable<object>), typeof(CavityColorView), new PropertyMetadata(null, (d, e) =>
            {
                var newValue = (IEnumerable<object>)e.NewValue;
                var Cavity = (CavityColorView)d;
                foreach (var item in newValue)
                {
                    if (item is not INotifyPropertyChanged notify)
                        return;
                    notify.PropertyChanged += Cavity.NotifyPropertyChanged;
                }
            }));

        

        private static void call(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public object SelectValue
        {
            get { return (object)GetValue(SelectValueProperty); }
            set { SetValue(SelectValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectValueProperty =
            DependencyProperty.Register("SelectValue", typeof(object), typeof(CavityColorView), new PropertyMetadata(null));



        public string SelectPath
        {
            get { return (string)GetValue(SelectPathProperty); }
            set
            {
                SetValue(SelectPathProperty, value);
            }
        }
        // Using a DependencyProperty as the backing store for SelectPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectPathProperty =
            DependencyProperty.Register("SelectPath", typeof(string), typeof(CavityColorView), new PropertyMetadata(null));
        public int MaxRow
        {
            get { return (int)GetValue(MaxRowProperty); }
            set { SetValue(MaxRowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxRowProperty =
            DependencyProperty.Register("MaxRow", typeof(int), typeof(CavityColorView), new PropertyMetadata(1));

       


    }
}
