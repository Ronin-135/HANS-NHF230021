using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryIoc;
using HelperLibrary;
using Machine;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace WPFMachine.ViewModels
{
    partial class DryingOvenToolViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<object> ovens = new ObservableCollection<object>();

        [ObservableProperty]
        private RunProDryingOven curOvens;

        [ObservableProperty]

        private BindingList<object> cavitRows = new();

        [ObservableProperty]
        private int selectedIndex;

        [ObservableProperty]
        private ObservableCollection<object> pltList = new();

        [ObservableProperty]
        private ObservableCollection<object> alarm = new();

        [ObservableProperty]
        private ObservableCollection<PropView> parameters;

        private INotifyPropertyChanged paramChanged;

        [ObservableProperty]
        private CavityRowData rowData;

        public DryingOvenToolViewModel(IEnumerable<RunProDryingOven> dryingOvens)
        {
            Ovens.AddRange(dryingOvens);

        }
        partial void OnCurOvensChanged(RunProDryingOven value)
        {
            if (value == null) return;
            OnSelectedIndexChanged(SelectedIndex);
        }

        partial void OnSelectedIndexChanged(int value)
        {
            var obs = new ObservableCollection<object>();
            Alarm.Clear();

            if (CurOvens == null) return;
            RowData = (CavityRowData)CurOvens.CavityDataSource[value];
            for (int i = 0; i < RowData.Plts.Count(); i++)
            {
                obs.Add(new
                {
                    Plt = RowData.Plts[i],
                    UnBaseTempValue = RowData.RealTimeData.UnBaseTempValue[0][i],
                    poiling = RowData.RealTimeData.UnBaseTempValue[1][i],
                });
                Alarm.Add(new
                {
                    Plt = RowData.Plts[i],
                    BaseTempAlarmState = RowData.RealTimeData.UnAlarmTempState[i],
                });
            }
            Parameters ??= new();
            Parameters.Clear();
            if (paramChanged != null)
                paramChanged.PropertyChanged -= ParamChangedPropertyChanged;
            paramChanged = RowData.RealTimeData.ProcessParam;
            paramChanged.PropertyChanged += ParamChangedPropertyChanged;
            var parainfos = CavityData.PropListInfo;
            foreach (var propinfo in parainfos)
            {

                Parameters.Add(new PropView
                {
                    Name = propinfo.att.Name,
                    Val = Convert.ToDouble( propinfo.info.GetValue(paramChanged)),
                    Key = propinfo.info.Name,
                    GetVal = propinfo.info
                });
            }

            PltList = obs;
        }

        private void ParamChangedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var key = e.PropertyName;
            var propView = Parameters.First(p => p.Key == key);
            propView.Val = Convert.ToDouble( propView.GetVal.GetValue(sender));
        }

        [RelayCommand]
        public void Connect(bool isconnect)
        {
            if (CurOvens == null) return;

            string rest = "";
            if (CurOvens.DryOvenConnect(isconnect))
            {
                rest = isconnect ? "连接成功" : "断开成功";
                ShowMsgBox.ShowDialog($"{CurOvens.RunName}:{rest}", MessageType.MsgMessage);
            }
            else
            {
                rest = isconnect ? "连接失败" : "连接失败";
                ShowMsgBox.ShowDialog($"{CurOvens.RunName}:{rest}", MessageType.MsgMessage);
            }
        }

        [RelayCommand]
        public async Task OperateAction(DryParam dryOven)
        {
            await Task.Run(() =>
            {
                CurOvens.ManualAction(SelectedIndex, dryOven.Cmd, dryOven.CmdParam);
            });
        }


    }
    public class DryParam 
    {
        public DryOvenCmd Cmd { get; set; }

        public bool CmdParam { get; set; }  
    }


    partial class PropView : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private double val;

        [ObservableProperty]
        private string key;

        public PropertyInfo GetVal;
    }
}
