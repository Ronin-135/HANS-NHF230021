using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperLibrary;
using Machine.Framework.Robot;
using Machine;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;
using Machine.Framework.ExtensionMethod;
using System.Drawing;

namespace WPFMachine.ViewModels
{
    partial class RobotToolViewModel : ObservableObject
    {
        public RobotToolViewModel(IEnumerable<IRobot> OnLoadRobot)
        {
            OnOffRobots = OnLoadRobot.Where(A => A.GetType() != typeof(RunProTransferRobot));
            transferRobot = OnLoadRobot.Where(A => A.GetType() == typeof(RunProTransferRobot));
        }

        #region //上下料机器人
        /// <summary>
        /// 上下料选择机器人
        /// </summary>
        [ObservableProperty]
        private IRobot onOffCurRobot;

        /// <summary>
        /// 上下料机器人属性
        /// </summary>
        [ObservableProperty]
        public IEnumerable<IRobot> onOffRobots;

        [ObservableProperty]
        private KeyValuePair<int, RobotInfoStation> onOffCurPos;

        partial void OnOnOffCurPosChanged(KeyValuePair<int, RobotInfoStation> value)
        {
            SelectRowSource = Enumerable.Range(1, value.Value.RobotFormula.maxRow);
            SelectColSource = Enumerable.Range(1, value.Value.RobotFormula.maxCol);
        }

        /// <summary>
        /// 选择列
        /// </summary>
        [ObservableProperty]
        private IEnumerable<int> selectRowSource;

        /// <summary>
        /// 选择行
        /// </summary>
        [ObservableProperty]
        private IEnumerable<int> selectColSource;

        /// <summary>
        /// 上下料行选择
        /// </summary>
        [ObservableProperty]
        private int selectRowValue;
 

        /// <summary>
        /// 上下料列选择
        /// </summary>
        [ObservableProperty]
        private int selectColValue;

        /// <summary>
        /// 上下料机器人手动工位信息
        /// </summary>
        [ObservableProperty]
        private string onOffRobotHandInfo;

        [RelayCommand]
        void RobotConnect(string isConnect)
        {
            if (!Def.PingCheck(OnOffCurRobot.RobotIP()))
            {
                ShowMsgBox.ShowDialog(string.Format("{0}IP地址Ping不通,请检查网络是否连接!!!", OnOffCurRobot.RobotName()), MessageType.MsgWarning);
                return;
            }
            string info;
            bool connectState = Convert.ToBoolean(isConnect);
            if (OnOffCurRobot.RobotConnect(connectState))
            {
                info = connectState ? "连接成功" : "断开成功";
            }
            else
            {
                info = connectState ? "连接失败" : "断开失败";
            }
            ShowMsgBox.Show(OnOffCurRobot.RobotName() + $"{info}！！！", MessageType.MsgMessage);

        }


        [RelayCommand]

        private async Task RobotAction(string robotAction)
        {
            if (OnOffCurRobot == null)
            {
                ShowMsgBox.Show("请选择机器人！！！", MessageType.MsgMessage);
                return;
            }
            RobotAction RobotAction = Enum.Parse<RobotAction>(robotAction);
            if (SelectRowValue < 0 || SelectColValue < 0 || OnOffCurPos.Value == null)
            {
                ShowMsgBox.Show("请选择正确工位，再操作机器人", MessageType.MsgWarning);
                return;
            }
            int station = OnOffCurPos.Key;
            int row = SelectRowValue - 1;
            int col = SelectColValue - 1;
            OnOffRobotHandInfo = string.Format("{0}{1}-{2}工位-{3}行-{4}列", OnOffCurRobot.RobotName(),robotAction, station, SelectRowValue, SelectColValue);

            if (RobotAction == RobotAction.HOME)
            {

                if (await Task.Run<bool>(() => OnOffCurRobot.RobotHome()))
                {
                    ShowMsgBox.ShowDialog(OnOffCurRobot.RobotName() + $"{robotAction}成功！！！", MessageType.MsgMessage);
                }
            }
            else
            {
                // 手动操作站点检查
                if (RobotAction != RobotAction.UP && !OnOffCurRobot.ManualCheckStation(station, row, col, RobotAction, false))
                //if (!OnOffCurRobot.ManualCheckStation(station, row, col, RobotAction, false))//临时屏蔽机器人上升检查物流线感应器duanyh2024-1108
                {
                    return;
                }
                if (await Task.Run<bool>(() => OnOffCurRobot.RobotMove(station, row, col, OnOffCurRobot.RobotSpeed(), RobotAction, OnOffCurPos.Value.Motorpos, false)))
                {
                    ShowMsgBox.ShowDialog(OnOffCurRobot.RobotName() + $"{robotAction}成功！！！", MessageType.MsgMessage);
                }
            }
        }

        #endregion

        #region //调度机器人

        [ObservableProperty]
        private IRobot curTransferRobot;

        [ObservableProperty]
        private IEnumerable<IRobot> transferRobot;

        [ObservableProperty]
        private KeyValuePair<int, RobotInfoStation> curTransferRobotCurPos;

        partial void OnCurTransferRobotCurPosChanged(KeyValuePair<int, RobotInfoStation> value)
        {
            CurTransferRobotRowSource = Enumerable.Range(1, value.Value.RobotFormula.maxRow);
            CurTransferRobotColSource = Enumerable.Range(1, value.Value.RobotFormula.maxCol);
        }

        /// <summary>
        /// 调度行选项
        /// </summary>
        [ObservableProperty]
        private IEnumerable<int> curTransferRobotRowSource;
        /// <summary>
        /// 调度列选项
        /// </summary>
        [ObservableProperty]
        private IEnumerable<int> curTransferRobotColSource;

        /// <summary>
        /// 调度传感器
        /// </summary>
        [ObservableProperty]
        private IEnumerable<object> inputs;

        /// <summary>
        /// 行选择
        /// </summary>
        [ObservableProperty]
        private int curTransferRobotRowValue;

        /// <summary>
        /// 列选择
        /// </summary>
        [ObservableProperty]
        private int curTransferRobotColValue;

        /// <summary>
        /// 调度机器人手动工位信息
        /// </summary>
        [ObservableProperty]
        private string transferRobotHandInfo;
        [RelayCommand]
        async Task CurTransferRobotConnect(string isConnect)
        {
            if (!Def.PingCheck(CurTransferRobot.RobotIP()))
            {
                ShowMsgBox.ShowDialog(string.Format("{0}IP地址Ping不通,请检查网络是否连接!!!", CurTransferRobot.RobotName()), MessageType.MsgWarning);
                return;
            }
            string info;
            bool connectState = Convert.ToBoolean(isConnect);
            if (await Task.Run<bool>(() => CurTransferRobot.RobotConnect(connectState)))
            {
                info = connectState ? "连接成功" : "断开成功";
            }
            else
            {
                info = connectState ? "连接失败" : "断开失败";
            }
            ShowMsgBox.ShowDialog(CurTransferRobot.RobotName() + $"{info}！！！", MessageType.MsgMessage);
        }


        private int nRobotMovePos;           // (界面)调度机器人移动点位

        [RelayCommand]
        async Task CurTransferRobotAction(string robotAction)
        {
            if (CurTransferRobot == null)
            {
                ShowMsgBox.ShowDialog("请选择机器人！！！", MessageType.MsgMessage);
                return;
            }
            RobotAction RobotAction = Enum.Parse<RobotAction>(robotAction);
            if (CurTransferRobotRowValue < 0 || CurTransferRobotColValue < 0)
            {
                ShowMsgBox.ShowDialog("请选择正确工位，再操作调度机器人", MessageType.MsgWarning);
                return;
            }
            var info = CurTransferRobot.GetRobotActionInfo(false);
            int station = CurTransferRobotCurPos.Key;
            int row = CurTransferRobotRowValue - 1;
            int col = CurTransferRobotColValue - 1;
            int speed = CurTransferRobot.RobotSpeed();

            TransferRobotHandInfo = string.Format("调度机器人{0}-{1}工位-{2}行-{3}列", robotAction, station, CurTransferRobotRowValue, CurTransferRobotColValue);
            if (RobotAction == Machine.RobotAction.MOVE)
            {
                if ((RobotAction)info.action == Machine.RobotAction.PICKIN || (RobotAction)info.action == Machine.RobotAction.PLACEIN)
                {
                    string strInfo = string.Format("{1}当前动作:{0}，是否继续移动", ((RobotAction)info.action).ToString(), CurTransferRobot.RobotName());
                    if (ButtonResult.Retry == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                    {
                        return;
                    }
                }
                nRobotMovePos = station * (row + 1) * (col + 1);
            }
            else
            {
                if (nRobotMovePos != station * (row + 1) * (col + 1))
                {
                    ShowMsgBox.ShowDialog($"{robotAction}工位与移动工位不匹配，严禁取放托盘", MessageType.MsgWarning);
                    return;
                }
            }
            // 手动操作站点检查
            if (!CurTransferRobot.ManualCheckStation(station, row, col, RobotAction, RobotAction== Machine.RobotAction.PICKIN))
            {
                return;
            }
            if (await Task.Run<bool>(() => CurTransferRobot.RobotMove(station, row, col, speed, RobotAction, MotorPosition.Invalid, false)))
            {
                ShowMsgBox.ShowDialog(CurTransferRobot.RobotName() + $"{robotAction}成功！！！", MessageType.MsgMessage);
            }
        }

        /// <summary>
        /// 检查调度位置
        /// </summary>
        /// <param name="robot"></param>
        /// <returns></returns>
        private bool CheckCurTransferRobotPos(IRobot robot)
        {
            if (robot is RunProOffloadRobot)
            {
                return false;
            }
            return true;
        }


        //partial void OnCurTransferRobotChanged(IRobot value)
        //{
        //    Inputs = value?.GetInputState();
        //}
        #endregion
    }
}
