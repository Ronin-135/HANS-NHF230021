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
    /// RobotPalletView.xaml 的交互逻辑
    /// </summary>
    public partial class RobotPalletView : UserControl
    {
        public RobotPalletView()
        {
            InitializeComponent();
        }



        public Visibility Disable
        {
            get { return (Visibility)GetValue(DisableProperty); }
            set { SetValue(DisableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisableProperty =
            DependencyProperty.Register("Disable", typeof(Visibility), typeof(RobotPalletView), new PropertyMetadata(Visibility.Hidden));



        public Visibility PressureVisi
        {
            get { return (Visibility)GetValue(PressureVisiProperty); }
            set { SetValue(PressureVisiProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PressureVisi.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressureVisiProperty =
            DependencyProperty.Register("PressureVisi", typeof(Visibility), typeof(RobotPalletView), new PropertyMetadata(Visibility.Hidden));



        public Style BoderSty
        {
            get { return (Style)GetValue(BoderStyProperty); }
            set { SetValue(BoderStyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BoderSty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BoderStyProperty =
            DependencyProperty.Register("BoderSty", typeof(Style), typeof(RobotPalletView), new PropertyMetadata(null));



        public IList PltEnables
        {
            get { return (IList)GetValue(PltEnablesProperty); }
            set { SetValue(PltEnablesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PltEnables.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PltEnablesProperty =
            DependencyProperty.Register("PltEnables", typeof(IList), typeof(RobotPalletView), new PropertyMetadata(null));


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(RobotPalletView), new PropertyMetadata(""));


        public IEnumerable<object> Plts
        {
            get { return (IEnumerable<object>)GetValue(PltsProperty); }
            set {
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
            DependencyProperty.Register("Plts", typeof(IEnumerable<object>), typeof(RobotPalletView), new PropertyMetadata(null, (d, e) =>
            {
                var newValue = (IEnumerable<object>)e.NewValue;
                var Cavity = (RobotPalletView)d;
                foreach (var item in newValue)
                {
                    if (item is not INotifyPropertyChanged notify)
                        return;
                    notify.PropertyChanged += Cavity.NotifyPropertyChanged;
                }
            }));




        public object SelectValue
        {
            get { return (object)GetValue(SelectValueProperty); }
            set { SetValue(SelectValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectValueProperty =
            DependencyProperty.Register("SelectValue", typeof(object), typeof(RobotPalletView), new PropertyMetadata(null));




        public string SelectPath
        {
            get { return (string)GetValue(SelectPathProperty); }
            set { 
                SetValue(SelectPathProperty, value);
            }
        }
        public int MaxRow
        {
            get { return (int)GetValue(MaxRowProperty); }
            set { SetValue(MaxRowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxRowProperty =
            DependencyProperty.Register("MaxRow", typeof(int), typeof(RobotPalletView), new PropertyMetadata(1));

        public int MaxCol
        {
            get { return (int)GetValue(MaxColProperty); }
            set { SetValue(MaxColProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxCol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxColProperty =
            DependencyProperty.Register("MaxCol", typeof(int), typeof(RobotPalletView), new PropertyMetadata(1));
        // Using a DependencyProperty as the backing store for SelectPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectPathProperty =
            DependencyProperty.Register("SelectPath", typeof(string), typeof(RobotPalletView), new PropertyMetadata(null));


    }
}
