using HelperLibrary;
using Newtonsoft.Json;
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
using static Machine.MachineCtrl;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOnloadLine : RunProcess
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
            Auto_MotorMoveRecvPos,
            Auto_WaitBatteryInpos,
            Auto_CheckBatteryStates,
            Auto_SendPickSignal,
            Auto_WaitPickFinish,
            Auto_WorkEnd,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadLineMsgStartID,
            RecvTimeout,
        }

        #endregion


        #region // 字段

        // 【相关模组】
        private RunProOnloadLineScan onloadLineScan;

        // 【IO/电机】
        public int IRecvHasBat;            // 入口接收有电池检查
        private int[] IBatInpos;            // 电池到位检查
        private int ISafePosCheck;          // 安全位检查
        private int IInposCheck;            // 到位检查

        private int OTransferMotor;         // 转移电机


        // 【模组参数】

        // 【模组数据】
        public bool bPickLineDown;          // 来料线取下降
        private int nCurRecvGroup;          // 当前接料组（2列为1组）
        public int nCurNGGroup;		        // 当前NG组（2列为1组）
        #endregion


        #region // 构造函数

        public RunProOnloadLine(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 1, 4, 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IRecvHasBat = -1;
            IBatInpos = new int[4] { -1, -1, -1, -1 };
            ISafePosCheck = -1;
            IInposCheck = -1;

            OTransferMotor = -1;

            // 模组参数

            // 模组数据
            bPickLineDown = false;
            nCurRecvGroup = -1;
            nCurNGGroup = -1;
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
            InputAdd("IRecvHasBat", ref IRecvHasBat);
            InputAdd("IBatInpos[1]", ref IBatInpos[0]);
            InputAdd("IBatInpos[2]", ref IBatInpos[1]);
            InputAdd("IBatInpos[3]", ref IBatInpos[2]);
            InputAdd("IBatInpos[4]", ref IBatInpos[3]);
            InputAdd("ISafePosCheck", ref ISafePosCheck);
            InputAdd("IInposCheck", ref IInposCheck);

            OutputAdd("OTransferMotor", ref OTransferMotor);

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
                        // 检查安全位感应器
                        if (!CheckInputState(ISafePosCheck, false))
                        {
                            bCheckOK = false;
                        }

                        if (bCheckOK)
                        {
                            OutputAction(OTransferMotor, false);
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

                        if (IsEmptyRow(0))
                        {
                            // 有取料请求
                            if (CheckEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Require))
                            {
                                SetEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Response);
                                this.nextAutoStep = AutoSteps.Auto_WaitBatteryInpos;
                                SaveRunData(SaveType.AutoStep);
                                MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOnlLineTime, false);
                                break;
                            }
                        }
                        else
                        {
                            HasNGCol();
                            this.nextAutoStep = AutoSteps.Auto_SendPickSignal;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }

                        MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOnlLineTime, true);
                        break;
                    }
                case AutoSteps.Auto_WaitBatteryInpos:
                    {
                        CurMsgStr("等待接收电池到位", "Wait battery inpos");

                        if (null != onloadLineScan)
                        {
                            if (CheckEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Require))
                            {
                                SetEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Response);
                            }

                            if (CheckEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Ready))
                            {
                                if (TransferBattery())
                                {                                   
                                    SetEvent(onloadLineScan, ModuleEvent.OnloadLineScanSendBat, EventState.Finished);
                                    this.nextAutoStep = AutoSteps.Auto_CheckBatteryStates;
                                    SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckBatteryStates:
                    {
                        CurMsgStr("检查电池状态", "Check Battery States");
                        if (HasNGCol() || !IsEmptyRow(0))
                        {
                            this.nextAutoStep = AutoSteps.Auto_SendPickSignal;

                        }
                        else
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        }
                        SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        break;
                    }
                case AutoSteps.Auto_SendPickSignal:
                    {
                        CurMsgStr("向上料机器人发送取料信号", "Send Pick Signal To OnloadRobot");

                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OnloadLinePickBattery, ref curState);
                        if (EventState.Finished == curState || EventState.Invalid == curState)
                        {
                            if (!IsEmptyRow(0))
                            {
                                if (CheckBatState())
                                {
                                    Sleep(200);
                                    HasNGCol();
                                    SetEvent(this, ModuleEvent.OnloadLinePickBattery, EventState.Require);
                                    this.nextAutoStep = AutoSteps.Auto_WaitPickFinish;
                                    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                                    break;
                                }
                            }
                        }

                        break;
                    }
                case AutoSteps.Auto_WaitPickFinish:
                    {
                        CurMsgStr("等待机器人取料完成", "Wait Onload Robot Pick Finished");
                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OnloadLinePickBattery, ref curState);

                        // 检测到连料感应器
                        if (!CheckInputState(ISafePosCheck, false))
                        {
                            break;
                        }

                        if (EventState.Response == curState)
                        {
                            SetEvent(this, ModuleEvent.OnloadLinePickBattery, EventState.Ready);
                        }
                        if (EventState.Finished == curState)
                        {
                            //检查电池数据与感应器状态
                            if (CheckBatState())
                            {
                                if (nCurNGGroup > -1)
                                {
                                    nCurNGGroup = -1;
                                }

                                if (IsEmptyRow(0))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;

                                }
                                else
                                {
                                    HasNGCol();
                                    this.nextAutoStep = AutoSteps.Auto_SendPickSignal;
                                }
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);                                         
                            }
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
            if (OTransferMotor > -1 && Outputs(OTransferMotor) == output)
            {
                if (!PickLineDown())
                {
                    return false;
                }          
            }
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

        public override void AfterStopAction()
        {
            // MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOnlLineTime, true);
            base.AfterStopAction();
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
            this.bPickLineDown = FileStream.ReadBool(RunModule, "bPickLineDown", this.bPickLineDown);
            this.nCurRecvGroup = FileStream.ReadInt(RunModule, "nCurRecvGroup", this.nCurRecvGroup);
            this.nCurNGGroup = FileStream.ReadInt(RunModule, "nCurNGGroup", this.nCurNGGroup);
            base.LoadRunData();
        }

        /// <summary>
        /// 保存运行数据
        /// </summary>
        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                FileStream.WriteBool(RunModule, "bPickLineDown", this.bPickLineDown);
                FileStream.WriteInt(RunModule, "nCurRecvGroup", this.nCurRecvGroup);
                FileStream.WriteInt(RunModule, "nCurNGGroup", this.nCurNGGroup);
            }

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

            if (GetEvent(this, ModuleEvent.OnloadLinePickBattery, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
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
            if (!(InputState(IBatInpos[0], false) && InputState(IBatInpos[1], false) && InputState(IBatInpos[2], false) 
                && InputState(IBatInpos[3], false) && InputState(IInposCheck, false)))
            {
                ShowMsgBox.ShowDialog("来料取料线感应器感应有电池，请人工取走后再清除来料取料线任务", MessageType.MsgMessage);
                return false;
            }
            RunProOnloadRobot runOnloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProOnloadRobot;
            if (runOnloadRobot.CheckRobotPos((int)OnloadRobotStation.OnloadLine, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("上料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            base.InitRunData();
            OutputAction(OTransferMotor, false);
            SaveRunData(SaveType.Battery| SaveType.AutoStep | SaveType.SignalEvent);
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


        #region // 模组参数和相关模组读取

        /// <summary>
        /// 参数读取（初始化时调用）
        /// </summary>
        public override bool ReadParameter()
        {
            base.ReadParameter();
            return true;
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;

            // 来料扫码
            strValue = IniFile.ReadString(strModule, "OnloadLineScan", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadLineScan = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadLineScan;
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
        /// 来料线NG电池列
        /// </summary>
        public bool HasNGCol()
        {
            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx += 2)
            {
                if (Battery[0, nColIdx].Type == BatType.NG || Battery[0, nColIdx + 1].Type == BatType.NG)
                {
                    nCurNGGroup = nColIdx;
                    return true;
                }
            }
            nCurNGGroup = -1;
            return false;
        }

        /// <summary>
        /// 来料线NG电池列
        /// </summary>
        public bool HasNGColData(int nColIdx, ref int nNGGroup)
        {
            if (Battery[0, nColIdx].Type == BatType.NG && Battery[0, nColIdx + 1].Type == BatType.NG)
            {
                nNGGroup = nColIdx;
                return true;
            }
            nNGGroup = -1;
            return false;
        }

        /// <summary>
        /// 满列检查
        /// </summary>
        public bool IsFullCol(int nRow)
        {
            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
            {
                if (Battery[nRow, nColIdx].Type == BatType.Invalid)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 判断只有一组电池
        /// </summary>
        public bool IsColCount()
        {
            if (Battery[0, 0].Type == BatType.OK && Battery[0, 1].Type == BatType.OK
               && Battery[0, 2].Type == BatType.Invalid && Battery[0, 3].Type == BatType.Invalid)
            {
                return true;
            }
            else if (Battery[0, 0].Type == BatType.Invalid && Battery[0, 1].Type == BatType.Invalid
                     && Battery[0, 2].Type == BatType.OK && Battery[0, 3].Type == BatType.OK)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 来料线有NG电池
        /// </summary>
        public bool HasNGBat()
        {
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (Battery[0, i].IsType(BatType.NG))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查电池数据与感应器状态是否匹配
        /// </summary>
        public bool CheckBatState()
        {
            bool bResult = true;
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (!CheckInputState(IBatInpos[i], Battery[0, i].Type > BatType.Invalid))
                {
                    bResult = false;
                    break;
                }
            }
            return bResult;
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
                return CheckInputState(IBatInpos[nBatIdx], bHasBat);
            }
            else
            {
                return InputState(IBatInpos[nBatIdx], bHasBat);
            }
        }

        /// <summary>
        /// 机器人取料位是否下降
        /// </summary>
        public bool PickLineDown(bool bAlarm = true)
        {
            if (bPickLineDown && bAlarm)
            {
                ShowMsgBox.Show(RunModule + "机器人来料线取料位下降，定位电机不能移动", MessageType.MsgWarning);
                return false;
            }
            return bPickLineDown;
        }

        /// <summary>
        /// 来料线有NG电池
        /// </summary>
        public bool HasNGBat(out int ngBatPos)
        {
            ngBatPos = 0;
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (Battery[0, i].IsType(BatType.NG))
                {
                    ngBatPos |= (0x01 << i);
                }
            }
            if (ngBatPos == 0)
            {
                return false;
            }
            return true;
        }

        public bool OnlineOkPos(ref int onLinePos)
        {
            onLinePos = 0;
            for (int nTmpIdx = 0; nTmpIdx < Battery.GetLength(1); nTmpIdx++)
            {
                if (Battery[0, nTmpIdx].Type == BatType.OK)
                {
                    onLinePos |= (0x01 << nTmpIdx);
                }
            }
            if (onLinePos == 0)
            {
                return false;
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
            bool bTransfer = false;

            // 开始转移
            OutputAction(OTransferMotor, true);

            while (true)
            {
                if (InputState(IInposCheck, true) && InputState(IBatInpos[0], true) && InputState(IBatInpos[1], true) && InputState(IBatInpos[2], true) && InputState(IBatInpos[3], true))
                {
                    bTransfer = true;
                    break;
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
                Sleep(300); // 延迟200豪秒停止
                OutputAction(OTransferMotor, false);
            }
            else
            {
                OutputAction(OTransferMotor, false);
                ShowMessageBox(1, "接收电池过程超时", "请检查来料取料线有料感应是否正常", MessageType.MsgAlarm);
            }

            // 检测到连料感应器
            if (!CheckInputState(ISafePosCheck, false))
            {
                bTransfer = false;
            }

            return bTransfer;
        }

    }
}
