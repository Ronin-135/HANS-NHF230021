using CommunityToolkit.Mvvm.ComponentModel;
using HelperLibrary;
using ImTools;
using Machine;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using static Machine.RunProCoolingStove;

namespace Machine
{
    internal class RunProCoolingStove : RunProcess
    {
        #region 枚举
        public enum ModuleDef
        {
            Pallet_All = 4,
            Pallet_MaxCol = 1,
            Pallet_MaxRow = 4,
        }

        private new enum InitSteps
        {
            Init_DataRecover,
            Init_CheckPlt,
            Init_CheckDoorClose,
            Init_CloseOvenDoor,
            Init_DoorCloseFinished,
            Init_CheckDoorOpen,
            Init_OpenOvenDoor,
            Init_DoorOpenFinished,
            Init_End
        }
        private new enum AutoSteps
        {
            Auto_WaitWorkStart,
            Auto_PreCloseOvenDoor,
            Auto_OpenOvenDoor,
            Auto_WaitActionFinished,
            Auto_CheckPltState,
            Auto_CloseOvenDoor,

            Auto_WorkEnd,
            Auto_Ation,
            Auto_End,
        }
        #endregion

        #region 硬件
        public int[] IPltLeftCheck = new int[(int)ModuleDef.Pallet_All];       // 腔体夹具检测
        public int[] IPltRightCheck = new int[(int)ModuleDef.Pallet_All];
        public int[] IPltHasCheck = new int[(int)ModuleDef.Pallet_All];        // 夹具检测
        private int[] IOvenDoorExtendFinish;                                   // 炉门伸出关门到位
        private int[] IOvenDoorRetractionFinish;                               // 炉门缩回开门到位

        private int[] OOpenOvenDoor;                                           // 输出：炉门开
        private int[] OCloseOvenDoor;                                          // 输出：炉门关

        private int nCreatePat;                         // 创建托盘
        private bool bCreatePatType;                    // 创建托盘类型
        private int nReleasePat;                        // 清除托盘

        #endregion

        #region 参数设置项
        private bool[] bBufEnable = new bool[(int)ModuleDef.Pallet_MaxRow] { false, false, false, false };
        private ModuleEvent curRespEvent;               // 当前响应信号
        private EventState curEventState;               // 当前信号状态（临时使用）
        public int CoolingTim;                          // 冷却时间（分钟）
        private int nCurOperatRow;                      // 当前操作行
        private int nCurOperatCol;                      // 当前操作列（临时使用）
        private int nCurCheckRow;                       // 当前检查行（初始化使用）
        public OvenDoorState[] DoorState;
        private int[] coolingTime = new int[(int)ModuleDef.Pallet_All] { 0, 0, 0, 0 };                // 冷却计时器
        
        #endregion

        #region 内部字段
        private bool[] nEnableState = new bool[(int)ModuleDef.Pallet_MaxRow] { false, false, false, false };
        public int[] CoolingTime
        {
            get { return coolingTime; }
            set { SetProperty(ref coolingTime, value); }
        }

        private AutoSteps NextAutoStep
        {
            get
            {
                return (AutoSteps)nextAutoStep;
            }
            set
            {
                if ((int)nextAutoStep != (int)value)
                {
                    SaveRunData(SaveType.AutoStep);
                }
                nextAutoStep = value;
            }
        }

        public bool[] BBufEnable
        {
            get => bBufEnable;
            set { SetProperty(ref bBufEnable, value); }
        }

        public DateTime[] StartTime = new DateTime[(int)ModuleDef.Pallet_All];
        #endregion

        /// <summary>
        /// 检查模组事件状态
        /// </summary>
        /// <returns></returns>
        public override bool CheckModuleEventState()
        {
            EventState curEventState = EventState.Invalid;
            int nEventRowIdx = -1;
            int nEventColIdx = -1;

            for (ModuleEvent eventIdx = ModuleEvent.OvenPickWaitCooling; eventIdx < ModuleEvent.OvenEventEnd; eventIdx++)
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
            if (runTransfer.CheckRobotPos((int)TransferRobotStation.CooligStove))
            {
                ShowMsgBox.ShowDialog("调度在取料，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            // 信号初始化
            if (null != ArrEvent)
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx);
                }
            }
            SaveRunData(SaveType.AutoStep | SaveType.Variables);
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


        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {

            // IO/电机
            IOvenDoorExtendFinish = new int[(int)ModuleDef.Pallet_MaxRow];                         // 炉门左右伸出到位
            IOvenDoorRetractionFinish = new int[(int)ModuleDef.Pallet_MaxRow];                     // 炉门左右缩回到位
            OOpenOvenDoor = new int[(int)ModuleDef.Pallet_MaxRow];                                 // 炉门开门
            OCloseOvenDoor = new int[(int)ModuleDef.Pallet_MaxRow];                                // 炉门关门
            // 模组参数
            DoorState = new OvenDoorState[4] { OvenDoorState.Invalid, OvenDoorState.Invalid, OvenDoorState.Invalid, OvenDoorState.Invalid };
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
            for (int nIdx = 0; nIdx < (int)ModuleDef.Pallet_All; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IPltLeftCheck" + strIndex, ref IPltLeftCheck[nIdx]);
                InputAdd("IPltRightCheck" + strIndex, ref IPltRightCheck[nIdx]);
                InputAdd("IPltHasCheck" + strIndex, ref IPltHasCheck[nIdx]);
            }


            for (int nIdx = 0; nIdx < 4; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                OutputAdd("OOpenOvenDoor" + strIndex, ref OOpenOvenDoor[nIdx]);
                OutputAdd("OCloseOvenDoor" + strIndex, ref OCloseOvenDoor[nIdx]);
                InputAdd("IOvenDoorExtendFinish" + strIndex, ref IOvenDoorExtendFinish[nIdx]);
                InputAdd("IOvenDoorRetractionFinish" + strIndex, ref IOvenDoorRetractionFinish[nIdx]);
            }

            return true;
        }

        public override bool ReadParameter()
        {
            base.ReadParameter();
            for (int i = 0; i < bBufEnable.Length; i++)
            {
                bBufEnable[i] = ReadParam(RunModule, "BufEnable" + i, false);
                if (bBufEnable[i] && Pallet[i].Type != PltType.Invalid && !nEnableState[i])
                {
                    StartTime[i] = ReadParam(RunModule, "StartTime" + i, DateTime.Now);
                    Pallet[i].StartTime = StartTime[i].ToString("yyyy/MM/dd HH:mm:ss");
                    SaveRunData(SaveType.Variables | SaveType.Pallet);
                }
                nEnableState[i] = bBufEnable[i];
            }
            CoolingTim = ReadParam(RunModule, "CoolingTim", -1);
            BBufEnable = BBufEnable.ToArray();
            //RaisePropertyChanged(nameof(BBufEnable));

            return true;
        }

        #region //构造函数
        public RunProCoolingStove(int RunID) : base(RunID)
        {
            InitCreateObject((int)ModuleDef.Pallet_All, 0, 0, (int)ModuleEvent.CoolingPutAction + 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            InsertPrivateParam("BufEnable0", "1层冷却使能", "TRUE启用，FALSE禁用", bBufEnable[0]);
            InsertPrivateParam("BufEnable1", "2层冷却使能", "TRUE启用，FALSE禁用", bBufEnable[1]);
            InsertPrivateParam("BufEnable2", "3层冷却使能", "TRUE启用，FALSE禁用", bBufEnable[2]);
            InsertPrivateParam("BufEnable3", "4层冷却使能", "TRUE启用，FALSE禁用", bBufEnable[3]);
            InsertPrivateParam("CoolingTim", "冷却时间(分钟)", "单位分钟", CoolingTim);
        }
        #endregion



        #region 模组运行
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
                        for (int i = 0; i < (int)ModuleDef.Pallet_All; i++)
                        {
                            if (Pallet[i].Type != PltType.Invalid && StartTime[i] == default)
                            {
                                if(DateTime.TryParse(Pallet[i].StartTime, out DateTime dt))
                                {
                                    StartTime[i]= dt;
                                }
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckPlt;
                        break;
                    }
                case InitSteps.Init_CheckPlt:
                    {
                        CurMsgStr("检查托盘状态", "Check Pallet status");
                        GetCoolingCount();//获取倒计时
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            if (!CheckInputState(IPltLeftCheck[nPltIdx], Pallet[nPltIdx].Type > PltType.Invalid) ||
                                !CheckInputState(IPltRightCheck[nPltIdx], Pallet[nPltIdx].Type > PltType.Invalid))
                            {

                            }
                        }

                        nCurCheckRow = (int)ModuleDef.Pallet_MaxRow - 1;
                        this.nextInitStep = InitSteps.Init_CheckDoorClose;
                        break;
                    }
                case InitSteps.Init_CheckDoorClose:
                    {
                        CurMsgStr("检查炉门关闭", "Init check door close");


                        if (nCurCheckRow >= 0 &&
                        GetOvenDoorState(nCurCheckRow) != OvenDoorState.Close &&
                        DoorState[nCurCheckRow] == OvenDoorState.Close)
                        {
                            this.nextInitStep = InitSteps.Init_CloseOvenDoor;
                        }
                        else
                        {
                            this.nextInitStep = InitSteps.Init_DoorCloseFinished;
                        }
                        break;
                    }

                case InitSteps.Init_CloseOvenDoor:
                    {
                        CurMsgStr("关闭炉门", "Init close oven door");

                        //if (CheckInputState(IOvenDoorScreenState[nCurCheckRow], true)) //没有光栅硬件防呆
                        //{
                        //    break;
                        //}

                        DoorState[nCurCheckRow] = OvenDoorState.Close;
                        if (OutputAction(OCloseOvenDoor[nCurCheckRow], true))
                        {
                            if (GetOvenDoorState(nCurCheckRow) == OvenDoorState.Close)
                            {
                                this.nextInitStep = InitSteps.Init_DoorCloseFinished;
                            }
                        }

                        break;
                    }
                case InitSteps.Init_DoorCloseFinished:
                    {
                        CurMsgStr("炉门关闭完成", "Init door close finished");

                        if (nCurCheckRow > 0)
                        {
                            nCurCheckRow--;
                            this.nextInitStep = InitSteps.Init_CheckDoorClose;
                        }
                        else
                        {
                            nCurCheckRow = 0;
                            this.nextInitStep = InitSteps.Init_CheckDoorOpen;
                        }
                        break;
                    }
                case InitSteps.Init_CheckDoorOpen:
                    {
                        CurMsgStr("检查炉门打开", "Init check door open");

                        if (nCurCheckRow < (int)ModuleDef.Pallet_MaxRow &&
                        GetOvenDoorState(nCurCheckRow) != OvenDoorState.Open &&
                        DoorState[nCurCheckRow] == OvenDoorState.Open)
                        {
                            this.nextInitStep = InitSteps.Init_OpenOvenDoor;
                        }
                        else
                        {
                            this.nextInitStep = InitSteps.Init_DoorOpenFinished;
                        }

                        break;
                    }
                case InitSteps.Init_OpenOvenDoor:
                    {
                        CurMsgStr("打开炉门", "Init open oven door");


                        //if (CheckInputState(IOvenDoorScreenState[nCurCheckRow], true)) //无光栅硬件防呆
                        //{
                        //    break;
                        //}
                        DoorState[nCurCheckRow] = OvenDoorState.Open;
                        if (OutputAction(OOpenOvenDoor[nCurCheckRow], true))
                        {
                            if (GetOvenDoorState(nCurCheckRow) == OvenDoorState.Open)
                            {
                                this.nextInitStep = InitSteps.Init_DoorOpenFinished;
                            }
                        }
                        break;
                    }
                case InitSteps.Init_DoorOpenFinished:
                    {
                        CurMsgStr("炉门打开完成", "Init door open finished");

                        if (nCurCheckRow < (int)ModuleDef.Pallet_MaxRow - 1)
                        {
                            nCurCheckRow++;
                            this.nextInitStep = InitSteps.Init_CheckDoorOpen;
                        }
                        else
                        {
                            nCurCheckRow = -1;
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

            switch (NextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Work end");


                        // 测试使用 //暂停使用
                        // 创建托盘
                        if ((nCreatePat >= 0 && nCreatePat < (int)ModuleDef.Pallet_All) && false)
                        {
                            
                            if (Pallet[nCreatePat].IsType(PltType.Invalid) || Pallet[nCreatePat].IsType(PltType.NG))
                            {
                                Pallet[nCreatePat].Release();
                                if (bCreatePatType)
                                { Pallet[nCreatePat].Type = PltType.WaitCooling; }
                                else
                                { Pallet[nCreatePat].Type = PltType.WaitOffload; }

                                Pallet[nCreatePat].Stage = PltStage.Offload;
                                SaveRunData(SaveType.Pallet, nCreatePat);
                            }
                            nCreatePat = -1;
                            SaveParameter();
                        }
                        // 清除托盘
                        if ((nReleasePat >= 0 && nReleasePat < (int)ModuleDef.Pallet_All) && false)
                        {
                            Pallet[nReleasePat].Release();
                            SaveRunData(SaveType.Pallet, nReleasePat);

                            nReleasePat = -1;
                            SaveParameter();
                        }
                        GetCoolingCount();//获取倒计时
                        for (int i = 0; i < (int)ModuleDef.Pallet_All; i++)
                        {
                            var plt = Pallet[i];
                            // 没有时间赋值时间
                            if (plt.Type != PltType.Invalid && StartTime[i] == default)
                            {
                                StartTime[i] = DateTime.Now;
                                if (bBufEnable[i]) Pallet[i].StartTime = StartTime[i].ToString("yyyy/MM/dd HH:mm:ss");
                                SaveRunData(SaveType.Variables | SaveType.Pallet);
                            }
                            if (plt.Type == PltType.Invalid && StartTime[i] != default)
                            {
                                StartTime[i] = default;
                                SaveRunData(SaveType.Variables);
                            }
                            // 当前使能打开 && 时间有值 && 时间满足设定分钟 && 托盘状态为待冷却
                            if (bBufEnable[i] &&
                                StartTime[i] != default &&
                                (DateTime.Now - StartTime[i]).TotalMinutes >= CoolingTim &&
                                Pallet[i].Type != PltType.WaitOffload)
                            {
                                Pallet[i].Type = PltType.WaitOffload;
                                Pallet[i].EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                SaveRunData(SaveType.Pallet);
                            }

                        }




                        #region 信号交互


                        // ================================== 发送放托盘信号 ==================================
                        for (ModuleEvent nEvent = ModuleEvent.OvenPickWaitCooling; nEvent < ModuleEvent.OvenEventEnd; nEvent++)
                        {
                            // 取消状态改为无效状态
                            if (GetEvent(this, nEvent, ref curEventState) && (EventState.Cancel == curEventState))
                            {
                                SetEvent(this, nEvent, EventState.Invalid);
                            }
                        }
                        if (HasPlacePos(Pallet))
                        {
                            //放：烘烤完成托盘
                            if ((CheckEvent(this, ModuleEvent.OvenPickWaitCooling, EventState.Invalid) ||
                            CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Finished)))
                            {
                                SetEvent(this, ModuleEvent.CoolingPutAction, EventState.Require);
                            }
                        }



                        // ================================== 发送取托盘信号 ==================================
                        // 取：待下料托盘（冷却完成的托盘）
                        if (HasOffloadPlt(Pallet))
                        {
                            if (GetEvent(this, ModuleEvent.CoolingPutAction, ref curEventState) &&
                                (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                            {
                                SetEvent(this, ModuleEvent.CoolingPutAction, EventState.Require);
                            }
                        }

                        int a = 0, c = 0;
                        CheckEvent(this, ModuleEvent.OvenPickWaitCooling, EventState.Invalid, ref a, ref c);


                        // 信号响应
                        for (ModuleEvent eventIdx = ModuleEvent.CoolingPutAction; eventIdx < ModuleEvent.OvenEventEnd; eventIdx++)
                        {
                            if (GetEvent(this, eventIdx, ref curEventState, ref nCurOperatRow, ref nCurOperatCol))
                            {
                                if (EventState.Response == curEventState)
                                {
                                    curRespEvent = eventIdx;
                                    NextAutoStep = AutoSteps.Auto_PreCloseOvenDoor;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }
                            }
                        }


                        // 信号响应
                        if (CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Invalid) ||
                            CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Finished))
                        {
                            SetEvent(this, ModuleEvent.CoolingPutAction, EventState.Require);
                        }

                        if (CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Response))
                        {
                            //NextAutoStep = AutoSteps.Auto_Ation;
                            //NextAutoStep = AutoSteps.Auto_PreCloseOvenDoor;
                        }
                        #endregion
                        break;
                    }

                case AutoSteps.Auto_PreCloseOvenDoor:
                    {
                        this.msgChs = string.Format("冷却炉[{0}]层预先关闭炉门", nCurOperatRow + 1);
                        this.msgEng = string.Format("Oven [{0}] row pre close oven door", nCurOperatRow + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        //if ((InputState(IOvenDoorExtendFinish[nCurOperatRow], false)/* || InputState(IOvenDoorUpFinish[nCurOperatRow], false) 光栅 */))
                        //{
                        //    //if (CheckInputState(IOvenDoorScreenState[nCurOperatRow], false) && !DryRun)//光栅
                        //    //{
                        //    //    break;
                        //    //}
                        //}
                        OutputAction(OOpenOvenDoor[nCurOperatRow], false);
                        if (OutputAction(OCloseOvenDoor[nCurOperatRow], true) || DryRun)
                        {
                            if (InputState(IOvenDoorExtendFinish[nCurOperatRow], true))
                            {
                                OutputAction(OCloseOvenDoor[nCurOperatRow], false);
                                this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        else
                        {
                            // 暂时不用，待确认后使用
                            //if (CheckEvent(this, curRespEvent, EventState.Cancel))
                            //{

                            //    SetEvent(this, curRespEvent, EventState.Invalid);
                            //    this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;
                            //    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                            //    break;
                            //}
                        }
                        break;
                    }
                case AutoSteps.Auto_OpenOvenDoor:
                    {
                        this.msgChs = string.Format("冷却炉[{0}]层打开炉门", nCurOperatRow + 1);
                        this.msgEng = string.Format("Oven [{0}] row open oven door", nCurOperatRow + 1);
                        CurMsgStr(this.msgChs, this.msgEng);


                        if (OutputAction(OOpenOvenDoor[nCurOperatRow], true) && OutputAction(OCloseOvenDoor[nCurOperatRow], false) || DryRun)
                        {
                            if (InputState(IOvenDoorRetractionFinish[nCurOperatRow], true))
                            {
                                OutputAction(OOpenOvenDoor[nCurOperatRow], false);
                                this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            else if (DryRun)
                            {
                                this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            else
                            {

                            }
                        }
                        else
                        {
                            // 暂时不用，待确认后使用
                            //if (CheckEvent(this, curRespEvent, EventState.Cancel))
                            //{
                            //    SetEvent(this, curRespEvent, EventState.Invalid);
                            //    this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                            //    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                            //    break;
                            //}
                        }
                        Sleep(5000);
                        break;
                    }

                case AutoSteps.Auto_WaitActionFinished:
                    {
                        CurMsgStr("等待调度机器人动作完成", "Work end");

                        if (CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Response))
                        {
                            SetEvent(this, ModuleEvent.CoolingPutAction, EventState.Ready);
                        }
                        if (CheckEvent(this, ModuleEvent.CoolingPutAction, EventState.Finished))
                        {
                            SaveRunData(SaveType.AutoStep);
                            NextAutoStep = AutoSteps.Auto_CheckPltState;
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckPltState:
                    {
                        this.msgChs = string.Format("冷却炉[{0}]层检查托盘状态", nCurOperatRow + 1);
                        this.msgEng = string.Format("Oven [{0}] row check Pallet State", nCurOperatRow + 1);
                        CurMsgStr(this.msgChs, this.msgEng);


                        //if ((GetPlt(nCurOperatRow, nCurOperatCol).Type > PltType.Invalid) 
                        //    && InputState(IPltLeftCheck[GetRowColToIndex(nCurOperatRow, nCurOperatCol)], true)
                        //    && InputState(IPltRightCheck[GetRowColToIndex(nCurOperatRow, nCurOperatCol)], true)
                        //    && InputState(IPltHasCheck[GetRowColToIndex(nCurOperatRow, nCurOperatCol)], true))

                        if (bBufEnable[nCurOperatRow] || Def.IsNoHardware() || DryRun)
                        {
                            this.nextAutoStep = AutoSteps.Auto_CloseOvenDoor;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        else
                        {
                            //string strPlt = bHasPlt ? "有" : "无";
                            //string strData = !bHasPlt ? "有" : "无";
                            //string strDisp = "请停机检查腔体中夹具状态！";
                            //string strMsg = string.Format("{0}层腔体中检测到{1}夹具，实际应该{2}夹具", nCurOperatRow + 1, strPlt, strData);
                            //ShowMessageBox((int)RunID.CoolingStove, strMsg, strDisp, MessageType.MsgWarning);
                            break;
                        }

                    }
                case AutoSteps.Auto_CloseOvenDoor:
                    {
                        this.msgChs = string.Format("干燥炉[{0}]层关闭炉门", nCurOperatRow + 1);
                        this.msgEng = string.Format("Oven [{0}] row close Oven door", nCurOperatRow + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        //if (!DryRun && OutputState(OCloseOvenDoor[nCurOperatRow], false))
                        //{
                        //    break;
                        //}
                        OutputAction(OOpenOvenDoor[nCurOperatRow], false);
                        if (DryRun || OutputAction(OCloseOvenDoor[nCurOperatRow], true))
                        {

                            if (InputState(IOvenDoorExtendFinish[nCurOperatRow], true))
                            {
                                OutputAction(OCloseOvenDoor[nCurOperatRow], false);
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            else if (DryRun)
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        else
                        {
                            //if (CheckEvent(this, curRespEvent, EventState.Invalid))
                            //{
                            //    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            //    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            //    break;
                            //}
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

        public override bool CheckPallet(int nPltIdx, bool bHasPlt, bool bAlarm = true)
        {
            //var plt = Pallet.GetValue(GetIndexPallet(nPltIdx)) as Pallet;
            var res = InputState(IPltLeftCheck[nPltIdx], bHasPlt) &&
                InputState(IPltRightCheck[nPltIdx], bHasPlt) &&
                InputState(IPltHasCheck[nPltIdx], bHasPlt);
            if (bAlarm)
            {
                CheckInputState(IPltHasCheck[nPltIdx], bHasPlt);
                CheckInputState(IPltLeftCheck[nPltIdx], bHasPlt);
                CheckInputState(IPltRightCheck[nPltIdx], bHasPlt);
            }
            //if (!res)
            //{
            //    string strPlt = bHasPlt ? "有" : "无";
            //    string strData = !bHasPlt ? "有" : "无";
            //    string strDisp = "请停机检查腔体中夹具状态！";
            //    string strMsg = string.Format("{0}层腔体中检测到{1}夹具，实际应该{2}夹具", nCurOperatRow + 1, strPlt, strData);
            //    ShowMessageBox((int)RunID.CoolingStove, strMsg, strDisp, MessageType.MsgWarning);
            //}
            return res;

        }

        #endregion



        #region 公共方法
        public Pallet GetPallet(int row, int col) => Pallet[GetRowColToIndex(row, col)];

        public int[] GetIndexPallet(int Length) => new int[2] { Length / (int)ModuleDef.Pallet_MaxCol, Length % (int)ModuleDef.Pallet_MaxCol };
        public int GetRowColToIndex(int row, int col) => row * (int)ModuleDef.Pallet_MaxCol + col;


        /// <summary>
        /// 有待下料托盘
        /// </summary>
        public bool HasOffloadPlt(Pallet[] plt)
        {
            int nCavityIdx = 0;

            for (int nPltIdx = 0; nPltIdx < plt.Length; nPltIdx++)
            {
                nCavityIdx = (nPltIdx) / (int)ModuleDef.Pallet_MaxCol;
                if (nPltIdx < 2) { nCavityIdx = 0; }
                else if (nPltIdx > 1 && nPltIdx < 4)
                {
                    nCavityIdx = 1;
                }
                else if (nPltIdx > 3)
                {
                    nCavityIdx = 2;
                }

                if ((bool)bBufEnable.GetValue(nCavityIdx))
                {
                    if (plt[nPltIdx].IsType(PltType.WaitOffload) && !PltIsEmpty(plt[nPltIdx]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 有放托盘位
        /// </summary>
        public bool HasPlacePos(Pallet[] plt)
        {
            int nCavityIdx = 0;

            for (int nPltIdx = 0; nPltIdx < plt.Length; nPltIdx++)
            {
                nCavityIdx = nPltIdx / (int)ModuleDef.Pallet_MaxCol;
                if (nPltIdx < 2) { nCavityIdx = 0; }
                else if (nPltIdx > 1 && nPltIdx < 4)
                {
                    nCavityIdx = 1;
                }
                else if (nPltIdx > 3)
                {
                    nCavityIdx = 2;
                }

                if ((bool)bBufEnable.GetValue(nCavityIdx))
                {
                    if (plt[nPltIdx].IsType(PltType.Invalid))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取冷却倒计时
        /// </summary>
        /// <param name="nPltIdx">托盘号</param>
        /// <returns></returns>
        private void GetCoolingCount()
        {
            int[] Coolings = new int[(int)ModuleDef.Pallet_MaxRow] { 0, 0, 0, 0 };
            for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_MaxRow; nPltIdx++)
            {
                int nCavityIdx = (int)ModuleDef.Pallet_MaxRow - 1 - nPltIdx;
                if ((CoolingTim - (int)(DateTime.Now - StartTime[nPltIdx]).TotalMinutes < 0) || !BBufEnable[nPltIdx])
                {
                    Coolings[nCavityIdx] = CoolingTim;
                    continue;
                }
                Coolings[nCavityIdx] = CoolingTim - (int)(DateTime.Now - StartTime[nPltIdx]).TotalMinutes;
            }
            this.CoolingTime = Coolings;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取托盘数据
        /// </summary>
        public Pallet GetPlt(int nRowIdx, int nColIdx)
        {
            if (nRowIdx < 0 || nRowIdx >= (int)ModuleDef.Pallet_MaxRow ||
                nColIdx < 0 || nColIdx >= (int)ModuleDef.Pallet_MaxCol)
            {
                return null;
            }
            return Pallet[nRowIdx * (int)ModuleDef.Pallet_MaxCol + nColIdx];
        }


        /// <summary>
        /// 炉门动作
        /// </summary>
        public enum OvenDoorState
        {
            Invalid = 0,                // 未知
            Close,                      // 关闭
            Open,                       // 打开
            Action,                     // 动作中
        }


        /// <summary>
        /// 获取炉门状态
        /// </summary>
        public OvenDoorState GetOvenDoorState(int nOvenRow)
        {
            if (InputState(IOvenDoorExtendFinish[nOvenRow], true) &&
                InputState(IOvenDoorRetractionFinish[nOvenRow], false))
            {
                return OvenDoorState.Close;
            }

            else if (InputState(IOvenDoorExtendFinish[nOvenRow], false) &&
                 InputState(IOvenDoorRetractionFinish[nOvenRow], true))
            {
                return OvenDoorState.Open;
            }
            else
            { return OvenDoorState.Action; }
        }
        #endregion
    }

}


