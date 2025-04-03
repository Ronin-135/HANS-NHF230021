using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperLibrary;
using ImTools;
using Machine;
using Machine.Framework.ExtensionMethod;
using Machine.Framework.Robot;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SqlSugar;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure.Enumeration;
using static Machine.Framework.Robot.IStackerCrane;

namespace WPFMachine.ViewModels
{
    internal partial class DebuggingToolViewModel : ObservableObject, INavigationAware
    {
        private IRegionManager regionManager;
        [ObservableProperty]
        private ObservableCollection<Navigable> navigationBar;

        [ObservableProperty]
        private object curNaviga;


        public DebuggingToolViewModel(IRegionManager regionManager, IContainerProvider containerProvider)
        {
            this.regionManager = regionManager;
            NavigationBar = containerProvider.Resolve<ObservableCollection<Navigable>>(Page.RegionName.DebugginToolRegion);
            
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
            if (CurNaviga == null)
            {
                regionManager.RequestNavigate(Page.RegionName.DebugginToolRegion, NavigationBar.First().NaviCmd);
            }


        }

        [RelayCommand]
        private void Navigation(object[] ViewName)
        {
            if (ViewName.FirstOrDefault() is not Navigable navigable) return;
            regionManager.RequestNavigate(Page.RegionName.DebugginToolRegion, navigable.NaviCmd);
        }



    }
}
