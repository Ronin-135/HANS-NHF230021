using CommunityToolkit.Mvvm.ComponentModel;
using HelperLibrary;
using Machine;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using ScottPlot;
using CommunityToolkit.Mvvm.Input;
using WPFMachine.Views.Control;
using System.Windows;
using ScottPlot.Demo.WPF.WpfDemos;
using System.Windows.Media;
using Color = System.Drawing.Color;
using ImTools;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WPFMachine.ViewModels
{
    [RegionMemberLifetime(KeepAlive = true)]
    internal partial class AnimatedInterfaceViewModel : ObservableObjectResourceDictionary
    {
        #region 导航

        #endregion

        #region 获取模组
        public MachineCtrl MachineCtrl => container.Resolve<MachineCtrl>();
        public RunProOffloadRobot RunProOffloadRobot => container.Resolve<RunProOffloadRobot>();
        public RunProOffloadBuffer RunProOffloadBuffer => container.Resolve<RunProOffloadBuffer>();

        public RunProOffloadLine RunProOffloadLine => container.Resolve<RunProOffloadLine>();

        public RunProOffloadFake RunProOffloadFake => container.Resolve<RunProOffloadFake>();



        public RunProManualOperat RunProManualOperat => container.Resolve<RunProManualOperat>();
        public RunProPalletBuf RunProPalletBuf => container.Resolve<RunProPalletBuf>();
        public RunProCoolingStove RunProCoolingStove => container.Resolve<RunProCoolingStove>();

        public RunProOnloadRobot RunProOnloadRobot => container.Resolve<RunProOnloadRobot>();


        public RunProOnloadBuffer RunProOnloadBuffer => container.Resolve<RunProOnloadBuffer>();
        public RunProOnloadFake RunProOnloadFake => container.Resolve<RunProOnloadFake>();
        public RunProOnloadNG RunProOnloadNG => container.Resolve<RunProOnloadNG>();
        public RunProOnloadLineScan RunProOnloadLineScans => container.Resolve<RunProOnloadLineScan>();
        public RunProOnloadLine RunProOnloadLines => container.Resolve<RunProOnloadLine>();


        public RunProTransferRobot RunProTransferRobot => container.Resolve<RunProTransferRobot>();

        public IEnumerable<RunProDryingOven> RunProDryingOvens0 => GetRunProDryingOvens(0);
        public IEnumerable<RunProDryingOven> RunProDryingOvens1 => GetRunProDryingOvens(1);

        public IEnumerable<RunProDryingOven> GetRunProDryingOvens(int ovenGroup)
        {
            var v = container.Resolve<IEnumerable<RunProDryingOven>>();
            v = v.Where(A =>
            {
                if (ovenGroup == A.GetOvenDisplayGroup()) {
                    A.Pallet = A.Pallet.Reverse().ToArray();
                    return true;
                }
                return false;
            });
            return v.Reverse();
        }
        private IContainerExtension container;

        [ObservableProperty]
        private ObservableCollection<object> infoShow;
        [ObservableProperty]
        private ObservableCollection<object> listBoxCountShow;
        /// <summary>
        /// 清除生产数据
        /// </summary>
        [RelayCommand]
        void handClearProuductData()
        {
            MachineCtrl.GetInstance().DataList_Auto_Reset();

        }

        public AnimatedInterfaceViewModel(IContainerExtension container, IDialogService dialogService)
        {
            this.container = container;
            HelperLibrary.ShowMsgBox.MsgBoxServer = dialogService;
            InfoShow = new()
            {
                new ShowBatInfo {Type = BatType.OK, Name = "OK电池" },
                new ShowBatInfo {Type = BatType.NG, Name = "NG电池" },
                new ShowBatInfo{Type = BatType.Fake, Name = "假电池" },
                new ShowBatInfo{Type = BatType.BKFill, Name = "填充电池" },
                new ShowBatInfo {Type = BatType.TypeEnd, Name = "空托盘" },
                new ShowCavityInfo { State = CavityState.Work, Name = "烘烤中", Plts = Enumerable.Repeat(new Pallet(), 2).ForEach(plt=>{ plt.FillPltBat();plt.Type= PltType.OK; }).ToArray()},
                new ShowCavityInfo { State = CavityState.Detect, Name = "待出假电池", Plts = Enumerable.Repeat(new Pallet(), 2).ForEach(plt=>{ plt.FillPltBat();plt.Type= PltType.Detect;plt.Bat[0,0].Type= BatType.Fake; }).ToArray()},
                new ShowCavityInfo { State = CavityState.WaitRes, Name = "待上传水含量", Plts = Enumerable.Repeat(new Pallet(), 2).ForEach(plt => { plt.FillPltBat(); plt.Type = PltType.WaitRes; plt.Bat[0,0].Type= BatType.FakePos;}).ToArray()},
                new ShowCavityInfo { State = CavityState.Invalid, Name = "待下料", Plts = Enumerable.Repeat(new Pallet(), 2).ForEach(plt => { plt.FillPltBat(); plt.Type = PltType.WaitOffload; }).ToArray()},
                new ShowCavityInfo { State = CavityState.Rebaking, Name = "水含量超标", Plts = Enumerable.Repeat(new Pallet(), 2).ForEach(plt => { plt.FillPltBat(); plt.Type = PltType.WaitRebakeBat; }).ToArray()},

            };
            ListBoxCountShow = App.Ioc.Resolve<ObservableCollection<object>>("ShowProductDatas");



            //// 读取本地Mes下发参数数据
            //MachineCtrl.GetInstance().ReadMesCraftParamConfig();

            //bool bResult = true;
            //// Mes操作
            //for (int nOvenIdx = 0; nOvenIdx < (int)DryingOvenCount.DryingOvenNum; nOvenIdx++)
            //{
            //    if (!MachineCtrl.GetInstance().MesLoginCheck(nOvenIdx))
            //    {
            //        bResult = false;
            //        ShowMsgBox.ShowDialog("Mes登录失败，请手动在Mes设置设备登录中登录", MessageType.MsgAlarm);
            //        break;
            //    }
            //}

            //// 登录成功后调用参数下发
            //if (bResult)
            //{
            //    MachineCtrl.GetInstance().MesGetSpecifications();
            //}
        }

        #endregion
        
    }





    [INotifyPropertyChanged]
    partial class ShowCountInfo<T> : DependencyObject
    {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private string colorBrush;
        [ObservableProperty]
        private T productData;




    }

    class ShowBatInfo
    {
        public BatType Type { get; set; }

        public string Name { get; set; }
    }

    class ShowCavityInfo
    {
        public string Name { get; set; }

        public Pallet[] Plts { get; set; }

        public const int col = 2;// (int)Machine.PltMaxCount.PltCount;

        public int Col => col;

        public CavityState State { get; set; }

    }
}
