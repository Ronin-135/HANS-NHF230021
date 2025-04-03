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
using System.Collections.ObjectModel;
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
    public partial class OvenChartWin : UserControl
    {
        public OvenChartWin()
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
            DependencyProperty.Register("Disable", typeof(Visibility), typeof(OvenChartWin), new PropertyMetadata(Visibility.Hidden));
        
        public ObservableCollection<WPFMachine.Frame.RealTimeTemperature.Condition> Conditions
        {
            get { return (ObservableCollection<WPFMachine.Frame.RealTimeTemperature.Condition>)GetValue(ConditionsProperty); }
            set { SetValue(ConditionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConditionsProperty =
            DependencyProperty.Register("Conditions", typeof(ObservableCollection<WPFMachine.Frame.RealTimeTemperature.Condition>), typeof(OvenChartWin), new PropertyMetadata(null));

        public List<string> Cavity
        {
            get { return (List<string>)GetValue(CavityProperty); }
            set { SetValue(CavityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CavityProperty =
            DependencyProperty.Register("Cavity", typeof(List<string>), typeof(OvenChartWin), new PropertyMetadata(null));


        public List<string> Pallet
        {
            get { return (List<string>)GetValue(PalletProperty); }
            set { SetValue(PalletProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Disable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PalletProperty =
            DependencyProperty.Register("Pallet", typeof(List<string>), typeof(OvenChartWin), new PropertyMetadata(null));
    }
}