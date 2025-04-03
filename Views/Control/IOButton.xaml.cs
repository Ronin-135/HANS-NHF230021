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
    /// IOButton.xaml 的交互逻辑
    /// </summary>
    public partial class IOButton : UserControl
    {




        public Brush EnableBrush
        {
            get { return (Brush)GetValue(EnableBrushProperty); }
            set { SetValue(EnableBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableBrushProperty =
            DependencyProperty.Register("EnableBrush", typeof(Brush), typeof(IOButton), new PropertyMetadata(new SolidColorBrush() { Color = Colors.Black}, CallBack));


        


        public string TextNum
        {
            get { return (string)GetValue(TextNumProperty); }
            set { SetValue(TextNumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextNumProperty =
            DependencyProperty.Register("TextNum", typeof(string), typeof(IOButton), new PropertyMetadata(""));




        public string TextName
        {
            get { return (string)GetValue(TextNameProperty); }
            set { SetValue(TextNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextNameProperty =
            DependencyProperty.Register("TextName", typeof(string), typeof(IOButton), new PropertyMetadata(""));




        public Brush RoundBorder
        {
            get { return (Brush)GetValue(RoundBorderProperty); }
            set { SetValue(RoundBorderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RoundBorder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RoundBorderProperty =
            DependencyProperty.Register("RoundBorder", typeof(Brush), typeof(IOButton), new PropertyMetadata(new SolidColorBrush(Colors.Black)));






        public Brush EnableNoBrush
        {
            get { return (Brush)GetValue(EnableNoBrushProperty); }
            set { SetValue(EnableNoBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableNoBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableNoBrushProperty =
            DependencyProperty.Register("EnableNoBrush", typeof(Brush), typeof(IOButton), new PropertyMetadata(new SolidColorBrush(), CallBack));




        public Brush CurBrush
        {
            get { return (Brush)GetValue(CurBrushProperty); }
            set { SetValue(CurBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for curBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurBrushProperty =
            DependencyProperty.Register("CurBrush", typeof(Brush), typeof(IOButton), new PropertyMetadata(new SolidColorBrush(), CallBack));





        public bool Enable
        {
            get { return (bool)GetValue(EnableProperty); }
            set { SetValue(EnableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Enable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.Register("Enable", typeof(bool), typeof(IOButton), new PropertyMetadata(false, CallBack));






        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommandParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(IOButton), new PropertyMetadata(null));




        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(IOButton), new PropertyMetadata(null));





        private static void CallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var Button = (IOButton)d;
            Button.UpBrush();
        }
        public void UpBrush()
        {
            CurBrush = Enable ? EnableBrush : EnableNoBrush;
        }

        public IOButton()
        {
            InitializeComponent();
        }

    }
}
