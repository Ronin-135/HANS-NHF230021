using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Prism.Mvvm;
using WPFMachine.Frame.DataStructure;

namespace WPFMachine.Views.Control
{
    /// <summary>
    /// OvenView.xaml 的交互逻辑
    /// </summary>
    public partial class OvenView : UserControl
    {

        public int MaxRow
        {
            get { return (int)GetValue(MaxRowProperty); }
            set { SetValue(MaxRowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxRowProperty =
            DependencyProperty.Register("MaxRow", typeof(int), typeof(OvenView), new PropertyMetadata(1));




        public int MaxCol
        {
            get { return (int)GetValue(MaxColProperty); }
            set { SetValue(MaxColProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxCol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxColProperty =
            DependencyProperty.Register("MaxCol", typeof(int), typeof(OvenView), new PropertyMetadata(1));


        public int MaxCavityRow
        {
            get { return (int)GetValue(MaxCavityRowProperty); }
            set { SetValue(MaxCavityRowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxCavityRowProperty =
            DependencyProperty.Register("MaxCavityRow", typeof(int), typeof(OvenView), new PropertyMetadata(1));




        public int MaxCavityCol
        {
            get { return (int)GetValue(MaxCavityColProperty); }
            set { SetValue(MaxCavityColProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxCol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxCavityColProperty =
            DependencyProperty.Register("MaxCavityCol", typeof(int), typeof(OvenView), new PropertyMetadata(1));


        public OvenView()
        {
            InitializeComponent();
        }

    }

    public class Text : BindableBase
    {
        private int furnaceLayerCol;
        public int FurnaceLayerCol
        {
            get { return furnaceLayerCol; }
            set { SetProperty(ref furnaceLayerCol, value); }
        }

        private int furnaceLayerRow;
        public int FurnaceLayerRow
        {
            get { return furnaceLayerRow; }
            set { SetProperty(ref furnaceLayerRow, value); }
        }

        private ObservableCollection<object> cavityDataSource =  new ObservableCollection<object>();
        public ObservableCollection<object> CavityDataSource
        {
            get { return cavityDataSource; }
            set { SetProperty(ref cavityDataSource, value); }
        }

        public Text()
        {
            FurnaceLayerCol = 2;
            FurnaceLayerRow = 2;
            cavityDataSource.Add(new Cavity());
            cavityDataSource.Add(new Cavity());
            cavityDataSource.Add(new Cavity());
            cavityDataSource.Add(new Cavity());

        }




    }

    public class Cavity : BindableBase
    {
        private ObservableCollection<object> plts = new ObservableCollection<object>();
        public ObservableCollection<object> Plts
        {
            get { return plts; }
            set { SetProperty(ref plts, value); }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private int pltRow;
        public int PltRow
        {
            get { return pltRow; }
            set { SetProperty(ref pltRow, value); }
        }

        private int pltCol;
        public int PltCol
        {
            get { return pltCol; }
            set { SetProperty(ref pltCol, value); }
        }



        public Cavity()
        {
            PltRow = 6;
            PltCol = 1;
            Title = "测试1";
            var plt = new Pallet() { Type = PltType.OK };
            plt.Bat[0, 0].Type = BatType.Fake;
            plt.Bat[0, 0].Code = "Qiang12";
            plt.Code = "托盘条码: 1289034";
            plt.Name = "Test";
            Plts.Add(plt);
            var plt1 = new Pallet() { Type = PltType.OK };
            plt1.Bat[0, 0].Type = BatType.OK;
            Plts.Add(plt1);
            Plts.Add(new Pallet() { Type = PltType.OK} );
            Plts.Add(new Pallet() { Type = PltType.WaitOffload});
            Plts.Add(new Pallet() { Type = PltType.WaitRebakingToOven }) ;
            Plts.Add(new Pallet() { Type = PltType.WaitRebakeBat});
            
        }




    }
}
