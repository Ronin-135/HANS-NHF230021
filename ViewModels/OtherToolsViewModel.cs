using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperLibrary;
using Machine;
using Machine.Framework.ExtensionMethod;
using Prism.Ioc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.RealTimeTemperature;

namespace WPFMachine.ViewModels
{
    internal partial class OtherToolsViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ScanCode> codes = new ObservableCollection<ScanCode>();

        [ObservableProperty]
        private ScanCode curCode;

        
        [ObservableProperty]
        private IEnumerable<IRobot> robots;

        [ObservableProperty]
        private IRobot curRobot;

        public object OvenChartModule { get; }




        public OtherToolsViewModel(IEnumerable<ScanCode> scans, IEnumerable<IRobot> robots, OvenChartWinModel ovenChart)
        {
            Codes.AddRange(scans);
            this.robots = robots;
            OvenChartModule = ovenChart;
        }
        [RelayCommand]
        private async Task ScanConnect()
        {

            if (await Task.Run<bool>(() => CurCode.Connect()))
            {

                ShowMsgBox.ShowDialog(CurCode.GetName() + "枪连接成功！！！", MessageType.MsgMessage);

            }
            else
            {
                ShowMsgBox.ShowDialog(CurCode.GetName() + "枪连接失败！！！", MessageType.MsgMessage);

            }

        }

        [RelayCommand]
        private async Task DisConnect()
        {
            if (await Task.Run<bool>(() => CurCode.Disconnect()))
            {
                ShowMsgBox.ShowDialog(CurCode.GetName() + "枪断开成功！！！", MessageType.MsgMessage);

            }
            else
            {
                ShowMsgBox.ShowDialog(CurCode.GetName() + "扫码枪断开成功！！！", MessageType.MsgMessage);

            }
        }

        [RelayCommand]
        private async Task Scan()
        {
            if (!CurCode.IsConnect())
            {
                ShowMsgBox.ShowDialog("请先连接扫码枪", MessageType.MsgMessage);
                return;
            }
            var str = string.Empty /*default(string)*/;
            if (await Task.Run<bool>(() => CurCode.SendAndWait(ref str)))
                ShowMsgBox.ShowDialog("扫码成功: " + str, MessageType.MsgMessage);
            else
                ShowMsgBox.ShowDialog("扫码失败:" + str, MessageType.MsgMessage);
        }

        [RelayCommand]
        private async Task ClawAction(bool? boolnull)
        {
            if (boolnull == null) return;
            var msg = boolnull.Value ? "关闭" : "打开";

            if (await Task.Run<bool>(() => CurRobot.FingerClose((uint)CurRobot.Finger_All, (bool)boolnull)))
            {
                ShowMsgBox.ShowDialog($"{CurRobot.RobotName()}抓手{msg}成功", MessageType.MsgMessage);

            }

        }


        }
}
