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
    class RunProPalletBuf : RunProcess
    {

        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckPlt,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_WaitActionFinished,
            Auto_WorkEnd,
        }

        private enum ModuleDef
        {
            // 无效
            DefInvalid = -1,

            // 托盘
            Pallet_0 = 0,
            Pallet_1,
            Pallet_2,
            Pallet_3,
            Pallet_All,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.PalletBufferMsgStartID,
        }
        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】
        private int[] IPltLeftCheck;                    // 托盘左到位感应
        private int[] IPltRightCheck;                   // 托盘右到位感应
        private int[] IPltHasCheck;                     // 托盘有料感应

        // 【模组参数】
        private bool[] bBufEnable;                      // 托盘缓存使能
        private int nCreatePat;                         // 创建托盘
        private int nReleasePat;                        // 清除托盘

        // 【模组数据】
        private ModuleEvent curRespEvent;               // 当前响应信号
        private EventState curEventState;               // 当前信号状态（临时使用）
        private int nCurOperatRow;                      // 当前操作行
        private int nCurOperatCol;                      // 当前操作列（临时使用）

        public bool[] BBufEnable { get => 
                bBufEnable; 
            set { SetProperty(ref bBufEnable, value); } }


        #endregion


        #region // 构造函数

        public RunProPalletBuf(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject((int)ModuleMaxPallet.PalletBuf, 0, 0, (int)ModuleEvent.PltBufEventEnd);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("BufEnable1", "1层缓存使能", "TRUE启用，FALSE禁用", bBufEnable[0], ParameterLevel.PL_STOP_OPER);
            InsertPrivateParam("BufEnable2", "2层缓存使能", "TRUE启用，FALSE禁用", bBufEnable[1], ParameterLevel.PL_STOP_OPER);
            InsertPrivateParam("BufEnable3", "3层缓存使能", "TRUE启用，FALSE禁用", bBufEnable[2], ParameterLevel.PL_STOP_OPER);
            InsertPrivateParam("BufEnable4", "4层缓存使能", "TRUE启用，FALSE禁用", bBufEnable[3], ParameterLevel.PL_STOP_OPER);
            InsertPrivateParam("CreatePat", "创建托盘", "创建托盘：0~3号托盘", nCreatePat);
            InsertPrivateParam("ReleasePat", "清除托盘", "清除托盘：0~3号托盘", nReleasePat);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IPltLeftCheck = new int[4] { -1, -1, -1, -1 };
            IPltRightCheck = new int[4] { -1, -1, -1, -1 };
            IPltHasCheck = new int[4] { -1, -1, -1, -1 };
            // 模组参数
            bBufEnable = new bool[4] { false, false, false, false };
            nCreatePat = -1;
            nReleasePat = -1;
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
            for (int nIdx = 0; nIdx < 4; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IPltLeftCheck" + strIndex, ref IPltLeftCheck[nIdx]);
                InputAdd("IPltRightCheck" + strIndex, ref IPltRightCheck[nIdx]);
                InputAdd("IPltHasCheck" + strIndex, ref IPltHasCheck[nIdx]);
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
                        this.nextInitStep = InitSteps.Init_CheckPlt;
                        break;
                    }
                case InitSteps.Init_CheckPlt:
                    {
                        CurMsgStr("检查托盘状态", "Check Pallet status");

                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            if (!CheckInputState(IPltLeftCheck[nPltIdx], Pallet[nPltIdx].Type > PltType.Invalid) || 
                                !CheckInputState(IPltRightCheck[nPltIdx], Pallet[nPltIdx].Type > PltType.Invalid))
                            {
                                bBufEnable[nPltIdx] = false;
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

                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            // 有空位
                            if (IsPltBufEN(nPltIdx) && Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                // 放空盘
                                if (GetEvent(this, ModuleEvent.PltBufPlaceEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.PltBufPlaceEmptyPlt, EventState.Require);
                                }

                                // 放NG空盘
                                if (GetEvent(this, ModuleEvent.PltBufPlaceNGEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.PltBufPlaceNGEmptyPlt, EventState.Require);
                                }
                            }

                            // 有空托盘
                            if (IsPltBufEN(nPltIdx) && Pallet[nPltIdx].IsType(PltType.OK) && PltIsEmpty(Pallet[nPltIdx]))
                            {
                                if (GetEvent(this, ModuleEvent.PltBufPickEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.PltBufPickEmptyPlt, EventState.Require);
                                }
                            }

                            // 有NG空托盘
                            if (IsPltBufEN(nPltIdx) && Pallet[nPltIdx].IsType(PltType.NG) && PltIsEmpty(Pallet[nPltIdx]))
                            {
                                if (GetEvent(this, ModuleEvent.PltBufPickNGEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.PltBufPickNGEmptyPlt, EventState.Require);
                                }
                            }
                        }

                        // 有响应
                        for (ModuleEvent eventIdx = ModuleEvent.PltBufPlaceEmptyPlt; eventIdx < ModuleEvent.PltBufEventEnd; eventIdx++)
                        {
                            if (GetEvent(this, eventIdx, ref curEventState, ref nCurOperatRow, ref nCurOperatCol))
                            {
                                if (EventState.Response == curEventState && nCurOperatRow > -1 && nCurOperatRow < (int)ModuleDef.Pallet_All)
                                {
                                    curRespEvent = eventIdx;
                                    this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                                    SaveRunData(SaveType.Variables | SaveType.AutoStep);
                                    break;
                                }
                            }
                        }

                        /////////////////////////////////////////////////////////////////////////////////////////
                        // 测试使用
                        if (nCreatePat >= 0 && nCreatePat < (int)ModuleDef.Pallet_All)
                        {
                            if (Pallet[nCreatePat].IsType(PltType.Invalid) || Pallet[nCreatePat].IsType(PltType.NG))
                            {
                                Pallet[nCreatePat].Release();
                                Pallet[nCreatePat].Type = PltType.OK;
                                Pallet[nCreatePat].Stage = PltStage.Invalid;
                                SaveRunData(SaveType.Pallet, nCreatePat);
                            }
                            nCreatePat = -1;
                            SaveParameter();
                        }
                        if (nReleasePat >= 0 && nReleasePat < (int)ModuleDef.Pallet_All)
                        {
                            Pallet[nReleasePat].Release();
                            SaveRunData(SaveType.Pallet, nReleasePat);

                            nReleasePat = -1;
                            SaveParameter();
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinished:
                    {
                        CurMsgStr("等待动作完成", "Wait action finished");

                        // 响应
                        if (CheckEvent(this, curRespEvent, EventState.Response))
                        {
                            bool bHasPlt = false;
                            if (ModuleEvent.PltBufPlaceEmptyPlt == curRespEvent || ModuleEvent.PltBufPlaceNGEmptyPlt == curRespEvent)
                            {
                                bHasPlt = false;
                            }
                            else if (ModuleEvent.PltBufPickEmptyPlt == curRespEvent || ModuleEvent.PltBufPickNGEmptyPlt == curRespEvent)
                            {
                                bHasPlt = true;
                            }

                            // 托盘检查
                            if (CheckPallet(nCurOperatRow, bHasPlt))
                            {
                                SetEvent(this, curRespEvent, EventState.Ready);
                            }
                        }

                        // 完成
                        else if (CheckEvent(this, curRespEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
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
            nCurOperatRow = -1;
            curRespEvent = ModuleEvent.ModuleEventInvalid;

            base.InitRunData();
        }

        /// <summary>
        /// 加载运行数据
        /// </summary>
        public override void LoadRunData()
        {
            string section, key;
            section = this.RunModule;

            // 其他变量
            curRespEvent = (ModuleEvent)FileStream.ReadInt(section, "curRespEvent", (int)curRespEvent);

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
                FileStream.WriteInt(section, "curRespEvent", (int)curRespEvent);
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

            for (ModuleEvent eventIdx = ModuleEvent.PltBufPlaceEmptyPlt; eventIdx < ModuleEvent.PltBufEventEnd; eventIdx++)
            {
                if (GetEvent(this, eventIdx, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
                    (EventState.Response == curEventState || EventState.Ready == curEventState))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleData()
        {
            RunProTransferRobot runTransfer = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
            if (runTransfer.CheckRobotPos((int)TransferRobotStation.PalletBuffer))
            {
                ShowMsgBox.ShowDialog("调度在取料，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            curRespEvent = ModuleEvent.ModuleEventInvalid;
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            // 信号初始化
            if (null != ArrEvent)
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx, (RunID)this.GetRunID());
                }
            }
            SaveRunData(SaveType.AutoStep | SaveType.SignalEvent | SaveType.Variables);
            return true;
        }
        /// <summary>
        /// 清除模组任务
        /// </summary>
        /// <returns></returns>
        public override bool ClearModuleTask()
        {
            return ClearModuleData();
        }
        #endregion


        #region // 模组参数和相关模组读取

        /// <summary>
        /// 写入数据库参数
        /// </summary>
        public override void SaveParameter()
        {
            WriteParameterCode(RunModule, "CreatePat", nCreatePat.ToString());
            WriteParameterCode(RunModule, "ReleasePat", nReleasePat.ToString());

            base.SaveParameter();
        }

        /// <summary>
        /// 参数读取（初始化时调用）
        /// </summary>
        public override bool ReadParameter()
        {
            base.ReadParameter();

            bBufEnable[0] = ReadParam(RunModule, "BufEnable1", false);
            bBufEnable[1] = ReadParam(RunModule, "BufEnable2", false);
            bBufEnable[2] = ReadParam(RunModule, "BufEnable3", false);
            bBufEnable[3] = ReadParam(RunModule, "BufEnable4", false);
            nCreatePat = ReadParam(RunModule, "CreatePat", -1);
            nReleasePat = ReadParam(RunModule, "ReleasePat", -1);
            BBufEnable = BBufEnable.ToArray();
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
        /// 检查托盘（硬件检测）
        /// </summary>
        public override bool CheckPallet(int nPltIdx, bool bHasPlt, bool bAlarm = true)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            if (nPltIdx < 0 || nPltIdx >= (int)ModuleDef.Pallet_All)
            {
                return false;
            }

            if (!InputState(IPltHasCheck[nPltIdx], bHasPlt) || !InputState(IPltLeftCheck[nPltIdx], bHasPlt) || !InputState(IPltRightCheck[nPltIdx], bHasPlt))
            {
                if (bAlarm)
                {
                    CheckInputState(IPltHasCheck[nPltIdx], bHasPlt);
                    CheckInputState(IPltLeftCheck[nPltIdx], bHasPlt);
                    CheckInputState(IPltRightCheck[nPltIdx], bHasPlt);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 缓存使能
        /// </summary>
        public bool IsPltBufEN(int nIndex)
        {
            if (nIndex > -1 && nIndex < (int)ModuleDef.Pallet_All)
            {
                return bBufEnable[nIndex];
            }
            return false;
        }
    }
}
