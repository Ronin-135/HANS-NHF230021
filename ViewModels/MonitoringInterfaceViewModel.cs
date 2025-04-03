using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Machine;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.ModuleClass;
using WPFMachine.Views;

namespace WPFMachine.ViewModels
{
    internal partial class MonitoringInterfaceViewModel : ObservableObjectResourceDictionary, INavigationAware
    {
        private LoadBox loadbox;

        [ObservableProperty]
        private ObservableCollection<object> modules;

        public MonitoringInterfaceViewModel(LoadBox loadbox, IEnumerable<RunProcess> processes, RunProManualOperat onloadRobot) 
        {
            this.loadbox = loadbox;
            Modules = new ObservableCollection<object>(processes);

        }

        [RelayCommand]
        public void MoveModulesItem(object item)
        {
            int i = -1;
            Modules.ForEach((module, index)=> 
            {
                if (module == item)
                    i = index;
            });

            if (Modules != null && i >= 1 ) 
            {
                Modules.Move(i, i - 1);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }

        public void BoundData()
        {

        }


    }
}
