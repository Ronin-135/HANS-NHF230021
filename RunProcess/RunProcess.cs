using EnumsNET;
using FastDeepCloner;
using HelperLibrary;
using Newtonsoft.Json;
using Prism.Mvvm;
using PropertyTools.Wpf;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Windows.Input;
using SystemControlLibrary;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using WPFMachine.Frame.ReadWriteData;
using WPFMachine.Frame.Userlib;
using WPFMachine.Views.Viewinterface;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public class RunProcess : RunEx, IPropView
    {
        #region // 步骤

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_End,
        }

        /// <summary>
        /// 检查步骤
        /// </summary>
        protected enum CheckSteps
        {
            /// <summary>
            /// 检查机器人位置
            /// </summary>
            Check_WorkStart = 0,

            Check_WorkEnd,
        }

        /// <summary>
        /// 自动运行步骤
        /// </summary>
        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_WorkEnd,
        }

        #endregion


        #region // 字段

        // 模组数据
        private Dictionary<string, ParameterFormula> insertParameterList;      // 模组中插入的参数集：<参数关键字key, 参数样式>
        private Dictionary<string, ParameterFormula> dataBaseParameterList;    // 数据库中保存的参数集：<参数关键字key, 参数样式>
        protected DataBaseRecord dbRecord;      // 数据库记录集
        private bool initStepSafe;              // 初始化安全标识
        private bool autoStepSafe;              // 自动运行安全标识
        private object autoCheckStep;           // 自动运行时的检查步骤
        public object lockDataBase = new object();            // 数据库更新锁

        // 模组参数
        private bool onLoad;                    // 上料使能
        private bool offLoad;                   // 下料使能

        // 自定义数据
        private object lockEvent;               // 信号锁
        private MEvent[] arrEvent;              // 模组信号
        private Battery[,] battery;             // 电池数组
        public Pallet[] pallet;                // 托盘数组
        private IniStream fileStream;           // Ini文件流
        private List<(MemberInfo memberInfo, Save att)> SaveIniList { get; } = new();

        #endregion


        #region // 属性

        /// <summary>
        /// 上料使能
        /// </summary>
        public bool OnLoad
        {
            get
            {
                return onLoad;
            }

            protected set
            {
                this.onLoad = value;
            }
        }

        /// <summary>
        /// 下料使能
        /// </summary>
        public bool OffLoad
        {
            get
            {
                return offLoad;
            }

            protected set
            {
                this.offLoad = value;
            }
        }

        /// <summary>
        /// 初始化安全
        /// </summary>
        public bool InitStepSafe
        {
            get
            {
                return initStepSafe;
            }

            protected set
            {
                this.initStepSafe = value;
            }
        }

        /// <summary>
        /// 自动运行安全
        /// </summary>
        public bool AutoStepSafe
        {
            get
            {
                return autoStepSafe;
            }

            protected set
            {
                this.autoStepSafe = value;
            }
        }

        /// <summary>
        /// 自动运行检查步骤
        /// </summary>
        public object AutoCheckStep
        {
            get
            {
                return autoCheckStep;
            }

            protected set
            {
                this.autoCheckStep = value;
            }
        }

        /// <summary>
        /// 数据库
        /// </summary>
        public DataBaseRecord DBRecord
        {
            get
            {
                return dbRecord;
            }

            protected set
            {
                dbRecord = value;
            }
        }


        /// <summary>
        /// 信号锁
        /// </summary>
        public object LockEvent
        {
            get
            {
                return this.lockEvent;
            }

            protected set
            {
                this.lockEvent = value;
            }
        }

        /// <summary>
        /// 模组信号
        /// </summary>
        public MEvent[] ArrEvent
        {
            get
            {
                return this.arrEvent;
            }

            set
            {
                this.arrEvent = value;
            }
        }

        /// <summary>
        /// 电池数组
        /// </summary>
        public Battery[,] Battery
        {
            get
            {
                return this.battery;
            }

            protected set
            {
                this.battery = value;
            }
        }

        /// <summary>
        /// 托盘数组
        /// </summary>
        public Pallet[] Pallet
        {
            get
            {
                return this.pallet;
            }

            set
            {
                this.pallet = value;
            }
        }

        /// <summary>
        /// Ini文件流
        /// </summary>
        public IniStream FileStream
        {
            get
            {
                return this.fileStream;
            }

            protected set
            {
                this.fileStream = value;
            }
        }


        private INotifyPropertyChanged dyCacheType;
        public INotifyPropertyChanged DyCacheType => dyCacheType ??= IPropView.CreateDyType(ParameterProperty, $"{this.GetType().Name}{this.RunModule}", this, (s, e) =>
        {
            var param = ParameterProperty.OfType<Property>().First(p => p.DisplayName == e.PropertyName);
            var value = s.GetType().GetProperty(param.DisplayName).GetValue(s).ToString();
            Param(param.DisplayName, dataBaseParameterList[param.Name].value.ToString(), value);

            WriteParameterCode(RunModule, param.Name, value);
            ReadParameter();
            #endregion

        });
        #region 界面使用属性
        public virtual IEnumerable<Battery> Bat => Battery.OfType<Battery>();

        public virtual int MaxCol => Battery.GetLength(1);
        public virtual int MaxRow => Battery.GetLength(0);

        #endregion


        #region // 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public RunProcess(int RunID) : base(RunID)
        {
            // 创建对象
            this.dbRecord = MachineCtrl.GetInstance().dbRecord;
            this.insertParameterList = new Dictionary<string, ParameterFormula>();
            this.dataBaseParameterList = new Dictionary<string, ParameterFormula>();
            this.fileStream = new IniStream();

            // 插入通用参数
            OnLoad = OffLoad = false;
            InsertPublicParam("OnLoad", "上料使能", "上料使能：True启用，False禁用", this.OnLoad, ParameterLevel.PL_STOP_MAIN);
            InsertPublicParam("OffLoad", "下料使能", "下料使能：True启用，False禁用", this.OffLoad, ParameterLevel.PL_STOP_MAIN);
            // 获取要自动保存的数据
            SaveIniList = this.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(m => m.MemberType.HasAnyFlags(MemberTypes.Field | MemberTypes.Property))
                .Where(m => m.GetCustomAttributes(typeof(Save), false).Length > 0)
                .Select(m => (m, (Save)m.GetCustomAttributes(typeof(Save), false).First()))
                .ToList();
        }

        #endregion


        #region // 配置读写

        /// <summary>
        /// 读取模组配置
        /// </summary>
        public override bool InitializeConfig(string module)
        {
            this.RunModule = module;
            this.RunName = IniFile.ReadString(module, "Name", "", Def.GetAbsPathName(Def.ModuleExCfg));
            this.RunClass = IniFile.ReadString(module, "Class", "", Def.GetAbsPathName(Def.ModuleExCfg));

            // 打开运行数据文件
            string strInfo = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), module);
            if (!this.FileStream.OpenRead(string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), module)))
            {
                Trace.Assert(false, string.Format("RunProcess({0}).InitializeConfig().FileStream.OpenRead() fail.", module));
                return false;
            }

            // 数据库中读取模组参数
            List<ParameterFormula> listPara = new List<ParameterFormula>();
            this.dbRecord.GetParameterList(Def.GetProductFormula(), module, listPara);
            foreach (var item in insertParameterList)
            {
                var dbParam = listPara.FirstOrDefault(para => para.key == item.Key);
                if (dbParam != default)
                {
                    this.dataBaseParameterList.Add(item.Key, dbParam);
                }
                else
                {
                    dataBaseParameterList.Add(item.Key, item.Value);
                }
            }

            // 基类初始化
            return base.InitializeConfig(module);
        }

        /// <summary>
        /// 保存模组配置
        /// </summary>
        public override bool SaveConfig()
        {
            return base.SaveConfig();
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
                        CurMsgStr("数据恢复", "Init data recover");
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
                        Trace.Assert(false, "RunProcess.InitOperation/no this init step");
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

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        break;
                    }
                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "RunEx::AutoOperation/no this run step");
                        break;
                    }
            }
        }

        #endregion


        #region // 防呆检查

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        public virtual bool CheckOutputCanActive(Output output, bool bOn)
        {
            return true;
        }

        /// <summary>
        /// 检查电机是否可移动
        /// </summary>
        public virtual bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public virtual void MonitorAvoidDie()
        {
            return;
        }


        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public virtual void AfterStopAction()
        {
            this.AutoCheckStep = CheckSteps.Check_WorkStart;
        }
        #endregion


        #region // 运行数据读写

        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public virtual void InitRunData()
        {
            this.nextInitStep = InitSteps.Init_DataRecover;
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            this.InitStepSafe = false;
            this.AutoStepSafe = false;
            this.AutoCheckStep = 0;

            // 信号初始化
            if (null != ArrEvent)
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx, (RunID)this.GetRunID());
                }
            }

            // 电池组初始化
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

            // 托盘组初始化
            if (null != Pallet)
            {
                for (int nPltIdx = 0; nPltIdx < Pallet.Length; nPltIdx++)
                {
                    Pallet[nPltIdx].Release();
                }
            }
        }

        /// <summary>
        /// 加载运行数据
        /// </summary>
        public virtual void LoadRunData()
        {
            string section, key;
            section = this.RunModule;

            // 读取自动步骤
            this.nextAutoStep = FileStream.ReadInt(section, "nextAutoStep", (int)this.nextAutoStep);

            // 读取模组信号
            for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
            {
                int nParam = 0;
                int nColIdx = 0;
                int nRowIdx = 0;
                EventState State = EventState.Invalid;
                ModuleEvent ModEvent = ModuleEvent.ModuleEventInvalid;
                ArrEvent[nEventIdx].GetEvent(ref ModEvent, ref State, ref nRowIdx, ref nColIdx, ref nParam);

                // 事件
                key = string.Format("ArrEvent[{0}].ModEvent", nEventIdx);
                ModEvent = (ModuleEvent)FileStream.ReadInt(section, key, (int)ModEvent);
                // 状态
                key = string.Format("ArrEvent[{0}].State", nEventIdx);
                State = (EventState)FileStream.ReadInt(section, key, (int)State);
                // 行号
                key = string.Format("ArrEvent[{0}].RowIdx", nEventIdx);
                nRowIdx = FileStream.ReadInt(section, key, nRowIdx);
                // 列号
                key = string.Format("ArrEvent[{0}].ColIdx", nEventIdx);
                nColIdx = FileStream.ReadInt(section, key, nColIdx);
                // 参数
                key = string.Format("ArrEvent[{0}].Param", nEventIdx);
                nParam = FileStream.ReadInt(section, key, nParam);

                // 设置事件
                ArrEvent[nEventIdx].SetEvent(ModEvent, State, nRowIdx, nColIdx, nParam, (RunID)this.GetRunID());
            }

            // 读取电池数据
            for (int nRowIdx = 0; nRowIdx < Battery.GetLength(0); nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                {
                    key = string.Format("Battery[{0},{1}].Type", nRowIdx, nColIdx);
                    Battery[nRowIdx, nColIdx].Type = (BatType)FileStream.ReadInt(section, key, (int)Battery[nRowIdx, nColIdx].Type);

                    key = string.Format("Battery[{0},{1}].NGType", nRowIdx, nColIdx);
                    Battery[nRowIdx, nColIdx].NGType = (BatNGType)FileStream.ReadInt(section, key, (int)Battery[nRowIdx, nColIdx].NGType);

                    key = string.Format("Battery[{0},{1}].Code", nRowIdx, nColIdx);
                    Battery[nRowIdx, nColIdx].Code = FileStream.ReadString(section, key, Battery[nRowIdx, nColIdx].Code);
                }
            }

            // 读取托盘数据
            for (int nPltIdx = 0; nPltIdx < this.Pallet.Length; nPltIdx++)
            {
                key = string.Format("Pallet[{0}].Code", nPltIdx);
                this.Pallet[nPltIdx].Code = FileStream.ReadString(section, key, this.Pallet[nPltIdx].Code);

                key = string.Format("Pallet[{0}].Type", nPltIdx);
                this.Pallet[nPltIdx].Type = (PltType)FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].Type);

                key = string.Format("Pallet[{0}].Stage", nPltIdx);
                this.Pallet[nPltIdx].Stage = (PltStage)FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].Stage);

                key = string.Format("Pallet[{0}].RowCount", nPltIdx);
                this.Pallet[nPltIdx].RowCount = FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].RowCount);

                key = string.Format("Pallet[{0}].ColCount", nPltIdx);
                this.Pallet[nPltIdx].ColCount = FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].ColCount);

                key = string.Format("Pallet[{0}].IsOnloadFake", nPltIdx);
                this.Pallet[nPltIdx].IsOnloadFake = FileStream.ReadBool(section, key, this.Pallet[nPltIdx].IsOnloadFake);

                key = string.Format("Pallet[{0}].SrcStation", nPltIdx);
                this.Pallet[nPltIdx].SrcStation = FileStream.ReadInt(section, key, this.Pallet[nPltIdx].SrcStation);

                key = string.Format("Pallet[{0}].SrcRow", nPltIdx);
                this.Pallet[nPltIdx].SrcRow = FileStream.ReadInt(section, key, this.Pallet[nPltIdx].SrcRow);

                key = string.Format("Pallet[{0}].SrcCol", nPltIdx);
                this.Pallet[nPltIdx].SrcCol = FileStream.ReadInt(section, key, this.Pallet[nPltIdx].SrcCol);

                key = string.Format("Pallet[{0}].StartTime", nPltIdx);
                this.Pallet[nPltIdx].StartTime = FileStream.ReadString(section, key, this.Pallet[nPltIdx].StartTime);

                key = string.Format("Pallet[{0}].EndTime", nPltIdx);
                this.Pallet[nPltIdx].EndTime = FileStream.ReadString(section, key, this.Pallet[nPltIdx].EndTime);

                key = string.Format("Pallet[{0}].PosInOven.OvenID", nPltIdx);
                this.Pallet[nPltIdx].PosInOven.OvenID = FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].PosInOven.OvenID);

                key = string.Format("Pallet[{0}].PosInOven.OvenRowID", nPltIdx);
                this.Pallet[nPltIdx].PosInOven.OvenRowID = FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].PosInOven.OvenRowID);

                key = string.Format("Pallet[{0}].PosInOven.OvenColID", nPltIdx);
                this.Pallet[nPltIdx].PosInOven.OvenColID = FileStream.ReadInt(section, key, (int)this.Pallet[nPltIdx].PosInOven.OvenColID);

                // 电池数据
                for (int nRowIdx = 0; nRowIdx < Pallet[nPltIdx].Bat.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Pallet[nPltIdx].Bat.GetLength(1); nColIdx++)
                    {
                        key = string.Format("Pallet[{0}].Bat[{1},{2}].Type", nPltIdx, nRowIdx, nColIdx);
                        Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Type = (BatType)FileStream.ReadInt(section, key, (int)Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Type);

                        key = string.Format("Pallet[{0}].Bat[{1},{2}].NGType", nPltIdx, nRowIdx, nColIdx);
                        Pallet[nPltIdx].Bat[nRowIdx, nColIdx].NGType = (BatNGType)FileStream.ReadInt(section, key, (int)Pallet[nPltIdx].Bat[nRowIdx, nColIdx].NGType);

                        key = string.Format("Pallet[{0}].Bat[{1},{2}].Code", nPltIdx, nRowIdx, nColIdx);
                        Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Code = FileStream.ReadString(section, key, Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Code);
                    }
                }

                // 自动保存
                foreach (var meinfo in SaveIniList)
                {
                    var str = FileStream.ReadString(section, meinfo.memberInfo.Name, meinfo.att.def != null ? Save.Serialize(meinfo.att.def) : Save.Serialize(meinfo.memberInfo.GetValue(this)));
                    meinfo.memberInfo.SetValue(this, Save.DeserializeObject(str, meinfo.memberInfo.GetOperationType()));
                }

            }
        }
        /// <summary>
        /// 保存运行数据
        /// </summary>
        public virtual void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key;
            section = this.RunModule;

            // 写自动步骤
            if (SaveType.AutoStep == (SaveType.AutoStep & saveType))
            {
                FileStream.WriteInt(section, "nextAutoStep", (int)this.nextAutoStep);
            }

            // 写模组信号
            if (SaveType.SignalEvent == (SaveType.SignalEvent & saveType))
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    int nParam = 0;
                    int nColIdx = 0;
                    int nRowIdx = 0;
                    EventState State = EventState.Invalid;
                    ModuleEvent ModEvent = ModuleEvent.ModuleEventInvalid;
                    ArrEvent[nEventIdx].GetEvent(ref ModEvent, ref State, ref nRowIdx, ref nColIdx, ref nParam);

                    // 事件
                    key = string.Format("ArrEvent[{0}].ModEvent", nEventIdx);
                    FileStream.WriteInt(section, key, (int)ModEvent);
                    // 状态
                    key = string.Format("ArrEvent[{0}].State", nEventIdx);
                    FileStream.WriteInt(section, key, (int)State);
                    // 行号
                    key = string.Format("ArrEvent[{0}].RowIdx", nEventIdx);
                    FileStream.WriteInt(section, key, nRowIdx);
                    // 列号
                    key = string.Format("ArrEvent[{0}].ColIdx", nEventIdx);
                    FileStream.WriteInt(section, key, nColIdx);
                    // 参数
                    key = string.Format("ArrEvent[{0}].Param", nEventIdx);
                    FileStream.WriteInt(section, key, nParam);
                }
            }

            // 写电池数据
            if (SaveType.Battery == (SaveType.Battery & saveType))
            {
                for (int nRowIdx = 0; nRowIdx < Battery.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Battery.GetLength(1); nColIdx++)
                    {
                        key = string.Format("Battery[{0},{1}].Type", nRowIdx, nColIdx);
                        FileStream.WriteInt(section, key, (int)Battery[nRowIdx, nColIdx].Type);

                        key = string.Format("Battery[{0},{1}].NGType", nRowIdx, nColIdx);
                        FileStream.WriteInt(section, key, (int)Battery[nRowIdx, nColIdx].NGType);

                        key = string.Format("Battery[{0},{1}].Code", nRowIdx, nColIdx);
                        FileStream.WriteString(section, key, Battery[nRowIdx, nColIdx].Code);
                    }
                }
            }

            // 写托盘数据
            if (SaveType.Pallet == (SaveType.Pallet & saveType))
            {
                for (int nPltIdx = 0; nPltIdx < this.Pallet.Length; nPltIdx++)
                {
                    if ((nPltIdx == index) || (index < 0))
                    {
                        key = string.Format("Pallet[{0}].Code", nPltIdx);
                        FileStream.WriteString(section, key, this.Pallet[nPltIdx].Code);

                        key = string.Format("Pallet[{0}].Type", nPltIdx);
                        FileStream.WriteInt(section, key, (int)this.Pallet[nPltIdx].Type);

                        key = string.Format("Pallet[{0}].Stage", nPltIdx);
                        FileStream.WriteInt(section, key, (int)this.Pallet[nPltIdx].Stage);

                        key = string.Format("Pallet[{0}].RowCount", nPltIdx);
                        FileStream.WriteInt(section, key, (int)this.Pallet[nPltIdx].RowCount);

                        key = string.Format("Pallet[{0}].ColCount", nPltIdx);
                        FileStream.WriteInt(section, key, (int)this.Pallet[nPltIdx].ColCount);

                        key = string.Format("Pallet[{0}].IsOnloadFake", nPltIdx);
                        FileStream.WriteBool(section, key, this.Pallet[nPltIdx].IsOnloadFake);

                        key = string.Format("Pallet[{0}].SrcStation", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].SrcStation);

                        key = string.Format("Pallet[{0}].SrcRow", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].SrcRow);

                        key = string.Format("Pallet[{0}].SrcCol", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].SrcCol);

                        key = string.Format("Pallet[{0}].StartTime", nPltIdx);
                        FileStream.WriteString(section, key, this.Pallet[nPltIdx].StartTime);

                        key = string.Format("Pallet[{0}].EndTime", nPltIdx);
                        FileStream.WriteString(section, key, this.Pallet[nPltIdx].EndTime);

                        key = string.Format("Pallet[{0}].PosInOven.OvenID", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].PosInOven.OvenID);

                        key = string.Format("Pallet[{0}].PosInOven.OvenRowID", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].PosInOven.OvenRowID);

                        key = string.Format("Pallet[{0}].PosInOven.OvenColID", nPltIdx);
                        FileStream.WriteInt(section, key, this.Pallet[nPltIdx].PosInOven.OvenColID);
                        // 电池数据
                        for (int nRowIdx = 0; nRowIdx < Pallet[nPltIdx].Bat.GetLength(0); nRowIdx++)
                        {
                            for (int nColIdx = 0; nColIdx < Pallet[nPltIdx].Bat.GetLength(1); nColIdx++)
                            {
                                key = string.Format("Pallet[{0}].Bat[{1},{2}].Type", nPltIdx, nRowIdx, nColIdx);
                                FileStream.WriteInt(section, key, (int)Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Type);

                                key = string.Format("Pallet[{0}].Bat[{1},{2}].NGType", nPltIdx, nRowIdx, nColIdx);
                                FileStream.WriteInt(section, key, (int)Pallet[nPltIdx].Bat[nRowIdx, nColIdx].NGType);

                                key = string.Format("Pallet[{0}].Bat[{1},{2}].Code", nPltIdx, nRowIdx, nColIdx);
                                FileStream.WriteString(section, key, Pallet[nPltIdx].Bat[nRowIdx, nColIdx].Code);
                            }
                        }
                    }
                }
            }
            if (RunClass == "RunProDryingOven" || RunClass == "RunProOnloadRobot" || RunClass == "RunProOffloadRobot")
            {
                CopyRunData();
            }
            FileStream.DataToFile();
        }

        /// <summary>
        /// 复制运行数据
        /// </summary>
        public void CopyRunData()
        {
            try
            {
                string strDataFolder = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), this.RunModule);
                string strDataBackupFolder = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataBakFolder), this.RunModule);

                if (File.Exists(strDataFolder))
                {
                    fileStream.FileLock.EnterReadLock();
                    // 复制并覆盖文件
                    File.Copy(strDataFolder, strDataBackupFolder, true);
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.CopyRunData() fail: " + ex.Message);
            }
            finally
            {
                fileStream.FileLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 删除运行数据
        /// </summary>
        public void DeleteRunData()
        {
            try
            {
                string strDataFolder = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), this.RunModule);
                string strDataBackupFolder = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataBakFolder), this.RunModule);

                if (File.Exists(strDataFolder))
                {
                    // 复制并覆盖文件
                    File.Copy(strDataFolder, strDataBackupFolder, true);
                }

                // 清除数据
                this.FileStream.ClearData();
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.DeleteRunData() fail: " + ex.Message);
            }
        }

        /// <summary>
        /// 备份删除数据运行数据
        /// </summary>
        public void CopyRunDataClearBak()
        {
            try
            {
                string sFilePath = Def.GetAbsPathName(Def.RunDataClearBakFolder) + DateTime.Now.ToString("yyyyMMdd") + "\\";
                string sFileName = this.RunModule + DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss");
                string strDataFolder = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), this.RunModule);
                string strDataBackupFolder = string.Format("{0}{1}.cfg", sFilePath, sFileName);
                if (!Directory.Exists(sFilePath)) Directory.CreateDirectory(sFilePath);
                if (File.Exists(strDataFolder))
                {
                    // 复制并覆盖文件
                    File.Copy(strDataFolder, strDataBackupFolder, true);
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("RunProcess.CopyRunDataClearBak() fail: " + ex.Message);
            }
            FileStream.DataToFile();
        }
        #endregion


        #region // 模组参数和相关模组读取

        /// <summary>
        /// 参数检查
        /// </summary>
        public virtual bool CheckParameter(string name, object value)
        {
            return true;
        }

        /// <summary>
        /// 参数读取
        /// </summary>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadParam(this.RunModule, "OnLoad", false);
            this.OffLoad = ReadParam(this.RunModule, "OffLoad", false);
            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public virtual void ReadRelatedModule()
        {
            return;
        }

        #endregion


        #region // 添加IO/电机

        /// <summary>
        /// 添加输入点位
        /// </summary>
        protected void InputAdd(string strKey, ref int nValue)
        {
            string value = IniFile.ReadString(RunModule, strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
            nValue = MachineCtrl.GetInstance().DecodeInputID(value);
            inputMap.Add(strKey, nValue);
        }

        /// <summary>
        /// 添加输出点
        /// </summary>
        protected void OutputAdd(string strKey, ref int nValue)
        {
            string value = IniFile.ReadString(RunModule, strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
            nValue = MachineCtrl.GetInstance().DecodeOutputID(value);
            outputMap.Add(strKey, nValue);
        }

        /// <summary>
        /// 添加电机
        /// </summary>
        protected void MotorAdd(string strKey, ref int nValue)
        {
            string value = IniFile.ReadString(RunModule, strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
            nValue = MachineCtrl.GetInstance().DecodeMotorID(value);
            motorMap.Add(strKey, nValue);
        }

        #endregion


        #region // 参数插入

        /// <summary>
        /// 获取模组参数列表（界面使用）
        /// </summary>
        public PropertyManage GetParameterList()
        {
            PropertyManage pm = this.ParameterProperty;
            foreach (Property item in this.ParameterProperty)
            {
                if (null != pm[item.Name])
                {
                    if (item.Value is int || item.Value is uint)
                    {
                        pm[item.Name].Value = ReadParam(this.RunModule, item.Name, Convert.ToInt32(item.Value));
                    }
                    else if (item.Value is bool)
                    {
                        pm[item.Name].Value = ReadParam(this.RunModule, item.Name, Convert.ToBoolean(item.Value));
                    }
                    else if (item.Value is double || item.Value is float)
                    {
                        pm[item.Name].Value = ReadParam(this.RunModule, item.Name, Convert.ToDouble(item.Value));
                    }
                    else if (item.Value is string)
                    {
                        pm[item.Name].Value = ReadParam(this.RunModule, item.Name, Convert.ToString(item.Value));
                    }
                }
            }
            return pm;
        }

        /// <summary>
        /// 添加通用参数（所有模组都有）
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertPublicParam(string key, string name, string description, object value, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraLevel));
            this.ParameterProperty.Add("通用组参数", key, name, description, value, (int)paraLevel, readOnly, visible);
        }

        /// <summary>
        /// 添加私有参数（模组独有参数）
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertPrivateParam(string key, string name, string description, object value, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraLevel));
            this.ParameterProperty.Add("模组参数", key, name, description, value, (int)paraLevel, readOnly, visible);
        }

        #endregion


        #region // 数据库参数读写




        protected T ReadParam<T>(string section, string key, T defaultValue)
        {
            if ((section == this.RunModule) && this.dataBaseParameterList.ContainsKey(key))
            {
                var val = this.dataBaseParameterList[key].value;
                if (string.IsNullOrWhiteSpace(val)) return defaultValue;
                if (typeof(T).IsEnum)
                {
                    Enum.Parse(typeof(T), val);
                    return defaultValue;
                }
                defaultValue = (T)Convert.ChangeType(val, typeof(T));
            }
            return defaultValue;
        }

        public T ReadParam<T>(string key, T defaultValue) => ReadParam(RunModule, key, defaultValue);



        /// <summary>
        /// 写入数据库参数（界面调用）
        /// </summary>
        public bool WriteParameterCode(string key, string value)
        {
            IniFile.WriteString("Parameter", key, value, Def.GetAbsPathName(Def.MachineCfg));
            return true;
        }

        /// <summary>
        /// 写入数据库参数（界面调用）
        /// </summary>

        private bool WriteParameter(string section, string key, string value, Action<string, string> synchronizationParameter)
        {
            var result = false;


            lock (lockDataBase)
            {
                if (section == this.RunModule)
                {
                    if (this.insertParameterList.ContainsKey(key))
                    {
                        ParameterFormula insertPara, dbPara;
                        insertPara = this.insertParameterList[key];

                        insertPara.module = section;
                        insertPara.value = value;
                        if (this.dataBaseParameterList.ContainsKey(key))
                        {
                            dbPara = this.dataBaseParameterList[key];
                            dbPara.value = insertPara.value;
                            dbPara.level = insertPara.level;
                            result = this.dbRecord.ModifyParameter(dbPara);
                        }
                        else
                        {
                            dataBaseParameterList.Add(key, insertParameterList[key]);
                            result = this.dbRecord.AddParameter(insertPara);
                        }

                        #region // 保存之后立即读取

                        synchronizationParameter?.Invoke(section, key);

                        #endregion
                    }
                }
            }


            return result;
        }

        private static void Param(string paramName, string oldValue, string value)
        {
            if (oldValue==value)
            {
                return;
            }
            //string sFilePath = "D:\\InterfaceOpetate\\ParameterChanged";
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\ParameterChanged", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "参数修改.CSV";
            string sColHead = "修改时间,账号, 参数名,参数旧值,参数新值";
            string sLog = string.Format("{0},{1},{2},{3},{4}"
            , DateTime.Now
                , MachineCtrl.MachineCtrlInstance.CurUser.UserName, paramName, oldValue, value
               );
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }



        /// <summary>
        /// 写入数据库参数（代码调用）
        /// </summary>
        public virtual void SaveParameter()
        {
            SynchronizationParameter(RunModule);
            return;
        }


        /// <summary>
        /// 写入数据库参数（代码调用）
        /// </summary>
        public bool WriteParameterCode(string section, string key, string value) => WriteParameter(section, key, value, null);



        /// <summary>
        /// 同步参数属性
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        private void SynchronizationParameter(string section, string key = null)
        {
            lock (lockDataBase)
            {
                List<ParameterFormula> listPara = new List<ParameterFormula>();
                this.dbRecord.GetParameterList(Def.GetProductFormula(), section, listPara);

                foreach (var item in listPara)
                {
                    //if (item.key == key || key == null)
                    //{
                    this.dataBaseParameterList[item.key] = item;
                    // 同步到UI
                    var paraInfo = DyCacheType.GetType().GetProperty(item.name);
                    paraInfo?.SetValue(DyCacheType, Convert.ChangeType(item.value, paraInfo.PropertyType));
                    //}

                }
            }

        }
        /// <summary>
        /// 同步参数字段
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        private void SynchronizationParameterFields(string section, string key = null)
        {
            lock (lockDataBase)
            {
                List<ParameterFormula> listPara = new List<ParameterFormula>();
                this.dbRecord.GetParameterList(Def.GetProductFormula(), section, listPara);

                foreach (var item in listPara)
                {
                    if (item.key == key)
                    {
                        this.dataBaseParameterList[item.key] = item;
                        // 同步到UI
                        var paraInfo = DyCacheType.GetType().GetField($"<f>{item.name}", BindingFlags.Instance | BindingFlags.NonPublic);
                        paraInfo?.SetValue(DyCacheType, Convert.ChangeType(item.value, paraInfo.FieldType));
                    }

                }
            }

        }

        #endregion


        #region // 插入报警信息到数据库

        /// <summary>
        /// 插入历史报警记录
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="msg">报警信息</param>
        /// <param name="msgType">报警类型</param>
        /// <param name="runModuleID">运行模组ID</param>
        /// <param name="runName">运行模组名</param>
        /// <param name="productFormula">产品参数ID</param>
        /// <param name="curTime">当前时间</param>
        public override void InsertAlarmInfo(int msgID, string msg, int msgType, int runModuleID, string runName, int productFormula, DateTime curTime)
        {
            try
            {
                lock (lockDataBase)
                {
                    dbRecord.AddAlarmInfo(new AlarmFormula(productFormula, msgID, msg, msgType, runModuleID, runName, curTime));
                }
            }
            catch { }
        }

        /// <summary>
        /// 记录历史报警记录
        /// </summary>
        ///  /// <param name="MsgID">报警ID</param>
        /// <param name="msg">报警信息</param>
        /// <param name="msgType">报警类型</param>
        public void RecordMessageInfo(int MsgID, string msg, MessageType msgType)
        {
            lock (lockDataBase)
            {
                InsertAlarmInfo(MsgID, msg, (int)msgType, GetRunID(), RunModule, MachineCtrl.GetInstance().ProductFormula, DateTime.Now);
            }
        }
        #endregion


        #region // IO和电机操作

        /// <summary>
        /// 获取输入
        /// </summary>
        public Input Inputs(int index)
        {
            return DeviceManager.Inputs(index);
        }

        /// <summary>
        /// 获取输出
        /// </summary>
        public Output Outputs(int index)
        {
            return DeviceManager.Outputs(index);
        }

        /// <summary>
        /// 获取电机
        /// </summary>
        public Motor Motors(int index)
        {
            return DeviceManager.Motors(index);
        }

        /// <summary>
        /// 查看输入状态
        /// </summary>
        public bool InputState(int input, bool isOn)
        {
            if (input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return (isOn ? DeviceManager.Inputs(input).IsOn() : DeviceManager.Inputs(input).IsOff());
        }

        /// <summary>
        /// 检查输入状态（可报警）
        /// </summary>
        public bool CheckInputState(int input, bool isOn)
        {
            if (input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return CheckInput(DeviceManager.Inputs(input), isOn);
        }

        /// <summary>
        /// 等待输入状态（有报警）
        /// </summary>
        public bool WaitInputState(int input, bool isOn)
        {
            if (input < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return WaitInput(DeviceManager.Inputs(input), isOn);
        }

        /// <summary>
        /// 查看输出状态
        /// </summary>
        public bool OutputState(int output, bool isOn)
        {
            if (output < 0 || Def.IsNoHardware())
            {
                return true;
            }
            return (isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff());
        }

        /// <summary>
        /// 输出状态
        /// </summary>
        public bool OutputAction(int output, bool isOn)
        {
            if (output < 0 || Def.IsNoHardware())
            {
                return true;
            }

            if (isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff())
            {
                return true;
            }

            return (isOn ? DeviceManager.Outputs(output).On() : DeviceManager.Outputs(output).Off());
        }

        #endregion


        #region // 自定义方法

        #region // 模组托盘、电池、信号初始化

        /// <summary>
        /// 初始化创建托盘、电池、信号
        /// </summary>
        public void InitCreateObject(int nPltCount, int nBatRow, int nBatCol, int nEventCount)
        {
            // 创建托盘
            this.Pallet = new Pallet[nPltCount];
            for (int nPltIdx = 0; nPltIdx < nPltCount; nPltIdx++)
            {
                this.Pallet[nPltIdx] = new Pallet();
            }

            // 创建电池数组
            this.Battery = new Battery[nBatRow, nBatCol];
            for (int nRowIdx = 0; nRowIdx < nBatRow; nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < nBatCol; nColIdx++)
                {
                    this.Battery[nRowIdx, nColIdx] = new Battery();
                }
            }

            // 创建信号
            this.lockEvent = new object();
            this.ArrEvent = new MEvent[nEventCount];
            for (int nEventIdx = 0; nEventIdx < nEventCount; nEventIdx++)
            {
                this.ArrEvent[nEventIdx] = new MEvent();
                this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx, (RunID)this.GetRunID());
            }
        }

        #endregion


        #region // 模组信号操作

        /// <summary>
        /// 状态切换检查
        /// </summary>
        public bool StateCheck(EventState curState, EventState newState)
        {
            bool bResult = false;

            switch (curState)
            {
                case EventState.Invalid:
                    bResult = (EventState.Require == newState);
                    bResult |= (EventState.Invalid == newState);
                    break;
                case EventState.Require:
                    bResult = (EventState.Response == newState);
                    bResult |= (EventState.Cancel == newState);
                    bResult |= (EventState.Invalid == newState);
                    break;
                case EventState.Response:
                    bResult = (EventState.Ready == newState);
                    bResult |= (EventState.Cancel == newState);
                    break;
                case EventState.Ready:
                    bResult = (EventState.Finished == newState);
                    break;
                case EventState.Finished:
                    bResult = (EventState.Invalid == newState);
                    bResult |= (EventState.Require == newState);
                    break;
                case EventState.Cancel:
                    bResult = (EventState.Invalid == newState);
                    bResult |= (EventState.Cancel == newState);
                    break;
            }

            return bResult;
        }

        /// <summary>
        /// 设置模组信号
        /// </summary>
        public bool SetEvent(RunProcess run, ModuleEvent modEvent, EventState state, int nRowIdx = -1, int nColIdx = -1, int nParam1 = -1, string strRunName = "")
        {
            if (null == run || (int)modEvent < 0 || (int)modEvent >= run.ArrEvent.GetLength(0))
            {
                return false;
            }

            lock (run.LockEvent)
            {
                int nTmpRowIdx = -1;
                int nTmpColIdx = -1;
                int nTmpParam1 = -1;
                EventState tmpState = EventState.Invalid;
                ModuleEvent tmpEvent = ModuleEvent.ModuleEventInvalid;
                run.ArrEvent[(int)modEvent].GetEvent(ref tmpEvent, ref tmpState, ref nTmpRowIdx, ref nTmpColIdx, ref nTmpParam1);

                strRunName = tmpState > EventState.Invalid && tmpState < EventState.Finished ? strRunName : "";

                if (StateCheck(tmpState, state))
                {
                    run.ArrEvent[(int)modEvent].SetEvent(modEvent, state, nRowIdx, nColIdx, nParam1, (RunID)run.GetRunID(), strRunName);
                    run.SaveRunData(SaveType.SignalEvent);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组信号，并返回信号参数
        /// </summary>
        public bool GetEvent(RunProcess run, ModuleEvent modEvent, ref EventState state, ref int nRowIdx, ref int nColIdx)
        {
            if (null == run || (int)modEvent < 0 || (int)modEvent >= run.ArrEvent.GetLength(0))
            {
                return false;
            }

            lock (run.LockEvent)
            {
                int nParam1 = -1;
                ModuleEvent tmpEvent = (ModuleEvent)0;
                run.ArrEvent[(int)modEvent].GetEvent(ref tmpEvent, ref state, ref nRowIdx, ref nColIdx, ref nParam1);
            }
            return true;
        }

        /// <summary>
        /// 获取模组信号
        /// </summary>
        public bool GetEvent(RunProcess run, ModuleEvent modEvent, ref EventState state)
        {
            if (null == run || (int)modEvent < 0 || (int)modEvent >= run.ArrEvent.GetLength(0))
            {
                return false;
            }

            lock (run.LockEvent)
            {
                int nParam1 = -1;
                int nCurRowIdx = -1;
                int nCurColIdx = -1;
                ModuleEvent tmpEvent = (ModuleEvent)0;
                run.ArrEvent[(int)modEvent].GetEvent(ref tmpEvent, ref state, ref nCurRowIdx, ref nCurColIdx, ref nParam1);
            }
            return true;
        }

        /// <summary>
        /// 检查模组信号，并返回信号参数
        /// </summary>
        public bool CheckEvent(RunProcess run, ModuleEvent modEvent, EventState state, ref int nRowIdx, ref int nColIdx)
        {
            int nCurRowIdx = -1;
            int nCurColIdx = -1;
            EventState curState = EventState.Invalid;

            if (GetEvent(run, modEvent, ref curState, ref nCurRowIdx, ref nCurColIdx))
            {
                if (curState == state)
                {
                    nRowIdx = nCurRowIdx;
                    nColIdx = nCurColIdx;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查模组信号
        /// </summary>
        public bool CheckEvent(RunProcess run, ModuleEvent modEvent, EventState state)
        {
            int nCurRowIdx = -1;
            int nCurColIdx = -1;
            EventState curState = EventState.Invalid;

            if (GetEvent(run, modEvent, ref curState, ref nCurRowIdx, ref nCurColIdx))
            {
                return (curState == state);
            }
            return false;
        }

        #endregion


        #region // 托盘数据检查

        /// <summary>
        /// 获取托盘行列数
        /// </summary>
        public bool PltRowColCount(ref int nPltRow, ref int nPltCol)
        {
            nPltRow = nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);
            return true;
        }

        /// <summary>
        /// 满托盘检查
        /// </summary>
        public bool PltIsFull(Pallet Plt)
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
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                    {
                        if (Plt.Bat[nRowIdx, nColIdx].IsType(BatType.Invalid))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 空托盘检查
        /// </summary>
        public bool PltIsEmpty(Pallet Plt)
        {
            if (null == Plt)
            {
                return false;
            }
            lock (Plt.LockPlt)
            {
                foreach (Battery b in Plt.Bat)
                    if (b.Type > BatType.Invalid)
                        return false;
            }
            return true;
        }

        /// <summary>
        /// 检查托盘中某类型电池
        /// </summary>
        public bool PltHasTypeBat(Pallet Plt, BatType batType)
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
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                    {
                        if (Plt.Bat[nRowIdx, nColIdx].IsType(batType))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查托盘中某类型电池,并返回位置
        /// </summary>
        public bool PltHasTypeBat(Pallet Plt, BatType batType, ref int nRow, ref int nCol)
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
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                    {
                        if (Plt.Bat[nRowIdx, nColIdx].IsType(batType))
                        {
                            nRow = nRowIdx;
                            nCol = nColIdx;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #endregion


        //#region // 机器人操作

        ///// <summary>
        ///// 获取机器人ID
        ///// </summary>
        //public virtual int RobotID()
        //{
        //    return (int)RobotIndexID.Invalid;
        //}

        ///// <summary>
        ///// 获取机器人速度
        ///// </summary>
        //public virtual int RobotSpeed()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 获取机器人端口
        ///// </summary>
        //public virtual int RobotPort()
        //{
        //    return 0;
        //}

        ///// <summary>
        ///// 获取机器人IP
        ///// </summary>
        //public virtual string RobotIP()
        //{
        //    return "";
        //}

        ///// <summary>
        ///// 机器人连接状态
        ///// </summary>
        //public virtual bool RobotIsConnect()
        //{
        //    return false;
        //}

        ///// <summary>
        ///// 机器人连接
        ///// </summary>
        //public virtual bool RobotConnect(bool connect = true)
        //{
        //    return false;
        //}

        ///// <summary>
        ///// 机器人回原点
        ///// </summary>
        //public virtual bool RobotHome()
        //{
        //    return false;
        //}

        ///// <summary>
        ///// 机器人移动并等待完成
        ///// </summary>
        //public virtual bool RobotMove(int station, int row, int col, int speed, RobotAction action, MotorPosition motorLoc = MotorPosition.Invalid)
        //{
        //    return false;
        //}

        //#endregion


        #region // 硬件检查

        /// <summary>
        /// 检查托盘（硬件检测）
        /// </summary>
        public virtual bool CheckPallet(int nPltIdx, bool bHasPlt, bool bAlarm = true)
        {
            return false;
        }

        /// <summary>
        /// 检查电池（硬件检测）
        /// </summary>
        public virtual bool CheckBattery(int nBatIdx, bool bHasBat, bool bAlarm = true)
        {
            return false;
        }

        /// <summary>
        /// 手动操作工位检查
        /// </summary>
        public virtual bool ManualCheckStation(int station, int row, int col, bool bPickIn)
        {
            return false;
        }

        /// <summary>
        /// 休眠毫秒
        /// </summary>
        protected void Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }

        #endregion

        #region // 待机时间
        /// <summary>
        /// 待机时间
        /// </summary>
        public virtual void SystemWaitTime()
        {
        }
        #endregion


        #region  // 清运行数据

        /// <summary>
        /// 检查模组事件状态
        /// </summary>
        public virtual bool CheckModuleEventState()
        {
            return false;
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        public virtual bool ClearModuleData()
        {
            return false;
        }

        /// <summary>
        /// 清除模组任务
        /// </summary>
        public virtual bool ClearModuleTask()
        {
            ShowMsgBox.ShowDialog($"【{RunName}】不支持清任务", MessageType.MsgWarning);
            return false;
        }


        #endregion


        /// <summary>
        /// 自动保存Ini文件
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class Save : Attribute
        {
            public object def;


            public static string Serialize(object obj) => JsonConvert.SerializeObject(obj);
            public static object DeserializeObject(string strobj, Type type) => JsonConvert.DeserializeObject(strobj, type);


            public Save(object def)
            {
                this.def = def;
            }
        }

        #endregion


    }
}
