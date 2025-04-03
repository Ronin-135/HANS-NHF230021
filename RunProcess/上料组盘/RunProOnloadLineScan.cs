using HelperLibrary;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SystemControlLibrary;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOnloadLineScan : RunProcess
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckBat,
            Init_ScannerConnect,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_WaitRecvBattery,
            Auto_StartBatScan,
            Auto_MesCheckSFCStatus,
            Auto_WaitSendBattery,
            Auto_WaitPickFinished,
            Auto_WorkEnd,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadLineScanMsgStartID,
            TransferTimeout,
            MesCheckAlarm,
        }

        #endregion


        #region // 字段

        // 【相关模组】
        private RunProOnloadLine onloadLine;

        // 【IO/电机】
        #region 对接信号
        private int IFrontWorkStageSafe;    // 前工段安全信号
        private int IFrontWorkStageReady;   // 前工段准备好信号
        private int ODeviceSafe;            // 设备安全信号
        private int ORequire;               // 要料请求：请求来料
        private int OPickEnd;               // 取料完成：取料完成
        private int nClicksSafeCount;
        #endregion

        private int IRecvHasBat;            // 入口接收有电池检查
        private int IscanInpos;             // 扫码到位
        private int ISendFinCheck;          // 出口发送完成检查
        private int[] IBatInpos;            // 来料有料检查
        public int PISendFinCheck => ISendFinCheck;
        private int IScanCylPush;           // 扫码气缸，推出到位
        private int IScanCylPull;           // 扫码气缸，回退到位

     
        private int OTransMotor;            // 拉线转移电机
        private int OScanCylPush;           // 扫码气缸，推出到位
        private int OScanCylPull;           // 扫码气缸，回退到位   

        // 【模组参数】
        private bool bConveyerLineEN;       // 来料对接使能：TRUE对接，FALSE不对接
        private int nScanTimes;             // 扫码次数：=0,不扫码；>0,扫码
        private bool bScanEN;               // 扫码使能
        private string[] strScanIP;         // 扫码IP
        private string bstrCodeID;          // 电芯条码比对
        private int[] nScanPort;            // 扫码端口
        // 【模组数据】
        private int nScanCount;
        private ScanCode[] ScanCodeClient;  // 扫码枪客户端
        private int nCurScanCount;          // 当前扫码次数（临时使用）

        private bool connectState1;          // 扫码枪1连接状态(界面显示)
        private bool connectState2;          // 扫码枪2连接状态(界面显示)
        private bool connectState3;          // 扫码枪3连接状态(界面显示)
        private bool connectState4;          // 扫码枪4连接状态(界面显示)


        public bool ConnectState1
        {
            get { return connectState1; }
            set { SetProperty(ref connectState1, value); }
        }
        public bool ConnectState2
        {
            get { return connectState2; }
            set { SetProperty(ref connectState2, value); }
        }
        public bool ConnectState3
        {
            get { return connectState3; }
            set { SetProperty(ref connectState3, value); }
        }
        public bool ConnectState4
        {
            get { return connectState4; }
            set { SetProperty(ref connectState4, value); }
        }
        #endregion


        #region // 构造函数

        public RunProOnloadLineScan(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 1, 4, 1);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            InsertPrivateParam("ConveyerLineEN", "对接使能", "对接使能：TRUE对接物流线，FALSE不对接物流线", bConveyerLineEN);
            InsertPrivateParam("ScanTimes", "扫码次数", "扫码次数： = 0,不扫码； > 0,扫码", nScanTimes);
            InsertPrivateParam("ScanEN", "扫码使能", "TRUE启用，FALSE禁用", bScanEN);
            for (int i = 0; i < 4; i++)
            {
                string strKey = string.Format("ScanIP{0}", i + 1);
                string strName = string.Format("扫码IP{0}", i + 1);
                InsertPrivateParam(strKey, strName, strName, strScanIP[i], ParameterLevel.PL_STOP_ADMIN);
                strKey = string.Format("ScanPort{0}", i + 1);
                strName = string.Format("扫码端口{0}", i + 1);
                InsertPrivateParam(strKey, strName, strName, nScanPort[i], ParameterLevel.PL_STOP_ADMIN);
            }
            //InsertPrivateParam("strCodeID", "电芯条码比对", "电芯条码比对", bstrCodeID, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IFrontWorkStageSafe = -1;
            IFrontWorkStageReady = -1;
            IRecvHasBat = -1;
            IscanInpos = -1;
            ISendFinCheck = -1;
            IBatInpos = new int[4] { -1, -1,-1, -1 };
            IScanCylPush = -1;
            IScanCylPull = -1;
            nClicksSafeCount = 0;

            ODeviceSafe = -1;
            ORequire = -1;
            OPickEnd = -1;
            OTransMotor = -1;
            OScanCylPush = -1;
            OScanCylPull = -1;

            // 模组参数
            bConveyerLineEN = false;
            nScanTimes = 3;
            bstrCodeID ="";

            bScanEN = false;
            strScanIP = new string[4] { "", "" ,"", "" };

            nScanPort = new int[4] { 0, 0, 0, 0 };
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
            ScanCodeClient = MachineCtrl.Ioc.Resolve<IEnumerable<ScanCode>>().Where(scan => scan.Parent == null).Take(4).ToArray();
            for (int i = 0; i < ScanCodeClient.Length; i++)
            {
                var index = i;
                this.ScanCodeClient[i].Name = $"{this.RunName}_扫码枪 {i + 1}";
                ScanCodeClient[i].GetIpPort = () => (strScanIP[index], nScanPort[index]);
                ScanCodeClient[i].Parent = this;
            }

            // 添加IO/电机
            InputAdd("IFrontWorkStageSafe", ref IFrontWorkStageSafe);
            InputAdd("IFrontWorkStageReady", ref IFrontWorkStageReady);
            InputAdd("IRecvHasBat", ref IRecvHasBat);
            InputAdd("IscanInpos", ref IscanInpos);
            InputAdd("ISendFinCheck", ref ISendFinCheck);
            InputAdd("IBatInpos[1]", ref IBatInpos[0]);
            InputAdd("IBatInpos[2]", ref IBatInpos[1]);
            InputAdd("IBatInpos[3]", ref IBatInpos[2]);
            InputAdd("IBatInpos[4]", ref IBatInpos[3]);
            InputAdd("IScanCylPush", ref IScanCylPush);
            InputAdd("IScanCylPull", ref IScanCylPull);

            OutputAdd("ODeviceSafe", ref ODeviceSafe);
            OutputAdd("ORequire", ref ORequire);
            OutputAdd("OPickEnd", ref OPickEnd);
            OutputAdd("OTransMotor", ref OTransMotor);
            OutputAdd("OScanCylPush", ref OScanCylPush);
            OutputAdd("OScanCylPull", ref OScanCylPull);

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
                        else
                        {
                            // 清除信号
                            OutputAction(ODeviceSafe, false);
                            OutputAction(OPickEnd, false);
                            OutputAction(ORequire, false);
                            //if (!InputState(IResponse, false) || !InputState(IFrontWorkStageReady, false))
                            {
                                //string strMsg, strDisp;
                                //strMsg = "对接信号未清除！";
                                //strDisp = "清除“响应”和“准备好”信号！";
                                //ShowMessageBox(GetRunID() * 100 + 0, "对接信号未清除！", strDisp, MessageType.MsgAlarm);
                                //break;
                            }
                        }

                        this.nextInitStep = InitSteps.Init_CheckBat;
                        break;
                    }
                case InitSteps.Init_CheckBat:
                    {
                        CurMsgStr("检查电池状态", "Check battery status");

                        ScanConnect(0, false);
                        ScanConnect(1, false);
                        ScanConnect(2, false);
                        ScanConnect(3, false);
                        OutputAction(OTransMotor, false);

                        this.nextInitStep = InitSteps.Init_ScannerConnect;
                        break;

                        //// 以下检查功能会有误报，暂时不用
                        //bool bCheckOK = true;
                        //for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                        //{
                        //    if (!CheckInputState(IBatInpos[nColIdx], Battery[Battery.GetLength(0) - 1, nColIdx].Type > BatType.Invalid))
                        //    {
                        //        bCheckOK = false;
                        //        break;
                        //    }
                        //}

                        //if (bCheckOK)
                        //{
                        //    ScanConnect(0, false);
                        //    ScanConnect(1, false);
                        //    this.nextInitStep = InitSteps.Init_ScannerConnect;
                        //}
                        //break;
                    }
                case InitSteps.Init_ScannerConnect:
                    {
                        CurMsgStr("连接扫码枪", "Connect scanner");
                        if (ScanConnect(0) && ScanConnect(1) && ScanConnect(2) && ScanConnect(3))
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
                Sleep(100);
            }

            //一启动就给安全信号表示在自动中
            OutputAction(ODeviceSafe, true);
            ScanIsConnect(0);
            ScanIsConnect(1);
            ScanIsConnect(2);
            ScanIsConnect(3);
            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 停止或开始上料
                        if (!OnLoad)
                        {
                            CurMsgStr("接料使能关闭，停止接料", "Wait work start");
                            break;
                        }

                        if (IsEmptyRow(0) && CheckInputState(IscanInpos, false))
                        {
                            if (!bConveyerLineEN)
                            {
                                break;
                            }
                            if (OutputAction(ORequire, false) && OutputAction(OPickEnd, false) 
                                && InputState(IFrontWorkStageSafe, true) && InputState(IFrontWorkStageReady, true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WaitRecvBattery;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                            //else if (!bConveyerLineEN && InputState(IRecvHasBat, true))
                            //{
                            //    this.nextAutoStep = AutoSteps.Auto_WaitRecvBattery;
                            //    SaveRunData(SaveType.AutoStep);
                            //    break;
                            //}
                        }
                        else if (!IsEmptyRow(0))
                        {

                            EventState curState = EventState.Invalid;
                            GetEvent(this, ModuleEvent.OnloadLineScanSendBat, ref curState);

                            if (EventState.Invalid == curState || EventState.Finished == curState)
                            {
                                SetEvent(this, ModuleEvent.OnloadLineScanSendBat, EventState.Require);
                            }

                            if (EventState.Response == curState)
                            {
                                SetEvent(this, ModuleEvent.OnloadLineScanSendBat, EventState.Ready);
                                this.nextAutoStep = AutoSteps.Auto_WaitSendBattery;
                                break;
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_WaitRecvBattery:
                    {
                        CurMsgStr("等待接收电池到位", "Wait Recv Battery Inpos");

                        if (ScanCylPush(true))
                        {
                            if (!(CheckInputState(IFrontWorkStageSafe, true) && CheckInputState(IFrontWorkStageReady, true)))
                            {
                                break;
                            }
                            //请求进料
                            if (OutputAction(ORequire, true) && OutputAction(OPickEnd, false))
                            {
                                if (TransferBattery(true))
                                {
                                    OutputAction(ORequire, false);
                                    OutputAction(OPickEnd, true);

                                    ScanCylPush(false, false);
                                    this.nextAutoStep = AutoSteps.Auto_StartBatScan;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_StartBatScan:
                    {
                        CurMsgStr("开始电池扫码", "Start battery scan");

                        // 触发扫码  
                        for (int i = 0; i < 4; i++)
                        {
                            string strRecv = string.Empty;
                            if (ScanSend(ref strRecv, i))
                            {
                                Battery[0, i].Code = strRecv.Replace("\r", "").Replace("\n", "");
                                Battery[0, i].Type = BatType.OK;
                            }
                            else
                            {
                                Battery[Battery.GetLength(0) - 1, i].Type = BatType.NG;
                            }
                        }

                        if (Def.IsNoHardware() && bConveyerLineEN)
                        {
                            Random rnd = new Random();
                            for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                            {
                                if (1 == rnd.Next(1, 5))
                                {
                                    int a = rnd.Next(9999, 99999);
                                    Battery[Battery.GetLength(0) - 1, nColIdx].Code = a.ToString();
                                    Battery[Battery.GetLength(0) - 1, nColIdx].Type = BatType.NG;
                                }
                                else
                                {
                                    int a = rnd.Next(9999, 99999);
                                    Battery[Battery.GetLength(0) - 1, nColIdx].Code = a.ToString();
                                    Battery[Battery.GetLength(0) - 1, nColIdx].Type = BatType.OK;
                                }
                            }
                        }

                        this.nextAutoStep = Def.IsNoHardware() ? AutoSteps.Auto_WorkEnd : AutoSteps.Auto_MesCheckSFCStatus;
                        SaveRunData(SaveType.Battery | SaveType.AutoStep);

                        break;
                    }
                case AutoSteps.Auto_MesCheckSFCStatus:
                    {
                        CurMsgStr("MES检查电芯状态", "Check SFC Status");
                        List<DataItem> dataItem = new List<DataItem>();
                        string strMsg = "";
                        for (int i = 0; i < 4; i++)
                        {
                            // 进站
                            bool bStatus = false;
                            if (Battery[0, i].Code.Length > 5)
                            {
                                bStatus = MachineCtrl.GetInstance().MesWIPInStationCheck(Battery[0, i].Code.Replace("\r", "").Replace("\n", ""), ref strMsg, ref dataItem);
                            }
                            if (Battery[0, i].Type == BatType.OK && !bStatus)
                            {
                                Battery[0, i].Type = BatType.NG;
                            }
                            // 进站本地保存
                            MachineCtrl.GetInstance().SaveInStation(Battery[0, i].Code, bStatus, strMsg);
                        }

                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.Battery | SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_WaitSendBattery:
                    {
                        CurMsgStr("等待发送电池", "Wait send  battery");

                        if (ScanCylPush(false))
                        {
                            if (TransferBattery(false))
                            {
                                    if (null != onloadLine)
                                    {
                                        for (int nColIdx = 0; nColIdx < 4; nColIdx++)
                                        {
                                            onloadLine.Battery[0, nColIdx].CopyFrom(Battery[0, nColIdx]);
                                            Battery[0, nColIdx].Release();
                                        }
                                    }


                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                onloadLine.SaveRunData(SaveType.Battery);
                                //SaveRunData(SaveType.AutoStep);
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                break;
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
            if (output == DeviceManager.Outputs(ODeviceSafe) && bOn)
            {
                nClicksSafeCount++;
                if (nClicksSafeCount >= 3)
                {
                    if (ButtonResult.OK == ShowMsgBox.ShowDialog(RunName + $"确定将{output.Name}输出?", MessageType.MsgQuestion).Result)
                    {
                        nClicksSafeCount = 0;
                        return true;
                    }
                    nClicksSafeCount = 0;
                    return false;
                }
            }
            return false;
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
            OutputAction(ODeviceSafe, false);
        }

        #endregion


        #region // 运行数据读写

        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public override void InitRunData()
        {
            nScanCount = 0;
            OutputAction(ORequire, false);
            OutputAction(OPickEnd, false);
            OutputAction(OTransMotor, false);

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

            if (GetEvent(this, ModuleEvent.OnloadLineScanSendBat, ref curEventState, ref nEventRowIdx, ref nEventColIdx) &&
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
            EventState eventState = EventState.Invalid;
            if (GetEvent(this, ModuleEvent.OnloadLineScanSendBat, ref eventState) &&
                EventState.Response == eventState || EventState.Ready == eventState)
            {
                string strInfo = string.Format("《来料扫码》与《来料取料线》线体处于交互状态！\r\n确定是否清除？");
                if (ButtonResult.OK != ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                {
                    return false;
                }
            }
            base.CopyRunDataClearBak();
            nScanCount = 0;
            OutputAction(ORequire, false);
            OutputAction(OPickEnd, false);
            base.InitRunData();
            SaveRunData(SaveType.Battery | SaveType.AutoStep);
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
        /// 参数读取（初始化时调用）
        /// </summary>
        public override bool ReadParameter()
        {
            base.ReadParameter();
            string strKey;

            bConveyerLineEN = ReadParam(RunModule, "ConveyerLineEN", false);
            bScanEN = ReadParam(RunModule, "ScanEN", false);
            nScanTimes = ReadParam(RunModule, "ScanTimes", 3);
            bstrCodeID = ReadParam(RunModule, "strCodeID", "");

            for (int i = 0; i < 4; i++)
            {
                strKey = string.Format("ScanIP{0}", i + 1);
                strScanIP[i] = ReadParam(RunModule, strKey, "");
                strKey = string.Format("ScanPort{0}", i + 1);
                nScanPort[i] = ReadParam(RunModule, strKey, 0);
            }   
            return true;

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
        /// 扫码气缸推出
        /// </summary>
        /// <param name="push">true推出，false回退</param>
        /// <returns></returns>
        protected bool ScanCylPush(bool push, bool alarm = true)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            //检查IO配置
            for (int i = 0; i < 2; i++)
            {
                if (IScanCylPush < 0 || IScanCylPull < 0 || OScanCylPull < 0 || OScanCylPush < 0)
                {
                    return false;
                }
            }

            //操作
            OutputAction(OScanCylPush, push);
            OutputAction(OScanCylPull, !push);

            //检查到位
            if (alarm)
            {
                if (!(WaitInputState(IScanCylPush, push) && WaitInputState(IScanCylPull, !push)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 转移电池
        /// </summary>
        private bool TransferBattery(bool bRecv)
        {
            OutputAction(OTransMotor, true);
            TimeSpan TSpan;
            DateTime StartTime = DateTime.Now;
            bool bFinished = false;

            EventState curState = EventState.Invalid;
            while (true)
            {
                if (bRecv)
                {
                    if (InputState(IBatInpos[0], true) && InputState(IBatInpos[1], true) && InputState(IBatInpos[2], true) && InputState(IBatInpos[3], true))
                    {
                        bFinished = true;
                        break;
                    }
                }
                else
                {
                    GetEvent(this, ModuleEvent.OnloadLineScanSendBat, ref curState);
                    if (InputState(IscanInpos, false) && InputState(ISendFinCheck, false) && EventState.Finished == curState || InputState(onloadLine.IRecvHasBat, true))
                    {
                        bFinished = true;
                        break;
                    }
                }
                // 超时检查
                TSpan = DateTime.Now - StartTime;
                if (TSpan.TotalMilliseconds > 15 * 1000)
                {
                    break;
                }
                Sleep(1);
            }

            if (bFinished)
            {
                Sleep(200); // 延迟800毫秒停止
                OutputAction(OTransMotor, false);
            }
            else
            {
                OutputAction(OTransMotor, false);
                string strMsg = string.Format("扫码线体{0}电池超时", bRecv ? "接收" : "发送");
                ShowMessageBox((int)MsgID.TransferTimeout, strMsg, "请检查来料扫码线感应器是否正常", MessageType.MsgAlarm);
            }

            // 检测到连料感应器临时屏蔽duanyh2024-1108
            //if (!CheckInputState(IRecvHasBat, false) || !CheckInputState(ISendFinCheck, false))
            //{
            //    bFinished = false;
            //}

            return bFinished;
        }

        #region  // 扫码枪

        /// <summary>
        /// 获取扫码枪端口
        /// </summary>
        public int ScanPort(int ScanIdx)
        {
            return nScanPort[ScanIdx];
        }

        
        /// <summary>
        /// 获取扫码枪IP
        /// </summary>
        public string ScanIP(int ScanIdx)
        {
            return strScanIP[ScanIdx];
        }

        /// <summary>
        /// 扫码枪连接状态
        /// </summary>
        public bool ScanIsConnect(int ScanIdx)
        {
            if (!bScanEN)
            {
                return true;
            }
            if (ScanIdx==0) ConnectState1 = ScanCodeClient[ScanIdx].IsConnect();
            else if (ScanIdx == 1) ConnectState2 = ScanCodeClient[ScanIdx].IsConnect();
            else if (ScanIdx == 2) ConnectState3 = ScanCodeClient[ScanIdx].IsConnect();
            else if (ScanIdx == 3) ConnectState4 = ScanCodeClient[ScanIdx].IsConnect();

            return ScanCodeClient[ScanIdx].IsConnect();
        }

        /// <summary>
        /// 扫码枪连接
        /// </summary>
        public bool ScanConnect(int ScanIdx, bool connect = true)
        {
            if (!bScanEN || (connect && ScanIsConnect(ScanIdx)))
            {
                return true;
            }
            if (connect)
            {
                if (ScanCodeClient[ScanIdx].Connect())
                {
                    if (ScanIdx == 0) ConnectState1 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 1) ConnectState2 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 2) ConnectState3 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 3) ConnectState4 = ScanCodeClient[ScanIdx].IsConnect();
                    return true;
                }
            }
            else
            {
                if (ScanCodeClient[ScanIdx].Disconnect())
                {
                    if (ScanIdx == 0) ConnectState1 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 1) ConnectState2 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 2) ConnectState3 = ScanCodeClient[ScanIdx].IsConnect();
                    else if (ScanIdx == 3) ConnectState4 = ScanCodeClient[ScanIdx].IsConnect();
                }
            }
            return false;
        }

        /// <summary>
        /// 扫码
        /// </summary>
        public bool ScanSend(ref string strRecv, int ScanIdx, bool bWait = true)
        {
            if (!bScanEN)
            {
                strRecv = string.Format("EBF100{0}{1}{2}{3}", SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9), SysDef.GetRandom(0, 9));
                Thread.Sleep(50);
                return true;
            }
            int nScanTimeout = 3;
            if (bWait)
            {
                // 发送命令，并等待完成
                for (int i = 0; i < nScanTimes; i++)
                {
                    if (ScanCodeClient[ScanIdx].SendAndWait(ref strRecv, (uint)nScanTimeout))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // 发送命令，不等待
                return ScanCodeClient[ScanIdx].Send();
            }
            return false;
        }

        #endregion

        #region // mes接口
        
        #endregion
    }
}
