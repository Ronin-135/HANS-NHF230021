////////using HelperLibrary;
using EnumsNET;
using HelperLibrary;
using ImTools;
using Microsoft.VisualBasic;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;
using SystemControlLibrary;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
 
    class RunProTransferRobot : RunProcess ,IRobot
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckPallet,
            Init_RobotConnect,
            Init_End,
        }

        protected new enum AutoCheckStep
        {
            Auto_CheckRobotCmd = 0,
            Auto_CheckFinish,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_SendPickEventBeforeAction,
            Auto_SendPlaceEventBeforeAction,

            // 取料位计算
            Auto_CalcPickPos,

            // 取：上料
            Auto_OnloadPickMove,
            Auto_OnloadPickSendEvent,
            Auto_OnloadPickIn,
            Auto_OnloadPickDataTransfer,
            Auto_OnloadPickOut,
            Auto_OnloadPickCheckFinger,

            // 取：干燥炉
            Auto_DryingOvenPickMove,
            Auto_DryingOvenPickIn,
            Auto_DryingOvenPickDataTransfer,
            Auto_DryingOvenPickOut,
            Auto_DryingOvenPickCheckFinger,


            // 放冷却炉
            Auto_CoolingStovePlaceMove,
            Auto_CoolingStovePlaceIn,
            Auto_CoolingStovePlaceOut,
            Auto_CoolingStovePlaceCheckFinger,

            // 取：缓存架
            Auto_PalletBufPickMove,
            Auto_PalletBufPickIn,
            Auto_PalletBufPickDataTransfer,
            Auto_PalletBufPickOut,
            Auto_PalletBufPickCheckFinger,

            // 取：人工平台
            Auto_ManualPickMove,
            Auto_ManualPickIn,
            Auto_ManualPickDataTransfer,
            Auto_ManualPickOut,
            Auto_ManualPickCheckFinger,

            // 取：下料
            Auto_OffloadPickMove,
            Auto_OffloadPickSendEvent,
            Auto_OffloadPickIn,
            Auto_OffloadPickDataTransfer,
            Auto_OffloadPickOut,
            Auto_OffloadPickCheckFinger,
            Auto_OffloadReCalcPlace,

            // 放料位计算
            Auto_CalcPlacePos,

            // 取：上料
            Auto_OnloadPlaceMove,
            Auto_OnloadPlaceSendEvent,
            Auto_OnloadPlaceIn,
            Auto_OnloadPlaceDataTransfer,
            Auto_OnloadPlaceOut,
            Auto_OnloadPlaceCheckFinger,

            // 放：干燥炉
            Auto_DryingOvenPlaceMove,
            Auto_DryingOvenPlaceIn,
            Auto_DryingOvenPlaceDataTransfer,
            Auto_DryingOvenPlaceOut,
            Auto_DryingOvenPlaceCheckFinger,

            // 取：冷却炉
            Auto_CoolingStovePickMove,
            Auto_CoolingStovePickIn,
            Auto_CoolingStovePickOut,
            Auto_CoolingStovePickCheckFinger,

            // 放：缓存架
            Auto_PalletBufPlaceMove,
            Auto_PalletBufPlaceIn,
            Auto_PalletBufPlaceDataTransfer,
            Auto_PalletBufPlaceOut,
            Auto_PalletBufPlaceCheckFinger,

            // 放：人工平台
            Auto_ManualPlaceMove,
            Auto_ManualPlaceIn,
            Auto_ManualPlaceDataTransfer,
            Auto_ManualPlaceOut,
            Auto_ManualPlaceCheckFinger,

            // 放：下料
            Auto_OffloadPlaceMove,
            Auto_OffloadPlaceSendEvent,
            Auto_OffloadPlaceIn,
            Auto_OffloadPlaceDataTransfer,
            Auto_OffloadPlaceOut,
            Auto_OffloadPlaceCheckFinger,

            Auto_WorkEnd,
        }

        private enum ModuleDef
        {
            // 无效
            DefInvalid = -1,

            // 托盘
            Pallet_0 = 0,
            Pallet_All,
        }

        // 托盘匹配模式
        private enum MatchMode
        {
            //******* 放治具 *******
            Place_SameAndInvalid = 0,       // 同类型 && 无效
            Place_InvalidAndInvalid,        // 无效 && 无效
            Place_InvalidAndOther,          // 无效 && 其他
            Place_End,

            //******* 取治具 *******
            Pick_SameAndInvalid,            // 同类型 && 无效
            Pick_SameAndNotSame,            // 同类型 && !同类型
            Pick_SameAndOther,              // 同类型 && 其他
            Pick_End,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.TransferRobotMsgStartID,
            SendRbtMoveCmd,
            RbtDisConnect,
            RbtMoveCmdError,
            RbtMoveTimeout,
            AutoCheckPosStep,
        }
        #endregion


        #region // 数据结构定义

        private struct ActionInfo
        {
            private int row;
            private int col;
            private TransferRobotStation station;
            private ModuleEvent eEvent;

            public int Row { get => row; set => row = value; }
            public int Col { get => col; set => col = value; }
            public TransferRobotStation Station { get => station; set => station = value; }
            public ModuleEvent EEvent { get => eEvent; set => eEvent = value; }

            // 清除数据
            public void Release()
            {
                SetAction(TransferRobotStation.Invalid, -1, -1, ModuleEvent.ModuleEventInvalid);
            }

            // 设置动作
            public void SetAction(TransferRobotStation Station, int nRow, int nCol, ModuleEvent curEvent)
            {
                this.Row = nRow;
                this.col = nCol;
                this.Station = Station;
                this.eEvent = curEvent;
            }
        }
        #endregion


        #region // 字段

        // 【相关模组】
        private RunProPalletBuf palletBuf;                  // 托盘缓存
        private RunProManualOperat manualOperat;            // 人工平台
        private RunProOnloadRobot onloadRobot;              // 上料机器人
        private RunProOffloadRobot offloadRobot;            // 下料机器人
        private RunProDryingOven[] arrDryingOven;           // 干燥炉组
        private RunProCoolingStove coolingStove;            // 冷却炉

        // 【IO/电机】
        private int IPltLeftCheck;                          // 托盘左检测
        private int IPltRightCheck;                         // 托盘右检测
        private int IPltHasCheck;                           // 托盘有料感应
                                                            // 【模组参数】
        private string _strRobotIP;                         // 机器人IP
        public string strRobotIP { get => _strRobotIP; set => SetProperty(ref _strRobotIP, value); }           // 机器人IP
        private int _nRobotPort;                            // 机器人端口
        public int nRobotPort { get => _nRobotPort; set => SetProperty(ref _nRobotPort, value); }
        private bool bRobotEN;                              // 机器人使能
        private int nRobotSpeed;                            // 机器人速度：1-100
        private int nRobotTimeout;                          // 机器人超时时间(s)
        private int nRobotPickOutTimeout;                    //机器人取出超时时间(s)
        private bool bTimeOutAutoSearchStep;                // 调度等待超时自动搜索步骤
        private bool bPrintCT;                              // CT打印

        // 【模组数据】
        private ActionInfo PickAction;                      // 取动作信息
        private ActionInfo PlaceAction;                     // 放动作信息
        private ModuleEvent curEvent;                       // 当前信号（临时使用）
        private EventState curEventState;                   // 信号状态（临时使用）
        private int nEventRowIdx;                           // 信号行索引（临时使用）
        private int nEventColIdx;                           // 信号列索引（临时使用）
        private bool bIsOnloadFakePlt;                      // 指示托盘是否需要假电池

        private int nRobotID;                               // 机器人ID
        private int[] arrRobotCmd;                          // 机器人命令
        private RobotClient robotClient;                    // 机器人客户端
        public RobotActionInfo robotAutoInfo { get; set; }              // 机器人自动模式动作信息
        private RobotActionInfo robotDebugInfo;             // 机器人手动模式动作信息
        public bool robotProcessingFlag;                    // 机器人正在执行动作标志位
        private Dictionary<int, RobotFormula> robotStationInfo;  // 机器人工位信息
        private DateTime EventTimeOut;                      // 调度等待事件超时
        protected object nextAutoCheckStep;                 // 自动检查步骤
        public bool bOnloadRobotSafeEvent;                  //上料机器人安全信号
        public bool bOffloadRobotSafeEvent;                 //下料机器人安全信号
		private object nAutoStepCT;                         // CT步骤
        private DateTime dtAutoStepTime;                    // CT时间
        private bool connectState;      // 机器人连接状态(界面显示)
        public bool ConnectState
        {
            get { return connectState; }
            set { SetProperty(ref connectState, value); }
        }
        public bool RobotCrash { get => ((IRobot)onloadRobot).RobotCrash; set => ((IRobot)onloadRobot).RobotCrash = value; }

        public bool RobotProcessingFlag => ((IRobot)onloadRobot).RobotProcessingFlag;

        public Dictionary<int, RobotInfoStation> RobotStationInfo{ get; } = new();

        public int Finger_All => ((IRobot)onloadRobot).Finger_All;
        #endregion


        #region // 构造函数

        public RunProTransferRobot(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject((int)ModuleMaxPallet.TransferRobot, 0, 0, 0);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("RobotEN", "机器人使能", "TRUE启用，FALSE禁用", bRobotEN);
            InsertPrivateParam("RobotIP", "机器人IP", "机器人IP", strRobotIP);
            InsertPrivateParam("RobotPort", "机器人端口", "机器人通讯端口号", nRobotPort);
            InsertPrivateParam("RobotSpeed", "机器人速度", "机器人速度为：1~100", nRobotSpeed);
            InsertPrivateParam("RobotTimeout", "机器人超时", "机器人超时时间(s)", nRobotTimeout);
            InsertPrivateParam("RobotPickOutTimeout", "机器人取出超时", "机器人取出超时时间(s)", nRobotPickOutTimeout);

            //InsertPrivateParam("TimeOutAutoSearchStep", "自动搜索", "调度等待超时自动搜索步骤", bTimeOutAutoSearchStep, RecordType.RECORD_INT);
            InsertPrivateParam("PrintCT", "CT打印使能", "TRUE启用，FALSE禁用", bPrintCT);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IPltLeftCheck = -1;
            IPltRightCheck = -1;
            IPltHasCheck = -1;

            // 模组参数
            bRobotEN = false;
            strRobotIP = "";
            nRobotPort = 0;
            nRobotSpeed = 10;
            nRobotTimeout = 30;
            nRobotPickOutTimeout = 30;
            bPrintCT = false;
            ConnectState = false;

            // 模组数据
            arrRobotCmd = new int[10];
            robotClient = new RobotClient();
            robotAutoInfo = new RobotActionInfo();
            robotDebugInfo = new RobotActionInfo();
            robotStationInfo = new Dictionary<int, RobotFormula>();
            EventTimeOut = DateTime.Now;
            nextAutoCheckStep = new object();
            bOnloadRobotSafeEvent = false;
            bOffloadRobotSafeEvent = false;
			nAutoStepCT = new object();
            dtAutoStepTime = DateTime.Now;
            robotProcessingFlag = false;
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
            InputAdd("IPltLeftCheck", ref IPltLeftCheck);
            InputAdd("IPltRightCheck", ref IPltRightCheck);
            InputAdd("IPltHasCheck", ref IPltHasCheck);

            nRobotID = IniFile.ReadInt(this.RunModule, "RobotID", (int)RobotIndexID.OnloadRobot, Def.GetAbsPathName(Def.ModuleExCfg));
            InitRobotStation();

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
                        this.nextInitStep = InitSteps.Init_CheckPallet;
                        break;
                    }
                case InitSteps.Init_CheckPallet:
                    {
                        CurMsgStr("检查托盘状态", "Check pallet");

                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            if (!CheckPallet(nPltIdx, Pallet[nPltIdx].Type > PltType.Invalid))
                            {
                                break;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_RobotConnect;
                        break;
                    }
                case InitSteps.Init_RobotConnect:
                    {
                        CurMsgStr("连接机器人", "Connect robot");

                        if (RobotConnect())
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
                Sleep(20);
            }
            switch ((AutoCheckStep)this.nextAutoCheckStep)
            {
                case AutoCheckStep.Auto_CheckRobotCmd:
                    {
                        if (!CheckTransferRobotPos())
                        {
                            return;
                        }

                        nextAutoCheckStep = AutoCheckStep.Auto_CheckFinish;
                        break;
                    }
                default:
                    break;
            }

            if(nAutoStepCT != nextAutoStep)
            {
                if((int)nextAutoStep > 1 && bPrintCT)
                {
                    string sFilePath = "D:\\LogFile\\调度CT测试";
                    string sFileName = DateTime.Now.ToString("yyyyMMdd") + ".CSV";
                    string sColHead = "步骤名,步数,速度,时间(毫秒)";
                    string sLog = string.Format("{0},{1},{2},{3}", msgChs, (int)nAutoStepCT, nRobotSpeed, 
                        (DateTime.Now - dtAutoStepTime).Seconds * 1000 + (DateTime.Now - dtAutoStepTime).Milliseconds);
                    MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
                    nAutoStepCT = nextAutoStep;
                    dtAutoStepTime = DateTime.Now;
                }
            }
            switch ((AutoSteps)this.nextAutoStep)
            {
                #region // 信号发送和响应

                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        bool bCalcResult = false;

                        // 炉子取托盘(转移)
                        if (!bCalcResult) bCalcResult = CalcOvenPickTransfer(ref PickAction, ref PlaceAction);

                        // 动态取托盘
                        if (!bCalcResult) bCalcResult = CalcDynamicPick(ref PickAction, ref PlaceAction);

                        // 上料取托盘
                        if (!bCalcResult) bCalcResult = CalcOnLoadPick(ref PickAction, ref PlaceAction);
                        // 冷却架取托盘
                        if (!bCalcResult) bCalcResult = CalcCoolingPlace(ref PickAction, ref PlaceAction);
                        // 下料放托盘
                        if (!bCalcResult) bCalcResult = CalcOffLoadPlace(ref PickAction, ref PlaceAction);

                        // 上料放托盘
                        if (!bCalcResult) bCalcResult = CalcOnLoadPlace(ref PickAction, ref PlaceAction);
                        // 下料取托盘
                        if (!bCalcResult) bCalcResult = CalcOffLoadPick(ref PickAction, ref PlaceAction);

                        // 人工操作平台放托盘
                        if (!bCalcResult) bCalcResult = CalcManualOperPlatPlace(ref PickAction, ref PlaceAction);
                        // 人工操作平台取托盘
                        if (!bCalcResult) bCalcResult = CalcManualOperPlatPick(ref PickAction, ref PlaceAction);

                        if (bCalcResult)
                        {
                            this.nextAutoStep = AutoSteps.Auto_SendPickEventBeforeAction;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }

                #endregion


                #region // 预先发送信号

                case AutoSteps.Auto_SendPickEventBeforeAction:
                    {
                        CurMsgStr("动作前发送取料信号", "Send pick event before action");

                        if (PreSendEvent(PickAction))
                        {

                            if (PickAction.Station == TransferRobotStation.OffloadStation && PickAction.EEvent == ModuleEvent.OffloadPickEmptyPlt)
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPickPos;
                            }
                            else
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendPlaceEventBeforeAction;
                            }
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_SendPlaceEventBeforeAction:
                    {
                        CurMsgStr("动作前发送放料信号", "Send place event before action");

                        if (PreSendEvent(PlaceAction))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPickPos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 计算取料位

                case AutoSteps.Auto_CalcPickPos:
                    {
                        CurMsgStr("计算取料位", "Calc pick pos");

                        // 【干燥炉】
                        if (PickAction.Station > TransferRobotStation.Invalid && PickAction.Station <= TransferRobotStation.DryingOven_5)
                        {
                            this.nextAutoStep = AutoSteps.Auto_DryingOvenPickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        // 【取冷却架】
                        else if (PickAction.Station == TransferRobotStation.CooligStove)
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingStovePickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        // 【托盘缓存架】
                        else if (TransferRobotStation.PalletBuffer == PickAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletBufPickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【人工操作平台】
                        else if (TransferRobotStation.ManualOperat == PickAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_ManualPickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【上料区】
                        else if (TransferRobotStation.OnloadStation == PickAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_OnloadPickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【下料区】
                        else if (TransferRobotStation.OffloadStation == PickAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPickMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        break;
                    }

                #endregion


                #region // 取：上料

                case AutoSteps.Auto_OnloadPickMove:
                    {
                        CurMsgStr("机器人到上料取托盘移动", "Onload pick pallet move");

                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OnloadPickSendEvent;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPickSendEvent:
                    {
                        CurMsgStr("发送取上料端托盘信号", "Onload pick pallet send event");

                        if (CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Require))
                        {
                            if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Response, PickAction.Row, PickAction.Col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnloadPickIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPickIn:
                    {
                        CurMsgStr("机器人到上料端取托盘进", "Onload pick pallet in");
                        bool bSafe = false;
                        bOnloadRobotSafeEvent = true;
                        bSafe = onloadRobot.bRobotSafeEvent;
                        if (bSafe && onloadRobot.bRobotSafeEvent && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if(CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false))
                            {
                                if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Col]);
                                        run.Pallet[PickAction.Col].Release();
                                        run.SaveRunData(SaveType.Pallet, PickAction.Col);

                                        this.nextAutoStep = AutoSteps.Auto_OnloadPickDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }                           
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPickDataTransfer:
                    {
                        CurMsgStr("上料端取托盘数据转移", "Onload pick pallet data transfer");

                        /*RunProcess run = null;
                        if (GetModuleByStation(PickAction.Station, ref run))
                        {
                            // 数据转移
                            Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Col]);
                            run.Pallet[PickAction.Col].Release();
                            run.SaveRunData(SaveType.Pallet, PickAction.Col);

                            this.nextAutoStep = AutoSteps.Auto_OnloadPickOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_OnloadPickOut;
                        break;
                    }
                case AutoSteps.Auto_OnloadPickOut:
                    {
                        CurMsgStr("机器人到上料端取托盘出", "Onload pick pallet out");

                        if (CheckPallet(0, true))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                bOnloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_OnloadPickCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPickCheckFinger:
                    {
                        CurMsgStr("上料端取托盘后检查抓手", "Onload pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 取：干燥炉

                case AutoSteps.Auto_DryingOvenPickMove:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列取托盘移动",
                            GetStationName((TransferRobotStation)PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven pick pallet move");
                        
                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            EventTimeOut = DateTime.Now;
                            this.nextAutoStep = AutoSteps.Auto_DryingOvenPickIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPickIn:
                    {
				        this.msgChs = string.Format("机器人取托盘进[{0}-{1}行-{2}列]", GetStationName(PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        this.msgEng = string.Format("Drying oven pick pallet in[{0}-{1}row-{2}col]", GetStationName(PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
					
                        bool bSafe = false;
                        //   if (TransferRobotStation.DryingOven_0 == PickAction.Station || TransferRobotStation.DryingOven_1 == PickAction.Station)
                        //if (TransferRobotStation.DryingOven_0 == PickAction.Station 
                        //    || (TransferRobotStation.DryingOven_1 == PickAction.Station && PickAction.Col == 0))
                        //{
                        //    bOnloadRobotSafeEvent = true;
                        //    bSafe = onloadRobot.bRobotSafeEvent;
                        //}
                        // else if (TransferRobotStation.DryingOven_6 == PickAction.Station || TransferRobotStation.DryingOven_9 == PickAction.Station || TransferRobotStation.DryingOven_5 == PickAction.Station)
                        //else if (TransferRobotStation.DryingOven_5 == PickAction.Station)
                        // {
                        //    bOffloadRobotSafeEvent = true;
                        //    bSafe = offloadRobot.bRobotSafeEvent;
                        //} //6号炉下料避让暂时屏蔽duanyh2024-1108                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
                        //else
                        //{
                        //    bSafe = true;
                        //}

                        bSafe = true;
                        if (bSafe && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false)
                                && CheckOvenState((int)PickAction.Station, PickAction.Col))
                            {
                                
                                if ( RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    Pallet destPlt = null;
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        RunProDryingOven curOven = run as RunProDryingOven;
                                        destPlt = curOven.GetPlt(PickAction.Col, PickAction.Row);
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(destPlt);
                                        destPlt.Release();

                                        // 保存数据
                                        int nPltIdx = PickAction.Col * (int)ModuleRowCol.DryingOvenRow + PickAction.Row;
                                        curOven.SaveRunData(SaveType.Pallet, nPltIdx);


                                        // 设置来源工位（取待检测 和 回炉托盘）
                                        if (ModuleEvent.OvenPickDetectPlt == PickAction.EEvent || ModuleEvent.OvenPickRebakingPlt == PickAction.EEvent)
                                        {
                                            Pallet[(int)ModuleDef.Pallet_0].SrcRow = PickAction.Row;
                                            Pallet[(int)ModuleDef.Pallet_0].SrcCol = PickAction.Col;
                                            Pallet[(int)ModuleDef.Pallet_0].SrcStation = (int)PickAction.Station;
                                        }

                                        this.nextAutoStep = AutoSteps.Auto_DryingOvenPickDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        //else
                        //{
                        //    // 暂时不用，待确认后使用
                        //    if (bTimeOutAutoSearchStep && (DateTime.Now - EventTimeOut).TotalSeconds > 10)
                        //    {
                        //        if (ReadyWaitTimeOutSearchAutoStep())
                        //        {
                        //            bOnloadRobotSafeEvent = false;
                        //            bOffloadRobotSafeEvent = false;
                        //            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        //        }
                        //    }
                        //}
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPickDataTransfer:
                    {
                        CurMsgStr("干燥炉取托盘数据转移", "Drying oven pick pallet data transfer");

                        /*Pallet destPlt = null;
                        RunProcess run = null;
                        if (GetModuleByStation(PickAction.Station, ref run))
                        {
                            // 数据转移
                            RunProDryingOven curOven = run as RunProDryingOven;
                            destPlt = curOven.GetPlt(PickAction.Row, PickAction.Col);
                            Pallet[(int)ModuleDef.Pallet_0].CopyFrom(destPlt);
                            destPlt.Release();

                            // 保存数据
                            int nPltIdx = PickAction.Row * (int)ModuleRowCol.DryingOvenCol + PickAction.Col;
                            curOven.SaveRunData(SaveType.Pallet, nPltIdx);

                            // 设置来源工位（取待检测 和 回炉托盘）
                            if (ModuleEvent.OvenPickDetectPlt == PickAction.EEvent || ModuleEvent.OvenPickRebakingPlt == PickAction.EEvent)
                            {
                                Pallet[(int)ModuleDef.Pallet_0].SrcRow = PickAction.Row;
                                Pallet[(int)ModuleDef.Pallet_0].SrcCol = PickAction.Col;
                                Pallet[(int)ModuleDef.Pallet_0].SrcStation = (int)PickAction.Station;
                            }

                            this.nextAutoStep = AutoSteps.Auto_DryingOvenPickOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_DryingOvenPickOut;
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPickOut:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列取托盘出",
                            GetStationName((TransferRobotStation)PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven pick pallet out");

                        if (CheckPallet(0, true) && CheckOvenDoorState((int)PickAction.Station, PickAction.Col))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                if (Pallet[0].Type == PltType.WaitOffload)
                                {
                                    OffLoadTimeCsv(Pallet[0].Code); // 记录下料时间
                                }
                                //bOnloadRobotSafeEvent = false;
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_DryingOvenPickCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPickCheckFinger:
                    {
                        CurMsgStr("干燥炉取托盘后检查抓手", "Drying oven pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region   // 取 冷却炉
                case AutoSteps.Auto_CoolingStovePickMove:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列取托盘移动", GetStationName((TransferRobotStation)PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven pick pallet move");

                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            EventTimeOut = DateTime.Now;
                            this.nextAutoStep = AutoSteps.Auto_CoolingStovePickIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingStovePickIn:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列取托盘进", GetStationName((TransferRobotStation)PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven pick pallet in");
                        bool bSafe = false;
                        bOffloadRobotSafeEvent = true;
                        bSafe = offloadRobot.bRobotSafeEvent;

                        if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false))
                            {

                                if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    Pallet destPlt = null;
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        RunProCoolingStove curCoolingStove = run as RunProCoolingStove;
                                        destPlt = curCoolingStove.GetPallet(PickAction.Row, PickAction.Col);
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(destPlt);
                                        destPlt.Release();

                                        curCoolingStove.SaveRunData(SaveType.Pallet);


                                        //// 设置来源工位（取待检测 和 回炉托盘）
                                        //if (ModuleEvent.OvenPickDetectPlt == PickAction.eEvent || ModuleEvent.OvenPickRebakingPlt == PickAction.eEvent)
                                        //{
                                        //    Pallet[(int)ModuleDef.Pallet_0].SrcRow = PickAction.row;
                                        //    Pallet[(int)ModuleDef.Pallet_0].SrcCol = PickAction.col;
                                        //    Pallet[(int)ModuleDef.Pallet_0].SrcStation = (int)PickAction.station;
                                        //}

                                        this.nextAutoStep = AutoSteps.Auto_CoolingStovePickOut;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 暂时不用，待确认后使用
                            if (bTimeOutAutoSearchStep && (DateTime.Now - EventTimeOut).TotalSeconds > 10)
                            {
                                if (ReadyWaitTimeOutSearchAutoStep())
                                {
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingStovePickOut:
                    {

                        string strInfo = string.Format("机器人{0}工位{1}行{2}列取托盘出",
                           GetStationName((TransferRobotStation)PickAction.Station), PickAction.Row + 1, PickAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven pick pallet out");

                        if (CheckPallet(0, true))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_CoolingStovePickCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingStovePickCheckFinger:
                    {
                        CurMsgStr("干燥炉取托盘后检查抓手", "Drying oven pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 取：缓存架

                case AutoSteps.Auto_PalletBufPickMove:
                    {
                        CurMsgStr("机器人到缓存架取托盘移动", "Pallet buf pick pallet move");

                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletBufPickIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPickIn:
                    {
                        CurMsgStr("机器人到缓存架取托盘进", "Pallet buf pick pallet in");                       
                        //bool bSafe = false;
                        //bOffloadRobotSafeEvent = true;
                        //bSafe = offloadRobot.bRobotSafeEvent;
                        //if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        if (CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false))
                            {
                              
                                if ( RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Row]);
                                        run.Pallet[PickAction.Row].Release();
                                        run.SaveRunData(SaveType.Pallet, PickAction.Row);

                                        this.nextAutoStep = AutoSteps.Auto_PalletBufPickDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPickDataTransfer:
                    {
                        CurMsgStr("缓存架取托盘数据转移", "Pallet buf pick pallet data transfer");

                        /*RunProcess run = null;
                        if (GetModuleByStation(PickAction.Station, ref run))
                        {
                            // 数据转移
                            Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Row]);
                            run.Pallet[PickAction.Row].Release();
                            run.SaveRunData(SaveType.Pallet, PickAction.Row);

                            this.nextAutoStep = AutoSteps.Auto_PalletBufPickOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_PalletBufPickOut;
                        break;
                    }
                case AutoSteps.Auto_PalletBufPickOut:
                    {
                        CurMsgStr("机器人到缓存架取托盘出", "Pallet buf pick pallet out");

                        if (CheckPallet(0, true))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_PalletBufPickCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPickCheckFinger:
                    {
                        CurMsgStr("缓存架取托盘后检查抓手", "Pallet buf pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 取：人工平台

                case AutoSteps.Auto_ManualPickMove:
                    {
                        CurMsgStr("机器人到人工平台取托盘移动", "Manual pick pallet move");

                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_ManualPickIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPickIn:
                    {
                        CurMsgStr("机器人到人工平台取托盘进", "Manual pick pallet in");
                        //bool bSafe = false;
                        //bOffloadRobotSafeEvent = true;
                        //bSafe = offloadRobot.bRobotSafeEvent;
                        //if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        if (CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false))
                            {

                                if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[0]);
                                        run.Pallet[0].Release();
                                        run.SaveRunData(SaveType.Pallet, 0);

                                        this.nextAutoStep = AutoSteps.Auto_ManualPickDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPickDataTransfer:
                    {
                        CurMsgStr("人工平台取托盘数据转移", "Manual pick pallet data transfer");

                        /*RunProcess run = null;
                        if (GetModuleByStation(PickAction.Station, ref run))
                        {
                            // 数据转移
                            Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[0]);
                            run.Pallet[0].Release();
                            run.SaveRunData(SaveType.Pallet, 0);

                            this.nextAutoStep = AutoSteps.Auto_ManualPickOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_ManualPickOut;
                        break;
                    }
                case AutoSteps.Auto_ManualPickOut:
                    {
                        CurMsgStr("机器人到人工平台取托盘出", "Manual pick pallet out");

                        if (CheckPallet(0, true))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_ManualPickCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPickCheckFinger:
                    {
                        CurMsgStr("人工平台取托盘后检查抓手", "Manual pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 取：下料

                case AutoSteps.Auto_OffloadPickMove:
                    {
                        CurMsgStr("机器人到下料取托盘移动", "Offload pick pallet move");

                        if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPickSendEvent;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPickSendEvent:
                    {
                        CurMsgStr("发送取下料端托盘信号", "Offload pick pallet send event");

                        if (CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Require))
                        {
                            if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Response, PickAction.Row, PickAction.Col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPickIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPickIn:
                    {
                        CurMsgStr("机器人到下料端取托盘进", "Offload pick pallet in");
                        bool bSafe = false;
                        bOffloadRobotSafeEvent = true;
                        bSafe = offloadRobot.bRobotSafeEvent;

                        if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PickAction.Station, PickAction.Row, PickAction.Col, true) && CheckPallet(0, false))
                            {
                                if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKIN))
                                {
                                    RunProcess run = null;
                                    if (GetModuleByStation(PickAction.Station, ref run))
                                    {
                                        // 数据转移
                                        Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Col]);
                                        run.Pallet[PickAction.Col].Release();
                                        run.SaveRunData(SaveType.Pallet, PickAction.Col);

                                        this.nextAutoStep = AutoSteps.Auto_OffloadPickDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPickDataTransfer:
                    {
                        CurMsgStr("下料端取托盘数据转移", "Offload pick pallet data transfer");

                        /*RunProcess run = null;
                        if (GetModuleByStation(PickAction.Station, ref run))
                        {
                            // 数据转移
                            Pallet[(int)ModuleDef.Pallet_0].CopyFrom(run.Pallet[PickAction.Col]);
                            run.Pallet[PickAction.Col].Release();
                            run.SaveRunData(SaveType.Pallet, PickAction.Col);

                            this.nextAutoStep = AutoSteps.Auto_OffloadPickOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_OffloadPickOut;
                        break;
                    }
                case AutoSteps.Auto_OffloadPickOut:
                    {
                        CurMsgStr("机器人到下料端取托盘出", "Offload pick pallet out");

                        if (CheckPallet(0, true))
                        {
                            if (RobotMove(PickAction.Station, PickAction.Row, PickAction.Col, nRobotSpeed, RobotAction.PICKOUT))
                            {
                                bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_OffloadPickCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPickCheckFinger:
                    {
                        CurMsgStr("下料端取托盘后检查抓手", "Offload pick pallet check finger");

                        if (SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Finished))
                        {
                            if(PickAction.EEvent == ModuleEvent.OffloadPickEmptyPlt)
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadReCalcPlace;
                            }
                            else
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            }
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadReCalcPlace:
                    {
                        CurMsgStr("下料端取托盘后重新计算放料位", "Offload Re Calc Place");

                        if (ReCalcPlaceOffEmptyPlt(ref PlaceAction))
                        {
                            //if (PlaceAction.Station == TransferRobotStation.DryingOven_0 && PlaceAction.Row == 2)
                            //{
                            //    this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            //    SaveRunData(SaveType.AutoStep);
                            //}
                            //else if (PreSendEvent(PlaceAction))
                            //{
                            //    this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            //    SaveRunData(SaveType.AutoStep);
                            //}
                            if (PreSendEvent(PlaceAction))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion


                #region // 计算放料位置

                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc place pos");
                        
                        // 【干燥炉】
                        if (PlaceAction.Station > TransferRobotStation.Invalid && PlaceAction.Station <= TransferRobotStation.DryingOven_5)
                        {
                            this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceMove;
                            SaveRunData(SaveType.AutoStep);

                            break;
                        }

                        // 【托盘缓存架】
                        else if (TransferRobotStation.PalletBuffer == PlaceAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【人工操作平台】
                        else if (TransferRobotStation.ManualOperat == PlaceAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_ManualPlaceMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【上料区】
                        else if (TransferRobotStation.OnloadStation == PlaceAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_OnloadPlaceMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【冷却架】
                        if (TransferRobotStation.CooligStove == PlaceAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingStovePlaceMove;

                            SaveRunData(SaveType.AutoStep);
                            break;
                        }

                        // 【下料区】
                        else if (TransferRobotStation.OffloadStation == PlaceAction.Station)
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPlaceMove;
                            SaveRunData(SaveType.AutoStep);
                            break;
                        }
                        break;
                    }

                #endregion


                #region // 放：上料

                case AutoSteps.Auto_OnloadPlaceMove:
                    {
                        CurMsgStr("机器人到上料端放托盘移动", "Onload place pallet move");

                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OnloadPlaceSendEvent;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPlaceSendEvent:
                    {
                        CurMsgStr("发送放上料端托盘信号", "Onload place pallet send event");

                        if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Require))
                        {
                            if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Response, PlaceAction.Row, PlaceAction.Col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnloadPlaceIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPlaceIn:
                    {
                        CurMsgStr("机器人到上料端放托盘进", "Onload place pallet in");

                        if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true))
                            {
                                if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 数据转移
                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        Pallet[(int)ModuleDef.Pallet_0].Code = "";
                                        run.Pallet[PlaceAction.Col].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        run.SaveRunData(SaveType.Pallet, PlaceAction.Col);

                                        this.nextAutoStep = AutoSteps.Auto_OnloadPlaceDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPlaceDataTransfer:
                    {
                        CurMsgStr("上料端放托盘数据转移", "Onload place pallet data transfer");

                        /*
                        // 设置托盘是否需要假电池
                        if (ModuleEvent.OnloadPlaceEmptyPallet == PlaceAction.EEvent)
                        {
                            if (bIsOnloadFakePlt)
                            {
                                foreach (RunProDryingOven curOven in arrDryingOven)
                                {
                                    if (CheckEvent(curOven, ModuleEvent.OvenPlaceFakeFullPlt, EventState.Require))
                                    {
                                        Pallet[(int)ModuleDef.Pallet_0].IsOnloadFake = true;
                                        break;
                                    }
                                }
                            }
                            bIsOnloadFakePlt = !bIsOnloadFakePlt;
                            SaveRunData(SaveType.Variables);
                        }

                        // 数据转移
                        RunProcess run = null;
                        if (GetModuleByStation(PlaceAction.Station, ref run))
                        {
                            // Pallet[(int)ModuleDef.Pallet_0].Code = "";
                            run.Pallet[PlaceAction.Col].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                            Pallet[(int)ModuleDef.Pallet_0].Release();
                            run.SaveRunData(SaveType.Pallet, PlaceAction.Col);

                            this.nextAutoStep = AutoSteps.Auto_OnloadPlaceOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_OnloadPlaceOut;
                        break;
                    }
                case AutoSteps.Auto_OnloadPlaceOut:
                    {
                        CurMsgStr("机器人到上料端放托盘出", "Onload place pallet out");

                        if (CheckPallet(0, false))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnloadPlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnloadPlaceCheckFinger:
                    {
                        CurMsgStr("上料端放托盘后检查抓手", "Onload place pallet check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放：干燥炉

                case AutoSteps.Auto_DryingOvenPlaceMove:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列放托盘移动",
                            GetStationName((TransferRobotStation)PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven place pallet move");

                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            bOnloadRobotSafeEvent = false;
                            bOffloadRobotSafeEvent = false;
                            EventTimeOut = DateTime.Now;

                            if ((PlaceAction.Station == TransferRobotStation.DryingOven_0|| PlaceAction.Station == TransferRobotStation.DryingOven_1 || PlaceAction.Station == TransferRobotStation.DryingOven_5) &&( PlaceAction.Row == 2|| PlaceAction.Row == 3))
                            {
                                /*if (PreSendEvent(PlaceAction))*/
                                {
                                    this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceIn;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                            }
                            else
                            {
                                this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceIn;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPlaceIn:
                    {
                        this.msgChs = string.Format("机器人放托盘进[{0}-{1}行-{2}列]", GetStationName(PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        this.msgEng = string.Format("Drying oven place pallet in[{0}-{1}row-{2}col]", GetStationName(PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        bool bSafe = false;
                        ////   if (TransferRobotStation.DryingOven_0 == PlaceAction.Station || TransferRobotStation.DryingOven_1 == PlaceAction.Station)
                        //if (TransferRobotStation.DryingOven_0 == PlaceAction.Station
                        //    || (TransferRobotStation.DryingOven_1 == PlaceAction.Station && PlaceAction.Col == 0))
                        //{
                        //    bOnloadRobotSafeEvent = true;
                        //    bSafe = onloadRobot.bRobotSafeEvent;
                        //}
                        //// else if (TransferRobotStation.DryingOven_6 == (TransferRobotStation)PlaceAction.Station || TransferRobotStation.DryingOven_9 == (TransferRobotStation)PlaceAction.Station || TransferRobotStation.DryingOven_5 == (TransferRobotStation)PlaceAction.Station)
                        //else if (TransferRobotStation.DryingOven_5 == (TransferRobotStation)PlaceAction.Station)
                        //{
                        //    bOffloadRobotSafeEvent = true;

                        //    bSafe = offloadRobot.bRobotSafeEvent;
                        //}
                        //else
                        //{
                        bSafe = true;
                        //}
                        if (bSafe && CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true)
                                && CheckOvenState((int)PlaceAction.Station, PlaceAction.Col))
                            {
                               
                                if ( RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 修改电池状态
                                    if (ModuleEvent.OvenPlaceRebakingFakePlt == PlaceAction.EEvent)
                                    {
                                        int nRow, nCol;
                                        nRow = nCol = -1;
                                        if (PltHasTypeBat(Pallet[(int)ModuleDef.Pallet_0], BatType.RBFake, ref nRow, ref nCol))
                                        {
                                            Pallet[(int)ModuleDef.Pallet_0].Bat[nRow, nCol].Type = BatType.Fake;
                                        }
                                    }

                                    // 数据转移
                                    Pallet destPlt = null;
                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        RunProDryingOven curOven = run as RunProDryingOven;
                                        destPlt = curOven.GetPlt(PlaceAction.Col, PlaceAction.Row);
                                        destPlt.CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        destPlt.PosInOven.OvenID = curOven.GetOvenID();
                                        destPlt.PosInOven.OvenRowID = PlaceAction.Row;
                                        destPlt.PosInOven.OvenColID = PlaceAction.Col;
                                        MachineCtrl.GetInstance().FittingBinding(curOven.GetOvenID(), destPlt, PlaceAction.Row, PlaceAction.Col);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        // 保存数据
                                        int nPltIdx = PlaceAction.Col * (int)ModuleRowCol.DryingOvenRow + PlaceAction.Row;
                                        curOven.SaveRunData(SaveType.Pallet, nPltIdx);

                                        this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                  /*      else
                        {
                            // 暂时不用，待确认后使用
                            if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Require))
                            {
                                SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Response, PlaceAction.Row, PlaceAction.Col);
                            }

                            if (bTimeOutAutoSearchStep && (DateTime.Now - EventTimeOut).TotalSeconds > 10)
                            {
                                if (ReadyWaitTimeOutSearchAutoStep())
                                {
                                    bOnloadRobotSafeEvent = false;
                                    bOffloadRobotSafeEvent = false;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                            }
                        }*/
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPlaceDataTransfer:
                    {
                        CurMsgStr("干燥炉放托盘数据转移", "Drying oven place data transfer");

                        /*
                        // 修改电池状态
                        if (ModuleEvent.OvenPlaceRebakingFakePlt == PlaceAction.EEvent)
                        {
                            int nRow, nCol;
                            nRow = nCol = -1;
                            if (PltHasTypeBat(Pallet[(int)ModuleDef.Pallet_0], BatType.RBFake, ref nRow, ref nCol))
                            {
                                Pallet[(int)ModuleDef.Pallet_0].Bat[nRow, nCol].Type = BatType.Fake;
                            }
                        }

                        // 数据转移
                        Pallet destPlt = null;
                        RunProcess run = null;
                        if (GetModuleByStation(PlaceAction.Station, ref run))
                        {
                            RunProDryingOven curOven = run as RunProDryingOven;
                            destPlt = curOven.GetPlt(PlaceAction.Row, PlaceAction.Col);
                            destPlt.CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                            Pallet[(int)ModuleDef.Pallet_0].Release();

                            // 保存数据
                            int nPltIdx = PlaceAction.Row * (int)ModuleRowCol.DryingOvenCol + PlaceAction.Col;
                            curOven.SaveRunData(SaveType.Pallet, nPltIdx);

                            this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceOut;
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPlaceOut:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列放托盘出",
                           GetStationName((TransferRobotStation)PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven place pallet out");

                        if (CheckPallet(0, false) && CheckOvenDoorState((int)PlaceAction.Station, PlaceAction.Col))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                //bOnloadRobotSafeEvent = false;
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_DryingOvenPlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_DryingOvenPlaceCheckFinger:
                    {
                        CurMsgStr("干燥炉放托盘后检查抓手", "Drying oven place pallet check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放： 冷却炉
                case AutoSteps.Auto_CoolingStovePlaceMove:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列放托盘移动",GetStationName((TransferRobotStation)PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven place pallet move");
                        
                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            EventTimeOut = DateTime.Now;

                            this.nextAutoStep = AutoSteps.Auto_CoolingStovePlaceIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingStovePlaceIn:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列放托盘进",GetStationName((TransferRobotStation)PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr("机器人到冷却炉放托盘进", "Drying oven place pallet in");
                        bool bSafe = false;
                        bOffloadRobotSafeEvent = true;
                        bSafe = offloadRobot.bRobotSafeEvent;
                        if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true))
                            {

                                if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 数据转移

                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        run.Pallet[PlaceAction.Row].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        // 保存数据
                                        int nPltIdx = PlaceAction.Col * (int)ModuleRowCol.DryingOvenCol * PlaceAction.Row;
                                        run.SaveRunData(SaveType.Pallet);

                                        this.nextAutoStep = AutoSteps.Auto_CoolingStovePlaceOut;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 暂时不用，待确认后使用
                            if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Require))
                            {
                                SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Response, PlaceAction.Row, PlaceAction.Col);
                            }

                            if (bTimeOutAutoSearchStep && (DateTime.Now - EventTimeOut).TotalSeconds > 10)
                            {
                                if (ReadyWaitTimeOutSearchAutoStep())
                                {
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_CoolingStovePlaceOut:
                    {
                        string strInfo = string.Format("机器人{0}工位{1}行{2}列放托盘出",
                           GetStationName((TransferRobotStation)PlaceAction.Station), PlaceAction.Row + 1, PlaceAction.Col + 1);
                        CurMsgStr(strInfo, "Drying oven place pallet out");

                        if (CheckPallet(0, false))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_CoolingStovePlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;

                    }
                case AutoSteps.Auto_CoolingStovePlaceCheckFinger:
                    {
                        CurMsgStr("冷却炉放托盘后检查抓手", "Drying oven place pallet check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion


                #region // 放：缓存架

                case AutoSteps.Auto_PalletBufPlaceMove:
                    {
                        CurMsgStr("机器人到缓存架放托盘移动", "Pallet buf place pallet move");

                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPlaceIn:
                    {
                        CurMsgStr("机器人到缓存架放托盘进", "Pallet buf place pallet in");
                        //bool bSafe = false;
                        //bOffloadRobotSafeEvent = true;
                        //bSafe = offloadRobot.bRobotSafeEvent;
                        //if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true))
                            {

                                if ( RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 数据转移
                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        run.Pallet[PlaceAction.Row].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        run.SaveRunData(SaveType.Pallet, PlaceAction.Row);

                                        this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPlaceDataTransfer:
                    {
                        CurMsgStr("缓存架放托盘数据转移", "Pallet buf place pallet data transfer");

                        /*
                        // 数据转移
                        RunProcess run = null;
                        if (GetModuleByStation(PlaceAction.Station, ref run))
                        {
                            run.Pallet[PlaceAction.Row].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                            Pallet[(int)ModuleDef.Pallet_0].Release();
                            run.SaveRunData(SaveType.Pallet, PlaceAction.Row);

                            this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceOut;
                        break;
                    }
                case AutoSteps.Auto_PalletBufPlaceOut:
                    {
                        CurMsgStr("机器人到缓存架放托盘出", "Pallet buf place pallet out");

                        if (CheckPallet(0, false) && CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, true))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_PalletBufPlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletBufPlaceCheckFinger:
                    {
                        CurMsgStr("缓存架放托盘后检查抓手", "Pallet buf place pallet check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放：人工平台

                case AutoSteps.Auto_ManualPlaceMove:
                    {
                        CurMsgStr("机器人到人工平台放托盘移动", "Manual place pallet move");

                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_ManualPlaceIn;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPlaceIn:
                    {
                        CurMsgStr("机器人到人工平台放托盘进", "Manual place pallet in");
                        //bool bSafe = false;
                        //bOffloadRobotSafeEvent = true;
                        //bSafe = offloadRobot.bRobotSafeEvent;
                        //if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true))
                            {
                                if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 数据转移
                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        run.Pallet[0].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        run.SaveRunData(SaveType.Pallet, 0);

                                        this.nextAutoStep = AutoSteps.Auto_ManualPlaceDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPlaceDataTransfer:
                    {
                        CurMsgStr("人工平台放托盘数据转移", "Manual place pallet data transfer");

                        /*
                        // 数据转移
                        RunProcess run = null;
                        if (GetModuleByStation(PlaceAction.Station, ref run))
                        {
                            run.Pallet[0].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                            Pallet[(int)ModuleDef.Pallet_0].Release();
                            run.SaveRunData(SaveType.Pallet, 0);

                            this.nextAutoStep = AutoSteps.Auto_ManualPlaceOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_ManualPlaceOut;
                        break;
                    }
                case AutoSteps.Auto_ManualPlaceOut:
                    {
                        CurMsgStr("机器人到人工平台放托盘出", "Manual place pallet out");

                        if (CheckPallet(0, false) && CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, true))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                //bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_ManualPlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_ManualPlaceCheckFinger:
                    {
                        CurMsgStr("上料端取托盘后检查抓手", "Onload pick check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放：下料

                case AutoSteps.Auto_OffloadPlaceMove:
                    {
                        CurMsgStr("机器人到下料端放托盘移动", "Offload place pallet move");

                        if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.MOVE))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPlaceSendEvent;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlaceSendEvent:
                    {
                        CurMsgStr("发送放下料端托盘信号", "Offload place pallet send event");

                        if (CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Require))
                        {
                            if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Response, PlaceAction.Row, PlaceAction.Col))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPlaceIn;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlaceIn:
                    {
                        CurMsgStr("机器人到下料端放托盘进", "Offload place pallet in");
                        bool bSafe = false;
                        bOffloadRobotSafeEvent = true;
                        bSafe = offloadRobot.bRobotSafeEvent;
                        if (bSafe && offloadRobot.bRobotSafeEvent && CheckModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Ready))
                        {
                            if (CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, false) && CheckPallet(0, true))
                            {
                                if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEIN))
                                {
                                    // 数据转移
                                    RunProcess run = null;
                                    if (GetModuleByStation(PlaceAction.Station, ref run))
                                    {
                                        run.Pallet[PlaceAction.Col].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                                        Pallet[(int)ModuleDef.Pallet_0].Release();
                                        run.SaveRunData(SaveType.Pallet, PlaceAction.Col);

                                        this.nextAutoStep = AutoSteps.Auto_OffloadPlaceDataTransfer;
                                        SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlaceDataTransfer:
                    {
                        CurMsgStr("下料端放托盘数据转移", "Offload place pallet data transfer");

                        /*
                        // 数据转移
                        RunProcess run = null;
                        if (GetModuleByStation(PlaceAction.Station, ref run))
                        {
                            run.Pallet[PlaceAction.Col].CopyFrom(Pallet[(int)ModuleDef.Pallet_0]);
                            Pallet[(int)ModuleDef.Pallet_0].Release();
                            run.SaveRunData(SaveType.Pallet, PlaceAction.Col);

                            this.nextAutoStep = AutoSteps.Auto_OffloadPlaceOut;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                        }*/

                        this.nextAutoStep = AutoSteps.Auto_OffloadPlaceOut;
                        break;
                    }
                case AutoSteps.Auto_OffloadPlaceOut:
                    {
                        CurMsgStr("机器人到下料端放托盘出", "Offload place pallet out");

                        if (CheckPallet(0, false) && CheckStation((int)PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, true))
                        {
                            if (RobotMove(PlaceAction.Station, PlaceAction.Row, PlaceAction.Col, nRobotSpeed, RobotAction.PLACEOUT))
                            {
                                bOffloadRobotSafeEvent = false;
                                this.nextAutoStep = AutoSteps.Auto_OffloadPlaceCheckFinger;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPlaceCheckFinger:
                    {
                        CurMsgStr("下料端放托盘后检查抓手", "Offload place pallet check finger");

                        if (SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 工作完成

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

                #endregion
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
            PickAction.Release();
            PlaceAction.Release();
            bIsOnloadFakePlt = true;
            nextAutoCheckStep = AutoCheckStep.Auto_CheckRobotCmd;
            nAutoStepCT = AutoSteps.Auto_WaitWorkStart;

            Array.Clear(arrRobotCmd, 0, arrRobotCmd.Length);
            robotAutoInfo.Release();
            robotDebugInfo.Release();

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
            this.bIsOnloadFakePlt = FileStream.ReadBool(section, "bIsOnloadFakePlt", this.bIsOnloadFakePlt);
            this.bOnloadRobotSafeEvent = FileStream.ReadBool(section, "bOnloadRobotSafeEvent", this.bOnloadRobotSafeEvent);
            this.bOffloadRobotSafeEvent = FileStream.ReadBool(section, "bOffloadRobotSafeEvent", this.bOffloadRobotSafeEvent);

            // 动作信息
            string[] arrName = new string[] { "PickAction", "PlaceAction" };
            ActionInfo[] arrInfo = new ActionInfo[] { PickAction, PlaceAction };

            for (int nIdx = 0; nIdx < arrInfo.Length; nIdx++)
            {
                key = string.Format("{0}.Station", arrName[nIdx]);
                arrInfo[nIdx].Station = (TransferRobotStation)FileStream.ReadInt(section, key, (int)arrInfo[nIdx].Station);

                key = string.Format("{0}.Row", arrName[nIdx]);
                arrInfo[nIdx].Row = FileStream.ReadInt(section, key, arrInfo[nIdx].Row);

                key = string.Format("{0}.col", arrName[nIdx]);
                arrInfo[nIdx].Col = FileStream.ReadInt(section, key, arrInfo[nIdx].Col);

                key = string.Format("{0}.eEvent", arrName[nIdx]);
                arrInfo[nIdx].EEvent = (ModuleEvent)FileStream.ReadInt(section, key, (int)arrInfo[nIdx].EEvent);
            }

            PickAction = arrInfo[0];
            PlaceAction = arrInfo[1];

            // 机器人动作信息
            arrName = new string[] { "robotAutoInfo", "robotDebugInfo" };
            RobotActionInfo[] arrAction = new RobotActionInfo[] { robotAutoInfo, robotDebugInfo };

            for (int nIdx = 0; nIdx < arrAction.Length; nIdx++)
            {
                key = string.Format("{0}.Station", arrName[nIdx]);
                arrAction[nIdx].Station = FileStream.ReadInt(section, key, arrAction[nIdx].Station);

                key = string.Format("{0}.Row", arrName[nIdx]);
                arrAction[nIdx].Row = FileStream.ReadInt(section, key, arrAction[nIdx].Row);

                key = string.Format("{0}.col", arrName[nIdx]);
                arrAction[nIdx].Col = FileStream.ReadInt(section, key, arrAction[nIdx].Col);

                key = string.Format("{0}.Action", arrName[nIdx]);
                arrAction[nIdx].action = (RobotAction)FileStream.ReadInt(section, key, (int)arrAction[nIdx].action);

                key = string.Format("{0}.StationName", arrName[nIdx]);
                arrAction[nIdx].stationName = FileStream.ReadString(section, key, arrAction[nIdx].stationName);
            }

            robotAutoInfo = arrAction[0];
            robotDebugInfo = arrAction[1];

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
                FileStream.WriteBool(section, "bIsOnloadFakePlt", this.bIsOnloadFakePlt);
                FileStream.WriteBool(section, "bOnloadRobotSafeEvent", this.bOnloadRobotSafeEvent);
                FileStream.WriteBool(section, "bOffloadRobotSafeEvent", this.bOffloadRobotSafeEvent);

                // 动作信息
                string[] arrName = new string[] { "PickAction", "PlaceAction" };
                ActionInfo[] arrInfo = new ActionInfo[] { PickAction, PlaceAction };

                for (int nIdx = 0; nIdx < arrInfo.Length; nIdx++)
                {
                    key = string.Format("{0}.Station", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrInfo[nIdx].Station);

                    key = string.Format("{0}.Row", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrInfo[nIdx].Row);

                    key = string.Format("{0}.col", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrInfo[nIdx].Col);

                    key = string.Format("{0}.eEvent", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrInfo[nIdx].EEvent);
                }
            }
            else if (SaveType.Robot == (SaveType.Robot & saveType))
            {
                // 机器人动作信息
                string[] arrName = new string[] { "robotAutoInfo", "robotDebugInfo" };
                RobotActionInfo[] arrAction = new RobotActionInfo[] { robotAutoInfo, robotDebugInfo };

                for (int nIdx = 0; nIdx < arrAction.Length; nIdx++)
                {
                    key = string.Format("{0}.Station", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrAction[nIdx].Station);

                    key = string.Format("{0}.Row", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrAction[nIdx].Row);

                    key = string.Format("{0}.col", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrAction[nIdx].Col);

                    key = string.Format("{0}.Action", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrAction[nIdx].action);

                    key = string.Format("{0}.StationName", arrName[nIdx]);
                    FileStream.WriteString(section, key, arrAction[nIdx].stationName);
                }
            }

            base.SaveRunData(saveType, index);
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleData()
        {
            if (!CheckPallet(0, false, false))
            {
                string strInfo;
                strInfo = string.Format("调度机器人货叉感应到有非空托盘，禁止清除任务！\r\n请确认货叉上为空托盘，并将空托盘移除，否则禁止删除任务");
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return false;
            }
            base.CopyRunDataClearBak();
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            PickAction.Release();
            PlaceAction.Release();
            Pallet[0].Release();
            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.SignalEvent | SaveType.Pallet);
            return true;
        }

        /// <summary>
        /// 清除模组任务
        /// </summary>
        public override bool ClearModuleTask()
        {
            string strInfo = "";
            RunProTransferRobot runTransfer = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
            //只有无托盘才能清除
            if (!runTransfer.CheckPallet(0, false, false))
            {
                strInfo = string.Format("调度机器人货叉感应到有非空托盘，禁止清除任务！\r\n请确认货叉上为空托盘，并将空托盘移除，否则禁止删除任务");
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return false;
            }

            //自动要移动或取出放出，手动在“移动”或“放出”或“取出”
            if (!(robotDebugInfo.action == RobotAction.PLACEOUT || robotDebugInfo.action == RobotAction.PICKOUT || robotDebugInfo.action == RobotAction.MOVE))
            {
                strInfo = string.Format("调度机器人货叉不在安全位置，禁止清除任务！\r\n请确认货叉位置安全，否则禁止删除任务");
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return false;
            }

            // 检查调度与上料交互情况
            RunProcess runOnloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if (!runOnloadRobot.CheckModuleEventState())
            {
                strInfo = string.Format("《调度机器人》与《上料机器人》处于交互中\r\n请点击【确定】将清除《上料机器人》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOnloadRobot.ClearModuleData())
                    {
                        strInfo = string.Format("调度机器人模组任务清除【失败】！！！《调度机器人》与《上料机器人》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 检查调度与下料交互情况
            RunProcess runOffloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot);
            if (!runOffloadRobot.CheckModuleEventState())
            {
                strInfo = string.Format("《调度机器人》与《下料机器人》处于交互中\r\n请点击【确定】将清除《下料机器人》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOffloadRobot.ClearModuleData())
                    {
                        strInfo = string.Format("调度机器人模组任务清除【失败】！！！《调度机器人》与《下料机器人》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 检查调度与炉子交互情况
            for (RunID ovenID = RunID.DryOven0; ovenID < RunID.RunIDEnd; ovenID++)
            {
                int nOvenIdx = ovenID - RunID.DryOven0 + 1;
                RunProcess runDryingOven = MachineCtrl.GetInstance().GetModule(ovenID);
                if (!runDryingOven.CheckModuleEventState())
                {
                    strInfo = string.Format("《调度机器人》与《干燥炉{0}》处于交互中\r\n请点击【确定】将清除《干燥炉{1}》任务数据", nOvenIdx, nOvenIdx);
                    if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                    {
                        if (!runDryingOven.ClearModuleData())
                        {
                            strInfo = string.Format("干燥炉{0}模组任务清除【失败】！！！《干燥炉{1}》与《调度机器人》处于交互中!!!", nOvenIdx, nOvenIdx);
                            ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                            return false;
                        }
                    }
                    else return false;
                }
            }

            // 检查调度与托盘缓存交互情况
            RunProcess runPalletBuf = MachineCtrl.GetInstance().GetModule(RunID.PalletBuf);
            if (!runPalletBuf.CheckModuleEventState())
            {
                strInfo = string.Format("《调度机器人》与《托盘缓存》处于交互中\r\n请点击【确定】将清除《托盘缓存》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runPalletBuf.ClearModuleData())
                    {
                        strInfo = string.Format("调度机器人模组任务清除【失败】！！！《调度机器人》与《托盘缓存》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 检查调度与人工操作台交互情况
            RunProcess runManualOperate = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            if (!runManualOperate.CheckModuleEventState())
            {
                strInfo = string.Format("《调度机器人》与《人工操作台》处于交互中\r\n请点击【确定】将清除《人工操作台》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runManualOperate.ClearModuleData())
                    {
                        strInfo = string.Format("调度机器人模组任务清除【失败】！！！《调度机器人》与《人工操作台》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 检查调度与冷却炉交互情况
            RunProcess runCoolingStove = MachineCtrl.GetInstance().GetModule(RunID.CoolingStove);
            if (!runCoolingStove.CheckModuleEventState())
            {
                strInfo = string.Format("《调度机器人》与《冷却炉》处于交互中\r\n请点击【确定】将清除《冷却炉》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runCoolingStove.ClearModuleData())
                    {
                        strInfo = string.Format("调度机器人模组任务清除【失败】！！！《调度机器人》与《冷却炉》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            if (!runTransfer.ClearModuleData())
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

            bRobotEN = ReadParam(RunModule, "RobotEN", false);
            strRobotIP = ReadParam(RunModule, "RobotIP", "");
            nRobotPort = ReadParam(RunModule, "RobotPort", 0);
            nRobotSpeed = ReadParam(RunModule, "RobotSpeed", 10);
            nRobotTimeout = ReadParam(RunModule, "RobotTimeout", 30);
            nRobotPickOutTimeout = ReadParam(RunModule, "RobotPickOutTimeout", 30);

            bTimeOutAutoSearchStep = ReadParam(RunModule, "TimeOutAutoSearchStep", false);
            bPrintCT = ReadParam(RunModule, "PrintCT", false);

            return true;
        }

        /// <summary>
        /// 写入数据库参数
        /// </summary>
        public override void SaveParameter()
        {
            // 保存自动搜索变量信息
            string strMsg;
            strMsg = string.Format("调度自动搜索开启，m_bTimeOutAutoSearchStep值{0}", bTimeOutAutoSearchStep);
            MachineCtrl.GetInstance().WriteLog(strMsg);
            WriteParameterCode(RunModule, "TimeOutAutoSearchStep", bTimeOutAutoSearchStep.ToString());
            base.SaveParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;
            arrDryingOven = new RunProDryingOven[6];

            // 托盘缓存
            strValue = IniFile.ReadString(strModule, "PalletBuf", "", Def.GetAbsPathName(Def.ModuleExCfg));
            palletBuf = MachineCtrl.GetInstance().GetModule(strValue) as RunProPalletBuf;

            // 人工平台
            strValue = IniFile.ReadString(strModule, "ManualOperat", "", Def.GetAbsPathName(Def.ModuleExCfg));
            manualOperat = MachineCtrl.GetInstance().GetModule(strValue) as RunProManualOperat;

            // 上料机器人
            strValue = IniFile.ReadString(strModule, "OnloadRobot", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadRobot = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadRobot;

            // 下料机器人
            strValue = IniFile.ReadString(strModule, "OffloadRobot", "", Def.GetAbsPathName(Def.ModuleExCfg));
            offloadRobot = MachineCtrl.GetInstance().GetModule(strValue) as RunProOffloadRobot;

            // 冷却架
            strValue = IniFile.ReadString(strModule, "CoolingStove", "", Def.GetAbsPathName(Def.ModuleExCfg));
            coolingStove = MachineCtrl.GetInstance().GetModule(strValue) as RunProCoolingStove;

            // 干燥炉组
            for (int nOvenIdx = 0; nOvenIdx < arrDryingOven.Length; nOvenIdx++)
            {
                strValue = IniFile.ReadString(strModule, "DryingOven" + "[" + (nOvenIdx + 1) + "]", "", Def.GetAbsPathName(Def.ModuleExCfg));
                arrDryingOven[nOvenIdx] = MachineCtrl.GetInstance().GetModule(strValue) as RunProDryingOven;
            }
        }

        #endregion


        #region // 匹配路径

        // ================================ 匹配路径 ================================
        /// <summary>
        /// 计算动态取料
        /// </summary>
        private bool CalcDynamicPick(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            if (robotAutoInfo.Station >= (int)TransferRobotStation.DryingOven_0 
                && robotAutoInfo.Station <= (int)TransferRobotStation.DryingOven_5)
            {
                // 下料区放干燥完成托盘
                if (SearchOffLoadPlacePos(ModuleEvent.OffloadPlaceDryFinishedPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 干燥炉取待下料托盘（干燥完成托盘）
                    if (OvenGlobalSearch(true, ModuleEvent.OvenPickOffloadPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickOffloadPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OffloadStation, 0, nPlaceCol, ModuleEvent.OffloadPlaceDryFinishedPlt);
                        return true;
                    }
                }

                // 下料区放待检测含假电池托盘（未取走假电池的托盘）
                if (SearchOffLoadPlacePos(ModuleEvent.OffloadPlaceDetectFakePlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）
                    if (OvenGlobalSearch(true, ModuleEvent.OvenPickDetectPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickDetectPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OffloadStation, 0, nPlaceCol, ModuleEvent.OffloadPlaceDetectFakePlt);
                        return true;
                    }
                }

                // 上料区放待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceRebakingFakePlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 干燥炉取待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                    if (OvenGlobalSearch(true, ModuleEvent.OvenPickRebakingPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickRebakingPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceRebakingFakePlt);
                        return true;
                    }
                }

                // 上料区放NG非空托盘，转盘
                if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceNGPallet, ref nPlaceRow, ref nPlaceCol))
                {
                    // 干燥炉取NG非空托盘
                    if (OvenGlobalSearch(true, ModuleEvent.OvenPickNGPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickNGPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceNGPallet);
                        return true;
                    }
                }

                // 上料区放空托盘
                if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                {
                    // 干燥炉取空托盘
                    if (OvenGlobalSearch(true, ModuleEvent.OvenPickEmptyPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                        return true;
                    }
                }

                // 下料区取空托盘
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 上料区放空托盘
                    if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                        return true;
                    }
                }
            }
            if (robotAutoInfo.Station == (int)TransferRobotStation.OffloadStation 
                || robotAutoInfo.Station == (int)TransferRobotStation.PalletBuffer
                || robotAutoInfo.Station == (int)TransferRobotStation.ManualOperat)
            {
                // 下料区取空托盘
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 上料区放空托盘
                    if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                        return true;
                    }
                }

                // 下料区取等待水含量结果托盘（已取待测假电池的托盘）
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickDetectFakePlt, ref nPickRow, ref nPickCol))
                {
                    Pallet curPlt = null;
                    RunProDryingOven curOven = null;

                    if (null != offloadRobot)
                    {
                        curPlt = offloadRobot.Pallet[nPickCol];
                        nPlaceRow = curPlt.SrcRow;
                        nPlaceCol = curPlt.SrcCol;
                        nOvenID = curPlt.SrcStation - (int)TransferRobotStation.DryingOven_0;
                        curOven = GetOvenByID(nOvenID);
                    }

                    // 检查条件
                    if (nOvenID > -1 && nPlaceRow > -1 && nPlaceCol > -1 && null != curOven && null != curPlt)
                    {
                        if (CheckEvent(curOven, ModuleEvent.OvenPlaceWaitResultPlt, EventState.Require))
                        {
                            if (curOven.IsCavityEN(nPlaceRow) && !curOven.IsPressure(nPlaceRow) && !curOven.IsTransfer(nPlaceCol, nPlaceRow) &&
                                (CavityState.Detect == curOven.GetCavityState(nPlaceRow)) && curOven.GetPlt(nPlaceRow, nPlaceCol).IsType(PltType.Invalid))
                            {
                                // 取
                                Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickDetectFakePlt);
                                // 放
                                Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceWaitResultPlt);
                                return true;
                            }
                        }
                    }
                }

                // 下料区取NG空托盘
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickNGEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 人工操作平台放NG空托盘
                    if (SearchManualOperPlatPlacePos(ModuleEvent.ManualOperatPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickNGEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                        return true;
                    }

                    // 缓存架放NG空托盘
                    if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickNGEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceNGEmptyPlt);
                        return true;
                    }
                }

                // 人工操作平台取空托盘
                if (SearchManualOperPlatPickPos(ModuleEvent.ManualOperatPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 上料区放空托盘
                    if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPickEmptyPlt);
                        // 放
                        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                        return true;
                    }
                }

                // 缓存架取空托盘
                //if (SearchPltBufPickPos(ModuleEvent.PltBufPickEmptyPlt, ref nPickRow, ref nPickCol))
                //{
                //    // 上料区放空托盘
                //    if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                //    {
                //        // 取
                //        Pick.SetAction(TransferRobotStation.PalletBuffer, nPickRow, 0, ModuleEvent.PltBufPickEmptyPlt);
                //        // 放
                //        Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                //        return true;
                //    }
                //}
            }

            return false;
        }

        private bool CalcCoolingPlace(ref ActionInfo pickAction, ref ActionInfo placeAction)
        {
            int nPlaceRow = -1;
            int nPlaceCol = -1;
            int nPlaceOvenID = -1;
            if (OvenGlobalSearch(true, ModuleEvent.OvenPickWaitCooling, ref nPlaceOvenID, ref nPlaceRow, ref nPlaceCol))
            {
                if (SearchCoolingPickPos(ModuleEvent.CoolingPutAction, out var pickRow, out var pickCol))
                {
                    pickAction.SetAction(TransferRobotStation.DryingOven_0 + nPlaceOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPickWaitCooling);
                    placeAction.SetAction(TransferRobotStation.CooligStove, pickRow, pickCol, ModuleEvent.CoolingPutAction);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算上料取料
        /// </summary>
        private bool CalcOnLoadPick(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 上料区取NG空托盘
            if (SearchOnloadPickPos(ModuleEvent.OnloadPickNGEmptyPallet, ref nPickRow, ref nPickCol))
            {
                // 人工操作平台放NG空托盘
                if (SearchManualOperPlatPlacePos(ModuleEvent.ManualOperatPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OnloadStation, 0, nPickCol, ModuleEvent.OnloadPickNGEmptyPallet);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }

                // 缓存架放NG空托盘
                if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OnloadStation, 0, nPickCol, ModuleEvent.OnloadPickNGEmptyPallet);
                    // 放
                    Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceNGEmptyPlt);
                    return true;
                }
            }

            // 上料区取回炉假电池托盘（已放回假电池的托盘）
            if (SearchOnloadPickPos(ModuleEvent.OnloadPickRebakingFakePlt, ref nPickRow, ref nPickCol))
            {
                RunProDryingOven curOven = null;

                if (null != onloadRobot)
                {
                    nPlaceRow = onloadRobot.Pallet[nPickCol].SrcRow;
                    nPlaceCol = onloadRobot.Pallet[nPickCol].SrcCol;
                    nOvenID = onloadRobot.Pallet[nPickCol].SrcStation - (int)TransferRobotStation.DryingOven_0;
                    curOven = GetOvenByID(nOvenID);
                }

                // 检查条件
                if (nOvenID > -1 && nPlaceRow > -1 && nPlaceCol > -1 && null != curOven)
                {
                    if (CheckEvent(curOven, ModuleEvent.OvenPlaceRebakingFakePlt, EventState.Require))
                    {
                        if (curOven.IsCavityEN(nPlaceCol) && !curOven.IsPressure(nPlaceCol) && !curOven.IsTransfer(nPlaceCol, nPlaceRow) &&
                            (CavityState.Rebaking == curOven.GetCavityState(nPlaceCol)) && curOven.GetPlt(nPlaceCol, nPlaceRow).IsType(PltType.Invalid))
                        {
                            // 取
                            Pick.SetAction(TransferRobotStation.OnloadStation, 0, nPickCol, ModuleEvent.OnloadPickRebakingFakePlt);
                            // 放
                            Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceRebakingFakePlt);
                            return true;
                        }
                    }
                }
            }

            // 上料区取满托盘 或 带假电池满托盘
            for (int nIndex = 0; nIndex < 2; nIndex++)
            {
                ModuleEvent pickEvent = (0 == nIndex) ? ModuleEvent.OnloadPickOKFullPallet : ModuleEvent.OnloadPickOKFakeFullPallet;
                ModuleEvent placeEvent = (0 == nIndex) ? ModuleEvent.OvenPlaceFullPlt : ModuleEvent.OvenPlaceFakeFullPlt;

                if (SearchOnloadPickPos(pickEvent, ref nPickRow, ref nPickCol))
                {
                    // 干燥炉放满托盘 或 带假电池满托盘
                    if (OvenGlobalSearch(false, placeEvent, ref nOvenID, ref nPlaceRow, ref nPlaceCol))
                    {
                        // 取
                        Pick.SetAction(TransferRobotStation.OnloadStation, 0, nPickCol, pickEvent);
                        // 放
                        Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, placeEvent);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算上料放料
        /// </summary>
        private bool CalcOnLoadPlace(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 上料区放待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
            if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceRebakingFakePlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 干燥炉取待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickRebakingPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickRebakingPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceRebakingFakePlt);
                    return true;
                }
            }

            // 上料区放NG非空托盘，转盘
            if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceNGPallet, ref nPlaceRow, ref nPlaceCol))
            {
                // 干燥炉取NG非空托盘
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickNGPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickNGPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceNGPallet);
                    return true;
                }
            }

            // 上料区放空托盘
            if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
            {
                // 下料区取空托盘
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }
                
                // 干燥炉取空托盘
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickEmptyPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }

                // 人工操作平台取空托盘
                if (SearchManualOperPlatPickPos(ModuleEvent.ManualOperatPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }

                // 缓存架取空托盘
                if (SearchPltBufPickPos(ModuleEvent.PltBufPickEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.PalletBuffer, nPickRow, 0, ModuleEvent.PltBufPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 计算下料取料
        /// </summary>
        private bool CalcOffLoadPick(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 下料区取空托盘
            if (SearchOffLoadPickPos(ModuleEvent.OffloadPickEmptyPlt, ref nPickRow, ref nPickCol))
            {
                // 上料区放空托盘
                if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }

                // 缓存架放空托盘
                if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceEmptyPlt);
                    return true;
                }

                // 干燥炉放空托盘（反向搜索）
                if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceEmptyPlt, ref nOvenID, ref nPlaceRow, ref nPlaceCol, true))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceEmptyPlt);
                    return true;
                }
            }

            // 下料区取等待水含量结果托盘（已取待测假电池的托盘）
            if (SearchOffLoadPickPos(ModuleEvent.OffloadPickDetectFakePlt, ref nPickRow, ref nPickCol))
            {
                Pallet curPlt = null;
                RunProDryingOven curOven = null;

                if (null != offloadRobot)
                {
                    curPlt = offloadRobot.Pallet[nPickCol];
                    nPlaceRow = curPlt.SrcRow;
                    nPlaceCol = curPlt.SrcCol;
                    nOvenID = curPlt.SrcStation - (int)TransferRobotStation.DryingOven_0;
                    curOven = GetOvenByID(nOvenID);
                }

                // 检查条件
                if (nOvenID > -1 && nPlaceRow > -1 && nPlaceCol > -1 && null != curOven && null != curPlt)
                {
                    if (CheckEvent(curOven, ModuleEvent.OvenPlaceWaitResultPlt, EventState.Require))
                    {
                        if (curOven.IsCavityEN(nPlaceCol) && !curOven.IsPressure(nPlaceCol) && !curOven.IsTransfer(nPlaceCol, nPlaceRow) &&
                            (CavityState.Detect == curOven.GetCavityState(nPlaceCol)) && curOven.GetPlt(nPlaceCol, nPlaceRow).IsType(PltType.Invalid))
                        {
                            // 取
                            Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickDetectFakePlt);
                            // 放
                            Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceWaitResultPlt);
                            return true;
                        }
                    }
                }
            }

            // 下料区取NG空托盘
            if (SearchOffLoadPickPos(ModuleEvent.OffloadPickNGEmptyPlt, ref nPickRow, ref nPickCol))
            {
                // 人工操作平台放NG空托盘
                if (SearchManualOperPlatPlacePos(ModuleEvent.ManualOperatPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickNGEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }

                // 缓存架放NG空托盘
                if (SearchPltBufPlacePos (ModuleEvent.PltBufPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickNGEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceNGEmptyPlt);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算下料放料
        /// </summary>
        private bool CalcOffLoadPlace(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 下料区放干燥完成托盘
            if (SearchOffLoadPlacePos(ModuleEvent.OffloadPlaceDryFinishedPlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 干燥炉取待下料托盘（干燥完成托盘）
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickOffloadPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickOffloadPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OffloadStation, 0, nPlaceCol, ModuleEvent.OffloadPlaceDryFinishedPlt);
                    return true;
                }
            }

            // 下料区放干燥完成托盘
            if (SearchOffLoadPlacePos(ModuleEvent.OffloadPlaceDryFinishedPlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 干燥炉取待下料托盘（干燥完成托盘）
                if (SearchCoolingPlacePos(ModuleEvent.CoolingPutAction, out nPickRow, out nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.CooligStove, nPickRow, nPickCol, ModuleEvent.CoolingPutAction);
                    // 放
                    Place.SetAction(TransferRobotStation.OffloadStation, 0, nPlaceCol, ModuleEvent.OffloadPlaceDryFinishedPlt);
                    return true;
                }
            }

            // 下料区放待检测含假电池托盘（未取走假电池的托盘）
            if (SearchOffLoadPlacePos(ModuleEvent.OffloadPlaceDetectFakePlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickDetectPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickDetectPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OffloadStation, 0, nPlaceCol, ModuleEvent.OffloadPlaceDetectFakePlt);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 重新计算放下料空托盘
        /// </summary>
        private bool ReCalcPlaceOffEmptyPlt(ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPlaceRow, nPlaceCol;
            nPlaceRow = nPlaceCol = -1;
            
            // 上料区放空托盘
            if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
            {
                // 放
                Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                return true;
            }

            // 缓存架放空托盘
            if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceEmptyPlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 放
                Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceEmptyPlt);
                return true;
            }

            // 干燥炉放空托盘（反向搜索）
            if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceEmptyPlt, ref nOvenID, ref nPlaceRow, ref nPlaceCol, true))
            {
                // 放
                Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceEmptyPlt);
                return true;
            }
            return false;
        }

        private bool SearchCoolingPickPos(ModuleEvent eEvent, out int nRow, out int nCol) => SearchCooling(eEvent, out nRow, out nCol, plt => plt.Type == PltType.Invalid);

        private bool SearchCoolingPlacePos(ModuleEvent eEvent, out int nRow, out int nCol) => SearchCooling(eEvent, out nRow, out nCol, plt => plt.Type == PltType.WaitOffload);


        private bool SearchCooling(ModuleEvent eEvent, out int nRow, out int nCol, Func<Pallet, bool> Condition)
        {
            nRow = nCol = -1;
            if (!CheckEvent(coolingStove, eEvent, EventState.Require) || Condition == null)
            {
                return false;
            }
            for (int row = 0; row < (int)RunProCoolingStove.ModuleDef.Pallet_MaxRow; row++)
            {
                    // 腔体是空位置 使能打开
                    if (Condition(coolingStove.Pallet[row]) &&
                        coolingStove.BBufEnable[row])
                    {
                        nRow = row;
                        nCol = 0;
                        return true;
                    }
            }
            return false;

        }


        /// <summary>
        /// 计算人工操作平台取料
        /// </summary>
        private bool CalcManualOperPlatPick(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 人工操作平台取空托盘
            if (SearchManualOperPlatPickPos(ModuleEvent.ManualOperatPickEmptyPlt, ref nPickRow, ref nPickCol))
            {
                // 上料区放空托盘
                if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                    return true;
                }

                // 缓存架放空托盘
                if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceEmptyPlt, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceEmptyPlt);
                    return true;
                }

                // 干燥炉放空托盘（反向搜索）
                if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceEmptyPlt, ref nOvenID, ref nPlaceRow, ref nPlaceCol, true))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPickEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceEmptyPlt);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算人工操作平台放料
        /// </summary>
        private bool CalcManualOperPlatPlace(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 人工操作平台放NG空托盘
            if (SearchManualOperPlatPlacePos(ModuleEvent.ManualOperatPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol))
            {
                // 下料区取NG空托盘
                if (SearchOffLoadPickPos(ModuleEvent.OffloadPickNGEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.OffloadStation, 0, nPickCol, ModuleEvent.OffloadPickNGEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }

                // 干燥炉取NG空托盘
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickNGEmptyPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickNGEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }

                // 干燥炉取NG非空托盘
                if (OvenGlobalSearch(true, ModuleEvent.OvenPickNGPlt, ref nOvenID, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickNGPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }

                // 缓存架取NG空托盘
                if (SearchPltBufPickPos(ModuleEvent.PltBufPickNGEmptyPlt, ref nPickRow, ref nPickCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.PalletBuffer, nPickRow, 0, ModuleEvent.PltBufPickNGEmptyPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算炉子取料(转移)
        /// </summary>
        private bool CalcOvenPickTransfer(ref ActionInfo Pick, ref ActionInfo Place)
        {
            int nPickOvenID = -1, nPlaceOvenID = -1;
            int nPickRow, nPickCol;
            int nPlaceRow, nPlaceCol;
            nPickRow = nPickCol = -1;
            nPlaceRow = nPlaceCol = -1;

            // 干燥炉取待转移托盘（正向搜索）
/*            if (OvenGlobalSearch(true, ModuleEvent.OvenPickTransferPlt, ref nPickOvenID, ref nPickRow, ref nPickCol))
            {
                // 干燥炉放满托盘（反向搜索）
                if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceFullPlt, ref nPlaceOvenID, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nPickOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickTransferPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.DryingOven_0 + nPlaceOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceFullPlt);
                    return true;
                }

                // 干燥炉放满假托盘（反向搜索）
                if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceFakeFullPlt, ref nPlaceOvenID, ref nPlaceRow, ref nPlaceCol))
                {
                    // 取
                    Pick.SetAction(TransferRobotStation.DryingOven_0 + nPickOvenID, nPickRow, nPickCol, ModuleEvent.OvenPickTransferPlt);
                    // 放
                    Place.SetAction(TransferRobotStation.DryingOven_0 + nPlaceOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceFakeFullPlt);
                    return true;
                }
            }*/
            return false;
        }
        #endregion


        #region // 全局搜索

        private bool OvenGlobalSearch(bool bIsPick, ModuleEvent eEvent, ref int nOvenID, ref int nRow, ref int nCol, bool bInverseSearch = false)
        {
            //初始化,去除外部影响
            nRow = -1;
            nCol = -1;

            RunProDryingOven pDryOven = null;
            int nWaitOffFloorCount = 0;
            for (int nOven = 0; nOven < arrDryingOven.Length; nOven++)
            {
                pDryOven = arrDryingOven[nOven];
                if (null != pDryOven)
                {
                    nWaitOffFloorCount = pDryOven.Pallet.
                        Where(plt => plt.Type == PltType.WaitRes || plt.Type == PltType.WaitOffload).Count();
                }
            }
            if (ModuleEvent.OvenPickDetectPlt == eEvent && nWaitOffFloorCount >= MachineCtrl.GetInstance().nMaxWaitOffFloorCount)
            {
                return false;
            }

            // 取料
            if (bIsPick)
            {
                // 匹配模式搜索
                for (MatchMode modeIdx = MatchMode.Pick_SameAndInvalid; modeIdx < MatchMode.Pick_End; modeIdx++)
                {
                    if (bInverseSearch)
                    {
                        // （反向）遍历每个干燥炉
                        for (int Col = 0; Col < (int)ModuleRowCol.DryingOvenRow; Col++)
                        {
                            for (int nOvenArrayIdx = (arrDryingOven.Length - 1); nOvenArrayIdx >= 0; nOvenArrayIdx--)
                            {
                                if (SearchOvenPickPos(modeIdx, eEvent, nOvenArrayIdx, Col, ref nRow))
                                {
                                    nCol = Col;
                                    nOvenID = arrDryingOven[nOvenArrayIdx].GetOvenID();
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）|| 干燥炉取待下料托盘（干燥完成托盘）
                        if (ModuleEvent.OvenPickDetectPlt == eEvent || ModuleEvent.OvenPickOffloadPlt == eEvent || ModuleEvent.OvenPickWaitCooling == eEvent)
                        {
                            int[] nIdx = GetFullPltOfOvenPriority(eEvent);

                            for (int i = 0; i < arrDryingOven.Length; i++)
                            {
                                nOvenID = nIdx[i];
                                if (SearchOvenPickPosEx(modeIdx, eEvent, nOvenID , ref nRow ,ref nCol))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            // （正向）遍历每个干燥炉
                            int[] nIdx = GetFullPltOfOvenPriority(eEvent);
                            int numOven = -1;
                            if (SequenceOvenPlacePos(true, modeIdx, eEvent, ref numOven, ref nRow, ref nCol))
                            {
                                nOvenID = arrDryingOven[numOven].GetOvenID();
                                return true;

                            }
                            for (int nOvenArrayIdx = 0; nOvenArrayIdx < nIdx.Length; nOvenArrayIdx++)
                            {
                                for (int Col = 0; Col < (int)ModuleRowCol.DryingOvenCol; Col++)
                                {
                                    if (SearchOvenPickPos(modeIdx, eEvent, nIdx[nOvenArrayIdx], Col, ref nRow))
                                    {
                                        nCol = Col;
                                        nOvenID = arrDryingOven[nIdx[nOvenArrayIdx]].GetOvenID();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // 放料
            else
            {
                // 匹配模式搜索
                for (MatchMode modeIdx = MatchMode.Place_SameAndInvalid; modeIdx < MatchMode.Place_End; modeIdx++)
                {
                    if (bInverseSearch)
                    {
                        int numOven = -1;
                        if (SequenceOvenPlacePos(false, modeIdx, eEvent, ref numOven, ref nRow, ref nCol))
                        {
                            nOvenID = arrDryingOven[numOven].GetOvenID();
                            return true;

                        }
                        // （反向）遍历每个干燥炉
                        /*for (int nOvenArrayIdx = (arrDryingOven.Length - 1); nOvenArrayIdx >= 0; nOvenArrayIdx--)
                        {
                            if (SearchOvenPlacePos(modeIdx, eEvent, nOvenArrayIdx, ref nRow, ref nCol))
                            {
                                nOvenID = arrDryingOven[nOvenArrayIdx].GetOvenID();
                                return true;
                            }
                        }*/
                    }
                    else
                    {
                        // 干燥炉放上料完成OK满托盘 || 干燥炉放上料完成OK带假电池满托盘
                        if (ModuleEvent.OvenPlaceFullPlt == eEvent || ModuleEvent.OvenPlaceFakeFullPlt == eEvent)
                        {
                            //int[] nIdx = new int[10] { 5, 9, 4, 8, 3, 7, 2, 6, 1, 0 };
                            int[] nIdx = GetFullPltOfOvenPriority(eEvent);
                            for (int nOvenArrayIdx = 0; nOvenArrayIdx < nIdx.Length; nOvenArrayIdx++)
                            {
                                if (SearchOvenPlacePos(modeIdx, eEvent, nIdx[nOvenArrayIdx], ref nRow, ref nCol))
                                {
                                    nOvenID = arrDryingOven[nIdx[nOvenArrayIdx]].GetOvenID();
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            // （正向）遍历每个干燥炉
                            for (int nOvenArrayIdx = 0; nOvenArrayIdx < arrDryingOven.Length; nOvenArrayIdx++)
                            {
                                if (SearchOvenPlacePos(modeIdx, eEvent, nOvenArrayIdx, ref nRow, ref nCol))
                                {
                                    nOvenID = arrDryingOven[nOvenArrayIdx].GetOvenID();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 炉腔放指定托盘的干燥炉优先队列
        /// </summary>
        private int[] GetFullPltOfOvenPriority(ModuleEvent moduleEvent)
        {
            bool isMAX = true;
            Func<Pallet, bool> fun = (pallet) => true;
            switch (moduleEvent)
            {
                case ModuleEvent.OvenPlaceFullPlt:
                case ModuleEvent.OvenPlaceFakeFullPlt:
                    {
                        fun = (pallet) => pallet.IsType(PltType.OK) && pallet.IsStage(PltStage.Onload) && pallet.IsFull();
                        break;
                    }
                case ModuleEvent.OvenPickWaitCooling:
                    {
                        fun = (pallet) => (pallet.IsType(PltType.WaitCooling)) && pallet.IsStage(PltStage.Offload) && pallet.IsFull();
                        isMAX = false;
                        break;
                    }
                case ModuleEvent.OvenPickOffloadPlt:
                    {
                        fun = (pallet) => (pallet.IsType(PltType.WaitOffload)) && pallet.IsStage(PltStage.Offload) && pallet.IsFull();
                        isMAX = false;
                        break;
                    }
                case ModuleEvent.OvenPickDetectPlt:
                    {
                        fun = (pallet) => (pallet.IsType(PltType.Detect)) && pallet.IsStage(PltStage.Onload) && pallet.IsFull();
                        break;
                    }

            }

            int[] pri = new int[6] { 5, 4, 3, 2, 1, 0 };


            Dictionary<int,int> ovenAndPump = new Dictionary<int, int>() {{0, 1},{1, 0},{2, 3},{3, 2},{4, 5},{5, 4}};

            //获取每个干燥炉已放满托盘数量
            List<int> FullPltCounts = arrDryingOven.Select(Oven =>
            {
                int s = 0;
                Oven.CavityDataSource.ForEach((A, Index) =>
                {
                    if (IsUse(Oven, Index))
                        s += A.Plts.Count(pallet => fun(pallet));
                });
                return s;
            }).ToList();

            //上满电托盘和上假电托盘单独加共用真空泵影响计算
            if (ModuleEvent.OvenPlaceFullPlt == moduleEvent || ModuleEvent.OvenPlaceFakeFullPlt == moduleEvent)
            {
                //当共用真空泵的炉子中，有正在烘烤的炉子，则降低使用同一台真空泵的炉子放料优先级（禁用只降低自身炉子优先级）
                for (int i = 0; i < FullPltCounts.Count; i++)
                {
                    var cavitys = arrDryingOven[i].CavityDataSource;

                    int workCount = cavitys.Count(b => b.State == CavityState.Work);                                  //计算正在烘烤炉腔并赋予对应权重
                    FullPltCounts[ovenAndPump[i]] -= workCount;                                                       //对使用相同真空泵的炉子减去对应权重
                    int WaitCount = cavitys.Count(A => A.Plts.Any(pallet=> pallet.IsType(PltType.WaitOffload) || pallet.IsType(PltType.WaitCooling)));              //累加待下料，待冷却权重
                    if(WaitCount>0 && WaitCount< (int)ModuleRowCol.DryingOvenRow)
                        WaitCount -= cavitys.Count(A => A.Plts.All(pallet => pallet.IsType(PltType.Invalid)));
                    FullPltCounts[i] -= (workCount+ WaitCount);                                                                    //减去对应权重
                }
            }
                

            int OvenID = 0;
            //根据满托盘数量调整炉子优先上托盘顺序
            for (int i = 0; i < pri.Length; i++)
            {
                if (isMAX)
                {
                    OvenID = FullPltCounts.IndexOf(FullPltCounts.Max());
                    pri[i] = OvenID;
                    FullPltCounts[OvenID] =-(i + 66);                         //通过把数量改成负数排除被max重复选中
                }
                else 
                {
                    FullPltCounts = FullPltCounts.Select(A=>A==0 ? 66:A).ToList();
                    OvenID = FullPltCounts.IndexOf(FullPltCounts.Min());
                    pri[i] = OvenID;
                    FullPltCounts[OvenID] = (i + 66);                         //通过把数量加大排除被Min重复选中
                }
            }
            return pri;
        }


        /// <summary>
        /// 判断炉腔是否可放满托盘
        /// </summary>
        private bool IsUse(RunProDryingOven Oven,int nCol)
        {
            if (Oven.IsCavityEN(nCol) && !Oven.IsPressure(nCol)
                        && (CavityState.Work != Oven.GetCavityState(nCol))
                        && CavityState.Standby == Oven.GetCavityState(nCol))
                return true;
            return false;
        }

        #endregion


        #region // 模组搜索

        // ================================ 模组搜索 ================================

        /// <summary>
        /// 搜索上料区取料位置
        /// </summary>
        private bool SearchOnloadPickPos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(onloadRobot, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 上料区取回炉假电池托盘（已放回假电池的托盘）
                case ModuleEvent.OnloadPickRebakingFakePlt:
                    {
                        for (int nPltIdx = ((int)ModuleMaxPallet.OnloadRobot - 1); nPltIdx >= 0; nPltIdx--)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.WaitRebakingToOven) && !PltIsEmpty(onloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 上料区取NG空夹具
                case ModuleEvent.OnloadPickNGEmptyPallet:
                    {
                        for (int nPltIdx = ((int)ModuleMaxPallet.OnloadRobot - 1); nPltIdx >= 0; nPltIdx--)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.NG) && PltIsEmpty(onloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 上料区取OK满夹具
                case ModuleEvent.OnloadPickOKFullPallet:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OnloadRobot; nPltIdx++)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.OK) && !PltHasTypeBat(onloadRobot.Pallet[nPltIdx], BatType.Fake)
                                && PltIsFull(onloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 上料区取OK带假电池满夹具
                case ModuleEvent.OnloadPickOKFakeFullPallet:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OnloadRobot; nPltIdx++)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.OK) && PltHasTypeBat(onloadRobot.Pallet[nPltIdx], BatType.Fake)
                                && PltIsFull(onloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索上料区放料位置
        /// </summary>
        private bool SearchOnloadPlacePos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(onloadRobot, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 上料区放空夹具
                case ModuleEvent.OnloadPlaceEmptyPallet:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OnloadRobot; nPltIdx++)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 上料区放NG非空夹具，转盘
                case ModuleEvent.OnloadPlaceNGPallet:
                    {
                        if (onloadRobot.Pallet[(int)ModuleMaxPallet.OnloadRobot - 1].IsType(PltType.Invalid))
                        {
                            nRow = 0;
                            nCol = ((int)ModuleMaxPallet.OnloadRobot - 1);
                            return true;
                        }
                        break;
                    }
                // 上料区放待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                case ModuleEvent.OnloadPlaceRebakingFakePlt:
                    {
                        for (int nPltIdx = ((int)ModuleMaxPallet.OnloadRobot - 1); nPltIdx >= 0; nPltIdx--)
                        {
                            if (onloadRobot.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索下料区取料位置
        /// </summary>
        private bool SearchOffLoadPickPos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(offloadRobot, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 下料区取空夹具
                case ModuleEvent.OffloadPickEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OffloadRobot; nPltIdx++)
                        {
                            if (offloadRobot.Pallet[nPltIdx].IsType(PltType.OK) && PltIsEmpty(offloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
                case ModuleEvent.OffloadPickDetectFakePlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OffloadRobot; nPltIdx++)
                        {
                            if (offloadRobot.Pallet[nPltIdx].IsType(PltType.WaitRes) && !PltIsEmpty(offloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 下料区取NG空夹具
                case ModuleEvent.OffloadPickNGEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.OffloadRobot; nPltIdx++)
                        {
                            if (offloadRobot.Pallet[nPltIdx].IsType(PltType.NG) && PltIsEmpty(offloadRobot.Pallet[nPltIdx]))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索下料区放料位置
        /// </summary>
        private bool SearchOffLoadPlacePos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(offloadRobot, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 下料区放干燥完成夹具
                case ModuleEvent.OffloadPlaceDryFinishedPlt:
                    {
                        for (int nPltIdx = (int)ModuleMaxPallet.OffloadRobot - 1; nPltIdx > -1; nPltIdx--)
                        {
                            if (offloadRobot.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
                // 下料区放待检测含假电池夹具（未取走假电池的夹具）
                case ModuleEvent.OffloadPlaceDetectFakePlt:
                    {
                        for (int nPltIdx = 1; nPltIdx > -1; nPltIdx--)
                        {
                            if (offloadRobot.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = 0;
                                nCol = nPltIdx;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索人工操作平台取料位置
        /// </summary>
        private bool SearchManualOperPlatPickPos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(manualOperat, eEvent, EventState.Require))
            {
                return false;
            }

            // 人工操作平台取空托盘
            if (ModuleEvent.ManualOperatPickEmptyPlt == eEvent)
            {
                if (manualOperat.Pallet[0].IsType(PltType.OK) && PltIsEmpty(manualOperat.Pallet[0]))
                {
                    nRow = 0;
                    nCol = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索人工操作平台放料位置
        /// </summary>
        private bool SearchManualOperPlatPlacePos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(manualOperat, eEvent, EventState.Require))
            {
                return false;
            }

            // 人工操作平台取空托盘
            if (ModuleEvent.ManualOperatPlaceNGEmptyPlt == eEvent)
            {
                if (manualOperat.Pallet[0].IsType(PltType.Invalid))
                {
                    nRow = 0;
                    nCol = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索缓存架取料位置
        /// </summary>
        private bool SearchPltBufPickPos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(palletBuf, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 缓存架取空托盘
                case ModuleEvent.PltBufPickEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.PalletBuf; nPltIdx++)
                        {
                            if (palletBuf.IsPltBufEN(nPltIdx) && palletBuf.Pallet[nPltIdx].IsType(PltType.OK) && PltIsEmpty(palletBuf.Pallet[nPltIdx]))
                            {
                                nRow = nPltIdx;
                                nCol = 0;
                                return true;
                            }
                        }
                        break;
                    }
                // 缓存架取NG空托盘
                case ModuleEvent.PltBufPickNGEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.PalletBuf; nPltIdx++)
                        {
                            if (palletBuf.IsPltBufEN(nPltIdx) && palletBuf.Pallet[nPltIdx].IsType(PltType.NG) && PltIsEmpty(palletBuf.Pallet[nPltIdx]))
                            {
                                nRow = nPltIdx;
                                nCol = 0;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }

        /// <summary>
        /// 搜索缓存架放料位置
        /// </summary>
        private bool SearchPltBufPlacePos(ModuleEvent eEvent, ref int nRow, ref int nCol)
        {
            // 信号检查
            if (!CheckEvent(palletBuf, eEvent, EventState.Require))
            {
                return false;
            }

            switch (eEvent)
            {
                // 缓存架放空托盘
                case ModuleEvent.PltBufPlaceEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.PalletBuf; nPltIdx++)
                        {
                            if (palletBuf.IsPltBufEN(nPltIdx) && palletBuf.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = nPltIdx;
                                nCol = 0;
                                return true;
                            }
                        }
                        break;
                    }
                // 缓存架放NG空托盘
                case ModuleEvent.PltBufPlaceNGEmptyPlt:
                    {
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleMaxPallet.PalletBuf; nPltIdx++)
                        {
                            if (palletBuf.IsPltBufEN(nPltIdx) && palletBuf.Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                nRow = nPltIdx;
                                nCol = 0;
                                return true;
                            }
                        }
                        break;
                    }
            }
            return false;
        }
        /// <summary>
        /// 搜索干燥炉取干燥完成位置
        /// </summary>
        private bool DryingOvenStartTimeSort(ref int[] pSortArray)
        {
            if (null != pSortArray)
            {
                int nTmp = 0;
                DateTime dwSStartTime, dwDStartTime, dwTempTime;
                dwSStartTime = dwDStartTime = dwTempTime = new DateTime();
                int nDOvenID, nDFloor, nSOvenID, nSFloor;
                nDOvenID = nDFloor = nSOvenID = nSFloor = -1;

                RunProDryingOven run = null;

                for (int i = 0; i < arrDryingOven.Length * (int)ModuleRowCol.DryingOvenCol; i++)
                {
                    pSortArray[i] = i;
                }

                for (int i = 0; i < arrDryingOven.Length * (int)ModuleRowCol.DryingOvenCol; i++)
                {
                    for (int j = i; j < arrDryingOven.Length * (int)ModuleRowCol.DryingOvenCol; j++)
                    {
                        nSOvenID = pSortArray[i] / (int)ModuleRowCol.DryingOvenCol;
                        nSFloor = pSortArray[i] % (int)ModuleRowCol.DryingOvenCol;
                        run = GetOvenByID(nSOvenID);
                        dwSStartTime = run.GetStartTime(nSFloor);

                        nDOvenID = pSortArray[j] / (int)ModuleRowCol.DryingOvenCol;
                        nDFloor = pSortArray[j] % (int)ModuleRowCol.DryingOvenCol;
                        run = GetOvenByID(nDOvenID);
                        dwDStartTime = run.GetStartTime(nDFloor);

                        if (dwSStartTime > dwDStartTime && dwDStartTime != dwTempTime)
                        {
                            nTmp = pSortArray[i];
                            pSortArray[i] = pSortArray[j];
                            pSortArray[j] = nTmp;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 搜索干燥炉取料位置
        /// </summary>
        private bool SearchOvenPickPos(MatchMode curMatchMode, ModuleEvent eEvent, int nOvenArrayIdx, int nCol , ref int nRowIdx)
        {
            Pallet curPlt = null;
            RunProDryingOven curOven = arrDryingOven[nOvenArrayIdx];

            // 信号检查
            if (!CheckEvent(arrDryingOven[nOvenArrayIdx], eEvent, EventState.Require) || !curOven.IsModuleEnable())
            {
                return false;
            }

            int i = -1;
            var tempList = curOven.pallet.Skip(nCol * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
            switch (curMatchMode)
            {
                // 同类型 && 无效
                case MatchMode.Pick_SameAndInvalid:
                    {
                        if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                            && (CavityState.Work != curOven.GetCavityState(nCol))
                            && (CavityState.Maintenance != curOven.GetCavityState(nCol))
                            && CavityState.Standby == curOven.GetCavityState(nCol))
                        {
                            switch (eEvent)
                            {
                                // 干燥炉取空托盘
                                case ModuleEvent.OvenPickEmptyPlt:
                                    {
                                        if (!tempList.Any(A => A.IsType(PltType.OK) && PltIsEmpty(A)))
                                            break;
                                        tempList.ForEach((Pallet A,int Index) =>
                                            {
                                                if (A.IsType(PltType.OK) && A.IsStage(PltStage.Invalid) && PltIsEmpty(A))
                                                        i = Index;
                                            });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取NG非空托盘 和 NG空托盘
                                case ModuleEvent.OvenPickNGPlt:
                                case ModuleEvent.OvenPickNGEmptyPlt:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.NG) && ((ModuleEvent.OvenPickNGPlt == eEvent) ? !PltIsEmpty(A) : PltIsEmpty(A)))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取待下料托盘（干燥完成托盘）
                                case ModuleEvent.OvenPickOffloadPlt:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.WaitOffload) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取待冷却托盘（干燥完成托盘）
                                case ModuleEvent.OvenPickWaitCooling:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.WaitCooling) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                // 同类型 && !同类型
                case MatchMode.Pick_SameAndNotSame:
                    {
                        if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                            && (CavityState.Work != curOven.GetCavityState(nCol))
                            && (CavityState.Maintenance != curOven.GetCavityState(nCol))
                            && CavityState.Standby == curOven.GetCavityState(nCol))
                        {
                            switch (eEvent)
                            {
                                // 干燥炉取空托盘
                                case ModuleEvent.OvenPickEmptyPlt:
                                    {

                                        if (!tempList.Any(A => A.IsType(PltType.OK) && PltIsEmpty(A)))
                                            break;
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.OK) && A.IsStage(PltStage.Invalid) && !PltIsEmpty(A))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取NG非空托盘 和 NG空托盘
                                case ModuleEvent.OvenPickNGPlt:
                                case ModuleEvent.OvenPickNGEmptyPlt:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.NG) && ((ModuleEvent.OvenPickNGPlt == eEvent) ? !PltIsEmpty(A) : PltIsEmpty(A)))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取待下料托盘（干燥完成托盘）
                                case ModuleEvent.OvenPickOffloadPlt:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.WaitOffload) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                                // 干燥炉取待冷却托盘（干燥完成托盘）
                                case ModuleEvent.OvenPickWaitCooling:
                                    {
                                        tempList.ForEach((Pallet A, int Index) =>
                                        {
                                            if (A.IsType(PltType.WaitCooling) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                i = Index;
                                        });
                                        nRowIdx = i;
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                // 同类型 && 其他
                case MatchMode.Pick_SameAndOther:
                    {
                        if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                                && (CavityState.Work != curOven.GetCavityState(nCol))
                                && (CavityState.Maintenance != curOven.GetCavityState(nCol)))
                        {
                            for (int RowIdx = 0; RowIdx < (int)ModuleRowCol.DryingOvenCol; RowIdx++)
                            {
                                curPlt = curOven.pallet.Skip(nCol * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray()[RowIdx];
                                switch (eEvent)
                                {
                                    // 干燥炉取空托盘
                                    case ModuleEvent.OvenPickEmptyPlt:
                                        {
                                            if (CavityState.Standby == curOven.GetCavityState(nCol) /*&& !curOven.IsTransfer(nRowIdx)*/ && curPlt.IsType(PltType.OK) &&
                                                curPlt.IsStage(PltStage.Invalid) && PltIsEmpty(curPlt))
                                            {
                                                nRowIdx = RowIdx;
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉取NG非空托盘 和 NG空托盘
                                    case ModuleEvent.OvenPickNGPlt:
                                    case ModuleEvent.OvenPickNGEmptyPlt:
                                        {
                                            if (CavityState.Standby == curOven.GetCavityState(nCol)/* && !curOven.IsTransfer(nRowIdx)*/ && curPlt.IsType(PltType.NG)
                                                && ((ModuleEvent.OvenPickNGPlt == eEvent) ? !PltIsEmpty(curPlt) : PltIsEmpty(curPlt)))
                                            {
                                                nRowIdx = RowIdx;
                                                return true;
                                            }
                                            break;
                                        }
                                    // 干燥炉取待下料托盘（干燥完成托盘）
                                    case ModuleEvent.OvenPickOffloadPlt:
                                        {
                                            if ((CavityState.Standby == curOven.GetCavityState(nCol)) && curPlt.IsType(PltType.WaitOffload) && curPlt.IsStage(PltStage.Baking) &&
                                                !PltIsEmpty(curPlt))
                                            {
                                                nRowIdx = RowIdx;
                                                return true;
                                            }
                                            break;
                                        }
                                    case ModuleEvent.OvenPickWaitCooling:
                                        {
                                            tempList.ForEach((Pallet A, int Index) =>
                                            {
                                                if (A.IsType(PltType.WaitCooling) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                    i = Index;
                                            });
                                            nRowIdx = i;
                                            break;
                                        }
                                }
                            }
                        }
                        break;
                    }
            }

            if (nRowIdx != -1)
                return true;

            // 开始搜索
            if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                    && (CavityState.Work != curOven.GetCavityState(nCol))
                    && (CavityState.Maintenance != curOven.GetCavityState(nCol)))
            {
                for (int RowIdx = 0; RowIdx < (int)ModuleRowCol.DryingOvenRow; RowIdx++)
                {
                    curPlt = tempList[RowIdx];
                    switch (eEvent)
                    {
                        // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）
                        case ModuleEvent.OvenPickDetectPlt:
                            {
                                if (CavityState.Detect == curOven.GetCavityState(nCol) /*&& !curOven.IsTransfer(nRowIdx)*/ && curPlt.IsType(PltType.Detect)
                                    && curPlt.IsStage(PltStage.Onload) && PltHasTypeBat(curPlt, BatType.Fake) && !PltIsEmpty(curPlt))
                                {
                                    nRowIdx = RowIdx;
                                    return true;
                                }
                                break;
                            }
                        // 干燥炉取待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                        case ModuleEvent.OvenPickRebakingPlt:
                            {
                                if (CavityState.Rebaking == curOven.GetCavityState(nCol) /*&& !curOven.IsTransfer(nRowIdx)*/ && curPlt.IsType(PltType.WaitRebakeBat)
                                    && curPlt.IsStage(PltStage.Onload) && PltHasTypeBat(curPlt, BatType.Fake) && !PltIsEmpty(curPlt))
                                {
                                    nRowIdx = RowIdx;
                                    return true;
                                }
                                break;
                            }
                        case ModuleEvent.OvenPickWaitCooling:
                            {
                                if (CavityState.Standby == curOven.GetCavityState(nCol) && /*!curOven.IsTransfer(nCol) &&*/ curPlt.IsType(PltType.WaitCooling) &&
                                curPlt.IsStage(PltStage.Baking) && !PltIsEmpty(curPlt))
                                {
                                    nRowIdx = RowIdx;
                                    return true;
                                }
                                break;
                            }
                            // 干燥炉取待转移托盘（真空失败）
                            //case ModuleEvent.OvenPickTransferPlt:
                            //    {
                            //        if (CavityState.Standby == curOven.GetCavityState(nCol) /*&& curOven.IsTransfer(nRowIdx)*/ && curPlt.IsType(PltType.OK)
                            //            && curPlt.IsStage(PltStage.Onload) && !PltIsEmpty(curPlt))
                            //        {
                            //            if (tempList.All(A => A.IsType(PltType.OK) && A.IsStage(PltStage.Onload)))
                            //            {
                            //                nRowIdx = RowIdx;
                            //                return true;
                            //            }
                            //        }
                            //        break;
                            //    }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 搜索干燥炉取料位置Ex
        /// </summary>
        private bool SearchOvenPickPosEx(MatchMode curMatchMode, ModuleEvent eEvent, int nOvenArrayIdx, ref int nRowIdx,ref int nColIdx)
        {
            Pallet curPlt = null;
            RunProDryingOven curOven = arrDryingOven[nOvenArrayIdx];

            // 信号检查
            if (!CheckEvent(arrDryingOven[nOvenArrayIdx], eEvent, EventState.Require))
            {
                return false;
            }

            int i = -1;

            for (int nCol = 0; nCol < (int)ModuleRowCol.DryingOvenCol; nCol++)
            {
                var tempList = curOven.pallet.Skip(nCol * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
                switch (curMatchMode)
                {
                    // 同类型 && 无效
                    case MatchMode.Pick_SameAndInvalid:
                        {
                            if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                                && (CavityState.Work != curOven.GetCavityState(nCol))
                                && (CavityState.Maintenance != curOven.GetCavityState(nCol))
                                && CavityState.Standby == curOven.GetCavityState(nCol))
                            {
                                switch (eEvent)
                                {
                                    // 干燥炉取待下料托盘（干燥完成托盘）
                                    case ModuleEvent.OvenPickOffloadPlt:
                                        {
                                            tempList.ForEach((Pallet A, int Index) =>
                                            {
                                                if (A.IsType(PltType.WaitOffload) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                    i = Index;
                                            });
                                            nRowIdx = i;
                                            nColIdx = nCol;
                                            break;
                                        }
                                    // 干燥炉取待冷却托盘（干燥完成托盘）
                                    case ModuleEvent.OvenPickWaitCooling:
                                        {
                                            tempList.ForEach((Pallet A, int Index) =>
                                            {
                                                if (A.IsType(PltType.WaitCooling) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                    i = Index;
                                            });
                                            nRowIdx = i;
                                            nColIdx = nCol;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    // 同类型 && !同类型
                    case MatchMode.Pick_SameAndNotSame:
                        {
                            if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                                && (CavityState.Work != curOven.GetCavityState(nCol))
                                && (CavityState.Maintenance != curOven.GetCavityState(nCol))
                                && CavityState.Standby == curOven.GetCavityState(nCol))
                            {
                                switch (eEvent)
                                {
                                    // 干燥炉取待下料托盘（干燥完成托盘）
                                    case ModuleEvent.OvenPickOffloadPlt:
                                        {
                                            tempList.ForEach((Pallet A, int Index) =>
                                            {
                                                if (A.IsType(PltType.WaitOffload) && A.IsStage(PltStage.Baking) && !PltIsEmpty(A))
                                                    i = Index;
                                            });
                                            nRowIdx = i;
                                            nColIdx = nCol;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    // 同类型 && 其他
                    case MatchMode.Pick_SameAndOther:
                        {
                            if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                                    && (CavityState.Work != curOven.GetCavityState(nCol))
                                    && (CavityState.Maintenance != curOven.GetCavityState(nCol)))
                            {
                                for (int RowIdx = 0; RowIdx < (int)ModuleRowCol.DryingOvenRow; RowIdx++)
                                {
                                    curPlt = tempList[RowIdx];

                                    if (null != curPlt)
                                    {
                                        switch (eEvent)
                                        {
                                            // 干燥炉取待下料托盘（干燥完成托盘）
                                            case ModuleEvent.OvenPickOffloadPlt:
                                                {
                                                    if ((CavityState.Standby == curOven.GetCavityState(nRowIdx)) && curPlt.IsType(PltType.WaitOffload) && curPlt.IsStage(PltStage.Baking) &&
                                                        !PltIsEmpty(curPlt))
                                                    {
                                                        nRowIdx = RowIdx;
                                                        nColIdx = nCol;
                                                        return true;
                                                    }
                                                    break;
                                                }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
                if (nRowIdx != -1)
                    return true;

                // 开始搜索
                if (curOven.IsCavityEN(nCol) && !curOven.IsPressure(nCol)
                        && (CavityState.Work != curOven.GetCavityState(nCol))
                        && (CavityState.Maintenance != curOven.GetCavityState(nCol)))
                {
                    for (int RowIdx = 0; RowIdx < (int)ModuleRowCol.DryingOvenRow; RowIdx++)
                    {
                        curPlt = tempList[RowIdx];
                        if (null != curPlt)
                        {
                            switch (eEvent)
                            {
                                // 干燥炉取待检测含假电池托盘（未取走假电池的托盘）
                                case ModuleEvent.OvenPickDetectPlt:
                                    {
                                        if (CavityState.Detect == curOven.GetCavityState(nCol) && /*!curOven.IsTransfer(nRowIdx) &&*/ curPlt.IsType(PltType.Detect)
                                            && curPlt.IsStage(PltStage.Onload) && PltHasTypeBat(curPlt, BatType.Fake) && !PltIsEmpty(curPlt))
                                        {
                                            nRowIdx = RowIdx;
                                            nColIdx = nCol;
                                            return true;
                                        }
                                        break;
                                    }
                                // 干燥炉取待回炉含假电池托盘（已取走假电池，待重新放回假电池的托盘）
                                case ModuleEvent.OvenPickRebakingPlt:
                                    {
                                        if (CavityState.Rebaking == curOven.GetCavityState(nCol) && /*!curOven.IsTransfer(nRowIdx) &&*/ curPlt.IsType(PltType.WaitRebakeBat)
                                            && curPlt.IsStage(PltStage.Onload) && PltHasTypeBat(curPlt, BatType.Fake) && !PltIsEmpty(curPlt))
                                        {
                                            nRowIdx = RowIdx;
                                            nColIdx = nCol;
                                            return true;
                                        }
                                        break;
                                    }
                                    // 干燥炉取待转移托盘（真空失败）
                                    /* case ModuleEvent.OvenPickTransferPlt:
                                         {
                                             if (CavityState.Standby == curOven.GetCavityState(nCol) && *//*curOven.IsTransfer(nRowIdx) &&*//* curPlt.IsType(PltType.OK)
                                                 && curPlt.IsStage(PltStage.Onload) && !PltIsEmpty(curPlt))
                                             {
                                                 nRowIdx = RowIdx;
                                                 return true;
                                             }
                                             break;
                                         }*/
                            }
                        }

                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 搜索干燥炉放料位置
        /// </summary>
        private bool SearchOvenPlacePos(MatchMode curMatchMode, ModuleEvent eEvent, int nOvenArrayIdx, ref int nRow, ref int nCol)
        {
            Pallet[] rowPlt = new Pallet[2] { null, null };
            nCol = 0;
            nRow = -1;
            RunProDryingOven curOven = arrDryingOven[nOvenArrayIdx];

            // 信号检查
            if (!CheckEvent(curOven, eEvent, EventState.Require))
            {
                return false;
            }

            for (int Col = 0; Col < (int)ModuleRowCol.DryingOvenCol; Col++)
            {
                int i = -1;
                var tempList = curOven.pallet.Skip(Col * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();

                switch (curMatchMode)
                {
                    // 同类型 && 无效
                    case MatchMode.Place_SameAndInvalid:
                        {
                            {
                                if (curOven.IsCavityEN(Col) && !curOven.IsPressure(Col) /*&& !curOven.IsTransfer(nRowIdx)*/ &&
                                    (CavityState.Standby == curOven.GetCavityState(Col)))
                                {
                                    switch (eEvent)
                                    {
                                        // 干燥炉放NG非空托盘 和 NG空托盘
                                        case ModuleEvent.OvenPlaceEmptyPlt:
                                        case ModuleEvent.OvenPlaceNGEmptyPlt:
                                            {
                                                // 放NG空托盘时，忽略配对托盘是否为空，只要求托盘有NG属性
                                                bool bIgnoreCheck = (ModuleEvent.OvenPlaceEmptyPlt == eEvent) ? true : false;
                                                //PltType pltType = (ModuleEvent.OvenPlaceEmptyPlt == eEvent) ? PltType.OK : PltType.NG;
                                                PltType pltType = (ModuleEvent.OvenPlaceEmptyPlt == eEvent) ? PltType.Invalid : PltType.NG;

                                                if (tempList.Any(A => A.IsType(PltType.OK) && A.IsStage(PltStage.Onload)))
                                                    break;

                                                tempList.ForEach((Pallet A, int Index) =>
                                                {
                                                    if ((A.IsType(PltType.Invalid) && A.IsType(pltType) && A.IsStage(PltStage.Invalid) &&
                                                    (bIgnoreCheck || PltIsEmpty(A))))
                                                        i = Index;
                                                });
                                                nCol = Col;
                                                nRow = i;

                                                break;
                                            }
                                        // 干燥炉放上料满托盘 和 带假电池满托盘
                                        case ModuleEvent.OvenPlaceFullPlt:
                                        case ModuleEvent.OvenPlaceFakeFullPlt:
                                            {
                                                bool bHasFakeBat = (ModuleEvent.OvenPlaceFullPlt == eEvent) ? false : true;

                                                bool isFull = tempList.All(A => !A.IsType(PltType.Invalid));                            //没有空位
                                                bool isNoFake = tempList.Count(A => PltHasTypeBat(A, BatType.OK)) >= (int)ModuleRowCol.DryingOvenRow-1 && !tempList.Any(A => PltHasTypeBat(A, BatType.Fake));    //炉腔快满还没有假电池
                                                bool isHasFake = tempList.Any(A => PltHasTypeBat(A, BatType.Fake));                     //有假电池了

                                                //1.没有空位 2.上满电池托盘时炉腔内没有假电池托盘 3上假电池托盘时炉腔内已有假电池托盘    以上三种情况直接跳过
                                                if (isFull || !bHasFakeBat && isNoFake || bHasFakeBat && isHasFake)
                                                    break;

                                                //优先上没有待下料与待冷却托盘的炉腔
                                                if (tempList.Any(A => A.IsType(PltType.WaitOffload) || A.IsType(PltType.WaitCooling)) && Col == 0)
                                                {
                                                    if (IsUse(curOven,1) && curOven.CavityDataSource[1].Plts.Any(A => A.IsType(PltType.Invalid)))
                                                        break;
                                                }

                                                //处理同干燥炉不同炉腔上满托盘优先逻辑
                                                var fullPallteCount = curOven.CavityDataSource.Select((cavity,Index) => IsUse(curOven,Index) ? cavity.Plts.Count(pallet => pallet.IsType(PltType.OK)) : 0).ToList();
                                                //if (fullPallteCount[Col] != fullPallteCount.Max())
                                                //    break; //两列中有一列满托盘会不放托盘暂时屏蔽duanyh2024-1108

                                                //找空位放托盘
                                                tempList.ForEach((Pallet A, int Index) =>
                                                {
                                                    if (A.IsType(PltType.Invalid))
                                                        i = Index;
                                                });
                                                nCol = Col;
                                                nRow = i;
                                                break;
                                            }
                                        case ModuleEvent.OvenPlaceRebakingFakePlt:
                                            {
                                                if (tempList.Any(A => A.IsType(PltType.Invalid)) || tempList.Select(A => A.IsType(PltType.OK) && A.IsStage(PltStage.Onload)).Count()!=(int)ModuleRowCol.DryingOvenCol-1)
                                                    break;
                                                tempList.ForEach((Pallet A, int Index) =>
                                                {
                                                    if (A.IsType(PltType.Invalid))
                                                        i = Index;
                                                });
                                                nCol = Col;
                                                nRow = i;
                                                break;
                                            }
                                    }
                                }
                            }
                            break;
                        }
                    // 无效 && 无效
                    case MatchMode.Place_InvalidAndInvalid:
                        {
                                if (curOven.IsCavityEN(Col) && !curOven.IsPressure(Col) /*&& !curOven.IsTransfer(nRowIdx)*/ &&
                                    (CavityState.Standby == curOven.GetCavityState(Col)) && tempList.All(A=>A.IsType(PltType.Invalid)))
                                {
                                    tempList.ForEach((Pallet A, int Index) =>
                                    {
                                        if (A.IsType(PltType.Invalid))
                                            i = Index;
                                    });
                                    nRow = i;
                                    nCol = Col;
                                }
                            break;
                        }
                    // 无效 && 其他
                    case MatchMode.Place_InvalidAndOther:
                        {
                            break;
                        }
                }

                if (nRow != -1)
                    return true;
            }

            return false;
        }

        ///<summary>
        /// 排序干燥炉放料位置
        ///</summary>
        private bool SequenceOvenPlacePos(bool bIsPick, MatchMode curMatchMode, ModuleEvent eEvent,ref int nOvenArrayNum, ref int nRow, ref int nCol)
        {
            Pallet[] rowPlt = new Pallet[2] { null, null };
            nCol = 0;
            nRow = -1;
            List<int[]> list = new List<int[]>();


            // （反向）遍历每个干燥炉
            for (int nOvenArrayIdx = (arrDryingOven.Length - 1); nOvenArrayIdx >= 0; nOvenArrayIdx--)
            {
                RunProDryingOven curOven = arrDryingOven[nOvenArrayIdx];
                // 信号检查
                if (bIsPick)
                {
                    // 信号检查
                    if (!CheckEvent(arrDryingOven[nOvenArrayIdx], eEvent, EventState.Require) || !curOven.IsModuleEnable())
                    {
                        continue;
                    }
                }
                else
                {
                    if (!CheckEvent(curOven, eEvent, EventState.Require))
                    {
                        continue;
                        //return false;
                    }
                }
                for (int Col = 0; Col < (int)ModuleRowCol.DryingOvenCol; Col++)
                {
                    switch (curMatchMode)
                    {
                        // 同类型 && 无效
                        case MatchMode.Place_SameAndInvalid:
                        case MatchMode.Pick_SameAndInvalid:
                        case MatchMode.Pick_SameAndNotSame:
                        case MatchMode.Pick_SameAndOther:
                            {
                                if (curOven.IsCavityEN(Col) && !curOven.IsPressure(Col) /*&& !curOven.IsTransfer(nRowIdx)*/ &&
                                    (CavityState.Standby == curOven.GetCavityState(Col)))
                                {
                                    switch (eEvent)
                                    {
                                        // 干燥炉放NG非空托盘 和 NG空托盘
                                        case ModuleEvent.OvenPlaceEmptyPlt:
                                        case ModuleEvent.OvenPlaceNGEmptyPlt:
                                        case ModuleEvent.OvenPickEmptyPlt:
                                            {
                                                if (curOven.GetCavityState(Col) != CavityState.Standby || !curOven.IsCavityEN(Col))
                                                {
                                                    continue;
                                                }
                                                int i = -1;
                                                var tempList = curOven.pallet.Skip(Col * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();

                                                int numberInvalid = 0;
                                                int numberEmpty = 0;
                                                int numberOk = 0;
                                                for (int row = 0; row < (int)ModuleRowCol.DryingOvenRow; row++)//判断空位置数量，
                                                {
                                                    if (tempList[row].Type == PltType.Invalid)//空位置
                                                    {
                                                        numberInvalid += 1;
                                                    }
                                                    if (tempList[row].Type == PltType.OK && tempList[row].IsStage(PltStage.Invalid))//空托盘
                                                    {
                                                        numberEmpty += 1;
                                                    }
                                                    if (tempList[row].Type >= PltType.WaitCooling && tempList[row].Stage == PltStage.Offload)//待冷却或下料托盘
                                                    {
                                                        numberEmpty += 1;
                                                    }
                                                    if (tempList[row].Type == PltType.OK && tempList[row].Stage == PltStage.Onload)//待烘烤托盘
                                                    {
                                                        numberOk += 1;
                                                    }
                                                }
                                                list.Add(new int[] { nOvenArrayIdx, Col, numberInvalid, numberEmpty, numberOk });
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                    }

                }

            }
            if (list.Count!=0)
            {
                if (bIsPick)
                {
                    list.Sort((a, b) =>
                    {
                        int numok = b[4].CompareTo(a[4]); if (numok == 0)
                        {
                            return b[3].CompareTo(a[3]);
                        }
                        return numok;
                    });
                }
                else
                {
                    list.Sort((a, b) =>
                    {
                        int numok = a[4].CompareTo(b[4]); if (numok == 0)
                        {
                            return b[2].CompareTo(a[2]);
                        }
                        return numok;
                    });
                }
                nOvenArrayNum = list[0][0];
                nCol = list[0][1];
                int c = -1;
                RunProDryingOven curOventest = arrDryingOven[nOvenArrayNum];
                var tempListtest = curOventest.pallet.Skip(nCol * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
                if (!bIsPick)
                {
                    bool bIgnoreCheck = (ModuleEvent.OvenPlaceEmptyPlt == eEvent) ? true : false;
                    PltType pltType = (ModuleEvent.OvenPlaceEmptyPlt == eEvent) ? PltType.Invalid : PltType.NG;
                    tempListtest.ForEach((Pallet A, int Index) =>
                    {
                        if ((A.IsType(PltType.Invalid) && A.IsType(pltType) && A.IsStage(PltStage.Invalid) &&
                        (bIgnoreCheck || PltIsEmpty(A))))
                            c = Index;
                    });
                }
                else
                {
                    if (!tempListtest.Any(A => A.IsType(PltType.OK) && PltIsEmpty(A)))
                        return false;
                    tempListtest.ForEach((Pallet A, int Index) =>
                    {
                        if (A.IsType(PltType.OK) && A.IsStage(PltStage.Invalid) && PltIsEmpty(A))
                            c = Index;
                    });
                }
                nRow = c;
            }
            if (nRow != -1)
                return true;else return false;
        }

        /// <summary>
        /// 搜索炉子放料(转移)
        /// </summary>
        private bool OvenGlobalSearchTransfer(ModuleEvent eEvent, ref int nOvenID, ref int nRow, ref int nCol)
        {
            Pallet[] rowPlt = new Pallet[2] { null, null };

            if (eEvent != ModuleEvent.OvenPlaceFullPlt)
            {
                return false;
            }

            for (int nOvenArrayIdx = 0; nOvenArrayIdx < arrDryingOven.Length; nOvenArrayIdx++)
            {
                RunProDryingOven curOven = arrDryingOven[nOvenArrayIdx];
                for (int nRowIdx = 0; nRowIdx < (int)ModuleRowCol.DryingOvenRow; nRowIdx++)
                {
                    rowPlt[0] = curOven.GetPlt(nRowIdx, 0);
                    rowPlt[1] = curOven.GetPlt(nRowIdx, 1);

                    if (curOven.IsCavityEN(nRowIdx)
                        && !curOven.IsPressure(nRowIdx)
                        //&& !curOven.IsTransfer(nRowIdx)
                        && CavityState.Standby == curOven.GetCavityState(nRowIdx)
                        && curOven.IsModuleEnable())
                    {
                        // 干燥炉放转移托盘（真空失败）
                        if (rowPlt[0].IsType(PltType.Invalid) && rowPlt[0].IsStage(PltStage.Invalid) && PltIsEmpty(rowPlt[0])
                            && rowPlt[1].IsType(PltType.Invalid) && rowPlt[1].IsStage(PltStage.Invalid) && PltIsEmpty(rowPlt[1]))
                        {
                            nOvenID = nOvenArrayIdx;
                            nRow = nRowIdx;
                            nCol = 0;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 调度等待超时自动搜索步骤
        /// </summary>
        private bool ReadyWaitTimeOutSearchAutoStep()
        {
            Pallet pPallet = Pallet[(int)ModuleDef.Pallet_0];

            bTimeOutAutoSearchStep = false;
            SaveParameter();
            if (pPallet.Type == PltType.Invalid
                 && SetModuleEvent(PickAction.Station, PickAction.EEvent, EventState.Cancel, PickAction.Row, PickAction.Col))
            {
                this.nextAutoStep = AutoSteps.Auto_WaitWorkStart; //大机器人抓手为空，直接等待开始信号
                return true;
            }
            else
            {
                int nPlaceRow = -1;
                int nPlaceCol = -1;
                int nOvenID = -1;
                //大机器人抓手为空托盘
                if (pPallet.IsEmpty() && pPallet.Type != PltType.NG)
                {
                    //搜索上料是否要空托盘
                    if (SearchOnloadPlacePos(ModuleEvent.OnloadPlaceEmptyPallet, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.OnloadStation, 0, nPlaceCol, ModuleEvent.OnloadPlaceEmptyPallet);
                        //PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }

                    //搜索缓存架是否要空托盘
                    if (SearchPltBufPlacePos(ModuleEvent.PltBufPlaceEmptyPlt, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPlaceEmptyPlt);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }

                    //炉子要空托盘请求
                    if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceEmptyPlt, ref nOvenID, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceEmptyPlt);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }
                }

                //机器人抓手为上料完成托盘
                if (!pPallet.IsEmpty() && pPallet.Stage == PltStage.Onload)
                {
                    ModuleEvent placeEvent = pPallet.HasTypeBat(BatType.Fake) ? ModuleEvent.OvenPlaceFakeFullPlt : ModuleEvent.OvenPlaceFullPlt;
                    if (OvenGlobalSearch(false, placeEvent, ref nOvenID, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, placeEvent);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }
                }

                // 机器人抓手为NG空托盘
                if (pPallet.IsEmpty() && pPallet.Type == PltType.NG)
                {
                    // 人工治具区要NG料盘
                    if (SearchManualOperPlatPlacePos(ModuleEvent.ManualOperatPlaceNGEmptyPlt, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.ManualOperat, 0, 0, ModuleEvent.ManualOperatPlaceNGEmptyPlt);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }
                    // 缓存架要NG料盘
                    if (SearchPltBufPlacePos(ModuleEvent.PltBufPickNGEmptyPlt, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.PalletBuffer, nPlaceRow, 0, ModuleEvent.PltBufPickNGEmptyPlt);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }
                    // 炉子要NG料盘
                    if (OvenGlobalSearch(false, ModuleEvent.OvenPlaceNGEmptyPlt, ref nOvenID, ref nPlaceRow, ref nPlaceCol) &&
                        SetModuleEvent(PlaceAction.Station, PlaceAction.EEvent, EventState.Cancel, PlaceAction.Row, PlaceAction.Col))
                    {
                        // 放
                        PlaceAction.SetAction(TransferRobotStation.DryingOven_0 + nOvenID, nPlaceRow, nPlaceCol, ModuleEvent.OvenPlaceNGEmptyPlt);
                        PreSendEvent(PlaceAction);
                        nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        return true;
                    }
                }

            }
            ShowMsgBox.ShowDialog("未搜索到重新放料位置，请检查调度托盘信息！", MessageType.MsgQuestion);
            return false;
        }
        #endregion


        #region // 模组信号操作

        /// <summary>
        /// 设置模组信号
        /// </summary>
        public bool SetModuleEvent(TransferRobotStation station, ModuleEvent modEvent, EventState state, int nRowIdx = -1, int nColIdx = -1, int nParam1 = -1)
        {
            RunProcess run = null;

            if (GetModuleByStation(station, ref run))
            {
                return SetEvent(run, modEvent, state, nRowIdx, nColIdx, nParam1,this.RunName);
            }
            return false;
        }

        /// <summary>
        /// 检查模组信号，并返回信号参数
        /// </summary>
        public bool CheckModuleEvent(TransferRobotStation station, ModuleEvent modEvent, EventState state, ref int nRowIdx, ref int nColIdx)
        {
            RunProcess run = null;

            if (GetModuleByStation(station, ref run))
            {
                return CheckEvent(run, modEvent, state, ref nRowIdx, ref nColIdx);
            }
            return false;
        }

        /// <summary>
        /// 检查模组信号
        /// </summary>
        public bool CheckModuleEvent(TransferRobotStation station, ModuleEvent modEvent, EventState state)
        {
            RunProcess run = null;

            if (GetModuleByStation(station, ref run))
            {
                return CheckEvent(run, modEvent, state);
            }
            return false;
        }

        #endregion
        

        #region // 模组操作

        /// <summary>
        /// 通过干燥炉ID获取模组
        /// </summary>
        public RunProDryingOven GetOvenByID(int nOvenID)
        {
            if (nOvenID > -1)
            {
                foreach (RunProDryingOven curOven in arrDryingOven)
                {
                    if (nOvenID == curOven.GetOvenID())
                    {
                        return curOven;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 通过机器人站点获取模组
        /// </summary>
        public bool GetModuleByStation(TransferRobotStation station, ref RunProcess run)
        {
            // 干燥炉
            if (station > TransferRobotStation.Invalid && station <= TransferRobotStation.DryingOven_5)
            {
                run = GetOvenByID(station - TransferRobotStation.DryingOven_0);
                return true;
            }
            // 托盘缓存架
            else if (TransferRobotStation.PalletBuffer == station)
            {
                run = palletBuf;
                return true;
            }
            // 人工操作平台
            else if (TransferRobotStation.ManualOperat == station)
            {
                run = manualOperat;
                return true;
            }
            // 上料模组
            else if (TransferRobotStation.OnloadStation == station)
            {
                run = onloadRobot;
                return true;
            }
            // 下料模组
            else if (TransferRobotStation.OffloadStation == station)
            {
                run = offloadRobot;
                return true;
            }
            // 冷却模组
            else if (TransferRobotStation.CooligStove == station)
            {
                run = coolingStove;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 预先发送模组信号
        /// </summary>
        private bool PreSendEvent(ActionInfo info)
        {
            // 干燥炉、托盘缓存架、人工操作平台
            if ((info.Station > TransferRobotStation.Invalid && info.Station <= TransferRobotStation.DryingOven_5) || 
                (TransferRobotStation.PalletBuffer == info.Station) || (TransferRobotStation.ManualOperat == info.Station) || 
                (TransferRobotStation.CooligStove == info.Station))
            {
                if (CheckModuleEvent(info.Station, info.EEvent, EventState.Require))
                {
                    return SetModuleEvent(info.Station, info.EEvent, EventState.Response, info.Row, info.Col);
                }
                return false;
            }

            return true;
        }

        #endregion


        #region // 硬件检查相关

        /// <summary>
        /// 工位检查
        /// </summary>
        private bool CheckStation(int station, int row, int col, bool bPickIn)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            int nPltIdx = -1;
            string strInfo = "";
            RunProcess run = null;

            if (GetModuleByStation((TransferRobotStation)station, ref run) && null != run)
            {
                // 干燥炉
                if (station > (int)TransferRobotStation.Invalid && station <= (int)TransferRobotStation.DryingOven_5)
                {
                    RunProDryingOven oven = run as RunProDryingOven;

                    // 特殊的检查
                    if (!oven.OvenIsConnect())
                    {
                        strInfo = string.Format("\r\n检测到{0}连接已断开，无法查询到数据，禁止操作！", run.RunName);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 炉门检查
                    if (oven.CurCavityData(col).DoorState != OvenDoorState.Open)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层炉门关闭..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 联机检查
                    if (oven.CurCavityData(col).OnlineState != OvenOnlineState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层为本地状态..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    nPltIdx = col * (int)ModuleRowCol.DryingOvenRow + row;

                }
                // 托盘缓存架
                else if ((int)TransferRobotStation.PalletBuffer == station)
                {
                    nPltIdx = row;
                }
                // 人工操作平台
                else if ((int)TransferRobotStation.ManualOperat == station)
                {
                    nPltIdx = 0;
                }
                // 上料模组
                else if ((int)TransferRobotStation.OnloadStation == station)
                {
                    nPltIdx = col;
                }
                // 下料模组
                else if ((int)TransferRobotStation.OffloadStation == station)
                {
                    nPltIdx = col;
                }
                // 冷却炉
                else if ((int)TransferRobotStation.CooligStove == station)
                {
                    nPltIdx = row;
                }
                return run.CheckPallet(nPltIdx, bPickIn, true);
            }
            return false;
        }

        /// <summary>
        /// 托盘检测
        /// </summary>
        public override bool CheckPallet(int nPltIdx, bool bHasPlt, bool bAlarm = true)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            if (nPltIdx < 0 || nPltIdx >= (int)ModuleDef.Pallet_All)
            {
                return false;
            }

            if (/*!InputState(IPltHasCheck, bHasPlt) ||*/ !InputState(IPltLeftCheck, bHasPlt) || !InputState(IPltRightCheck, bHasPlt))
            {
                if (bAlarm)
                {
                    //CheckInputState(IPltHasCheck, bHasPlt);
                    CheckInputState(IPltLeftCheck, bHasPlt);
                    CheckInputState(IPltRightCheck, bHasPlt);
                }
                return false;
            }
            return true;
        }


        /// <summary>
        /// 自动运行开始时调度机器人位置防呆检查
        /// </summary>
        /// <returns></returns>
        public bool CheckRobotStartPos(out string msg)
        {
            msg = string.Empty;
            //手自动在同一位置
            if (robotAutoInfo.Station == robotDebugInfo.Station
                && robotAutoInfo.Row == robotDebugInfo.Row
                && robotAutoInfo.Col == robotDebugInfo.Col
                && robotAutoInfo.action == robotDebugInfo.action)
            {
                return true;
            }

            if (robotAutoInfo.Station == 0)
            {
                if (!(robotDebugInfo.action == RobotAction.PLACEOUT
                   || robotDebugInfo.action == RobotAction.PICKOUT
                   || robotDebugInfo.action == RobotAction.MOVE))
                {
                    msg = "位置异常！！！请将堆垛机移至“移动”位置";
                    return false;
                }
                return true;
            }

            //自动要移动或取出放出，手动在“移动”或“放出”或“取出”
            if ((robotAutoInfo.action == RobotAction.MOVE || robotAutoInfo.action == RobotAction.PLACEOUT || robotAutoInfo.action == RobotAction.PICKOUT)
                && (robotDebugInfo.action == RobotAction.PLACEOUT || robotDebugInfo.action == RobotAction.PICKOUT || robotDebugInfo.action == RobotAction.MOVE))
            {
                return true;
            }
            //在手自动同一工位行列，自动要取，手动要在移动位
            if (robotAutoInfo.Station == robotDebugInfo.Station
              && robotAutoInfo.Row == robotDebugInfo.Row
              && robotAutoInfo.Col == robotDebugInfo.Col)
            {
                if ((robotAutoInfo.action == RobotAction.PICKIN || robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.PLACEIN || robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.PLACEOUT || robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.PICKOUT || robotDebugInfo.action == RobotAction.PICKIN)
                    || (robotAutoInfo.action == RobotAction.PLACEOUT || robotDebugInfo.action == RobotAction.PLACEIN))
                {
                    return true;
                }
            }


            msg = string.Format("请切换到【机器人调试界面】\r\n将堆垛机移动到{0}工位{1}行{2}列{3} 重新复位启动",
            GetStationName((TransferRobotStation)robotAutoInfo.Station),
            robotAutoInfo.Row + 1,
            robotAutoInfo.Col + 1,
            robotAutoInfo.action);
            RecordMessageInfo((int)MsgID.AutoCheckPosStep, msg, MessageType.MsgAlarm);
            return false;
        }


        /// <summary>
        /// 手动操作工位检查（调试界面调用）
        /// </summary>
        public override bool ManualCheckStation(int station, int row, int col, bool bPickIn)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            int nPltIdx = -1;
            string strInfo = "";
            bool bDestHasPlt = false;
            bool bFingerHasPlt = false;
            RunProcess run = null;

            // 1.检查抓手是否有电池
            bFingerHasPlt = InputState(IPltLeftCheck, true) || InputState(IPltRightCheck, true) /*|| InputState(IPltHasCheck, true)*/;

            // 2.检查目标位置是否有电池
            if (GetModuleByStation((TransferRobotStation)station, ref run) && null != run)
            {
                // 干燥炉
                if (station > (int)TransferRobotStation.Invalid && station <= (int)TransferRobotStation.DryingOven_5)
                {
                    RunProDryingOven oven = run as RunProDryingOven;

                    // 特殊的检查
                    if (!oven.OvenIsConnect())
                    {
                        strInfo = string.Format("\r\n检测到{0}连接已断开，无法查询到数据，禁止操作！", run.RunName);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    int nCavityIdx = col;
                    // 光幕检查
                    if (oven.CurCavityData(nCavityIdx).ScreenState == OvenScreenState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层安全光幕有遮挡...严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 炉门检查
                    if (oven.CurCavityData(nCavityIdx).DoorState != OvenDoorState.Open)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层炉门关闭..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 联机检查
                    if (oven.CurCavityData(nCavityIdx).OnlineState != OvenOnlineState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层为本地状态..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                    
                    nPltIdx = col * (int)ModuleRowCol.DryingOvenRow + row;

                    // 检查托盘未放好
                    if (!run.CheckPallet(nPltIdx, true, false) && !run.CheckPallet(nPltIdx, false, false))
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层{2}号托盘未放好..严禁进行取放治具！", run.RunName, row + 1, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                }
                // 托盘缓存架
                else if ((int)TransferRobotStation.PalletBuffer == station)
                {
                    nPltIdx = row;
                }
                // 人工操作平台
                else if ((int)TransferRobotStation.ManualOperat == station)
                {
                    nPltIdx = 0;
                }
                // 上料模组
                else if ((int)TransferRobotStation.OnloadStation == station)
                {
                    nPltIdx = col;
                }
                // 下料模组
                else if ((int)TransferRobotStation.OffloadStation == station)
                {
                    //nPltIdx = col;
                    //if (!offloadRobot.CheckUpCylState(nPltIdx))
                    //{
                    //    return false;
                    //}
                    nPltIdx = col;
                }
                // 冷却炉
                else if ((int)TransferRobotStation.CooligStove == station)
                {
                    nPltIdx = row;
                    RunProCoolingStove CooligStove = run as RunProCoolingStove;
                    // 炉门检查
                    if ((OvenDoorState)CooligStove.GetOvenDoorState(nPltIdx) != OvenDoorState.Open)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层炉门关闭..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                }

                bDestHasPlt = run.CheckPallet(nPltIdx, true, false);

                // 1.同时有
                if (bFingerHasPlt && bDestHasPlt)
                {
                    strInfo = "\r\n检测到插料架和目标工位都有托盘，禁止操作！";
                }
                // 2.抓手有，目标无，禁止取
                else if (bFingerHasPlt && !bDestHasPlt && bPickIn)
                {
                    strInfo = "\r\n检测到插料架有托盘，禁止取托盘！";
                }
                // 3.抓手无，目标有，禁止放
                else if (!bFingerHasPlt && bDestHasPlt && !bPickIn)
                {
                    strInfo = "\r\n检测到目标工位有托盘，禁止放托盘！";
                }
                // 4.同时无，禁止取放
                else if (!bFingerHasPlt && !bDestHasPlt)
                {
                    strInfo = "\r\n检测到插料架和目标工位都无托盘，禁止取放托盘！";
                }

                if (!string.IsNullOrEmpty(strInfo))
                {
                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 炉子状态检查
        /// </summary>
        public bool CheckOvenState(int station, int col)
        {
            if (Def.IsNoHardware() || DryRun)
            {
                return true;
            }

            RunProcess run = null;
            string strInfo = "";
            if (GetModuleByStation((TransferRobotStation)station, ref run) && null != run)
            {
                if (station > (int)TransferRobotStation.Invalid && station <= (int)TransferRobotStation.DryingOven_5)
                {
                    RunProDryingOven oven = run as RunProDryingOven;

                    // 特殊的检查
                    if (!oven.OvenIsConnect())
                    {
                        strInfo = string.Format("\r\n检测到{0}连接已断开，无法查询到数据，禁止操作！", run.RunName);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                    //int nCavityIdx = oven.GetOvenGroup() == 0 ? col : 1 - col;
                    int nCavityIdx = col;
                    // 光幕检查
                    if (oven.CurCavityData(nCavityIdx).ScreenState == OvenScreenState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层安全光幕有遮挡...严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 炉门检查
                    if (oven.CurCavityData(nCavityIdx).DoorState != OvenDoorState.Open)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层炉门关闭..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 联机检查
                    if (oven.CurCavityData(nCavityIdx).OnlineState != OvenOnlineState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层为本地状态..严禁进行取放治具！", run.RunName, col + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 炉门状态检查（取出放出）
        /// </summary>
        public bool CheckOvenDoorState(int station, int col)
        {
            if (Def.IsNoHardware() || DryRun)
            {
                return true;
            }

            RunProcess run = null;
            string strInfo = "";
            if (GetModuleByStation((TransferRobotStation)station, ref run) && null != run)
            {
                if (station > (int)TransferRobotStation.Invalid && station <= (int)TransferRobotStation.DryingOven_5)
                {
                    RunProDryingOven oven = run as RunProDryingOven;

                    // 特殊的检查
                    if (!oven.OvenIsConnect())
                    {
                        strInfo = string.Format("\r\n检测到{0}连接已断开，无法查询到数据，禁止操作！", run.RunName);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                    //int nCavityIdx = oven.GetOvenGroup() == 0 ? col : 1 - col;
                    int nCavityIdx = col;
                    // 炉门检查
                    if (oven.CurCavityData(nCavityIdx).DoorState != OvenDoorState.Open)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层炉门关闭..严禁进行取放治具！", run.RunName, nCavityIdx + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }

                    // 联机检查
                    if (oven.CurCavityData(nCavityIdx).OnlineState != OvenOnlineState.Have)
                    {
                        strInfo = string.Format("\r\n检测到{0}第{1}层为本地状态..严禁进行取放治具！", run.RunName, nCavityIdx + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 机器人安全位检查
        /// </summary>
        public bool RobotInSafePos()
        {
            if (!bRobotEN)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 机器人位置检查
        /// </summary>
        public bool CheckTransferRobotPos()
        {
            if (robotAutoInfo.Station != robotDebugInfo.Station
                            || robotAutoInfo.Row != robotDebugInfo.Row
                            || robotAutoInfo.Col != robotDebugInfo.Col
                            || robotAutoInfo.action != robotDebugInfo.action)
            {
                if (robotAutoInfo.Station == 0 || robotDebugInfo.Station == 0
                    || robotAutoInfo.action == RobotAction.PICKOUT || robotAutoInfo.action == RobotAction.PLACEOUT
                    || robotDebugInfo.action == RobotAction.PICKOUT || robotDebugInfo.action == RobotAction.PLACEOUT)
                {
                    return true;
                }
                else
                {
                    string strInfo = string.Format("请切换到【机器人调试界面】\r\n将调度机器人移动到{0}工位{1}行{2}列{3} 重新复位启动",
                    GetStationName((TransferRobotStation)robotAutoInfo.Station),
                    robotAutoInfo.Row + 1,
                    robotAutoInfo.Col + 1,
                    robotAutoInfo.action);
                    ShowMessageBox((int)MsgID.AutoCheckPosStep, "位置异常", strInfo, MessageType.MsgWarning);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查机器人位置（清任务防呆用）
        /// </summary>
        /// <returns></returns>
        public bool CheckRobotPos(int nStation)
        {
            if (nStation != robotDebugInfo.Station)
            {
                return false;
            }
            if (robotDebugInfo.action == RobotAction.PICKOUT || robotDebugInfo.action == RobotAction.PLACEOUT || robotDebugInfo.action == RobotAction.MOVE)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查机器人是否安全
        /// </summary>
        private bool CheckRobotIsSafe(int nStation)
        {

            // 上料
            if (nStation == (int)TransferRobotStation.OnloadStation)
            {
                // 检查机器人是否在安全位
                RunProOnloadRobot onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProOnloadRobot;
                if (!onloadRobot.RobotIsConnect())
                {
                    ShowMsgBox.ShowDialog("上料机器人未连接，请在机器人界面将上料机器人连接移动至安全位", MessageType.MsgWarning);
                    return false;
                }

                if (!onloadRobot.CheckRobotIsSafety())
                {
                    ShowMsgBox.ShowDialog("上料机器人不在安全位，调度机器人不能操作，\n请在机器人界面将上料机器人移动至安全位", MessageType.MsgWarning);
                    return false;
                }
            }

            // 下料
            else if (nStation == (int)TransferRobotStation.OffloadStation)
            {
                RunProOffloadRobot offloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
                if (!offloadRobot.RobotIsConnect())
                {
                    ShowMsgBox.ShowDialog("下料机器人未连接，请在机器人界面将下料机器人连接移动至安全位", MessageType.MsgWarning);
                    return false;
                }
                if (!offloadRobot.CheckRobotIsSafety())
                {
                    ShowMsgBox.ShowDialog("下料机器人不在安全位，调度机器人不能操作，\n请在机器人界面将下料机器人移动至安全位", MessageType.MsgWarning);
                    return false;
                }
            }

            return true;
        }
        #endregion


        #region // 机器人相关

        /// <summary>
        /// 获取机器人ID
        /// </summary>
        public  int RobotID()
        {
            return nRobotID;
        }

        /// <summary>
        /// 获取机器人速度
        /// </summary>
        public  int RobotSpeed()
        {
            return nRobotSpeed;
        }

        /// <summary>
        /// 获取机器人端口
        /// </summary>
        public  int RobotPort()
        {
            return nRobotPort;
        }

        /// <summary>
        /// 获取机器人IP
        /// </summary>
        public  string RobotIP()
        {
            return strRobotIP;
        }

        /// <summary>
        /// 机器人连接状态
        /// </summary>
        public  bool RobotIsConnect()
        {
            if (!bRobotEN && Def.IsNoHardware())
            {
                return true;
            }

            return robotClient.IsConnect();
        }

        /// <summary>
        /// 机器人连接
        /// </summary>
        public  bool RobotConnect(bool connect = true)
        {
            if (!bRobotEN || (connect && RobotIsConnect()) )
            {
                return true;
            }
            if (connect)
            {
                if (robotClient.Connect(strRobotIP, nRobotPort))
                {
                    ConnectState = true;
                    return true;
                }
            }
            else
            {
                if (robotClient.Disconnect())
                {
                    ConnectState = false;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 机器人移动
        /// </summary>
        public bool RobotMove(int[] frame, bool bWait = true)
        {
            if (!bRobotEN && Def.IsNoHardware())
            {
                return true;
            }

            if (bWait)
            {
                int[] arrRecv = new int[(int)RobotCmdFrame.End];

                // 发送命令，并等待完成
                if (robotClient.SendAndWait(frame, ref arrRecv, (uint)nRobotTimeout))
                {
                    return RobotMoveFinish(frame, 1);
                }
            }
            else
            {
                // 发送命令，不等待
                return robotClient.Send(frame);
            }
            return false;
        }
        public bool CheckTransRobotStep()
        {
            //return AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep;
            bool transRobotMove = (AutoSteps.Auto_OnloadPickMove == (AutoSteps)this.nextAutoStep) || (AutoSteps.Auto_OnloadPickSendEvent == (AutoSteps)this.nextAutoStep)
                                  || (AutoSteps.Auto_OnloadPickIn == (AutoSteps)this.nextAutoStep)|| (AutoSteps.Auto_OnloadPickDataTransfer == (AutoSteps)this.nextAutoStep)
                                  || (AutoSteps.Auto_OnloadPickOut == (AutoSteps)this.nextAutoStep) || (AutoSteps.Auto_OnloadPickCheckFinger == (AutoSteps)this.nextAutoStep)
                                  || (AutoSteps.Auto_OnloadPlaceMove == (AutoSteps)this.nextAutoStep) || (AutoSteps.Auto_OnloadPlaceSendEvent == (AutoSteps)this.nextAutoStep)
                                  || (AutoSteps.Auto_OnloadPlaceIn == (AutoSteps)this.nextAutoStep) || (AutoSteps.Auto_OnloadPlaceDataTransfer == (AutoSteps)this.nextAutoStep)
                                  || (AutoSteps.Auto_OnloadPlaceOut == (AutoSteps)this.nextAutoStep) || (AutoSteps.Auto_OnloadPlaceCheckFinger == (AutoSteps)this.nextAutoStep);
            return transRobotMove;          
        }
        /// <summary>
        /// 机器人移动并等待完成
        /// </summary>
        public  bool RobotMove(int station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid)
        {
            return RobotMove((TransferRobotStation)station, row, col, speed, action, motorLoc);
        }

        /// <summary>
        /// 机器人移动并等待完成
        /// </summary>
        public bool RobotMove(TransferRobotStation station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid)
        {
            if (RobotCmd(station, row, col, speed, action, ref arrRobotCmd))
            {
                if (!bRobotEN && Def.IsNoHardware())
                {
                    return true;
                }

                // 机器人移动，并等待动作完成
                if (robotClient.Send(arrRobotCmd))
                {
                    return RobotMoveFinish(arrRobotCmd, nRobotTimeout);
                }
            }
            return false;
        }

        /// <summary>
        /// 获取机器人命令帧
        /// </summary>
        public bool RobotCmd(TransferRobotStation station, int row, int col, int speed, RobotAction action, ref int[] frame)
        {
/*            if (station >= TransferRobotStation.DryingOven_6 && station <= TransferRobotStation.DryingOven_9)
            {
                return false;
            }*/

            frame[(int)RobotCmdFrame.Station] = (int)station;
            frame[(int)RobotCmdFrame.StationRow] = row + 1;
            frame[(int)RobotCmdFrame.StationCol] = col + 1;
            frame[(int)RobotCmdFrame.Speed] = speed;
            frame[(int)RobotCmdFrame.Action] = (int)action;
            frame[(int)RobotCmdFrame.Result] = (int)RobotAction.END;

            if (MCState.MCRunning == MachineCtrl.GetInstance().RunsCtrl.GetMCState())
            {
                robotAutoInfo.SetInfo((int)station, row, col, action, GetStationName(station));
                robotDebugInfo.SetInfo((int)station, row, col, action, GetStationName(station));
            }
            else
            {
                robotDebugInfo.SetInfo((int)station, row, col, action, GetStationName(station));
            }

            SaveRunData(SaveType.Robot);
            return true;
        }

        /// <summary>
        /// 等待机器人移动完成
        /// </summary>
        public bool RobotMoveFinish(int[] frame, int waitTime)
        {
            if (!bRobotEN && Def.IsNoHardware())
            {
                return true;
            }

            int nErrCode = -1;
            string strMsg, strDisp;
            int[] arrRecv = new int[(int)RobotCmdFrame.End];
            DateTime startTime = DateTime.Now;

            robotProcessingFlag = true;
            while (true)
            {
                Array.Clear(arrRecv, 0, arrRecv.Length);

                if (robotClient.GetResult(ref arrRecv))
                {
                    // 移动完成
                    if (RobotAction.FINISH == (RobotAction)arrRecv[(int)RobotCmdFrame.Result])
                    {
                        if (frame[(int)RobotCmdFrame.Station] == arrRecv[(int)RobotCmdFrame.Station] &&
                            frame[(int)RobotCmdFrame.StationRow] == arrRecv[(int)RobotCmdFrame.StationRow] &&
                            frame[(int)RobotCmdFrame.StationCol] == arrRecv[(int)RobotCmdFrame.StationCol] &&
                            frame[(int)RobotCmdFrame.Action] == arrRecv[(int)RobotCmdFrame.Action])
                        {
                            nErrCode = 0;
                        }
                        break;
                    }
                    // 断开连接
                    else if (RobotAction.DISCONNECT == (RobotAction)arrRecv[(int)RobotCmdFrame.Result])
                    {
                        nErrCode = 1;
                        break;
                    }
                    // 结果错误
                    else if (RobotAction.ERR == (RobotAction)arrRecv[(int)RobotCmdFrame.Result])
                    {
                        nErrCode = 2;
                        break;
                    }
                }
                if (frame[(int)RobotCmdFrame.Action] == (int)RobotAction.PICKOUT)
                {
                    // 超时检查
                    if ((DateTime.Now - startTime).TotalSeconds > nRobotPickOutTimeout)
                    {
                        nErrCode = 3;
                        break;
                    }
                }
                // 超时检查
                if ((DateTime.Now - startTime).TotalSeconds > waitTime)
                {
                    nErrCode = 3;
                    break;
                }

                Sleep(1);
            }

            robotProcessingFlag = false;
            if (1 == nErrCode)
            {
                strDisp = "请检机器人位置后重新连接";
                strMsg = string.Format("{0}收到连接断开反馈", RunName);
                ShowMessageBox((int)MsgID.RbtDisConnect, strMsg, strDisp, MessageType.MsgAlarm);
                return false;
            }
            else if (2 == nErrCode)
            {
                strDisp = "请检机器人当前位置";
                strMsg = string.Format("{0}指令错误", RunName);
                ShowMessageBox((int)MsgID.RbtMoveCmdError, strMsg, strDisp, MessageType.MsgAlarm);
                return false;
            }
            else if (3 == nErrCode)
            {
                strDisp = "请检机器人当前位置和状态";
                strMsg = string.Format("{0}等待动作完成超时", RunName);
                ShowMessageBox((int)MsgID.RbtMoveTimeout, strMsg, strDisp, MessageType.MsgAlarm);
                return false;
            }
            else if (-1 == nErrCode)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取机器人工位名称
        /// </summary>
        public String GetStationName(TransferRobotStation station)
        {
            string strName = "";

            if (this.robotStationInfo.ContainsKey((int)station))
            {
                strName = this.robotStationInfo[(int)station].stationName;
            }
            return strName;
        }

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        public RobotActionInfo GetRobotActionInfo(bool bAutoInfo = true)
        {
            return bAutoInfo ? robotAutoInfo : robotDebugInfo;
        }

        /// <summary>
        /// 初始化机器人工位
        /// </summary>
        public void InitRobotStation()
        {
            if (null == robotStationInfo)
            {
                return;
            }

            int nFormulaID = Def.GetProductFormula();
            string strRobotName = this.RunName;
            //List<RobotFormula> listStation = new List<RobotFormula>();
            // 清除原来的数据
            dbRecord.DeleteRobotAllStation(new RobotFormula() { formulaID = nFormulaID, robotID = nRobotID });
            // 创建新数据
            var listStation = Enum.GetNames(typeof(TransferRobotStation))
                .Select(s => (TransferRobotStation)Enum.Parse(typeof(TransferRobotStation), s))
                .OrderBy(s => s)
                .Where(s => s.GetAttributes().Where(att => att.GetType() == typeof(Info)).Any())
                .Select(station => (station, station.GetAttributes().Where(att => att.GetType() == typeof(Info)).Select(att => att as Info).First()))
                .Select(dt => (new RobotFormula(nFormulaID, nRobotID, strRobotName, (int)dt.station, $"【{(int)dt.station}】{dt.Item2.info}", dt.Item2.MaxRow, dt.Item2.MaxCol), dt.Item2.Motor));

            foreach (var item in listStation)
            {
                this.robotStationInfo.Add(item.Item1.stationID, item.Item1);
                dbRecord.AddRobotStation(item.Item1);
                RobotStationInfo.Add(item.Item1.stationID, (item.Item1, item.Motor));
            }
        }

        #endregion

        #region // mes接口

        /// <summary>
        /// 下料时间CSV
        /// </summary>
        private void OffLoadTimeCsv(string strPalletCade)
        {
            //string sFilePath = "D:\\OffLoadTimeCsv";
            string sFilePath = string.Format("{0}\\OffLoadTimeCsv", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "下料时间.CSV";
            string sColHead = "托盘条码,下料时间";
            string sLog = string.Format("{0},{1}"
                , strPalletCade
                , DateTime.Now);
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }

        public IRobotInfoBase GetRobotActionRecvInfo()
        {
            return robotDebugInfo;
        }

        public bool IsCollisionAlarm(out string msg)
        {
            msg = default;
            return false;
        }

        public void CloseOutPutState()
        {
            //((IRobot)onloadRobot).CloseOutPutState();
        }

        public string RobotName()
        {
            return this.RunName;
        }

        public bool RobotHome()
        {
            return false;
        }

        public bool RobotMove(int station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid, bool isAuto = true)
        {
            return RobotMove((TransferRobotStation)station, row, col, speed, action, motorLoc);
        }

        public bool ManualCheckStation(int station, int row, int col, RobotAction action, bool bPickIn)
        {
            // 检查机器人是否安全
            if (action == RobotAction.PICKIN || action == RobotAction.PLACEIN)
            {
                if (!CheckRobotIsSafe(station))
                {
                    return false;
                }
                return ManualCheckStation(station, row, col, bPickIn);
            }
            return true;
        }

        public bool FingerClose(uint fingers, bool close)
        {
            return false;        }
        #endregion
    }
}
