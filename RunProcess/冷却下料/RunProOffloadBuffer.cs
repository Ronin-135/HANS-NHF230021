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
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProOffloadBuffer : RunProcess
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckFinger,
            Init_JackUpCylBack,
            Init_MotorUHome,
            Init_CheckBat,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_JackUpCylPickBack,
            Auto_MotorPickMove,
            Auto_JackUpCylPickPush,
            Auto_PickCheckFinger,
            Auto_MotorPlaceMove,
            Auto_JackUpCylPlaceBack,
            Auto_PlaceCheckFinger,
            Auto_WaitActionFinished,
            Auto_WorkEnd,
        }

        private enum ModuleDef
        {
            // 无效
            DefInvalid = -1,

            JackUp_1 = 0, // 下气缸
            JackUp_2,     // 上气缸

            Finger_col = 6,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadBufferMsgStartID,
        }
        #endregion


        #region // 字段

        // 【相关模组】
        private RunProOffloadRobot offloadRobot;

        // 【IO/电机】
        private int IOpen;                            // 夹爪松开
        private int IClose;                           // 夹爪夹紧
        private int IFingerCheck;                     // 夹爪有料检测
        private int OOpen;                            // 夹爪松开
        private int OClose;                           // 夹爪夹紧
        private int []IBufCheck;                      // 配对位电池检测
        private int MotorU;                           // 配对电机U
        private int []IJackUpCylBack;                 // 顶升气缸回退(数组1为下气缸，2为上）
        private int []IJackUpCylPush;			      // 顶升气缸推出(数组1为下气缸，2为上）
        private int []OJackUpCylBack;                 // 顶升气缸回退(数组1为下气缸，2为上）
        private int []OJackUpCylPush;                 // 顶升气缸推出(数组1为下气缸，2为上）

        // 【模组参数】
        private float fColDistance;                     // 列距

        // 【模组数据】
        private ModuleEvent curRespEvent;               // 当前响应信号
        private EventState curEventState;               // 当前信号状态（临时使用）
        private int nCurPickCol;					    // 当前取料列
        private int nCurPlaceCol;                       // 当前放料列
        private DateTime nBatteryOverrTime;             // 电芯超时时间
        private int nSetBatTime;                        // 设置电芯超时时间
        #endregion


        #region // 构造函数
        public RunProOffloadBuffer(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject(0, 1, (int)ModuleDef.Finger_col, 2);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();

            // 插入参数
            //InsertPrivateParam("ColDistance", "配对位列距", "0", fColDistance, RecordType.RECORD_DOUBLE);
            InsertPrivateParam("SetBatTime", "设置电芯超时时间(min)", "180", nSetBatTime);
        }
        #endregion


        #region // 模组数据初始化和配置读取


        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机
            IOpen = -1;
            IClose = -1;
            IFingerCheck = -1;
            OOpen = -1;
            OClose = -1;
            IBufCheck = new int[(int)ModuleDef.Finger_col];
            IJackUpCylBack = new int[2] { -1, -1 };
            IJackUpCylPush = new int[2] { -1, -1 };
            OJackUpCylBack = new int[2] { -1, -1 };
            OJackUpCylPush = new int[2] { -1, -1 };
            MotorU = -1;
            
            for (int i = 0; i < IBufCheck.Length; i++)
            {
                IBufCheck[i] = -1;
            }
            // 模组参数
            nCurPickCol = -1;
            nCurPlaceCol = -1;
            nSetBatTime = 180;
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
            MotorAdd("MotorU", ref MotorU);

            InputAdd("IOpen", ref IOpen);
            InputAdd("IClose", ref IClose);
            InputAdd("IFingerCheck", ref IFingerCheck);
            OutputAdd("OOpen", ref OOpen);
            OutputAdd("OClose", ref OClose);

            for (int nIdx = 0; nIdx < IBufCheck.Length; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IBufCheck" + strIndex, ref IBufCheck[nIdx]);
            }

            for (int nIdx = 0; nIdx < 2; nIdx++)
            {
                string strIndex = "[" + (nIdx + 1) + "]";
                InputAdd("IJackUpCylBack" + strIndex, ref IJackUpCylBack[nIdx]);
                InputAdd("IJackUpCylPush" + strIndex, ref IJackUpCylPush[nIdx]);
                OutputAdd("OJackUpCylBack" + strIndex, ref OJackUpCylBack[nIdx]);
                OutputAdd("OJackUpCylPush" + strIndex, ref OJackUpCylPush[nIdx]);
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
                case InitSteps.Init_CheckFinger:
                    {
                        CurMsgStr("检查夹爪状态", "Check Finger status");
                        bool bHas = Battery[0, (int)ModuleDef.Finger_col].Type > BatType.Invalid;
                        if (FingerCheck(bHas))
                        {
                            if (bHas) // 有电池不回零
                            {
                                this.nextInitStep = InitSteps.Init_CheckBat;
                            }
                            else
                            {
                                this.nextInitStep = InitSteps.Init_JackUpCylBack;
                            }
                           
                        }
                        break;
                    }
                case InitSteps.Init_JackUpCylBack:
                    {
                        CurMsgStr("顶升气缸回退", "JackUp Cyl Back");

                        if (JackUpCylPush(ModuleDef.JackUp_2, false) 
                            && FingerClose(false)
                            && JackUpCylPush(ModuleDef.JackUp_1, false))
                        {
                            this.nextInitStep = InitSteps.Init_MotorUHome;
                        }
                        break;
                    }
                case InitSteps.Init_MotorUHome:
                    {
                        CurMsgStr("电机U回零", "MotorU Home");

                        if (this.MotorU < 0 || MotorHome(this.MotorU))
                        {
                            this.nextInitStep = InitSteps.Init_CheckBat;
                        }
                        break;
                    }
                case InitSteps.Init_CheckBat:
                    {
                        CurMsgStr("检查电池状态", "Check battery status");

                        //this.nextInitStep = InitSteps.Init_End;
                        //break;

                        // 以下检查功能会有误报，暂时不用
                        bool bCheckOK = true;
                        for (int nColIdx = 0; nColIdx < Battery.GetLength(1) - 1; nColIdx++)
                        {
                            if (!CheckInputState(IBufCheck[nColIdx], Battery[0, nColIdx].Type > BatType.Invalid))
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
                Sleep(100);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        //取料
                        if (GetEvent(this, ModuleEvent.OffloadBufPickBattery, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                        {
                            if (HasBatCount() > 0)
                            {
                                nBatteryOverrTime = DateTime.Now;
                                SetEvent(this, ModuleEvent.OffloadBufPickBattery, EventState.Require);
                            }
                        }

                        //放料
                        if (GetEvent(this, ModuleEvent.OffloadBufPlaceBattery, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                        {
                            if (HasBatCount() <= (int)ModuleDef.Finger_col)
                            {
                                nBatteryOverrTime = DateTime.Now;
                                SetEvent(this, ModuleEvent.OffloadBufPlaceBattery, EventState.Require);
                            }
                        }

                        //// 调整电池
                        //if (HasAdjustBat())
                        //{
                        //    this.nextAutoStep = AutoSteps.Auto_JackUpCylPickBack;
                        //    SaveRunData(SaveType.Variables | SaveType.AutoStep);
                        //    break;
                        //}

                        // 有响应
                        if (GetEvent(this, ModuleEvent.OffloadBufPickBattery, ref curEventState) &&
                            EventState.Response == curEventState)
                        {
                            curRespEvent = ModuleEvent.OffloadBufPickBattery;
                            this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                            SaveRunData(SaveType.Variables | SaveType.AutoStep);
                            break;
                        }
                        if (GetEvent(this, ModuleEvent.OffloadBufPlaceBattery, ref curEventState) &&
                            EventState.Response == curEventState)
                        {
                            curRespEvent = ModuleEvent.OffloadBufPlaceBattery;
                            this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                            SaveRunData(SaveType.Variables | SaveType.AutoStep);
                            break;
                        }
                        if ( (DateTime.Now - nBatteryOverrTime).TotalMinutes > nSetBatTime)
                        {
                            if (HasBatCount()>0)
                            {
                                string strInfo = string.Format("配对台电池存放时间已超过设定值，请手动【取出并清除记忆】");
                                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);								
                            }
                        }
                        break;
                    }
                #region// 转移电池
                case AutoSteps.Auto_JackUpCylPickBack:
                    {
                        CurMsgStr("顶升气缸取料回退", "JackUp Cyl Pick Back");
                        if (JackUpCylPush(ModuleDef.JackUp_2, false)
                            && FingerClose(false)
                            && JackUpCylPush(ModuleDef.JackUp_1, false))
                        {
                            this.nextAutoStep = AutoSteps.Auto_MotorPickMove;
                        }
                        break;
                    }
                case AutoSteps.Auto_MotorPickMove:
                    {
                        CurMsgStr("电机取料移动", "Motor Pick Move");
                        float fPos = -nCurPickCol * fColDistance;
                        if (MotorUMove(fPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_JackUpCylPickPush;
                        }
                        break;
                    }
                case AutoSteps.Auto_JackUpCylPickPush:
                    {
                        CurMsgStr("顶升气缸取料推出", "JackUp Cyl Pick Push");
                        if (JackUpCylPush(ModuleDef.JackUp_1, true)
                            && FingerClose(true)
                            && JackUpCylPush(ModuleDef.JackUp_2, true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PickCheckFinger;
                        }
                        break;
                    }
                case AutoSteps.Auto_PickCheckFinger:
                    {
                        CurMsgStr("取料后检查抓手", "Pick Check Finger");
                        //if (FingerCheck(true))
                        {
                            Battery[0, (int)ModuleDef.Finger_col].CopyFrom(Battery[0, nCurPickCol]);
                            Battery[0, nCurPickCol].Release();
                            this.nextAutoStep = AutoSteps.Auto_MotorPlaceMove;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_MotorPlaceMove:
                    {
                        CurMsgStr("电机放料移动", "Motor Place Move");
                        float fPos = -nCurPlaceCol * fColDistance;
                        if (MotorUMove(fPos) && BufCheck(nCurPickCol, false))
                        {
                            this.nextAutoStep = AutoSteps.Auto_JackUpCylPlaceBack;
                        }
                        break;
                    }
                case AutoSteps.Auto_JackUpCylPlaceBack:
                    {
                        CurMsgStr("顶升气缸放料回退 ", "JackUp Cyl Place Back");
                        if (JackUpCylPush(ModuleDef.JackUp_2, false)
                            && FingerClose(false)
                            && JackUpCylPush(ModuleDef.JackUp_1, false))
                        {
                            this.nextAutoStep = AutoSteps.Auto_PlaceCheckFinger;
                        }
                        break;
                    }
                case AutoSteps.Auto_PlaceCheckFinger:
                    {
                        CurMsgStr("放料后检查抓手", "Place Check Finger");
                        if (BufCheck(nCurPlaceCol, true))
                        {
                            Battery[0, nCurPlaceCol].CopyFrom(Battery[0, (int)ModuleDef.Finger_col]);
                            Battery[0, (int)ModuleDef.Finger_col].Release();
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.Battery | SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion
                case AutoSteps.Auto_WaitActionFinished:
                    {
                        CurMsgStr("等待动作完成", "Wait action finished");
                        // 响应
                        if (CheckEvent(this, curRespEvent, EventState.Response))
                        {
                            SetEvent(this, curRespEvent, EventState.Ready);
                        }

                        // 完成
                        if (CheckEvent(this, curRespEvent, EventState.Finished))
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

            for (int i = 0; i < 2; i++)
            {
                if (IJackUpCylBack[i] < 0 || IJackUpCylPush[i] < 0 || IOpen < 0 || IClose < 0)
                {
                    return false;
                }

                if (!(CheckInputState(IJackUpCylBack[i], true) && CheckInputState(IJackUpCylPush[i], false)))
                {
                    string strError = string.Format("顶升气缸{0}没有回退，配对位电机不能移动", i + 1);
                    ShowMsgBox.ShowDialog(strError, MessageType.MsgWarning);
                    return false;
                }

                if (!(CheckInputState(IOpen, true) && CheckInputState(IClose, false)))
                {
                    string strError = string.Format("夹爪气缸没有松开，配对位电机不能移动");
                    ShowMsgBox.ShowDialog(strError, MessageType.MsgWarning);
                    return false;
                }
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
            curRespEvent = ModuleEvent.ModuleEventInvalid;

            base.InitRunData();
        }

        public bool InitRunDataB()
        {
            if ((AutoSteps)this.nextAutoStep != AutoSteps.Auto_WaitWorkStart
                 && (AutoSteps)this.nextAutoStep != AutoSteps.Auto_WorkEnd)
            {
                string strInfo = string.Format("线体处于交互状态，不能清除数据！");
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return false;
            }
            curRespEvent = ModuleEvent.ModuleEventInvalid;

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
            curRespEvent = (ModuleEvent)FileStream.ReadInt(section, "curRespEvent", (int)curRespEvent);
            nCurPickCol = FileStream.ReadInt(section, "CurPickCol", nCurPickCol);
            nCurPlaceCol = FileStream.ReadInt(section, "CurPlaceCol", nCurPlaceCol);
            nBatteryOverrTime = DateTime.Now;

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
                FileStream.WriteInt(section, "CurPickCol", nCurPickCol);
                FileStream.WriteInt(section, "CurPlaceCol", nCurPlaceCol);
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

            for (ModuleEvent eventIdx = ModuleEvent.OffloadBufPickBattery; eventIdx <= ModuleEvent.OffloadBufPlaceBattery; eventIdx++)
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
            RunProOffloadRobot runOffloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            if (runOffloadRobot.CheckRobotPos((int)OnloadRobotStation.OnloadLine, RobotAction.DOWN))
            {
                ShowMsgBox.ShowDialog("下料机器人在下降位置，请移至安全位", MessageType.MsgMessage);
                return false;
            }
            base.CopyRunDataClearBak();
            curRespEvent = ModuleEvent.ModuleEventInvalid;
            base.InitRunData();
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

            fColDistance = (float)ReadParam(RunModule, "ColDistance", (double)0);
            nSetBatTime = ReadParam(RunModule, "SetBatTime", 180);
            return true;
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;

            // 下料机器人
            strValue = IniFile.ReadString(strModule, "OffloadRobot", "", Def.GetAbsPathName(Def.ModuleExCfg));
            offloadRobot = MachineCtrl.GetInstance().GetModule(strValue) as RunProOffloadRobot;
        }

        #endregion

        
        #region // 抓手、电机和暂存硬件操作

        /// <summary>
        /// 抓手关闭
        /// </summary>
        private bool FingerClose(bool close)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            // 操作
            if (OClose< 0 || OOpen< 0)
            {
                return true;
            }
            OutputAction(OClose, close);
            OutputAction(OOpen, !close);

            // 检查到位
            if (!(WaitInputState(IClose, close) && WaitInputState(IOpen, !close)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 抓手检查
        /// </summary>
        private bool FingerCheck(bool hasBat)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            if (!CheckInputState(IFingerCheck, hasBat))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 暂存检查
        /// </summary>
        public bool BufCheck(int nBufIdx, bool hasBat)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }
            if (nBufIdx > IBufCheck.Length - 1)
            {
                return !hasBat;
            }
            if (!InputState(IBufCheck[nBufIdx], hasBat))
            {
                Sleep(200);
                if (!CheckInputState(IBufCheck[nBufIdx], hasBat))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 顶升气缸推出
        /// </summary>
        private bool JackUpCylPush(ModuleDef JackUp, bool bPush)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            int nCylIdx = (int)JackUp;
            // 检查IO配置
            if (IJackUpCylBack[nCylIdx] < 0 || IJackUpCylPush[nCylIdx] < 0 || OJackUpCylBack[nCylIdx] < 0 || OJackUpCylPush[nCylIdx] < 0)
            {
                return false;
            }

            // 操作
            OutputAction(OJackUpCylBack[nCylIdx], !bPush);
            OutputAction(OJackUpCylPush[nCylIdx], bPush);

            // 检查到位
            if (!(WaitInputState(IJackUpCylBack[nCylIdx], !bPush) && WaitInputState(IJackUpCylPush[nCylIdx], bPush)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 配对电机移动
        /// </summary>
        private bool MotorUMove(float offset = 0)
        {
            if (this.MotorU < 0)
            {
                return true;
            }
            return MotorMove(this.MotorU, 0, offset);
        }
        #endregion


        #region // 取放料计算

        /// <summary>
        /// 需要调整电池
        /// </summary>
        private bool HasAdjustBat()
        {
            nCurPlaceCol = -1;
            for (int i = 1; i < Battery.GetLength(1) - 2; i++)
            {
                if (Battery[0, i].Type == BatType.Invalid && nCurPlaceCol == -1)
                {
                    nCurPlaceCol = i;
                }
                if (nCurPlaceCol > -1 && Battery[0, i + 1].Type == BatType.OK)
                {
                    nCurPickCol = i + 1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 配对位电池数量
        /// </summary>
        public int HasBatCount()
        {
            int count = 0;
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (Battery[0, i].Type > BatType.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion


        #region // 检查

        /// <summary>
        /// 有料检查
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        public bool HasBattery(int nIndex)
        {
            if (Battery[0, nIndex].Type > BatType.Invalid)
            {
                return true;
            }
            return false;
        }
        public bool HasBattery()
        {
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (Battery[0, i].Type > BatType.Invalid)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 电池位置和数量
        /// </summary>
        /// <param name="bufBit"></param>
        /// <param name="bufBatNum"></param>
        public void BatPosAndCount(ref int bufBit, ref int bufBatNum)
        {
            for (int i = 0; i < Battery.GetLength(1); i++)
            {
                if (Battery[0, i].Type == BatType.OK)
                {
                    bufBit |= (0x01 << i);
                    bufBatNum++;
                }
            }
        }
        #endregion
    }
}
