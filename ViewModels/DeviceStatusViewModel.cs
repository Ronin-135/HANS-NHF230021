using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Machine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using WPFMachine.Views;

namespace WPFMachine.ViewModels
{
    internal partial class DeviceStatusViewModel: ObservableObject
    {
        [ObservableProperty]
        private string codes = "停机";
        [ObservableProperty]
        private string codes1;

        [ObservableProperty]
        private ObservableCollection<string> items;
        [ObservableProperty]
        private ObservableCollection<string> items1;

        private static DeviceStatus deviceStatusWindow;

        public DeviceStatusViewModel()
        {
            //设备状态
            Items = new ObservableCollection<string>
            {
                "离线",
                "待机",
                "自动运行",
                "手动运行",
                "报警/故障",
                "停机",
                "维护",
            };
            //设备停机原因代码
            Items1 = new ObservableCollection<string>
            {
                "短停机",
                "待料",
                "吃饭",
                "换型",
                "设备故障",
                "来料不良",
                "设备校验",
                "首件点检",
                "品质异常",
                "堆料",
                "环境异常",
                "设定信息不完善",
                "其他"
            };

        }
        
        public Window CurrentWindow { get; set; }

        [RelayCommand]
        private  void UpdataMES()
        {
            string str = codes;
            string str1 = codes1;
            str = Items.IndexOf(str).ToString();
            str1 = Items1.IndexOf(str1).ToString();
            string strErr = "";
            CurrentWindow?.Close();
            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(null,str, str1,ref strErr);
        }

        public static void CheckCondition()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (deviceStatusWindow == null || !deviceStatusWindow.IsVisible)
                {
                    deviceStatusWindow = new DeviceStatus();

                    deviceStatusWindow.Show();
                }
            });
        }
    }
}
