using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using HslCommunication.Profinet.Omron;
using HslCommunication;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame;
using Prism.Services.Dialogs;
using WPFMachine.Frame.ExtensionMethod;
using System.ComponentModel;
using WPFMachine.Views.Viewinterface;
using SystemControlLibrary.Mode;
using Newtonsoft.Json;
using WPFMachine.Frame.DataStructure;
using System.Reflection;
using WPFMachine.Frame.Userlib;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFMachine;
using System.Windows;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using WPFMachine.ViewModels;
using WPFMachine.Page;
using WPFMachine.Views;
using ImTools;
using WPFMachine.Frame.RealTimeTemperature;
using System.Security.Policy;
using System.Security.Cryptography;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Machine
{
    [INotifyPropertyChanged]
    public partial class MachineCtrl : ControlInterface, IPropView
    {
        #region // 枚举定义

        private enum MsgID
        {
            Start = ModuleMsgID.SystemStartID,
            KickTrashRobotCrash,
            OnloadRobotCrash,
            CoolOnloadRobotCrash,
            CoolOffloadRobotCrash,
            OnloadPressureLow,
            OffloadPressureLow,
            SafeDoorAlarm,
        }


        /// <summary>
        /// 计时类型
        /// </summary>
        public enum TimingType
        {
            WaitOnlLineTime = 0x01 << 0,      // 等待上料物流线时长
            WaitOffLineTime = 0x01 << 1,      // 等待下料物流线时长
            MCRunningTime = 0x01 << 2,        // 运行时长
            MCStopRunTime = 0x01 << 3,        // 停机时长
            MCAlarmTime = 0x01 << 4,          // 报警时长
        }

        #endregion


        #region // 字段

        // 系统字段

        public static IContainerProvider Ioc { get; } = ((App)Application.Current).Container;

        private static MachineCtrl machineCtrl;
        private PropertyManage parameterProperty;   // 系统参数
        private List<RunProcess> listRuns;          // 运行模组
        private List<string> listInput;             // 输入点
        private List<string> listOutput;            // 输出点
        private List<string> listMotor;             // 电机
        private Task taskSysThread;                 // 系统线程
        private bool bIsRunSysThread;               // 指示线程运行
        private Task taskSafeDoorThread;            // 安全门线程
        private Task taskRobotAlarmThread;          // 机器人报警线程
        private bool bIsRuntaskRobotAlarmThread;    // 指示机器人报警运行
        private bool bIsRunSafeDoorThread;          // 指示线程运行
        private Task taskWCThread;                  // 水含量线程
        private bool bIsRunWCThread;                // 水含量指示线程运行
        private DateTime towerStartTime;            // 灯塔开始时间
        public DataBaseRecord dbRecord;             // 数据库
        public int machineID;                       // 设备ID
        public bool OverOrder;                      // 主界面顺序 

        // 输入输出
        private int[] IStartButton;                 // 输入：启动按钮
        private int[] IStopButton;                  // 输入：停止按钮
        private int[] IEStopButton;                 // 输入：急停按钮
        private int[] IResetButton;                 // 输入：复位按钮
        private int[] IManAutoButton;               // 输入：手自动切换按钮
        private int[] IPlcRunButton;                // 输入：Plc运行按钮
        private int[] OStartLed;                    // 输出：启动按钮灯
        private int[] OStopLed;                     // 输出：停止按钮灯
        private int[] OResetLed;                    // 输出：复位按钮灯
        public int[] OLightTowerRed;                // 输出：灯塔-红
        private int[] OLightTowerYellow;            // 输出：灯塔-黄
        private int[] OLightTowerGreen;             // 输出：灯塔-绿
        private int[] OLightTowerBuzzer;            // 输出：灯塔-蜂鸣器
        private int[] OHeartBeat;                   // 输出：模拟心跳

        private int[] IOnloadRobotAlarm;             //输入：上料机器人碰撞报警
        private int[] ITransferRobotAlarm;           //输入：调度机器人碰撞报警
        private int[] IOffloadRobotAlarm;            //输入：下料机器人碰撞报警
        private int[] IRobotCrash;                   //输入：机器人碰撞
        private int[] IBufDoor;                      //输入：缓存门磁
        private int[] ICylAlarm;                     //输入：气缸报警
        private int[] IRasterAlarm;                     //输入：地轨光栅报警
        private int[] ICheckPressure;                //输入：气压不足

        private int[] ISafeDoorState;               // 输入：安全门开关状态
        private int[] ISafeDoorEStop;               // 输入：安全门安全开关
        private int[] ISafeDoorOpenReq;             // 输入：安全门开门请求按钮
        private int[] ISafeDoorCloseReq;            // 输入：安全门关门请求按钮
        private int[] OSafeDoorOpenLed;             // 输出：安全门开门请求按钮LED
        private int[] OSafeDoorCloseLed;            // 输出：安全门关门请求按钮LED
        private int[] OSafeDoorUnlock;              // 输出：安全门解锁
        private int[] ITransferGoat;                // 输入：调度替罪羊

        // 参数设置
        private Object lockRowCol;                  // 行列数修改锁
        private bool dataRecover;                   // 是否数据恢复
        private bool autoUploadWaterValue;           // 是否自动上传水含量
        private bool updataMES;                     // 上传MES数据
        private int pltMaxRow;                      // 托盘最大行
        private int pltMaxCol;                      // 托盘最大列
        private int nLineNum;                       // 拉线
        private bool reOvenWait;                    // 回炉选择

        private int productFormula;                 // 产品配方
        public int nStayOvenOutTime;                // 电池入炉开始烘烤后，停留时间超过设定小时后区分显示         
        public int nPressureHintTime;               // 烘烤完成保压提示时间
        private bool updatavBindingMES;             // 上传托盘MES绑定
        public bool updataFakeBindingMES;           // 上传假电池MES绑定
        public int productionFileStorageLife;       // 生产信息文件存储时间：天

        ObservableCollection<object> ShowProductDatas = App.Ioc.Resolve<ObservableCollection<object>>("ShowProductDatas");
        //配方参数修改旧值
        public double parameterOldValue;
        // 生产统计列表
        public int M_nOnloadTotal
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "上料数量:").ProductData; }
            set
            {
                //ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "上料PPM:").ProductData = M_nOnloadTotal - nOnloadOldTotal;
                ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "上料数量:").ProductData = value;
            }
        }

        public int m_nOnloadFakeTotal;              // 上料假电池数量
        public int M_nOnloadFakeTotal
        {
            get { return m_nOnloadFakeTotal; }
            set { SetProperty(ref m_nOnloadFakeTotal, value); }
        }           // 下料数量
        public int M_nOffloadTotal
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "下料数量:").ProductData; }
            set {
                //ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "下料PPM:").ProductData = M_nOffloadTotal - nOffloadOldTotal;

                ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "下料数量:").ProductData = value; }
        }


        public int M_nWaitOnlLineTime
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "待上料时间(Min):").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "待上料时间(Min):").ProductData = value; }
        }

        public int M_nWaitOffLineTime
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "待下料时间(Min):").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "待下料时间(Min):").ProductData = value; }
        }

        public int M_nMCRunningTime
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "运行时间(Min):").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "运行时间(Min):").ProductData = value; }
        }

        public int M_nMCStopRunTime
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "停机时间(Min):").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "停机时间(Min):").ProductData = value; }
        }

        public int M_nAlarmTime
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "报警时间(Min):").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "报警时间(Min):").ProductData = value; }
        }


        // 下料假电池数量
        public int m_nOffloadFakeTotal;

        public TimingType timingType;               // 计时类型
        public (int, DateTime, bool)[] timings;     // 计时数组
        public int m_nOnloadYeuid;                  // 每小时上料数量
        public int m_nOffloadYeuid;                 // 每小时下料数量
        // NG数量
        public int M_nNgTotal
        {
            get { return ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "NG数量:").ProductData; }
            set { ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "NG数量:").ProductData = value; }
        }

        //上料旧数量
        private int OldTotal;
        public int nOnloadOldTotal
        {
            get { return OldTotal; }
            set
            {
                OldTotal = value;
                //ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "上料PPM:").ProductData = M_nOnloadTotal - nOnloadOldTotal;
            }
        }
        // 下料旧数量

        public int OffloadOldTotal;
        public int nOffloadOldTotal
        {
            get { return OffloadOldTotal; }
            set
            {
                OffloadOldTotal = value;
                //ShowProductDatas.OfType<ShowCountInfo<int>>().First(f => f.Name == "下料PPM:").ProductData = M_nOffloadTotal - nOffloadOldTotal;
            }
        }
        bool bRecordData;                           // 记录生产数量

        public WaterContentClient m_WCClient;       // 水含量客户端
        public string strWCInfo;                    // 水含量信息刷新
        private CavityData[] arrCavity;             // 腔体数据
        private string wcserverIP;                  // 水含量服务端IP

        // mes参数
        // 文件数据

        /// <summary>
        /// 文件名
        /// </summary>
        private string fileName;
        /// <summary>
        /// 文件线程间隔时间
        /// </summary>
        private DateTime timeFile;
        /// <summary>
        /// 文件清除线程
        /// </summary>
        private Task taskPastDueFileThread;
        /// <summary>
        /// 指示线程运行
        /// </summary>
        private bool bIsFileThread;

        /// <summary>
        /// 生产信息文件路径
        /// </summary>
        public string ProductionFilePath { get; private set; }
        /// <summary>
        /// Log文件存储时间：天
        /// </summary>
        private int nFileLifeDay;

        private object MesReportLock;               // MES数据存储
        private object CsvLogLock;                  // csvlog锁
        public WaterMode eWaterMode;                // 水含量模式
        public string []strMesWaterName;            // MES水含量名称，0:混合型，1:阳极，2:阴极
        //public MysqlServer dbMySqlServer;           // 数据库

        public string[] strHCMesParamURL;           // 海辰MES参数
        public string[] strHCMesParam;              // 海辰MES参数
        public List<string> MesMessageList;         // MES临时消息集合
        private Task taskHCMesThread;               // HCMES线程
        private bool bIsHCMesThread;                // HCMES线程运行
        public int nHCFormulaIdex;                  // 海辰配方索引

        // 屏保
        private Task taskScrSaverThread;            // 屏保线程
        private bool bIsRunScrSaverThread;          // 屏保指示线程运行
        private bool bIsSafeDoorOpen;               // 安全门状态
        private bool bPlcOldState;                  // PLC旧状态
        public int nPlcStateCount;                  // PLC状态计数


        // spc
        private Task taskSpcAlarmThread;            // Spc报警线程
        private bool bIsRuntaskSpcAlarmThread;      // 指示Spc报警运行
        public int productionMode;                  //生产模式
        public readonly OmronFinsNet LoadingPlc;    // 上料客户端
        public readonly OmronFinsNet UnLoadingPlc;  // 下料客户端
        public int nMaxWaitOffFloorCount;           // 最大下料腔体数量
        public bool bOvenRestEnable;                // 炉层屏蔽原因使能
        public int nHeartBeatCount;                 // PLC心跳计数
        public bool bOldHeartBeat;                  // PLC旧心跳状态

        // MES
        private HttpClient httpClient;              // Mes通讯接口
        private Object lockMes;                     // MES数据锁
        private bool isMESConnect;                  // MES连接状态
        public bool IsMESConnect
        {
            get { return isMESConnect; }
            set { SetProperty(ref isMESConnect, value); }
        }
        private string MesShieldString;             // Mes屏蔽字符串

        public string MesIp;                        // MesIP地址
        public string UserNo;                       // 工号
        public string PassWord;                     // 密码
        public string DeviceSn;                     // 设备号
        public string MoNumber;                     // 制令单
        public string GroupCode;                    // 工序代码

        // Mes参数代码
        public string PreHeatTime;                  // 预热时间
        public string VacHeatTime;                  // 真空加热时间
        public string PressureUpperLimit;           // 真空烘烤段真空度最大值
        public string PressureLowerLimit;           // 真空烘烤段真空度最小值
        public string PressureAvg;                  // 真空烘烤段真空度均值
        public string TempMax;                      // 真空烘烤温度最大值
        public string TempMin;                      // 真空烘烤温度最小值
        public string TempAvg;                      // 真空烘烤温度均值
        public string Environmental;                // 环境露点(手动输入)
        public string JustValue;                    // 正极极片水含量
        public string NegativeValue;                // 负极极片水含量
        public string MingleValue;                  // 混合样水含量
        public string CavityNumber;                 // 腔体编号
        public string PalletCode;                   // 夹具编号
        public string Classes;                      // 班次
        public string Totality;                     // 总数,不良数,良品数
        public string ReworkRecord;                 // 返工记录(手动输入)

        private bool nUpRuningFlag = false;         //设备运行上传标志

        public string[] OperatorId;                 // 操作ID(多台炉子)
        public MesParameterData ParData;            // 下发参数列表

        private Task taskMesStatusThread;           // Mes状态线程
        private bool bIsRunMesStatusThread;         // Mes状态线程运行
        private DateTime StatusStartTime;           // Mes状态开始时间      
        private DateTime GetParamTime;              // Mes获取参数时间      

        private Task taskMachineState;              // 设备状态线程
        private bool bIsRuntaskMachineState;        // 设备状态运行
        private DateTime MachineStateTime;          // Mes状态开始时间
        private bool bIsEquipmentState;               // 设备状态上传
        private bool bIsMESState;               // MES在线检测标志


        // Mes对象
        private HeartBeat HeartBeat;                      // 设备登录
        private StateAndStopReasonUpload StateAndStopReasonUpload;           // 设备状态+停机原因
        private AlarmUpload AlarmUpload;              // 设备报警上传
        private ProcessDataUpload ProcessDataUpload;    // 设备过程参数
        private WIPInStationCheck WIPInStationCheck;      // 检查服务器运行状态
        private ResultDataUploadAssembly ResultDataUploadAssembly;                  // 设备运行状态上传
        private DeviceParameterAlarm deviceParameterAlarm;  // 设备报警数据上传
        private RecordFtpFilePathAndFileName recordFtpFilePathAndFileName; //上传路径及文件名
        private EnergyUpload EnergyUpload;          // 能源数据上传
        private FittingCheckForTary fittingCheckForTary;    // 托盘检查
        private FittingCheckForCell fittingCheckForCell;    // 电芯检查
        private FittingBinding fittingBinding;              // 托盘电芯绑定
        private FittingUnBinding fittingUnBinding;          // 托盘电芯解绑
        private LoginCheck LoginCheck;                      // 操作员登录校验
        private BakeDataUpload BakeDataUpload;              // 烘箱数据采集接口


        //MES 信息
        public string MesUserName;                  // MES 用户
        public string MesPassword;                  // MES 密码
        public string EquipPC_Password;             // 上位机验证密码      
        public string Equip_Code;                   // 设备编码
        public string EquipPC_ID;                   // 上位机软件编号
        public string MesEms_code;                  // 设备编号(新)
        public string MesMaterialCode;              // 物料编码
        public string MesProductType;               // 产品类型
        public string ManufactureCode;              // 工单号
        public string Operator;                     // 操作员


        // Mes处理委托
        delegate T del<T>(int nOvenIdx, MesInterface mesInterface, MESResponse recvData);
        del<bool>[] MesDel;

        #region 委托
        public static MachineCtrl MachineCtrlInstance => machineCtrl;

        #endregion
        #endregion


        #region // 属性
        /// <summary>
        /// 托盘最大行
        /// </summary>
        public int PltMaxRow
        {
            get => pltMaxRow;
            set
            {
                this.SetProperty(ref pltMaxRow, value);
            }
        }
        /// <summary>
        /// 托盘最大列
        /// </summary>
        public int PltMaxCol
        {
            get => pltMaxCol;
            set
            {
                this.SetProperty(ref pltMaxCol, value);
            }
        }
        /// <summary>
        /// 模组列表
        /// </summary>
        public List<RunProcess> ListRuns
        {
            get
            {
                return listRuns;
            }

            private set
            {
                this.listRuns = value;
            }
        }

        /// <summary>
        /// 数据恢复
        /// </summary>
        public bool DataRecover
        {
            get
            {
                return dataRecover;
            }

            private set
            {
                this.dataRecover = value;
            }
        }
        /// <summary>
        /// 自动上传水含量
        /// </summary>
        public bool AutoUploadWaterValue
        {
            get
            {
                return autoUploadWaterValue;
            }

            private set
            {
                this.autoUploadWaterValue = value;
            }
        }
        /// <summary>
        /// 上传MES数据
        /// </summary>
        public bool UpdataMES
        {
            get
            {
                return updataMES;
            }

            private set
            {
                this.updataMES = value;
            }
        }

        public int ProductionMode
        {
            get
            {
                return productionMode;
            }

        }
        /// <summary>
        /// 回炉选择
        /// </summary>
        public bool ReOvenWait
        {
            get
            {
                return reOvenWait;
            }

            private set
            {
                this.reOvenWait = value;
            }
        }

        /// <summary>
        /// 产品配方
        /// </summary>
        public int ProductFormula
        {
            get
            {
                return productFormula;
            }

            private set
            {
                this.productFormula = value;
            }
        }

        /// <summary>
        /// 水含量服务端IP
        /// </summary>
        public string WCServerIP
        {
            get
            {
                return wcserverIP;
            }

            private set
            {
                this.wcserverIP = value;
            }
        }

        private INotifyPropertyChanged dyCacheType;
        public INotifyPropertyChanged DyCacheType => dyCacheType ??= IPropView.CreateDyType(parameterProperty, GetType().Name, this, (s, e) =>
        {
            var name = parameterProperty.OfType<Property>().First(p => p.DisplayName == e.PropertyName).Name;
            var prop = s.GetType().GetProperty(e.PropertyName);

            var value = prop.GetValue(s);
            Task.Run(() =>
            {
                if (CheckParameter(name, value))
                {
                    WriteParameter(name, value.ToString());
                    ReadParameter();
                }
                else
                {
                    prop.SetValue(DyCacheType, Convert.ChangeType(parameterProperty.OfType<Property>().First(p => p.DisplayName == e.PropertyName).Value, prop.PropertyType));
                }
            });


        });

        public string RunName { get; set; } = "系统";

        [ObservableProperty]
        private User curUser;




        [ObservableProperty]
        private string userName = "未登录";

        Stopwatch stopwatch = new Stopwatch();
        #endregion


        #region // 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MachineCtrl()
        {
            this.parameterProperty = new PropertyManage();
            this.ListRuns = new List<RunProcess>();
            this.listInput = new List<string>();
            this.listOutput = new List<string>();
            this.listMotor = new List<string>();
            this.dbRecord = new DataBaseRecord();
            this.towerStartTime = DateTime.Now;

            this.IStartButton = new int[(int)SystemIOGroup.PanelButton];
            this.IStopButton = new int[(int)SystemIOGroup.PanelButton];
            this.IEStopButton = new int[(int)SystemIOGroup.IEStopNum];
            this.IResetButton = new int[(int)SystemIOGroup.PanelButton];
            this.IManAutoButton = new int[(int)SystemIOGroup.PanelButton];
            this.IPlcRunButton = new int[(int)SystemIOGroup.PanelButton];
            this.OStartLed = new int[(int)SystemIOGroup.PanelButton];
            this.OStopLed = new int[(int)SystemIOGroup.PanelButton];
            this.OResetLed = new int[(int)SystemIOGroup.PanelButton];
            this.OLightTowerRed = new int[(int)SystemIOGroup.LightTower];
            this.OLightTowerYellow = new int[(int)SystemIOGroup.LightTower];
            this.OLightTowerGreen = new int[(int)SystemIOGroup.LightTower];
            this.OLightTowerBuzzer = new int[(int)SystemIOGroup.LightTower];
            this.OHeartBeat = new int[(int)SystemIOGroup.HeartBeat];

            this.ISafeDoorState = new int[(int)SystemIOGroup.SafeDoor];
            this.ISafeDoorEStop = new int[(int)SystemIOGroup.SafeDoor];
            this.ISafeDoorOpenReq = new int[(int)SystemIOGroup.SafeDoor];
            this.ISafeDoorCloseReq = new int[(int)SystemIOGroup.SafeDoor];
            this.OSafeDoorOpenLed = new int[(int)SystemIOGroup.SafeDoor];
            this.OSafeDoorCloseLed = new int[(int)SystemIOGroup.SafeDoor];
            this.OSafeDoorUnlock = new int[(int)SystemIOGroup.SafeDoor];

            this.IOnloadRobotAlarm = new int[(int)SystemIOGroup.OnOffLoadRobot];
            this.IOffloadRobotAlarm = new int[(int)SystemIOGroup.OnOffLoadRobot];
            this.ITransferRobotAlarm = new int[(int)SystemIOGroup.TransferRobot];
            this.IRobotCrash = new int[(int)SystemIOGroup.RobotCrash];
            this.ICheckPressure = new int[2];
            this.ITransferGoat = new int[2];
            this.IBufDoor = new int[2];
            this.ICylAlarm = new int[2];
            this.IRasterAlarm = new int[2];

            this.lockRowCol = new object();
            this.pltMaxRow = 0;
            this.pltMaxCol = 0;
            this.nLineNum = 0;
            this.parameterOldValue = 1;
            this.productFormula = 1;
            this.wcserverIP = "192.168.1.11";
            this.OverOrder = false;

            this.lockMes = new object();
            this.httpClient=new HttpClient();
            this.updataMES = true;
            this.dataRecover = true;
            this.autoUploadWaterValue = false;
            this.ManufactureCode = "111";
            this.Operator = "11";
            nMaxWaitOffFloorCount = 5;
            strMesWaterName = new string[3] { "BKCADHMDTY", "阳极", "阴极" };
            productionFileStorageLife = 30;

            InsertPrivateParam("UpdataMES", "上传MES数据", "TRUE:上传MES；FALSE:不上传MES", updataMES);
            InsertPrivateParam("DataRecover", "数据恢复", "TRUE:初始化时恢复数据；FALSE:清除旧数据，不恢复", dataRecover);
            InsertPrivateParam("PalletMaxRow", "托盘最大行", "托盘最大行数 >0", pltMaxRow);
            InsertPrivateParam("PalletMaxCol", "托盘最大列", "托盘最大列数 >0", pltMaxCol);
            InsertPrivateParam("nLineNum", "拉线", "拉线名称", nLineNum);
            InsertPrivateParam("productFormula", "产品配方", "启用第几套电机点位", productFormula);
            InsertPrivateParam("wcserverIP", "水含量服务端IP", "服务端IP", wcserverIP);
            InsertPrivateParam("AutoUploadWaterValue", "自动上传水含量", "TRUE:自动上传水含量；FALSE:手动上传水含量", autoUploadWaterValue);
            InsertPrivateParam("MaxWaitOffFloorCount", "最大下料腔体数量", "最大下料腔体数量，大于最大腔体数量，不下假电池", nMaxWaitOffFloorCount);
            InsertPrivateParam("MesWaterName[0]", "混合型水含量名称", "MES混合型水含量名称", strMesWaterName[0], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("MesWaterName[1]", "阳极水含量名称", "MES阳极水含量名称", strMesWaterName[1], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("MesWaterName[2]", "阴极水含量名称", "MES阴极水含量名称", strMesWaterName[2], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("productionFileStorageLife", "生产文件存储", "生产信息文件存储时间：天", productionFileStorageLife);

            InitProduceCount();
            m_WCClient = new WaterContentClient();
            strWCInfo = "";
            ReadProduceCount();

            taskSysThread = null;
            bIsRunSysThread = false;
            taskSafeDoorThread = null;
            bIsRunSafeDoorThread = false;
            taskWCThread = null;
            bIsRunWCThread = false;

            // 文件数据
            this.fileName = "生产信息";
            this.timeFile = new DateTime();
            this.taskPastDueFileThread = null;
            this.bIsFileThread = false;

            MesReportLock = new object();
            CsvLogLock = new object();
            bRecordData = false;
            bIsEquipmentState = true;
            bIsMESState = false;

            //strHCMesParamURL = new string[(int)MesInterface.InStationCheckIME];
            strHCMesParam = new string[20];
            nHCFormulaIdex = 0;

            MesMessageList = new List<string>();
            //dbMySqlServer = new MysqlServer();
            this.isMESConnect = false;
            taskScrSaverThread = null;
            bIsRunScrSaverThread = false;
            bIsSafeDoorOpen = false;
            bPlcOldState = false;
            nPlcStateCount = 0;

            OmronClientFactory.ReadConfig();
            LoadingPlc = OmronClientFactory.CreateLoadingPlc();
            UnLoadingPlc = OmronClientFactory.CreateUnLoadingPlc();
            OmronClientFactory.SetProperty(ref LoadingPlc, ref UnLoadingPlc);
        }

        //SPC 报警连接
        public void ConnectOmronPLC(string plcIP, int plcPort, int plcSA1, int plcDA1, int plcDA2, ref OperateResult operateResult, ref OmronFinsNet omronFinsNet)
        {
            byte[] plcinfo = System.Text.Encoding.Default.GetBytes(new char[3] { (char)plcSA1, (char)plcDA1, (char)plcDA2 });
            omronFinsNet = new OmronFinsNet(plcIP, plcPort);
            omronFinsNet.SA1 = plcinfo[0];
            omronFinsNet.DA1 = plcinfo[1];
            omronFinsNet.DA2 = plcinfo[2];
            //连接plc
            try
            {
                operateResult = omronFinsNet.ConnectServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        ~MachineCtrl()
        {
            ReleaseThread();
        }

        #endregion


        #region // 初始化函数

        /// <summary>
        /// 本类实例
        /// </summary>
        public static MachineCtrl GetInstance()
        {
            if (null == machineCtrl)
            {
                machineCtrl = new MachineCtrl();
            }
            return machineCtrl;
        }

        /// <summary>
        /// 系统初始化
        /// </summary>
        public bool Initialize()
        {
            string section, name;

            #region // input
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = "Input" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.InputCfg));
                if ("" == name)
                {
                    break;
                }
                this.listInput.Add(name);
            }
            #endregion

            #region // output
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = "Output" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.OutputCfg));
                if ("" == name)
                {
                    break;
                }
                this.listOutput.Add(name);
            }
            #endregion

            #region // motor
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = string.Format("{0}Motor{1}.cfg", Def.MotorCfgFolder, index);
                name = "Motor" + index;
                if (!File.Exists(section))
                {
                    break;
                }
                this.listMotor.Add(name);
            }
            #endregion

            // 删除已有模组信息，重新创建
            if (File.Exists(Def.ModuleCfg))
            {
                File.Delete(Def.ModuleCfg);
            }

            if (!base.Initialize(listMotor.Count, listInput.Count, listOutput.Count))
            {
                Environment.Exit(0);
                return false;
            }

            #region // 电机点位初始化

            int num = DeviceManager.GetMotorManager().MotorsTotal;
            for (int index = 0; index < num; index++)
            {
                LoadMotorLocation(index);
            }
            #endregion

            #region // MES配置参数
            foreach (MesInterface mes in Enum.GetValues(typeof(MesInterface)))
            {
                MesDefine.ReadMesConfig(mes);
            }

            MesDefine.ReadMesConfig();
            #endregion


            return true;
        }

        /// <summary>
        /// 模组线程初始化
        /// </summary>
        protected override bool InitializeRunThreads()
        {
            Trace.Assert(null == this.RunsCtrl, "ControlInterface.RunsCtrl is null.");

            #region // 数据库和文件检查

            // 运行数据路径
            if (!Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataFolder)) ||
                !Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataBakFolder)))
            {
                Trace.Assert(false, "CreateFilePath( " + Def.GetAbsPathName(Def.RunDataFolder) + " ) fail.");
                return false;
            }

            #endregion

            #region // MES配置参数，资源班次信息

            #endregion

            #region // 系统配置

            bool alarmStopMC = IniFile.ReadBool("Run", "AlarmStopMC", false, Def.GetAbsPathName(Def.MachineCfg));
            int countModules = IniFile.ReadInt("Modules", "CountModules", 1, Def.GetAbsPathName(Def.ModuleExCfg));
            this.OverOrder = IniFile.ReadBool("Modules", "OverOrder", false, Def.GetAbsPathName(Def.ModuleExCfg));
            this.machineID = IniFile.ReadInt("Modules", "MachineID", -1, Def.GetAbsPathName(Def.ModuleExCfg));
            string str = IniFile.ReadString("WaterMode", "eWaterMode", " ", Def.GetAbsPathName(Def.MachineCfg));
            eWaterMode = (WaterMode)System.Enum.Parse(typeof(WaterMode), str);
            if (this.machineID < 0)
            {
                MessageBox.Show("设备编号MachineID未配置，请在ModuleEx.cfg中配置");
                //ShowMsgBox.ShowDialog("设备编号MachineID未配置，请在ModuleEx.cfg中配置", MessageType.MsgAlarm);
            }
            // 读系统参数
            ReadParameter();
            #endregion

            #region // 生成系统模组

            IniFile.WriteInt("Modules", "CountModules", countModules + 1, Def.GetAbsPathName(Def.ModuleCfg));
            IniFile.WriteString("Module0", "Name", "System", Def.GetAbsPathName(Def.ModuleCfg));

            #endregion

            #region // 创建模组


            RunProcess runModule = null;
            string strSection, strKey, strClass;
            Dictionary<int, string> checkRunID = new Dictionary<int, string>();
            strSection = strKey = strClass = "";

            for (int index = 0; index < countModules; index++)
            {
                int runID = index;
                strKey = "Module" + index;
                strSection = IniFile.ReadString("Modules", strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
                strClass = IniFile.ReadString(strSection, "Class", "", Def.GetAbsPathName(Def.ModuleExCfg));
                runID = IniFile.ReadInt(strSection, "RunID", -1, Def.GetAbsPathName(Def.ModuleExCfg));

                var type = Type.GetType($"Machine.{strClass}");
                runModule = Activator.CreateInstance(type, runID) as RunProcess;

                ListRuns.Add(runModule);
                if (!checkRunID.ContainsKey(runID))
                {
                    checkRunID.Add(runID, strSection);
                }
                else
                {
                    MessageBox.Show((strSection + "模组RunID = " + runID + "已存在，请检查！"));

                    //ShowMsgBox.ShowDialog((strSection + "模组RunID = " + runID + "已存在，请检查！"), MessageType.MsgAlarm);
                    return false;
                }

                List<int> inputs, outputs, motors;
                runModule.AlarmStopMC(alarmStopMC);

                if (!runModule.InitializeConfig(strSection))
                {
                    MessageBox.Show("读取" + strSection + "模组配置异常，请检查后重新操作");

                    //ShowMsgBox.ShowDialog("读取" + strSection + "模组配置异常，请检查后重新操作", MessageType.MsgAlarm);
                    return false;
                }

                runModule.GetHardwareConfig(out inputs, out outputs, out motors);
                WriteModuleCfg(index + 1, strSection, inputs, outputs, motors);
            }

            #endregion

            #region // 读取该模组的关联模组

            foreach (RunProcess run in this.ListRuns)
            {
                // 有硬件运行时不能空运行
                if (!Def.IsNoHardware())
                {
                    run.DryRun = false;
                }

                run.ReadRelatedModule();
            }

            #endregion

            #region // 创建RunCtrl

            this.RunsCtrl = new RunCtrl();
            if (null == this.RunsCtrl)
            {
                MessageBox.Show("创建RunCtrl线程失败");
                //ShowMsgBox.ShowDialog("创建RunCtrl线程失败", MessageType.MsgAlarm);
                return false;
            }

            if (!this.RunsCtrl.Initialize(countModules, (this.ListRuns.ConvertAll<RunEx>(tmp => tmp as RunEx)), (new ManualDebugCheck(this.ListRuns.Count))))
            {
                MessageBox.Show("RunCtrl线程初始化失败");

                //ShowMsgBox.ShowDialog("RunCtrl线程初始化失败", MessageType.MsgAlarm);
                return false;
            }

            // 设置回调函数
            this.RunsCtrl.beforeStart = BeforeStart;
            this.RunsCtrl.afterStop = AfterStop;

            #endregion

            #region // 读取系统IO，系统设置参数，统计数据

            // 读系统IO
            ReadSystemIO();
            // 读系统参数
            ReadParameter();
            // 读取统计数据
            ReadTotalData();

            // 清理临时列表
            listInput.Clear();
            listOutput.Clear();
            listMotor.Clear();

            #endregion

            #region // 其他初始化
            //获取干燥炉资源号
            OperatorId = new string[(int)DryingOvenCount.DryingOvenNum].Select(s => "").ToArray();
            GetOvenResourceID();

            if (!InitThread())
            {
                return false;
            }

            #endregion

            return true;
        }

        #endregion


        #region // 模组获取及硬件配置

        /// <summary>
        /// 保存模组配置
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="moduleName">模组名称</param>
        /// <param name="inputs">输入列表</param>
        /// <param name="outputs">输出列表</param>
        /// <param name="motors">电机列表</param>
        private void WriteModuleCfg(int index, string moduleName, List<int> inputs, List<int> outputs, List<int> motors)
        {
            string section = "Module" + index;

            // 模组名
            IniFile.WriteString(section, "Name", moduleName, Def.GetAbsPathName(Def.ModuleCfg));

            // 输入
            int count = inputs.Count;
            IniFile.WriteInt(section, "InputCount", count, Def.GetAbsPathName(Def.ModuleCfg));
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Input" + i), inputs[i], Def.GetAbsPathName(Def.ModuleCfg));
            }
            // 输出
            count = outputs.Count;
            IniFile.WriteInt(section, "OutputCount", count, Def.GetAbsPathName(Def.ModuleCfg));
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Output" + i), outputs[i], Def.GetAbsPathName(Def.ModuleCfg));
            }
            // 电机
            count = motors.Count;
            IniFile.WriteInt(section, "MotorCount", count, Def.GetAbsPathName(Def.ModuleCfg));
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Motor" + i), motors[i], Def.GetAbsPathName(Def.ModuleCfg));
            }
        }

        /// <summary>
        /// 根据模组名获取模组
        /// </summary>
        /// <param name="moduleName">模组名</param>
        public RunProcess GetModule(string runModule)
        {
            foreach (RunProcess run in this.ListRuns)
            {
                if ((null != run) && (runModule == run.RunModule))
                {
                    return run;
                }
            }
            return null;
        }

        public T[] GetModule<T>() => ListRuns.OfType<T>().ToArray();

        /// <summary>
        /// 根据模组ID获取模组
        /// </summary>
        /// <param name="runID">模组ID</param>
        public RunProcess GetModule(RunID runID)
        {
            foreach (RunProcess run in this.ListRuns)
            {
                if ((null != run) && ((int)runID == run.GetRunID()))
                {
                    return run;
                }
            }
            return null;
        }

        #endregion


        #region // 设备运行检查

        /// <summary>
        /// 设备启动前检查是否能启动
        /// </summary>
        /// <returns></returns>
        protected bool BeforeStart()
        {
            MCState mcState = RunsCtrl.GetMCState();
            if (Def.IsNoHardware())
            {
                if (MCState.MCInitComplete == mcState || MCState.MCStopRun == mcState || MCState.MCRunErr == mcState)
                {
                    SetTiming(TimingType.MCRunningTime, true);
                    SetTiming(TimingType.MCStopRunTime, false);
                }
                return true;
            }
            if (MCState.MCRunning == RunsCtrl.GetMCState())
            {
                return false;
            }

            if (!this.UpdataMES && !Def.IsNoHardware())
            {
                string msg = string.Format("【离线生产】将不能上传MES，是否继续！");
                if (ButtonResult.OK != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion).Result)
                {
                    return false;
                }
            }

            if (MCState.MCInitComplete == mcState || MCState.MCStopRun == mcState || MCState.MCRunErr == mcState)
            {
                SetTiming(TimingType.MCRunningTime, true);
                SetTiming(TimingType.MCStopRunTime, false);
            }
            return true;
        }

        /// <summary>
        /// 设备停止后进行的操作
        /// </summary>
        protected void AfterStop()
        {
            foreach (var item in this.ListRuns)
            {
                item.AfterStopAction();
            }

            if (MCState.MCRunning == RunsCtrl.GetMCState())
            {
                SetTiming(TimingType.MCRunningTime, false);
                SetTiming(TimingType.MCStopRunTime, true);
            }
            return;
        }
        /// <summary>
        /// 根据模组ID获取模组
        /// </summary>
        public bool GetModule(RunID runID, ref RunProcess runProcess)
        {
            if ((int)runID >= 0 && (int)runID < (int)RunID.RunIDEnd)
            {
                runProcess = this.ListRuns[(int)runID];
                return (null != runProcess);
            }
            runProcess = null;
            return false;
        }
        #endregion


        #region // 解析IO及电机配置

        /// <summary>
        /// 解析输入
        /// </summary>
        public int DecodeInputID(string strID)
        {
            if (string.IsNullOrEmpty(strID))
            {
                return -1;
            }
            return this.listInput.IndexOf(strID);
        }

        /// <summary>
        /// 解析输出
        /// </summary>
        public int DecodeOutputID(string strID)
        {
            if (string.IsNullOrEmpty(strID))
            {
                return -1;
            }
            return this.listOutput.IndexOf(strID);
        }

        /// <summary>
        /// 解析电机
        /// </summary>
        public int DecodeMotorID(string strID)
        {
            if (string.IsNullOrEmpty(strID))
            {
                return -1;
            }

            strID = "Motor" + strID.Trim("M".ToCharArray());
            return this.listMotor.IndexOf(strID);
        }

        /// <summary>
        /// 加载电机的点位
        /// </summary>
        internal bool LoadMotorLocation(int motorID)
        {
            List<MotorFormula> motorlist = new List<MotorFormula>();
            if (this.dbRecord.GetMotorPosList(Def.GetProductFormula(), motorID, motorlist))
            {
                DeviceManager.GetMotorManager().LstMotors[motorID].DeleteAllLoc();
                motorlist.Sort(delegate (MotorFormula left, MotorFormula right) { return left.posID - right.posID; });
                foreach (var item in motorlist)
                {
                    if ((int)MotorCode.MotorOK != DeviceManager.GetMotorManager().LstMotors[motorID].AddLocation(item.posID, item.posName, item.posValue))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        #endregion


        #region // 系统参数操作

        /// <summary>
        /// 参数读取（界面调用）
        /// </summary>
        public bool ReadParameter()
        {
            this.pltMaxRow = IniFile.ReadInt("Parameter", "PalletMaxRow", this.pltMaxRow, Def.GetAbsPathName(Def.MachineCfg));
            this.pltMaxCol = IniFile.ReadInt("Parameter", "PalletMaxCol", this.pltMaxCol, Def.GetAbsPathName(Def.MachineCfg));
            this.ProductionFilePath = IniFile.ReadString("Parameter", "ProductionFilePath", this.ProductionFilePath, Def.GetAbsPathName(Def.MachineCfg));
            this.nFileLifeDay = IniFile.ReadInt("Parameter", "FileLifeDay", this.nFileLifeDay, Def.GetAbsPathName(Def.MachineCfg));
            this.nLineNum = IniFile.ReadInt("Parameter", "nLineNum", 10, Def.GetAbsPathName(Def.MachineCfg));
            //this.reOvenWait = IniFile.ReadBool("Parameter", "reOvenWait", true, Def.GetAbsPathName(Def.MachineCfg));
            this.productionMode = IniFile.ReadInt("Parameter", "ProductionMode", this.productionMode, Def.GetAbsPathName(Def.MachineCfg));
            this.productFormula = IniFile.ReadInt("Parameter", "productFormula", this.productFormula, Def.GetAbsPathName(Def.MachineCfg));
            this.wcserverIP = IniFile.ReadString("Parameter", "wcserverIP", this.wcserverIP, Def.GetAbsPathName(Def.MachineCfg));

            this.UpdataMES = IniFile.ReadBool("Parameter", "UpdataMES", true, Def.GetAbsPathName(Def.MachineCfg));
            this.DataRecover = IniFile.ReadBool("Parameter", "DataRecover", true, Def.GetAbsPathName(Def.MachineCfg));
            this.AutoUploadWaterValue = IniFile.ReadBool("Parameter", "AutoUploadWaterValue", false, Def.GetAbsPathName(Def.MachineCfg));

            this.nMaxWaitOffFloorCount = IniFile.ReadInt("Parameter", "MaxWaitOffFloorCount", this.nMaxWaitOffFloorCount, Def.GetAbsPathName(Def.MachineCfg));
            this.strMesWaterName[0] = IniFile.ReadString("Parameter", "MesWaterName[0]", this.strMesWaterName[0], Def.GetAbsPathName(Def.MachineCfg));
            this.strMesWaterName[1] = IniFile.ReadString("Parameter", "MesWaterName[1]", this.strMesWaterName[1], Def.GetAbsPathName(Def.MachineCfg));
            this.strMesWaterName[2] = IniFile.ReadString("Parameter", "MesWaterName[2]", this.strMesWaterName[2], Def.GetAbsPathName(Def.MachineCfg));
            this.bOvenRestEnable = IniFile.ReadBool("Parameter", "OvenRestEnable", false, Def.GetAbsPathName(Def.MachineCfg));
            this.nStayOvenOutTime = IniFile.ReadInt("Parameter", "StayOvenOutTime", this.nStayOvenOutTime, Def.GetAbsPathName(Def.MachineCfg));
            this.nPressureHintTime = IniFile.ReadInt("Parameter", "PressureHintTime", this.nPressureHintTime, Def.GetAbsPathName(Def.MachineCfg));

            return true;
        }

        /// <summary>
        /// 参数写入（界面调用）
        /// </summary>
        public bool WriteParameter(string key, string value)
        {
            IniFile.WriteString("Parameter", key, value, Def.GetAbsPathName(Def.MachineCfg));
            return true;
        }

        /// <summary>
        /// 参数检查（界面调用）
        /// </summary>
        public virtual bool CheckParameter(string name, object value)
        {

            if ("DataRecover" == name)
            {
                IDialogResult dialog = default;
                if (!Convert.ToBoolean(value))
                {

                    App.Current.Dispatcher.Invoke(() =>
                    {
                         dialog = ShowMsgBox.ShowDialog("是否取消数据恢复？", MessageType.MsgQuestion);
                    });
                    if (ButtonResult.OK == dialog.Result)
                    {
                        if (ButtonResult.OK == ShowMsgBox.ShowDialog("取消数据恢复会清除所有运行数据！\r\n请确认是否清除所有运行数据？", MessageType.MsgQuestion).Result)
                        {
                            foreach (var item in MachineCtrl.GetInstance().ListRuns)
                            {
                                item.DeleteRunData();
                            }

                            return true;
                        }
                    }
                    return false;
                }
            }
            else if ("PalletMaxRow" == name)
            {
                if (Convert.ToInt32(value) <= 0)
                {
                    ShowMsgBox.ShowDialog("参数设置太小！", MessageType.MsgAlarm);
                    return false;
                }

                //if (Convert.ToInt32(value) > row)
                //{
                //    ShowMsgBox.ShowDialog("托盘行数量必须 <= " + (row) + "!", MessageType.MsgAlarm);
                //    return false;
                //}

            }
            else if ("PalletMaxCol" == name)
            {
                if (Convert.ToInt32(value) <= 0)
                {
                    ShowMsgBox.ShowDialog("参数设置太小！", MessageType.MsgAlarm);
                    return false;
                }

                //if (Convert.ToInt32(value) > (int)PltRowCol.MaxCol)
                //{
                //    ShowMsgBox.ShowDialog("托盘列数量必须 <= " + ((int)PltRowCol.MaxCol) + "!", MessageType.MsgAlarm);
                //    return false;
                //}
            }

            return true;
        }
        public T ReadParam<T>(string key, T defaultValue)
        {
            var readStr = IniFile.ReadString("Parameter", key, defaultValue.ToString(), Def.GetAbsPathName(Def.MachineCfg));
            return (T)Convert.ChangeType(readStr, typeof(T));
        }

        /// <summary>
        /// 添加系统参数
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        private void InsertPrivateParam(string key, string name, string description, object value, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.parameterProperty.Add("系统参数", key, name, description, value, (int)paraLevel, readOnly, visible);
        }

        #endregion

        #region // 文件清除线程

        /// <summary>
        /// 文件清除线程入口
        /// </summary>
        private void FileThreadProc()
        {
            while (bIsFileThread)
            {
                PastDueFile();
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 过期文件清除
        /// </summary>
        private bool PastDueFile()
        {
            // 间隔时间(2小时)
            if ((DateTime.Now - timeFile).TotalHours > 1)
            {
                timeFile = DateTime.Now;
                OvenPositionInfo ovenPosition = new OvenPositionInfo();
                ovenPosition.StatrTime1 = timeFile.AddDays(-7);

                RealDataHelp.DeleteRealData(ovenPosition);

                // 生成信息路径不能为空 && 文件保存时间大于0天
                if (!string.IsNullOrEmpty(this.ProductionFilePath) && this.nFileLifeDay > 0)
                {
                    // 生产信息
                    DeleteFile(this.ProductionFilePath, nFileLifeDay);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileDirect">文件夹路径</param>
        /// <param name="saveDay">保存时间</param>
        private void DeleteFile(string fileDirect, int saveDay)
        {
            if (!Directory.Exists(fileDirect)) return;

            // 判断是否为文件夹
            if (File.GetAttributes(fileDirect) == FileAttributes.Directory)
            {
                RecursionDeleteFile(new DirectoryInfo(fileDirect), saveDay);
            }
        }

        private void RecursionDeleteFile(DirectoryInfo directory, int saveDay)
        {
            try
            {
                FileInfo[] fileInfo = directory.GetFiles("*.*");                        // 文件
                DirectoryInfo[] directorys = directory.GetDirectories();                // 文件夹

                if (fileInfo.Count() < 1 && directorys.Count() < 1)                     // 无文件与无文件夹,则删除 "父" 文件夹
                {
                    Directory.Delete(directory.FullName, true);
                    return;
                }

                foreach (var file in fileInfo)                                          // 遍历文件
                {
                    TimeSpan time = DateTime.Now - file.LastWriteTime;                   // 当前时间 - 文件创建时间
                    if (time.Days > saveDay)
                    {
                        File.Delete(file.FullName);                                     // 删除超过时间的文件
                    }
                }

                foreach (var dire in directorys)                                        // 获取子文件夹内的文件列表,递归遍历
                {
                    RecursionDeleteFile(dire, saveDay);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }


        #endregion


        #region // 系统IO


        #region // IO读取

        /// <summary>
        /// 读IO
        /// </summary>
        private void ReadSystemIO()
        {
            int count = 0;
            string key = "";
            string path = "";
            string module = "System";
            List<int> inputs, outputs, motors;
            path = Def.GetAbsPathName(Def.ModuleExCfg);
            inputs = new List<int>();
            outputs = new List<int>();
            motors = new List<int>();

            #region // 按钮输入

            count = (int)SystemIOGroup.PanelButton;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("IStartButton[" + (idx + 1) + "]");
                this.IStartButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IStartButton[idx] > -1) inputs.Add(this.IStartButton[idx]);

                key = ("IStopButton[" + (idx + 1) + "]");
                this.IStopButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IStopButton[idx] > -1) inputs.Add(this.IStopButton[idx]);

                key = ("IResetButton[" + (idx + 1) + "]");
                this.IResetButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IResetButton[idx] > -1) inputs.Add(this.IResetButton[idx]);

                key = ("IManAutoButton[" + (idx + 1) + "]");
                this.IManAutoButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IManAutoButton[idx] > -1) inputs.Add(this.IManAutoButton[idx]);

                key = ("IPlcRunButton[" + (idx + 1) + "]");
                this.IPlcRunButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IPlcRunButton[idx] > -1) inputs.Add(this.IPlcRunButton[idx]);
            }

            for (int idx = 0; idx < (int)SystemIOGroup.IEStopNum; idx++)
            {
                key = ("IEStopButton[" + (idx + 1) + "]");
                this.IEStopButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopButton[idx] > -1) inputs.Add(this.IEStopButton[idx]);
            }

            #endregion

            #region // 按钮输出

            for (int idx = 0; idx < count; idx++)
            {
                key = ("OStartLed[" + (idx + 1) + "]");
                this.OStartLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OStartLed[idx] > -1) outputs.Add(this.OStartLed[idx]);

                key = ("OStopLed[" + (idx + 1) + "]");
                this.OStopLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OStopLed[idx] > -1) outputs.Add(this.OStopLed[idx]);

                key = ("OResetLed[" + (idx + 1) + "]");
                this.OResetLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OResetLed[idx] > -1) outputs.Add(this.OResetLed[idx]);
            }

            #endregion

            #region // 灯塔输出

            count = (int)SystemIOGroup.LightTower;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("OLightTowerRed[" + (idx + 1) + "]");
                this.OLightTowerRed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerRed[idx] > -1) outputs.Add(this.OLightTowerRed[idx]);

                key = ("OLightTowerYellow[" + (idx + 1) + "]");
                this.OLightTowerYellow[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerYellow[idx] > -1) outputs.Add(this.OLightTowerYellow[idx]);

                key = ("OLightTowerGreen[" + (idx + 1) + "]");
                this.OLightTowerGreen[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerGreen[idx] > -1) outputs.Add(this.OLightTowerGreen[idx]);

                key = ("OLightTowerBuzzer[" + (idx + 1) + "]");
                this.OLightTowerBuzzer[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerBuzzer[idx] > -1) outputs.Add(this.OLightTowerBuzzer[idx]);
            }

            #endregion

            #region //心跳输出
            count = (int)SystemIOGroup.HeartBeat;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("OHertBeat[" + (idx + 1) + "]");
                this.OHeartBeat[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OHeartBeat[idx] > -1) outputs.Add(this.OHeartBeat[idx]);
            }
            #endregion

            #region // 安全门IO

            count = (int)SystemIOGroup.SafeDoor;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("ISafeDoorEStop[" + (idx + 1) + "]");
                this.ISafeDoorEStop[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ISafeDoorEStop[idx] > -1) inputs.Add(this.ISafeDoorEStop[idx]);

                key = ("ISafeDoorOpenReq[" + (idx + 1) + "]");
                this.ISafeDoorOpenReq[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ISafeDoorOpenReq[idx] > -1) inputs.Add(this.ISafeDoorOpenReq[idx]);

                key = ("ISafeDoorCloseReq[" + (idx + 1) + "]");
                this.ISafeDoorCloseReq[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ISafeDoorCloseReq[idx] > -1) inputs.Add(this.ISafeDoorCloseReq[idx]);

                key = ("OSafeDoorOpenLed[" + (idx + 1) + "]");
                this.OSafeDoorOpenLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OSafeDoorOpenLed[idx] > -1) outputs.Add(this.OSafeDoorOpenLed[idx]);

                key = ("OSafeDoorCloseLed[" + (idx + 1) + "]");
                this.OSafeDoorCloseLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OSafeDoorCloseLed[idx] > -1) outputs.Add(this.OSafeDoorCloseLed[idx]);

                key = ("OSafeDoorUnlock[" + (idx + 1) + "]");
                this.OSafeDoorUnlock[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OSafeDoorUnlock[idx] > -1) outputs.Add(this.OSafeDoorUnlock[idx]);
            }

            #endregion

            #region // 机器人报警输入
            count = (int)SystemIOGroup.OnOffLoadRobot;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("IOnLoadRobotAlarm[" + (idx + 1) + "]");
                this.IOnloadRobotAlarm[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IOnloadRobotAlarm[idx] > -1) inputs.Add(this.IOnloadRobotAlarm[idx]);
            }
            for (int idx = 0; idx < count; idx++)
            {
                key = ("IOffLoadRobotAlarm[" + (idx + 1) + "]");
                this.IOffloadRobotAlarm[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IOffloadRobotAlarm[idx] > -1) inputs.Add(this.IOffloadRobotAlarm[idx]);
            }
            count = (int)SystemIOGroup.TransferRobot;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("ITransferRobotAlarm[" + (idx + 1) + "]");
                this.ITransferRobotAlarm[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ITransferRobotAlarm[idx] > -1) inputs.Add(this.ITransferRobotAlarm[idx]);
            }

            count = (int)SystemIOGroup.RobotCrash;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("IRobotCrash[" + (idx + 1) + "]");
                this.IRobotCrash[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IRobotCrash[idx] > -1) inputs.Add(this.IRobotCrash[idx]);
            }

            for (int idx = 0; idx < 2; idx++)
            {
                key = ("IBufDoor[" + (idx + 1) + "]");
                this.IBufDoor[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IBufDoor[idx] > -1) inputs.Add(this.IBufDoor[idx]);

                key = ("ICylAlarm[" + (idx + 1) + "]");
                this.ICylAlarm[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ICylAlarm[idx] > -1) inputs.Add(this.ICylAlarm[idx]);

                key = ("IRasterAlarm[" + (idx + 1) + "]");
                this.IRasterAlarm[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IRasterAlarm[idx] > -1) inputs.Add(this.IRasterAlarm[idx]);
            }
            #endregion

            #region // 调度替罪羊报警输入
            count = 2;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("ITransferGoat[" + (idx + 1) + "]");
                this.ITransferGoat[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ITransferGoat[idx] > -1) inputs.Add(this.ITransferGoat[idx]);
            }
            #endregion

            #region // 上下料气压报警输入
            count = 2;
            for (int idx = 0; idx < count; idx++)
            {
                key = ("ICheckPressure[" + (idx + 1) + "]");
                this.ICheckPressure[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.ICheckPressure[idx] > -1) inputs.Add(this.ICheckPressure[idx]);
            }
            #endregion

            WriteModuleCfg(0, module, inputs, outputs, motors);
        }

        #endregion


        #region // IO操作

        /// <summary>
        /// 查看输入状态
        /// </summary>
        private bool InputState(int input, bool isOn)
        {
            if (input > -1)
            {
                return (isOn ? DeviceManager.Inputs(input).IsOn() : DeviceManager.Inputs(input).IsOff());
            }
            return false;
        }

        /// <summary>
        /// 查看输出状态
        /// </summary>
        private bool OutputState(int output, bool isOn)
        {
            if (output > -1)
            {
                return (isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff());
            }
            return false;
        }

        /// <summary>
        /// 输出状态
        /// </summary>
        private bool OutputAction(int output, bool isOn)
        {
            if (output > -1)
            {
                if (isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff())
                {
                    return true;
                }

                return (isOn ? DeviceManager.Outputs(output).On() : DeviceManager.Outputs(output).Off());
            }
            return false;
        }

        #endregion


        #region // 系统按钮、安全门检查

        /// <summary>
        /// 启动按钮
        /// </summary>
        private bool StartBtnPress()
        {
            foreach (var item in this.IStartButton)
            {
                if (InputState(item, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 停止按钮
        /// </summary>
        private bool StopBtnPress()
        {
            foreach (var item in this.IStopButton)
            {
                if (InputState(item, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 复位按钮
        /// </summary>
        private bool ResetBtnPress()
        {
            foreach (var item in this.IResetButton)
            {
                if (InputState(item, true))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 手自动切换按钮
        /// </summary>
        private bool ManAutoBtnPress()
        {
            return true;
            foreach (var item in this.IManAutoButton)
            {
                if (InputState(item, false))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// PLC运行按钮
        /// </summary>
        public bool PlcRunPress()
        {
            for (int i = 0; i < IPlcRunButton.Length; i++)
            {
                if (InputState(IPlcRunButton[i], false))
                {
                    string str = i == 0 ? "上料PLC" : "下料PLC";
                    ShowMsgBox.ShowDialog($"{str}不在运行中，请检查后启动！", MessageType.MsgAlarm);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 急停按钮
        /// </summary>
        private bool IEStpBtnPress(ref int nIndex)
        {
            for (int i = 0; i < (int)SystemIOGroup.IEStopNum; i++)
            {
                if (InputState(IEStopButton[i], true))
                {
                    nIndex = i;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 安全门开关输入
        /// </summary>
        public bool ISafeDoorEStopBtnPress()
        {
            //return false;
            foreach (var item in this.ISafeDoorEStop)
            {
                if (InputState(item, false))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 安全门开关输入状态
        /// </summary>
        public bool ISafeDoorEStopState(int input, bool isOn)
        {
            //return true;
            if (Def.IsNoHardware())
            {
                return true;
            }
            if (InputState(ISafeDoorEStop[input], isOn))
            {
                return true;
            }
            return false;

        }
        #endregion


        #region // 系统IO、安全门、机器人监视线程

        /// <summary>
        /// 初始化线程
        /// </summary>
        private bool InitThread()
        {
            try
            {
                if (null == taskSysThread)
                {
                    bIsRunSysThread = true;
                    taskSysThread = new Task(SystemThreadProc, TaskCreationOptions.LongRunning);
                    taskSysThread.Start();
                }

                if (null == taskSafeDoorThread)
                {
                    bIsRunSafeDoorThread = true;
                    taskSafeDoorThread = new Task(SafeDoorThreadProc, TaskCreationOptions.LongRunning);
                    taskSafeDoorThread.Start();
                }

                if (null == taskRobotAlarmThread)
                {
                    bIsRuntaskRobotAlarmThread = true;
                    taskRobotAlarmThread = new Task(RobotAlarmThread, TaskCreationOptions.LongRunning);
                    taskRobotAlarmThread.Start();
                }

                if (null == taskWCThread)
                {
                    bIsRunWCThread = true;
                    taskWCThread = new Task(WCThreadProc, TaskCreationOptions.LongRunning);
                    taskWCThread.Start();
                }

                if (null == taskMachineState)
                {
                    bIsRuntaskMachineState = true;
                    taskMachineState = new Task(MachineStateThread);
                    taskMachineState.Start();
                }

                //if (null == taskMesStatusThread)
                //{
                //    bIsRunMesStatusThread = true;
                //    taskMesStatusThread = new Task(MesStatusThreadProc, TaskCreationOptions.LongRunning);
                //    taskMesStatusThread.Start();
                //}

                if (null == taskScrSaverThread)
                {
                    bIsRunScrSaverThread = true;
                    taskScrSaverThread = new Task(ScrSaverThreadProc, TaskCreationOptions.LongRunning);
                    taskScrSaverThread.Start();
                }

                if (null == taskPastDueFileThread)
                {
                    bIsFileThread = true;
                    taskPastDueFileThread = new Task(FileThreadProc, TaskCreationOptions.LongRunning);
                    taskPastDueFileThread.Start();
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        private bool ReleaseThread()
        {
            try
            {
                if (null != taskSysThread)
                {
                    bIsRunSysThread = false;
                    taskSysThread.Wait();
                    taskSysThread.Dispose();
                    taskSysThread = null;
                }


                if (null != taskSafeDoorThread)
                {
                    bIsRunSafeDoorThread = false;
                    taskSafeDoorThread.Wait();
                    taskSafeDoorThread.Dispose();
                    taskSafeDoorThread = null;
                }

                if (null != taskWCThread)
                {
                    bIsRunWCThread = false;
                    taskWCThread.Wait();
                    taskWCThread.Dispose();
                    taskWCThread = null;
                }

                if (null != taskScrSaverThread)
                {
                    bIsRunScrSaverThread = false;
                    taskScrSaverThread.Wait();
                    taskScrSaverThread.Dispose();
                    taskScrSaverThread = null;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 系统线程入口
        /// </summary>
        private void SystemThreadProc()
        {
            while (bIsRunSysThread)
            {
                SystemIOMonitor();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 系统IO监视
        /// </summary>
        private void SystemIOMonitor()
        {
            Thread.Sleep(200);

            if (Def.IsNoHardware())
            {
                return;
            }

            //if (!Def.IsNoHardware() && !LoadingPlc.Connect())
            //{
            //    MachineCtrl.GetInstance().dbRecord.AddAlarmInfo(new AlarmFormula(productFormula, 1111, "上料PCL连接异常，请重启软件！", 2, 7, "MachineCtrl", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            //    ShowMsgBox.ShowDialog("上料PCL连接异常，请重启软件！", MessageType.MsgAlarm);
            //}

            //if (!Def.IsNoHardware() && !UnLoadingPlc.Connect())
            //{
            //    MachineCtrl.GetInstance().dbRecord.AddAlarmInfo(new AlarmFormula(productFormula, 1111, "下料PCL连接异常，请重启软件！", 2, 7, "MachineCtrl", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            //    ShowMsgBox.ShowDialog("下料PCL连接异常，请重启软件！", MessageType.MsgAlarm);
            //}

            // 灯塔状态
            int towerCount = (int)SystemIOGroup.LightTower;
            MCState mcState = this.RunsCtrl.GetMCState();
            RunProOnloadRobot onloadRobot = GetModule(RunID.OnloadRobot) as RunProOnloadRobot;
            RunProOffloadRobot offloadRobot = GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            RunProTransferRobot transferRobot = GetModule(RunID.Transfer) as RunProTransferRobot;

            // 闲置、初始化停止、运行停止
            if (MCState.MCIdle == mcState || MCState.MCStopInit == mcState || MCState.MCStopRun == mcState)
            {
                for (int nIdx = 0; nIdx < towerCount; nIdx++)
                {
                    OutputAction(OLightTowerRed[nIdx], false);
                    OutputAction(OLightTowerYellow[nIdx], true);
                    OutputAction(OLightTowerGreen[nIdx], false);
                    OutputAction(OLightTowerBuzzer[nIdx], false);
                    OutputAction(OStartLed[nIdx], false);
                    OutputAction(OStopLed[nIdx], true);
                }
            }
            // 初始化完成（闪烁）
            else if (MCState.MCInitComplete == mcState || onloadRobot.bRobotCrash || offloadRobot.bRobotCrash)
            {
                if ((DateTime.Now - towerStartTime).TotalMilliseconds >= 800.0)
                {
                    FlashingGreen(towerCount);

                    for (int nIdx = 0; nIdx < towerCount; nIdx++)
                    {
                        OutputAction(OStartLed[nIdx], false);
                        OutputAction(OStopLed[nIdx], true);
                    }
                }
            }
            else if (onloadRobot.bRobotCrash || offloadRobot.bRobotCrash)
            {
                if ((DateTime.Now - towerStartTime).TotalMilliseconds >= 800.0)
                {
                    FlashingGreen(towerCount);

                    if (onloadRobot.bRobotCrash)
                    {
                        OutputAction(OStartLed[0], false);
                        OutputAction(OStopLed[0], true);
                    }
                    if (offloadRobot.bRobotCrash)
                    {
                        OutputAction(OStartLed[1], false);
                        OutputAction(OStopLed[1], true);
                    }
                }
            }
            // 初始化中、运行中
            else if (MCState.MCInitializing == mcState || MCState.MCRunning == mcState)
            {
                for (int nIdx = 0; nIdx < towerCount; nIdx++)
                {
                    OutputAction(OLightTowerRed[nIdx], false);
                    OutputAction(OLightTowerYellow[nIdx], false);
                    OutputAction(OLightTowerGreen[nIdx], true);
                    OutputAction(OLightTowerBuzzer[nIdx], false);
                    OutputAction(OStartLed[nIdx], true);
                    OutputAction(OStopLed[nIdx], false);
                }
            }
            // 初始化错误、运行错误
            else if (MCState.MCInitErr == mcState || MCState.MCRunErr == mcState)
            {
                if ((DateTime.Now - towerStartTime).TotalMilliseconds >= 800.0)
                {
                    bool bState = OutputState(OLightTowerBuzzer[0], true);

                    for (int nIdx = 0; nIdx < towerCount; nIdx++)
                    {
                        OutputAction(OLightTowerRed[nIdx], true);
                        OutputAction(OLightTowerYellow[nIdx], false);
                        OutputAction(OLightTowerGreen[nIdx], false);
                        OutputAction(OLightTowerBuzzer[nIdx], !bState);
                        OutputAction(OStartLed[nIdx], false);
                        OutputAction(OStopLed[nIdx], true);
                    }
                }
            }

            int nIndex = -1;
            string msg = "";
            // 急停按下
            if (IEStpBtnPress(ref nIndex))
            {
                Thread.Sleep(200);
                if (IEStpBtnPress(ref nIndex))
                {
                    switch (nIndex)
                    {
                        case 0:
                            msg = "上料控制柜";
                            break;
                        case 1:
                            msg = "下料控制柜";
                            break;
                        case 2:
                            msg = "上料安全门";
                            break;
                        case 3:
                            msg = "下料安全门";
                            break;
                        default:
                            break;
                    }
                    this.RunsCtrl.Stop();
                    string Msg = string.Format("{0}急停按钮被按下，请检查", msg);

                    transferRobot.RecordMessageInfo((int)MsgID.SafeDoorAlarm, Msg, MessageType.MsgAlarm);
                    ShowMsgBox.ShowDialog(Msg, MessageType.MsgAlarm);
                    return;
                }
            }

            // 自动按下
            if (ManAutoBtnPress())
            {
                // 停止按下
                if (StopBtnPress())
                {
                    this.RunsCtrl.Stop();
                }
                // 复位按下
                else if (ResetBtnPress())
                {
                    this.RunsCtrl.Reset();
                }
                // 启动按下
                else if (StartBtnPress())
                {
                    if (ISafeDoorEStopBtnPress())
                    {
                        Thread.Sleep(200);
                        if (ISafeDoorEStopBtnPress())
                        {
                            this.RunsCtrl.Stop();
                            ShowMsgBox.ShowDialog("安全门未关闭，请检查！", MessageType.MsgAlarm);
                            return;
                        }
                    }

                    if (onloadRobot.robotProcessingFlag || offloadRobot.robotProcessingFlag || transferRobot.robotProcessingFlag)
                    {
                        ShowMsgBox.ShowDialog("机器人动作运行中，请等待机器人动作停止后再进行启动操作", MessageType.MsgMessage);
                        return;
                    }

                    if (!transferRobot.CheckRobotStartPos(out msg) || !onloadRobot.CheckRobotStartPos(out msg) || !offloadRobot.CheckRobotStartPos(out msg))
                    {
                        this.RunsCtrl.Stop();
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                        return;
                    }
                    if (PlcRunPress())
                    {
                        stopwatch.Start();
                        if (stopwatch.Elapsed.TotalSeconds >= 2)
                        {
                            this.RunsCtrl.Start();
                            stopwatch.Reset();
                        }

                    }
                }
            }
            else
            {
                if (MCState.MCInitializing == mcState || MCState.MCRunning == mcState)
                {
                    this.RunsCtrl.Stop();
                }

                if (StartBtnPress())
                {
                    ShowMsgBox.ShowDialog("手自动开关在手动位置，请切换到自动，后启动！", MessageType.MsgAlarm);
                }

                //上下料夹爪状态复位，避免切自动掉落电池
                onloadRobot.CloseOutPutState();
                offloadRobot.CloseOutPutState();
            }
            foreach (var item in this.ListRuns)
            {
                item.MonitorAvoidDie();
            }

        }

        /// <summary>
        /// 安全门线程入口
        /// </summary>
        private void SafeDoorThreadProc()
        {
            while (bIsRunSafeDoorThread)
            {
                SafeDoorMonitor();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 机器人报警线程入口
        /// </summary>
        private void RobotAlarmThread()
        {
            while (bIsRuntaskRobotAlarmThread)
            {
                RobotMonitor();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 设备状态线程
        /// </summary>
        private void MachineStateThread()
        {
            while (bIsRuntaskMachineState)
            {
                UpLoadMachineState();
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Mes状态线程入口
        /// </summary>
        private void MesStatusThreadProc()
        {
            while (bIsRunMesStatusThread)
            {
                // 检查Mes状态
                CheckMesStatus();

                // 获取Mes下发参数
                GetMesSpecifications();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 上传设备状态
        /// </summary>
        public void UpLoadMachineState()
        {
            if (!bIsMESState) MesHeartbeat(this.Equip_Code);
            if ((DateTime.Now - MachineStateTime).TotalMinutes < 10)
            {
                return;
            }
            MachineStateTime = DateTime.Now;
            
            for (RunID nOvenIdx = RunID.DryOven0; nOvenIdx < RunID.RunIDEnd; nOvenIdx++)
            {
                RunProDryingOven oven = GetModule(nOvenIdx) as RunProDryingOven;

                // 炉子是否连接
                if (oven.OvenIsConnect())
                {
                    string strResourceID = oven.GetstrResourceID();
                    MesHeartbeat(strResourceID);
                    MesEnergyUpload(oven.GetOvenID(), (float)oven.bgCavityData[0].fEnergySum, (float)oven.bgCavityData[0].unOneDayEnergy, (float)(oven.bgCavityData[0].unBatAverEnergy));
                    //if (0 != oven.bgCavityData[0].fEnergySum)
                    //{
                    //    MesEnergyUpload(oven.GetOvenID(), (uint)oven.bgCavityData[0].fEnergySum, (uint)oven.bgCavityData[0].fMinutesEnergy, (uint)(oven.bgCavityData[0].fEnergyTime / 60));
                    //}
                }
            }
        }

        /// <summary>
        /// 设备报警或复位上传
        /// </summary>
        public bool MesSendAlarmInfo(string strResourceID, AlarmsItem alarms)
        {
            //需要PLC提供规划的报警代码，基于设备厂家提供的报警清单，在MES中维护报警代码和报警详细描述的对应关系
            string nErrStr = "";
            return MesAlarmUpload(strResourceID, alarms.ALARM_VALUE, alarms.ALARM_CODE, alarms.START_TIME, ref nErrStr);

        }

        /// <summary>
        /// 检查Mes状态
        /// </summary>
        private void CheckMesStatus()
        {
            if ((DateTime.Now - StatusStartTime).TotalMinutes > 5)
            {
                StatusStartTime = DateTime.Now;
                //MesHeartbeat();
            }
            Thread.Sleep(500);
        }

        /// <summary>
        /// 获取生产规格参数下发
        /// </summary>
        private void GetMesSpecifications()
        {
            if ((DateTime.Now - GetParamTime).TotalMinutes > 20)
            {
                GetParamTime = DateTime.Now;
                //MesGetSpecifications();
            }
        }

        /// <summary>
        /// 安全门监视
        /// </summary>
        private void SafeDoorMonitor()
        {
            #region // 时长计时

            // 报警时长
            if (MCState.MCRunErr == RunsCtrl.GetMCState())
            {
                SetTiming(TimingType.MCAlarmTime, true);
                SetTiming(TimingType.MCRunningTime, false);
                SetTiming(TimingType.MCStopRunTime, true);
            }
            else
            {
                SetTiming(TimingType.MCAlarmTime, false);
            }

            // 计时
            Enum.GetValues(typeof(TimingType)).Cast<TimingType>().ForEach((type, nIdx) =>
            {
                if (type == (type & timingType))
                {
                    if (!timings[nIdx].Item3)
                    {
                        timings[nIdx] = (0, DateTime.Now, true);
                    }

                    int nMinutes = (int)(DateTime.Now - timings[nIdx].Item2).TotalMinutes;
                    if (timings[nIdx].Item1 != nMinutes)
                        timings[nIdx].Item1 = nMinutes;
                }
                else if (timings[nIdx].Item3)
                {
                    timings[nIdx].Item3 = false;
                }
            });

            // 界面刷新
            if (M_nWaitOnlLineTime != timings[0].Item1) M_nWaitOnlLineTime = timings[0].Item1;
            if (M_nWaitOffLineTime != timings[1].Item1) M_nWaitOffLineTime = timings[1].Item1;
            if (M_nMCRunningTime != timings[2].Item1) M_nMCRunningTime = timings[2].Item1;
            if (M_nMCStopRunTime != timings[3].Item1) M_nMCStopRunTime = timings[3].Item1;
            if (M_nAlarmTime != timings[4].Item1) M_nAlarmTime = timings[4].Item1;

            if ((M_nMCStopRunTime >= 3 && bIsEquipmentState))
            {
                bIsEquipmentState = false;
                //DeviceStatusViewModel.CheckCondition();
                string strErr = "";
                MesStateAndStopReasonUpload(this.Equip_Code, "5", "0", ref strErr);
            }
            if (M_nMCStopRunTime == 0) { bIsEquipmentState = true; }

            if (RunsCtrl.GetMCState() != MCState.MCRunning)
            {
                nUpRuningFlag = true;
            }
            if ((RunsCtrl.GetMCState() == MCState.MCRunning) && nUpRuningFlag)
            {
                string strErr = "";
                MesStateAndStopReasonUpload(this.Equip_Code, "2", "", ref strErr);
                nUpRuningFlag = false;
            }

            #endregion

            // 心跳状态
            if ((DateTime.Now - towerStartTime).TotalMilliseconds >= 500.0)
            {
                bool bState = OutputState(OHeartBeat[0], true);
                OutputAction(OHeartBeat[0], !bState);
                OutputAction(OHeartBeat[1], bState);

                // 关闭屏保页面
                if (bState == bPlcOldState)
                {
                    nPlcStateCount++;
                }
                else
                {
                    nPlcStateCount = 0;
                    bPlcOldState = bState;
                }
            }
            // 待添加
            for (int j = 0; j < (int)SystemIOGroup.IEStopNum; j++)
            {
                if (InputState(IEStopButton[j], true))
                {
                    Thread.Sleep(200);
                    if (InputState(IEStopButton[j], true))
                    {
                        for (int i = 0; i < (int)SystemIOGroup.SafeDoor; i++)
                        {
                            OutputAction(OSafeDoorUnlock[i], true);
                            OutputAction(OSafeDoorOpenLed[i], true);
                            OutputAction(OSafeDoorCloseLed[i], false);
                        }

                    }
                }
            }
            for (int i = 0; i < (int)SystemIOGroup.SafeDoor; i++)
            {
                if (InputState(ISafeDoorOpenReq[i], true))
                {
                    Thread.Sleep(1000);
                    if (InputState(ISafeDoorOpenReq[i], true))
                    {
                        this.RunsCtrl.Stop();
                        MCState mcState = this.RunsCtrl.GetMCState();
                        if (MCState.MCRunning != mcState)
                        {
                            OutputAction(OSafeDoorOpenLed[i], true);
                            OutputAction(OSafeDoorUnlock[i], true);
                            OutputAction(OSafeDoorCloseLed[i], false);
                        }
                    }
                }
                if (InputState(ISafeDoorCloseReq[i], true))
                {
                    Thread.Sleep(500);
                    if (InputState(ISafeDoorCloseReq[i], true))
                    {
                        OutputAction(OSafeDoorOpenLed[i], false);
                        OutputAction(OSafeDoorUnlock[i], false);
                        OutputAction(OSafeDoorCloseLed[i], true);
                    }
                }
            }

            // 8点记录生产数据
            string strTime = DateTime.Now.ToString("hh:mm");
            if (strTime == "08:00")
            {
                if (!bRecordData)
                {
                    bRecordData = true;
                    DataList_Auto_Reset();
                }
            }
            else
            {
                bRecordData = false;
            }

            Thread.Sleep(200);
        }
        public void DataList_Auto_Reset() 
        {
            string sFilePath = string.Format("{0}\\ProductData", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "上下料数据.CSV";
            string sColHead, sLog;
            sColHead = "日期,";
            sLog = DateTime.Now.ToString("") + ",";
            var listProduct = ShowProductDatas.OfType<ShowCountInfo<int>>().ToList();
            foreach (var item in listProduct)
            {
                sColHead += item.Name + ",";
                sLog += item.ProductData + ",";
            }
            sColHead = sColHead.TrimEnd(',');
            sLog = sLog.TrimEnd(',');
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
            MachineCtrl.GetInstance().InitProduceCount();
            MachineCtrl.GetInstance().SaveProduceCount();
            for (int i = 0; i < (int)TransferRobotStation.DryingOven_5; i++)
            {
                RunProDryingOven oven = MachineCtrl.GetInstance().GetModule(RunID.DryOven0 + i) as RunProDryingOven;
                oven.ReleaseBatCount();

            }
        }
        /// <summary>
        /// 机器人报警监视
        /// </summary>
        private void RobotMonitor()
        {

            if (Def.IsNoHardware())
            {
                Thread.Sleep(10);
                return;
            }

            // 机器人碰撞
            //RunProKickTrashRobot kickTrashRobot = GetModule(RunID.KickTrashRobot) as RunProKickTrashRobot;
            RunProOnloadRobot onloadRobot = GetModule(RunID.OnloadRobot) as RunProOnloadRobot;
            RunProOffloadRobot offloadRobot = GetModule(RunID.OffloadRobot) as RunProOffloadRobot;
            string strAlarmInfo = "";

            if (InputState(ICheckPressure[0], true))
            {
                Thread.Sleep(200);
                if (InputState(ICheckPressure[0], true))
                {
                    strAlarmInfo = "上料气压过低,请检查上料气压";
                    ShowMsgBox.ShowDialog(strAlarmInfo, MessageType.MsgAlarm);
                    onloadRobot.RecordMessageInfo((int)MsgID.OnloadPressureLow, strAlarmInfo, MessageType.MsgAlarm);
                    this.RunsCtrl.Stop();
                }
            }
            if (InputState(ICheckPressure[1], true))
            {
                Thread.Sleep(200);
                if (InputState(ICheckPressure[1], true))
                {
                    strAlarmInfo = "下料气压过低,请检查下料气压";
                    ShowMsgBox.ShowDialog(strAlarmInfo, MessageType.MsgAlarm);
                    offloadRobot.RecordMessageInfo((int)MsgID.OffloadPressureLow, strAlarmInfo, MessageType.MsgAlarm);
                    this.RunsCtrl.Stop();
                }
            }
            if (InputState(IOnloadRobotAlarm[0], true))
            {
                strAlarmInfo = "上料机器人柔性碰撞报警";
                offloadRobot.RecordMessageInfo((int)MsgID.KickTrashRobotCrash + 0, strAlarmInfo, MessageType.MsgWarning);
                ShowMsgBox.ShowDialog(strAlarmInfo, MessageType.MsgAlarm);
            }
            if (InputState(IOffloadRobotAlarm[0], true))
            {
                strAlarmInfo = "下料机器人柔性碰撞报警";
                offloadRobot.RecordMessageInfo((int)MsgID.KickTrashRobotCrash + 0, strAlarmInfo, MessageType.MsgWarning);
                ShowMsgBox.ShowDialog(strAlarmInfo, MessageType.MsgAlarm);
            }
            /*var robots = GetModule<IRobot>();
            robots.ForEach((robat, index) =>
            {
                if (InputState(IRobotCrash[index], true) && !robat.RobotCrash)
                {
                    Thread.Sleep(200);
                    if (InputState(IRobotCrash[index], true))
                    {
                        robat.RobotCrash = true;
                        strAlarmInfo = $"{robat.RobotName()}柔性碰撞报警";
                        (robat as RunProcess).RecordMessageInfo((int)MsgID.KickTrashRobotCrash + index, strAlarmInfo, MessageType.MsgWarning);
                        ShowMsgBox.ShowDialog(strAlarmInfo, MessageType.MsgAlarm);
                    }
                }
                if (InputState(IRobotCrash[index], false) && robat.RobotCrash)
                {
                    Thread.Sleep(200);
                    if (InputState(IRobotCrash[index], false))
                    {
                        robat.RobotCrash = false;
                    }
                }
            });*/


            for (int i = 0; i < (int)SystemIOGroup.TransferRobot; i++)
            {
                if (InputState(ITransferRobotAlarm[i], true))
                {
                    //this.RunsCtrl.Stop();
                    //ShowMsgBox.ShowDialog("调度机器人接近传感器报警，请检查机器人状态，后复位启动！", MessageType.MsgAlarm);
                }
            }
            /*
            for (int i = 0; i < 2; i++)
            {
                if (InputState(ITransferGoat[i], true))
                {
                    this.RunsCtrl.Stop();
                    ShowMsgBox.ShowDialog("调度替罪羊未归位，请检查后复位启动！", MessageType.MsgAlarm);
                }
            }*/
        }

        /// <summary>
        /// 检查机器人碰撞信号
        /// </summary>
        public bool CheckRobotCrashSingle()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            bool en = false;
            GetModule<IRobot>().ForEach((robat, index) =>
            {
                if (InputState(IRobotCrash[0], true))
                {
                    string strMsg = string.Format($"{robat.RobotName()}碰撞信号未复位，请检查！");
                    ShowMsgBox.ShowDialog(strMsg, MessageType.MsgAlarm);
                    en = true;
                }
            });
            if (en)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 屏保线程入口
        /// </summary>
        private void ScrSaverThreadProc()
        {
            while (bIsRunScrSaverThread)
            {
                ScrSaverMonitor();
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 屏保监视
        /// </summary>
        private void ScrSaverMonitor()
        {
            if (Def.IsNoHardware())
            {
                Thread.Sleep(500);
                return;
            }

            // 安全门打开
            if (ISafeDoorEStopBtnPress())
            {
                Thread.Sleep(500);
                if (!bIsSafeDoorOpen)
                {
                    if (ISafeDoorEStopBtnPress())
                    {
                        RunID runID = RunID.RunIDEnd;
                        RunProDryingOven oven = null;
                        for (int nOvenIdx = 0; nOvenIdx < runID - RunID.DryOven0; nOvenIdx++)
                        {
                            runID = RunID.DryOven0 + nOvenIdx;
                            oven = GetModule(runID) as RunProDryingOven;
                            oven.OvenPcSafeDoorState(PCSafeDoorState.Open);
                        }
                        string strMsg = string.Format("安全门打开");
                        RunProTransferRobot transferRobot = GetModule(RunID.Transfer) as RunProTransferRobot;
                        transferRobot.RecordMessageInfo((int)MsgID.SafeDoorAlarm, strMsg, MessageType.MsgAlarm);
                        bIsSafeDoorOpen = true;
                    }
                }
            }
            else
            {
                if (bIsSafeDoorOpen)
                {
                    RunID runID = RunID.RunIDEnd;
                    RunProDryingOven oven = null;
                    for (int nOvenIdx = 0; nOvenIdx < runID - RunID.DryOven0; nOvenIdx++)
                    {
                        runID = RunID.DryOven0 + nOvenIdx;
                        oven = GetModule(runID) as RunProDryingOven;
                        oven.OvenPcSafeDoorState(PCSafeDoorState.Close);
                    }
                    bIsSafeDoorOpen = false;
                }
            }
            Thread.Sleep(200);
        }
        #endregion


        #region // 灯控


        /// <summary>
        /// 绿灯闪烁(三色灯)
        /// </summary>
        /// <param name="towerCount"></param>
        public void FlashingGreen(int towerCount)
        {
            bool bState = OutputState(OLightTowerGreen[0], true);

            for (int nIdx = 0; nIdx < towerCount; nIdx++)
            {
                OutputAction(OLightTowerRed[nIdx], false);
                OutputAction(OLightTowerYellow[nIdx], false);
                OutputAction(OLightTowerGreen[nIdx], !bState);
                OutputAction(OLightTowerBuzzer[nIdx], false);
            }
        }

        #endregion

        #endregion


        #region // 数据统计

        /// <summary>
        /// 设置计时(开启或停止)
        /// </summary>
        public void SetTiming(TimingType type, bool bIsStart)
        {
            if (bIsStart)
            {
                if (0 == (type & timingType)) timingType |= type;
            }
            else
            {
                if (type == (type & timingType)) timingType &= ~type;
            }
        }

        /// <summary>
        /// 初始化生产数量
        /// </summary>
        public void InitProduceCount()
        {
            timings = new (int, DateTime, bool)[Enum.GetValues(typeof(TimingType)).Length];
            M_nOnloadTotal = 0;
            m_nOnloadFakeTotal = 0;

            M_nWaitOnlLineTime=0;
            M_nWaitOffLineTime = 0;
            M_nMCRunningTime = 0;
            M_nMCStopRunTime = 0;
            M_nAlarmTime = 0;

            M_nOffloadTotal = 0;
            m_nOffloadFakeTotal = 0;
            M_nNgTotal = 0;
            nOnloadOldTotal = 0;
            nOffloadOldTotal = 0;
        }

        /// <summary>
        /// 保存生产数量
        /// </summary>
        public void SaveProduceCount()
        {
            IniFile.WriteInt("ProduceCount", "nOnloadTotal", M_nOnloadTotal, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "m_nOnloadFakeTotal", m_nOnloadFakeTotal, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nOffloadTotal", M_nOffloadTotal, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "m_nOffloadFakeTotal", m_nOffloadFakeTotal, Def.GetAbsPathName(Def.MachineCfg));

            IniFile.WriteInt("ProduceCount", "nOnloadYeuid", m_nOnloadYeuid, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nOffloadYeuid", m_nOffloadYeuid, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nNgTotal", M_nNgTotal, Def.GetAbsPathName(Def.MachineCfg));

            IniFile.WriteInt("ProduceCount", "nAlarmTime", M_nAlarmTime, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nWaitOnlLineTime", M_nWaitOnlLineTime, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nWaitOffLineTime", M_nWaitOffLineTime, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nMCRunningTime", M_nMCRunningTime, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nMCStopRunTime", M_nMCStopRunTime, Def.GetAbsPathName(Def.MachineCfg));

            IniFile.WriteInt("ProduceCount", "nOnloadOldTotal", nOnloadOldTotal, Def.GetAbsPathName(Def.MachineCfg));
            IniFile.WriteInt("ProduceCount", "nOffloadOldTotal", nOffloadOldTotal, Def.GetAbsPathName(Def.MachineCfg));
        }

        /// <summary>
        /// 读取生产数量
        /// </summary>
        public void ReadProduceCount()
        {
            this.M_nOnloadTotal = IniFile.ReadInt("ProduceCount", "nOnloadTotal", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.m_nOnloadFakeTotal = IniFile.ReadInt("ProduceCount", "m_nOnloadFakeTotal", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nOffloadTotal = IniFile.ReadInt("ProduceCount", "nOffloadTotal", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.m_nOffloadFakeTotal = IniFile.ReadInt("ProduceCount", "m_nOffloadFakeTotal", 0, Def.GetAbsPathName(Def.MachineCfg));

            this.m_nOnloadYeuid = IniFile.ReadInt("ProduceCount", "nOnloadYeuid", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.m_nOffloadYeuid = IniFile.ReadInt("ProduceCount", "nOffloadYeuid", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nNgTotal = IniFile.ReadInt("ProduceCount", "nNgTotal", 0, Def.GetAbsPathName(Def.MachineCfg));

            this.M_nAlarmTime = IniFile.ReadInt("ProduceCount", "nAlarmTime", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nWaitOnlLineTime = IniFile.ReadInt("ProduceCount", "nWaitOnlLineTime", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nWaitOffLineTime = IniFile.ReadInt("ProduceCount", "nWaitOffLineTime", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nMCRunningTime = IniFile.ReadInt("ProduceCount", "nMCRunningTime", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.M_nMCStopRunTime = IniFile.ReadInt("ProduceCount", "nMCStopRunTime", 0, Def.GetAbsPathName(Def.MachineCfg));

            this.nOnloadOldTotal = IniFile.ReadInt("ProduceCount", "nOnloadOldTotal", 0, Def.GetAbsPathName(Def.MachineCfg));
            this.nOffloadOldTotal = IniFile.ReadInt("ProduceCount", "nOffloadOldTotal", 0, Def.GetAbsPathName(Def.MachineCfg));
        }

        #endregion

        #region // 数据统计

        /// <summary>
        /// 读统计数据
        /// </summary>
        private void ReadTotalData()
        {

        }

        #region // 自定义方法

        /// <summary>
        /// 获取托盘最大行列
        /// </summary>
        public void GetPltRowCol(ref int nRowCount, ref int nColCount)
        {
            lock (lockRowCol)
            {
                nRowCount = PltMaxRow;
                nColCount = PltMaxCol;
            }
        }

        /// <summary>
        /// 设置托盘的最大行列
        /// </summary>
        public void SetPltRowCol(int nRowCount, int nColCount)
        {
            lock (lockRowCol)
            {
                PltMaxRow = nRowCount;
                PltMaxCol = nColCount;
            }
        }

        public bool PlcIsAuto(int num)
        {
            //if (InputState(IManAutoButton[num], true))
            {
                return true;
            }
            return false;
        }


        ///// <summary>
        ///// 获取托盘位使能状态
        ///// </summary>
        ///// <param name="runID"></param>
        ///// <param name="posIdx"></param>
        ///// <param name="isEnable"></param>
        ///// <returns></returns>
        //public bool GetPltPosEnable(RunID runID, int posIdx, ref bool isEnable)
        //{
        //    if ((int)runID < 0 || (int)runID >= (int)RunID.RunIDEnd)
        //    {
        //        return false;
        //    }

        //    // 本地数据
        //    RunProcess run = null;
        //    if (GetInstance().GetModule(runID, ref run))
        //    {
        //        if (run is RunProTrolleyBuf)
        //        {
        //            RunProTrolleyBuf pltBuf = run as RunProTrolleyBuf;
        //            isEnable = pltBuf.IsPltBufEN(posIdx);
        //            return true;
        //        }
        //        else if (run is RunProManualOperat)
        //        {
        //            RunProManualOperat manualPlatform = run as RunProManualOperat;
        //            isEnable = manualPlatform.IsOperatEN();
        //            return true;
        //        }
        //        else if (run is RunProOnloadRobot)
        //        {
        //            RunProOnloadRobot onLoadPlt = run as RunProOnloadRobot;
        //            isEnable = onLoadPlt.IsOnloadPltEN(posIdx);
        //            return true;
        //        }
        //        else if (run is RunProOffloadRobot)
        //        {
        //            RunProOffloadRobot pltOffLoad = run as RunProOffloadRobot;
        //            isEnable = pltOffLoad.IsPltOffLoadEN(posIdx);
        //            return true;
        //        }

        //    }

        //    return false;
        //}


        #endregion


        #region // 水含量上传
        /// <summary>
        /// 水含量线程入口
        /// </summary>
        private void WCThreadProc()
        {
            while (bIsRunWCThread)
            {
                if (m_WCClient.IsConnect())
                {
                    WCUploadMonitor();
                }
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// 水含量监视
        /// </summary>
        private void WCUploadMonitor()
        {
            RunProDryingOven pDryOven = null;
            for (int nOven = 0; nOven < (int)DryingOvenCount.DryingOvenNum; nOven++)
            {
                pDryOven = GetModule(RunID.DryOven0 + nOven) as RunProDryingOven;
                if (null != pDryOven)
                {
                    for (int nFloor = 0; nFloor < (int)ModuleRowCol.DryingOvenCol; nFloor++)
                    {
                        if (WCState.WCStateUpLoad == pDryOven.GetWCUploadStatus(nFloor))
                        {
                            for (int i = 0; i < (int)ModuleRowCol.DryingOvenRow; i++)
                            {
                                if ((pDryOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nFloor + i].HasFake() &&
                                    PltType.WaitRes == pDryOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nFloor + i].Type))
                                {
                                    if (SendTestWaterRequire(pDryOven, nFloor))
                                    {
                                        pDryOven.SetWCUploadStatus(nFloor, WCState.WCStateWaitFinish);
                                        pDryOven.SaveRunData(SaveType.Variables);
                                    }
                                }
                            }
                        }
                        if (WCState.WCStateWaitFinish == pDryOven.GetWCUploadStatus(nFloor))
                        {
                            if (GetTestWaterValue(pDryOven, nFloor))
                            {
                                ClearUploadStatus(pDryOven, nFloor);
                                pDryOven.SetWCUploadStatus(nFloor, WCState.WCStateInvalid);
                                pDryOven.SaveRunData(SaveType.Variables);
                            }
                        }
                    }
                }
            }
            Thread.Sleep(1000 * 5);
        }

        /// <summary>
        /// 发送与等待
        /// </summary>
        private bool SendToDeviceAndWait(int nCmdType, ref string strCmd, ref string _strRecv)
        {
            bool bRes = false;
            string strTmp = "";
            if (nCmdType == 1)            //设置上传水含量的炉腔
            {
                strTmp = string.Format("R,{0},", nLineNum);
            }
            else if (nCmdType == 2)        //获取水含量
            {
                strTmp = string.Format("Q,{0},", nLineNum);
            }
            else if (nCmdType == 3)     //清除上传状态
            {
                strTmp = string.Format("D,{0},", nLineNum);
            }
            strCmd = strTmp + strCmd;

            if (m_WCClient.SendAndWait(strCmd, ref _strRecv))
            {
                strWCInfo = strCmd;
                bRes = true;
            }
            return bRes;
        }

        /// <summary>
        /// 发送水含量请求
        /// </summary>
        private bool SendTestWaterRequire(RunProDryingOven pOven, int nCurFlowID/* =0*/)
        {
            if (nCurFlowID < 0 || nCurFlowID > (int)ModuleRowCol.DryingOvenCol)
            {
                return false;
            }
            string strCmd = "";
            bool Res = false;
            int nMinute = 0;
            //炉子ID，炉层ID，假电池条码，左夹具条码，右夹具条码，开始干燥时间，干燥时间，水含量
            string strFBCode = "";
            strFBCode = pOven.GetWaterContentCode(nCurFlowID);

            string strATrayCode = "";
            string strBTrayCode = "";

            for (int i = 0; i < (int)ModuleRowCol.DryingOvenRow; i++)
            {
                if (pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].HasFake())
                {
                    strATrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
                else
                {
                    strBTrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
            }

            string strStartTime = pOven.GetStartTime(nCurFlowID).ToString();
            pOven.UpdateOvenData(arrCavity);
            if (arrCavity != null)
            {
                nMinute = (int)arrCavity[nCurFlowID].UnWorkTime;
            }

            strCmd = string.Format("{0},{1},{2},{3},{4},{5},{6},0,0,0,END", pOven.GetOvenID(), nCurFlowID,
                strFBCode, strATrayCode, strBTrayCode, strStartTime, nMinute);
            string strRecvData = "";
            if (SendToDeviceAndWait(1, ref strCmd, ref strRecvData))
            {
                strRecvData.Replace("\r\n", string.Empty);
                if (strRecvData == strCmd)
                {
                    Res = true;
                }
            }
            return Res;
        }

        /// <summary>
        /// 获取水含量值 
        /// </summary>
        private bool GetTestWaterValue(RunProDryingOven pOven, int nCurFlowID/* =0*/)
        {

            if (nCurFlowID < 0 || nCurFlowID > (int)ModuleRowCol.DryingOvenCol)
            {
                return false;
            }
            string strCmd = "";
            bool Res = false;
            //炉子ID，炉层ID，假电池条码，左夹具条码，右夹具条码，开始干燥时间，干燥时间，水含量
            string strFBCode = "";
            strFBCode = pOven.GetWaterContentCode(nCurFlowID);

            string strATrayCode = "";
            string strBTrayCode = "";

            for (int i = 0; i < (int)ModuleRowCol.DryingOvenRow; i++)
            {
                if (pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].HasFake())
                {
                    strATrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
                else
                {
                    strBTrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
            }

            string strStartTime = pOven.GetStartTime(nCurFlowID).ToString();
            string strBakingTime = "";

            strBakingTime = "0";

            strCmd = string.Format("{0},{1},{2},{3},{4},{5},{6},0,0,0,END", pOven.GetOvenID(), nCurFlowID,
                strFBCode, strATrayCode, strBTrayCode, strStartTime, strBakingTime);
            string strRecvData = "";
            if (SendToDeviceAndWait(2, ref strCmd, ref strRecvData))
            {
                strRecvData.Replace("\r\n", string.Empty);
                if (!string.IsNullOrEmpty(strRecvData) && strRecvData != strCmd)
                {
                    try
                    {
                        float[] fWcValue = new float[3] { 0, 0, 0 };
                        strRecvData.Replace("\r\n", string.Empty);
                        string[] strArray = strRecvData.Split(',');

                        if (strArray.Count() >= 6)
                        {
                            fWcValue[0] = (float)Convert.ToDouble(strArray[5]);         //水含量值混合型
                            fWcValue[1] = (float)Convert.ToDouble(strArray[6]);         //水含量值阳极
                            fWcValue[2] = (float)Convert.ToDouble(strArray[7]);         //水含量值阴极
                            bool bRes = false;
                            switch (MachineCtrl.GetInstance().eWaterMode)
                            {
                                case WaterMode.混合型:
                                    {
                                        bRes = fWcValue[0] > 0;
                                        break;
                                    }
                                case WaterMode.阳极:
                                    {
                                        bRes = fWcValue[1] > 0;
                                        break;
                                    }
                                case WaterMode.阴极:
                                    {
                                        bRes = fWcValue[2] > 0;
                                        break;
                                    }
                                case WaterMode.阴阳极:
                                    {
                                        bRes = (fWcValue[1] > 0 && fWcValue[2] > 0);
                                        break;
                                    }
                                //case WaterMode.阴阳隔膜级:
                                //    {
                                //        bRes = (fWcValue[1] > 0 && fWcValue[2] > 0 && fWcValue[3] > 0);
                                //        break;
                                //    }
                                default:
                                    break;
                            }
                            if (bRes)
                            {
                                pOven.SetWaterContent(nCurFlowID, fWcValue);
                                Res = true;
                            }
                        }
                    }
                    catch { }
                }
            }
            return Res;
        }

        /// <summary>
        /// 清除水含量状态
        /// </summary>
        private bool ClearUploadStatus(RunProDryingOven pOven, int nCurFlowID/* =0*/)
        {

            if (nCurFlowID < 0 || nCurFlowID > (int)ModuleRowCol.DryingOvenCol)
            {
                return false;
            }
            string strCmd = "";
            bool Res = false;
            //炉子ID，炉层ID，假电池条码，左夹具条码，右夹具条码，开始干燥时间，干燥时间，水含量
            string strFBCode = "";
            strFBCode = pOven.GetWaterContentCode(nCurFlowID);

            string strATrayCode = "";
            string strBTrayCode = "";

            for (int i = 0; i < (int)ModuleRowCol.DryingOvenRow; i++)
            {
                if (pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].HasFake())
                {
                    strATrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
                else
                {
                    strBTrayCode = pOven.Pallet[(int)ModuleRowCol.DryingOvenRow * nCurFlowID + i].Code;
                }
            }

            string strStartTime = pOven.GetStartTime(nCurFlowID).ToString();
            string strBakingTime;

            strBakingTime = "0";

            strCmd = string.Format("{0},{1},{2},{3},{4},{5},{6},0,0,0,END", pOven.GetOvenID(), nCurFlowID,
                strFBCode, strATrayCode, strBTrayCode, strStartTime, strBakingTime);
            string strRecvData = "";
            if (SendToDeviceAndWait(3, ref strCmd, ref strRecvData))
            {
                strRecvData.Replace("\r\n", string.Empty);
                if (strRecvData == strCmd)
                {
                    Res = true;
                }
            }
            return Res;
        }

        public void SaveWaterMode(WaterMode waterMode)
        {
            if (waterMode == eWaterMode)
            {
                return;
            }
            switch (waterMode)
            {
                case WaterMode.混合型:
                    {
                        eWaterMode = WaterMode.混合型;
                        break;
                    }
                case WaterMode.阳极:
                    {
                        eWaterMode = WaterMode.阳极;
                        break;
                    }
                case WaterMode.阴极:
                    {
                        eWaterMode = WaterMode.阴极;
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        eWaterMode = WaterMode.阴阳极;
                        break;
                    }
                //case WaterMode.阴阳隔膜级:
                //    {
                //        MachineCtrl.GetInstance().eWaterMode = WaterMode.阴阳隔膜级;
                //        break;
                //    }
                default:
                    break;
            }

            IniFile.WriteString("WaterMode", "eWaterMode", waterMode.ToString(), Def.GetAbsPathName(Def.MachineCfg));
        }

        #endregion


        # region // 写CSV,LOG文件

        /// <summary>
        /// 写CSV文件
        /// </summary>
        public void WriteCSV(string FilePath, string FileName, string ColHead, string FileContent)
        {
            try
            {
                if (!Directory.Exists(FilePath)) Directory.CreateDirectory(FilePath);
                FilePath = Path.Combine(FilePath, FileName);
                bool flag = File.Exists(FilePath);
                if (flag) WriteFile(FilePath, FileContent);
                else
                {
                    WriteFile(FilePath, ColHead);
                    WriteFile(FilePath, FileContent);
                }
            }
            catch { }
        }

        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="FileContent"></param>
        public void WriteFile(string FilePath, string FileContent)
        {
            lock (CsvLogLock)
            {
                FileStream fs = new FileStream(FilePath, FileMode.Append);

                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                sw.WriteLine(FileContent);
                sw.Flush();
                sw.Close();
                fs.Close();
            }

        }

        /// <summary>
        /// 写LOG文件
        /// </summary>
        public void WriteLog(string message, string FilePath = "", string FileName = "LogFile.log", int saveday = 7)
        {
            try
            {
                FilePath = string.Format("{0}\\LogFile", MachineCtrl.GetInstance().ProductionFilePath);
                if (!Directory.Exists(FilePath)) Directory.CreateDirectory(FilePath);
                string strCurTime = DateTime.Now.ToString("yyyyMMdd") + FileName;
                string sPath = Path.Combine(FilePath, strCurTime);

                WriteFile(sPath, message);

                string[] files = Directory.GetFiles(FilePath);
                for (int i = 0; i < files.Length; i++)
                {
                    DateTime curTime = DateTime.Now;
                    FileInfo fileInfo = new FileInfo(files[i]);
                    DateTime createTime = fileInfo.CreationTime;
                    if (curTime > createTime.AddDays(saveday))
                    {
                        File.Delete(files[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region //保存本地文件

        /// <summary>
        /// 保存温度数据
        /// </summary>
        /// <param name="strLog"></param>
        /// <param name="nOvenId"></param>
        /// <param name="nCurCol"></param>
        /// <param name="strStartTime"></param>
        public void SaveTempData(string strLog, int nOvenId = 0, int nCurCol = 0, string strStartTime = "")
        {
            string strColHead = "", strFilePath = "", strFilePathEx = "";
            string strFileName = DateTime.Now.ToString("yyyyMMdd") + ".CSV";

            DateTime startTime = DateTime.Now;
            if (!string.IsNullOrEmpty(strStartTime))
            {
                startTime = Convert.ToDateTime(strStartTime);
            }
            strFileName = startTime.ToString("yyyyMMdd") + "-" + nOvenId + Convert.ToString((nCurCol + 10), 16).ToUpper() + ".CSV";
            //strFilePath = string.Format("D:\\software\\生产信息\\20)实时温度保存(SaveTempData)\\{0}号炉", nOvenId);
            //strFilePathEx = string.Format("D:\\MESLog\\20)实时温度保存(SaveTempData)\\{0}号炉", nOvenId);
            strFilePath = string.Format("{0}\\20)实时温度保存(SaveTempData)\\{1}号炉", MachineCtrl.GetInstance().ProductionFilePath, nOvenId);
            strFilePathEx = string.Format("{0}\\MESLog\\20)实时温度保存(SaveTempData)\\{1}号炉", MachineCtrl.GetInstance().ProductionFilePath, nOvenId);
            strColHead = "干燥炉资源号,干燥炉编号ID,炉列1A-2B,当前时间,当前运行时间,当前真空值," +
                "托盘条码1,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," + "托盘条码2,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," + "托盘条码3,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," +
                "托盘条码4,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," + "托盘条码5,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," + "托盘条码6,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," +
                "托盘条码7,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3," + "托盘条码8,控温1, 巡检1, 控温2, 巡检2, 控温3, 巡检3,";

            lock (MesReportLock)
            {
                WriteCSV(strFilePath, strFileName, strColHead, strLog);
                WriteCSV(strFilePathEx, strFileName, strColHead, strLog);
            }
        }
        #endregion

        #region // Mes
        
        public bool MesInStationCheckIME(string PltCode)
        {
            string mesSendData = JsonConvert.SerializeObject(PltCode);
            bool bIsError = false;
            string strMsg = string.Empty;
            return false;//11
            //return UploadMesIMD(MesInterface.InStationCheckIME, mesSendData, ref bIsError, ref strMsg);
        }
        public bool MesOutStationCheckIME(string PltCode)
        {
            string mesSendData = JsonConvert.SerializeObject(PltCode);
            bool bIsError = false;
            string strMsg = string.Empty;
            return false;//11
            //return UploadMesIMD(MesInterface.InStationCheckIME, mesSendData, ref bIsError, ref strMsg);
        }
        
        public bool MesRecordFtpFilePathAndFileName(string productSn)
        {
            recordFtpFilePathAndFileName.SetValue("L3ZZCBAKG007", this.OperatorId[0], productSn, "Input.cfg", "D:\\HANS\\HANS_CompanyItem\\YC_XWD\\bin\\Debug\\System", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

            string mesSendData = JsonConvert.SerializeObject(recordFtpFilePathAndFileName);

            bool bIsError = false;
            string strMsg = string.Empty;
            //return UploadMes(MesInterface.RecordFtpFilePathAndFileName, mesSendData, ref bIsError, ref strMsg);
            return false;//11
        }

        /// <summary>
        /// 上传Mes
        /// </summary>
        /// <param name="mesInterface">MES接口枚举</param>
        /// <param name="mesData">MES发送数据</param>
        /// <returns></returns>
        private bool UploadMes(MesInterface mesInterface, string mesData, ref bool bIsError, ref string strMsg, int nOvenId = 0, bool bIsShowMsgBox = true)
        {
            if (!Enum.IsDefined(typeof(MesInterface), mesInterface) || string.IsNullOrEmpty(mesData)) return false;
            if (!this.UpdataMES) return true;
            string mesSendData = mesData;
            string mesRecvData = string.Empty;
            string Url = string.Empty;
            string MesEnglishName = MesDefine.GetMesTitle(mesInterface, 1);
            MesConfig mes = MesDefine.GetMesCfg(mesInterface);
            Url = mes.MesUrl;
            
            lock (this.lockMes)
            {
                try
                {
                    // MES界面显示
                    mes.SetMesInfo(mesSendData, "");
                    string mesUrl = Url;
                    mesRecvData = httpClient.PostRaw(mesUrl, mesSendData);
                    mes.SetMesInfo(mesSendData, mesRecvData);
                    MESResponse recvData = JsonConvert.DeserializeObject<MESResponse>(mesRecvData);
                    mes.SetAllParameter(recvData);
                    if (MesDefine.MesReturn_CodeMessger != null && MesDefine.MesReturn_CodeMessger.ContainsKey(recvData.Return_Code))
                    {
                        strMsg = MesDefine.MesReturn_CodeMessger[recvData.Return_Code];
                    }
                    else
                    {
                        strMsg = "未知的返回代码: " + recvData.Return_Code; // 或者给出其他默认错误信息
                    }

                    //strMsg = MesDefine.MesReturn_CodeMessger[recvData.Return_Code];
                    // 结果
                    if (recvData.Return_Code=="S") return true;
                    else
                    {
                        if (MesDefine.MESNG_CodeMessger != null && MesDefine.MESNG_CodeMessger.ContainsKey(recvData.Return_Code))
                        {
                            string strmmsg = "";
                            string strnMsg = MesDefine.MESNG_CodeMessger[recvData.Return_Code];
                            MesAlarmUpload(this.Equip_Code,1, strnMsg, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),ref strmmsg);
                        }
                        strMsg = (string.IsNullOrEmpty(recvData.Msg) ? MesEnglishName + "() Msg is null!" : MesDefine.GetMesTitle(mesInterface, 0) + ":\r\n" + recvData.Msg);
                        //if (bIsShowMsgBox) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    bIsError = true;
                    strMsg = mesRecvData = MesEnglishName + "() error : " + ex.Message;
                    //ShowMsgBox.ShowDialog(MesEnglishName + "() error : " + ex.Message, MessageType.MsgWarning);
                }
                finally
                {
                    SaveMesLog(mesInterface, mesSendData, mesRecvData.Replace("\r", "").Replace("\n", ""), Url, nOvenId);
                }
            }
            return false;
        }
        private bool UploadMesIMD(MesInterface mesInterface, string mesData, ref bool bIsError, ref string strMsg, int nOvenId = 0, bool bIsShowMsgBox = true)
        {
            if (!Enum.IsDefined(typeof(MesInterface), mesInterface) || string.IsNullOrEmpty(mesData)) return false;
            if (!this.UpdataMES) return true;
            //if (mesInterface != MesInterface.LoginCheck && !MesIsLogin()) return false;

            string mesSendData = "jsonData=" + mesData;
            string mesRecvData = string.Empty;
            string Url = string.Empty;
            string MesEnglishName = MesDefine.GetMesTitle(mesInterface, 1);
            MesConfig mes = MesDefine.GetMesCfg(mesInterface);
            Url = mes.MesUrl;

            lock (this.lockMes)
            {
                try
                {
                    // MES界面显示
                    mes.SetMesInfo(mesSendData, "");

                    // Post请求
                    string mesUrl = Url;
                    mesRecvData = httpClient.Post(mesUrl, mesSendData);

                    mes.SetMesInfo(mesSendData, mesRecvData);

                    MESResponse recvData = JsonConvert.DeserializeObject<MESResponse>(mesRecvData);
                    mes.SetAllParameter(recvData);

                    bool bResult = false;
                    if (mesInterface == MesInterface.LoginCheck)
                    {
                        bResult = MesDel[0](nOvenId, mesInterface, recvData);
                    }
                    else if (mesInterface == MesInterface.WIPInStationCheck)//11
                    {
                        bResult = MesDel[1](0, mesInterface, recvData);
                    }
                    else if (mesInterface >= MesInterface.WIPInStationCheck)//11
                    {
                        bResult = MesDel[2](0, mesInterface, recvData);
                    }

                    // 结果
                    if (bResult)
                    {
                        strMsg = "成功";
                        return true;
                    }
                    else
                    {
                        strMsg = (string.IsNullOrEmpty(recvData.Msg) ? MesEnglishName + "() Msg is null!" : MesDefine.GetMesTitle(mesInterface, 0) + ":/r/n  " + recvData.Msg);
                        if (bIsShowMsgBox) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    bIsError = true;
                    strMsg = mesRecvData = MesEnglishName + "() error : " + ex.Message;
                    ShowMsgBox.ShowDialog(MesEnglishName + "() error : " + ex.Message, MessageType.MsgWarning);
                }

                finally
                {
                    SaveMesLog(mesInterface, mesSendData, mesRecvData, Url, nOvenId);
                }
            }
            return false;
        }

        /// <summary>
        /// 在制品上传Mes
        /// </summary>
        private bool UploadMesWIPInStation(MesInterface mesInterface, string mesData, ref bool bIsError, ref string strMsg, ref List<DataItem> dataItem, int nOvenId = 0, bool bIsShowMsgBox = true)
        {
            if (!Enum.IsDefined(typeof(MesInterface), mesInterface) || string.IsNullOrEmpty(mesData)) return false;
            if (!this.UpdataMES) return true;
            string mesSendData = mesData;
            string mesRecvData = string.Empty;
            string Url = string.Empty;
            string MesEnglishName = MesDefine.GetMesTitle(mesInterface, 1);
            MesConfig mes = MesDefine.GetMesCfg(mesInterface);
            Url = mes.MesUrl;

            lock (this.lockMes)
            {
                try
                {
                    // MES界面显示
                    mes.SetMesInfo(mesSendData, "");
                    string mesUrl = Url;
                    mesRecvData = httpClient.PostRaw(mesUrl, mesSendData);
                    mes.SetMesInfo(mesSendData, mesRecvData);
                    WIPInStationCheckResponse recvData = JsonConvert.DeserializeObject<WIPInStationCheckResponse>(mesRecvData);
                    mes.SetAllParameter(recvData);
                    strMsg = MesDefine.MesReturn_CodeMessger[recvData.Return_Code];
                    // 结果
                    if (recvData?.Return_Code == "S")
                    {
                        foreach (var variable in recvData.Data)
                        {
                            dataItem.Add(
                                new DataItem()
                                {
                                    Wip_No = variable.Wip_No,
                                    Return_Code = variable.Return_Code,
                                    Msg = variable.Msg
                                });
                        }
                        //dataItem =recvData.Data;
                        return true;
                    }
                    else
                    {
                        strMsg = (string.IsNullOrEmpty(recvData.Msg) ? MesEnglishName + "() Msg is null!" : MesDefine.GetMesTitle(mesInterface, 0) + ":\r\n" + recvData.Msg);
                        //if (bIsShowMsgBox) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    bIsError = true;
                    strMsg = mesRecvData = MesEnglishName + "() error : " + ex.Message;
                    //ShowMsgBox.ShowDialog(MesEnglishName + "() error : " + ex.Message, MessageType.MsgWarning);
                }
                finally
                {
                    SaveMesLog(mesInterface, mesSendData, mesRecvData, Url, nOvenId);
                }
            }
            return false;
        }

        /// <summary>
        /// Mes是否登录
        /// </summary>
        public bool MesIsLogin(bool bIsAlarm = true)
        {
            for (int nOvenId = 0; nOvenId < this.OperatorId.Length; nOvenId++)
            {
                if (string.IsNullOrEmpty(this.OperatorId[nOvenId]))
                {
                    if (bIsAlarm) ShowMsgBox.ShowDialog("Mes未登录!!!", MessageType.MsgWarning);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取设备号
        /// </summary>
        private string GetDeviceSn(int nOvenId)
        {
            string sDeviceSn = this.DeviceSn;

            if (!string.IsNullOrEmpty(DeviceSn) && DeviceSn.Length > 2)
            {
                string Front = DeviceSn.Substring(0, DeviceSn.Length - 2);
                string Rear = DeviceSn.Substring(DeviceSn.Length - 2, 2);

                int nId = 0;
                if (int.TryParse(Rear, out nId) && nId >= 0)
                {
                    if (nId < (int)DryingOvenCount.DryingOvenNum && nId + nOvenId <= (int)DryingOvenCount.DryingOvenNum)
                    {
                        sDeviceSn = Front + "0" + (nId + nOvenId);
                    }
                    else
                    {
                        sDeviceSn = Front + (nId + nOvenId);
                    }
                }
            }
            return sDeviceSn;
        }

        /// <summary>
        /// 获取干燥炉资源号
        /// </summary>
        private void GetOvenResourceID()
        {
            RunProDryingOven oven = null;
            for (RunID nOvenIdx = RunID.DryOven0; nOvenIdx < RunID.RunIDEnd; nOvenIdx++)
            {
                int nOvenId = (int)nOvenIdx - (int)RunID.DryOven0;
                oven = GetModule(nOvenIdx) as RunProDryingOven;
                this.OperatorId[(int)nOvenId] = oven.GetstrResourceID();
            }
        }

        /// <summary>
        /// 获取炉子设备状态
        /// </summary>
        private bool GetOvenDeviceStatus(OvenWorkState WorkState, CavityState state, ref string StatusCode, ref string StatusDescription)
        {
            if (CavityState.Maintenance == state)
            {
                StatusCode = "9";
                StatusDescription = "故障";
                return true;
            }

            bool bResult = true;
            switch (WorkState)
            {
                case OvenWorkState.Invalid:
                    {
                        StatusCode = "2";
                        StatusDescription = "未知原因";
                        break;
                    }
                case OvenWorkState.Stop:
                    {
                        StatusCode = "18";
                        StatusDescription = "待机";
                        break;
                    }
                case OvenWorkState.Start:
                    {
                        StatusCode = "7";
                        StatusDescription = "运行";
                        break;
                    }
                default:
                    {
                        bResult = false;
                        break;
                    }
            }
            return bResult;
        }

        /// <summary>
        /// Mes登录处理结果
        /// </summary>
        private bool LoginResult(int nOvenIdx, MesInterface mesInterface, MESResponse recvData)
        {
            int nSessionId = -1;
            int.TryParse(recvData.Return_Code, out nSessionId);
            // 执行结果
            if (recvData.Return_Code == "S" && nSessionId > 0)
            {
                this.OperatorId[nOvenIdx] = nSessionId.ToString();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Mes下发参数处理结果
        /// </summary>
        private bool SpecificationsResult(int nOvenIdx, MesInterface mesInterface, MESResponse recvData)
        {
            // 执行结果
            if (recvData.Return_Code == "S")
            {
                //if (null != recvData.testResultDetails && recvData.testResultDetails.Count > 0)
                //{
                //    MesParameterData MesParData = new MesParameterData();
                //    MesParData.ParamList = new List<MesParameterDataList>();

                //    foreach (var list in recvData.testResultDetails)
                //    {
                //        if (!string.IsNullOrEmpty(list.ToString()))
                //        {
                //            MesParameterDataList data = JsonConvert.DeserializeObject<MesParameterDataList>(list.ToString());
                //            MesParData.ParamList.Add(data);
                //        }
                //    }
                //    // 保存工艺下发参数配置文件
                //    SaveMesCraftParamConfig(MesParData);

                //    // 更新炉子数据
                //    UpdatOvenParamete(this.ParData);
                //}
                return true;
            }
            else
            {
                return false;
            }
        }

        #region // Mes数据相关

        /// <summary>
        /// 更新炉子参数
        /// </summary>
        public bool UpdatOvenParamete(MesParameterData ParameData)
        {
            if (null == ParameData || null == ParameData.ParamList || ParameData.ParamList.Count == 0) return true;

            try
            {
                RunProDryingOven Oven = null;
                for (int id = (int)RunID.DryOven0; id < (int)RunID.DryOven0 + (int)DryingOvenCount.DryingOvenNum; id++)
                {
                    Oven = (RunProDryingOven)GetModule((RunID)id);
                    if (null != Oven)
                    {
                        foreach (MesParameterDataList data in ParameData.ParamList)
                        {
                            /*CompareParameter(Oven, data);*/
                        }
                        Oven.ReadParameter();
                    }
                    (PropertyInfo info, OvenParameterAttribute att)[] propListInfo = Oven.processParam.GetPropListInfo();
                    string[] unArrPage = new string[propListInfo.Length];
                    if (null != propListInfo)
                    {
                        int num = 0;
                        foreach (var (info, att) in propListInfo)
                        {
                            unArrPage[num] = info.GetValue(Oven.processParam).ToString();
                            num++;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine("UpdatOvenParamete() error:" + ex.ToString());
                return false;
            }

            return false;
        }

        /// <summary>
        /// 对比参数
        /// </summary>
        private bool CompareParameter(RunProDryingOven oven, MesParameterDataList data)
        {
            if (null == oven || string.IsNullOrEmpty(data.paramCode) || string.IsNullOrEmpty(data.paramFirstUpper) || string.IsNullOrEmpty(data.paramFirstLower))
            {
                return true;
            }

            string ValueName = string.Empty;
            float fUpperValue = 0;
            float fLowerValue = 0;
            float fValue = 0;

            //int nIdx = data.paramFirstUpper.IndexOf(".");
            //if(nIdx > 0) data.paramFirstUpper = data.paramFirstUpper.Substring(0 , nIdx);
            if (!float.TryParse(data.paramFirstUpper, out fUpperValue) || !float.TryParse(data.paramFirstLower, out fLowerValue))
            {
                ShowMsgBox.ShowDialog("Mes下发参数最大或最小值数据格式错误,请检查!!!", MessageType.MsgWarning);
                return false;
            }

            // 预热时间
            if (data.paramCode.Equals(PreHeatTime))
            {
                fValue = oven.processParam.UnPreHeatTime = (uint)(fUpperValue * 60);
                ValueName = "PreHeatTime";

                // 回炉预热时间
                //oven.BackPreTemp = oven.PreHeatTime;
                //oven.WriteParameter(oven.RunModule, "unBackPreTemp", fValue.ToString());
            }
            // 真空时间
            if (data.paramCode.Equals(VacHeatTime))
            {
                fValue = oven.processParam.UnVacHeatTime = (uint)(fUpperValue * 60);
                ValueName = "VacHeatTime";

                // 回炉真空时间
                //oven.BackVacTemp = oven.VacHeatTime;
                //oven.WriteParameter(oven.RunModule, "unBackVacTemp", fValue.ToString());
            }
            // 真空压力上限
            else if (data.paramCode.Equals(PressureUpperLimit))
            {
                fValue = oven.processParam.UnPressureUpperLimit = (uint)fUpperValue;
                ValueName = "PressureUpperLimit";
            }
            // 真空压力下限
            else if (data.paramCode.Equals(PressureLowerLimit))
            {
                fValue = oven.processParam.UnPressureLowerLimit = (uint)fLowerValue;
                ValueName = "PressureLowerLimit";
            }
            // 温度上限
            else if (data.paramCode.Equals(TempMax))
            {
                fValue = oven.processParam.UnTempUpperLimit = (uint)fUpperValue;
                ValueName = "TempUpperLimit";
            }
            // 温度下限
            else if (data.paramCode.Equals(TempMin))
            {
                fValue = oven.processParam.UnTempLowerLimit = (uint)fLowerValue;
                ValueName = "TempLowerLimit";
            }
            // 混合样水含量
            else if (data.paramCode.Equals(MingleValue))
            {
                oven.dWaterStandard[0] = fValue = fUpperValue;
                ValueName = "WaterStandard[0]";
            }
            // 正极极片水含量
            else if (data.paramCode.Equals(JustValue))
            {
                oven.dWaterStandard[1] = fValue = fUpperValue;
                ValueName = "WaterStandard[1]";
            }
            // 负极极片水含量
            else if (data.paramCode.Equals(NegativeValue))
            {
                oven.dWaterStandard[2] = fValue = fUpperValue;
                ValueName = "WaterStandard[2]";
            }

            return oven.WriteParameterCode(oven.RunModule, ValueName, fValue.ToString(""));
        }

        /// <summary>
        /// 获取参数数据
        /// </summary>
        public double GetParamValue(int nOvenIdx, int nCavityIdx, string sNnme, float[] fWaterStandard)
        {
            if (nOvenIdx < 0 || nOvenIdx > (int)DryingOvenCount.DryingOvenNum ||
                nCavityIdx < 0 || nCavityIdx >= (int)ModuleMaxPallet.DryingOven ||
                3 != fWaterStandard.Length) return 0;

            RunProDryingOven oven = GetModule(RunID.DryOven0 + nOvenIdx) as RunProDryingOven;
            CavityData cavityData = oven.bgCavityData[nCavityIdx];
            double dValue = 0;

            // 预热时间
            if (sNnme.Equals(PreHeatTime))
            {
                dValue = (double)cavityData.ProcessParam.UnPreHeatTime / 60.0;
            }
            // 真空时间
            if (sNnme.Equals(VacHeatTime))
            {
                dValue = (double)cavityData.ProcessParam.UnVacHeatTime / 60.0;
            }
            // 混合样水含量
            else if (sNnme.Equals(MingleValue))
            {
                dValue = fWaterStandard[0];
            }
            // 正极极片水含量
            else if (sNnme.Equals(JustValue))
            {
                dValue = fWaterStandard[1];
            }
            // 负极极片水含量
            else if (sNnme.Equals(NegativeValue))
            {
                dValue = fWaterStandard[2];
            }

            //////////////////////////////////////////  最大最小值  ////////////////////////////////////////////
            // 真空压力上限
            else if (sNnme.Equals(PressureUpperLimit))
            {
                dValue = oven.nMaxVacm[nCavityIdx] > cavityData.ProcessParam.UnPressureUpperLimit ? cavityData.ProcessParam.UnPressureUpperLimit : oven.nMaxVacm[nCavityIdx];
            }
            // 真空压力下限
            else if (sNnme.Equals(PressureLowerLimit))
            {
                dValue = oven.nMinVacm[nCavityIdx] < cavityData.ProcessParam.UnPressureLowerLimit ? cavityData.ProcessParam.UnPressureLowerLimit : oven.nMinVacm[nCavityIdx];
            }
            // 温度上限
            else if (sNnme.Equals(TempMax))
            {
                dValue = oven.nMaxTemp[nCavityIdx] > cavityData.ProcessParam.UnTempUpperLimit ? cavityData.ProcessParam.UnTempUpperLimit : oven.nMaxTemp[nCavityIdx];
            }
            // 温度下限
            else if (sNnme.Equals(TempMin))
            {
                dValue = oven.nMinTemp[nCavityIdx] < cavityData.ProcessParam.UnTempLowerLimit ? cavityData.ProcessParam.UnTempLowerLimit : oven.nMinTemp[nCavityIdx];
            }
            // 真空烘烤温度均值
/*            else if (sNnme.Equals(TempAvg))
            {
                if (oven.dTempAvgValue[nCavityIdx] > cavityData.ProcessParam.UnTempUpperLimit || oven.dTempAvgValue[nCavityIdx] < cavityData.ProcessParam.UnTempLowerLimit)
                {
                    dValue = cavityData.ProcessParam.UnTempLowerLimit;
                }
                else
                {
                    dValue = oven.dTempAvgValue[nCavityIdx];
                }
            }
            // 真空烘烤段真空度均值
            else if (sNnme.Equals(PressureAvg))
            {
                if (oven.dVacmAvgValue[nCavityIdx] > cavityData.ProcessParam.UnPressureUpperLimit || oven.dVacmAvgValue[nCavityIdx] < cavityData.ProcessParam.UnPressureLowerLimit)
                {
                    dValue = cavityData.ProcessParam.UnPressureUpperLimit;
                }
                else
                {
                    dValue = oven.dVacmAvgValue[nCavityIdx];
                }
            }
*/
            dValue = double.Parse(dValue.ToString("F2"));
            return dValue;
        }

        /// <summary>
        /// 获取其它参数
        /// </summary>
        public paramList GetOtherParam(int nParamIdx, float fReworkRecord, float fEnvironmental, int nOkCount, int nNgCount, int nOvenIdx, int nCavityIdx, string PltCode)
        {
            paramList param = new paramList();

            switch (nParamIdx)
            {
                case 0:
                    {
                        param.paramCode = this.Classes ?? "";
                        param.paramName = "班次";
                        param.paramValue = Def.GetClasses();
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                case 1:
                    {
                        param.paramCode = this.Totality ?? "";
                        param.paramName = "总数、不良数、良品数";
                        param.paramValue = 1 + "、" + 0 + "、" + 1;
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                case 2:
                    {
                        param.paramCode = this.ReworkRecord ?? "";
                        param.paramName = "返工记录";
                        param.paramValue = fReworkRecord.ToString();
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                case 3:
                    {
                        param.paramCode = this.Environmental ?? "";
                        param.paramName = "环境露点";
                        param.paramValue = fEnvironmental.ToString();
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                case 4:
                    {
                        param.paramCode = this.CavityNumber ?? "";
                        param.paramName = "腔体编号";
                        param.paramValue = (nOvenIdx + 1).ToString() + "-" + GetCavityMark(nCavityIdx);
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                case 5:
                    {
                        param.paramCode = this.PalletCode ?? "";
                        param.paramName = "夹具编号";
                        param.paramValue = PltCode;
                        param.paramResult = "0";
                        param.paramUnit = "";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return param;
        }

        /// <summary>
        /// 获取炉层编号
        /// </summary>
        private string GetCavityMark(int nCavityIdx)
        {
            string strCavityMark = string.Empty;

            switch (nCavityIdx)
            {
                case 0:
                    strCavityMark = "A";
                    break;
                case 1:
                    strCavityMark = "B";
                    break;
                case 2:
                    strCavityMark = "C";
                    break;
                case 3:
                    strCavityMark = "D";
                    break;
                default:
                    strCavityMark = "E";
                    break;
            }
            return strCavityMark;
        }

        /// <summary>
        /// 保存Mes数据Json日志
        /// </summary>
        public void SaveMesLog(MesInterface mesInterface, string sSendData, string sRecvData, string sUrl, int nOvenId)
        {

            if (!Enum.IsDefined(typeof(MesInterface), mesInterface) || null == sSendData || null == sRecvData || null == sUrl) return;

            try
            {
                sSendData = sSendData.Replace(",", ";");
                sRecvData = sRecvData.Replace(",", ";");

                string sFilePath = string.Empty;
                string sFileName = string.Empty;
                switch (mesInterface)
                {
                    case MesInterface.ResourcesID:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\设备资源号", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesResourcesID.CSV";
                            break;
                        }
                    case MesInterface.HeartBeat:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP001设备在线检测", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesHeartBeat.CSV"; 
                            break;
                        }
                    case MesInterface.StateAndStopReasonUpload:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP002设备状态_停机原因上传", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesStateAndStopReasonUpload.CSV"; 
                            break;
                        }
                    case MesInterface.AlarmUpload:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP003设备报警上传", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + string.Format("MesAlarmUpload.CSV", nOvenId + 1); 
                            break;
                        }
                    case MesInterface.ProcessDataUpload:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP004设备过程参数上传", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesProcessDataUpload.CSV"; 
                            break;
                        }
                    case MesInterface.LoginCheck:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP021操作员登录校验", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesLoginCheck.CSV"; 
                            break;
                        }
                    case MesInterface.BakeDataUpload:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\烘箱数据采集", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesBakeDataUpload.CSV"; 
                            break;
                        }
                    case MesInterface.EnergyUpload:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\能源数据", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "MesUploadEnergyData.CSV"; 
                            break;
                        }
                    case MesInterface.ResultDataUploadAssembly:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP042产品结果数据上传", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "ResultDataUploadAssembly.CSV"; 
                            break;
                        }
                    case MesInterface.WIPInStationCheck:
                        {
                            sFilePath = string.Format("{0}\\MES信息\\EIP024在制品进站校验接口", this.ProductionFilePath);
                            sFileName = DateTime.Now.ToString("yyyyMMdd") + "WIPInStationCheck.CSV";
                            break;
                        }
                        //case MesInterface.FittingCheckForTary:
                        //    {
                        //        sFilePath = string.Format("{0}\\MesFittingCheckForTary", this.ProductionFilePath);
                        //        sFileName = DateTime.Now.ToString("yyyyMMdd") + "托盘检查.CSV";
                        //        break;
                        //    }
                        //case MesInterface.FittingCheckForCell:
                        //    {
                        //        sFilePath = string.Format("{0}\\MES信息\\MesFittingCheckForCell", this.ProductionFilePath);
                        //        sFileName = DateTime.Now.ToString("yyyyMMdd") + "电芯检查.CSV";
                        //        break;
                        //    }
                        //case MesInterface.FittingBinding:
                        //    {
                        //        sFilePath = string.Format("{0}\\MES信息\\MesFittingBinding", this.ProductionFilePath);
                        //        sFileName = DateTime.Now.ToString("yyyyMMdd") + "托盘电芯绑定.CSV";
                        //        break;
                        //    }
                        //case MesInterface.FittingUnBinding:
                        //    {
                        //        sFilePath = string.Format("{0}\\MES信息\\MesFittingUnBinding", this.ProductionFilePath);
                        //        sFileName = DateTime.Now.ToString("yyyyMMdd") + "托盘电芯解绑.CSV";
                        //        break;
                        //    }
                }
                string sColHead = "触发时间,接口名称,发送数据,返回数据";
                string sContent = DateTime.Now.ToString("HH:mm:ss") + "," + sUrl + "," + sSendData + "," + sRecvData;

                lock (MesReportLock)
                {
                    WriteCSV(sFilePath, sFileName, sColHead, sContent);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("SaveMesLog() error:" + ex.Message);
            }
        }

        /// <summary>
        /// 保存进站数据
        /// </summary>
        public void SaveInStation(string batteryCode, bool bStatus, string strMsg)
        {
            try
            {
                string sStatus = UpdataMES ? bStatus.ToString() : "未打开上传Mes使能";
                batteryCode = string.IsNullOrEmpty(batteryCode) ? "" : batteryCode;
                string sFilePath = string.Format("{0}\\产品进站数据", this.ProductionFilePath);
                string sFileName = DateTime.Now.ToString("yyyyMMdd") + "产品进站数据.CSV";
                string sColHead = "进站时间,电池条码,Mes执行结果,Mes返回信息";
                string sContent = DateTime.Now.ToString("HH:mm:ss") + "," + batteryCode + "," + sStatus + "," + strMsg;
                string strInfo = sContent.Replace("\r"," ");
                lock (MesReportLock)
                {
                    WriteCSV(sFilePath, sFileName, sColHead, strInfo);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("SaveInStation() error:" + ex.Message);
            }
        }

        /// <summary>
        /// 保存产品出站数据
        /// </summary>
        public bool SaveProductData(int nOvenIdx, int nCavityIdx, string batteryCode, List<paramList> paramList, List<StepData> stepList, bool bStatus, ref bool bIsOne)
        {
            try
            {
                batteryCode = batteryCode ?? "";
                string sFilePath = string.Format("{0}\\产品出站数据", this.ProductionFilePath);
                string sFileName = DateTime.Now.ToString("yyyyMMdd") + "产品出站数据.CSV";
                string sColHead = "日期,电池条码,炉号,炉层";
                string sContent = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + batteryCode + "," + (nOvenIdx + 1) + "," + (nCavityIdx + 1);
                string sCode = sContent;

                // 参数
                foreach (var param in paramList)
                {
                    sColHead += "," + param.paramName ?? "";
                    sContent += "," + param.paramValue ?? "";
                }

                // 工步数据
                for (int nIdx = 0; nIdx < stepList.Count; nIdx++)
                {
                    int nStepIdx = nIdx + 1;
                    sColHead += string.Format(",工步号{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].step;
                    sColHead += string.Format(",托盘号{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].trayNo;
                    sColHead += string.Format(",公布名称{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].stepName;
                    sColHead += string.Format(",工序代码{0}", nStepIdx);
                    sContent += "," + this.GroupCode ?? "";
                    sColHead += string.Format(",开始时间{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].startDate;
                    sColHead += string.Format(",结束时间{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].endDate;
                    sColHead += string.Format(",循环号{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].circulatingNumber;
                    sColHead += string.Format(",工步时长{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].turnTime;
                    sColHead += string.Format(",结束温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].endTemperature;
                    sColHead += string.Format(",平均温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].avgvol;
                    sColHead += string.Format(",库位最高温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].maxHousetemp;
                    sColHead += string.Format(",库位最底温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].minHousetemp;
                    sColHead += string.Format(",前探头库位开始温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].startFirstHousetemp;
                    sColHead += string.Format(",前探头库位结束温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].endFirstHousetemp;
                    sColHead += string.Format(",后探头库位开始温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].startAfterHousetemp;
                    sColHead += string.Format(",后探头库位结束温度{0}", nStepIdx);
                    sContent += "," + stepList[nIdx].endAfterHousetemp;
                }

                sColHead += ",Mes执行结果";
                sContent += "," + (!updataMES ? "未打开上传Mes使能" : bStatus.ToString());

                if (!Directory.Exists(sFilePath)) Directory.CreateDirectory(sFilePath);
                sFilePath = Path.Combine(sFilePath, sFileName);
                bool flag = File.Exists(sFilePath);
                if (flag)
                {
                    WriteFile(sFilePath, bIsOne ? sCode : sContent);
                    bIsOne = true;
                }
                else
                {
                    WriteFile(sFilePath, sColHead);
                    WriteFile(sFilePath, sContent);
                    bIsOne = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("SaveProductData() error:" + ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 保存MES工艺下发参数
        /// </summary>
        public void SaveMesCraftParamConfig(MesParameterData data)
        {
            if (null == data || null == data.ParamList) return;
            bool bResult = false;

            try
            {
                // 对比数据(相同参数不用保存)
                if (this.ParData.ParamList.Count == data.ParamList.Count)
                {
                    for (int nIdx = 0; nIdx < data.ParamList.Count; nIdx++)
                    {
                        if (this.ParData.ParamList[nIdx].paramCode != data.ParamList[nIdx].paramCode || this.ParData.ParamList[nIdx].paramName != data.ParamList[nIdx].paramName ||
                            this.ParData.ParamList[nIdx].paramFirstUpper != data.ParamList[nIdx].paramFirstUpper || this.ParData.ParamList[nIdx].paramFirstLower != data.ParamList[nIdx].paramFirstLower ||
                            this.ParData.ParamList[nIdx].paramUnit != data.ParamList[nIdx].paramUnit || this.ParData.ParamList[nIdx].paramReTestUpper != data.ParamList[nIdx].paramReTestUpper ||
                            this.ParData.ParamList[nIdx].paramReTestLower != data.ParamList[nIdx].paramReTestLower)
                        {
                            bResult = true;
                            break;
                        }
                    }
                }
                else
                {
                    bResult = true;
                }

                if (bResult)
                {
                    this.ParData = data;
                    string sParamData = JsonConvert.SerializeObject(data);
                    if (!string.IsNullOrEmpty(sParamData)) IniFile.WriteString("MesCraftParameter", "MesCraftParam", sParamData, Def.GetAbsPathName(Def.MesCraftParameterCFG));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("SaveMesCraftParamConfig() error:" + ex.Message);
            }
        }

        /// <summary>
        /// 读取MES工艺下发参数
        /// </summary>
        public void ReadMesCraftParamConfig()
        {
            try
            {
                string sMesCraftPara = IniFile.ReadString("MesCraftParameter", "MesCraftParam", "", Def.GetAbsPathName(Def.MesCraftParameterCFG));

                MesParameterData data = JsonConvert.DeserializeObject<MesParameterData>(sMesCraftPara);
                if (null != data && null != data.ParamList && data.ParamList.Count > 0)
                {
                    this.ParData = data;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ReadMesParamConfig() error:" + ex.Message);
            }
        }

        #endregion

        #endregion

        #region MESInterface

        /// <summary>
        /// 手动调用MES
        /// </summary>
        /// <param name="mesInterface">MES接口</param>
        /// <returns></returns>
        public bool MESInvoke(MesInterface mesInterface)
        {
            if (Def.IsNoHardware()) return true;
            if (!UpdataMES)
            {
                ShowMsgBox.ShowDialog("MES上传使能未打开!!!", MessageType.MsgWarning);
                return false;
            }
            string strMsg = "";
            MesConfig mesCfg = MesDefine.GetMesCfg(mesInterface);
            Dictionary<string, string> mesPara = mesCfg.Parameter.ToDictionary(p => p.Key, p => p.Value);

            switch (mesInterface)
            {
                case MesInterface.HeartBeat://设备在线
                    {
                        if (!MesHeartbeat(this.Equip_Code)) 
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            return false;
                        }
                        ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                        return true;
                    }
                case MesInterface.StateAndStopReasonUpload:// 设备状态_停机原因上传
                    {
                        try
                        {
                            string Status = mesPara["设备状态"];
                            string REASON_CODE = mesPara["停机原因"];
                            if (!MesStateAndStopReasonUpload(this.Equip_Code, Status, REASON_CODE, ref strMsg))
                            {
                                ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                return false;
                            }
                            ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                            return true;
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                    }
                case MesInterface.AlarmUpload:// 设备报警上传
                    {
                        try
                        {
                            string strOvenIdx = mesPara["炉号"];
                            string ALARM_VALUE = mesPara["报警状态"];
                            string ALARM_CODE = mesPara["报警代码"];
                            if (!MesAlarmUpload(this.OperatorId[(int.Parse(strOvenIdx) - 1)], int.Parse(ALARM_VALUE), ALARM_CODE, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ref strMsg)) 
                            {
                                ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                return false;
                            }
                            ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                            return true;
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                    }
                case MesInterface.ProcessDataUpload:// 设备过程参数上传 FTPUploadFileData
                    {
                        try
                        {
                            string strOvenIdx = mesPara["炉号"];
                            string strCavityIdx = mesPara["腔体号"];
                            RunID nOvenIdx = (RunID)(int.Parse(strOvenIdx) + 13);
                            if ((nOvenIdx < RunID.DryOven0) || (nOvenIdx > RunID.DryOven5))
                            {
                                ShowMsgBox.ShowDialog("上传失败！\r\n 炉号参数错误！", MessageType.MsgMessage);
                                break;
                            }
                            RunProDryingOven oven = GetModule(nOvenIdx) as RunProDryingOven;
                            // 炉子是否连接
                            if (oven.OvenIsConnect())
                            {
                                int rOvenIdx = (int.Parse(strOvenIdx) - 1);
                                int nCavityIdx = (int.Parse(strCavityIdx) - 1);
                                if (!oven.FTPUploadFileData(nCavityIdx, nCavityIdx, rOvenIdx)) 
                                {
                                    ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                    return false;
                                }
                                ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                                return true;
                            }
                            ShowMsgBox.ShowDialog(strOvenIdx+"炉子未连接", MessageType.MsgMessage);
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                        break;
                    }
                case MesInterface.LoginCheck://操作员登录校验接口 EIP021
                    {
                        if (!MesLoginCheck(this.Equip_Code))
                        {
                            IsMESConnect = false;
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            return false;
                        }
                        IsMESConnect = true;
                        ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                        return true;
                    }
                case MesInterface.WIPInStationCheck:// 在制品进站校验接口;
                    {
                        try
                        {
                            string strBatteryCode = mesPara["电芯条码"];
                            List<DataItem> dataItem = new List<DataItem>();
                            if (!MesWIPInStationCheck(strBatteryCode.Replace("\r", "").Replace("\n", ""), ref strMsg, ref dataItem)) 
                            {
                                ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                return false;
                            }
                            ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                            return true;
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                    }
                    break;
                case MesInterface.ResultDataUploadAssembly:// 产品结果数据上传接口-装配化成段
                    {
                        try
                        {
                            string strOvenIdx = mesPara["炉号"];
                            string strCavityIdx = mesPara["腔体号"];
                            string strBatteryCode = mesPara["电芯条码"];
                            string strWaterValue0 = mesPara["水含量混合型值"];
                            string strWaterValue1 = mesPara["水含量阳极值"];
                            string strWaterValue2 = mesPara["水含量阴极值"];
                            RunID nOvenIdx = (RunID)(int.Parse(strOvenIdx) + 13);
                            if ((nOvenIdx < RunID.DryOven0) || (nOvenIdx > RunID.DryOven5))
                            {
                                ShowMsgBox.ShowDialog("上传失败！\r\n 炉号参数错误！", MessageType.MsgMessage);
                                break;
                            }
                            RunProDryingOven oven = GetModule(nOvenIdx) as RunProDryingOven;
                            // 炉子是否连接
                            if (oven.OvenIsConnect())
                            {
                                float[,] fWaterContentValue = new float[2, 5];            // 水含量值[层][阴阳]
                                int nCurOperatCol = (int.Parse(strCavityIdx) - 1);					    // 当前操作列
                                fWaterContentValue[nCurOperatCol, 0] = float.Parse(strWaterValue0);
                                fWaterContentValue[nCurOperatCol, 1] = float.Parse(strWaterValue1);
                                fWaterContentValue[nCurOperatCol, 2] = float.Parse(strWaterValue1);
                                fWaterContentValue[nCurOperatCol, 4] = float.Parse(strWaterValue2);
                                if (!oven.MesUploadOvenFinishData(oven.GetOvenID(), fWaterContentValue, nCurOperatCol, ref strMsg, strBatteryCode)) 
                                {
                                    string strErr = strBatteryCode + "上传失败! \r\n" + strMsg;
                                    ShowMsgBox.ShowDialog(strErr, MessageType.MsgMessage);
                                    return false;
                                }
                                ShowMsgBox.ShowDialog(strBatteryCode + "上传成功！", MessageType.MsgMessage);
                                return true;
                            }
                            ShowMsgBox.ShowDialog(strOvenIdx + "炉子未连接", MessageType.MsgWarning);
                        }
                        catch (Exception ex)
                        {
                            string strErr = "上传失败！\r\n" + ex.ToString();
                            ShowMsgBox.ShowDialog(strErr, MessageType.MsgMessage);
                            break;
                        }
                    }
                    break;
                case MesInterface.BakeDataUpload:// 烘箱数据采集
                    {
                        try
                        {
                            string strOvenIdx = mesPara["炉号"];
                            string strCavityIdx = mesPara["腔体号"];
                            string strOvenStatus = mesPara["炉腔状态"];
                            RunID nOvenIdx = (RunID)(int.Parse(strOvenIdx) + 13);
                            if ((nOvenIdx < RunID.DryOven0) || (nOvenIdx > RunID.DryOven5))
                            {
                                break;
                            }
                            RunProDryingOven oven = GetModule(nOvenIdx) as RunProDryingOven;
                            // 炉子是否连接
                            if (oven.OvenIsConnect())
                            {
                                int nCurOperatCol = (int.Parse(strCavityIdx) - 1);					    // 当前操作列
                                string Status = "0";// 状态（0：进烘箱；1：出烘箱）
                                if (!oven.MesOvenStartAndEndData(nCurOperatCol, oven.GetOvenID(), Status, ref strMsg)) 
                                {
                                    ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                    return false;
                                }
                                ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                                return true;
                            }
                            ShowMsgBox.ShowDialog(strOvenIdx + "炉子未连接", MessageType.MsgWarning);
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                    }
                    break;
                case MesInterface.EnergyUpload:// 能源数据上传
                    {
                        try
                        {
                            string strOvenIdx = mesPara["炉号"];
                            RunID nOvenIdx = (RunID)(int.Parse(strOvenIdx) + 13);
                            if ((nOvenIdx< RunID.DryOven0) || (nOvenIdx > RunID.DryOven5))
                            {
                                break;
                            }
                            RunProDryingOven oven = GetModule(nOvenIdx) as RunProDryingOven;
                            // 炉子是否连接
                            if (oven.OvenIsConnect())
                            {
                                if (!MesEnergyUpload(oven.GetOvenID(), (float)oven.bgCavityData[0].fEnergySum, (float)oven.bgCavityData[0].unOneDayEnergy, (float)(oven.bgCavityData[0].unBatAverEnergy)))
                                {
                                    ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                                    break;
                                }
                                ShowMsgBox.ShowDialog("上传成功！", MessageType.MsgMessage);
                                break;
                            }
                            ShowMsgBox.ShowDialog(strOvenIdx + "炉子未连接", MessageType.MsgWarning);
                        }
                        catch (Exception e)
                        {
                            ShowMsgBox.ShowDialog("上传失败！", MessageType.MsgMessage);
                            break;
                        }
                        break;
                    }
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// 操作员登录校验接口 EIP021
        /// </summary>
        /// <param name="loginCheck">登录校验接口</param>
        /// <param name="strErr">反馈信息</param>
        /// <returns></returns>
        public bool MesLoginCheck(string strResourceID)
        {
            bool bIsError = false;
            string strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            LoginCheck.SetValue(this.EquipPC_ID, this.EquipPC_Password, strResourceID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.MesUserName, this.MesPassword);
            string mesSendData = JsonConvert.SerializeObject(LoginCheck);
            bool result = UploadMes(MesInterface.LoginCheck, mesSendData, ref bIsError, ref strMsg, 0);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
            
        }

        /// <summary>
        /// 设备在线接口EIP001
        /// </summary>
        public bool MesHeartbeat(string strResourceID)
        {
            bool bIsError = false;
            string strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            if(strResourceID==null) return true;
            ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, err) => true;
            HeartBeat.SetValue(this.EquipPC_ID, this.EquipPC_Password, strResourceID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), true);
            string mesSendData = JsonConvert.SerializeObject(HeartBeat);
            if (UploadMes(MesInterface.HeartBeat, mesSendData, ref bIsError, ref strMsg, 0))
            {
                IsMESConnect = true;
                return true;
            }
            IsMESConnect = false;
            ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return false;
        }

        /// <summary>
        /// 设备状态+停机原因上传EIP002
        /// </summary>
        public bool MesStateAndStopReasonUpload(string strResourceID,string Status, string REASON_CODE, ref string strMsg)
        {
            bool bIsError = false;
            strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            //if(IsMESConnect) return true;
            //int Status = 0;// 0：离线 1：待机 2：自动运行 3：手动运行 4：报警/故障 5：停机 6：维护
            string START_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");//开始时间
            /// 停机原因代码
            /// 0. 短停机；1. 待料；2. 吃饭；3. 换型；4. 设备故障；5. 来料不良；6. 设备校验；
            /// 7. 首件/点检；8. 品质异常；9. 堆料；10. 环境异常；11. 设定信息不完善；12. 其他
            //int REASON_CODE = 0;
            if (strResourceID == null) strResourceID = this.Equip_Code;
            StateAndStopReasonUpload.SetValue(this.EquipPC_ID, this.EquipPC_Password, strResourceID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), Status, START_TIME, REASON_CODE);
            string mesSendData = JsonConvert.SerializeObject(StateAndStopReasonUpload);
            bool result = UploadMes(MesInterface.StateAndStopReasonUpload, mesSendData, ref bIsError, ref strMsg);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
        }

        /// <summary>
        /// 设备报警上传接口EIP003
        /// </summary>
        public bool MesAlarmUpload(string strResourceID, int ALARM_VALUE, string ALARM_CODE, string START_TIME, ref string strMsg)
        {
            if (!this.UpdataMES) return true;
            //if (IsMESConnect) return true;
            if (string.IsNullOrEmpty(ALARM_CODE)) return false;
            //int ALARM_VALUE = 0;//报警状态
            //string ALARM_CODE = "A002";// 报警代码
            //string START_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");// 报警开始时间
            AlarmUpload.SetValue(this.EquipPC_ID, this.EquipPC_Password, strResourceID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ALARM_VALUE, ALARM_CODE, START_TIME);
            string mesSendData = JsonConvert.SerializeObject(AlarmUpload);
            bool bIsError = false;
            bool result = UploadMes(MesInterface.AlarmUpload, mesSendData, ref bIsError, ref strMsg);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
        }

        /// <summary>
        /// 设备过程参数EIP004
        /// </summary>
        public bool MesProcessDataUpload(int nOvenId, List<paramListItem> paramList, ref string strMsg)
        {
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            //if (IsMESConnect) return true;
            if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum  || null == paramList) return false;
            bool bIsError = false;
            
            ProcessDataUpload.SetValue(this.EquipPC_ID, this.EquipPC_Password, this.OperatorId[nOvenId], DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), paramList);
            string mesSendData = JsonConvert.SerializeObject(ProcessDataUpload);
            bool result = UploadMes(MesInterface.ProcessDataUpload, mesSendData, ref bIsError, ref strMsg, nOvenId);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
        }

        /// <summary>
        /// 在制品进站校验接口EIP023
        /// </summary>
        public bool MesWIPInStationCheck(string WIP_NO, ref string strMsg, ref List<DataItem> dataItem)
        {
            string nMsg = string.Empty;
            bool bIsError = false;
            strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            List<WIPList>  WIPItemData = new List<WIPList>();
            WIPItemData.Add(new WIPList() { WIP_NO = WIP_NO });
            WIPInStationCheck.SetValue(this.EquipPC_ID, this.EquipPC_Password, this.Equip_Code, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), WIPItemData);
            string mesSendData = JsonConvert.SerializeObject(WIPInStationCheck);
            bool result = UploadMesWIPInStation(MesInterface.WIPInStationCheck, mesSendData, ref bIsError, ref strMsg, ref dataItem);
            if (!result)
            {
                foreach (var variable in dataItem)
                {
                    nMsg += variable.Msg + "\r\n";
                    strMsg += variable.Msg + "  ";
                }
                //ShowMsgBox.ShowDialog(strMsg + "\r\n" + nMsg + "条数据上传失败！", MessageType.MsgWarning);
            }
            return result;
        }

        /// <summary>
        /// 产品结果数据上传接口-装配化成段EIP042
        /// </summary>
        public bool MesResultDataUploadAssembly(string strResourceID, int nCol, Pallet[] pallets, List<OUT_LISTItem> oUT_LISTItems, ref string strErr, bool bIstest = false)
        {
            bool bIsError = false;
            string strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            //if (IsMESConnect) return true;
            string strMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string strMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            if (nCol == 0)
            {
                strMESTime_Start = pallets[7].StartTime;
                strMESTime_End = pallets[7].EndTime;
            }
            else if (nCol == 1)
            {
                strMESTime_Start = pallets[15].StartTime;
                strMESTime_End = pallets[15].EndTime;
            }
            if (bIstest)
            {
                strMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                strMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
            string MANUFACTURE_CODE = this.ManufactureCode; //工单号
            string OPRATOR = this.Operator;// 操作员
            ResultDataUploadAssembly.SetValue(this.EquipPC_ID, this.EquipPC_Password, strResourceID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), strMESTime_Start, strMESTime_End, MANUFACTURE_CODE, OPRATOR, oUT_LISTItems);
            string mesSendData = JsonConvert.SerializeObject(ResultDataUploadAssembly);
            bool result = UploadMes(MesInterface.ResultDataUploadAssembly, mesSendData, ref bIsError, ref strMsg);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
        }

        /// <summary>
        /// 能源数据上传
        /// </summary>
        public bool MesEnergyUpload(int nOvenId, float fEnergySum, float unOneDayEnergy, float unBatAverEnergy)
        {
            if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum)
            {
                return true;
            }
            if (!this.UpdataMES) return true;
            //if (IsMESConnect) return true;
            List<Energy> energyData = new List<Energy>();
            Energy energy = new Energy
            {
                Param_Code = "总电量KW/H",
                Param_Value = fEnergySum.ToString("F2"),
            };
            energyData.Add(energy);

            energy = new Energy
            {
                Param_Code = "单日耗电量KW/H",
                Param_Value = unOneDayEnergy.ToString("F2"),
            };
            energyData.Add(energy);

            energy = new Energy
            {
                Param_Code = "电芯平均能耗KW/H",
                Param_Value = unBatAverEnergy.ToString("F2"),
            };
            energyData.Add(energy);

            EnergyUpload.SetValue(this.EquipPC_ID, this.EquipPC_Password, this.OperatorId[nOvenId], DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), energyData);
            string mesSendData = JsonConvert.SerializeObject(EnergyUpload);
            bool bIsError = false;
            string strMsg = string.Empty;
            bool result = UploadMes(MesInterface.EnergyUpload, mesSendData, ref bIsError, ref strMsg);
            if (!result) ShowMsgBox.ShowDialog(strMsg, MessageType.MsgWarning);
            return result;
        }

        /// <summary>
        /// 烘烤数据采集
        /// </summary>
        public bool MesBakeDataUpload(string sType, ref string strMsg, List<Battery_CodeList> bats, int nOvenId, int PltNum, Pallet[] pallet, bool bIstest = false)
        {
            bool bIsError = false;
            strMsg = string.Empty;
            if (!UpdataMES)
            {
                strMsg = "MES使能未打开！";
                return true;
            }
            //if (IsMESConnect) return true;
            if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum) return false;

            // 托盘有电芯(防止全为填充电芯)
            if (!pallet[PltNum].HasTypeBat(BatType.OK) && !pallet[PltNum].HasTypeBat(BatType.Fake) && !bIstest) return true;
            string Status = sType;// 状态（0：进烘箱；1：出烘箱）
            string Tray_Code = pallet[PltNum].Code;// 托盘码
            string Box_No = nOvenId.ToString();// 烘箱号
            string Access_Box_Time = Status == "0" ? pallet[PltNum].StartTime : pallet[PltNum].EndTime;
            if (bIstest || Access_Box_Time=="")//手动测试用
            {
                Access_Box_Time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
            //static List<Battery_CodeList> GetWipList(List<string> bats)
            //{
            //    List<Battery_CodeList> wipList = new List<Battery_CodeList>();
            //    for (int i = 0; i < bats.Count; i++)
            //    {
            //        wipList.Add(new Battery_CodeList { Cell_Code = bats[i], position = (i + 1).ToString() });
            //    }
            //    return wipList;
            //}
            BakeDataUpload.SetValue(this.EquipPC_ID, this.EquipPC_Password, this.OperatorId[nOvenId], DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), Status, Tray_Code.Replace("\r", "").Replace("\n", ""), Box_No, Access_Box_Time, bats);
            string mesSendData = JsonConvert.SerializeObject(BakeDataUpload);
            return UploadMes(MesInterface.BakeDataUpload, mesSendData, ref bIsError, ref strMsg);
        }
        /*烘烤数据采集
        /// <summary>
        /// 9.托盘检查
        /// </summary>
       public bool MesFittingCheckForTary(string PltCode)
       {
           if (!updatavBindingMES) return true;
       
           fittingCheckForTary.SetValue(this.OperatorId[0], PltCode, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
           string mesSendData = JsonConvert.SerializeObject(fittingCheckForTary);
       
           bool bIsError = false;
           string strMsg = string.Empty;
           return UploadMes(MesInterface.FittingCheckForTary, mesSendData, ref bIsError, ref strMsg);
       }

       // <summary>
       // 10.电芯检查
       // </summary>
        public bool MesFittingCheckForCell(int nOvenId, Battery bat)
        {
            //是否更新假电池绑定
            if (!updataFakeBindingMES && bat.Type == BatType.Fake) return true;
       
            fittingCheckForCell.SetValue(this.OperatorId[nOvenId], bat.Code, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            string mesSendData = JsonConvert.SerializeObject(fittingCheckForCell);
       
            bool bIsError = false;
            string strMsg = string.Empty;
            return UploadMes(MesInterface.FittingCheckForCell, mesSendData, ref bIsError, ref strMsg);
        }
        /// <summary>
        /// 11.托盘电芯绑定
        /// </summary>
        public bool MesFittingBinding(int nOvenId, Pallet pallet)
        {
            if (!updatavBindingMES) return true;
       
            if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum) return false;
       
            // 托盘有电芯(防止全为填充电芯)
            if (!pallet.HasTypeBat(BatType.OK) && !pallet.HasTypeBat(BatType.Fake)) return true;
       
            int nMaxRow = 0;
            int nMaxCol = 0;
            this.GetPltRowCol(ref nMaxRow, ref nMaxCol);
            List<BatList> bats = new List<BatList>();
       
            for (int nRow = 0; nRow < nMaxRow; nRow++)
            {
                for (int nCol = 0; nCol < nMaxCol; nCol++)
                {
                    //是否更新假电池绑定
                    if (!updataFakeBindingMES && pallet.Bat[nRow, nCol].Type == BatType.Fake) continue;
       
                    if ((pallet.Bat[nRow, nCol].Type != BatType.OK && pallet.Bat[nRow, nCol].Type != BatType.Fake) || pallet.Bat[nRow, nCol].Code.Length <= 10) continue;
                    int nBind = nRow * nMaxCol + nCol;
                    string sBatCode = pallet.Bat[nRow, nCol].Code;
       
                    bats.Add(new BatList() { productSn = sBatCode, position = nBind.ToString(), remark = "" });
                }
            }
       
            fittingBinding.SetValue(this.OperatorId[nOvenId], pallet.Code, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), bats);
            string mesSendData = JsonConvert.SerializeObject(fittingBinding);
       
            bool bIsError = false;
            string strMsg = string.Empty;
            return UploadMes(MesInterface.FittingBinding, mesSendData, ref bIsError, ref strMsg);
        }

       // <summary>
       // 12.托盘电芯解绑(整盘)
       // </summary>
       public bool MesFittingUnBinding(int nOvenId, Pallet pallet)
       {
           if (!updatavBindingMES) return true;
       
           if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum || string.IsNullOrEmpty(pallet.Code)) return true;
       
           // 托盘有电芯(防止全为填充电芯--清尾料电芯)
           if (!pallet.HasTypeBat(BatType.OK) && (!updataFakeBindingMES && pallet.HasTypeBat(BatType.Fake))) return true;
       
           fittingUnBinding.SetValue(this.GroupCode, this.OperatorId[nOvenId], "", pallet.Code, "0", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
           string mesSendData = JsonConvert.SerializeObject(fittingUnBinding);
       
           bool bIsError = false;
           string strMsg = string.Empty;
           return UploadMes(MesInterface.FittingUnBinding, mesSendData, ref bIsError, ref strMsg);
       }
         */

        /// <summary>
        /// 托盘电芯绑定
        /// </summary>
        public bool FittingBinding(int nOvenId, Pallet pallet,int Row,int Col)
        {
            if (nOvenId < 0 || nOvenId >= (int)DryingOvenCount.DryingOvenNum) return false;
       
            // 托盘有电芯(防止全为填充电芯)
            if (!pallet.HasTypeBat(BatType.OK) && !pallet.HasTypeBat(BatType.Fake)) return true;
       
            int nMaxRow = 0;
            int nMaxCol = 0;
            this.GetPltRowCol(ref nMaxRow, ref nMaxCol);
            string strFilePath = string.Format("{0}\\托盘绑定\\{1}号炉", MachineCtrl.GetInstance().ProductionFilePath, nOvenId + 1);
            string strFileName = DateTime.Now.ToString("yyyy-MM-dd") + "-" + "托盘绑定" + ".CSV";
            string strColHead = "时间,托盘码,炉腔行,炉腔列,电芯码,电芯位置,电芯类型";
            string strLog = string.Empty;
            List<string> bats = new List<string>();
            for (int nRow = 0; nRow < nMaxRow; nRow++)
            {
                for (int nCol = 0; nCol < nMaxCol; nCol++)
                {
                    //if ((pallet.Bat[nRow, nCol].Type != BatType.OK && pallet.Bat[nRow, nCol].Type != BatType.Fake) || pallet.Bat[nRow, nCol].Code.Length <= 10) continue;
                    string sBatType = pallet.Bat[nRow, nCol].Type==BatType.OK ? "正常电芯" : "假电芯";
                    int nBind = nRow * nMaxCol + nCol + 1;
                    string sBatCode = pallet.Bat[nRow, nCol].Code.Replace("\r", "").Replace("\n", "");
                    strLog+=string.Format("{0},{1},{2},{3},{4},{5},{6}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), pallet.Code.Replace("\r", "").Replace("\n", ""), Row + 1, Col + 1, sBatCode, nBind, sBatType);
                    bats.Add(sBatCode);
                }
            }
            strLog = strLog.TrimEnd();  // 移除末尾的换行符和空白字符
            WriteCSV(strFilePath, strFileName, strColHead, strLog);
            return true;
        }
        #endregion
    }
}
#endregion
