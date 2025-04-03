using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure;

namespace WPFMachine.ViewModels
{
    public partial class BatterysViewModule : ObservableObjectResourceDictionary
    {
        [ObservableProperty]
        private ObservableCollection<object> bats = new ObservableCollection<object>();

        [ObservableProperty]
        private int maxrow;

        [ObservableProperty]
        private int maxCol;

        [ObservableProperty]
        private GridLength titleHeight;



        public double TitlePercent
        {
            set => TitleHeight = new GridLength(value, GridUnitType.Star);
            get => TitleHeight.Value;
        }

        public BatterysViewModule()
        {


        }

    }
}
