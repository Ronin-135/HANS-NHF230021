using HelperLibrary;
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
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOnloadNG : RunProcess
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
            Auto_WaitFinished,
            Auto_TransferBat,
            Auto_WorkEnd,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadNGMsgStartID,
            TransferTimeOut,
        }
        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】
        private int OTransferMotor;             // 转移电机
        private int IOffloadCheck;              // 出口下料检查
        private int IMidPos;                    // 中间位检查
        private int IPlaceCheck;                // 放料检查
        private int IManualBtn;                 // 输出按钮

        // 【模组参数】

        // 【模组数据】

        #endregion


        #region // 构造函数

        public RunProOnloadNG(int RunID) : base(RunID)
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
            IOffloadCheck = -1;
            IMidPos = -1;
            IPlaceCheck = -1;
            IManualBtn = -1;
            // 模组参数
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
            InputAdd("IOffloadCheck", ref IOffloadCheck);
            InputAdd("IMidPos", ref IMidPos);
            InputAdd("IPlaceCheck", ref IPlaceCheck);
            InputAdd("IManualBtn", ref IManualBtn);

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
                        OutputAction(OTransferMotor, false);
                        if (CheckInputState(IPlaceCheck, !IsEmptyRow(0)))
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

                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OnloadNGPlaceBattery, ref curState);
                        if (EventState.Invalid == curState || EventState.Finished == curState)
                        {
                            if (IsEmptyRow(0) && InputState(IPlaceCheck, false))
                            {
                                // 发送取料请求
                                SetEvent(this, ModuleEvent.OnloadNGPlaceBattery, EventState.Require);
                                WriteLog();
                                break;
                            }
                            else
                            {
                                // 转移电池
                                this.nextAutoStep = AutoSteps.Auto_TransferBat;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        else if (EventState.Response == curState)
                        {
                            if (CheckInputState(IPlaceCheck, false) && CheckInputState(IMidPos, false))
                            {
                                // 发送准备信号
                                OutputAction(OTransferMotor, false);
                                if (SetEvent(this, ModuleEvent.OnloadNGPlaceBattery, EventState.Ready))
                                {
                                    WriteLog();
                                    this.nextAutoStep = AutoSteps.Auto_WaitFinished;
                                    SaveRunData(SaveType.AutoStep);
                                }
                                break;
                            }
                        }

                        // 检查按钮
                        if (!Def.IsNoHardware() && InputState(IManualBtn, true) && EventState.Response != curState)
                        {
                            // 转移电池
                            OutputAction(OTransferMotor, true);
                        }
                        else
                        {
                            OutputAction(OTransferMotor, false);
                        }

                        // 检查下料端
                        if (!Def.IsNoHardware() && InputState(IOffloadCheck, true))
                        {
                            //Sleep(10);
                            Sleep(200);
                            if (InputState(IOffloadCheck, true))
                            {
                                ShowMsgBox.ShowDialog("NG电池已满，请人工取走NG电池", MessageType.MsgWarning, 15);
                            }
                        }

                        break;
                    }
                case AutoSteps.Auto_WaitFinished:
                    {
                        CurMsgStr("等待放料完成", "Wait place finished");

                        if (CheckEvent(this, ModuleEvent.OnloadNGPlaceBattery, EventState.Finished))
                        {
                            WriteLog();
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferBat:
                    {
                        CurMsgStr("转移电池", "Transfer Battery");

                        if (Def.IsNoHardware() || DryRun || TransferBattery())
                        {
                            // 数据转移
                            for (int nRowIdx = (Battery.GetLength(0) - 2); nRowIdx >= 0 ; nRowIdx--)
                            {
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    Battery[nRowIdx + 1, nColIdx].CopyFrom(Battery[nRowIdx, nColIdx]);
                                    Battery[nRowIdx, nColIdx].Release();
                                    Battery[nRowIdx + 1, nColIdx].Release();
                                }
                            }
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
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

        public bool InitRunDataB()
        {
            //if ((AutoSteps)this.nextAutoStep != AutoSteps.Auto_WaitWorkStart
            //     && (AutoSteps)this.nextAutoStep != AutoSteps.Auto_WorkEnd)
            //{
            //    string strInfo = string.Format("线体处于交互状态，不能清除数据！");
            //    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
            //    return false;
            //}

            base.InitRunData();
            return true;
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

            if (GetEvent(this, ModuleEvent.OnloadNGPlaceBattery, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
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
            if (runOnloadRobot.CheckRobotPos((int)OnloadRobotStation.NGOutput, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("上料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            base.InitRunData();
            OutputAction(OTransferMotor, false);
            SaveRunData(SaveType.SignalEvent | SaveType.AutoStep);
            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleTask()
        {
            return ClearModuleData();
        }

        #endregion


        /// <summary>
        /// 空行检查
        /// </summary>
        public bool IsEmptyRow(int nRow)
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
        /// 转移电池
        /// </summary>
        private bool TransferBattery()
        {
            TimeSpan TSpan;
            DateTime StartTime = DateTime.Now;
            bool bOffloadPrompt = false;
            bool bTransfer = false;

            // 检查下料端
            if (InputState(IOffloadCheck, true))
            {
                ShowMsgBox.ShowDialog("NG电池已满，请人工取走NG电池", MessageType.MsgWarning, 5);
                return false;
            }


            // 如果中间位有，则等待离开
            if (InputState(IMidPos, true) || !IsEmptyRow(0))
            {
                StartTime = DateTime.Now;
          
                OutputAction(OTransferMotor, true);

                while (true)
                {
                    if(InputState(IMidPos, true))
                    {

                        break;
                    }

                    TSpan = DateTime.Now - StartTime;
                    if (TSpan.TotalMilliseconds > 5 * 1000)
                    {
                        break;
                    }

                    Sleep(1);
                }
                while (true)
                {
                    if (InputState(IMidPos, false))
                    {
                        
                        bOffloadPrompt = true;
                        bTransfer = true;
                        break;
                    }

                    // 超时检查
                    TSpan = DateTime.Now - StartTime;
                    if (TSpan.TotalMilliseconds > 15 * 1000)
                    {
                        break;
                    }

                    Sleep(1);
                }
            }

            //Sleep(100);
            Sleep(1000);

            OutputAction(OTransferMotor, false);

            if (bOffloadPrompt && InputState(IOffloadCheck, true))
            {
                ShowMsgBox.ShowDialog("NG电池已满，请人工取走NG电池", MessageType.MsgWarning, 5);
            }

            if (!bTransfer)
            {
                ShowMessageBox((int)MsgID.TransferTimeOut, "转移NG电池过程超时", "请检查后重试", MessageType.MsgAlarm, 5);
            }

            return bTransfer;
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

            if (nBatIdx < 0 || nBatIdx >= 1)
            {
                return false;
            }

            if (bAlarm)
            {
                return CheckInputState(IMidPos, false) && CheckInputState(IPlaceCheck, bHasBat);
            }
            else
            {
                return InputState(IMidPos, false) && InputState(IPlaceCheck, bHasBat);
            }
        }
        public void WriteLog()
        {
            EventState curState = EventState.Invalid;
            GetEvent(this, ModuleEvent.OnloadNGPlaceBattery, ref curState);
            string strTmp = String.Format("{0} {1} {2} ", curState, this.nextAutoStep, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            MachineCtrl.GetInstance().WriteLog(strTmp, "D:\\LogFile", "RunProOnloadNG.log");
        }
    }
}
