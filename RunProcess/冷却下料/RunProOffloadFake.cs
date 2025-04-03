using HelperLibrary;
using Newtonsoft.Json;
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
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOffloadFake : RunProcess
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

        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】
        private int[] OTransferMotor;             // 转移电机
        private int[] IOffloadCheck;              // 出口下料检查
        private int IMidPos;                    // 中间位检查
        private int[] IPlaceCheck;                // 放料检查

        // 【模组参数】
        public bool LeftTryEnable;          // 左边拉带使能
        public bool RightTryEnable;        // 右边拉带使能

        // 【模组数据】

        #endregion


        #region // 构造函数

        public RunProOffloadFake(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 3, 1, 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("LTryEnable", "一号通道使能", "TRUE启用，FALSE禁用", LeftTryEnable);
            //InsertPrivateParam("RTryEnable", "二号通道使能", "TRUE启用，FALSE禁用", RightTryEnable);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            OTransferMotor = new int[1] { -1 };
            IOffloadCheck = new int[1] { -1 };
            IMidPos = -1;
            IPlaceCheck = new int[1] { -1 };

            // 模组参数

            LeftTryEnable = false;
            RightTryEnable = false;
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

            for (int i = 0; i < 1; i++)
            {
                OutputAdd($"OTransferMotor[{i + 1}]", ref OTransferMotor[i]);
                InputAdd($"IOffloadCheck[{i + 1}]", ref IOffloadCheck[i]);
                InputAdd($"IPlaceCheck[{i + 1}]", ref IPlaceCheck[i]);
            }
            InputAdd("IMidPos", ref IMidPos);
           

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

                        // if (CheckInputState(IPlaceCheck, !IsEmptyRow(0)))
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
                        for (int i = 0; i < IOffloadCheck.Length; i++)
                        {
                            if (InputState(IOffloadCheck[i], false))
                            {
                                Battery[2, i].Release();
                                SaveRunData(SaveType.Battery);
                            }

                        }
                        if (Battery[0, 0].Type > BatType.Invalid /*&& Battery[0, 1].Type>BatType.Invalid*/)
                        {
                            this.nextAutoStep = AutoSteps.Auto_TransferBat;
                        }

                        CurMsgStr("等待开始信号", "Wait work start");
                        CheckSenser_Alarm();
                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OffloadFakePlaceBat, ref curState);
                        if (EventState.Invalid == curState || EventState.Finished == curState)
                        {
                            int emptyCol = -1;
                            if (IsEmptyRow(0,ref emptyCol) && InputState(IPlaceCheck[emptyCol], false))
                            {
                                // 发送取料请求
                                SetEvent(this, ModuleEvent.OffloadFakePlaceBat, EventState.Require);
                            }
                            if (IsHasBat(0))
                            {
                                // 转移电池
                                this.nextAutoStep = AutoSteps.Auto_TransferBat;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        else if (EventState.Response == curState)
                        {
                            if (CheckInputState(IPlaceCheck[0], false) || CheckInputState(IPlaceCheck[1],false))
                            {
                                // 发送准备信号
                                SetEvent(this, ModuleEvent.OffloadFakePlaceBat, EventState.Ready);
                                this.nextAutoStep = AutoSteps.Auto_WaitFinished;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitFinished:
                    {
                        CurMsgStr("等待放料完成", "Wait place finished");

                        EventState curState = EventState.Invalid;
                        GetEvent(this, ModuleEvent.OffloadFakePlaceBat, ref curState);
                        if (EventState.Invalid == curState || EventState.Finished == curState)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_TransferBat:
                    {
                        CurMsgStr("转移电池", "Transfer Battery");
                        int transferComp_Try = -1;
                        if (Def.IsNoHardware() || DryRun || TransferBattery(ref transferComp_Try))
                        {
                            // 数据转移
                            if(Def.IsNoHardware() || DryRun)
                                 transferComp_Try = Battery[0, 0].Type > BatType.Invalid ? 0 : 1;
                            try
                            {
                                Battery[2, transferComp_Try].CopyFrom(Battery[0, transferComp_Try]);
                                Battery[0, transferComp_Try].Release();
                            }
                            catch
                            {
                                ShowMsgBox.ShowDialog("转移记忆失败！",MessageType.MsgWarning);
                            }

                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
                        }
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.Battery | SaveType.AutoStep);
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
            // 待添加电池、托盘、信号 数据初始化

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
        /// 检查模组事件状态
        /// </summary>
        /// <returns></returns>
        public override bool CheckModuleEventState()
        {
            EventState curEventState = EventState.Invalid;
            int nEventRowIdx = -1;
            int nEventColIdx = -1;

            if (GetEvent(this, ModuleEvent.OffloadFakePlaceBat, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
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
            // 下料机器人与下料假电池输出交互情况
            RunProOffloadRobot runOffloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            if (runOffloadRobot.CheckRobotPos((int)OnloadRobotStation.OnloadLine, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("下料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            if (!CheckModuleEventState())
            {
                string strInfo = string.Format("《下料假电池》与《下料机器人》处于交互中\r\n点击【确定】将清除《下料机器人》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOffloadRobot.ClearModuleData())
                    {
                        strInfo = string.Format("下料假电池模组数据清除【失败】！！！《下料假电池》与《下料机器人》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }
            base.CopyRunDataClearBak();
            base.InitRunData();
            var transferBat = Battery[0, 0].Type > BatType.Invalid ? 0 : 1;
            OutputAction(OTransferMotor[transferBat], false);
            SaveRunData(SaveType.Battery | SaveType.AutoStep);
            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleTask()
        {
            if (!ClearModuleData())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 参数读取（初始化时调用）
        /// </summary>
        public override bool ReadParameter()
        {
            base.ReadParameter();
            LeftTryEnable = ReadParam(RunModule, "RTryEnable", false);
            RightTryEnable = ReadParam(RunModule, "LTryEnable", false);
            return true;
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
            }

            base.SaveRunData(saveType, index);
        }

        #endregion


        /// <summary>
        /// 放电池检查
        /// </summary>
        private bool IsEmptyRow(int nRow,ref int emptyCol)
        {
            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
            {
                if (Battery[nRow, nColIdx].Type == BatType.Invalid)
                {
                    emptyCol = nColIdx;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 有电池检查
        /// </summary>
        private bool IsHasBat(int nRow)
        {
            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
            {
                if (Battery[nRow, nColIdx].Type > BatType.Invalid)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 转移电池
        /// </summary>
        private bool TransferBattery(ref int nCol)
        {
            TimeSpan TSpan;
            DateTime StartTime = DateTime.Now;
            bool bOffloadPrompt = false;
            bool bTransfer = false;

            if (!CheckSenser_Alarm()) return false;

            

            var transferBat = Battery[0, 0].Type > BatType.Invalid ? 0 : 1;

            // 开始转移
            OutputAction(OTransferMotor[transferBat], true);
            while (true)
            {
                // 下料位检测到电池
                if (InputState(IOffloadCheck[transferBat], true) /*&& InputState(IMidPos, false)*/)
                {
                    bOffloadPrompt = true;
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

            Sleep(100);
            OutputAction(OTransferMotor[transferBat], false);

            if (bOffloadPrompt && InputState(IOffloadCheck[transferBat], true))
            {
                nCol = transferBat;
                ShowMsgBox.ShowDialog($"拉带{transferBat + 1}待测假电池已满，请人工取走待测电池", MessageType.MsgWarning, 5);
            }

            if (!bTransfer && InputState(IOffloadCheck[transferBat], false))
            {
                ShowMsgBox.ShowDialog($"拉带{transferBat + 1}转移假电池过程超时，请检查后重试", MessageType.MsgAlarm, 5);
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

            if (nBatIdx < 0 || nBatIdx > 1)
            {
                return false;
            }

            if (bAlarm)
            {
                return CheckInputState(IPlaceCheck[nBatIdx], bHasBat);
            }
            else
            {
                return InputState(IPlaceCheck[nBatIdx], bHasBat);
            }
        }
        public bool GetTryEnable(int nTry)
        {
            return nTry == 0 ? LeftTryEnable : RightTryEnable;
        }

        public bool CheckSenser_Alarm()
        {
            if (Def.IsNoHardware()) return true;
            //var check_Offload = InputState(IOffloadCheck[0], true) && InputState(IOffloadCheck[1], true);
            var left_Check = InputState(IOffloadCheck[0], true);
            //var right_Check = InputState(IOffloadCheck[0], true);

            // 检查下料端
            //if (check_Offload)
            //{
            //    ShowMsgBox.ShowDialog("双通道待测假电池已满，请人工取走待测电池", MessageType.MsgWarning, 8);
            //    return false;
            //}
            //else
            if (left_Check)

                ShowMsgBox.ShowDialog("通道待测假电池已满，请人工取走待测电池", MessageType.MsgWarning, 8);

            //else if (right_Check)

            //    ShowMsgBox.ShowDialog("2通道待测假电池已满，请人工取走待测电池", MessageType.MsgWarning, 8);

            return true;

        }
    }
}
