using CommunityToolkit.Mvvm.ComponentModel;
using Machine;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine
{
    partial class CavityRowData : ObservableObject
    {
        public IList<Pallet> Plts { get; set; }

        /// <summary>
        /// 腔体使能
        /// </summary>
        [ObservableProperty]
        private bool ovenEnable;

        /// <summary>
        /// 腔体保压
        /// </summary>
        [ObservableProperty]
        private bool pressure;

        /// <summary>
        /// 腔体状态
        /// </summary>
        [ObservableProperty]
        private CavityState state;


        [ObservableProperty]
        private int rowIndex;


        public CavityData CavityData = new CavityData();

        [ObservableProperty]
        public CavityData realTimeData = new CavityData();
    }
}
