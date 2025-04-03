using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.ExtensionMethod;

namespace WPFMachine.Views.Control
{
    /// <summary>
    /// BatterysView.xaml 的交互逻辑
    /// </summary>
    public partial class BatterysView : UserControl
    {


        public GridLength TitleHeight
        {
            get { return (GridLength)GetValue(TitleHeightProperty); }
            set { SetValue(TitleHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TitleHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleHeightProperty =
            DependencyProperty.Register("TitleHeight", typeof(GridLength), typeof(BatterysView), new PropertyMetadata(new GridLength(10, GridUnitType.Pixel)));



        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(BatterysView), new PropertyMetadata(""));




        public Orientation A
        {
            get { return (Orientation)GetValue(AProperty); }
            set { SetValue(AProperty, value); }
        }

        // Using a DependencyProperty as the backing store for A.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AProperty =
            DependencyProperty.Register("A", typeof(Orientation), typeof(BatterysView), new PropertyMetadata(Orientation.Horizontal, gyfy));

        private static void gyfy(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BatterysView batterys = (BatterysView)d;
            int temp = batterys.MaxRow;
            if (batterys.A == Orientation.Vertical)
            {
                batterys.MaxRow = batterys.MaxCol;
                batterys.MaxCol = temp;
            }
        }

        public IEnumerable Bats
        {
            get { return (IEnumerable)GetValue(BatsProperty); }
            set { SetValue(BatsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Bats.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BatsProperty =
            DependencyProperty.Register("Bats", typeof(IEnumerable), typeof(BatterysView), new PropertyMetadata(null));




        public Visibility ContentVisibility
        {
            get { return (Visibility )GetValue(ContentVisibilityProperty); }
            set { SetValue(ContentVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContentVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentVisibilityProperty =
            DependencyProperty.Register("ContentVisibility", typeof(Visibility ), typeof(BatterysView), new PropertyMetadata(Visibility.Visible));



        public Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(BatterysView), new PropertyMetadata(new Thickness(0)));



        public Thickness ContentBorderThickness
        {
            get { return (Thickness)GetValue(ContentBorderThicknessProperty); }
            set { SetValue(ContentBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContentBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentBorderThicknessProperty =
            DependencyProperty.Register("ContentBorderThickness", typeof(Thickness), typeof(BatterysView), new PropertyMetadata(new Thickness(0)));




        public int MaxRow
        {
            get {return (int)GetValue(MaxRowProperty);}
            set {SetValue(MaxRowProperty, value);}
        }

        // Using a DependencyProperty as the backing store for MaxRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxRowProperty =
            DependencyProperty.Register("MaxRow", typeof(int), typeof(BatterysView), new PropertyMetadata(1));




        public int MaxCol
        {
            get {return (int)GetValue(MaxColProperty);}
            set {SetValue(MaxColProperty, value);}
        }

        // Using a DependencyProperty as the backing store for MaxCol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxColProperty =
            DependencyProperty.Register("MaxCol", typeof(int), typeof(BatterysView), new PropertyMetadata(1));




        public bool IsReversal
        {
            get { return (bool)GetValue(IsReversalProperty); }
            set { SetValue(IsReversalProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReversal.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReversalProperty =
            DependencyProperty.Register("IsReversal", typeof(bool), typeof(BatterysView), new PropertyMetadata(false));








        public BatterysView()
        {
            this.Margin = new Thickness(5);
            InitializeComponent();

        }

    }
}
