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
using static Machine.MachineCtrl;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOffloadLine : RunProcess
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
            Auto_SendPickSignal,
            Auto_WaitResetSignal,
            Auto_WorkEnd,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadLineMsgStartID,
        }
        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】
        private int IResponse;              // 2 响应：物流线正在准备
        private int IReady;                 // 3 准备好：物流线就绪可放料
        private int ORequire;               // 1 要料请求：请求电池
        private int OPlacing;               // 4 放料中：放料中，物流线不能移动
        private int[] IBatInpos;            // 电池到位检查
        private int MotorU;                 // 平移电机U

        // 【模组参数】
        private bool bConveyerLineEN;       // 下料对接使能：TRUE对接，FALSE不对接

        // 【模组数据】
        private bool isOffloadLineReady;    // 物流线准备OK

        #endregion


        #region // 构造函数

        public RunProOffloadLine(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 1, 4, 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("ConveyerLineEN", "下料物流线使能", "下料使能：TRUE对接物流线，FALSE不对接物流线", bConveyerLineEN);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IResponse = -1;
            IReady = -1;
            ORequire = -1;
            OPlacing = -1;
            IBatInpos = new int[4] { -1, -1, -1, -1 };

            // 模组参数
            bConveyerLineEN = false;
            isOffloadLineReady = false;
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
            InputAdd("IResponse", ref IResponse);
            InputAdd("IReady", ref IReady);
            OutputAdd("ORequire", ref ORequire);
            OutputAdd("OPlacing", ref OPlacing);
            InputAdd("IBatInpos[1]", ref IBatInpos[0]);
            InputAdd("IBatInpos[2]", ref IBatInpos[1]);
            InputAdd("IBatInpos[3]", ref IBatInpos[2]);
            InputAdd("IBatInpos[4]", ref IBatInpos[3]);
            MotorAdd("MotorU", ref MotorU);
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

                        for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                        {
                            if (!CheckInputState(IBatInpos[nColIdx], Battery[0, nColIdx].Type > BatType.Invalid))
                            {
                                break;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_End;
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

                        // 停止或开始下料
                        if (!OffLoad)
                        {
                            break;
                        }

                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OffloadLinePlaceBat, ref curState);
                        if (!isOffloadLineReady)
                        {
                            bool bReady = InputState(IReady, true);
                            bool bResponse = InputState(IResponse, true);
                            bool bRequire = OutputState(ORequire, true);
                            bool bPicking = OutputState(OPlacing, true);

                            // 发送请求
                            if (bConveyerLineEN && (!bRequire && !bPicking))
                            {
                                OutputAction(ORequire, true);
                                OutputAction(OPlacing, false);
                            }

                            // 有准备好信号
                            if (!bConveyerLineEN || bReady)
                            {
                                // 防止信号闪烁
                                Sleep(500);
                                bReady = InputState(IReady, true);
                                if (bConveyerLineEN && !bReady)
                                {
                                    break;
                                }

                                // 清除电池
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    Battery[0, nColIdx].Release();
                                    isOffloadLineReady = true;
                                    SaveRunData(SaveType.Variables | SaveType.Battery);
                                }

                                if (EventState.Invalid == curState || EventState.Finished == curState)
                                {
                                    // 发送取料请求
                                    SetEvent(this, ModuleEvent.OffloadLinePlaceBat, EventState.Require);
                                }
                            }
                        }
                        else
                        {
                            // 发送取料中
                            if (EventState.Response == curState && OutputAction(OPlacing, true) && OutputAction(ORequire, false) && (!bConveyerLineEN || InputState(IReady, true)) )
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendPickSignal;
                                SaveRunData(SaveType.AutoStep);
                                MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOffLineTime, false);
                                break;
                            }
                        }
                        MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOffLineTime, true);
                        break;
                    }
                case AutoSteps.Auto_SendPickSignal:
                    
                    {
                        CurMsgStr("发送取料信号", "Send Pick Signal");

                        //if (!bConveyerLineEN || /*(CheckInputState(IResponse, false) &&*/ InputState(IReady, true))
                        {
                            EventState curState = EventState.Invalid;
                            GetEvent(this, ModuleEvent.OffloadLinePlaceBat, ref curState);
                            if (EventState.Invalid == curState || EventState.Finished == curState)
                            {
                                if (!IsEmptyRow(0))
                                {
                                    // 放料完成
                                    if (!bConveyerLineEN || (OutputAction(ORequire, false) && OutputAction(OPlacing, false)))
                                    {
                                        
                                        this.nextAutoStep = AutoSteps.Auto_WaitResetSignal;
                                        SaveRunData(SaveType.AutoStep);
                                        break;
                                    }
                                }
                                else
                                {
                                    // 发送取料请求
                                    SetEvent(this, ModuleEvent.OffloadLinePlaceBat, EventState.Require);
                                }
                            }

                            if (EventState.Response == curState)
                            {
                                // 硬件检查
                                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                                {
                                    if (!CheckInputState(IBatInpos[0], Battery[0, nColIdx].Type > BatType.Invalid))
                                    {
                                        break;
                                    }
                                }
                                // 发送取料准备好
                                SetEvent(this, ModuleEvent.OffloadLinePlaceBat, EventState.Ready);

                                this.nextAutoStep = AutoSteps.Auto_WaitResetSignal;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_WaitResetSignal:
                    {
                        CurMsgStr("等待复位对接信号", "Wait reset Signal");
                        //if (!bConveyerLineEN || /*(InputState(IResponse, false) &&*/ InputState(IReady, false) )
                        {
                            EventState curState = EventState.Invalid;
                            GetEvent(this, ModuleEvent.OffloadLinePlaceBat, ref curState);
                            if (EventState.Invalid == curState || EventState.Finished == curState)
                            {
                                OutputAction(ORequire, false);
                                OutputAction(OPlacing, false);
                                isOffloadLineReady = false;
                                SetEvent(this, ModuleEvent.OffloadLinePlaceBat, EventState.Invalid);
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.Variables | SaveType.AutoStep);
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
            //MachineCtrl.MachineCtrlInstance.SetTiming(TimingType.WaitOffLineTime, false);
            base.AfterStopAction();
        }

        #endregion


        #region // 运行数据读写

        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public override void InitRunData()
        {
            // 待添加电池、托盘、信号 数据初始化
            isOffloadLineReady = false;
            OutputAction(ORequire, false);
            OutputAction(OPlacing, false);

            base.InitRunData();
        }

        public  bool InitRunDataB()
        {
            //if ((AutoSteps)this.nextAutoStep != AutoSteps.Auto_WaitWorkStart
            //    && (AutoSteps)this.nextAutoStep != AutoSteps.Auto_WorkEnd)
            //{
            //    string strInfo = string.Format("线体处于交互状态，不能清除数据！");
            //    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
            //    return false;
            //}
            // 待添加电池、托盘、信号 数据初始化
            isOffloadLineReady = false;
            OutputAction(ORequire, false);
            OutputAction(OPlacing, false);

            base.InitRunData();
            return true;
        }
        /// <summary>
        /// 加载运行数据
        /// </summary>
        public override void LoadRunData()
        {
            string section, key;
            section = this.RunModule;

            // 其他变量
            this.isOffloadLineReady = FileStream.ReadBool(section, "isOffloadLineReady", this.isOffloadLineReady);

            base.LoadRunData();
        }

        /// <summary>
        /// 保存运行数据
        /// </summary>
        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key;
            section = this.RunModule;

            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                // 其他变量
                FileStream.WriteBool(section, "isOffloadLineReady", this.isOffloadLineReady);
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

            if (GetEvent(this, ModuleEvent.OffloadLinePlaceBat, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
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
            RunProOffloadRobot runOffloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            if (runOffloadRobot.CheckRobotPos((int)OnloadRobotStation.OnloadLine, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("下料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            base.InitRunData();
            isOffloadLineReady = false;
            OutputAction(ORequire, false);
            OutputAction(OPlacing, false);
            SaveRunData(SaveType.Battery | SaveType.AutoStep);
            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        /// <returns></returns>
        public override bool ClearModuleTask()
        {

            if (!ClearModuleData())
            {
                return false;
            }
            return true;

        }
        #endregion


        #region // 模组参数和相关模组读取

        /// <summary>
        /// 参数读取（初始化时调用）
        /// </summary>
        public override bool ReadParameter()
        {
            base.ReadParameter();

            bConveyerLineEN = ReadParam(RunModule, "ConveyerLineEN", false);

            return true;
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;
        }

        #endregion


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
                return CheckInputState(IBatInpos[nBatIdx], bHasBat);
            }
            else
            {
                return InputState(IBatInpos[nBatIdx], bHasBat);
            }
        }

    }
}
