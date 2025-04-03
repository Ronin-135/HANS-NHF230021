

using System;
using System.Collections.Generic;
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
    /// PalletAbbreviateView.xaml 的交互逻辑
    /// </summary>
    public partial class PalletAbbreviateView : UserControl
    {



        public bool WhetherToDisable
        {
            get { return (bool)GetValue(WhetherToDisableProperty); }
            set { SetValue(WhetherToDisableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WhetherToDisable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WhetherToDisableProperty =
            DependencyProperty.Register("WhetherToDisable", typeof(bool), typeof(PalletAbbreviateView), new PropertyMetadata(true));



        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(PalletAbbreviateView), new PropertyMetadata(null));





        public string Tips
        {
            get { return (string)GetValue(TipsProperty); }
            set { SetValue(TipsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Tips.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TipsProperty =
            DependencyProperty.Register("Tips", typeof(string), typeof(PalletAbbreviateView), new PropertyMetadata("", call));

        private static void call(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public PalletAbbreviateView()
        {
            InitializeComponent();
        }
    }
}
