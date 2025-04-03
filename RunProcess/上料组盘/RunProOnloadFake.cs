using HelperLibrary;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;

namespace Machine
{
    class RunProOnloadFake : RunProcess
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckBat,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_InposCheck,
            Auto_WaitOnFinish,
            Auto_TransferBat,
            Auto_WorkEnd,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadFakeMsgStartID,
            CheckPickBatPos,
            PlaceFakeBatHint,
            TransferTimeout,
        }

        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】
        private int OTransferMotor;             // 转移电机
        private int IOnloadCheck;               // 入口上料检查
        private int IMidPos;                    // 中间位检查
        private int IInposCheck;                // 到位检查
        private int[] IBatInpos;                // 电池到位
        private int IManualBtn;                 // 人工按扭
        // 【模组参数】

        // 【模组数据】
        private DateTime dtStartTime;           // 起始时间
        #endregion


        #region // 构造函数

        public RunProOnloadFake(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 2, 4, 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            OTransferMotor = -1;
            IOnloadCheck = -1;
            IMidPos = -1;
            IInposCheck = -1;
            IBatInpos = new int[4] { -1, -1, -1, -1 };
            IManualBtn = -1;

            dtStartTime = DateTime.Now;
        }

        /// <summary>
        /// 读取模组配置
        /// </summary>
        public override bool InitializeConfig(string module)
        {
            // 基类初始化
            if (!base.InitializeConfig(module))
            {
                return false;
            }

            // 添加IO/电机
            OutputAdd("OTransferMotor", ref OTransferMotor);
            InputAdd("IOnloadCheck", ref IOnloadCheck);
            InputAdd("IMidPos", ref IMidPos);
            InputAdd("IInposCheck", ref IInposCheck);
            InputAdd("IManualBtn", ref IManualBtn);

            for (int nIdx = 0; nIdx < 4; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IBatInpos" + strIndex, ref IBatInpos[nIdx]);
            }

            return true;
        }

        #endregion


        #region // 模组运行

        protected override void PowerUpRestart()
        {
            base.PowerUpRestart();
            CurMsgStr("准备好", "Ready");

            InitRunData();
        }

        protected override void InitOperation()
        {
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                InitFinished();
                return;
            }

            switch ((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecover:
                    {
                        CurMsgStr("数据恢复", "Data recover");
                        if (MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckBat;
                        break;
                    }
                case InitSteps.Init_CheckBat:
                    {
                        CurMsgStr("检查电池状态", "Check battery status");

                        bool bCheckOK = true;
                        for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                        {
                            if (!CheckInputState(IBatInpos[nColIdx], Battery[0, nColIdx].Type > BatType.Invalid))
                            {
                                bCheckOK = false;
                                break;
                            }
                        }

                        if (bCheckOK)
                        {
                            this.nextInitStep = InitSteps.Init_End;
                        }
                        break;
                    }
                case InitSteps.Init_End:
                    {
                        CurMsgStr("初始化完成", "Init operation finished");
                        InitFinished();
                        break;
                    }

                default:
                    {
                        Trace.Assert(false, "this init step invalid");
                        break;
                    }
            }
        }

        protected override void AutoOperation()
        {
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }
            if (Def.IsNoHardware())
            {
                Sleep(10);
            }
           
            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        // 创建放料位电池
                        if (InputState(IOnloadCheck, true))
                        {
                            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                            {
                                Battery[1, nColIdx].Type = BatType.Fake;
                            }
                        }
                        // 取料位无电池
                        if (IsEmptyRow(0))
                        {
                            if (!IsEmptyRow(1) && InputState(IOnloadCheck, true) && InputState(IManualBtn, true) && InputState(IInposCheck, false))
                            {
                                this.nextAutoStep = AutoSteps.Auto_TransferBat;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                            }
                            else if (!DryRun)
                            {
                                // 缺料提示  每分钟
                                if ((DateTime.Now - dtStartTime).TotalSeconds > 10)
                                {
                                    ShowMessageBox((int)MsgID.PlaceFakeBatHint, "假电池位缺料", "请人工放入假电池！！！", MessageType.MsgWarning, 10);
                                    dtStartTime = DateTime.Now;
                                }
                            }
                            break;
                        }
                        else// 取料位有电池
                        {
                            EventState curState = EventState.Invalid;
                            GetEvent(this, ModuleEvent.OnloadFakePickBattery, ref curState);
                            if (EventState.Invalid == curState || EventState.Finished == curState)
                            {
                                int nPickColIdx = -1;

                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    if (Battery[0, nColIdx].Type > BatType.Invalid)
                                    {
                                        nPickColIdx = nColIdx;
                                        break;
                                    }
                                }
                                // 发送取料请求
                                SetEvent(this, ModuleEvent.OnloadFakePickBattery, EventState.Require, 0, nPickColIdx);
                                break;
                            }
                            else if (EventState.Response == curState)
                            {
                                if (!InputState(IMidPos, false))
                                {
                                    ShowMessageBox((int)MsgID.CheckPickBatPos, "假电池连料", "请检查电池是否到位或者感应器是否正常", MessageType.MsgWarning);
                                    break;
                                }
                                // 检查电池
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    if (Battery[0, nColIdx].Type > BatType.Invalid)
                                    {
                                        if (!CheckInputState(IBatInpos[nColIdx], true))
                                        {
                                            break;
                                        }
                                    }
                                }
                                // 发送准备信号
                                SetEvent(this, ModuleEvent.OnloadFakePickBattery, EventState.Ready);
                                break;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_InposCheck:
                    {
                        CurMsgStr("入口上料检查", "Inpos Check");
                        
                        Sleep(200);
                        if (InputState(IOnloadCheck, true))
                        {
                            dtStartTime = DateTime.Now;
                            this.nextAutoStep = AutoSteps.Auto_WaitOnFinish;
                            SaveRunData(SaveType.AutoStep);
                        }
                        else
                        {
                            ShowMessageBox((int)MsgID.PlaceFakeBatHint, "假电池位缺料", "请人工放入假电池！！！", MessageType.MsgWarning, 10);
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitOnFinish:
                    {
                        CurMsgStr("等待人工上料完成", "Wait On Finish");
                        TimeSpan timeSpan;

                        if (InputState(IOnloadCheck, true))
                        {
                            // 创建电池
                            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                            {
                                Battery[1, nColIdx].Type = BatType.Fake;
                            }
                        }
                        if(!IsEmptyRow(0))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
                        }
                        else if(InputState(IManualBtn, true))
                        {
                            if (Def.IsNoHardware() || DryRun || TransferBatteryOne(true))
                            {
                                // 数据转移
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    Battery[0, nColIdx].Type = InputState(IBatInpos[nColIdx], true) ? BatType.Fake : BatType.Invalid;
                                    Battery[1, nColIdx].Release();
                                }

                                Sleep(500);

                                // 传感器检查
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    Battery[0, nColIdx].Type = InputState(IBatInpos[nColIdx], true) ? BatType.Fake : BatType.Invalid;
                                }
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.Battery | SaveType.AutoStep);
                                break;
                            }
                        }
                        else
                        {                           
                            timeSpan = DateTime.Now - dtStartTime;
                            if (timeSpan.TotalMilliseconds > 2*60*1000 )
                            {
                                dtStartTime = DateTime.Now;
                                string strMSg = string.Format("人工上假电池超时");
                                String strDispose = string.Format("上完假电池后请按按钮！");
                                ShowMessageBox((int)MsgID.PlaceFakeBatHint, strMSg, strDispose, MessageType.MsgMessage);
                            }

                        }
                        break;
                    }
                case AutoSteps.Auto_TransferBat:
                    {
                        CurMsgStr("转移电池", "Transfer Battery");

                        if (Def.IsNoHardware() || DryRun || TransferBatteryOne(true))
                        {
                            // 传感器检查 数据转移
                            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                            {
                                Battery[0, nColIdx].Type = InputState(IBatInpos[nColIdx], true) ? BatType.Fake : BatType.Invalid;
                                Battery[1, nColIdx].Release();
                            }
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
                            break;
                        }
                        break;
                    }
                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "this auto step invalid");
                        break;
                    }
            }
        }

        #endregion


        #region // 防呆检查

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            return true;
        }

        /// <summary>
        /// 检查电机是否可移动
        /// </summary>
        public override bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            return true;
        }

        /// <summary>
        /// 模组防呆监视
        /// </summary>
        public override void MonitorAvoidDie()
        {
            return;
        }

        #endregion


        #region // 运行数据读写

        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public override void InitRunData()
        {
            base.InitRunData();
        }

        /// <summary>
        /// 加载运行数据
        /// </summary>
        public override void LoadRunData()
        {

            base.LoadRunData();
        }

        /// <summary>
        /// 保存运行数据
        /// </summary>
        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            base.SaveRunData(saveType, index);
        }

        /// <summary>
        /// 检查模组事件状态
        /// </summary>
        /// <returns></returns>
        public override bool CheckModuleEventState()
        {
            EventState curEventState = EventState.Invalid;
            int nEventRowIdx = -1;
            int nEventColIdx = -1;

            if (GetEvent(this, ModuleEvent.OnloadFakePickBattery, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
                (EventState.Response == curEventState || EventState.Ready == curEventState))
            {

                return false;
            }

            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleData()
        {
            RunProOnloadRobot runOnloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProOnloadRobot;
            if (runOnloadRobot.CheckRobotPos((int)OnloadRobotStation.FakeInput, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("上料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            base.InitRunData();
            OutputAction(OTransferMotor, false);
            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.SignalEvent);
            return true;
        }

        /// <summary>
        /// 清除模组任务
        /// </summary>
        public override bool ClearModuleTask()
        {
            return ClearModuleData();
        }
        #endregion


        #region // 检查

        /// <summary>
        /// 空行检查
        /// </summary>
        private bool IsEmptyRow(int nRow)
        {
            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
            {
                if (Battery[nRow, nColIdx].Type > BatType.Invalid)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查电池（硬件检测）
        /// </summary>
        public override bool CheckBattery(int nBatIdx, bool bHasBat, bool bAlarm = true)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            if (nBatIdx < 0 || nBatIdx >= IBatInpos.Length)
            {
                return false;
            }

            if (bAlarm)
            {
                return CheckInputState(IBatInpos[nBatIdx], bHasBat) && CheckInputState(IMidPos, false);
            }
            else
            {
                return InputState(IBatInpos[nBatIdx], bHasBat) && InputState(IMidPos, false);
            }
        }

        #endregion


        #region // 操作

        /// <summary>
        /// 转移电池一行
        /// </summary>
        private bool TransferBatteryOne(bool bInposCheck)
        {
            TimeSpan TSpan;
            DateTime StartTime = DateTime.Now;
            bool bTransfer = false;

            // 开始转移
            OutputAction(OTransferMotor, true);

            while (true)
            {
                if (bInposCheck)
                {
                    // 电池到位
                    if (InputState(IInposCheck, true))
                    {
                        bTransfer = true;
                        break;
                    }
                }
                // 超时检查
                TSpan = DateTime.Now - StartTime;
                if (TSpan.TotalMilliseconds > 20 * 1000)
                {
                    break;
                }

                Sleep(1);
            }

            if (bTransfer)
            {
                Sleep(800); // 延迟500豪秒停止
                OutputAction(OTransferMotor, false);
            }
            else
            {
                OutputAction(OTransferMotor, false);
                ShowMessageBox((int)MsgID.TransferTimeout, "转移假电池超时", "请检查后重试！！！", MessageType.MsgAlarm);
            }

            return bTransfer;
        }

        #endregion

    }
}
