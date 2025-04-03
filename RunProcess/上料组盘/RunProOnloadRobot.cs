using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
using HelperLibrary;
using ImTools;
using Machine.Framework.Robot;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemControlLibrary;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOnloadRobot : RunProcess, IRobot
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_RobotConnect,
            Init_RobotHome,
            Init_CheckFinger,
            Init_CheckPallet,
            Init_MotorHome,
            Init_ScannerConnect,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 机器人避让
            Auto_RobotAvoidMove,
            Auto_WaitActionFinished,

            // 托盘扫码
            Auto_PalletScanCodeMove,
            Auto_PalletScanCodeDown,
            Auto_PalletScanCodeAction,
            Auto_PalletScanCodeUp,
            Auto_MesCheckJigVaild,

            // 取：来料线
            Auto_OnlinePosSetEvent,
            Auto_OnlinePosPickMove,
            Auto_OnlinePosPickDown,
            Auto_OnlinePosFingerAction,
            Auto_OnlinePosPickUp,
            Auto_OnlinePosCheckFinger,

            // 取：假电池扫码
            Auto_FakePosSetEvent,
            Auto_FakeScanCodeMove,
            Auto_FakeScanCodeDown,
            Auto_FakeScanCodeAction,
            Auto_FakeScanCodeUp,

            // 取：假电池线
            Auto_FakePosPickMove,
            Auto_FakePosPickDown,
            Auto_FakePosFingerAction,
            Auto_FakePosPickUp,
            Auto_FakePosCheckFinger,

            // 取：NG夹具
            Auto_NgPalletPickMove,
            Auto_NgPalletPickDown,
            Auto_NgPalletFingerAction,
            Auto_NgPalletPickUp,
            Auto_NgPalletCheckFinger,

            // 暂存位取放流程
            Auto_BufferPosSetEvent,
            Auto_BufferPosMove,
            Auto_BufferPosDown,
            Auto_BufferPosFingerCheck,
            Auto_BufferPosFingerAction,
            Auto_BufferPosUp,
            Auto_BufferPosCheckFinger,

            // 计算放位置
            Auto_CalcPlacePos,

            // 放：托盘
            Auto_MesBindJigBat,
            Auto_MesUnBindBat,
            Auto_PalletPosPlaceMove,
            Auto_PalletPosPlaceDown,
            Auto_PalletPosFingerAction,
            Auto_PalletPosPlaceUp,
            Auto_PalletPosCheckFinger,

            // 放：回炉托盘
            Auto_RebakingPltPosPlaceMove,
            Auto_RebakingPltPosPlaceDown,
            Auto_RebakingPltPosFingerAction,
            Auto_RebakingPltPosPlaceUp,
            Auto_RebakingPltPosCheckFinger,

            // 放：NG线
            Auto_NGLinePosSetEvent,
            Auto_NGLinePosPlaceMove,
            Auto_NGLinePosPlaceDown,
            Auto_NGLinePosPlaceAction,
            Auto_NGLinePosPlaceUp,
            Auto_NGLinePosCheckFinger,
            Auto_WorkEnd,
        }

        private enum ModuleDef
        {
            // 无效
            DefInvalid = 0,

            // 托盘
            Pallet_0 = 0,
            Pallet_1,
            Pallet_2,
            Pallet_All,

            // 抓手
            Finger_0 = 0x01 << 0,
            Finger_1 = 0x01 << 1,
            Finger_2 = 0x01 << 2,
            Finger_3 = 0x01 << 3,
            Finger_All = 0x0F,

            Finger_Count = 4,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadRobotMsgStartID,
            MesUnBindBat,
            CheckRobotDownPos,
            MesBindBat,
            MesCheckPlt,
            MesCheckBat,
            CheckFingerState,
            CheckRobotPos,
            CheckMotorPos,
            CheckRobotMove,
            RobotConnetState,
            CheckRobotOrder,
            WaitActionTimeOut,
            AutoCheckPosStep,
            RobotCheckStep,
        }

        #endregion


        #region // 数据结构定义

        private struct ActionInfo
        {
            public OnloadRobotStation station;
            public int row;
            public int col;
            public ModuleDef finger;
            public bool fingerClose;
            public MotorPosition motorPos;

            // 清除数据
            public void Release()
            {
                SetAction(OnloadRobotStation.Invalid, -1, -1, ModuleDef.Finger_All, false, MotorPosition.Invalid);
            }

            // 设置动作
            public void SetAction(OnloadRobotStation curStation, int curRow, int curCol, ModuleDef curFinger, bool figClose, MotorPosition curMotorPos)
            {
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = figClose;
                this.motorPos = curMotorPos;
            }
        };

        #endregion


        #region // 字段

        // 【相关模组】
        private RunProOnloadLine onloadLine;                // 来料线
        private RunProOnloadFake onloadFake;                // 假电池线
        private RunProOnloadNG onloadNG;                    // NG输出线
        private RunProOnloadBuffer onloadBuffer;            // 上料配对

        // 【IO/电机】
        private int[] IOpen;                                // 夹爪松开
        private int[] IClose;                               // 夹爪夹紧
        private int[] IFingerCheck;                         // 夹爪有料检测
        private int[] OOpen;                                // 夹爪松开
        private int[] OClose;                               // 夹爪夹紧
        private int[] IPltLeftCheck;                        // 托盘左检测
        private int[] IPltRightCheck;                       // 托盘右检测
        private int[] IPltHasCheck;                         // 托盘有料感应
        private int MotorU;                                 // 夹爪调宽电机U
        private int[] IPltCheckOK;                          // 托盘检测OK
        private int[] IPltCheckNG;                          // 托盘检测NG

        // 【模组参数】
        private bool bRobotEN;
        private string _strRobotIP;
        public string strRobotIP { get => _strRobotIP; set => SetProperty(ref _strRobotIP, value); }           // 机器人IP
        private int _nRobotPort;
        public int nRobotPort { get => _nRobotPort; set => SetProperty(ref _nRobotPort, value); }              // 机器人端口
        private int nRobotSpeed;                            // 机器人速度：1-100
        private int nRobotTimeout;                          // 机器人超时时间(s)
        private int nRobotFingerSpeed;                      // 机器人速度：1-100


        private bool bScanPalletEN;                         // 扫夹具条码使能
        private bool bScanBatteryEN;                         // 假电池扫码使能
        private string strScanIP;                           // 扫码IP
        private int nScanPort;                              // 扫码端口
        private int nScanTimes;                             // 扫码次数：=0,不扫码；>0,扫码
        private bool bOnlFakePat;                           // 强制为假电池托盘
        private bool bOnlNomalPat;                          // 强制为正常托盘
        private int nCreatePat;                             // 创建托盘
        private int nCreatePatBat;                          // 创建托盘电池
        private int nReleasePat;                            // 清除托盘
        private int nFakeRow;                               // 夹具放假电池行
        private int nFakeCol;                               // 夹具放假电池列

        private bool bOnloadClear;                          // 上料清尾料功能
        private bool bClearFake;                            // 清尾料料盘类型
        private int nClearPaller;                           // 清尾料料盘号
        // 【模组数据】
        private ActionInfo PickAction;                      // 取动作信息
        private ActionInfo PlaceAction;                     // 放动作信息
        private ModuleEvent curEvent;                       // 当前信号（临时使用）
        private EventState curEventState;                   // 信号状态（临时使用）
        private int nEventRowIdx;                           // 信号行索引（临时使用）
        private int nEventColIdx;                           // 信号列索引（临时使用）
        private int nCurScanCount;                          // 当前扫码次数（临时使用）
        private int nCurPalletIdx;                          // 当前夹具索引
        private int nCurRebakePlt;                          // 当前回炉托盘
        private int nCurAvoidPlt;                           // 当前避让托盘
        private ModuleEvent curAvoidEvent;                  // 当前避让信号

        private int nRobotID;                               // 机器人ID
        private int[] arrRobotCmd;                          // 机器人命令
        public RobotClient robotClient { get; set; }                    // 机器人客户端
        private RobotActionInfo robotAutoInfo;              // 机器人自动模式动作信息
        private RobotActionInfo robotDebugInfo;             // 机器人手动模式动作信息
        private RobotActionInfo robotRecvInfo;              // 机器人手动模式接收动作信息

        private Dictionary<int, RobotFormula> robotStationInfo;  // 机器人工位信息

        private ScanCode[] ScanCodeClient;                    // 扫码枪客户端
        private bool[] bBindingFlag;                        // 电芯绑定标志
        public bool bRobotSafeEvent;                        // 机器人安全信号
        public bool bRobotCrash;                            // 机器人碰撞
        public bool robotProcessingFlag;                    // 机器人运行标志
        private bool connectState;      // 机器人连接状态(界面显示)
        private bool scanconnectState;      // 机器人扫码枪连接状态(界面显示)

        public bool ConnectState
        {
            get { return connectState; }
            set { SetProperty(ref connectState, value); }
        }
        public bool ScanconnectState
        {
            get { return scanconnectState; }
            set { SetProperty(ref scanconnectState, value); }
        }
        public Dictionary<int, RobotInfoStation> RobotStationInfo { get; } = new();
        public bool RobotCrash { get { return bRobotCrash; } set { value = bRobotCrash; } }

        int IRobot.Finger_All => (int)ModuleDef.Finger_All;

        bool IRobot.RobotProcessingFlag => robotProcessingFlag;
        #endregion


        #region // 构造函数

        public RunProOnloadRobot(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject((int)ModuleMaxPallet.OnloadRobot, (int)ModuleDef.Finger_Count, 1, (int)ModuleEvent.OnloadEventEnd);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("RobotEN", "机器人使能", "TRUE启用，FALSE禁用", bRobotEN);
            InsertPrivateParam("RobotIP", "机器人IP", "机器人IP", strRobotIP);
            InsertPrivateParam("RobotPort", "机器人端口", "机器人通讯端口号", nRobotPort);
            InsertPrivateParam("RobotSpeed", "机器人速度", "机器人速度为：1~100", nRobotSpeed, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("RobotTimeout", "机器人超时", "机器人超时时间(s)", nRobotTimeout);
            InsertPrivateParam("RobotFingerSpeed", "自动运行空夹爪机器人速度", "自动运行机器人空夹爪速度为：1~100", nRobotFingerSpeed, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("ScanPalletEN", "托盘扫码使能", "TRUE扫码，FALSE禁用", bScanPalletEN);
            InsertPrivateParam("ScanBatteryEN", "假电池扫码使能", "TRUE扫码，FALSE禁用", bScanBatteryEN);
            InsertPrivateParam("OnlFakePat", "上假电池托盘", "上假电池托盘模式：TRUE启用，FALSE禁用", bOnlFakePat);
            InsertPrivateParam("OnlNomalPat", "上正常托盘", "上正常托盘模式：TRUE启用，FALSE禁用", bOnlNomalPat);
            InsertPrivateParam("CreatePat", "创建托盘", "创建托盘：0~2号托盘", nCreatePat);
            InsertPrivateParam("CreatePatBat", "创建托盘电池", "创建托盘电池：0~2号托盘", nCreatePatBat);
            InsertPrivateParam("ReleasePat", "清除托盘", "清除托盘：0~2号托盘", nReleasePat);
            InsertPrivateParam("FakeRow", "假电池行", "指示上假电池的行号", nFakeRow);
            InsertPrivateParam("FakeCol", "假电池列", "指示上假电池的列号", nFakeCol);

            InsertPrivateParam("ScanIP", "扫码IP", "扫码IP", strScanIP);
            InsertPrivateParam("ScanPort", "扫码端口", "扫码通讯端口号", nScanPort);
            InsertPrivateParam("ScanTimes", "扫码次数", "扫码次数： = 0,不扫码； > 0,扫码", nScanTimes);

            InsertPrivateParam("OnloadClear", "上料清尾料使能", "TRUE启用，FALSE禁用", bOnloadClear);
            InsertPrivateParam("ClearFake", "清尾料料盘类型", "类型 ：True, 假电池料盘; False, 正常料盘", bClearFake);
            InsertPrivateParam("ClearPaller", "清尾料料盘号", "料盘： = 0 - 2", nClearPaller);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IOpen = new int[(int)ModuleDef.Finger_Count];
            IClose = new int[(int)ModuleDef.Finger_Count];
            IFingerCheck = new int[(int)ModuleDef.Finger_Count];
            IPltLeftCheck = new int[3];
            IPltRightCheck = new int[3];
            IPltHasCheck = new int[3];
            IPltCheckNG = new int[3];
            IPltCheckOK = new int[3];
            OOpen = new int[(int)ModuleDef.Finger_Count];
            OClose = new int[(int)ModuleDef.Finger_Count];
            MotorU = -1;
            bBindingFlag = new bool[(int)ModuleDef.Finger_Count];

            for (int nIdx = 0; nIdx < 3; nIdx++)
            {
                IPltLeftCheck[nIdx] = -1;
                IPltRightCheck[nIdx] = -1;
                IPltCheckOK[nIdx] = -1;
                IPltCheckNG[nIdx] = -1;
            }

            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
            {
                IOpen[nIdx] = -1;
                IClose[nIdx] = -1;
                IFingerCheck[nIdx] = -1;
                OOpen[nIdx] = -1;
                OClose[nIdx] = -1;
                bBindingFlag[nIdx] = false;
            }

            // 模组参数
            bRobotEN = false;
            strRobotIP = "";
            nRobotPort = 0;
            nRobotSpeed = 10;
            nRobotTimeout = 30;
            nRobotFingerSpeed = 10;
            ConnectState = false;
            ScanconnectState = false;

            bScanPalletEN = false;
            bScanBatteryEN = false;
            bOnlFakePat = false;
            bOnlNomalPat = false;
            nCreatePat = -1;
            nCreatePatBat = -1;
            nFakeRow = 1;
            nFakeCol = 1;

            strScanIP = "";
            nScanPort = 0;

            bOnloadClear = false;
            bClearFake = false;
            nClearPaller = -1;
            nReleasePat = -1;
            // 模组数据
            arrRobotCmd = new int[10];
            robotClient = new RobotClient();
            robotAutoInfo = new RobotActionInfo();
            robotDebugInfo = new RobotActionInfo();
            robotRecvInfo = new RobotActionInfo();
            robotStationInfo = new Dictionary<int, RobotFormula>();
            //ScanCodeClient = new ScanCode();
            bRobotSafeEvent = false;
            bRobotCrash = false;
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

            // 模组数据
            ScanCodeClient = MachineCtrl.Ioc.Resolve<IEnumerable<ScanCode>>().Where(scan => scan.Parent == null).Take(1).ToArray();
            this.ScanCodeClient[0].Name = $"{this.RunName}_扫码枪";
            ScanCodeClient[0].GetIpPort = () => (strScanIP, nScanPort);
            ScanCodeClient[0].Parent = this;
            // 添加IO/电机
            MotorAdd("MotorU", ref MotorU);

            for (int nIdx = 0; nIdx < 3; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IPltLeftCheck" + strIndex, ref IPltLeftCheck[nIdx]);
                InputAdd("IPltRightCheck" + strIndex, ref IPltRightCheck[nIdx]);
                InputAdd("IPltHasCheck" + strIndex, ref IPltHasCheck[nIdx]);
            }

            for (int nIdx = 0; nIdx < 3; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IPltCheckOK" + strIndex, ref IPltCheckOK[nIdx]);
                InputAdd("IPltCheckNG" + strIndex, ref IPltCheckNG[nIdx]);
            }

            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IOpen" + strIndex, ref IOpen[nIdx]);
                InputAdd("IClose" + strIndex, ref IClose[nIdx]);
                InputAdd("IFingerCheck" + strIndex, ref IFingerCheck[nIdx]);
                OutputAdd("OOpen" + strIndex, ref OOpen[nIdx]);
                OutputAdd("OClose" + strIndex, ref OClose[nIdx]);
            }

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
                        this.nextInitStep = InitSteps.Init_RobotConnect;
                        break;
                    }
                case InitSteps.Init_RobotConnect:
                    {
                        CurMsgStr("连接机器人", "Connect robot");

                        if (RobotConnect())
                        {
                            this.nextInitStep = InitSteps.Init_RobotHome;
                        }
                        break;
                    }
                case InitSteps.Init_RobotHome:
                    {
                        CurMsgStr("机器人回零", "Robot home");

                        if (RobotHome())
                        {
                            this.nextInitStep = InitSteps.Init_CheckFinger;
                        }
                        break;
                    }
                case InitSteps.Init_CheckFinger:
                    {
                        CurMsgStr("检查抓手感应器", "Check finger sensor");

                        for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                        {
                            ModuleDef finger = (ModuleDef)(0x01 << i);
                            if (!FingerCheck(finger, FingerBat(finger).Type > BatType.Invalid))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckPallet;
                        break;
                    }
                case InitSteps.Init_CheckPallet:
                    {
                        CurMsgStr("检查夹具感应器", "Check pallet sensor");

                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            if (!CheckPallet(nPltIdx, Pallet[nPltIdx].Type > PltType.Invalid))
                            {
                                break;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_MotorHome;
                        break;
                    }
                case InitSteps.Init_MotorHome:
                    {
                        CurMsgStr("电机回零", "Motor home");

                        if (this.MotorU < 0 || MotorHome(this.MotorU))
                        {
                            ScanConnect(false);
                            this.nextInitStep = InitSteps.Init_ScannerConnect;
                        }
                        break;
                    }
                case InitSteps.Init_ScannerConnect:
                    {
                        CurMsgStr("连接扫码枪", "Connect scanner");
                        if (ScanConnect())
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
                Sleep(10);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(50);
            }
            ScanIsConnect();
            switch ((AutoSteps)this.nextAutoStep)
            {
                #region // 信号发送和响应

                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        /////////////////////////////////////////////////////////////////////////////////////////
                        // 信号发送响应
                        for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.Pallet_All; nPltIdx++)
                        {
                            if (Pallet[nPltIdx].IsType(PltType.Invalid))
                            {
                                //if ((int)ModuleDef.Pallet_2 != nPltIdx)
                                {
                                    // 放：空托盘
                                    if (GetEvent(this, ModuleEvent.OnloadPlaceEmptyPallet, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                    {
                                        SetEvent(this, ModuleEvent.OnloadPlaceEmptyPallet, EventState.Require);
                                    }

                                    // 放：回炉托盘（重新上假电池）
                                    if (GetEvent(this, ModuleEvent.OnloadPlaceRebakingFakePlt, ref curEventState) &&
                                        (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                    {
                                        SetEvent(this, ModuleEvent.OnloadPlaceRebakingFakePlt, EventState.Require);
                                    }
                                }

                                // 放：NG非空托盘
                                if ((int)ModuleDef.Pallet_2 == nPltIdx)
                                {
                                    if (GetEvent(this, ModuleEvent.OnloadPlaceNGPallet, ref curEventState) &&
                                        (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                    {
                                        SetEvent(this, ModuleEvent.OnloadPlaceNGPallet, EventState.Require);
                                    }
                                }
                            }

                            // 取：回炉托盘
                            else if (Pallet[nPltIdx].IsType(PltType.WaitRebakingToOven)/* && PltIsFull(Pallet[nPltIdx])*/)
                            {
                                if (GetEvent(this, ModuleEvent.OnloadPickRebakingFakePlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OnloadPickRebakingFakePlt, EventState.Require);
                                }
                            }

                            // 取：NG空托盘
                            else if (Pallet[nPltIdx].IsType(PltType.NG) && PltIsEmpty(Pallet[nPltIdx]))
                            {
                                if (GetEvent(this, ModuleEvent.OnloadPickNGEmptyPallet, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OnloadPickNGEmptyPallet, EventState.Require);
                                }
                            }

                            // 取：上料完成托盘
                            else if (Pallet[nPltIdx].IsType(PltType.OK) && PltIsFull(Pallet[nPltIdx]))
                            {
                                // 设置阶段
                                if (Pallet[nPltIdx].IsStage(PltStage.Invalid))
                                {
                                    Pallet[nPltIdx].Stage |= PltStage.Onload;
                                    _ = Pallet[nPltIdx].WhetherThereAreIsEmpty;
                                    SaveRunData(SaveType.Pallet, nPltIdx);
                                }

                                if (PltHasTypeBat(Pallet[nPltIdx], BatType.Fake))
                                {
                                    // 带假电池托盘
                                    if (GetEvent(this, ModuleEvent.OnloadPickOKFakeFullPallet, ref curEventState) &&
                                        (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                    {
                                        SetEvent(this, ModuleEvent.OnloadPickOKFakeFullPallet, EventState.Require);
                                    }
                                }
                                else
                                {
                                    // 不带假电池托盘
                                    if (GetEvent(this, ModuleEvent.OnloadPickOKFullPallet, ref curEventState) &&
                                        (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                    {
                                        SetEvent(this, ModuleEvent.OnloadPickOKFullPallet, EventState.Require);
                                    }
                                }
                            }
                        }


                        // 信号响应
                        for (ModuleEvent eventIdx = ModuleEvent.OnloadPlaceEmptyPallet; eventIdx < ModuleEvent.OnloadEventEnd; eventIdx++)
                        {
                            if (GetEvent(this, eventIdx, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
                                (EventState.Response == curEventState || EventState.Ready == curEventState))
                            {
                                curAvoidEvent = eventIdx;
                                nCurAvoidPlt = nEventColIdx;
                                this.nextAutoStep = AutoSteps.Auto_RobotAvoidMove;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }

                        RunProTransferRobot TransferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
                        bool bSave = TransferRobot.bOnloadRobotSafeEvent;
                        if (bSave)
                        {
                            if (!this.bRobotSafeEvent)
                            {
                                if (RobotMove(OnloadRobotStation.OnloadLine, 0, 0, nRobotSpeed, RobotAction.MOVE))
                                {
                                    this.bRobotSafeEvent = true;
                                }
                            }
                            return;
                        }
                        else
                        {
                            this.bRobotSafeEvent = false;
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

                        if (nCreatePatBat >= 0 && nCreatePatBat < (int)ModuleDef.Pallet_All)
                        {
                            nCreatePatBat = -1;
                            SaveParameter();
                        }

                        if (nReleasePat >= 0 && nReleasePat < (int)ModuleDef.Pallet_All)
                        {
                            Pallet[nReleasePat].Release();
                            SaveRunData(SaveType.Pallet, nReleasePat);

                            nReleasePat = -1;
                            SaveParameter();
                        }

                        /////////////////////////////////////////////////////////////////////////////////////////
                        // 上料处理


                        // 扫托盘条码
                        if (CalcScanCodePos(ref PickAction))
                        {
                            this.AutoStepSafe = false;
                            this.nextAutoStep = AutoSteps.Auto_PalletScanCodeMove;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            break;
                        }
                        // 有回炉托盘
                        else if (HasRebakingPlt(ref nCurRebakePlt))
                        {
                            if (CalcPickRebakingFakePos(nCurRebakePlt, ref PickAction))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_FakePosSetEvent;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                        }
                        // 放电池托盘  
                        else if (HasOnloadPlt(ref nCurPalletIdx))
                        {
                            // 取假电池
                            if (CalcPickFakePos(nCurPalletIdx, ref PickAction))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_FakePosSetEvent;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                            // 取Ng料盘
                            else if (CalcPickNgPalletPos(ref PickAction))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_NgPalletPickMove;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                            // 取来料线电池
                            else if (CalcPickOnloadlinePos(nCurPalletIdx, ref PickAction))
                            {
                                this.AutoStepSafe = false;
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosSetEvent;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                        }

                        /////////////////////////////////////////////////////////////////////////////////////////
                        //上料清尾料模式
                        if (bOnloadClear)
                        {
                            if (nClearPaller >= 0 && nClearPaller < (int)ModuleDef.Pallet_All)
                            {
                                if (bClearFake)
                                {
                                    Pallet[nClearPaller].Bat[0, 0].Type = BatType.Fake;
                                }

                                if (!Pallet[nClearPaller].IsFull())
                                {
                                    Pallet[nClearPaller].FillPltBat();
                                    SaveRunData(SaveType.Pallet, nClearPaller);
                                }
                                bOnloadClear = false;
                                nClearPaller = -1;
                                bClearFake = false;
                                SaveParameter();
                            }
                        }

                        // 无任务时回零位
                        if (!this.AutoStepSafe)
                        {
                            if (RobotMove(OnloadRobotStation.OnloadLine, 0, 0, nRobotFingerSpeed, RobotAction.MOVE, MotorPosition.Onload_LinePickPos))
                            {
                                this.AutoStepSafe = true;
                            }
                        }
                        break;
                    }

                #endregion


                #region // 机器人避让

                case AutoSteps.Auto_RobotAvoidMove:
                    {
                        CurMsgStr("机器人移动到避让位", "Robot avoid move");

                        if (CheckEvent(this, curAvoidEvent, EventState.Response))
                        {
                            if (RobotMove(OnloadRobotStation.OnloadLine, 0, 0, nRobotSpeed, RobotAction.MOVE, MotorPosition.Onload_LinePickPos))
                            {
                                this.bRobotSafeEvent = true;
                                SetEvent(this, curAvoidEvent, EventState.Ready, nEventRowIdx, nEventColIdx, -1, this.RunName);
                                this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinished:
                    {
                        CurMsgStr("等待调度机器人动作完成", "Wait transfer robot action finished");

                        if (CheckEvent(this, curAvoidEvent, EventState.Finished))
                        {
                            this.bRobotSafeEvent = false;
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 托盘扫码

                case AutoSteps.Auto_PalletScanCodeMove:
                    {
                        this.msgChs = string.Format("机器人移动到夹具扫码位[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.MOVE, PickAction.motorPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletScanCodeDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeDown:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]下降", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] down", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckPallet((int)(PickAction.station - OnloadRobotStation.PltScanCode_0), true) &&
                            FingerCheck(ModuleDef.Finger_All, false) && FingerClose(ModuleDef.Finger_All, false))
                        {
                            if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.DOWN))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletScanCodeAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeAction:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]扫码", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] scan code", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        string str = "";
                        if (ScanSend(ref str))
                        {
                            int nPltIdx = (int)(PickAction.station - OnloadRobotStation.PltScanCode_0);

                            Pallet[nPltIdx].Code = str.Replace("\r", "").Replace("\n", "");
                            this.nextAutoStep = AutoSteps.Auto_PalletScanCodeUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet, nPltIdx);
                        }
                        else
                        {
                            if (ButtonResult.OK == ShowMsgBox.ShowDialog("夹具扫码失败，确定：料盘打NG，取消：继续扫码", MessageType.MsgQuestion).Result)
                            {
                                Pallet[(int)(PickAction.station - OnloadRobotStation.PltScanCode_0)].Type = PltType.NG;
                                this.nextAutoStep = AutoSteps.Auto_PalletScanCodeUp;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletScanCodeUp:
                    {
                        this.msgChs = string.Format("机器人到夹具扫码位[{0}-{1}行-{2}列]上升", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot pick pos[{0}-{1}row-{2}col] Up", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_MesCheckJigVaild;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_MesCheckJigVaild:
                    {
                        CurMsgStr("MES托盘校验", "Check JIG Vaild");

                        string strMsg = "", strErr = "";
                        int nPltIdx = (int)(PickAction.station - OnloadRobotStation.PltScanCode_0);
                        if (false && Pallet[nPltIdx].Type == PltType.OK)
                        {
                            Pallet[nPltIdx].Type = PltType.NG;
                            strMsg = string.Format("MES托盘校验失败，托盘条码：{0}，失败原因：{1}", Pallet[nPltIdx].Code, strErr);
                            ShowMessageBox((int)MsgID.MesCheckPlt, strMsg, "MES异常！！！请在D盘MesLog文件中查看具体报警代码信息 ", MessageType.MsgWarning, 5);
                            break;
                        }
                        this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                        SaveRunData(SaveType.AutoStep | SaveType.Pallet, nPltIdx);
                        break;
                    }
                #endregion


                #region // 取：来料线

                case AutoSteps.Auto_OnlinePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人取移动到取来料线[{0}-{1}行-{2}列]前发送信号", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Set event before robot goto pick pos[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadLine, ModuleEvent.OnloadLinePickBattery, EventState.Require))
                        {
                            if (SetEvent(onloadLine, ModuleEvent.OnloadLinePickBattery, EventState.Response, 0, 0, -1, this.RunName))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosPickMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickMove:
                    {
                        this.msgChs = string.Format("机器人取移动到取来料线[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot pick　goto pick pos[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(ModuleDef.Finger_All, false))
                        {
                            if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.MOVE, PickAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OnlinePosPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickDown:
                    {
                        this.msgChs = string.Format("机器人取来料线[{0}-{1}行-{2}列]下降", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] down", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadLine, ModuleEvent.OnloadLinePickBattery, EventState.Ready))
                        {
                            if (FingerCheck(ModuleDef.Finger_All, false) && FingerClose(ModuleDef.Finger_All, false) &&
                                CheckStation((int)PickAction.station, PickAction.row, PickAction.col, (int)PickAction.finger, PickAction.fingerClose))
                            {
                                if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.DOWN))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_OnlinePosFingerAction;
                                    onloadLine.bPickLineDown = true;
                                    onloadLine.SaveRunData(SaveType.Variables);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosFingerAction:
                    {
                        this.msgChs = string.Format("取来料线抓手关闭[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Finger close[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose) && FingerClose(PickAction.finger, PickAction.fingerClose))
                        {
                            if (null != onloadLine)
                            {
                                int nIndex = (int)PickAction.finger;
                                if (onloadLine.nCurNGGroup > -1)
                                {
                                    //来料线4个NG
                                    int nNGGroup = -1;
                                    int nNGGroup1 = -1;
                                    if ((onloadLine.nCurNGGroup == 0) && onloadLine.HasNGColData(0, ref nNGGroup1) && onloadLine.HasNGColData(2, ref nNGGroup))
                                    {
                                        for (int nFingerIdx = 0; nFingerIdx < 4; nFingerIdx++)
                                        {
                                            Battery[nFingerIdx, 0].CopyFrom(onloadLine.Battery[0, nFingerIdx]);
                                            onloadLine.Battery[0, nFingerIdx].Release();
                                        }
                                    }
                                    else
                                    {
                                        for (int nFingerIdx = 0; nFingerIdx < 2; nFingerIdx++)
                                        {
                                            Battery[nFingerIdx, 0].CopyFrom(onloadLine.Battery[0, onloadLine.nCurNGGroup + nFingerIdx]);
                                            onloadLine.Battery[0, onloadLine.nCurNGGroup + nFingerIdx].Release();
                                        }
                                    }
                                }
                                else
                                {
                                    for (int nFingerIdx = 0; nFingerIdx < (int)ModuleDef.Finger_Count; nFingerIdx++)
                                    {
                                        if (1 == (nIndex & 0x01))
                                        {
                                            if (PickAction.col != 2)//判断夹爪1放的位置是否放来料线的第三列,否则进if，是则进else
                                            {
                                                Battery[nFingerIdx, 0].CopyFrom(onloadLine.Battery[0, nFingerIdx]);
                                                onloadLine.Battery[0, nFingerIdx].Release();
                                            }
                                            else
                                            {
                                                Battery[nFingerIdx, 0].CopyFrom(onloadLine.Battery[0, nFingerIdx + 2]);
                                                onloadLine.Battery[0, nFingerIdx + 2].Release();
                                            }
                                        }
                                        nIndex = nIndex >> 1;
                                    }
                                }
                                onloadLine.SaveRunData(SaveType.Battery);
                            }
                            this.nextAutoStep = AutoSteps.Auto_OnlinePosPickUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosPickUp:
                    {
                        this.msgChs = string.Format("机器人取来料线[{0}-{1}行-{2}列]上升", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] up", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OnlinePosCheckFinger;
                            onloadLine.bPickLineDown = false;
                            onloadLine.SaveRunData(SaveType.Variables);
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OnlinePosCheckFinger:
                    {
                        this.msgChs = string.Format("来料线[{0}-{1}行-{2}列]取料检查抓手", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Pick pos[{0}-{1}row-{2}col] check finger", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose))
                        {
                            if (CheckEvent(onloadLine, ModuleEvent.OnloadLinePickBattery, EventState.Ready))
                            {
                                SetEvent(onloadLine, ModuleEvent.OnloadLinePickBattery, EventState.Finished);
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }

                #endregion


                #region // 取：假电池扫码

                case AutoSteps.Auto_FakePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到假电池扫码位[{0}-{1}行-{2}列]前发送信号", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Set event before robot goto pick pos[{0}-{1}row-{2}col]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Require))
                        {
                            if (SetEvent(this.onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Response, PickAction.row, PickAction.col, -1, this.RunName))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakeScanCodeMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanCodeMove:
                    {
                        this.msgChs = string.Format("机器人移动到假电池扫码位[{0}-{1}行-{2}列]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot move to fake scan code pos[{0}-{1}row-{2}col]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(ModuleDef.Finger_All, false))
                        {
                            int nFakePos = PickAction.col;
                            if (RobotMove(OnloadRobotStation.FakeScanCode, PickAction.row, nFakePos, nRobotFingerSpeed, RobotAction.MOVE, PickAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakeScanCodeDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanCodeDown:
                    {
                        this.msgChs = string.Format("机器人假电池扫码位下降[{0}-{1}行-{2}列]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot fake scan code down[{0}-{1}row-{2}col]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Ready) && FingerCheck(ModuleDef.Finger_All, false))
                        {
                            if (FingerCheck(ModuleDef.Finger_All, false) && FingerClose(ModuleDef.Finger_All, false)
                                && CheckStation((int)PickAction.station, PickAction.row, PickAction.col, (int)PickAction.finger, PickAction.fingerClose))
                            {
                                int nFakePos = PickAction.col;
                                if (RobotMove(OnloadRobotStation.FakeScanCode, PickAction.row, nFakePos, nRobotFingerSpeed, RobotAction.DOWN))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_FakeScanCodeAction;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanCodeAction:
                    {
                        this.msgChs = string.Format("机器人假电池扫码[{0}-{1}行-{2}列]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot fake scan code[{0}-{1}row-{2}col]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        // 扫码（多次扫码）
                        string str = "";
                        if (ScanSendBattery(ref str) && (str.Length >= 5))
                        {
                            if (CheckFakeCode(str))
                            {
                                for (int nColIdx = 0; nColIdx < 4; nColIdx++)
                                {
                                    if (onloadFake.Battery[0, nColIdx].IsType(BatType.Fake))
                                    {
                                        onloadFake.Battery[0, nColIdx].Code = str;
                                        break;
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_FakeScanCodeUp;
                                onloadFake.SaveRunData(SaveType.Battery);
                                SaveRunData(SaveType.AutoStep);
                            }
                            else
                            {
                                for (int nColIdx = 0; nColIdx < 4; nColIdx++)
                                {
                                    if (onloadFake.Battery[0, nColIdx].IsType(BatType.Fake))
                                    {
                                        onloadFake.Battery[0, nColIdx].Type = BatType.NG;
                                        this.nextAutoStep = AutoSteps.Auto_FakeScanCodeUp;
                                        onloadFake.SaveRunData(SaveType.Battery);
                                        SaveRunData(SaveType.AutoStep);
                                        break;
                                    }
                                }
                                //ShowMsgBox.ShowDialog("假电池水含量未上传，假电池打NG", MessageType.MsgMessage,0 ,DialogResult.OK);
                            }

                        }
                        else
                        {
                            for (int nColIdx = 0; nColIdx < 4; nColIdx++)
                            {
                                if (onloadFake.Battery[0, nColIdx].IsType(BatType.Fake))
                                {
                                    onloadFake.Battery[0, nColIdx].Type = BatType.NG;
                                    this.nextAutoStep = AutoSteps.Auto_FakeScanCodeUp;
                                    onloadFake.SaveRunData(SaveType.Battery);
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakeScanCodeUp:
                    {
                        this.msgChs = string.Format("机器人假电池扫码位上升[{0}-{1}行-{2}列]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot fake scan code up[{0}-{1}row-{2}col]", GetStationName(OnloadRobotStation.FakeScanCode), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        int nFakePos = PickAction.col;
                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(OnloadRobotStation.FakeScanCode, PickAction.row, nFakePos, nRobotFingerSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_FakePosPickMove;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 取：假电池线

                case AutoSteps.Auto_FakePosPickMove:
                    {
                        this.msgChs = string.Format("机器人取移动到假电池位[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(ModuleDef.Finger_All, false))
                        {
                            if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.MOVE, PickAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_FakePosPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosPickDown:
                    {
                        this.msgChs = string.Format("机器人取假电池位[{0}-{1}行-{2}列]下降", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] down", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Ready))
                        {
                            if (FingerCheck(ModuleDef.Finger_All, false) && FingerClose(ModuleDef.Finger_All, false) &&
                                CheckStation((int)PickAction.station, PickAction.row, PickAction.col, (int)PickAction.finger, PickAction.fingerClose))
                            {
                                if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotSpeed, RobotAction.DOWN))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_FakePosFingerAction;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosFingerAction:
                    {
                        this.msgChs = string.Format("假电池位取料抓手关闭[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Fake battery qick finger close[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose) && FingerClose(PickAction.finger, PickAction.fingerClose))
                        {
                            if (null != onloadFake)
                            {
                                for (int nColIdx = 0; nColIdx < 4; nColIdx++)
                                {
                                    if (onloadFake.Battery[0, nColIdx].IsType(BatType.NG) || onloadFake.Battery[0, nColIdx].IsType(BatType.Fake))
                                    {
                                        Battery[0, 0].CopyFrom(onloadFake.Battery[0, nColIdx]);
                                        onloadFake.Battery[0, nColIdx].Release();
                                        onloadFake.SaveRunData(SaveType.Battery);

                                        this.nextAutoStep = AutoSteps.Auto_FakePosPickUp;
                                        SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosPickUp:
                    {
                        this.msgChs = string.Format("机器人取假电池位[{0}-{1}行-{2}列]上升", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] up", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_FakePosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_FakePosCheckFinger:
                    {
                        this.msgChs = string.Format("假电池位[{0}-{1}行-{2}列]取料后检查抓手", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Pick pos[{0}-{1}row-{2}col] check finger", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose))
                        {
                            if (CheckEvent(onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Ready))
                            {
                                SetEvent(onloadFake, ModuleEvent.OnloadFakePickBattery, EventState.Finished);
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }

                #endregion


                #region // 取：Ng夹具

                case AutoSteps.Auto_NgPalletPickMove:
                    {
                        this.msgChs = string.Format("机器人移动到NG托盘位[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(ModuleDef.Finger_All, false))
                        {
                            if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotFingerSpeed, RobotAction.MOVE, PickAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NgPalletPickDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NgPalletPickDown:
                    {
                        this.msgChs = string.Format("机器人取NG托盘位[{0}-{1}行-{2}列]下降", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] down", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(ModuleDef.Finger_All, false) && FingerClose(ModuleDef.Finger_All, false) &&
                            CheckStation((int)PickAction.station, PickAction.row, PickAction.col, (int)PickAction.finger, PickAction.fingerClose))
                        {
                            if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotSpeed, RobotAction.DOWN))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NgPalletFingerAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NgPalletFingerAction:
                    {
                        this.msgChs = string.Format("NG托盘位取料抓手关闭[{0}-{1}行-{2}列]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Finger close[{0}-{1}row-{2}col]", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        int nIndex = (int)PickAction.finger;
                        bool mesUnBindFlag = true;

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose) && FingerClose(PickAction.finger, PickAction.fingerClose))
                        {
                            for (int nCol = 0; nCol < (int)ModuleDef.Finger_Count; nCol++)
                            {
                                if (1 == (nIndex & 0x01))
                                {
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type == BatType.NG)
                                    {
                                        string strErr = "";
                                        if (true)
                                        {
                                            Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type = BatType.OK;
                                            mesUnBindFlag &= true;
                                        }
                                    }

                                }
                                nIndex = nIndex >> 1;
                            }

                            #region 强制解绑
                            if (!mesUnBindFlag)
                            {
                                UserFormula uesr = new UserFormula();
                                MachineCtrl.GetInstance().dbRecord.GetCurUser(ref uesr);
                                if (uesr.userLevel > UserLevelType.USER_MAINTENANCE)
                                {
                                    ShowMessageBox((int)MsgID.MesUnBindBat, "电芯解绑失败！！！", "请检查托盘、电芯的MES状态或登录维护人员权限强制解绑", MessageType.MsgWarning);
                                    break;
                                }
                                else
                                {
                                    string strInfo = "管理员权限：电芯解绑失败？ 是：手动MES解绑 否：检查托盘的MES状态";

                                    if (ButtonResult.OK != ShowMessageBox((int)RunID.OnloadRobot, strInfo, "请查看干燥炉真空或真空泵状态是否正常", MessageType.MsgQuestion).Result)
                                        break;
                                }
                            }

                            #endregion
                            nIndex = (int)PickAction.finger;
                            for (int nCol = 0; nCol < (int)ModuleDef.Finger_Count; nCol++)
                            {
                                if (1 == (nIndex & 0x01))
                                {
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type == BatType.Fake)
                                    {
                                        Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type = BatType.NG;
                                    }
                                    else if (Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type == BatType.NG)
                                    {
                                        Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Type = BatType.OK;
                                    }
                                    Battery[nCol, 0].CopyFrom(Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol]);
                                    Pallet[(int)ModuleDef.Pallet_2].Bat[PickAction.row, PickAction.col + nCol].Release();
                                }
                                nIndex = nIndex >> 1;
                            }
                            this.nextAutoStep = AutoSteps.Auto_NgPalletPickUp;

                            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.Pallet);
                            break;
                        }
                        break;
                    }
                case AutoSteps.Auto_NgPalletPickUp:
                    {
                        this.msgChs = string.Format("机器人取NG托盘位[{0}-{1}行-{2}列]上升", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Robot goto pick pos[{0}-{1}row-{2}col] up", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (RobotMove(PickAction.station, PickAction.row, PickAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_NgPalletCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_NgPalletCheckFinger:
                    {
                        this.msgChs = string.Format("NG托盘位[{0}-{1}行-{2}列]取料后检查抓手", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        this.msgEng = string.Format("Pick pos[{0}-{1}row-{2}col] check finger", GetStationName(PickAction.station), PickAction.row + 1, PickAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PickAction.finger, PickAction.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 计算放位置

                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc place pos");

                        // 放NG电池
                        if (CalcPlaceNGLinePos(ref PlaceAction))
                        {
                            this.nextAutoStep = AutoSteps.Auto_NGLinePosSetEvent;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 放回炉托盘
                        else if (CalcPlaceRebakingPltPos(nCurRebakePlt, ref PlaceAction))
                        {
                            this.nextAutoStep = AutoSteps.Auto_RebakingPltPosPlaceMove;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 放夹具(放两个)
                        else if (CalcPlaceOKPltPos(nCurPalletIdx, ref PlaceAction, false))
                        {
                            bBindingFlag[0] = false;
                            bBindingFlag[1] = false;
                            bBindingFlag[2] = false;
                            bBindingFlag[3] = false;
                            this.nextAutoStep = AutoSteps.Auto_MesBindJigBat;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 暂存取放
                        else if (CalcBufferPosB(nCurPalletIdx, ref PlaceAction))
                        {
                            Trace.Assert(PlaceAction.col > -1 && PlaceAction.col < 6);
                            this.nextAutoStep = AutoSteps.Auto_BufferPosSetEvent;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 放夹具(放4个)
                        else if (CalcPlaceOKPltPos(nCurPalletIdx, ref PlaceAction, true))
                        {
                            bBindingFlag[0] = false;
                            bBindingFlag[1] = false;
                            bBindingFlag[2] = false;
                            bBindingFlag[3] = false;
                            this.nextAutoStep = AutoSteps.Auto_MesBindJigBat;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        // 抓手为空
                        else if (FingerBat(ModuleDef.Finger_0).IsType(BatType.Invalid) &&
                                 FingerBat(ModuleDef.Finger_1).IsType(BatType.Invalid) &&
                                 FingerBat(ModuleDef.Finger_2).IsType(BatType.Invalid) &&
                                 FingerBat(ModuleDef.Finger_3).IsType(BatType.Invalid))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放：NG线

                case AutoSteps.Auto_NGLinePosSetEvent:
                    {
                        this.msgChs = string.Format("机器人放移动到NG位[{0}-{1}行-{2}列]前发送信号", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Set event before robot goto place NG pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadNG, ModuleEvent.OnloadNGPlaceBattery, EventState.Require))
                        {
                            if (SetEvent(this.onloadNG, ModuleEvent.OnloadNGPlaceBattery, EventState.Response, PlaceAction.row, PlaceAction.col, -1, this.RunName))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceMove:
                    {
                        this.msgChs = string.Format("机器人放移动到NG位[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.MOVE, PlaceAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceDown:
                    {
                        this.msgChs = string.Format("机器人放到NG位[{0}-{1}行-{2}列]下降", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col] down", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (CheckEvent(onloadNG, ModuleEvent.OnloadNGPlaceBattery, EventState.Ready))
                        {
                            if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose)
                                && CheckStation((int)PlaceAction.station, PlaceAction.row, PlaceAction.col, (int)PlaceAction.finger, PlaceAction.fingerClose))
                            {
                                if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.DOWN))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceAction;
                                }
                            }
                        }

                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceAction:
                    {
                        this.msgChs = string.Format("NG位放料抓手打开[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (!PlaceAction.fingerClose && RobotAction.DOWN != robotDebugInfo.action)
                        {
                            ShowMessageBox((int)MsgID.CheckRobotDownPos, "机器人不在下降位，抓手不能打开！！！", "请查看机器人当前位置", MessageType.MsgWarning);
                            break;
                        }

                        if (FingerClose(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            if (null != onloadNG)
                            {
                                int nIndex = (int)PlaceAction.finger;
                                for (int fingerIdx = 0; fingerIdx < (int)ModuleDef.Finger_Count; fingerIdx++)
                                {
                                    if (1 == (nIndex & 0x01) && Battery[fingerIdx, 0].IsType(BatType.NG))
                                    {
                                        onloadNG.Battery[0, fingerIdx % 2].CopyFrom(Battery[fingerIdx, 0]);
                                        Battery[fingerIdx, 0].Release();
                                        MachineCtrl.GetInstance().M_nNgTotal += 1;
                                    }
                                    nIndex = nIndex >> 1;
                                }
                                MachineCtrl.GetInstance().SaveProduceCount();
                                onloadNG.SaveRunData(SaveType.Battery);
                                this.nextAutoStep = AutoSteps.Auto_NGLinePosPlaceUp;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosPlaceUp:
                    {
                        this.msgChs = string.Format("机器人放到NG位[{0}-{1}行-{2}列]上升", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place NG pos[{0}-{1}row-{2}col] up", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_NGLinePosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_NGLinePosCheckFinger:
                    {
                        this.msgChs = string.Format("NG位[{0}-{1}行-{2}列]放料后检查抓手", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            if (CheckEvent(onloadNG, ModuleEvent.OnloadNGPlaceBattery, EventState.Ready))
                            {
                                SetEvent(onloadNG, ModuleEvent.OnloadNGPlaceBattery, EventState.Finished);
                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }

                #endregion


                #region // 暂存取放
                case AutoSteps.Auto_BufferPosSetEvent:
                    {
                        this.msgChs = string.Format("机器人移动到暂存放料位[{0}-{1}行-{2}列]前发送信号", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Set event before robot goto place buffer pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        ModuleEvent nEvent = PlaceAction.fingerClose ? ModuleEvent.OnloadBufPickBattery : ModuleEvent.OnloadBufPlaceBattery;
                        ActionInfo actionInfo = PlaceAction.fingerClose ? PickAction : PlaceAction;
                        if (CheckEvent(onloadBuffer, nEvent, EventState.Require))
                        {
                            if (SetEvent(this.onloadBuffer, nEvent, EventState.Response, actionInfo.row, actionInfo.col, -1, this.RunName))
                            {
                                this.nextAutoStep = AutoSteps.Auto_BufferPosMove;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosMove:
                    {
                        this.msgChs = string.Format("机器人移动到暂存放料位[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place bufffer pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.MOVE, PlaceAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_BufferPosDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosDown:
                    {
                        this.msgChs = string.Format("机器人到暂存放料位[{0}-{1}行-{2}列]下降", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place bufffer pos[{0}-{1}row-{2}col] down", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        ModuleEvent nEvent = PlaceAction.fingerClose ? ModuleEvent.OnloadBufPickBattery : ModuleEvent.OnloadBufPlaceBattery;
                        if (CheckEvent(onloadBuffer, nEvent, EventState.Ready))
                        {
                            if (BufCheck() && FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                            {
                                if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.DOWN))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_BufferPosFingerCheck;
                                }
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_BufferPosFingerCheck:
                    {
                        this.msgChs = string.Format("暂存放料假电池操作[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Fak finger open[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (!PlaceAction.fingerClose && RobotAction.DOWN != robotDebugInfo.action)
                        {
                            ShowMessageBox((int)MsgID.CheckRobotDownPos, "机器人不在下降位，抓手不能打开！！！", "请查看机器人当前位置", MessageType.MsgWarning);
                            break;
                        }

                        ModuleDef fingerIdx = ModuleDef.DefInvalid;

                        //夹爪有假电池
                        if ((FingerHasBatType(BatType.Fake, ref fingerIdx)) && (fingerIdx == ModuleDef.Finger_0))
                        {
                            if (FingerClose(fingerIdx, false))
                            {
                                if (FingerClose(fingerIdx, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                                }
                            }
                        }
                        else
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                        }
                        SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        break;
                    }
                case AutoSteps.Auto_BufferPosFingerAction:
                    {
                        this.msgChs = string.Format("暂存放料抓手操作[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (!PlaceAction.fingerClose && RobotAction.DOWN != robotDebugInfo.action)
                        {
                            ShowMessageBox((int)MsgID.CheckRobotDownPos, "机器人不在下降位，抓手不能打开！！！", "请查看机器人当前位置", MessageType.MsgWarning);
                            break;
                        }

                        if (FingerClose(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            // 取
                            if (PlaceAction.fingerClose)
                            {
                                int nIndex = (int)PlaceAction.finger;
                                for (int nFingerIdx = 0; nFingerIdx < (int)ModuleDef.Finger_Count; nFingerIdx++)
                                {
                                    if (1 == (nIndex & 0x01))
                                    {
                                        Battery[nFingerIdx, 0].CopyFrom(onloadBuffer.Battery[0, PlaceAction.col - 1 + nFingerIdx]);
                                        onloadBuffer.Battery[0, PlaceAction.col - 1 + nFingerIdx].Release();
                                    }
                                    nIndex = nIndex >> 1;
                                }
                            }
                            // 放
                            else
                            {
                                int nIndex = (int)PlaceAction.finger;
                                for (int nFingerIdx = 0; nFingerIdx < (int)ModuleDef.Finger_Count; nFingerIdx++)
                                {
                                    if (1 == (nIndex & 0x01))
                                    {
                                        onloadBuffer.Battery[0, PlaceAction.col - 1 + nFingerIdx].CopyFrom(Battery[nFingerIdx, 0]);

                                        Battery[nFingerIdx, 0].Release();
                                    }
                                    nIndex = nIndex >> 1;
                                }
                            }
                            this.nextAutoStep = AutoSteps.Auto_BufferPosUp;
                            onloadBuffer.SaveRunData(SaveType.Battery);
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosUp:
                    {
                        this.msgChs = string.Format("机器人到放暂存位[{0}-{1}行-{2}列]上升", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place buffer pos[{0}-{1}row-{2}col] up", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosCheckFinger:
                    {
                        this.msgChs = string.Format("暂存位[{0}-{1}行-{2}列]放料后检查抓手", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        ModuleEvent nEvent = PlaceAction.fingerClose ? ModuleEvent.OnloadBufPickBattery : ModuleEvent.OnloadBufPlaceBattery;
                        if (FingerCheck(PlaceAction.finger, PlaceAction.fingerClose) && SetEvent(onloadBuffer, nEvent, EventState.Finished))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion


                #region // 放：托盘
                case AutoSteps.Auto_MesBindJigBat:
                    {
                        this.msgChs = string.Format("MES绑定托盘电芯[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("MES Bind Jig Bat[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        string strErr = "";
                        int pltIndex = PlaceAction.station - OnloadRobotStation.Pallet_0;
                        int nMaxRow = 0, nMaxCol = 0;
                        MachineCtrl.GetInstance().GetPltRowCol(ref nMaxRow, ref nMaxCol);
                        int nCount = CalcPlaceOKPltCount(pltIndex) == (int)ModuleDef.Finger_Count ? 0 : 2;
                        int nIndex = (int)PlaceAction.finger;

                        for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                        {
                            if (BatType.OK == Battery[i, 0].Type && (nIndex & 0x01) == 1)
                            {
                                int nBind = PlaceAction.row * nMaxCol + PlaceAction.col + i;
                                if (MesBindJigBattery(Battery[i, 0].Code, Pallet[pltIndex].Code, PlaceAction.row, PlaceAction.col + i, nBind, ref strErr))
                                {
                                    bBindingFlag[i] = true;
                                }
                                else
                                {
                                    bBindingFlag[i] = false;

                                    UserFormula uesr = new UserFormula();
                                    MachineCtrl.GetInstance().dbRecord.GetCurUser(ref uesr);
                                    if (uesr.userLevel > UserLevelType.USER_MAINTENANCE)
                                    {
                                        ShowMessageBox((int)MsgID.MesUnBindBat, "电芯绑定失败！！！", "请检查托盘、电芯的MES状态或登录维护人员权限强制绑盘", MessageType.MsgWarning);
                                    }
                                    else
                                    {
                                        string strInfo = "管理员权限：电芯绑定失败？ 是：默认绑定成功 否：检查托盘的MES状态";
                                        if (ButtonResult.OK == ShowMessageBox((int)RunID.OnloadRobot, strInfo, "请查看干燥炉真空或真空泵状态是否正常", MessageType.MsgQuestion).Result)
                                            bBindingFlag[i] = true;
                                    }
                                }
                            }
                            else
                            {
                                bBindingFlag[i] = true;
                            }

                            nIndex = nIndex >> 1;
                        }


                        if (bBindingFlag[0] && bBindingFlag[1] && bBindingFlag[2] && bBindingFlag[3])
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPosPlaceMove;
                        }
                        else
                        {
                            if (nCount == 2)
                            {
                                if (bBindingFlag[2] && bBindingFlag[3])
                                {
                                    this.nextAutoStep = AutoSteps.Auto_PalletPosPlaceMove;
                                }
                            }
                            else
                            {
                                this.nextAutoStep = AutoSteps.Auto_MesUnBindBat;
                                SaveRunData(SaveType.Battery);
                            }
                        }
                        SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        break;
                    }
                case AutoSteps.Auto_MesUnBindBat:
                    {
                        this.msgChs = string.Format("MES解绑电芯[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("MES Un Bind Battery[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        string strErr = "";
                        int pltIndex = PlaceAction.station - OnloadRobotStation.Pallet_0;

                        for (int i = 0; i < 4; i++)
                        {
                            if (BatType.Fake == Battery[i, 0].Type)
                            {
                                bBindingFlag[i] = false;
                            }
                            if (bBindingFlag[i] && MesUnBindBattery(Battery[i, 0].Code, Pallet[pltIndex].Code, ref strErr))
                            {
                                bBindingFlag[i] = false;
                            }
                        }
                        if (!bBindingFlag[0] && !bBindingFlag[1] && !bBindingFlag[2] && !bBindingFlag[3])
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (bBindingFlag[i])
                                {
                                    bBindingFlag[i] = false;

                                }
                            }
                            ShowMessageBox((int)MsgID.MesUnBindBat, "电芯解绑失败！！！", "请检查托盘、电芯的MES状态", MessageType.MsgWarning);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPosPlaceMove:
                    {
                        this.msgChs = string.Format("机器人放移动到放料位[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.MOVE, PlaceAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPosPlaceDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPosPlaceDown:
                    {
                        this.msgChs = string.Format("机器人放到放料位[{0}-{1}行-{2}列]下降", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] down", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose) && CheckPallet((int)(PlaceAction.station - OnloadRobotStation.Pallet_0), true))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.DOWN))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PalletPosFingerAction;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPosFingerAction:
                    {
                        this.msgChs = string.Format("托盘放料抓手打开[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (!PlaceAction.fingerClose && RobotAction.DOWN != robotDebugInfo.action)
                        {
                            ShowMessageBox((int)MsgID.CheckRobotDownPos, "机器人不在下降位，抓手不能打开！！！", "请查看机器人当前位置", MessageType.MsgWarning);
                            break;
                        }

                        if (FingerClose(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            int pltIndex = PlaceAction.station - OnloadRobotStation.Pallet_0;

                            int nIndex = (int)PlaceAction.finger;
                            for (int fingerIdx = 0; fingerIdx < (int)ModuleDef.Finger_Count; fingerIdx++)
                            {
                                if (1 == (nIndex & 0x01))
                                {
                                    Pallet[pltIndex].Bat[PlaceAction.row, PlaceAction.col + (int)fingerIdx].CopyFrom(Battery[(int)fingerIdx, 0]);
                                    Battery[(int)fingerIdx, 0].Release();
                                    MachineCtrl.GetInstance().M_nOnloadTotal += 1;
                                    MachineCtrl.GetInstance().m_nOnloadYeuid += 1;
                                }
                                nIndex = nIndex >> 1;
                            }

                            MachineCtrl.GetInstance().SaveProduceCount();
                            this.nextAutoStep = AutoSteps.Auto_PalletPosPlaceUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery | SaveType.Pallet, pltIndex);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPosPlaceUp:
                    {
                        this.msgChs = string.Format("机器人放到放料位[{0}-{1}行-{2}列]上升", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] up", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotFingerSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PalletPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_PalletPosCheckFinger:
                    {
                        this.msgChs = string.Format("托盘位[{0}-{1}行-{2}列]放料后检查抓手", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }

                #endregion


                #region // 放：回炉托盘

                case AutoSteps.Auto_RebakingPltPosPlaceMove:
                    {
                        this.msgChs = string.Format("机器人移动到回炉托盘放料位[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.MOVE, PlaceAction.motorPos))
                            {
                                this.nextAutoStep = AutoSteps.Auto_RebakingPltPosPlaceDown;
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_RebakingPltPosPlaceDown:
                    {
                        this.msgChs = string.Format("机器人放到回炉托盘放料位[{0}-{1}行-{2}列]下降", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] down", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, !PlaceAction.fingerClose))
                        {
                            if (RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.DOWN))
                            {
                                this.nextAutoStep = AutoSteps.Auto_RebakingPltPosFingerAction;
                            }
                        }

                        break;
                    }
                case AutoSteps.Auto_RebakingPltPosFingerAction:
                    {
                        this.msgChs = string.Format("托盘放料抓手打开[{0}-{1}行-{2}列]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Finger open[{0}-{1}row-{2}col]", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (!PlaceAction.fingerClose && RobotAction.DOWN != robotDebugInfo.action)
                        {
                            ShowMessageBox((int)MsgID.CheckRobotDownPos, "机器人不在下降位，抓手不能打开！！！", "请查看机器人当前位置", MessageType.MsgWarning);
                            break;
                        }

                        if (FingerClose(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            int pltIndex = PlaceAction.station - OnloadRobotStation.Pallet_0;
                            Pallet[pltIndex].Bat[PlaceAction.row, PlaceAction.col].CopyFrom(Battery[0, 0]);
                            Pallet[pltIndex].Type = PltType.WaitRebakingToOven;
                            Battery[0, 0].Release();
                            nCurRebakePlt = -1;

                            this.nextAutoStep = AutoSteps.Auto_RebakingPltPosPlaceUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables | SaveType.Battery | SaveType.Pallet, pltIndex);
                        }
                        break;
                    }
                case AutoSteps.Auto_RebakingPltPosPlaceUp:
                    {
                        this.msgChs = string.Format("机器人放到回炉托盘放料位[{0}-{1}行-{2}列]上升", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Robot goto place pos[{0}-{1}row-{2}col] up", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (((int)OnloadRobotStation.Home == robotAutoInfo.Station) ||
                            RobotMove(PlaceAction.station, PlaceAction.row, PlaceAction.col, nRobotSpeed, RobotAction.UP))
                        {
                            this.nextAutoStep = AutoSteps.Auto_RebakingPltPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_RebakingPltPosCheckFinger:
                    {
                        this.msgChs = string.Format("回炉托盘[{0}-{1}行-{2}列]放料后检查抓手", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        this.msgEng = string.Format("Place pos[{0}-{1}row-{2}col] check finger", GetStationName(PlaceAction.station), PlaceAction.row + 1, PlaceAction.col + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        if (FingerCheck(PlaceAction.finger, PlaceAction.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
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
        /// 自动运行开始前机器人位置防呆检查
        /// </summary>
        public bool CheckOnloadRobotPos()
        {
            int info = -1;
            bool infoEn = false;
            //手自动在回零位置，则不判断
            if ((RobotAction.HOME == robotAutoInfo.action) && (RobotAction.HOME == robotDebugInfo.action))
            {
                return true;
            }
            else if ((robotAutoInfo.Station == robotDebugInfo.Station)
                && (robotAutoInfo.Row == robotDebugInfo.Row)
                && (robotAutoInfo.Col == robotDebugInfo.Col))
            {
                if ((robotAutoInfo.action == robotDebugInfo.action)
                    || (robotAutoInfo.action == RobotAction.DOWN && robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.UP && robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.UP && robotDebugInfo.action == RobotAction.DOWN))
                {
                    return true;
                }
                info = (int)RobotAction.MOVE;
                infoEn = true;
            }
            string msg, disp;
            msg = string.Format("机器人动作位置被改变");
            disp = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<{1}-{2}行-{3}列-{4}>\r\n位置，重新停止-复位-启动！"
                , RobotDef.RobotName[0], this.robotAutoInfo.stationName
                , this.robotAutoInfo.Row + 1, this.robotAutoInfo.Col + 1, RobotDef.RobotActionName[infoEn ? info : (int)robotAutoInfo.action]);
            ShowMessageBox((int)MsgID.AutoCheckPosStep, msg, disp, MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            if (robotDebugInfo.action != RobotAction.DOWN)
            {
                for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                {
                    if (output == DeviceManager.Outputs(OOpen[i]) && (InputState(IFingerCheck[i], true)))
                    {
                        string str = "";
                        str = string.Format("\r\n机器人不在下降位置，抓手{0}有电池，是否松开？", i + 1);
                        if (ButtonResult.No == ShowMessageBox((int)RunID.OnloadRobot, str, "请查看干燥炉真空或真空泵状态是否正常", MessageType.MsgQuestion).Result)
                            return false;
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// 检查电机是否可移动
        /// </summary>
        public override bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            //机器人在夹具下降位置，禁止移动U轴
            if (robotDebugInfo.action == RobotAction.DOWN)
            {
                string str = "";
                str = string.Format("\r\n 机器人在【{0}】工位{1}行{2}列下降位置，禁止移动调宽电机！", robotDebugInfo.Station, robotDebugInfo.Row + 1, robotDebugInfo.Col + 1);
                ShowMsgBox.ShowDialog(RunName + str, MessageType.MsgWarning);
                return false;
            }
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
            PickAction.Release();                               // 取动作信息
            PlaceAction.Release();                              // 放动作信息
            nCurPalletIdx = -1;                                 // 当前夹具索引
            nCurRebakePlt = -1;                                 // 当前回炉托盘
            nCurAvoidPlt = -1;                                  // 当前避让托盘
            curAvoidEvent = ModuleEvent.ModuleEventInvalid;     // 当前避让信号

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
            this.nCurPalletIdx = FileStream.ReadInt(section, "nCurPalletIdx", this.nCurPalletIdx);
            this.nCurRebakePlt = FileStream.ReadInt(section, "nCurRebakePlt", this.nCurRebakePlt);
            this.nCurAvoidPlt = FileStream.ReadInt(section, "nCurAvoidPlt", this.nCurAvoidPlt);
            this.curAvoidEvent = (ModuleEvent)FileStream.ReadInt(section, "curAvoidEvent", (int)this.curAvoidEvent);
            this.bBindingFlag[0] = FileStream.ReadBool(section, "bBindingFlag[0]", this.bBindingFlag[0]);
            this.bBindingFlag[1] = FileStream.ReadBool(section, "bBindingFlag[1]", this.bBindingFlag[1]);
            this.bRobotSafeEvent = FileStream.ReadBool(section, "bRobotSafeEvent", this.bRobotSafeEvent);
            this.bRobotCrash = FileStream.ReadBool(section, "bRobotCrash", this.bRobotCrash);

            // 动作信息
            string[] arrName = new string[] { "PickAction", "PlaceAction" };
            ActionInfo[] arrInfo = new ActionInfo[] { PickAction, PlaceAction };

            for (int nIdx = 0; nIdx < arrInfo.Length; nIdx++)
            {
                key = string.Format("{0}.station", arrName[nIdx]);
                arrInfo[nIdx].station = (OnloadRobotStation)FileStream.ReadInt(section, key, (int)arrInfo[nIdx].station);

                key = string.Format("{0}.row", arrName[nIdx]);
                arrInfo[nIdx].row = FileStream.ReadInt(section, key, arrInfo[nIdx].row);

                key = string.Format("{0}.col", arrName[nIdx]);
                arrInfo[nIdx].col = FileStream.ReadInt(section, key, arrInfo[nIdx].col);

                key = string.Format("{0}.finger", arrName[nIdx]);
                arrInfo[nIdx].finger = (ModuleDef)FileStream.ReadInt(section, key, (int)arrInfo[nIdx].finger);

                key = string.Format("{0}.fingerClose", arrName[nIdx]);
                arrInfo[nIdx].fingerClose = FileStream.ReadBool(section, key, arrInfo[nIdx].fingerClose);

                key = string.Format("{0}.motorPos", arrName[nIdx]);
                arrInfo[nIdx].motorPos = (MotorPosition)FileStream.ReadInt(section, key, (int)arrInfo[nIdx].motorPos);
            }

            PickAction = arrInfo[0];
            PlaceAction = arrInfo[1];

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
                FileStream.WriteInt(section, "nCurPalletIdx", this.nCurPalletIdx);
                FileStream.WriteInt(section, "nCurRebakePlt", this.nCurRebakePlt);
                FileStream.WriteInt(section, "nCurAvoidPlt", this.nCurAvoidPlt);
                FileStream.WriteInt(section, "curAvoidEvent", (int)this.curAvoidEvent);
                FileStream.WriteBool(section, "bBindingFlag[0]", this.bBindingFlag[0]);
                FileStream.WriteBool(section, "bBindingFlag[1]", this.bBindingFlag[1]);
                FileStream.WriteBool(section, "bRobotSafeEvent", this.bRobotSafeEvent);
                FileStream.WriteBool(section, "bRobotCrash", this.bRobotCrash);

                // 动作信息
                string[] arrName = new string[] { "PickAction", "PlaceAction" };
                ActionInfo[] arrInfo = new ActionInfo[] { PickAction, PlaceAction };

                for (int nIdx = 0; nIdx < arrInfo.Length; nIdx++)
                {
                    key = string.Format("{0}.station", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrInfo[nIdx].station);

                    key = string.Format("{0}.row", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrInfo[nIdx].row);

                    key = string.Format("{0}.col", arrName[nIdx]);
                    FileStream.WriteInt(section, key, arrInfo[nIdx].col);

                    key = string.Format("{0}.finger", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrInfo[nIdx].finger);

                    key = string.Format("{0}.fingerClose", arrName[nIdx]);
                    FileStream.WriteBool(section, key, arrInfo[nIdx].fingerClose);

                    key = string.Format("{0}.motorPos", arrName[nIdx]);
                    FileStream.WriteInt(section, key, (int)arrInfo[nIdx].motorPos);
                }
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

            for (ModuleEvent eventIdx = ModuleEvent.OnloadPlaceEmptyPallet; eventIdx < ModuleEvent.OnloadEventEnd; eventIdx++)
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
            if (robotDebugInfo.Station != (int)OnloadRobotStation.Home)
            {
                ShowMsgBox.ShowDialog("上料机器人不在回零位，禁止清除任务！\r\n请将上料机器人回零！", MessageType.MsgMessage);
                return false;
            }
            if (!FingerCheck(ModuleDef.Finger_All, false))
            {
                ShowMsgBox.ShowDialog("上料机器人抓手有电池，请人工取走后再清除上料机器人任务", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            PickAction.Release();
            PlaceAction.Release();
            nCurAvoidPlt = -1;
            curAvoidEvent = ModuleEvent.ModuleEventInvalid;
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            // 抓手电池初始化
            if (null != Battery)
            {
                for (int nRowIdx = 0; nRowIdx < Battery.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                    {
                        Battery[nRowIdx, nColIdx].Release();
                    }
                }
            }

            // 信号初始化
            if (null != ArrEvent)
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx, (RunID)this.GetRunID());
                }
            }
            SaveRunData(SaveType.AutoStep | SaveType.SignalEvent | SaveType.Battery | SaveType.Variables);
            return true;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public override bool ClearModuleTask()
        {
            // 上料机器人与调度交互情况
            RunProTransferRobot runTransfer = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
            if (runTransfer.CheckRobotPos((int)TransferRobotStation.OnloadStation))
            {
                ShowMsgBox.ShowDialog("调度在取料，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            if (!CheckModuleEventState())
            {
                string strInfo = string.Format("《上料机器人》与《调度机器人》处于交互中\r\n请点击【确定】将清除《调度机器人》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runTransfer.ClearModuleData())
                    {
                        strInfo = string.Format("上料机器人模组数据清除【失败】！！！《上料机器人》与《调度机器人》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 上料机器人与上料缓存模组交互情况
            RunProcess runOnloadBuffer = MachineCtrl.GetInstance().GetModule(RunID.OnloadBuffer);
            if (!runOnloadBuffer.CheckModuleEventState())
            {
                string strInfo = string.Format("《上料机器人》与《上料缓存模组》处于交互中\r\n请点击【确定】将清除《上料缓存模组》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOnloadBuffer.ClearModuleData())
                    {
                        strInfo = string.Format("上料机器人模组数据清除【失败】！！！《上料机器人》模组与《上料缓存模组》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 上料机器人与上料取料线模组交互情况
            RunProcess runOnloadLine = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine);
            if (!runOnloadLine.CheckModuleEventState())
            {
                string strInfo = string.Format("《上料机器人》与《上料取料线模组》处于交互中\r\n请点击【确定】将清除《上料取料线模组》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOnloadLine.ClearModuleData())
                    {
                        strInfo = string.Format("上料机器人模组数据清除【失败】！！！《上料机器人》模组与《上料取料线模组》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 上料机器人与上料NG输出线模组交互情况
            RunProcess runOnloadNG = MachineCtrl.GetInstance().GetModule(RunID.OnloadNG);
            if (!runOnloadNG.CheckModuleEventState())
            {
                string strInfo = string.Format("《上料机器人》与《上料NG输出线模组》处于交互中\r\n请点击【确定】将清除《上料NG输出线模组》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOnloadNG.ClearModuleData())
                    {
                        strInfo = string.Format("上料机器人模组数据清除【失败】！！！《上料机器人》模组与《上料NG输出线模组》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

            // 上料机器人与上料假电池输入线模组交互情况
            RunProcess runOnloadFake = MachineCtrl.GetInstance().GetModule(RunID.OnloadFake);
            if (!runOnloadFake.CheckModuleEventState())
            {
                string strInfo = string.Format("《上料机器人》与《上料假电池输入线模组》处于交互中\r\n请点击【确定】将清除《上料假电池输入线模组》数据");
                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    if (!runOnloadFake.ClearModuleData())
                    {
                        strInfo = string.Format("上料机器人模组数据清除【失败】！！！《上料机器人》模组与《上料假电池输入线模组》处于交互中!!!");
                        ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                        return false;
                    }
                }
                else return false;
            }

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

            bRobotEN = ReadParam(RunModule, "RobotEN", false);
            strRobotIP = ReadParam(RunModule, "RobotIP", "");
            nRobotPort = ReadParam(RunModule, "RobotPort", 0);
            nRobotSpeed = ReadParam(RunModule, "RobotSpeed", 10);
            nRobotTimeout = ReadParam(RunModule, "RobotTimeout", 30);
            nRobotFingerSpeed = ReadParam(RunModule, "RobotFingerSpeed", 10);

            bScanPalletEN = ReadParam(RunModule, "ScanPalletEN", false);
            bScanBatteryEN = ReadParam(RunModule, "ScanBatteryEN", false);
            bOnlFakePat = ReadParam(RunModule, "OnlFakePat", false);
            bOnlNomalPat = ReadParam(RunModule, "OnlNomalPat", false);
            nCreatePat = ReadParam(RunModule, "CreatePat", -1);
            nCreatePatBat = ReadParam(RunModule, "CreatePatBat", -1);
            nReleasePat = ReadParam(RunModule, "ReleasePat", -1);
            nFakeRow = ReadParam(RunModule, "FakeRow", 1);
            nFakeCol = ReadParam(RunModule, "FakeCol", 1);

            strScanIP = ReadParam(RunModule, "ScanIP", "");
            nScanPort = ReadParam(RunModule, "ScanPort", 0);
            nScanTimes = ReadParam(RunModule, "ScanTimes", 0);

            bOnloadClear = ReadParam(RunModule, "OnloadClear", false);
            bClearFake = ReadParam(RunModule, "ClearFake", false);
            nClearPaller = ReadParam(RunModule, "ClearPaller", 0);

            return true;
        }

        /// <summary>
        /// 写入数据库参数
        /// </summary>
        public override void SaveParameter()
        {
            WriteParameterCode(RunModule, "CreatePat", nCreatePat.ToString());
            WriteParameterCode(RunModule, "CreatePatBat", nCreatePatBat.ToString());
            WriteParameterCode(RunModule, "ReleasePat", nReleasePat.ToString());
            WriteParameterCode(RunModule, "OnloadClear", bOnloadClear.ToString());
            WriteParameterCode(RunModule, "ClearFake", bClearFake.ToString());
            WriteParameterCode(RunModule, "ClearPaller", nClearPaller.ToString());

            base.SaveParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;

            // 来料线
            strValue = IniFile.ReadString(strModule, "OnloadLine", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadLine = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadLine;

            // 假电池线
            strValue = IniFile.ReadString(strModule, "OnloadFake", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadFake = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadFake;

            // NG输出线
            strValue = IniFile.ReadString(strModule, "OnloadNG", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadNG = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadNG;

            // 上料配对
            strValue = IniFile.ReadString(strModule, "OnloadBuffer", "", Def.GetAbsPathName(Def.ModuleExCfg));
            onloadBuffer = MachineCtrl.GetInstance().GetModule(strValue) as RunProOnloadBuffer;
        }

        #endregion


        #region // 取放料计算相关

        /// <summary>
        /// 有上料托盘，并返回托盘索引
        /// </summary>
        private bool HasOnloadPlt(ref int nCurPlt)
        {
            // 索引无效，托盘无效，托盘是满盘，都需要重新搜索新的上料托盘
            if ((nCurPlt < 0) || Pallet[nCurPlt].IsType(PltType.Invalid) || PltIsFull(Pallet[nCurPlt]))
            {
                for (int nPltIdx = 0; nPltIdx < Pallet.Length; nPltIdx++)
                {
                    if (Pallet[nPltIdx].IsType(PltType.OK) && !PltIsFull(Pallet[nPltIdx]))
                    {
                        if (Pallet[nPltIdx].IsEmpty())
                        {
                            CalcIsOnloadFake(nPltIdx);
                        }

                        // 强制为假电池托盘
                        if (bOnlFakePat && !bOnlNomalPat)
                        {
                            Pallet[nPltIdx].IsOnloadFake = true;
                        }
                        // 强制为正常托盘
                        if (bOnlNomalPat && !bOnlFakePat)
                        {
                            Pallet[nPltIdx].IsOnloadFake = false;
                        }

                        nCurPlt = nPltIdx;
                        SaveRunData(SaveType.Pallet, nCurPlt);
                        return true;
                    }
                }
            }
            // 索引有效，有托盘，不是满盘，则继续在该托盘放电池
            else if ((nCurPlt > -1) && Pallet[nCurPlt].IsType(PltType.OK) && !PltIsFull(Pallet[nCurPlt]))
            {
                return true;
            }

            nCurPlt = -1;
            return false;
        }

        /// <summary>
        /// 计算托盘上假电池
        /// </summary>
        private void CalcIsOnloadFake(int nPltIdx)
        {
            int NormalPltCount = 0;
            int FakePltCount = 0;

            for (int i = 0; i < (int)ModuleDef.Pallet_All; i++)
            {
                if (Pallet[i].Type == PltType.OK && !Pallet[i].IsEmpty())
                {
                    if (PltHasTypeBat(Pallet[i], BatType.Fake))
                    {
                        FakePltCount++;
                    }
                    else
                    {
                        NormalPltCount++;
                    }
                }
            }

            //当上料已经有两个假电池托盘时，不再允许增加假电池托盘
            if (FakePltCount == 2)
            {
                Pallet[nPltIdx].IsOnloadFake = false;
                return;
            }

            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;

            if (run.Pallet[0].Type == PltType.OK && !run.Pallet[0].IsEmpty())
            {
                if (PltHasTypeBat(run.Pallet[0], BatType.Fake))
                {
                    FakePltCount++;
                }
                else
                {
                    NormalPltCount++;
                }
            }

            Pallet pallet;
            for (int oven = 0; oven < (int)TransferRobotStation.DryingOven_5; oven++)
            {
                RunProDryingOven ovenData = MachineCtrl.GetInstance().GetModule(RunID.DryOven0 + oven) as RunProDryingOven;

                //模组使能没开直接跳过
                if (!ovenData.IsModuleEnable())
                    continue;
                for (int col = 0; col < (int)ModuleRowCol.DryingOvenCol; col++)
                {
                    //炉腔使能没开直接跳过
                    if (!ovenData.CavityDataSource[col].OvenEnable || ovenData.CavityDataSource[col].State != CavityState.Standby)
                        continue;
                    for (int row = 0; row < (int)ModuleRowCol.DryingOvenRow; row++)
                    {
                        pallet = ovenData.Pallet[col * (int)ModuleRowCol.DryingOvenRow + row];
                        if ((pallet.Type == PltType.OK || pallet.Type == PltType.NG)
                            && !pallet.IsEmpty() && pallet.Bat[0, 0].Type > BatType.Invalid)
                        {
                            if (PltHasTypeBat(pallet, BatType.Fake))
                            {
                                FakePltCount++;
                            }
                            else
                            {
                                NormalPltCount++;
                            }
                        }
                    }
                }
            }
            if ((FakePltCount * 9) <= NormalPltCount)
            {
                Pallet[nPltIdx].IsOnloadFake = true;
            }
            else
            {
                Pallet[nPltIdx].IsOnloadFake = false;
            }
        }

        /// <summary>
        /// 有回炉托盘，并返回托盘索引
        /// </summary>
        private bool HasRebakingPlt(ref int nCurPlt)
        {
            for (int nPltIdx = 0; nPltIdx < Pallet.Length; nPltIdx++)
            {
                if (Pallet[nPltIdx].IsType(PltType.WaitRebakeBat))
                {
                    nCurPlt = nPltIdx;
                    return true;
                }
            }
            nCurPlt = -1;
            return false;
        }

        // ================================ 取料计算 ================================

        /// <summary>
        /// 计算托盘扫码位
        /// </summary>
        private bool CalcScanCodePos(ref ActionInfo info)
        {
            if (bScanPalletEN || Def.IsNoHardware())
            {
                // 检查夹爪是否有电池，带电池不能扫码
                for (ModuleDef idx = ModuleDef.Finger_0; idx < ModuleDef.Finger_All; idx++)
                {
                    if (FingerBat(idx).Type > BatType.Invalid)
                    {
                        return false;
                    }
                }

                // 查看托盘是否需要扫码
                for (int nPltIdx = 0; nPltIdx < Pallet.Length; nPltIdx++)
                {
                    if (Pallet[nPltIdx].IsType(PltType.OK) && ("" == Pallet[nPltIdx].Code)
                        && PltIsEmpty(Pallet[nPltIdx]) /*&& InputState(IPltCheckOK[nPltIdx], true)*/)
                    {
                        info.SetAction(OnloadRobotStation.PltScanCode_0 + nPltIdx, 0, 0, ModuleDef.Finger_All, false, MotorPosition.Onload_ScanPalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取假电池位
        /// </summary>
        private bool CalcPickFakePos(int nCurPlt, ref ActionInfo info)
        {
            if (nCurPlt < 0 || nCurPlt >= Pallet.Length)
            {
                return false;
            }

            int nRowIdx = -1;
            int nColIdx = -1;
            EventState state = EventState.Invalid;
            RunProOnloadFake run = this.onloadFake;

            // 托盘需要假电池 && 暂存中有OK电池
            if (IsOnloadFake(nCurPalletIdx) && onloadBuffer.HasBatCount() > 2)
            {
                if (GetEvent(run, ModuleEvent.OnloadFakePickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                {
                    if (run.Battery[nRowIdx, nColIdx].IsType(BatType.Fake))
                    {
                        // 假电池放在偶数列
                        if (0 == ((nFakeCol - 1) % 2))
                        {
                            info.SetAction(OnloadRobotStation.FakeInput, 0, nColIdx, ModuleDef.Finger_0, true, MotorPosition.Onload_FakePos);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查假电池
        /// </summary>
        private bool CheckFakeCode(string strFakeCode)
        {
            for (int i = 0; i < (int)ModuleDef.Pallet_All; i++)
            {
                if (Pallet[i].Type == PltType.OK && !Pallet[i].IsEmpty())
                {
                    if (PltHasTypeBat(Pallet[i], BatType.Fake) && Pallet[i].Bat[0, 0].Code == strFakeCode)
                    {
                        return false;
                    }
                }
            }

            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;

            if (run.Pallet[0].Type == PltType.OK && !run.Pallet[0].IsEmpty())
            {
                if (PltHasTypeBat(run.Pallet[0], BatType.Fake) && run.Pallet[0].Bat[0, 0].Code == strFakeCode)
                {
                    return false;
                }
            }

            for (int oven = 0; oven < (int)DryingOvenCount.DryingOvenNum; oven++)
            {
                run = MachineCtrl.GetInstance().GetModule(RunID.DryOven0 + oven) as RunProDryingOven;

                for (int row = 0; row < (int)ModuleRowCol.DryingOvenRow; row++)
                {
                    for (int col = 0; col < (int)ModuleRowCol.DryingOvenCol; col++)
                    {
                        if (run.Pallet[row * (int)ModuleRowCol.DryingOvenCol + col].Type == PltType.OK && !run.Pallet[row * (int)ModuleRowCol.DryingOvenCol + col].IsEmpty())
                        {
                            if (PltHasTypeBat(run.Pallet[row * (int)ModuleRowCol.DryingOvenCol + col], BatType.Fake) && run.Pallet[row * (int)ModuleRowCol.DryingOvenCol + col].Bat[0, 0].Code == strFakeCode)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            run = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            for (int i = 0; i < (int)ModuleDef.Pallet_All - 1; i++)
            {
                if (run.Pallet[i].Type == PltType.OK && !run.Pallet[i].IsEmpty())
                {
                    if (PltHasTypeBat(run.Pallet[i], BatType.Fake) && run.Pallet[i].Bat[0, 0].Code == strFakeCode)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 计算取回炉假电池位
        /// </summary>
        private bool CalcPickRebakingFakePos(int nCurPlt, ref ActionInfo info)
        {
            if (nCurPlt < 0 || nCurPlt >= Pallet.Length)
            {
                return false;
            }

            int nRowIdx, nColIdx;
            int nPltFakeRow, nPltFakeCol;
            EventState state = EventState.Invalid;
            RunProOnloadFake run = this.onloadFake;
            nPltFakeRow = nPltFakeCol = -1;
            nRowIdx = nColIdx = -1;

            if (GetEvent(run, ModuleEvent.OnloadFakePickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
            {
                if (PltHasTypeBat(Pallet[nCurPlt], BatType.Fake, ref nPltFakeRow, ref nPltFakeCol))
                {
                    if (run.Battery[nRowIdx, nColIdx].IsType(BatType.Fake))
                    {
                        // 假电池放在偶数列
                        if (0 == (ModuleDef)(nPltFakeCol % 2))
                        {
                            info.SetAction(OnloadRobotStation.FakeInput, 0, nColIdx, ModuleDef.Finger_0, true, MotorPosition.Onload_FakePos);
                        }
                        // 假电池放在奇数列
                        else
                        {
                            info.SetAction(OnloadRobotStation.FakeInput, 0, nColIdx + 1, ModuleDef.Finger_1, true, MotorPosition.Onload_FakePos);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取来料线电池位
        /// </summary>
        private bool CalcPickOnloadlinePos(int nCurPlt, ref ActionInfo info)
        {
            int nRowIdx = -1;
            int nColIdx = -1;
            EventState state = (EventState)0;
            RunProOnloadLine run = this.onloadLine;

            int nRowIdxFake = -1;
            int nColIdxFake = -1;
            EventState stateFake = (EventState)0;
            if (IsOnloadFake(nCurPlt) && onloadBuffer.HasBatCount() > 2)
            {
                return false;
            }

            if (GetEvent(run, ModuleEvent.OnloadLinePickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state
                && onloadBuffer.HasBatCount() < (int)ModuleDef.Finger_Count)
            {
                ModuleDef nIdx = ModuleDef.DefInvalid;
                bool bFind = false;
                int nCol = -1;
                //来料线有NG电池 或者 需要上假电池，暂存不为空
                if (run.HasNGCol() || onloadLine.nCurNGGroup > -1)
                {
                    for (int nTmpIdx = 0; nTmpIdx < 2; nTmpIdx++)
                    {
                        if (run.Battery[0, onloadLine.nCurNGGroup + nTmpIdx].Type > BatType.Invalid)
                        {
                            bFind = true;
                            nCol = 0;
                            if (0 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_0;
                            }
                            else if (1 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_1;
                            }
                        }
                        int nNGGroup = -1;
                        int nNGGroup1 = -1;
                        if ((onloadLine.nCurNGGroup == 0) && run.HasNGColData(0, ref nNGGroup1) && run.HasNGColData(2, ref nNGGroup))
                        {
                            if (run.Battery[0, nNGGroup + nTmpIdx].Type > BatType.Invalid)
                            {
                                bFind = true;
                                if (0 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_2;
                                }
                                else if (1 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_3;
                                }
                            }
                        }
                    }
                }
                //来料线全是OK电池 
                //需要上假电池 暂存不为空 取12号
                else if (PltIsEmpty(Pallet[nCurPlt]) && IsOnloadFake(nCurPlt) && (onloadBuffer.HasBatCount() > 0 && onloadBuffer.HasBatCount() <= 2))
                {
                    for (int nTmpIdx = 0; nTmpIdx < 2; nTmpIdx++)
                    {
                        if (run.Battery[0, nTmpIdx].Type > BatType.Invalid)
                        {
                            bFind = true;
                            nCol = 0;
                            if (0 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_0;
                            }
                            else if (1 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_1;
                            }
                        }
                    }

                    if (!bFind)
                    {
                        for (int nTmpIdx = 2; nTmpIdx < 4; nTmpIdx++)
                        {
                            if (run.Battery[0, nTmpIdx].Type > BatType.Invalid)
                            {
                                bFind = true;
                                nCol = 0;
                                if (2 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_0;
                                }
                                else if (3 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_1;
                                }

                            }
                        }
                        if (bFind)
                        {
                            info.SetAction(OnloadRobotStation.OnloadLine, 0, 2, nIdx, true, MotorPosition.Onload_LinePickPos);
                            return true;
                        }
                    }
                }
                else if (!IsOnloadFake(nCurPlt) && onloadBuffer.HasBatCount() > 1 && onloadBuffer.HasBatCount() <= 3 && onloadLine.Battery.Cast<Battery>().ToList().FindAll(o => o.Type != BatType.OK).Count() > 0)//非空托盘，不需要假电池，暂存位存在3个以内的电池,主要处理存在无效电池的情况
                {
                    for (int nTmpIdx = 0; nTmpIdx < 2; nTmpIdx++)//寻找1,2列电池
                    {
                        if (run.Battery[0, nTmpIdx].Type > BatType.Invalid)
                        {
                            bFind = true;
                            nCol = 0;
                            if (0 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_0;
                            }
                            else if (1 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_1;
                            }
                        }
                    }
                    if (!bFind)
                    {
                        for (int nTmpIdx = 2; nTmpIdx < 4; nTmpIdx++)//寻找3，4列电池
                        {
                            if (run.Battery[0, nTmpIdx].Type > BatType.Invalid)
                            {
                                bFind = true;
                                nCol = 0;
                                if (2 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_0;
                                }
                                else if (3 == nTmpIdx)
                                {
                                    nIdx |= ModuleDef.Finger_1;
                                }
                            }
                        }
                        if (bFind)
                        {
                            info.SetAction(OnloadRobotStation.OnloadLine, 0, 2, nIdx, true, MotorPosition.Onload_LinePickPos);
                            return true;
                        }
                    }

                }
                else
                {
                    for (int nTmpIdx = 0; nTmpIdx < (int)ModuleDef.Finger_Count; nTmpIdx++)
                    {
                        if (run.Battery[0, nTmpIdx].Type > BatType.Invalid)
                        {
                            bFind = true;
                            if (0 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_0;
                            }
                            else if (1 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_1;
                            }
                            else if (2 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_2;
                            }
                            else if (3 == nTmpIdx)
                            {
                                nIdx |= ModuleDef.Finger_3;
                            }
                        }
                    }
                    nCol = 0;
                }
                if (bFind)
                {
                    info.SetAction(OnloadRobotStation.OnloadLine, 0, onloadLine.nCurNGGroup != 2 ? 0 : 2, nIdx, true, MotorPosition.Onload_LinePickPos);
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 计算取Ng托盘位
        /// </summary>
        private bool CalcPickNgPalletPos(ref ActionInfo info)
        {
            if (Pallet[(int)ModuleDef.Pallet_2] == null)
            {
                return false;
            }
            int nPltRow = 0;
            int nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);
            // 托盘需要假电池 && 暂存中有3个OK电池,优先取配对位，不取NG盘
            if (IsOnloadFake(nCurPalletIdx) && onloadBuffer.HasBatCount() > 2)
            {
                return false;
            }
            if (Pallet[(int)ModuleDef.Pallet_2].IsType(PltType.NG) && !PltIsEmpty(Pallet[(int)ModuleDef.Pallet_2]))
            {
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx += (int)ModuleDef.Finger_Count)
                    {
                        // 清除托盘中的填充电池
                        for (int nTmpIdx = 0; nTmpIdx < (int)ModuleDef.Finger_Count; nTmpIdx++)
                        {
                            if (nColIdx + nTmpIdx < nPltCol)
                            {
                                if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].IsType(BatType.BKFill))
                                {
                                    Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].Release();
                                }
                            }
                        }

                        ModuleDef nIdx = ModuleDef.DefInvalid;
                        bool bFind = false;
                        // 计算取电池位置
                        if (nPltCol % 4 == 0)
                        {
                            if (nColIdx + (int)ModuleDef.Finger_Count <= nPltCol)
                            {
                                for (int nTmpIdx = 0; nTmpIdx < (int)ModuleDef.Finger_Count; nTmpIdx++)
                                {
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].Type > BatType.Invalid)
                                    {
                                        bFind = true;
                                        if (0 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_0;
                                        }
                                        else if (1 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_1;
                                        }
                                        else if (2 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_2;
                                        }
                                        else if (3 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_3;
                                        }
                                    }
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].Type == BatType.Fake)
                                    {
                                        break;
                                    }

                                    if ((int)nIdx == 14) // 234有，取2
                                    {
                                        nIdx = ModuleDef.DefInvalid;
                                        nIdx |= ModuleDef.Finger_1;
                                        break;
                                    }
                                    if ((int)nIdx == 12) // 34有，取12
                                    {
                                        nIdx = ModuleDef.DefInvalid;
                                        nIdx |= ModuleDef.Finger_0;
                                        nIdx |= ModuleDef.Finger_1;
                                        nColIdx = nColIdx + 2;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (nColIdx == 0)
                            {
                                for (int nTmpIdx = 0; nTmpIdx < 2; nTmpIdx++)
                                {
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].Type > BatType.Invalid)
                                    {
                                        bFind = true;
                                        if (0 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_0;
                                        }
                                        else if (1 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_1;
                                        }
                                    }

                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, nColIdx + nTmpIdx].Type == BatType.Fake)
                                    {
                                        break;
                                    }

                                }
                            }
                            else if ((nColIdx - 2) + (int)ModuleDef.Finger_Count <= nPltCol)
                            {
                                for (int nTmpIdx = 0; nTmpIdx < (int)ModuleDef.Finger_Count; nTmpIdx++)
                                {
                                    if (Pallet[(int)ModuleDef.Pallet_2].Bat[nRowIdx, (nColIdx - 2) + nTmpIdx].Type > BatType.Invalid)
                                    {
                                        bFind = true;
                                        if (0 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_0;
                                        }
                                        else if (1 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_1;
                                        }
                                        else if (2 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_2;
                                        }
                                        else if (3 == nTmpIdx)
                                        {
                                            nIdx |= ModuleDef.Finger_3;
                                        }
                                    }
                                }
                                if (bFind)
                                {
                                    nColIdx = (nColIdx - 2);
                                }
                            }
                        }

                        //if (bFind)//NG转盘
                        //{
                        //    info.SetAction(OnloadRobotStation.NGTurnTable, nRowIdx, nColIdx, nIdx, true, MotorPosition.Onload_PalletPos);
                        //    return true;
                        //}
                    }
                }
            }
            return false;
        }

        // ================================ 放料计算 ================================

        /// <summary>
        /// 计算放NG电池位
        /// </summary>
        private bool CalcPlaceNGLinePos(ref ActionInfo info)
        {
            int nRowIdx = -1;
            int nColIdx = -1;
            EventState state = EventState.Invalid;
            RunProOnloadNG run = onloadNG;
            ModuleDef finger = ModuleDef.DefInvalid;


            if (GetEvent(run, ModuleEvent.OnloadNGPlaceBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
            {
                //if (run.Battery[0, 0].IsType(BatType.Invalid) && run.Battery[0, 1].IsType(BatType.Invalid) &&
                //    FingerBat(ModuleDef.Finger_2).IsType(BatType.Invalid) && FingerBat(ModuleDef.Finger_3).IsType(BatType.Invalid))
                //{
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    for (int j = i * 2; j < 2 + i * 2; j++)
                    //    {
                    //        ModuleDef fingerIdx = (ModuleDef)(0x01 << j);
                    //        if (FingerBat(fingerIdx).IsType(BatType.NG))
                    //        {
                    //            finger |= fingerIdx;
                    //        }
                    //    }
                    //    if (finger > ModuleDef.DefInvalid)
                    //    {
                    //        info.SetAction(OnloadRobotStation.NGOutput, 0, i, finger, false, MotorPosition.Onload_NGPos);
                    //        return true;
                    //    }
                    //}
                if (run.CheckBattery(0,false) && run.IsEmptyRow(0))
                {
                    for (int j = 0; j < 4; j++)
                    {
                        ModuleDef fingerIdx = (ModuleDef)(0x01 << j);
                        if (FingerBat(fingerIdx).IsType(BatType.NG))
                        {
                            finger |= fingerIdx;
                        }
                    }
                    if (finger > ModuleDef.DefInvalid)
                    {
                        info.SetAction(OnloadRobotStation.NGOutput, 0, 0, finger, false, MotorPosition.Onload_NGPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取放暂存位
        /// </summary>
        private bool CalcBufferPos(int nCurPlt, ref ActionInfo info)
        {
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return false;
            }

            ModuleDef fingerIdx = ModuleDef.DefInvalid;
            ModuleDef fingerInvIdx = ModuleDef.DefInvalid;

            int nRowIdx = -1;
            int nColIdx = -1;
            EventState state = EventState.Invalid;
            RunProOnloadBuffer run = onloadBuffer;

            int nCount = run.HasBatCount();
            // 抓手为空 -> 暂存可取
            if (FingerHasBatType(BatType.Invalid, ref fingerInvIdx) && ModuleDef.Finger_All == fingerInvIdx)
            {
                // 取4个  //+2 是第一个位置要空出来，满足12抓手有电池，到配对位配
                int col = nCount + 2 - 4;
                if (nCount >= (int)ModuleDef.Finger_Count && CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count
                     && !(IsOnloadFake(nCurPlt) && Pallet[nCurPlt].Bat[0, 0].Type == BatType.Invalid))
                {
                    if (GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, ModuleDef.Finger_All, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
                // 取两个
                if ((nCount > 1) && (CalcPlaceOKPltCount(nCurPlt) == 2))
                {
                    if (GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        fingerInvIdx = ModuleDef.DefInvalid;
                        fingerInvIdx |= ModuleDef.Finger_2;
                        fingerInvIdx |= ModuleDef.Finger_3;
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
            }
            // 抓手只有一个假电池
            else if (FingerHasBatType(BatType.Fake, ref fingerIdx) && ModuleDef.Finger_0 == fingerIdx
                   && FingerHasBatTypeCount(BatType.Invalid) > 2 && FingerHasBatType(BatType.Invalid, ref fingerInvIdx))
            {
                // 取3个
                if (nCount > 2)
                {
                    if (GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, 1, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
            }
            // 抓手有OK电池 
            else if (nCount < (int)ModuleDef.Finger_Count && FingerHasBatType(BatType.OK, ref fingerIdx)
                && FingerHasBatType(BatType.Invalid, ref fingerInvIdx) && !FingerHasBatType(BatType.Fake) && !FingerHasBatType(BatType.NG))
            {
                // 缓存可取
                if (GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx)
                    && EventState.Require == state && nCount > 0)
                {
                    int col = -1;
                    // 配一个 && 放2个
                    if (fingerIdx == ModuleDef.Finger_3 && nCount > 1 && (CalcPlaceOKPltCount(nCurPlt) == 2))
                    {
                        col = nCount + 2 - 3;
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, ModuleDef.Finger_2, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                    // 配一个 && 放4个
                    if (fingerInvIdx == ModuleDef.Finger_0 && CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count)
                    {
                        col = nCount + 2 - 1;
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, ModuleDef.Finger_0, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                    // 配二个 && 放4个
                    if (CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count && nCount > 1)
                    {
                        if (Battery[0, 0].Type == BatType.Invalid && Battery[1, 0].Type == BatType.Invalid
                            && Battery[2, 0].Type == BatType.OK && Battery[3, 0].Type == BatType.OK)
                        {
                            col = nCount + 2 - 2;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, col, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else if (Battery[0, 0].Type == BatType.OK && Battery[1, 0].Type == BatType.OK
                            && Battery[2, 0].Type == BatType.Invalid && Battery[3, 0].Type == BatType.Invalid)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, 0, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }
                    // 配三个 && 放4个
                    if ((fingerIdx == ModuleDef.Finger_0 || fingerIdx == ModuleDef.Finger_3) && (nCount > 2)
                        && CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count)
                    {
                        if (fingerIdx == ModuleDef.Finger_0)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, 1, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else
                        {
                            col = nCount + 2 - 3;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, col, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }
                }
                // 缓存可放
                if (GetEvent(run, ModuleEvent.OnloadBufPlaceBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                {
                    int nIndex = 0;
                    for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                    {
                        if (Battery[i, 0].Type == BatType.OK)
                        {
                            nIndex = i;
                            break;
                        }
                    }
                    int col = (nCount + 2 - nIndex) > 1 ? (nCount + 2 - nIndex) : 1;
                    info.SetAction(OnloadRobotStation.BatBuf, 0, col, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                    return true;
                }
            }
            // 抓手有4个OK电池 && 缓存可放 
            else if (nCount < (int)ModuleDef.Finger_Count && FingerHasBatType(BatType.OK, ref fingerIdx) && fingerIdx == ModuleDef.Finger_All)
            {
                if (GetEvent(run, ModuleEvent.OnloadBufPlaceBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                {
                    // 托盘放两个
                    if ((CalcPlaceOKPltCount(nCurPlt) == 2))
                    {
                        fingerIdx = ModuleDef.DefInvalid;
                        fingerIdx |= ModuleDef.Finger_0;
                        fingerIdx |= ModuleDef.Finger_1;
                        info.SetAction(OnloadRobotStation.BatBuf, 0, nCount + 2, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                    else if (IsOnloadFake(nCurPlt) && PltIsEmpty(Pallet[nCurPlt]))
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, nCount + 2, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
            }
            return false;
        }



        /// <summary>
        /// 计算取放暂存位   暂存无拨料机构
        /// </summary>
        private bool CalcBufferPosB(int nCurPlt, ref ActionInfo info)
        {
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return false;
            }
            ModuleDef fingerIdx = ModuleDef.DefInvalid;
            ModuleDef fingerInvIdx = ModuleDef.DefInvalid;
            int nRowIdx = -1;
            int nColIdx = -1;
            EventState state = EventState.Invalid;
            RunProOnloadBuffer run = onloadBuffer;
            int nCount = run.HasBatCount();
            // 抓手为空 -> 暂存可取
            if (FingerHasBatType(BatType.Invalid, ref fingerInvIdx) && ModuleDef.Finger_All == fingerInvIdx)
            {
                int col = -1;
                // 取4个 //+2 是第一个位置要空出来，满足12抓手有电池，到配对位配
                if (nCount >= (int)ModuleDef.Finger_Count && CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count
                     && !(IsOnloadFake(nCurPlt) && Pallet[nCurPlt].Bat[0, 0].Type == BatType.Invalid))
                {
                    col = run.CalPickPos(4) + 1;
                    if (col > -1 && GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, ModuleDef.Finger_All, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }

                // 取两个 34爪取2个 暂存有2、3、4、5个电池
                if ((nCount > 1) && (CalcPlaceOKPltCount(nCurPlt) == 2))
                {
                    if (run.HasBattery(5))
                    {
                        col = 6 - nCount - 1;
                    }
                    else if (run.HasBattery(4))
                    {
                        col = 5 - nCount - 1;
                    }
                    else
                    {
                        col = nCount - 2;
                    }
                    if (col > -1 && GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        fingerInvIdx = ModuleDef.DefInvalid;
                        fingerInvIdx |= ModuleDef.Finger_2;
                        fingerInvIdx |= ModuleDef.Finger_3;
                        info.SetAction(OnloadRobotStation.BatBuf, 0, col, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
            }
            //只有抓手1有OK || 只有抓手一有假电池，且配对位有三个以上，托盘需要4个电池  取成3个配对
            else if ((FingerHasBatType(BatType.OK, ref fingerIdx) || FingerHasBatType(BatType.Fake, ref fingerIdx)) && (fingerIdx == ModuleDef.Finger_0) && (FingerHasBatTypeCount(BatType.Invalid) == (int)ModuleDef.Finger_Count - 1) && (nCount >= (int)ModuleDef.Finger_Count - 1) && CalcPlaceOKPltCount(nCurPlt) == (int)ModuleDef.Finger_Count)
            {
                if (GetEvent(run, ModuleEvent.OnloadBufPickBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                {
                    int nCol = -1;
                    if (run.HasBattery(5))
                    {
                        nCol = 6 - nCount;
                    }
                    else if (run.HasBattery(4))
                    {
                        nCol = 5 - nCount;
                    }
                    else
                    {
                        nCol = 1;
                    }
                    if (nCol > -1)
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                        return true;
                    }
                }
            }
            // 抓手1有一个OK电池 暂存不足三个或者托盘不需要4个  放
            else if (FingerHasBatType(BatType.OK, ref fingerIdx) && (fingerIdx == ModuleDef.Finger_0)
                && (FingerHasBatTypeCount(BatType.Invalid) == 3)
                && FingerHasBatType(BatType.Invalid, ref fingerInvIdx) && (nCount < (int)ModuleDef.Finger_Count - 1 || CalcPlaceOKPltCount(nCurPlt) != (int)ModuleDef.Finger_Count))
            {
                int nCol = -1;
                if (run.HasBattery(5))
                {
                    nCol = 6 - nCount;
                }
                else if (run.HasBattery(4))
                {
                    nCol = 5 - nCount;
                }
                else
                {
                    nCol = nCount + 2;
                }

                if (nCol > -1)
                {
                    info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                    return true;
                }
            }
            //或者抓手2有一个电池，  放
            else if (FingerHasBatType(BatType.OK, ref fingerIdx) && fingerIdx == ModuleDef.Finger_1
                && (FingerHasBatTypeCount(BatType.Invalid) == 3))
            {
                int nCol = -1;
                if (run.HasBattery(5))
                {
                    nCol = 6 - nCount - 1;
                }
                else if (run.HasBattery(4))
                {
                    nCol = 5 - nCount - 1;
                }
                else
                {
                    nCol = nCount + 1;
                }

                if (nCol > -1)
                {
                    info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                    return true;
                }
            }
            //抓手有两个电池
            else if (FingerHasBatType(BatType.OK, ref fingerIdx) && FingerHasBatType(BatType.Invalid, ref fingerInvIdx) && (FingerHasBatTypeCount(BatType.Invalid) == 2))
            {
                int nCol = -1;
                //12爪有
                if (fingerIdx == (ModuleDef.Finger_0 | ModuleDef.Finger_1))
                {
                    if ((nCount == 2 || (nCount > 2 && run.HasBattery(5) && run.HasBattery(4)))
                        && !(IsOnloadFake(nCurPlt) && Pallet[nCurPlt].Bat[0, 0].Type == BatType.Invalid))//取配对成4个 且不上假电池
                    {
                        if (run.HasBattery(5))
                        {
                            nCol = 6 - nCount - 1;
                        }
                        else if (run.HasBattery(4))
                        {
                            nCol = 5 - nCount - 1;
                        }
                        else
                        {
                            nCol = 0;
                        }
                        if (nCol > -1)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }

                    }
                    //大于2个。6号或者5号位无电池，放
                    else if (nCount > 2 && (!run.HasBattery(5) && !run.HasBattery(4)))
                    {
                        {
                            nCol = nCount + 2;
                        }
                        if (nCol > -1)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }
                    else
                    {
                        if (run.HasBattery(5))
                        {
                            nCol = 6 - nCount - 1;
                        }
                        else if (run.HasBattery(4))
                        {
                            nCol = 5 - nCount - 1;
                        }
                        else
                        {
                            nCol = nCount + 2;
                        }
                        if (nCol > -1)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }

                }
                // 3.4爪有   
                //出现34有两个的情况 ： 
                //1 来料4个，需要配假电池暂存有1个或者2个，先取12，后取34。 此时暂存剩下1个 所以不存在抓手34有，暂存有3个及以上的情况
                //2 来料4个，需假电池，暂存为空。 先取12， 后取34。 此时夹爪34有，暂存有两个
                // 抓手34有，暂存有两个 如果在23位，可以配对， 否则直接放配对位
                else if (fingerIdx == (ModuleDef.Finger_2 | ModuleDef.Finger_3))
                {
                    if (nCount == 2)
                    {
                        if (run.HasBattery(5))
                        {
                            nCol = 1;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else if (run.HasBattery(4))
                        {
                            nCol = 0;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else
                        {
                            nCol = nCount;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }

                    }
                    else if (nCount < 2)
                    {
                        if (run.HasBattery(5))
                        {
                            nCol = 6 - nCount - 1 - 2;
                        }
                        else if (run.HasBattery(4))
                        {
                            nCol = 5 - nCount - 1 - 2;
                        }
                        else
                        {
                            nCol = nCount;
                        }

                        if (nCol > -1)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }
                    else if (nCount == 3)
                    {
                        {//在模拟无硬件模式下，一开始偶尔会出现电池被夹到3，4爪的情况，改进后虽未能重现，先把代码加上以防万一。
                            nCol = (!run.HasBattery(5) && !run.HasBattery(4)) ? 3 : ((!run.HasBattery(1) && !run.HasBattery(2)) ? 0 : -1);
                        }
                        if (nCol > -1)
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else
                        {
                            fingerInvIdx = ModuleDef.DefInvalid;
                            fingerInvIdx |= ModuleDef.Finger_0;
                            fingerInvIdx |= ModuleDef.Finger_1;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, 4, fingerIdx, true, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }

                }
                if (nCol > -1)
                {
                    //暂存不足两个，放
                    if (nCount < 2)
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                    }
                    //暂存有两个以上 取配成4个
                    else
                    {
                        info.SetAction(OnloadRobotStation.BatBuf, 0, nCol, fingerInvIdx, true, MotorPosition.Onload_MidBufPos);
                    }
                    return true;
                }
            }
            // 抓手有4个OK电池 && 缓存可放 
            else if (nCount < (int)ModuleDef.Finger_Count && FingerHasBatType(BatType.OK, ref fingerIdx) && fingerIdx == ModuleDef.Finger_All)
            {
                if (nCount == 0)
                {
                    if (GetEvent(run, ModuleEvent.OnloadBufPlaceBattery, ref state, ref nRowIdx, ref nColIdx) && EventState.Require == state)
                    {
                        // 托盘放两个
                        if ((CalcPlaceOKPltCount(nCurPlt) == 2))
                        {
                            fingerIdx = ModuleDef.DefInvalid;
                            fingerIdx |= ModuleDef.Finger_0;
                            fingerIdx |= ModuleDef.Finger_1;
                            info.SetAction(OnloadRobotStation.BatBuf, 0, 2, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                        else if (IsOnloadFake(nCurPlt) && PltIsEmpty(Pallet[nCurPlt]))
                        {
                            info.SetAction(OnloadRobotStation.BatBuf, 0, 2, fingerIdx, false, MotorPosition.Onload_MidBufPos);
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 计算放正常托盘位
        /// </summary>
        private bool CalcPlaceOKPltPos(int nCurPlt, ref ActionInfo info, bool bPlaceAll)
        {
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return false;
            }

            int nPltRowCount = 0;
            int nPltColCount = 0;

            ModuleDef fingerIdx = ModuleDef.DefInvalid;
            ModuleDef okBatCount = ModuleDef.DefInvalid;
            ModuleDef invBatCount = ModuleDef.DefInvalid;
            ModuleDef ngBatCount = ModuleDef.DefInvalid;
            // 获取托盘的最大行列
            PltRowColCount(ref nPltRowCount, ref nPltColCount);
            FingerHasBatType(BatType.OK, ref okBatCount);
            FingerHasBatType(BatType.Invalid, ref invBatCount);
            FingerHasBatType(BatType.NG, ref ngBatCount);

            if ((ModuleDef.Finger_All == okBatCount) || okBatCount > ModuleDef.Finger_1)
            {
                for (int nRowIdx = 0; nRowIdx < nPltRowCount; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltColCount; nColIdx += (int)ModuleDef.Finger_Count)
                    {
                        if (nPltColCount % 4 == 0)
                        {
                            if (Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid) &&
                            invBatCount == ModuleDef.DefInvalid && ngBatCount == ModuleDef.DefInvalid && bPlaceAll)
                            {
                                info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nRowIdx, nColIdx, ModuleDef.Finger_All, false, MotorPosition.Onload_PalletPos);
                                return true;
                            }
                        }
                        else
                        {
                            if (Battery[0, 0].IsType(BatType.OK) && Battery[1, 0].IsType(BatType.OK) &&
                                Battery[2, 0].IsType(BatType.Invalid) && Battery[3, 0].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].IsEmpty() && !bPlaceAll)
                            {
                                return false;
                            }
                            if (Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 1].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 2].IsType(BatType.Invalid) &&
                                Battery[2, 0].IsType(BatType.OK) && Battery[3, 0].IsType(BatType.OK) && !bPlaceAll &&
                                !PalletRowIsEmpty(Pallet[nCurPlt], nRowIdx) && PalletRowBatCount(Pallet[nCurPlt], nRowIdx) == 4)
                            {
                                fingerIdx |= ModuleDef.Finger_2;
                                fingerIdx |= ModuleDef.Finger_3;
                                info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nRowIdx, nPltColCount - 4, fingerIdx, false, MotorPosition.Onload_PalletPos);
                                return true;
                            }
                            else if (PalletRowIsEmpty(Pallet[nCurPlt], nPltRowCount - 1) &&
                                Pallet[nCurPlt].Bat[nRowIdx, 0].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].Bat[nRowIdx, 1].IsType(BatType.Invalid) &&
                                Battery[0, 0].IsType(BatType.OK) && Battery[1, 0].IsType(BatType.OK) &&
                                Battery[2, 0].IsType(BatType.Invalid) && Battery[3, 0].IsType(BatType.Invalid) && !bPlaceAll)
                            {
                                fingerIdx |= ModuleDef.Finger_0;
                                fingerIdx |= ModuleDef.Finger_1;
                                info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nRowIdx, 0, fingerIdx, false, MotorPosition.Onload_PalletPos);
                                return true;
                            }
                            else if (nColIdx > 0 && (nColIdx + 2 <= nPltColCount) &&
                                Pallet[nCurPlt].Bat[nRowIdx, nColIdx - 2].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].Bat[nRowIdx, nColIdx - 1].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                                Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                                invBatCount == ModuleDef.DefInvalid && ngBatCount == ModuleDef.DefInvalid && bPlaceAll)
                            {
                                info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nRowIdx, nColIdx - 2, ModuleDef.Finger_All, false, MotorPosition.Onload_PalletPos);
                                return true;
                            }
                            else if (nColIdx + (int)ModuleDef.Finger_Count <= nPltColCount)
                            {
                                if (Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                                 Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                                 Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                                 Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid) &&
                                 invBatCount == ModuleDef.DefInvalid && ngBatCount == ModuleDef.DefInvalid && bPlaceAll)
                                {
                                    info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nRowIdx, nColIdx, ModuleDef.Finger_All, false, MotorPosition.Onload_PalletPos);
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
        /// 计算放正常托盘数量
        /// </summary>
        private int CalcPlaceOKPltCount(int nCurPlt)
        {
            int nCount = 0;
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return nCount;
            }

            int nPltRowCount = 0;
            int nPltColCount = 0;

            // 获取托盘的最大行列
            PltRowColCount(ref nPltRowCount, ref nPltColCount);

            for (int nRowIdx = 0; nRowIdx < nPltRowCount; nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < nPltColCount; nColIdx += (int)ModuleDef.Finger_Count)
                {
                    if (nPltColCount % (int)ModuleDef.Finger_Count == 0)
                    {
                        if (nColIdx + (int)ModuleDef.Finger_Count <= nPltColCount)
                        {
                            if (Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid))
                            {
                                nCount = (int)ModuleDef.Finger_Count;
                                return nCount;
                            }
                        }
                    }
                    else if (nPltColCount % (int)ModuleDef.Finger_Count == 2)
                    {
                        if (nColIdx + (int)ModuleDef.Finger_Count <= nPltColCount && nColIdx == 0)
                        {
                            // 前面4个无，可放4个
                            if (Pallet[nCurPlt].Bat[nRowIdx, 0].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, 2].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, 3].IsType(BatType.Invalid))
                            {
                                nCount = (int)ModuleDef.Finger_Count;
                                return nCount;
                            }
                        }
                        // 前面4个有，后面两个有，中间可放4个
                        else if (nColIdx + (int)ModuleDef.Finger_Count <= nPltColCount &&
                            !Pallet[nCurPlt].Bat[nRowIdx, 0].IsType(BatType.Invalid) &&
                            !Pallet[nCurPlt].Bat[nRowIdx, 1].IsType(BatType.Invalid) &&
                            !Pallet[nCurPlt].Bat[nRowIdx, 2].IsType(BatType.Invalid) &&
                            !Pallet[nCurPlt].Bat[nRowIdx, 3].IsType(BatType.Invalid) &&
                            !Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 1].IsType(BatType.Invalid) &&
                            !Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 2].IsType(BatType.Invalid))
                        {
                            if (Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid))
                            {
                                nCount = (int)ModuleDef.Finger_Count;
                                return nCount;
                            }
                        }
                        // 前面4个有，后面两个无，后面放2个
                        else if (Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 2].IsType(BatType.Invalid) &&
                            PalletRowBatCount(Pallet[nCurPlt], nRowIdx) == 4)
                        {
                            nCount = (int)ModuleDef.Finger_Count / 2;
                            return nCount;
                        }
                        // 后面4个无，托盘当前行的电池数量是2的倍数，可放4个
                        else if (Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 1].IsType(BatType.Invalid) &&
                           Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 2].IsType(BatType.Invalid) &&
                           Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 3].IsType(BatType.Invalid) &&
                           Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 4].IsType(BatType.Invalid) &&
                           PalletRowBatCount(Pallet[nCurPlt], nRowIdx) % 4 == 2)
                        {
                            nCount = (int)ModuleDef.Finger_Count;
                            return nCount;
                        }
                    }
                }
            }

            return nCount;
        }

        /// <summary>
        /// 托盘某行为空
        /// </summary>
        private bool PalletRowIsEmpty(Pallet Plt, int nRow)
        {
            if (null == Plt)
            {
                return false;
            }

            int nPltRow = 0;
            int nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);

            lock (Plt.LockPlt)
            {
                for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                {
                    if (Plt.Bat[nRow, nColIdx].Type > BatType.Invalid)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 托盘某行电池数量
        /// </summary>
        private int PalletRowBatCount(Pallet Plt, int nRow)
        {
            int nCount = 0;
            if (null == Plt)
            {
                return nCount;
            }

            int nPltRow = 0;
            int nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);

            lock (Plt.LockPlt)
            {
                for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                {
                    if (Plt.Bat[nRow, nColIdx].Type > BatType.Invalid)
                    {
                        nCount++;
                    }
                }
            }
            return nCount;
        }

        /// <summary>
        /// 计算取数量
        /// </summary>
        private int CalcPickCount(int nCurPlt)
        {
            int nCount = 0;
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return nCount;
            }

            int nPltRowCount = 0;
            int nPltColCount = 0;

            // 获取托盘的最大行列
            PltRowColCount(ref nPltRowCount, ref nPltColCount);


            for (int nRowIdx = 0; nRowIdx < nPltRowCount; nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < nPltColCount; nColIdx += (int)ModuleDef.Finger_Count)
                {
                    if (!Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                        !Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                        !Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                        !Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid) &&
                        Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 1].IsType(BatType.Invalid) &&
                        Pallet[nCurPlt].Bat[nRowIdx, nPltColCount - 2].IsType(BatType.Invalid) &&
                        nPltColCount % 4 == 2)
                    {
                        nCount = (int)ModuleDef.Finger_Count / 2;
                        return nCount;
                    }
                }
            }

            return nCount;
        }

        /// <summary>
        /// 计算放回炉托盘位
        /// </summary>
        private bool CalcPlaceRebakingPltPos(int nCurPlt, ref ActionInfo info)
        {
            if (nCurPlt < 0 || nCurPlt > Pallet.Length)
            {
                return false;
            }

            int nPltFakeRow = 0;
            int nPltFakeCol = 0;
            ModuleDef finger = ModuleDef.DefInvalid;

            if (FingerHasBatType(BatType.Fake, ref finger) && FingerHasBatType(BatType.Invalid))
            {
                if (PltHasTypeBat(Pallet[nCurPlt], BatType.Fake, ref nPltFakeRow, ref nPltFakeCol))
                {
                    nPltFakeCol = (int)(nPltFakeCol / 4) * 4;
                    info.SetAction(OnloadRobotStation.Pallet_0 + nCurPlt, nPltFakeRow, nPltFakeCol, finger, false, MotorPosition.Onload_PalletPos);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 托盘是否需要假电池
        /// </summary>
        private bool IsOnloadFake(int nCurPlt)
        {
            if (nCurPlt < 0 || nCurPlt >= Pallet.Length)
            {
                return false;
            }

            // 检查是否需要放假电池
            if (Pallet[nCurPlt].IsOnloadFake)
            {
                int nPltRow = 0;
                int nPltCol = 0;
                PltRowColCount(ref nPltRow, ref nPltCol);

                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx += 2)
                    {
                        // 检查到空列（当前需要放电池的位置）
                        if (Pallet[nCurPlt].Bat[nRowIdx, nColIdx].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 1].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 2].IsType(BatType.Invalid) &&
                            Pallet[nCurPlt].Bat[nRowIdx, nColIdx + 3].IsType(BatType.Invalid))
                        {
                            // 根据设置是否到达放假电池的行列
                            if ((nRowIdx == (nFakeRow - 1) && ((nColIdx == (nFakeCol - 1)) || ((nColIdx + 1) == (nFakeCol - 1)))))
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        #endregion


        #region // 夹爪和暂存电池数据操作

        /// <summary>
        /// 夹爪电池数据
        /// </summary>
        private Battery FingerBat(ModuleDef finger)
        {
            if (finger < 0 || finger >= ModuleDef.Finger_All)
            {
                return null;
            }
            return Battery[(int)Math.Log2((int)finger), 0];
        }

        /// <summary>
        /// 抓手有某类型电池
        /// </summary>
        private bool FingerHasBatType(BatType batType)
        {
            for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
            {
                ModuleDef fingerIdx = (ModuleDef)(0x01 << i);
                if (FingerBat(fingerIdx).IsType(batType))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 抓手有某类型电池，返回抓手位置
        /// </summary>
        private bool FingerHasBatType(BatType batType, ref ModuleDef finger)
        {
            finger = ModuleDef.DefInvalid;
            bool bFind = false;

            for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
            {
                ModuleDef fingerIdx = (ModuleDef)(0x01 << i);
                if (FingerBat(fingerIdx).IsType(batType))
                {
                    bFind = true;
                    finger |= fingerIdx;
                }
            }
            if (bFind)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 抓手有某种电池数量
        /// </summary>
        public int FingerHasBatTypeCount(BatType batType)
        {
            int nBatCount = 0;
            for (int i = 0; i < Battery.GetLength(0); i++)
            {
                if (Battery[i, 0].Type == batType)
                {
                    nBatCount++;
                }
            }
            return nBatCount;
        }
        #endregion


        #region // 抓手、电机和暂存硬件操作

        /// <summary>
        /// 关闭夹爪打开输出
        /// </summary>
        public void CloseOutPutState()
        {
            for (int i = 0; i < 4; i++)
            {
                if (OutputState(OOpen[i], true))
                {
                    OutputAction(OOpen[i], false);
                }
            }
        }

        /// <summary>
        /// 抓手关闭
        /// </summary>
        private bool FingerClose(ModuleDef finger, bool close)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            int nIndex = (int)finger;

            if (finger >= ModuleDef.Finger_0 && finger <= ModuleDef.Finger_All)
            {
                // 操作
                for (int nFingerIdx = 0; nFingerIdx < IFingerCheck.Length; nFingerIdx++)
                {
                    if (1 == (nIndex & 0x01))
                    {
                        if (OClose[nFingerIdx] < 0 || OOpen[nFingerIdx] < 0)
                        {
                            return true;
                        }
                        OutputAction(OClose[nFingerIdx], close);
                        //OutputAction(OOpen[nFingerIdx], !close);
                    }
                    nIndex = nIndex >> 1;
                }

                nIndex = (int)finger;
                for (int nFingerIdx = 0; nFingerIdx < IFingerCheck.Length; nFingerIdx++)
                {
                    if (1 == (nIndex & 0x01))
                    {
                        if (OClose[nFingerIdx] < 0 || OOpen[nFingerIdx] < 0)
                        {
                            return true;
                        }
                        //OutputAction(OClose[nFingerIdx], close);
                        OutputAction(OOpen[nFingerIdx], !close);
                    }
                    nIndex = nIndex >> 1;
                }


                // 检查到位
                nIndex = (int)finger;
                for (int nFingerIdx = 0; nFingerIdx < IFingerCheck.Length; nFingerIdx++)
                {
                    if (1 == (nIndex & 0x01))
                    {
                        if (!(WaitInputState(IClose[nFingerIdx], close) && WaitInputState(IOpen[nFingerIdx], !close)))
                        {
                            return false;
                        }
                    }
                    nIndex = nIndex >> 1;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 抓手检查
        /// </summary>
        private bool FingerCheck(ModuleDef finger, bool hasBat)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            int nIndex = (int)finger;
            for (int nFingerIdx = 0; nFingerIdx < IFingerCheck.Length; nFingerIdx++)
            {
                if ((1 == (nIndex & 0x01)))
                {
                    if (!CheckInputState(IFingerCheck[nFingerIdx], hasBat))
                    {
                        return false;
                    }
                }
                nIndex = nIndex >> 1;
            }
            return true;
        }

        /// <summary>
        /// 暂存检查
        /// </summary>
        private bool BufCheck()
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for (int nBufIdx = 1; nBufIdx < 6; nBufIdx++)
            {
                if (!onloadBuffer.BufCheck(nBufIdx, onloadBuffer.HasBattery(nBufIdx)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 调宽电机移动
        /// </summary>
        private bool MotorUMove(MotorPosition motorLoc, float offset = 0)
        {
            if (this.MotorU < 0)
            {
                return true;
            }
            return MotorMove(this.MotorU, (int)motorLoc, offset);
        }

        /// <summary>
        /// 工位检查
        /// </summary>
        private bool CheckStation(int station, int row, int col, int finger, bool hasBat)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            bool bAlarm = true;
            int nIndex = finger;

            switch ((OnloadRobotStation)station)
            {
                case OnloadRobotStation.OnloadLine:
                    {
                        // 来料线
                        if (null != this.onloadLine)
                        {
                            if (onloadLine.nCurNGGroup > -1)
                            {
                                for (int nIdx = 0; nIdx < 2; nIdx++)
                                {
                                    if (1 == (nIndex & 0x01))
                                    {
                                        if (!onloadLine.CheckBattery(onloadLine.nCurNGGroup + nIdx, hasBat, bAlarm))
                                        {
                                            return false;
                                        }
                                    }
                                    nIndex = nIndex >> 1;
                                }
                            }
                            else
                            {
                                if (col==2)
                                {
                                    for (int nIdx = 2; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                                    {
                                        if (1 == (nIndex & 0x01))
                                        {
                                            if (!onloadLine.CheckBattery(nIdx, hasBat, bAlarm))
                                            {
                                                return false;
                                            }
                                        }
                                        nIndex = nIndex >> 1;
                                    }
                                }
                                else
                                {
                                    for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                                    {
                                        if (1 == (nIndex & 0x01))
                                        {
                                            if (!onloadLine.CheckBattery(nIdx, hasBat, bAlarm))
                                            {
                                                return false;
                                            }
                                        }
                                        nIndex = nIndex >> 1;
                                    }
                                }
                            }
                            return true;
                        }
                        break;
                    }
                case OnloadRobotStation.NGOutput:
                    {
                        // NG电池输出
                        if (null != this.onloadNG)
                        {
                            if (!onloadNG.CheckBattery(0, hasBat, bAlarm))
                            {
                                return false;
                            }
                            return true;
                        }
                        break;
                    }
                case OnloadRobotStation.FakeInput:
                case OnloadRobotStation.FakeScanCode:
                    {
                        // 假电池输入、扫码
                        if (null != this.onloadFake)
                        {
                            int nBatPos = ((int)ModuleDef.Finger_0 == finger) ? col : (col - 1);

                            if (!onloadFake.CheckBattery(nBatPos, hasBat, bAlarm))
                            {
                                return false;
                            }
                            return true;
                        }
                        break;
                    }
                // 夹具
                case OnloadRobotStation.Pallet_0:
                case OnloadRobotStation.Pallet_1:
                case OnloadRobotStation.Pallet_2:
                    {
                        for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                        {
                            if (InputState(IFingerCheck[i], false))
                            {
                                if (!InputState(IOpen[i], true) || !InputState(IClose[i], false))
                                {
                                    string strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能下降！", i + 1);

                                    ShowMessageBox((int)MsgID.CheckFingerState, strInfo, "请查看夹爪状态", MessageType.MsgMessage);
                                    return false;
                                }
                            }
                        }
                        if (CheckPallet(station - (int)OnloadRobotStation.Pallet_0, true, bAlarm))
                        {
                            return true;
                        }
                        break;
                    }
                //case OnloadRobotStation.NGTurnTable://NG转盘
                //    {
                //        for (int i = 0; i < (int)ModuleDef.Finger_Count; i++)
                //        {
                //            if (InputState(IFingerCheck[i], false))
                //            {
                //                if (!InputState(IOpen[i], true) || !InputState(IClose[i], false))
                //                {
                //                    string strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能下降！", i + 1);

                //                    ShowMessageBox((int)MsgID.CheckFingerState, strInfo, "请查看夹爪状态", MessageType.MsgMessage);
                //                    return false;
                //                }
                //            }
                //        }
                //        if (CheckPallet((int)OnloadRobotStation.Pallet_2 - (int)OnloadRobotStation.Pallet_0, true, bAlarm))
                //        {
                //            return true;
                //        }
                //        break;
                //    }
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// 托盘检查
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
        /// 检查电机是否在取料位
        /// </summary>
        public bool CheckMotorInPos(int nMotorPos)
        {
            if (Def.IsNoHardware() || MotorU < 0)
            {
                return true;
            }
            if (nMotorPos <= (int)MotorPosition.Invalid || nMotorPos >= (int)MotorPosition.Onload_Pos_End)
            {
                return false;
            }

            float fCurPos, fLocPos;
            fCurPos = fLocPos = 0;
            string str = "";
            Motors(MotorU).GetCurPos(ref fCurPos);
            Motors(MotorU).GetLocation(nMotorPos, ref str, ref fLocPos);
            if (Math.Abs(fCurPos - fLocPos) >= 1)
            {
                return false;
            }
            return true;
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

            string strInfo = "";
            ModuleDef fingerBatIdx = ModuleDef.DefInvalid;

            // 1.检查抓手是否有电池
            if (InputState(IFingerCheck[0], true) && InputState(IFingerCheck[1], true)
                && InputState(IFingerCheck[2], true) && InputState(IFingerCheck[3], true))
            {
                fingerBatIdx = ModuleDef.Finger_All;
            }
            else
            {
                for (int finger = 0; finger < (int)ModuleDef.Finger_Count; finger++)
                {
                    if (InputState(IFingerCheck[finger], true))
                    {
                        fingerBatIdx |= (ModuleDef)(0x01 << finger);
                    }
                }
            }

            for (int finger = 0; finger < (int)ModuleDef.Finger_Count; finger++)
            {
                if (InputState(IFingerCheck[finger], false))
                {
                    if (!InputState(IOpen[finger], true) || !InputState(IClose[finger], false))
                    {
                        strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能下降！", finger + 1);
                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                        return false;
                    }
                }
            }

            int nIndex = (int)fingerBatIdx;
            // 2.检查目标位置是否有电池
            switch ((OnloadRobotStation)station)
            {
                // 来料线
                case OnloadRobotStation.OnloadLine:
                    {
                        if (null != this.onloadLine)
                        {
                            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                            {
                                // 抓手有电池
                                if (1 == (nIndex & 0x01))
                                {
                                    if (onloadLine.CheckBattery(nIdx, true, false))
                                    {
                                        strInfo = "\r\n检测到来料线上存在电池，不能操作！";
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                // 抓手无电池
                                else
                                {
                                    if (onloadLine.CheckBattery(nIdx, false, false) && (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false)))
                                    {
                                        strInfo = string.Format("\r\n检测到抓手{0}未松开到位，不能操作！", nIdx + 1);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                nIndex = nIndex >> 1;
                            }
                            //if (!onloadLine.LineCheck())
                            //{
                            //    return false;
                            //}
                            return true;
                        }
                        break;
                    }

                // 复投线、托盘扫码
                case OnloadRobotStation.PltScanCode_0:
                case OnloadRobotStation.PltScanCode_1:
                case OnloadRobotStation.PltScanCode_2:
                case OnloadRobotStation.FakeScanCode:
                    {
                        if (ModuleDef.DefInvalid != fingerBatIdx)
                        {
                            strInfo = "\r\n禁止带电池扫码，不能操作！";
                            ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                            return false;
                        }
                        else
                        {
                            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                            {
                                if (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false))
                                {
                                    strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能操作！", nIdx + 1);
                                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                    return false;
                                }
                            }
                        }
                        return true;
                        break;
                    }

                // 暂存工位
                case OnloadRobotStation.BatBuf:
                    {
                        if (0 == col)
                        {
                            nIndex = nIndex >> 1;
                            for (int nIdx = 1; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                            {
                                // 抓手有电池
                                if (1 == (nIndex & 0x01))
                                {
                                    if (onloadBuffer.BufCheck(nIdx - 1, true))
                                    {
                                        strInfo = string.Format("\r\n检测到配对位{0}上存在电池，不能操作！", nIdx);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                // 抓手无电池
                                else
                                {
                                    if (onloadBuffer.BufCheck(nIdx - 1, true) && (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false)))
                                    {
                                        strInfo = string.Format("\r\n检测到抓手{0}未松开到位，不能操作！", nIdx + 1);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                nIndex = nIndex >> 1;
                            }
                            return true;
                        }
                        else if (col > 0 && col < 6)
                        {
                            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                            {
                                // 抓手有电池
                                if (1 == (nIndex & 0x01))
                                {
                                    if (onloadBuffer.BufCheck(nIdx + col - 1, true))
                                    {
                                        strInfo = string.Format("\r\n检测到配对位{0}上存在电池，不能操作！", nIdx + col);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                // 抓手无电池
                                else
                                {
                                    if (onloadBuffer.BufCheck(nIdx + col - 1, true) && (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false)))
                                    {
                                        strInfo = string.Format("\r\n检测到抓手{0}未松开到位，不能操作！", nIdx + 1);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                nIndex = nIndex >> 1;
                            }
                            return true;
                        }
                        break;
                    }
                // NG电池输出
                case OnloadRobotStation.NGOutput:
                    {
                        if (null != this.onloadNG)
                        {
                            // 只有1个传感器
                            if (!onloadNG.CheckBattery(0, false, false))
                            {
                                strInfo = "\r\n检测到NG电池输出线上存在电池，不能操作！";
                                ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                return false;
                            }
                            return true;
                        }
                        break;
                    }
                // 假电池输入
                case OnloadRobotStation.FakeInput:
                    {
                        if (null == this.onloadFake)
                        {
                            break;
                        }

                        if (3 == col)
                        {
                            int nColIdx = col;
                            int nFingerIdx = (int)ModuleDef.Finger_0;
                            bool bHasBufBat = onloadFake.CheckBattery(nColIdx, true, false);

                            // 抓手有电池
                            if ((ModuleDef)nFingerIdx == fingerBatIdx || ModuleDef.Finger_All == fingerBatIdx)
                            {
                                if (bHasBufBat)
                                {
                                    strInfo = string.Format("\r\n检测到假电池输入线第{0}列和抓手上都存在电池，禁止操作", nColIdx + 1);
                                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                    return false;
                                }
                            }
                            // 抓手无电池
                            else if (ModuleDef.DefInvalid == fingerBatIdx)
                            {
                                if (bHasBufBat && (!InputState(IOpen[0], true) || !InputState(IClose[0], false)))
                                {
                                    strInfo = string.Format("\r\n检测到抓手{0}未松开到位，不能操作！", 0 + 1);
                                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                    return false;
                                }
                            }
                            return true;
                        }
                        else if (col >= 0 && col < 3)
                        {
                            for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                            {
                                // 抓手有电池
                                if (1 == (nIndex & 0x01))
                                {
                                    if (onloadFake.CheckBattery(col + nIdx, true, false))
                                    {
                                        strInfo = string.Format("\r\n检测到假电池输入线【第{0}列】上存在电池", col + nIdx + 1);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                // 抓手无电池
                                else
                                {
                                    if (onloadFake.CheckBattery(col + nIdx, true, false) && (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false)))
                                    {
                                        strInfo = string.Format("\r\n检测到抓手{0}未松开到位，不能操作！", nIdx + 1);
                                        ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                        return false;
                                    }
                                }
                                nIndex = nIndex >> 1;
                            }
                            return true;
                        }
                        break;
                    }
                // 夹具
                case OnloadRobotStation.Pallet_0:
                case OnloadRobotStation.Pallet_1:
                case OnloadRobotStation.Pallet_2:
                    {
                        for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                        {
                            if((row >= Pallet[station - (int)OnloadRobotStation.Pallet_0].Bat.GetLength(0)) || ((col + nIdx) >= Pallet[station - (int)OnloadRobotStation.Pallet_0].Bat.GetLength(1)))
                            {
                                strInfo = string.Format("\r\n机器人指令行列错误！", nIdx + 1);
                                ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                return false;
                            }
                            if (InputState(IFingerCheck[nIdx], false))
                            {
                                if (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false))
                                {
                                    strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能操作！", nIdx + 1);
                                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                    return false;
                                }
                            }

                            if (InputState(IFingerCheck[nIdx], true) &&
                                Pallet[station - (int)OnloadRobotStation.Pallet_0].Bat[row, col + nIdx].Type > BatType.Invalid)
                            {
                                strInfo = string.Format("\r\n检测到托盘{0}行{1}列有电芯数据，不能操作！", row + 1, col + nIdx + 1);
                                ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                                return false;
                            }
                        }
                        if (CheckPallet(station - (int)OnloadRobotStation.Pallet_0, true, true))
                        {
                            return true;
                        }
                        break;
                    }
                //case OnloadRobotStation.NGTurnTable://NG转盘
                //    {
                //        for (int nIdx = 0; nIdx < (int)ModuleDef.Finger_Count; nIdx++)
                //        {
                //            if (InputState(IFingerCheck[nIdx], false))
                //            {
                //                if (!InputState(IOpen[nIdx], true) || !InputState(IClose[nIdx], false))
                //                {
                //                    strInfo = string.Format("\r\n检测到无料，抓手{0}未松开到位，不能操作！", nIdx + 1);
                //                    ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                //                    return false;
                //                }
                //            }

                //            if (InputState(IFingerCheck[nIdx], true) &&
                //                Pallet[(int)OnloadRobotStation.Pallet_2 - (int)OnloadRobotStation.Pallet_0].Bat[row, col + nIdx].Type > BatType.Invalid)
                //            {
                //                strInfo = string.Format("\r\n检测到托盘{0}行{1}列有电芯数据，不能操作！", row + 1, col + nIdx + 1);
                //                ShowMsgBox.ShowDialog(RobotDef.RobotName[this.RobotID()] + strInfo, MessageType.MsgMessage);
                //                return false;
                //            }
                //        }
                //        if (CheckPallet((int)OnloadRobotStation.Pallet_2 - (int)OnloadRobotStation.Pallet_0, true, true))
                //        {
                //            return true;
                //        }
                //        break;
                //    }
                default:
                    return true;

            }
            return false;
        }

        #endregion


        #region // 机器人相关操作

        /// <summary>
        /// 获取机器人ID
        /// </summary>
        public int RobotID()
        {
            return nRobotID;
        }

        /// <summary>
        /// 获取机器人速度
        /// </summary>
        public int RobotSpeed()
        {
            return nRobotSpeed;
        }

        /// <summary>
        /// 获取机器人端口
        /// </summary>
        public int RobotPort()
        {
            return nRobotPort;
        }

        /// <summary>
        /// 获取机器人IP
        /// </summary>
        public string RobotIP()
        {
            return strRobotIP;
        }

        public string RobotName()
        {
            return this.RunName;
        }

        /// <summary>
        /// 机器人连接状态
        /// </summary>
        public bool RobotIsConnect()
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
        public bool RobotConnect(bool connect = true)
        {
            if (!bRobotEN || (connect && RobotIsConnect()))
            {
                return true;
            }

            //return connect ? robotClient.Connect(strRobotIP, nRobotPort) : robotClient.Disconnect();
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
        /// 机器人回原点
        /// </summary>
        public bool RobotHome()
        {
            return RobotMove(OnloadRobotStation.Home, 0, 0, nRobotSpeed, RobotAction.HOME);
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
                    //机器人运行标志
                    robotProcessingFlag = true;
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

        /// <summary>
        /// 机器人移动并等待完成
        /// </summary>
        public bool RobotMove(int station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid, bool isAuto = true)
        {
            return RobotMove((OnloadRobotStation)station, row, col, speed, action, motorLoc, isAuto);
        }

        /// <summary>
        /// 机器人移动并等待完成
        /// </summary>
        public bool RobotMove(OnloadRobotStation station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid, bool isAuto = true)
        {
            if (!RobotIsConnect())
            {
                ShowMsgBox.ShowDialog("上料机器人未连接", MessageType.MsgAlarm);
                return false;
            }
            if ((RobotAction.UP == action) || (RobotAction.DOWN == action))
            {
                if ((int)station != robotDebugInfo.Station || row != robotDebugInfo.Row || col != robotDebugInfo.Col)
                {
                    string str = "";
                    str = string.Format("\r\n 机器人在【{0}】工位{1}行{2}列，机器人不能直接上升下降！！！", robotDebugInfo.Station, robotDebugInfo.Row + 1, robotDebugInfo.Col + 1);

                    ShowMessageBox((int)MsgID.CheckRobotPos, str, "请将机器人移动到正确位置", MessageType.MsgWarning);
                    return false;
                }

                if (MotorPosition.Invalid != motorLoc)
                {
                    if (!CheckMotorInPos((int)motorLoc))
                    {
                        string str = string.Format("\r\n {0}轴不在指定位置，机器人不能下降！", Motors(MotorU).Name);

                        ShowMessageBox((int)MsgID.CheckMotorPos, str, "请将调宽电机移动到正确位置", MessageType.MsgWarning);
                        return false;
                    }
                }


            }
            if (RobotAction.MOVE == action)
            {
                if (RobotAction.DOWN == robotDebugInfo.action)
                {
                    string str = "";
                    str = string.Format("\r\n 机器人在【{0}】工位{1}行{2}列下降位置，禁止发送移动指令", robotDebugInfo.Station, robotDebugInfo.Row + 1, robotDebugInfo.Col + 1);

                    ShowMessageBox((int)MsgID.CheckRobotMove, str, "请将机器人移动到正确位置", MessageType.MsgWarning);
                    return false;
                }
            }


            if (RobotCmd(station, row, col, speed, action, ref arrRobotCmd))
            {
                if (!bRobotEN && Def.IsNoHardware())
                {
                    return true;
                }
                // 机器人移动
                if (!robotClient.Send(arrRobotCmd, isAuto))
                {
                    return false;
                }
                robotProcessingFlag = true;
                // 电机移动
                if (MotorPosition.Invalid != motorLoc && RobotAction.MOVE == action)
                {
                    if (!MotorUMove(motorLoc))
                    {
                        RobotMoveFinish(arrRobotCmd, nRobotTimeout);
                        return false;
                    }
                }
                // 等待机器人动作完成
                return RobotMoveFinish(arrRobotCmd, nRobotTimeout);
            }
            return false;
        }

        /// <summary>
        /// 获取机器人命令帧
        /// </summary>
        public bool RobotCmd(OnloadRobotStation station, int row, int col, int speed, RobotAction action, ref int[] frame)
        {
            frame[(int)RobotCmdFrame.Station] = (int)station;
            frame[(int)RobotCmdFrame.StationRow] = row + 1;
            frame[(int)RobotCmdFrame.StationCol] = col + 1;
            frame[(int)RobotCmdFrame.Speed] = speed;
            frame[(int)RobotCmdFrame.Action] = (int)action;
            frame[(int)RobotCmdFrame.Result] = (int)RobotAction.END;

            if (MCState.MCInitializing == MachineCtrl.GetInstance().RunsCtrl.GetMCState() ||
                MCState.MCRunning == MachineCtrl.GetInstance().RunsCtrl.GetMCState())
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
        /// 等待机器人移动完成（秒）
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

                if ((DateTime.Now - startTime).TotalSeconds > 2)
                {
                    if (RobotAction.MOVING != (RobotAction)arrRecv[(int)RobotCmdFrame.Result])
                    {
                        nErrCode = 4;
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

            //机器人运行标志
            robotProcessingFlag = false;

            if (0 == nErrCode)
            {
                robotRecvInfo.SetInfo(arrRecv[(int)RobotCmdFrame.Station], arrRecv[(int)RobotCmdFrame.StationRow] - 1, arrRecv[(int)RobotCmdFrame.StationCol] - 1, RobotAction.FINISH, GetStationName((OnloadRobotStation)arrRecv[(int)RobotCmdFrame.Station]));
                SaveRunData(SaveType.Robot);
            }
            else
            {
                robotRecvInfo.SetInfo(arrRecv[(int)RobotCmdFrame.Station], arrRecv[(int)RobotCmdFrame.StationRow] - 1, arrRecv[(int)RobotCmdFrame.StationCol] - 1, RobotAction.ERR, GetStationName((OnloadRobotStation)arrRecv[(int)RobotCmdFrame.Station]));
                SaveRunData(SaveType.Robot);
            }

            if (1 == nErrCode)
            {
                strDisp = "请检查机器人位置后重新连接";
                strMsg = string.Format("{0}收到连接断开反馈", RunName);
                ShowMessageBox((int)MsgID.RobotConnetState, strMsg, strDisp, MessageType.MsgAlarm);
                return false;
            }
            else if (2 == nErrCode)
            {
                strDisp = "请检查机器人当前位置或操作是否正确";
                strMsg = string.Format("{0}指令错误", RunName);
                ShowMessageBox((int)MsgID.CheckRobotOrder, strMsg, strDisp, MessageType.MsgAlarm);
                return false;
            }
            else if (3 == nErrCode)
            {
                strDisp = "请检查机器人当前位置和状态，查看机器人网络连接状态或示教器是否报警";
                strMsg = string.Format("{0}等待动作完成超时", RunName);
                ShowMessageBox((int)MsgID.WaitActionTimeOut, strMsg, strDisp, MessageType.MsgAlarm, 5);
                return false;
            }
            else if (4 == nErrCode)
            {
                strDisp = "请检查机器人网络连接状态或示教器是否报警";
                strMsg = string.Format("{0}等待动作完成超时", RunName);
                ShowMessageBox((int)MsgID.WaitActionTimeOut, strMsg, strDisp, MessageType.MsgAlarm, 5);
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
        public String GetStationName(OnloadRobotStation station)
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
        /// 获取机器人动作接收信息
        /// </summary>
        public IRobotInfoBase GetRobotActionRecvInfo()
        {
            return robotRecvInfo;
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
            var listStation = Enum.GetNames(typeof(OnloadRobotStation))
                .Select(s => (OnloadRobotStation)Enum.Parse(typeof(OnloadRobotStation), s))
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

        /// <summary>
        /// 检查机器人手自动位置
        /// </summary>
        /// <param name="robotAutoInfo"></param>
        /// <param name="robotDebugInfo"></param>
        /// <returns></returns>
        public bool CheckRobotStartPos(out string msg)
        {
            int info = -1;
            bool infoEn = false;
            msg = string.Empty;
            //手自动在回零位置，则不判断
            if ((RobotAction.HOME == robotAutoInfo.action && RobotAction.HOME == robotDebugInfo.action)
                 || (RobotAction.MOVE == robotAutoInfo.action && RobotAction.HOME == robotDebugInfo.action))
            {
                return true;
            }
            else if ((robotAutoInfo.Station == robotDebugInfo.Station)
                && (robotAutoInfo.Row == robotDebugInfo.Row)
                && (robotAutoInfo.Col == robotDebugInfo.Col))
            {
                if ((robotAutoInfo.action == robotDebugInfo.action)
                    || (robotAutoInfo.action == RobotAction.DOWN && robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.UP && robotDebugInfo.action == RobotAction.MOVE)
                    || (robotAutoInfo.action == RobotAction.UP && robotDebugInfo.action == RobotAction.DOWN))
                {
                    return true;
                }
                info = (int)RobotAction.MOVE;
                infoEn = true;
            }
            msg = string.Format("请在【机器人调试】界面将 {0} 移动到\r\n<{1}-{2}行-{3}列-{4}>\r\n位置，重新停止-复位-启动！"
                , this.RunName, this.robotAutoInfo.stationName
                , this.robotAutoInfo.Row + 1, this.robotAutoInfo.Col + 1, RobotDef.RobotActionName[infoEn ? info : (int)robotAutoInfo.action]);
            RecordMessageInfo((int)MsgID.RobotCheckStep, msg, MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 检查机器人位置（清任务防呆用）
        /// </summary>
        /// <returns></returns>
        public bool CheckRobotPos(int nStation, RobotAction nAction)
        {
            //手自动在回零位置，则不判断
            if ((RobotAction.HOME == robotAutoInfo.action && RobotAction.HOME == robotDebugInfo.action)
                 || (RobotAction.MOVE == robotAutoInfo.action && RobotAction.HOME == robotDebugInfo.action))
            {
                return false;
            }
            else if ((nStation == robotDebugInfo.Station)&& (nAction == robotDebugInfo.action))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查机器人是否安全
        /// </summary>
        public bool CheckRobotIsSafety()
        {
            if (robotDebugInfo.Station == (int)OnloadRobotStation.Home)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region // 扫码枪

        /// <summary>
        /// 获取扫码枪端口
        /// </summary>
        public int ScanPort()
        {
            return this.nScanPort;
        }

        /// <summary>
        /// 获取扫码枪IP
        /// </summary>
        public string ScanIP(int scanIdx)
        {
            return this.strScanIP;
        }
        /// <summary>
        /// 扫码枪连接状态
        /// </summary>
        public bool ScanIsConnect()
        {
            if (!bScanPalletEN)
            {
                return true;
            }
            ScanconnectState = ScanCodeClient[0].IsConnect();
            return ScanCodeClient[0].IsConnect();
        }

        /// <summary>
        /// 扫码枪连接
        /// </summary>
        public bool ScanConnect(bool connect = true)
        {
            if (!bScanPalletEN || (connect && ScanIsConnect()))
            {
                return true;
            }
            ScanCode client = ScanCodeClient[0];
            if (connect)
            {
                if (client.Connect())
                {
                    ScanconnectState = ScanCodeClient[0].IsConnect();
                    return true;
                }
            }
            else
            {
                if (client.Disconnect())
                {
                    ScanconnectState = ScanCodeClient[0].IsConnect();
                }
            }
            return false;
        }

        /// <summary>
        /// 扫码
        /// </summary>
        public bool ScanSend(ref string strRecv, bool bWait = true)
        {
            if (!bScanPalletEN || Def.IsNoHardware())
            {
                int a = SysDef.GetRandom(0, 9);
                Sleep(10);
                int b = SysDef.GetRandom(0, 9);
                int c = SysDef.GetRandom(0, 9);
                Sleep(10);
                int d = SysDef.GetRandom(0, 9);

                strRecv = string.Format("GDHSDBAK{0}{1}{2}", a, b, c);
                Thread.Sleep(30);
                return true;
            }
            int nScanTimeout = 3;
            if (bWait)
            {
                // 发送命令，并等待完成
                for (int i = 0; i < nScanTimes; i++)
                {
                    if (ScanCodeClient[0].SendAndWait(ref strRecv, (uint)nScanTimeout))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // 发送命令，不等待
                return ScanCodeClient[0].Send();
            }
            return false;
        }

        public bool ScanSendBattery(ref string strRecv, bool bWait = true)
        {
            if (!bScanBatteryEN || Def.IsNoHardware())
            {
                strRecv = string.Format("JDC100{0}{1}{2}{3}", SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9));
                Thread.Sleep(50);
                return true;
            }
            int nScanTimeout = 3;
            if (bWait)
            {
                // 发送命令，并等待完成
                for (int i = 0; i < nScanTimes; i++)
                {
                    if (ScanCodeClient[0].SendAndWait(ref strRecv, (uint)nScanTimeout))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // 发送命令，不等待
                return ScanCodeClient[0].Send();
            }
            return false;
        }
        #endregion


        #region // mes接口
        /// <summary>
        /// 校验电芯条码
        /// </summary>
        private bool MesProveBatteryCode(string strSfcCode, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// 校验托盘条码
        /// </summary>
        private bool MesCheckJigVaild(string strJigCode, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// 电芯绑定
        /// </summary>
        private bool MesBindJigBattery(string strBattertyCode, string strJigCode, int nRows, int nCol, int nBindPos, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// 电芯解绑
        /// </summary>
        private bool MesUnBindBattery(string strBatteryCode, string strJigCode, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            return true;
        }

        bool IRobot.IsCollisionAlarm(out string msg)
        {
            msg = default;
            return false;
        }

        bool IRobot.ManualCheckStation(int station, int row, int col, RobotAction action, bool bPickIn)
        {
            return ManualCheckStation(station, row, col, bPickIn);
        }

        bool IRobot.FingerClose(uint fingers, bool close)
        {
            return FingerClose((ModuleDef)fingers, close);
        }
        #endregion

    }
}
