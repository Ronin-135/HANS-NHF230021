using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm.filter.log;
using HelperLibrary;
using Machine;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SystemControlLibrary;
using WPFMachine.Frame;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.Userlib;
using WPFMachine.Page;
using WPFMachine.Views;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.ViewModels
{
    partial class MainWindowViewModel : ObservableObjectResourceDictionary
    {
        [ObservableProperty]
        private ObservableCollection<MachinePage> navigationList = new();
        [ObservableProperty]
        private bool isClera = true;
        public string MainView => Page.RegionName.MainRegion;

        #region 命令

        private IRegionManager regionManager;

        public MachineCtrl Ctrl { get; }

        private LogIn logIn;

        [ObservableProperty]
        private ICollectionView navigationBar;
        private Timer outUser;
        private DateTime starctLogTime;

        [RelayCommand]
        void Resetting()
        {
            Ctrl.RunsCtrl.Reset();
        }
        [RelayCommand]
        void Reset()
        {
            SystemControlLibrary.MCState mCState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (mCState == SystemControlLibrary.MCState.MCRunning || mCState == SystemControlLibrary.MCState.MCInitializing)
            {    
                return;
            }
            if (ButtonResult.OK == ShowMsgBox.ShowDialog("整机重置会初始化所有数据！\r\n请确认是否整机重置", MessageType.MsgQuestion).Result)
            {
                Ctrl.RunsCtrl.Restart();
            }

        }

        [RelayCommand]
        void OpenLogIn()
        {
            if (Ctrl.CurUser != null)
            {
                Ctrl.CurUser = null;
                return;
            }
            logIn ??= (LogIn)((App)Application.Current).Container.Resolve(typeof(LogIn));
            logIn.Show();

        }

        #endregion

        public MainWindowViewModel(IRegionManager regionManager, MachineCtrl ctrl, IContainerProvider containerProvider, IDialogService dialogService)
        {
            ShowMsgBox.MsgBoxServer = dialogService;
            var navigationBar = containerProvider.Resolve<ObservableCollection<Navigable>>(Page.RegionName.MainRegion);

            NavigationBar = CollectionViewSource.GetDefaultView(navigationBar);

            NavigationBar.Filter = s =>
            {
                var nav = s as Navigable;
                return !nav.UserRoot || (nav.UserRoot && MachineCtrl.MachineCtrlInstance.CurUser == UserHelp.RootUser);
            };
            MachineCtrl.MachineCtrlInstance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MachineCtrl.CurUser))
                {
                    Application.Current.Dispatcher.BeginInvoke(() => NavigationBar.Refresh());
                    if (MachineCtrl.MachineCtrlInstance.CurUser == null)
                    {
                        IsClera = MachineCtrl.MachineCtrlInstance.CurUser != UserHelp.RootUser;
                    }
                    else
                    {
                        IsClera = MachineCtrl.MachineCtrlInstance.CurUser.Level.Name != UserHelp.RootUser.Level.Name;
                    }
                }
            };

            NavigationBar.Refresh();
            this.regionManager = regionManager;
            this.Ctrl = ctrl;
        }


        /// <summary>
        /// 关闭主窗体
        /// </summary>
        /// }
        [RelayCommand]
        private void Stop()
        {
            if (MachineCtrl.GetInstance().UpdataMES && (MachineCtrl.GetInstance().RunsCtrl.McState == MCState.MCRunning))
            {
                Ctrl.RunsCtrl.Stop();
                Task.Run(() =>
                {
                    DeviceStatusViewModel.CheckCondition();
                });
            }
            else
            {
                Ctrl.RunsCtrl.Stop();
            }
        }
        [RelayCommand]
        private void Start()
        {
            if (MachineCtrl.GetInstance().UpdataMES && !MachineCtrl.GetInstance().IsMESConnect)
            {
                ShowMsgBox.ShowDialog("MES通讯断开！\r\n 请通讯好后再试。", MessageType.MsgMessage);
                return;
            }
            Ctrl.RunsCtrl.Start();
        }
        [RelayCommand]

        private void Navigation(object ViewName)
        {
            regionManager.RequestNavigate(MainView, ViewName.ToString());
        }

        [RelayCommand]
        public void MouseMove()
        {
            UserHelp.UserLogInTime = DateTime.Now;
        }
    }


    public partial class Navigable : ObservableObjectResourceDictionary
    {
        /// <summary>
        /// 显示的名字
        /// </summary>
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        /// <summary>                 
        /// 导航的视图
        /// </summary>
        private string naviCmd;

        /// <summary>
        /// 显示的图标key
        /// </summary>
        [ObservableProperty]
        private string iconKind;

        /// <summary>
        /// 是否显示
        /// </summary>
        [ObservableProperty]
        private bool userRoot = false;


    }

}
