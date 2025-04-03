// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Window1.xaml.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Interaction logic for Window1.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFMachine.Views.Control
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ChartWin : UserControl
    {
        public ChartWin()
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
            DependencyProperty.Register("Disable", typeof(Visibility), typeof(ChartWin), new PropertyMetadata(Visibility.Hidden));
        
        public List<WPFMachine.Frame.RealTimeTemperature.Condition> Conditions
        {
            get { return (List<WPFMachine.Frame.RealTimeTemperature.Condition>)GetValue(ConditionsProperty); }
            set { SetValue(ConditionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConditionsProperty =
            DependencyProperty.Register("Conditions", typeof(List<WPFMachine.Frame.RealTimeTemperature.Condition>), typeof(ChartWin), new PropertyMetadata(null));

        public int CurCavity
        {
            get { return (int)GetValue(CurCavityProperty); }
            set { SetValue(CurCavityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurCavityProperty =
            DependencyProperty.Register("CurCavity", typeof(int), typeof(ChartWin), new PropertyMetadata(null));


        public int CurPallet
        {
            get { return (int)GetValue(CurPalletProperty); }
            set { SetValue(CurPalletProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurPalletProperty =
            DependencyProperty.Register("CurPallet", typeof(int), typeof(ChartWin), new PropertyMetadata(null));
        public List<int> Cavity
        {
            get { return (List<int>)GetValue(CavityProperty); }
            set { SetValue(CavityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CavityProperty =
            DependencyProperty.Register("Cavity", typeof(List<int>), typeof(ChartWin), new PropertyMetadata(null));


        public List<int> Pallet
        {
            get { return (List<int>)GetValue(PalletProperty); }
            set { SetValue(PalletProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PalletProperty =
            DependencyProperty.Register("Pallet", typeof(List<int>), typeof(ChartWin), new PropertyMetadata(null));
    }
}