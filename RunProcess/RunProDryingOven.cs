using CommunityToolkit.Mvvm.ComponentModel;
using HelperLibrary;
using ImTools;
using Microsoft.VisualBasic;
using Prism.Ioc;
using Prism.Services.Dialogs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SystemControlLibrary;
using WPFMachine;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;
using WPFMachine.Frame.ReadWriteData;
using WPFMachine.Frame.RealTimeTemperature;
using WPFMachine.Frame.Userlib;
using WPFMachine.ViewModels;
using WPFMachine.Views.Control;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    class RunProDryingOven : RunProcess
    {
        #region // 枚举定义

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_ConnectOven,
            Init_CheckDoorClose,
            Init_CloseOvenDoor,
            Init_DoorCloseFinished,
            Init_CheckDoorOpen,
            Init_OpenOvenDoor,
            Init_DoorOpenFinished,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_PreCloseOvenDoor,
            Auto_PreBreakVacuum,
            Auto_OpenOvenDoor,
            Auto_PreCheckPltState,
            Auto_WaitActionFinished,
            Auto_CheckPltState,
            Auto_CloseOvenDoor,
            Auto_SetPressure,

            Auto_OvenWorkStop,
            Auto_PreCheckVacPressure,
            Auto_SetOvenParameter,
            Auto_SetPreHeatVacBreath,
            Auto_OvenWorkStart,
            Auto_WorkEnd,
        }

        private enum ModuleDef
        {
            // 无效
            DefInvalid = -1,

            PalletMaxRow = 8,
            PalletMaxCol = 2,

        }

        private enum BakingType
        {
            Invalid = 0,    // 无效
            Normal,          // 正常Baking
            Rebaking,        // 重新Baking
        };

        private enum MsgID
        {
            Start = ModuleMsgID.DryingOvenMsgStartID,
            OvenConnectAlarm,
            CheckPallet,
            CheckOperate,
            OperateFail,
            AnomalyAlarm,
            DisConnect,
            CheckValue,
            CheckMES,
            FTPUploadErr,
            OvenPreHeatBreathState,
            OvenVacBreathState,
        }
        #endregion


        #region // 字段

        // 【相关模组】

        // 【IO/电机】

        // 【模组参数】
        private bool[,] bTransfer;                      // 炉腔转移：TRUE启用，FALSE禁用
        public bool[,] bOvenCavityNg;                   // 炉腔某层NG：TRUE启用，FALSE禁用
        private int[] nCirBakingTimes;                  // 循环干燥次数
        private int[] nCurBakingTimes;                  // 当前干燥次数
        public ProcessParam processParam;
        private static (PropertyInfo info, OvenParameterAttribute att)[] propListInfo;
        
        private int[] nRunTime;                         // 烘烤运行时间
        public int unDryTime;                           // 1)干燥时间
        public int unSetTempValue;                      // 2)烘烤温度
        public int unTempAddSv1;                        // 3)SV1温度增加量
        public int unTempAddSv2;                        // 4)SV2温度增加量
        public int unTempAddSv3;                        // 5)SV3温度增加量
        public int unTempAddSv4;                        // 6)SV4温度增加量
        public int unTempAddSv5;                        // 7)SV5温度增加量
        public int unStartTimeSv2;                      // 8)SV2开始时间
        public int unStartTimeSv3;                      // 9)SV3开始时间
        public int unStartTimeSv4;                      // 10)SV4开始时间
        public int unStartTimeSv5;                      // 11)SV5开始时间
        public int unPreHeatPressLow1;                  // 12)预热段1压力下限
        public int unPreHeatPressUp1;                   // 13)预热段1压力上限
        public int unPreHeatStartTime2;                 // 14)预热段2开始时间
        public int unPreHeatPressLow2;                  // 15)预热段2压力下限
        public int unPreHeatPressUp2;                   // 16)预热段2压力上限
        public int unHighVacStartTime;                  // 17)高真空段开始时间
        public int unHighVacFirTakeOutVacPress;         // 18)高真空段首抽真空压力
        public int unHighVacStartTime1;                 // 19)高真空段1开始时间
        public int unHighVacEndTime1;                   // 20)高真空段1结束时间
        public int unHighVacPressLow1;                  // 21)高真空段1压力下限
        public int unHighVacPressUp1;                   // 22)高真空段1压力上限
        public int unHighVacStartTime2;                 // 23)高真空段2开始时间
        public int unHighVacEndTime2;                   // 24)高真空段2结束时间
        public int unHighVacTakeOutVacCycle2;           // 25)高真空段2抽真空周期
        public int unHighVacTakeOutVacTime2;            // 26)高真空段2抽真空时间
        public int unBreatStartTime;                    // 27)呼吸开始时间
        public int unBreatEndTime;                      // 28)呼吸结束时间
        public int unBreatTouchPress;                   // 29)呼吸触发压力
        public int unBreatMinInterTime;                 // 30)呼吸最小间隔时间
        public int unBreatAirInfBackPress;              // 31)呼吸充气后压力
        public int unBreatBackKeepTime;                 // 32)呼吸后保持时间


        private uint unVacBkBTime;                     // 8)真空小于100PA时间标准值：>=则合格 ，<则重新干燥
        public int nNormalFormNo;                      // 正常配方号
        public int nReWorkFormNo;                      // 返工配方号

        private bool bPreHeatBreathEnable;              // 预热呼吸使能
        private bool bVacBreathEnable;                  // 真空呼吸使能

        private uint unOpenDoorPressure;                // 开门时真空压力：>则直接开门，<则先破真空再开门
        private uint unOpenDoorDelayTime;               // 开关炉门防呆时间（秒s）
        public double[] dWaterStandard;                 // 水含量标准值：<则合格，>则超标重新回炉干燥
        private string strOvenIP;                       // 干燥炉IP
        private int nOvenPort;                          // 干燥炉IP的端口
        private int nLocalNode;                         // 本机结点号
        private int nResouceUploadTime;			        // 干燥炉Resouce上传数据时间间隔
        private bool bPickUsPreState;                   // 取常压状态
        private string strResourceID;                   // 干燥炉资源号
        private bool[] bCavityClear;                    // 腔体清尾料功能
        public int nPlaceFakeRow;                       // 放假电池炉层
        private int nBakMaxCount;                       // 最大烘烤次数
        // 【模组数据】
        private DryingOvenClient ovenClient;            // 干燥炉客户端
        public CavityData[] bgCavityData;              // 后台更新腔体数据（临时）
        private CavityData[] curCavityData;             // 当前腔体数据（临时）
        private CavityData[] setCavityData;             // 设置腔体数据
        //private CavityState[] cavityState;              // 腔体状态
        private bool[] bClearMaintenance;               // 指示解除维修状态
        private float[,] fWaterContentValue;            // 水含量值[层][阴阳]
        private ModuleEvent curRespEvent;               // 当前响应信号
        private EventState curEventState;               // 当前信号状态
        private int nCurOperatRow;                      // 当前操作行
        private int nCurOperatCol;					    // 当前操作列
        private int nCurCheckCol;                       // 当前检查列（初始化使用）
        private int nOvenGroup;                         // 干燥炉组号
        private int nOvenDisplayGroup;                  // 干燥炉组号 (界面调用)
        private int nOvenID;                            // 干燥炉编号

        private Task bgThread;                          // 后台线程
        private bool bIsRunThread;                      // 指示线程运行
        private bool bCurConnectState;                  // 当前连接状态（提示用）
        private DateTime[] arrStartTime;                // 开始时间（测试用）
        private DateTime[] arrVacStartTime;             // 真空开始时间（MES）
        private DateTime[] processData;                 // 过程数据（MES）
        private int[] arrVacStartValue;                 // 真空第一次小于100Pa值（MES）

        private WCState[] WCUploadStatus;	            // 水含量上传状态
        public int[] nBakingType;                       // 指示Baking类型（重新Baking，继续Baking,正常Baking）
        private DateTime[] dtResouceStartTime;          // 起始时间(用于定时上传Resouce数据到MES
        private DateTime[] dtTempStartTime;             // 起始时间(用于定时上传Resouce数据到MES))
        public float[,,,,] unTempValue;                 // 温度值[层数，托盘数, 温度类型数, 发热板数, 曲线点数](曲线图界面)
        public int[,] unVacPressure;                    // 真空压力[层数, 曲线点数](曲线图界面)
        public int nGraphPosCount;                      // 曲线点数
        private DateTime[] dtGraphStartTime;            // 起始时间(用于曲线点数))

        public int[] nMinVacm;                         // 最小真空值
        public int[] nMaxVacm;                         // 最大真空值
        public int[] nMinTemp;                         // 最小温度
        public int[] nMaxTemp;                         // 最大温度
        private bool[] bStart;                          // 加真空小于100PA时间，重新启动
        private int[] nStartCount;                      // 启动计数,超过次数屏蔽炉腔
        private int[] nOvenVacm;                        // 当前真空值
        private int[] nOvenTemp;                        // 当前温度
        public int setOvenCount;                        // 安全门设置次数
        ShowCountInfo<int> ShowNBakingOverBat { get; set; }
        public int nBakingOverBat;                      // 烘烤完成电芯
        public int NBakingOverBat
        {
            get => ShowNBakingOverBat.ProductData;
            set => ShowNBakingOverBat.ProductData = value; 
        }
        public float fHistEnergySum;                    // 历史耗能总和
        public float fOneDayEnergy;                     // 单日耗能
        public float fBatAverEnergy;                    // 电芯平均能耗
        public int[] nBakCount;                         // 烘烤次数
        public int[,] pltTypeCount;                     // 托盘类型数量[列, 类型]
        public int[,] pltTypePos;                       // 托盘类型位置[列, 类型]
        public bool bHeartBeat;                         // 心跳状态
        private bool bAutoAnomaly;                      // 自动异常
        private bool[] bUploadWaterStatus;              // 上传水含量状态

        private bool BlowAlarm = false; //破真空报警上传标志
        private bool VacAlarm = false; //真空报警上传标志
        private bool TempAlarm = false; //真空报警上传标志
        


        public string TransferIndex => $@"{((int)(TransferRobotStation.DryingOven_0 + GetOvenID()))},.*,.*,"+(int)ModuleRowCol.DryingOvenCol;


        public CavityRowData[] CavityDataSource { get; } = Enumerable.Range(0, (int)ModuleDef.PalletMaxCol).Select(index => new CavityRowData { RowIndex = index }).ToArray();

        public IEnumerable<CavityRowData> CavityDataSourceView => CavityDataSource.SubarrayInversion((int)ModuleRowCol.DryingOvenCol).ToList();

        public string StrResourceID { get => strResourceID; set => strResourceID = value; }

        public string OvenIP
        {
            get { return strOvenIP; }
            set { SetProperty(ref strOvenIP, value); }
        }

        public int OvenPort
        {
            get { return nOvenPort; }
            set { SetProperty(ref nOvenPort, value); }
        }

        public bool CurConnectState
        {
            get { return bCurConnectState; }
            set { SetProperty(ref bCurConnectState, value); }
        }
        #endregion


        #region // 构造函数

        public RunProDryingOven(int RunID) : base(RunID)
        {
            // 创建托盘，电池组，信号
            InitCreateObject((int)ModuleMaxPallet.DryingOven, 0, 0, (int)ModuleEvent.OvenEventEnd);

            // 模组对象初始化
            InitModuleObject();

            // 上电重置
            PowerUpRestart();
            // 插入参数
            //InsertPrivateParam("DryTime", "干燥时间", "干燥时间(>0分钟)", unDryTime, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("SetTempValue", "烘烤温度", "烘烤温度(>0℃)", unSetTempValue, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("WaterOKValue", "水分合格值", "水分合格值(>0PP)", unWaterOKValue, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("DestTempSv1", "目标温度德尔塔SV1", "目标温度德尔塔SV1(>0℃)", unDestTempSv1, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("DestTempSv2", "目标温度德尔塔SV2", "目标温度德尔塔SV2(>0℃)", unDestTempSv2, ParameterLevel.PL_STOP_ADMIN);

            //InsertPrivateParam("DestTempSv3", "目标温度德尔塔SV3", "目标温度德尔塔SV3(>0℃)", unDestTempSv3, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("DestTempSv4", "目标温度德尔塔SV4", "目标温度德尔塔SV4(>0℃)", unDestTempSv4, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("DestTempSv5", "目标温度德尔塔SV5", "目标温度德尔塔SV5(>0℃)", unDestTempSv5, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("TempOverTime1", "温度阶段1结束时间", "温度阶段1结束时间(>0分钟)", unTempOverTime1, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("TempOverTime2", "温度阶段2结束时间", "温度阶段2结束时间(>0分钟)", unTempOverTime2, ParameterLevel.PL_STOP_ADMIN);

            //InsertPrivateParam("TempOverTime3", "温度阶段3结束时间", "温度阶段3结束时间(>0分钟)", unTempOverTime3, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("TempOverTime4", "温度阶段4结束时间", "温度阶段4结束时间(>0分钟)", unTempOverTime4, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("MornTakeOutVac", "最早抽高真空时间", "最早抽高真空时间(>0分钟)", unMornTakeOutVac, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("LateTakeOutVac", "最晚抽高真空时间", "最晚抽高真空时间(>0分钟)", unLateTakeOutVac, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("HighVacValue", "高真空值", "高真空值(>0Pa)", unHighVacValue, ParameterLevel.PL_STOP_ADMIN);

            //InsertPrivateParam("BreatStartTime", "呼吸开始时间", "呼吸开始时间(>0分钟)", unBreatStartTime, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("BreatOverTime", "呼吸结束时间", "呼吸结束时间(>0分钟)", unBreatOverTime, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("BreatInterval", "呼吸间隔", "呼吸间隔(>0分钟)", unBreatInterval, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("BreatTouchVac", "呼吸触发真空", "呼吸触发真空(>0Pa)", unBreatTouchVac, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("BreatBackVac", "呼吸后真空", "呼吸后真空(>0Pa)", unBreatBackVac, ParameterLevel.PL_STOP_ADMIN);
            //InsertPrivateParam("BreatBackKeepTime", "呼吸后保持时间", "呼吸后保持时间(>0秒", unBreatBackKeepTime, ParameterLevel.PL_STOP_ADMIN);

            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                InsertPrivateParam("OvenEnable" + (nColIdx + 1), (nColIdx + 1) + "列炉腔使能", "炉腔使能：TRUE启用，FALSE禁用", CavityDataSource[nColIdx].OvenEnable, ParameterLevel.PL_STOP_OPER);
            }

            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                InsertPrivateParam("Pressure" + (nColIdx + 1), (nColIdx + 1) + "列炉腔保压", "炉腔保压：TRUE启用，FALSE禁用", CavityDataSource[nColIdx].Pressure, ParameterLevel.PL_STOP_OPER);
            }

            //string strKey = "";
            //for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            //{
            //    for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
            //    {
            //        strKey = string.Format("OvenCavityNg[{0},{1}]", nColIdx, nRowIdx);
            //        InsertPrivateParam(strKey, string.Format("第{0}列炉腔第{1}层NG", nColIdx + 1, nRowIdx + 1), "", bOvenCavityNg[nColIdx, nRowIdx],  ParameterLevel.PL_STOP_MAIN);
            //    }
            //}

            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                InsertPrivateParam("CavityClear" + (nColIdx + 1), (nColIdx + 1) + "列炉腔清尾料", "炉腔清尾料：TRUE启用，FALSE禁用", bCavityClear[nColIdx], ParameterLevel.PL_STOP_OPER);
            }
            InsertPrivateParam("PickUsPreState", "取常压状态", "TRUE开关炉门判断常压状态，FALSE判断真空值", bPickUsPreState);
            InsertPrivateParam("PreHeatBreathEnable", "预热呼吸使能", "预热呼吸使能：TRUE启用，FALSE禁用", bPreHeatBreathEnable, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("VacBreathEnable", "真空呼吸使能", "真空呼吸使能：TRUE启用，FALSE禁用", bVacBreathEnable, ParameterLevel.PL_STOP_ADMIN);


            InsertPrivateParam("NormalFormNo", "正常配方号", "正常配方号(>0)", nNormalFormNo, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("ReWorkFormNo", "返工配方号", "返工配方号(>0)", nReWorkFormNo, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("VacBkBTime", "真空小于100PA时间标准值", "真空小于100PA时间标准值：>=则合格,<则重新干燥", unVacBkBTime, ParameterLevel.PL_STOP_ADMIN);


            InsertPrivateParam("PlaceFakeRow", "放假电池炉层", "放假电池炉层(1至8层，0为随机)", nPlaceFakeRow);
            InsertPrivateParam("BakMaxCount", "最大烘烤次数", "烘烤次数限制(>2)", nBakMaxCount);
            // 有温差，不支持分散转腔
            //for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            //{
            //    for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
            //    {
            //        strKey = string.Format("Transfer[{0},{1}]", nColIdx, nRowIdx);
            //        InsertPrivateParam(strKey, string.Format("第{0}列炉腔第{1}层转移", nColIdx + 1, nRowIdx + 1), "", bTransfer[nColIdx, nRowIdx],  ParameterLevel.PL_STOP_ADMIN);
            //    }
            //}

            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                InsertPrivateParam("CirBakingTimes" + (nColIdx + 1), "第" + (nColIdx + 1) + "列抽检周期（次）", "", nCirBakingTimes[nColIdx], ParameterLevel.PL_STOP_ADMIN);
            }

            InsertPrivateParam("OpenDoorPressure", "开门时真空压力", "开门时真空压力(>0)", unOpenDoorPressure);
            InsertPrivateParam("OpenDoorDelayTime", "开关炉门延时时间", "开关炉门防呆时间（秒s）", unOpenDoorDelayTime);
            InsertPrivateParam("WaterStandard[0]", "混合型水含量标准值", "混合型水含量标准值：≤ 则合格，> 则超标重新回炉干燥", dWaterStandard[0], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("WaterStandard[1]", "阳极水含量标准值", "阳极水含量标准值：≤ 则合格，> 则超标重新回炉干燥", dWaterStandard[1], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("WaterStandard[2]", "阴极水含量标准值", "阴极水含量标准值：≤ 则合格，> 则超标重新回炉干燥", dWaterStandard[2], ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("OvenIP", "干燥炉IP", "干燥炉IP", strOvenIP);
            InsertPrivateParam("OvenPort", "干燥炉端口", "干燥炉IP的Port", nOvenPort);
            InsertPrivateParam("ResouceUploadTime", "温度数据采集周期(s)", "Resouce上传数据时间间隔", nResouceUploadTime, ParameterLevel.PL_STOP_ADMIN);
            InsertPrivateParam("StrResourceID", "干燥炉资源号", "干燥炉资源号", strResourceID);

        }

        #endregion


        #region // 模组数据初始化和配置读取

        /// <summary>
        /// 初始化模组对象
        /// </summary>
        private void InitModuleObject()
        {
            // IO/电机

            // 模组参数
            bTransfer = new bool[(int)ModuleRowCol.DryingOvenCol, (int)ModuleRowCol.DryingOvenRow];
            bCavityClear = new bool[(int)ModuleRowCol.DryingOvenCol] { false, false };
            nCirBakingTimes = new int[(int)ModuleRowCol.DryingOvenCol] { 1, 1 };
            nCurBakingTimes = new int[(int)ModuleRowCol.DryingOvenCol] { 1, 1 };
            nRunTime = new int[(int)ModuleRowCol.DryingOvenCol] { 0, 0 };
            dWaterStandard = new double[3] { 200, 250, 400 };
            bOvenCavityNg = new bool[(int)ModuleRowCol.DryingOvenCol, (int)ModuleRowCol.DryingOvenRow];
            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                {
                    bOvenCavityNg[nColIdx, nRowIdx] = false;
                    bTransfer[nColIdx, nRowIdx] = false;
                }
            }

            processParam = new ProcessParam();
            propListInfo = processParam.GetPropListInfo();

            unDryTime = 0;
            unSetTempValue = 0;
            unTempAddSv1 = 0;
            unTempAddSv2 = 0;
            unTempAddSv3 = 0;
            unTempAddSv4 = 0;
            unTempAddSv5 = 0;
            unStartTimeSv2 = 0;
            unStartTimeSv3 = 0;
            unStartTimeSv4 = 0;
            unStartTimeSv5 = 0;
            unPreHeatPressLow1 = 0;
            unPreHeatPressUp1 = 0;
            unPreHeatStartTime2 = 0;
            unPreHeatPressLow2 = 0;
            unPreHeatPressUp2 = 0;
            unHighVacStartTime = 0;
            unHighVacFirTakeOutVacPress = 0;
            unHighVacStartTime1 = 0;
            unHighVacEndTime1 = 0;
            unHighVacPressLow1 = 0;
            unHighVacPressUp1 = 0;
            unHighVacStartTime2 = 0;
            unHighVacEndTime2 = 0;
            unHighVacTakeOutVacCycle2 = 0;
            unHighVacTakeOutVacTime2 = 0;
            unBreatStartTime = 0;
            unBreatEndTime = 0;
            unBreatTouchPress = 0;
            unBreatMinInterTime = 0;
            unBreatAirInfBackPress = 0;
            unBreatBackKeepTime = 0;

            unOpenDoorPressure = 96000;
            unOpenDoorDelayTime = 20;
            strOvenIP = "";
            nOvenPort = 9600;
            nLocalNode = 150;
            nResouceUploadTime = 6;
            nGraphPosCount = 0;
            bPickUsPreState = true;
            strResourceID = "";
            nPlaceFakeRow = 0;
            nBakMaxCount = 3;

            nNormalFormNo = 1;
            nReWorkFormNo = 2;
            // 模组数据
            nOvenID = -1;
            nOvenGroup = 0;
            nOvenDisplayGroup = 0;
            bgThread = null;
            bIsRunThread = false;
            CurConnectState = false;
            bPreHeatBreathEnable = false;
            bVacBreathEnable = false;

            bgCavityData = new CavityData[(int)ModuleRowCol.DryingOvenCol];
            curCavityData = new CavityData[(int)ModuleRowCol.DryingOvenCol];
            setCavityData = new CavityData[(int)ModuleRowCol.DryingOvenCol];
            bClearMaintenance = new bool[(int)ModuleRowCol.DryingOvenCol];
            fWaterContentValue = new float[(int)ModuleRowCol.DryingOvenCol, 5];
            arrStartTime = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            arrVacStartTime = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            arrVacStartValue = new int[(int)ModuleRowCol.DryingOvenCol];
            WCUploadStatus = new WCState[(int)ModuleRowCol.DryingOvenCol];
            nBakingType = new int[(int)ModuleRowCol.DryingOvenCol];
            processData = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            dtResouceStartTime = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            dtTempStartTime = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            dtGraphStartTime = new DateTime[(int)ModuleRowCol.DryingOvenCol];
            unTempValue = new float[(int)ModuleRowCol.DryingOvenCol, (int)ModuleRowCol.DryingOvenRow, 4, 20, 120 * 10];
            unVacPressure = new int[(int)ModuleRowCol.DryingOvenCol, 120 * 10];
            nMinVacm = new int[(int)ModuleRowCol.DryingOvenCol];
            nMaxVacm = new int[(int)ModuleRowCol.DryingOvenCol];
            nMinTemp = new int[(int)ModuleRowCol.DryingOvenCol];
            nMaxTemp = new int[(int)ModuleRowCol.DryingOvenCol];
            bStart = new bool[(int)ModuleRowCol.DryingOvenCol];
            nStartCount = new int[(int)ModuleRowCol.DryingOvenCol];
            nOvenVacm = new int[(int)ModuleRowCol.DryingOvenCol];
            nOvenTemp = new int[(int)ModuleRowCol.DryingOvenCol];
            nBakCount = new int[(int)ModuleRowCol.DryingOvenCol];
            bUploadWaterStatus = new bool[(int)ModuleRowCol.DryingOvenCol];
            bHeartBeat = false;
            bAutoAnomaly = false;

            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                bgCavityData[nCavityIdx] = new CavityData();
                curCavityData[nCavityIdx] = new CavityData();
                setCavityData[nCavityIdx] = new CavityData();
                arrStartTime[nCavityIdx] = new DateTime();
                arrVacStartTime[nCavityIdx] = new DateTime();
                arrVacStartValue[nCavityIdx] = 0;
                WCUploadStatus[nCavityIdx] = new WCState();
                nBakingType[nCavityIdx] = (int)BakingType.Invalid;
                dtResouceStartTime[nCavityIdx] = DateTime.Now;
                processData[nCavityIdx] = DateTime.Now;
                dtTempStartTime[nCavityIdx] = DateTime.Now;
                dtGraphStartTime[nCavityIdx] = DateTime.Now;
                TempValueRelease(nCavityIdx);
                bStart[nCavityIdx] = false;
                nStartCount[nCavityIdx] = 0;
                nBakCount[nCavityIdx] = 0;
                bUploadWaterStatus[nCavityIdx] = false;
            }

            pltTypeCount = new int[(int)ModuleRowCol.DryingOvenCol, (int)PltType.PltTypeEnd];
            pltTypePos = new int[(int)ModuleRowCol.DryingOvenCol, (int)PltType.PltTypeEnd];
            for (int nCol = 0; nCol < (int)ModuleRowCol.DryingOvenCol; nCol++)
            {
                for (int nType = 0; nType < (int)PltType.PltTypeEnd; nType++)
                {
                    pltTypeCount[nCol, nType] = 0;
                    pltTypePos[nCol, nType] = -1;
                }
            }

            Pallet.ForEach(
                (pallet, index) => 
                {
                    int i = index >= (int)ModuleDef.PalletMaxRow ? 0:1;
                    if (CavityDataSource[i].Plts == null)
                        CavityDataSource[i].Plts = new List<Pallet>();
                    CavityDataSource[i].Plts.Add(pallet); 
                });
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

            // 模组配置
            nOvenID = IniFile.ReadInt(module, "OvenID", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            nOvenGroup = IniFile.ReadInt(module, "OvenGroup", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            nOvenDisplayGroup = IniFile.ReadInt(module, "OvenDisplayGroup", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            nLocalNode = IniFile.ReadInt(module, "LocalNode", 150, Def.GetAbsPathName(Def.ModuleExCfg));
            ovenClient = new DryingOvenClient(nOvenGroup, CavityDataSource);
            ShowNBakingOverBat = App.Ioc.Resolve<ObservableCollection<object>>("ShowProductDatas").OfType<ShowCountInfo<int>>().FindFirst(f => f.Name == $"干燥炉{nOvenID + 1}产能:");

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
                        this.nextInitStep = InitSteps.Init_ConnectOven;
                        break;
                    }

                case InitSteps.Init_ConnectOven:
                    {
                        CurMsgStr("连接干燥炉", "Init connect drying oven");

                        if (DryRun || Def.IsNoHardware())
                        {
                            InitThread();
                            this.nextInitStep = InitSteps.Init_End;
                            break;
                        }
                        else if (DryOvenConnect())
                        {
                            InitThread();
                            nCurCheckCol = (int)ModuleDef.PalletMaxCol - 1;
                            this.nextInitStep = InitSteps.Init_CheckDoorClose;
                        }
                        break;
                    }
                case InitSteps.Init_CheckDoorClose:
                    {
                        CurMsgStr("检查炉门关闭", "Init check door close");

                        if (UpdateOvenData(curCavityData))
                        {
                            if (nCurCheckCol >= 0 &&
                                OvenDoorState.Close == SetCavityData(nCurCheckCol).DoorState &&
                                OvenDoorState.Close != CurCavityData(nCurCheckCol).DoorState)
                            {
                                this.nextInitStep = InitSteps.Init_CloseOvenDoor;
                            }
                            else
                            {
                                this.nextInitStep = InitSteps.Init_DoorCloseFinished;
                            }
                        }
                        break;
                    }
                case InitSteps.Init_CloseOvenDoor:
                    {
                        CurMsgStr("关闭炉门", "Init close oven door");

                        setCavityData[nCurCheckCol].DoorState = OvenDoorState.Close;
                        if (OvenDoorOperate(nCurCheckCol, setCavityData[nCurCheckCol]))
                        {
                            this.nextInitStep = InitSteps.Init_DoorCloseFinished;
                        }
                        break;
                    }
                case InitSteps.Init_DoorCloseFinished:
                    {
                        CurMsgStr("炉门关闭完成", "Init door close finished");

                        if (nCurCheckCol > 0)
                        {
                            nCurCheckCol--;
                            this.nextInitStep = InitSteps.Init_CheckDoorClose;
                        }
                        else
                        {
                            nCurCheckCol = 0;
                            this.nextInitStep = InitSteps.Init_CheckDoorOpen;
                        }
                        break;
                    }
                case InitSteps.Init_CheckDoorOpen:
                    {
                        CurMsgStr("检查炉门打开", "Init check door open");

                        if (UpdateOvenData(curCavityData))
                        {
                            if (nCurCheckCol < (int)ModuleDef.PalletMaxRow &&
                                OvenDoorState.Open == SetCavityData(nCurCheckCol).DoorState &&
                                OvenDoorState.Open != CurCavityData(nCurCheckCol).DoorState)
                            {
                                this.nextInitStep = InitSteps.Init_OpenOvenDoor;
                            }
                            else
                            {
                                this.nextInitStep = InitSteps.Init_DoorOpenFinished;
                            }
                        }
                        break;
                    }
                case InitSteps.Init_OpenOvenDoor:
                    {
                        CurMsgStr("打开炉门", "Init open oven door");

                        setCavityData[nCurCheckCol].DoorState = OvenDoorState.Open;
                        if (OvenDoorOperate(nCurCheckCol, setCavityData[nCurCheckCol]))
                        {
                            this.nextInitStep = InitSteps.Init_DoorOpenFinished;
                        }
                        break;
                    }
                case InitSteps.Init_DoorOpenFinished:
                    {
                        CurMsgStr("炉门打开完成", "Init door open finished");

                        if (nCurCheckCol < (int)ModuleDef.PalletMaxCol - 1)
                        {
                            nCurCheckCol++;
                            this.nextInitStep = InitSteps.Init_CheckDoorOpen;
                        }
                        else
                        {
                            nCurCheckCol = -1;
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
            try
            {
                switch ((AutoSteps)this.nextAutoStep)
                {
                    #region // 信号发送和响应

                    case AutoSteps.Auto_WaitWorkStart:
                        {
                            CurMsgStr("等待开始信号", "Wait work start");
                            //清尾料
                            CavityTailing();
                            // 等待工作的炉腔
                            if (HasWaitWorkCavity(ref nCurOperatCol) /*|| HasAnomalyWorkCavity(ref nCurOperatCol)*/)
                            {
                                if (nBakCount[nCurOperatCol] > nBakMaxCount)
                                {
                                    CavityDataSource[nCurOperatCol].OvenEnable = false;
                                    SaveParameter();
                                    string strErr = string.Format("干燥炉{0}第{1}层烘烤次数超过3次，不能重新启动", nOvenID + 1, nCurOperatCol + 1);
                                    ShowMsgBox.ShowDialog(strErr, MessageType.MsgWarning);
                                }
                                else
                                {
                                    TempValueRelease(nCurOperatCol);
                                    bStart[nCurOperatCol] = true;
                                    this.nextAutoStep = AutoSteps.Auto_OvenWorkStop;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables | SaveType.MaxMinValue);
                                }
                                break;
                            }

                            if (nOvenID == 0)
                            {
                                int iiiii = 0;
                            }
                            // 水含量结果检测，待添加
                            if (CheckWaterContent(fWaterContentValue, ref nCurOperatCol))
                            {
                                string strErr = "";
                                if (!DryRun && !OvenIsConnect())
                                {
                                    strErr = "炉子未连接，水含量上传失败";
                                    ShowMessageBox((int)MsgID.DisConnect, strErr, "请检查连接", MessageType.MsgWarning);
                                    break;
                                }
                                strErr = "";
                                bool bRetest = false;
                                if (!UploadBatWaterStatus(nCurOperatCol, ref bRetest, ref strErr))
                                {
                                    fWaterContentValue[nCurOperatCol, 0] = -1.0f;
                                    fWaterContentValue[nCurOperatCol, 1] = -1.0f;
                                    fWaterContentValue[nCurOperatCol, 2] = -1.0f;
                                    CavityDataSource[nCurOperatCol].OvenEnable = false;
                                    SaveParameter();
                                    SaveRunData(SaveType.Variables);
                                    ShowMessageBox((int)MsgID.CheckMES, strErr, "请查询MesLog", MessageType.MsgWarning);
                                    break;
                                }

                                bUploadWaterStatus[nCurOperatCol] = true;

                                // 水含量合格
                                //if ((MachineCtrl.GetInstance().UpdataMES && !bRetest) ||
                                //    (!MachineCtrl.GetInstance().UpdataMES && CheckWater(fWaterContentValue, nCurOperatCol)))
                                if (CheckWater(fWaterContentValue, nCurOperatCol))
                                {
                                    strErr = "";
                                    if (!MesUploadOvenFinish(nOvenID, fWaterContentValue, nCurOperatCol, ref strErr))
                                    {
                                        fWaterContentValue[nCurOperatCol, 0] = -1.0f;
                                        fWaterContentValue[nCurOperatCol, 1] = -1.0f;
                                        fWaterContentValue[nCurOperatCol, 2] = -1.0f;
                                        CavityDataSource[nCurOperatCol].OvenEnable = false;
                                        SaveParameter();
                                        SaveRunData(SaveType.Variables);
                                        ShowMessageBox((int)MsgID.CheckMES, strErr, "请查询MesLog", MessageType.MsgWarning);
                                        break;
                                    }

                                    bUploadWaterStatus[nCurOperatCol] = false;

                                    for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.PalletMaxRow; nPltIdx++)
                                    {
                                        if (GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.WaitRes))
                                        {
                                            int nIndex = nCurOperatCol * (int)ModuleDef.PalletMaxRow + nPltIdx;
                                            Pallet[nIndex].Stage |= PltStage.Baking;

                                            RunProCoolingStove coolingStove = (RunProCoolingStove)MachineCtrl.GetInstance().GetModule(RunID.CoolingStove);
                                            if(coolingStove.Enabled && coolingStove.CoolingTim >0)
                                                Pallet[nIndex].Type = PltType.WaitCooling;
                                            else
                                                Pallet[nIndex].Type = PltType.WaitOffload;
                                            SaveRunData(SaveType.Pallet, nIndex);
                                        }
                                    }

                                    if (nCurBakingTimes[nCurOperatCol] >= nCirBakingTimes[nCurOperatCol])
                                    {
                                        nCurBakingTimes[nCurOperatCol] = 1;
                                        fWaterContentValue[nCurOperatCol, 0] = -1.0f;
                                        fWaterContentValue[nCurOperatCol, 1] = -1.0f;
                                        fWaterContentValue[nCurOperatCol, 2] = -1.0f;
                                    }
                                    else
                                    {
                                        nCurBakingTimes[nCurOperatCol]++;
                                    }
                                    nBakCount[nCurOperatCol] = 0;
                                    NBakingOverBat += CalBatCount(nCurOperatCol, PltType.WaitOffload, BatType.OK);
                                    SetCavityState(nCurOperatCol, CavityState.Standby);
                                    SetWCUploadStatus(nCurOperatCol, WCState.WCStateInvalid);
                                    SaveRunData(SaveType.Variables);
                                }
                                // 水含量超标 || 复烘
                                else if (nCurBakingTimes[nCurOperatCol] == 1)
                                {
                                    bUploadWaterStatus[nCurOperatCol] = false;

                                    for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.PalletMaxRow; nPltIdx++)
                                    {
                                        if (GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.WaitRes))
                                        {
                                            int nIndex = nCurOperatCol * (int)ModuleDef.PalletMaxRow + nPltIdx;
                                            Pallet[nIndex].Type = PltType.WaitRebakeBat;
                                            SaveRunData(SaveType.Pallet, nIndex);
                                        }
                                    }
                                    UploadBatWaterNG(nCurOperatCol);
                                    fWaterContentValue[nCurOperatCol, 0] = -1.0f;
                                    fWaterContentValue[nCurOperatCol, 1] = -1.0f;
                                    fWaterContentValue[nCurOperatCol, 2] = -1.0f;
                                    nBakingType[nCurOperatCol] = (int)BakingType.Normal;
                                    SetCavityState(nCurOperatCol, CavityState.Rebaking);
                                    SetWCUploadStatus(nCurOperatCol, WCState.WCStateInvalid);
                                    SaveRunData(SaveType.Variables);
                                }
                            }


                            // ================================== 发送放托盘信号 ==================================
                            for (ModuleEvent nEvent = ModuleEvent.OvenPlaceEmptyPlt; nEvent < ModuleEvent.OvenEventEnd; nEvent++)
                            {
                                // 取消状态改为无效状态
                                if (GetEvent(this, nEvent, ref curEventState) && (EventState.Cancel == curEventState))
                                {
                                    SetEvent(this, nEvent, EventState.Invalid);
                                }
                            }

                            if (HasPlacePos())
                            {
                                // 放：带假电池满托盘
                                if (GetEvent(this, ModuleEvent.OvenPlaceFakeFullPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceFakeFullPlt, EventState.Require);
                                }

                                // 放：满托盘
                                if (GetEvent(this, ModuleEvent.OvenPlaceFullPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceFullPlt, EventState.Require);
                                }

                                // 放：空托盘
                                if (GetEvent(this, ModuleEvent.OvenPlaceEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceEmptyPlt, EventState.Require);
                                }

                                // 放：NG空托盘
                                if (GetEvent(this, ModuleEvent.OvenPlaceNGEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceNGEmptyPlt, EventState.Require);
                                }
                            }

                            // 放：等待水含量结果托盘（已取待测假电池的托盘）
                            if (HasPlaceWiatResPltPos())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPlaceWaitResultPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceWaitResultPlt, EventState.Require);
                                }
                            }

                            // 放：回炉托盘（已放回假电池的托盘）
                            if (HasPlaceRebakingPltPos())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPlaceRebakingFakePlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPlaceRebakingFakePlt, EventState.Require);
                                }
                            }

                            // ================================== 发送取托盘信号 ==================================

                            // 取：空托盘
                            if (HasEmptyPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickEmptyPlt, EventState.Require);
                                }
                            }

                            // 取：NG空托盘
                            if (HasNGEmptyPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickNGEmptyPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickNGEmptyPlt, EventState.Require);
                                }
                            }

                            // 取：NG非空托盘
                            if (HasNGPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickNGPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickNGPlt, EventState.Require);
                                }
                            }

                            // 取：待检测托盘（未取走假电池的托盘）
                            if (HasDetectPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickDetectPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickDetectPlt, EventState.Require);
                                }
                            }

                            // 取：待回炉托盘（已取走假电池，待重新放回假电池的托盘）
                            if (HasRebakingPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickRebakingPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickRebakingPlt, EventState.Require);
                                }
                            }

                            // 取：待冷却托盘
                            if (HasWaitCooling())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickWaitCooling, ref curEventState) &&
                                (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickWaitCooling, EventState.Require);
                                }
                            }

                            // 取：待下料托盘（干燥完成的托盘）
                            if (HasOffloadPlt())
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickOffloadPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickOffloadPlt, EventState.Require);
                                }
                            }

                            //有转移满料
                            /*if (HasTransferFullPlt(Pallet))
                            {
                                if (GetEvent(this, ModuleEvent.OvenPickTransferPlt, ref curEventState) &&
                                    (EventState.Invalid == curEventState || EventState.Finished == curEventState))
                                {
                                    SetEvent(this, ModuleEvent.OvenPickTransferPlt, EventState.Require);
                                }
                            }*/

                            // 信号响应
                            for (ModuleEvent eventIdx = ModuleEvent.OvenPlaceEmptyPlt; eventIdx < ModuleEvent.OvenEventEnd; eventIdx++)
                            {
                                if (GetEvent(this, eventIdx, ref curEventState, ref nCurOperatRow, ref nCurOperatCol))
                                {
                                    if (EventState.Response == curEventState &&
                                        nCurOperatRow > -1 && nCurOperatRow < (int)ModuleDef.PalletMaxRow &&
                                        nCurOperatCol > -1 && nCurOperatCol < (int)ModuleDef.PalletMaxCol)
                                    {
                                        curRespEvent = eventIdx;
                                        this.nextAutoStep = AutoSteps.Auto_PreCloseOvenDoor;
                                        SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                        break;
                                    }
                                }
                            }
                            break;
                        }

                    #endregion


                    #region // 取放托盘流程

                    case AutoSteps.Auto_PreCloseOvenDoor:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列预先关闭炉门", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col pre close oven door", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].DoorState = OvenDoorState.Close;
                            if (DryRun || OvenDoorOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PreBreakVacuum;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            else
                            {
                                // 暂时不用，待确认后使用
                                if (CheckEvent(this, curRespEvent, EventState.Cancel))
                                {

                                    SetEvent(this, curRespEvent, EventState.Invalid);
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                                    break;
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_PreBreakVacuum:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列预先破真空", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col pre break vacuum", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            if (UpdateOvenData(curCavityData))
                            {
                                //int nCurCol = (0 == nOvenGroup) ? nCurOperatCol : (1 - nCurOperatCol);
                                bool bRes = bPickUsPreState ? CurCavityData(nCurOperatCol).BlowUsPreState == OvenBlowUsPreState.Have
                                    : CurCavityData(nCurOperatCol).unVacPressure >= unOpenDoorPressure;
                                if (DryRun || bRes)
                                {
                                    setCavityData[nCurOperatCol].BlowState = OvenBlowState.Close;
                                    if (DryRun || OvenBreakVacOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;
                                    }
                                }
                                else
                                {
                                    if (OvenBlowState.Open != CurCavityData(nCurOperatCol).BlowState)
                                    {
                                        setCavityData[nCurOperatCol].BlowState = OvenBlowState.Open;
                                        OvenBreakVacOperate(nCurOperatCol, setCavityData[nCurOperatCol]);
                                    }
                                    //设置保压
                                    if (OvenPressureState.Close != CurCavityData(nCurOperatCol).PressureState)
                                    {
                                        setCavityData[nCurOperatCol].PressureState = OvenPressureState.Close;
                                        OvenPressureOperate(nCurOperatCol, setCavityData[nCurOperatCol]);
                                    }
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_OpenOvenDoor:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列打开炉门", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col open oven door", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].DoorState = OvenDoorState.Open;
                            if (DryRun || OvenDoorOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PreCheckPltState;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            else
                            {
                                // 暂时不用，待确认后使用
                                if (CheckEvent(this, curRespEvent, EventState.Cancel))
                                {
                                    SetEvent(this, curRespEvent, EventState.Invalid);
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                                    break;
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_PreCheckPltState:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列预先检查托盘状态", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] pre check pallet state", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            if (UpdateOvenData(curCavityData))
                            {
                                // 交换硬件数据
                                //int nCurCol = (0 == nOvenGroup) ? nCurOperatCol : (1 - nCurOperatCol);
                                OvenPalletState pltState = CurCavityData(nCurOperatCol).PltState[nCurOperatRow];

                                //if (Def.IsNoHardware() || DryRun || OvenPalletState.Invalid != pltState)
                                {
                                    bool bHasPlt = (OvenPalletState.Have == pltState);
                                    bool bHasData = (GetPlt(nCurOperatCol, nCurOperatRow).Type > PltType.Invalid);

                                    if (Def.IsNoHardware() || DryRun || (OvenPalletState.Invalid != pltState && bHasPlt == bHasData))
                                    {
                                        if (SetEvent(this, curRespEvent, EventState.Ready, nCurOperatCol, nCurOperatRow))
                                        {
                                            this.nextAutoStep = AutoSteps.Auto_WaitActionFinished;
                                            SaveRunData(SaveType.AutoStep);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        string strMsg, strDisp, strPlt, strData;
                                        strPlt = bHasPlt ? "有" : "无";
                                        strData = bHasData ? "有" : "无";
                                        strDisp = "请停机检查炉腔中夹具状态！";
                                        strMsg = string.Format("{0}层{1}列炉腔中检测到{2}夹具，实际应该{3}夹具", nCurOperatRow + 1, nCurOperatCol + 1, strPlt, strData);
                                        ShowMessageBox((int)MsgID.CheckPallet, strMsg, strDisp, MessageType.MsgWarning);
                                        break;
                                    }
                                }

                                // 暂时不用，待确认后使用
                                if (CheckEvent(this, curRespEvent, EventState.Cancel))
                                {
                                    SetEvent(this, curRespEvent, EventState.Invalid);
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep | SaveType.SignalEvent);
                                    break;
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_WaitActionFinished:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列等待动作完成", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col wait action finished", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            if (CheckEvent(this, curRespEvent, EventState.Finished))
                            {
                                // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                if (ModuleEvent.OvenPlaceWaitResultPlt == curRespEvent)
                                {
                                    // 切换托盘状态
                                    for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.PalletMaxRow; nPltIdx++)
                                    {
                                        if (GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.Detect) ||
                                            GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.WaitRes))
                                        {
                                            int nIndex = nCurOperatCol * (int)ModuleDef.PalletMaxRow + nPltIdx;
                                            Pallet[nIndex].Type = PltType.WaitRes;
                                            SaveRunData(SaveType.Pallet, nIndex);
                                        }
                                    }

                                    // 切换腔体状态
                                    SetCavityState(nCurOperatCol, CavityState.WaitRes);
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }

                                // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                else if (ModuleEvent.OvenPlaceRebakingFakePlt == curRespEvent)
                                {
                                    // 切换托盘状态
                                    for (int nPltIdx = 0; nPltIdx < (int)ModuleDef.PalletMaxRow; nPltIdx++)
                                    {
                                        if (GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.WaitRebakeBat) ||
                                            GetPlt(nCurOperatCol, nPltIdx).IsType(PltType.WaitRebakingToOven))
                                        {
                                            int nIndex = nCurOperatCol * (int)ModuleDef.PalletMaxRow + nPltIdx;
                                            Pallet[nIndex].Type = PltType.OK;
                                            SaveRunData(SaveType.Pallet, nIndex);
                                        }
                                    }

                                    // 切换腔体状态
                                    SetCavityState(nCurOperatCol, CavityState.Standby);
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }

                                // 取放其他类型托盘
                                else
                                {
                                    this.nextAutoStep = AutoSteps.Auto_CheckPltState;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }

                            break;
                        }
                    case AutoSteps.Auto_CheckPltState:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列检查托盘状态", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col check Pallet State", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            if (UpdateOvenData(curCavityData))
                            {
                                // 交换硬件数据
                                //int nCurCol = (0 == nOvenGroup) ? nCurOperatCol : (1 - nCurOperatCol);
                                OvenPalletState pltState = CurCavityData(nCurOperatCol).PltState[nCurOperatRow];

                                //if (Def.IsNoHardware() || DryRun || OvenPalletState.Invalid != pltState)
                                {
                                    bool bHasPlt = (OvenPalletState.Have == pltState);
                                    bool bHasData = (GetPlt(nCurOperatCol, nCurOperatRow).Type > PltType.Invalid);

                                    if (!CavityDataSource[nCurOperatCol].OvenEnable || Def.IsNoHardware() || DryRun || (OvenPalletState.Invalid != pltState && bHasPlt == bHasData))
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_CloseOvenDoor;
                                        SaveRunData(SaveType.AutoStep);
                                        break;
                                    }
                                    else
                                    {
                                        string strMsg, strDisp, strPlt, strData;
                                        strPlt = bHasPlt ? "有" : "无";
                                        strData = bHasData ? "有" : "无";
                                        strDisp = "请停机检查炉腔中夹具状态！";
                                        strMsg = string.Format("{0}层{1}列炉腔中检测到{2}夹具，实际应该{3}夹具", nCurOperatRow + 1, nCurOperatCol + 1, strPlt, strData);
                                        ShowMessageBox((int)MsgID.CheckPallet, strMsg, strDisp, MessageType.MsgWarning);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_CloseOvenDoor:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列关闭炉门", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col close Oven door", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].DoorState = OvenDoorState.Close;
                            if (DryRun || OvenDoorOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                if (ModuleEvent.OvenPlaceWaitResultPlt == curRespEvent)
                                {
                                    this.nextAutoStep = AutoSteps.Auto_SetPressure;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                                else
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                }
                            }
                            else
                            {
                                if (CheckEvent(this, curRespEvent, EventState.Invalid))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                    break;
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_SetPressure:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列设置保压", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col Set Pressure", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            // 设置保压
                            setCavityData[nCurOperatCol].PressureState = CavityDataSource[nCurOperatCol].Pressure? OvenPressureState.Open : OvenPressureState.Close;
                            if (DryRun || OvenPressureOperate(nCurOperatCol, setCavityData[nCurOperatCol], false))
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                            }
                            break;
                        }
                    #endregion


                    #region // 启动流程

                    case AutoSteps.Auto_OvenWorkStop:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列停止", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col  work stop", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].WorkState = OvenWorkState.Stop;
                            if (DryRun || OvenStartOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                this.nextAutoStep = AutoSteps.Auto_PreCheckVacPressure;
                                SaveRunData(SaveType.AutoStep);
                            }
                            break;
                        }
                    case AutoSteps.Auto_PreCheckVacPressure:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列预先检查真空压力", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col pre check vac pressure", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            if (UpdateOvenData(curCavityData))
                            {
                                //int nCurCol = (0 == nOvenGroup) ? nCurOperatCol : (1 - nCurOperatCol);
                                bool bRes = bPickUsPreState ? CurCavityData(nCurOperatCol).BlowUsPreState == OvenBlowUsPreState.Have
                                      : CurCavityData(nCurOperatCol).unVacPressure >= unOpenDoorPressure;
                                if (DryRun || bRes)
                                {
                                    setCavityData[nCurOperatCol].BlowState = OvenBlowState.Close;
                                    if (DryRun || OvenBreakVacOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_SetOvenParameter;
                                    }
                                }
                                else
                                {
                                    if (OvenBlowState.Open != CurCavityData(nCurOperatCol).BlowState)
                                    {
                                        setCavityData[nCurOperatCol].BlowState = OvenBlowState.Open;
                                        OvenBreakVacOperate(nCurOperatCol, setCavityData[nCurOperatCol]);
                                    }
                                }
                            }
                            break;
                        }
                    case AutoSteps.Auto_SetOvenParameter:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列设置参数", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col set  parameter", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            //int nFromNo = nBakingType[nCurOperatCol] == (int)BakingType.Rebaking ? nReWorkFormNo : nNormalFormNo;
                            int nFromNo = this.nNormalFormNo;
                            string strInfo = string.Format("点击【确定】将选择正常流程，点击【取消】将选择返工流程!");
                            if (nBakingType[nCurOperatCol] == (int)BakingType.Rebaking)
                            {
                                if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                                {
                                    nFromNo = this.nNormalFormNo;
                                }
                                else
                                {
                                    nFromNo = this.nReWorkFormNo;
                                }
                            }
                            var allFormula = UserHelp.Db.Queryable<FormulaDb>().ToArray();
                            var nFormulaList = allFormula.GroupBy(f => f.Formulaid);
                            foreach (var fs in nFormulaList)
                            {
                                if (fs.Key == nFromNo)
                                {
                                    CavityData.PropListInfo.ForEach(fdb =>
                                    {
                                        var curfdb = fs.FirstOrDefault(f => f.ParamIndex == fdb.att.IndexPrarmeter);
                                        fdb.info.SetValue(setCavityData[nCurOperatCol].ProcessParam, Convert.ChangeType(curfdb.ParameterValue, fdb.info.PropertyType));
                                    });
                                }
                            }
                            nRunTime[nCurOperatCol] = unDryTime;

                            if (DryRun || OvenParamOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                nStartCount[nCurOperatCol] = 0;
                                this.nextAutoStep = AutoSteps.Auto_SetPreHeatVacBreath;
                                SaveRunData(SaveType.Variables);
                            }
                            break;
                        }
                    case AutoSteps.Auto_SetPreHeatVacBreath:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]层启动前设置预热真空呼吸", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] row set PreHeat Vac Breath", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].PreHeatBreathState = bPreHeatBreathEnable ? OvenPreHeatBreathState.Open : OvenPreHeatBreathState.Close;
                            setCavityData[nCurOperatCol].VacBreathState = bVacBreathEnable ? OvenVacBreathState.Open : OvenVacBreathState.Close;
                            if (DryRun || (OvenPreHeatBreathOperate(nCurOperatCol, setCavityData[nCurOperatCol]) && OvenVacBreathOperate(nCurOperatCol, setCavityData[nCurOperatCol])))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OvenWorkStart;
                            }

                            break;
                        }
                    case AutoSteps.Auto_OvenWorkStart:
                        {
                            this.msgChs = string.Format("干燥炉[{0}]列启动", nCurOperatCol + 1);
                            this.msgEng = string.Format("Oven [{0}] col work start", nCurOperatCol + 1);
                            CurMsgStr(this.msgChs, this.msgEng);

                            setCavityData[nCurOperatCol].WorkState = OvenWorkState.Start;
                            if (DryRun || OvenStartOperate(nCurOperatCol, setCavityData[nCurOperatCol]))
                            {
                                string strErr = "";
                                for (int nRow = 0; nRow < (int)ModuleDef.PalletMaxRow; nRow++)
                                {
                                    Pallet[(int)ModuleDef.PalletMaxRow * nCurOperatCol + nRow].StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                }
                                string strCode = "0";
                                MesOvenStartAndEnd(nCurOperatCol, nOvenID, strCode, "", ref strErr);
                                //if (!MesOvenStartAndEnd(nCurOperatCol, nOvenID, strCode, "", ref strErr))
                                //{
                                //    setCavityData[nCurOperatCol].WorkState = OvenWorkState.Stop;
                                //    OvenStartOperate(nCurOperatCol, setCavityData[nCurOperatCol]);
                                //    string strMsg = string.Format("Mes托盘开始失败，失败原因：{0}", strErr);
                                //    ShowMessageBox((int)MsgID.CheckMES, strMsg, "请查询MesLog", MessageType.MsgWarning);

                                //    CavityDataSource[nCurOperatCol].OvenEnable = false;                     // Mes托盘开始失败设置为禁用状态
                                //    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                //    SaveParameter();
                                //    SaveRunData(SaveType.AutoStep);
                                //    return;
                                //}
                                UpdateOvenData(bgCavityData);
                                CavityDataSource[nCurOperatCol].Plts.ForEach((Pallet pallet,int index)=> 
                                    RealDataHelp.AddOvenPositionInfo(
                                        new OvenPositionInfo(DateTime.Now, nRunTime[nCurOperatCol], this.strResourceID, this.GetOvenID(), (nCurOperatCol + 1), pallet.Code, index))
                                );
                                
                                MySQLMesOperationRecord(nCurOperatCol, "01", "自动运行");
                                arrStartTime[nCurOperatCol] = DateTime.Now;
                                arrVacStartTime[nCurOperatCol] = arrStartTime[nCurOperatCol];
                                arrVacStartValue[nCurOperatCol] = 0;
                                nBakingType[nCurOperatCol] = (int)BakingType.Normal;
                                SetCavityState(nCurOperatCol, CavityState.Work);

                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables | SaveType.Pallet);
                            }
                            else
                            {
                                nStartCount[nCurOperatCol]++;
                                if (nStartCount[nCurOperatCol] > 3)
                                {
                                    CavityDataSource[nCurOperatCol].OvenEnable = false;                     // 启动超时设置为禁用状态
                                    setCavityData[nCurOperatCol].WorkState = OvenWorkState.Stop;
                                    OvenStartOperate(nCurOperatCol, setCavityData[nCurOperatCol]);
                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveParameter();
                                    SaveRunData(SaveType.AutoStep);
                                }
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
            catch (Exception ex)
            {
                if (!bAutoAnomaly)
                {
                    MachineCtrl.GetInstance().WriteLog($"{RunName}AutoOperation()error {ex.Message}");
                }
                bAutoAnomaly = true;
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
            nCurOperatRow = 0;
            nCurOperatCol = 0;
            nCurCheckCol = 0;
            curEventState = EventState.Invalid;
            curRespEvent = ModuleEvent.ModuleEventInvalid;

            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                bgCavityData[nCavityIdx].Release();
                curCavityData[nCavityIdx].Release();
                setCavityData[nCavityIdx].Release();
                CavityDataSource[nCavityIdx].State = CavityState.Standby;
                bClearMaintenance[nCavityIdx] = false;
                fWaterContentValue[nCavityIdx, 0] = -1.0f;
                fWaterContentValue[nCavityIdx, 1] = -1.0f;
                fWaterContentValue[nCavityIdx, 2] = -1.0f;
                WCUploadStatus[nCavityIdx] = WCState.WCStateInvalid;
                nCurBakingTimes[nCavityIdx] = 1;
                nBakCount[nCavityIdx] = 0;
            }

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
            this.curRespEvent = (ModuleEvent)FileStream.ReadInt(section, "curRespEvent", (int)this.curRespEvent);
            this.nCurOperatRow = FileStream.ReadInt(section, "nCurOperatRow", this.nCurOperatRow);
            this.nCurOperatCol = FileStream.ReadInt(section, "nCurOperatCol", this.nCurOperatCol);
            this.NBakingOverBat = FileStream.ReadInt(section, "nBakingOverBat", this.NBakingOverBat);

            // 腔体状态
            for (int nIdx = 0; nIdx < CavityDataSource.Length; nIdx++)
            {
                key = string.Format("cavityState[{0}]", nIdx);
                CavityDataSource[nIdx].State = (CavityState)FileStream.ReadInt(section, key, (int)CavityDataSource[nIdx].State);
            }

            // 水含量上传状态
            for (int nIdx = 0; nIdx < WCUploadStatus.Length; nIdx++)
            {
                key = string.Format("WCUploadStatus[{0}]", nIdx);
                WCUploadStatus[nIdx] = (WCState)FileStream.ReadInt(section, key, (int)WCUploadStatus[nIdx]);
            }

            // 炉层烘烤开始时间
            for (int nIdx = 0; nIdx < arrStartTime.Length; nIdx++)
            {
                key = string.Format("arrStartTime[{0}]", nIdx);
                string str = "";
                str = FileStream.ReadString(section, key, "");
                if (!string.IsNullOrEmpty(str))
                {
                    arrStartTime[nIdx] = Convert.ToDateTime(str);
                }
            }

            // 炉层真空开始时间
            for (int nIdx = 0; nIdx < arrVacStartTime.Length; nIdx++)
            {
                key = string.Format("arrVacStartTime[{0}]", nIdx);
                string str = "";
                str = FileStream.ReadString(section, key, "");
                if (!string.IsNullOrEmpty(str))
                {
                    arrVacStartTime[nIdx] = Convert.ToDateTime(str);
                }
            }

            // 炉层真空第一次小于100Pa值
            for (int nIdx = 0; nIdx < arrVacStartValue.Length; nIdx++)
            {
                key = string.Format("arrVacStartValue[{0}]", nIdx);
                this.arrVacStartValue[nIdx] = FileStream.ReadInt(section, key, this.arrVacStartValue[nIdx]);
            }

            // 水含量值
            for (int nIdx = 0; nIdx < fWaterContentValue.GetLength(0); nIdx++)
            {
                key = string.Format("fWaterContentValue[{0}, 0]", nIdx);
                fWaterContentValue[nIdx, 0] = (float)FileStream.ReadDouble(section, key, fWaterContentValue[nIdx, 0]);
                key = string.Format("fWaterContentValue[{0}, 1]", nIdx);
                fWaterContentValue[nIdx, 1] = (float)FileStream.ReadDouble(section, key, fWaterContentValue[nIdx, 1]);
                key = string.Format("fWaterContentValue[{0}, 2]", nIdx);
                fWaterContentValue[nIdx, 2] = (float)FileStream.ReadDouble(section, key, fWaterContentValue[nIdx, 2]);
            }

            // 当前干燥次数
            for (int nIdx = 0; nIdx < nCurBakingTimes.Length; nIdx++)
            {
                key = string.Format("nCurBakingTimes[{0}]", nIdx);
                nCurBakingTimes[nIdx] = FileStream.ReadInt(section, key, nCurBakingTimes[nIdx]);
            }

            // Baking类型
            for (int nIdx = 0; nIdx < nBakingType.Length; nIdx++)
            {
                key = string.Format("nBakingType[{0}]", nIdx);
                nBakingType[nIdx] = FileStream.ReadInt(section, key, nBakingType[nIdx]);
            }

            // 加真空小于100PA时间，重新启动
            for (int nIdx = 0; nIdx < bStart.Length; nIdx++)
            {
                key = string.Format("bStart[{0}]", nIdx);
                bStart[nIdx] = FileStream.ReadBool(section, key, bStart[nIdx]);
            }

            // 参数运行时间
            for (int nIdx = 0; nIdx < nRunTime.Length; nIdx++)
            {
                key = string.Format("nRunTime[{0}]", nIdx);
                nRunTime[nIdx] = FileStream.ReadInt(section, key, nRunTime[nIdx]);
            }

            // 烘烤次数
            for (int nIdx = 0; nIdx < nBakCount.Length; nIdx++)
            {
                key = string.Format("nBakCount[{0}]", nIdx);
                nBakCount[nIdx] = FileStream.ReadInt(section, key, nBakCount[nIdx]);
            }

            // 上传水含量状态
            for (int nIdx = 0; nIdx < bUploadWaterStatus.Length; nIdx++)
            {
                key = string.Format("bUploadWaterStatus[{0}]", nIdx);
                bUploadWaterStatus[nIdx] = FileStream.ReadBool(section, key, bUploadWaterStatus[nIdx]);
            }

            // 干燥炉数据
            for (int nIdx = 0; nIdx < setCavityData.Length; nIdx++)
            {
                // 门状态
                key = string.Format("setCavityData[{0}].DoorState", nIdx);
                setCavityData[nIdx].DoorState = (OvenDoorState)FileStream.ReadInt(section, key, (int)setCavityData[nIdx].DoorState);

                // 干燥炉参数
/*                key = string.Format("setCavityData[{0}].unDryTime", nIdx);
                setCavityData[nIdx].unDryTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unDryTime);
                key = string.Format("setCavityData[{0}].unSetTempValue", nIdx);
                setCavityData[nIdx].unSetTempValue = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unSetTempValue);
                key = string.Format("setCavityData[{0}].unTempAddSv1", nIdx);
                setCavityData[nIdx].unTempAddSv1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unTempAddSv1);
                key = string.Format("setCavityData[{0}].unTempAddSv2", nIdx);
                setCavityData[nIdx].unTempAddSv2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unTempAddSv2);
                key = string.Format("setCavityData[{0}].unTempAddSv3", nIdx);
                setCavityData[nIdx].unTempAddSv3 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unTempAddSv3);
                key = string.Format("setCavityData[{0}].unTempAddSv4", nIdx);
                setCavityData[nIdx].unTempAddSv4 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unTempAddSv4);
                key = string.Format("setCavityData[{0}].unTempAddSv5", nIdx);
                setCavityData[nIdx].unTempAddSv5 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unTempAddSv5);

                key = string.Format("setCavityData[{0}].unStartTimeSv2", nIdx);
                setCavityData[nIdx].unStartTimeSv2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unStartTimeSv2);
                key = string.Format("setCavityData[{0}].unStartTimeSv3", nIdx);
                setCavityData[nIdx].unStartTimeSv3 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unStartTimeSv3);
                key = string.Format("setCavityData[{0}].unStartTimeSv4", nIdx);
                setCavityData[nIdx].unStartTimeSv4 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unStartTimeSv4);
                key = string.Format("setCavityData[{0}].unStartTimeSv5", nIdx);
                setCavityData[nIdx].unStartTimeSv5 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unStartTimeSv5);
                key = string.Format("setCavityData[{0}].unPreHeatPressLow1", nIdx);
                setCavityData[nIdx].unPreHeatPressLow1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unPreHeatPressLow1);
                key = string.Format("setCavityData[{0}].unPreHeatPressUp1", nIdx);
                setCavityData[nIdx].unPreHeatPressUp1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unPreHeatPressUp1);
                key = string.Format("setCavityData[{0}].unPreHeatStartTime2", nIdx);
                setCavityData[nIdx].unPreHeatStartTime2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unPreHeatStartTime2);

                key = string.Format("setCavityData[{0}].unPreHeatPressLow2", nIdx);
                setCavityData[nIdx].unPreHeatPressLow2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unPreHeatPressLow2);
                key = string.Format("setCavityData[{0}].unPreHeatPressUp2", nIdx);
                setCavityData[nIdx].unPreHeatPressUp2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unPreHeatPressUp2);
                key = string.Format("setCavityData[{0}].unHighVacStartTime", nIdx);
                setCavityData[nIdx].unHighVacStartTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime);
                key = string.Format("setCavityData[{0}].unHighVacFirTakeOutVacPress", nIdx);
                setCavityData[nIdx].unHighVacFirTakeOutVacPress = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacFirTakeOutVacPress);
                key = string.Format("setCavityData[{0}].unHighVacStartTime1", nIdx);
                setCavityData[nIdx].unHighVacStartTime1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime1);
                key = string.Format("setCavityData[{0}].unHighVacEndTime1", nIdx);
                setCavityData[nIdx].unHighVacEndTime1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacEndTime1);
                key = string.Format("setCavityData[{0}].unHighVacPressLow1", nIdx);
                setCavityData[nIdx].unHighVacPressLow1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacPressLow1);

                key = string.Format("setCavityData[{0}].unHighVacPressUp1", nIdx);
                setCavityData[nIdx].unHighVacPressUp1 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacPressUp1);
                key = string.Format("setCavityData[{0}].unHighVacStartTime2", nIdx);
                setCavityData[nIdx].unHighVacStartTime2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime2);
                key = string.Format("setCavityData[{0}].unHighVacEndTime2", nIdx);
                setCavityData[nIdx].unHighVacEndTime2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacEndTime2);
                key = string.Format("setCavityData[{0}].unHighVacTakeOutVacCycle2", nIdx);
                setCavityData[nIdx].unHighVacTakeOutVacCycle2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacTakeOutVacCycle2);
                key = string.Format("setCavityData[{0}].unHighVacTakeOutVacTime2", nIdx);
                setCavityData[nIdx].unHighVacTakeOutVacTime2 = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unHighVacTakeOutVacTime2);
                key = string.Format("setCavityData[{0}].unBreatStartTime", nIdx);
                setCavityData[nIdx].unBreatStartTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatStartTime);
                key = string.Format("setCavityData[{0}].unBreatEndTime", nIdx);
                setCavityData[nIdx].unBreatEndTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatEndTime);

                key = string.Format("setCavityData[{0}].unBreatTouchPress", nIdx);
                setCavityData[nIdx].unBreatTouchPress = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatTouchPress);
                key = string.Format("setCavityData[{0}].unBreatMinInterTime", nIdx);
                setCavityData[nIdx].unBreatMinInterTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatMinInterTime);
                key = string.Format("setCavityData[{0}].unBreatAirInfBackPress", nIdx);
                setCavityData[nIdx].unBreatAirInfBackPress = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatAirInfBackPress);
                key = string.Format("setCavityData[{0}].unBreatBackKeepTime", nIdx);
                setCavityData[nIdx].unBreatBackKeepTime = FileStream.ReadInt(section, key, (int)setCavityData[nIdx].unBreatBackKeepTime);*/
            }

            for (int nCol = 0; nCol < (int)ModuleDef.PalletMaxCol; nCol++)
            {
                // 最小真空值
                key = string.Format("nMinVacm[{0}]", nCol);
                nMinVacm[nCol] = FileStream.ReadInt(section, key, nMinVacm[nCol]);

                // 最大真空值
                key = string.Format("nMaxVacm[{0}]", nCol);
                nMaxVacm[nCol] = FileStream.ReadInt(section, key, nMaxVacm[nCol]);

                // 最小温度
                key = string.Format("nMinTemp[{0}]", nCol);
                nMinTemp[nCol] = FileStream.ReadInt(section, key, nMinTemp[nCol]);

                // 最大温度
                key = string.Format("nMaxTemp[{0}]", nCol);
                nMaxTemp[nCol] = FileStream.ReadInt(section, key, nMaxTemp[nCol]);

                // 当前真空
                key = string.Format("nOvenVacm[{0}]", nCol);
                nOvenVacm[nCol] = FileStream.ReadInt(section, key, nOvenVacm[nCol]);

                //当前温度
                key = string.Format("nOvenTemp[{0}]", nCol);
                nOvenTemp[nCol] = FileStream.ReadInt(section, key, nOvenTemp[nCol]);
            }

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
                FileStream.WriteInt(section, "curRespEvent", (int)this.curRespEvent);
                FileStream.WriteInt(section, "nCurOperatRow", this.nCurOperatRow);
                FileStream.WriteInt(section, "nCurOperatCol", this.nCurOperatCol);
                FileStream.WriteInt(section, "nBakingOverBat", this.NBakingOverBat);

                // 腔体状态
                for (int nIdx = 0; nIdx < CavityDataSource.Length; nIdx++)
                {
                    key = string.Format("cavityState[{0}]", nIdx);
                    FileStream.WriteInt(section, key, (int)CavityDataSource[nIdx].State);
                }

                // 水含量上传状态
                for (int nIdx = 0; nIdx < WCUploadStatus.Length; nIdx++)
                {
                    key = string.Format("WCUploadStatus[{0}]", nIdx);
                    FileStream.WriteInt(section, key, (int)WCUploadStatus[nIdx]);
                }

                // 炉层烘烤开始时间
                for (int nIdx = 0; nIdx < arrStartTime.Length; nIdx++)
                {
                    key = string.Format("arrStartTime[{0}]", nIdx);
                    FileStream.WriteString(section, key, arrStartTime[nIdx].ToString());
                }

                // 炉层真空开始时间
                for (int nIdx = 0; nIdx < arrVacStartTime.Length; nIdx++)
                {
                    key = string.Format("arrVacStartTime[{0}]", nIdx);
                    FileStream.WriteString(section, key, arrVacStartTime[nIdx].ToString());
                }

                // 炉层真空第一次小于100Pa值
                for (int nIdx = 0; nIdx < arrVacStartValue.Length; nIdx++)
                {
                    key = string.Format("arrVacStartValue[{0}]", nIdx);
                    FileStream.WriteInt(section, key, this.arrVacStartValue[nIdx]);
                }

                // 水含量值
                for (int nIdx = 0; nIdx < fWaterContentValue.GetLength(0); nIdx++)
                {
                    key = string.Format("fWaterContentValue[{0}, 0]", nIdx);
                    FileStream.WriteDouble(section, key, fWaterContentValue[nIdx, 0]);
                    key = string.Format("fWaterContentValue[{0}, 1]", nIdx);
                    FileStream.WriteDouble(section, key, fWaterContentValue[nIdx, 1]);
                    key = string.Format("fWaterContentValue[{0}, 2]", nIdx);
                    FileStream.WriteDouble(section, key, fWaterContentValue[nIdx, 2]);
                }

                // 当前干燥次数
                for (int nIdx = 0; nIdx < nCurBakingTimes.Length; nIdx++)
                {
                    key = string.Format("nCurBakingTimes[{0}]", nIdx);
                    FileStream.WriteInt(section, key, nCurBakingTimes[nIdx]);
                }

                // Baking类型
                for (int nIdx = 0; nIdx < nBakingType.Length; nIdx++)
                {
                    key = string.Format("nBakingType[{0}]", nIdx);
                    FileStream.WriteInt(section, key, nBakingType[nIdx]);
                }

                // 加真空小于100PA时间，重新启动
                for (int nIdx = 0; nIdx < bStart.Length; nIdx++)
                {
                    key = string.Format("bStart[{0}]", nIdx);
                    FileStream.WriteBool(section, key, bStart[nIdx]);
                }

                // 参数运行时间
                for (int nIdx = 0; nIdx < nRunTime.Length; nIdx++)
                {
                    key = string.Format("nRunTime[{0}]", nIdx);
                    FileStream.WriteInt(section, key, (int)nRunTime[nIdx]);
                }

                // 烘烤次数
                for (int nIdx = 0; nIdx < nBakCount.Length; nIdx++)
                {
                    key = string.Format("nBakCount[{0}]", nIdx);
                    FileStream.WriteInt(section, key, nBakCount[nIdx]);
                }

                // 上传水含量状态
                for (int nIdx = 0; nIdx < bUploadWaterStatus.Length; nIdx++)
                {
                    key = string.Format("bUploadWaterStatus[{0}]", nIdx);
                    FileStream.WriteBool(section, key, bUploadWaterStatus[nIdx]);
                }

                // 干燥炉数据
                for (int nIdx = 0; nIdx < setCavityData.Length; nIdx++)
                {
                    // 门状态
                    key = string.Format("setCavityData[{0}].DoorState", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].DoorState);

                    // 干燥炉参数
                  /*  key = string.Format("setCavityData[{0}].unDryTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unDryTime);
                    key = string.Format("setCavityData[{0}].unSetTempValue", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unSetTempValue);
                    key = string.Format("setCavityData[{0}].unTempAddSv1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unTempAddSv1);
                    key = string.Format("setCavityData[{0}].unTempAddSv2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unTempAddSv2);
                    key = string.Format("setCavityData[{0}].unTempAddSv3", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unTempAddSv3);
                    key = string.Format("setCavityData[{0}].unTempAddSv4", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unTempAddSv4);
                    key = string.Format("setCavityData[{0}].unTempAddSv5", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unTempAddSv5);

                    key = string.Format("setCavityData[{0}].unStartTimeSv2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unStartTimeSv2);
                    key = string.Format("setCavityData[{0}].unStartTimeSv3", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unStartTimeSv3);
                    key = string.Format("setCavityData[{0}].unStartTimeSv4", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unStartTimeSv4);
                    key = string.Format("setCavityData[{0}].unStartTimeSv5", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unStartTimeSv5);
                    key = string.Format("setCavityData[{0}].unPreHeatPressLow1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unPreHeatPressLow1);
                    key = string.Format("setCavityData[{0}].unPreHeatPressUp1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unPreHeatPressUp1);
                    key = string.Format("setCavityData[{0}].unPreHeatStartTime2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unPreHeatStartTime2);

                    key = string.Format("setCavityData[{0}].unPreHeatPressLow2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unPreHeatPressLow2);
                    key = string.Format("setCavityData[{0}].unPreHeatPressUp2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unPreHeatPressUp2);
                    key = string.Format("setCavityData[{0}].unHighVacStartTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime);
                    key = string.Format("setCavityData[{0}].unHighVacFirTakeOutVacPress", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacFirTakeOutVacPress);
                    key = string.Format("setCavityData[{0}].unHighVacStartTime1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime1);
                    key = string.Format("setCavityData[{0}].unHighVacEndTime1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacEndTime1);
                    key = string.Format("setCavityData[{0}].unHighVacPressLow1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacPressLow1);

                    key = string.Format("setCavityData[{0}].unHighVacPressUp1", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacPressUp1);
                    key = string.Format("setCavityData[{0}].unHighVacStartTime2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacStartTime2);
                    key = string.Format("setCavityData[{0}].unHighVacEndTime2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacEndTime2);
                    key = string.Format("setCavityData[{0}].unHighVacTakeOutVacCycle2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacTakeOutVacCycle2);
                    key = string.Format("setCavityData[{0}].unHighVacTakeOutVacTime2", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unHighVacTakeOutVacTime2);
                    key = string.Format("setCavityData[{0}].unBreatStartTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatStartTime);
                    key = string.Format("setCavityData[{0}].unBreatEndTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatEndTime);

                    key = string.Format("setCavityData[{0}].unBreatTouchPress", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatTouchPress);
                    key = string.Format("setCavityData[{0}].unBreatMinInterTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatMinInterTime);
                    key = string.Format("setCavityData[{0}].unBreatAirInfBackPress", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatAirInfBackPress);
                    key = string.Format("setCavityData[{0}].unBreatBackKeepTime", nIdx);
                    FileStream.WriteInt(section, key, (int)setCavityData[nIdx].unBreatBackKeepTime);*/
                }

            }

            if (SaveType.MaxMinValue == (SaveType.MaxMinValue & saveType))
            {
                for (int nCol = 0; nCol < (int)ModuleDef.PalletMaxCol; nCol++)
                {
                    // 最小真空值
                    key = string.Format("nMinVacm[{0}]", nCol);
                    FileStream.WriteInt(section, key, nMinVacm[nCol]);

                    // 最大真空值
                    key = string.Format("nMaxVacm[{0}]", nCol);
                    FileStream.WriteInt(section, key, nMaxVacm[nCol]);

                    // 最小温度
                    key = string.Format("nMinTemp[{0}]", nCol);
                    FileStream.WriteInt(section, key, nMinTemp[nCol]);

                    // 最大温度
                    key = string.Format("nMaxTemp[{0}]", nCol);
                    FileStream.WriteInt(section, key, nMaxTemp[nCol]);

                    key = string.Format("nOvenVacm[{0}]", nCol);
                    FileStream.WriteInt(section, key, nOvenVacm[nCol]);

                    key = string.Format("nOvenTemp[{0}]", nCol);
                    FileStream.WriteInt(section, key, nOvenTemp[nCol]);
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

            for (ModuleEvent eventIdx = ModuleEvent.OvenPlaceEmptyPlt; eventIdx < ModuleEvent.OvenEventEnd; eventIdx++)
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
            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                if (CavityDataSource[nColIdx].OvenEnable)
                {
                    string strInfo;
                    strInfo = string.Format("炉腔使能未关！\r\n请关闭炉腔使能再试，否则禁止删除任务");
                    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                    return false;
                }
                if ((OvenDoorState.Open == CurCavityData(nColIdx).DoorState) && (!Def.IsNoHardware()))
                {
                    string strInfo;
                    strInfo = string.Format("炉门未关！\r\n请关闭炉门再试，否则禁止删除任务");
                    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                    return false;
                }
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
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                SetCavityData(nCavityIdx).DoorState = OvenDoorState.Close;
            }
            SaveRunData(SaveType.AutoStep | SaveType.Variables | SaveType.SignalEvent);
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

            string strKey = "";
            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                CavityDataSource[nColIdx].OvenEnable = ReadParam(RunModule, "OvenEnable" + (nColIdx + 1), false);
                CavityDataSource[nColIdx].Pressure = ReadParam(RunModule, "Pressure" + (nColIdx + 1), false);
                nCirBakingTimes[nColIdx] = ReadParam(RunModule, "CirBakingTimes" + (nColIdx + 1), 1);
                bCavityClear[nColIdx] = ReadParam(RunModule, "CavityClear" + (nColIdx + 1), false);

                for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                {
                    strKey = string.Format("OvenCavityNg[{0},{1}]", nColIdx, nRowIdx);
                    bOvenCavityNg[nColIdx, nRowIdx] = ReadParam(RunModule, strKey, false);

                    strKey = string.Format("Transfer[{0},{1}]", nColIdx, nRowIdx);
                    bTransfer[nColIdx, nRowIdx] = ReadParam(RunModule, strKey, false);
                }
            }

            nNormalFormNo = ReadParam(RunModule, "NormalFormNo", nNormalFormNo);
            nReWorkFormNo = ReadParam(RunModule, "ReWorkFormNo", nReWorkFormNo);
            unVacBkBTime = (uint)ReadParam(RunModule, "VacBkBTime", (int)unVacBkBTime);

            unOpenDoorPressure = (uint)ReadParam(RunModule, "OpenDoorPressure", (int)unOpenDoorPressure);
            unOpenDoorDelayTime = (uint)ReadParam(RunModule, "OpenDoorDelayTime", (int)unOpenDoorDelayTime);
            dWaterStandard[0] = ReadParam(RunModule, "WaterStandard[0]", dWaterStandard[0]);
            dWaterStandard[1] = ReadParam(RunModule, "WaterStandard[1]", dWaterStandard[1]);
            dWaterStandard[2] = ReadParam(RunModule, "WaterStandard[2]", dWaterStandard[2]);
            strOvenIP = ReadParam(RunModule, "OvenIP", strOvenIP);
            nOvenPort = ReadParam(RunModule, "OvenPort", nOvenPort);
            nResouceUploadTime = ReadParam(RunModule, "ResouceUploadTime", nResouceUploadTime);
            bPickUsPreState = ReadParam(RunModule, "PickUsPreState", bPickUsPreState);
            strResourceID = ReadParam(RunModule, "StrResourceID", strResourceID);
            nPlaceFakeRow = ReadParam(RunModule, "PlaceFakeRow", nPlaceFakeRow);
            nBakMaxCount = ReadParam(RunModule, "BakMaxCount", nBakMaxCount);
            bPreHeatBreathEnable = ReadParam(RunModule, "PreHeatBreathEnable", bPreHeatBreathEnable);
            bVacBreathEnable = ReadParam(RunModule, "VacBreathEnable", bVacBreathEnable);
            return true;
        }

        /// <summary>
        /// 写入数据库参数
        /// </summary>
        public override void SaveParameter()
        {
            string strKey = "";
            for (int nColIdx = 0; nColIdx < (int)ModuleDef.PalletMaxCol; nColIdx++)
            {
                WriteParameterCode(RunModule, "OvenEnable" + (nColIdx + 1), CavityDataSource[nColIdx].OvenEnable.ToString());
                WriteParameterCode(RunModule, "Pressure" + (nColIdx + 1), CavityDataSource[nColIdx].Pressure.ToString());

                WriteParameterCode(RunModule, "CavityClear" + (nColIdx + 1), bCavityClear[nColIdx].ToString());

                for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                {
                    strKey = string.Format("OvenCavityNg[{0},{1}]", nColIdx, nRowIdx);
                    WriteParameterCode(RunModule, strKey, bOvenCavityNg[nColIdx, nRowIdx].ToString());

                    strKey = string.Format("Transfer[{0},{1}]", nColIdx, nRowIdx);
                    WriteParameterCode(RunModule, strKey, bTransfer[nColIdx, nRowIdx].ToString());
                }
            }
            base.SaveParameter();
        }

        /// <summary>
        /// 参数检查
        /// </summary>
        public override bool CheckParameter(string name, object value)
        {
            int nValue = 0;
            int nMax = 10000;
            int nMin = -500;
            switch (name)
            {
                case "DryTime":
                case "SetTempValue":
                case "unTempAddSv1":
                case "unTempAddSv2":
                case "unTempAddSv3":
                case "unTempAddSv4":
                case "unTempAddSv5":
                case "unStartTimeSv2":
                case "unStartTimeSv3":
                case "unStartTimeSv4":
                case "unStartTimeSv5":
                case "unPreHeatPressLow1":
                case "unPreHeatPressUp1":
                case "unPreHeatStartTime2":
                case "unPreHeatPressLow2":
                case "unPreHeatPressUp2":
                case "unHighVacStartTime":
                case "unHighVacFirTakeOutVacPress":
                case "unHighVacStartTime1":
                case "unHighVacEndTime1":
                case "unHighVacPressLow1":
                case "unHighVacPressUp1 ":
                case "unHighVacStartTime2 ":
                case "unHighVacEndTime2 ":
                case "unHighVacTakeOutVacCycle2 ":
                case "unHighVacTakeOutVacTime2 ":
                case "unBreatStartTime ":
                case "unBreatEndTime ":
                case "unBreatTouchPress ":
                case "unBreatMinInterTime ":
                case "unBreatAirInfBackPress ":
                case "unBreatBackKeepTime ":
                    {
                        nValue = (int)value;
                        if (nValue >= nMin && nValue <= nMax)
                        {
                            return true;
                        }
                        break;
                    }
                default:
                    {
                        return true;
                    }
            }
            ShowMsgBox.ShowDialog(string.Format("{0}参数最小值{1}，最大值{2}，修改值{3}，参数不在范围内，修改失败", name, nMin, nMax, nValue), MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// PageToParameter参数
        /// </summary>
        public void PageToParameter(int[] ArrPage, int nFormNo, string strFromName)
        {
            unDryTime = ArrPage[0];
            unSetTempValue = ArrPage[1];
            unTempAddSv1 = ArrPage[2];
            unTempAddSv2 = ArrPage[3];

            unTempAddSv3 = ArrPage[4];
            unTempAddSv4 = ArrPage[5];
            unTempAddSv5 = ArrPage[6];
            unStartTimeSv2 = ArrPage[7];
            unStartTimeSv3 = ArrPage[8];
            unStartTimeSv4 = ArrPage[9];
            unStartTimeSv5 = ArrPage[10];
            unPreHeatPressLow1 = ArrPage[11];
            unPreHeatPressUp1 = ArrPage[12];

            unPreHeatStartTime2 = ArrPage[13];
            unPreHeatPressLow2 = ArrPage[14];
            unPreHeatPressUp2 = ArrPage[15];
            unHighVacStartTime = ArrPage[16];
            unHighVacFirTakeOutVacPress = ArrPage[17];
            unHighVacStartTime1 = ArrPage[18];
            unHighVacEndTime1 = ArrPage[19];
            unHighVacPressLow1 = ArrPage[20];

            unHighVacPressUp1 = ArrPage[21];
            unHighVacStartTime2 = ArrPage[22];
            unHighVacEndTime2 = ArrPage[23];
            unHighVacTakeOutVacCycle2 = ArrPage[24];
            unHighVacTakeOutVacTime2 = ArrPage[25];
            unBreatStartTime = ArrPage[26];
            unBreatEndTime = ArrPage[27];

            unBreatTouchPress = ArrPage[28];
            unBreatMinInterTime = ArrPage[29];
            unBreatAirInfBackPress = ArrPage[30];
            unBreatBackKeepTime = ArrPage[31];

            //string sFilePath = "D:\\InterfaceOpetate\\PageToParameter";
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\PageToParameter", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "配方修改.CSV";
            string sColHead = "获取时间,模组名称,配方号,配方名称";
            string sLog = string.Format("{0},{1},{2},{3}"
                , DateTime.Now
                , RunName
                , nFormNo
                , strFromName);
            for (int i = 0; i < ArrPage.Length; i++)
            {
                sColHead += $",{DryingOvenDef.OvenParaName[i]}";
                sLog += $",{ArrPage[i]}";
            }

            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
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


        #region // 后台线程

        /// <summary>
        /// 初始化线程
        /// </summary>
        private bool InitThread()
        {
            try
            {
                if (null == bgThread)
                {
                    bIsRunThread = true;
                    bgThread = new Task(ThreadProc, TaskCreationOptions.LongRunning);
                    bgThread.Start();
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
                if (null != bgThread)
                {
                    bIsRunThread = false;
                    bgThread.Wait();
                    bgThread.Dispose();
                    bgThread = null;
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
        /// 线程入口函数
        /// </summary>
        private void ThreadProc()
        {
            while (bIsRunThread)
            {
                RunWhile();
                Sleep(1);
            }
        }

        /// <summary>
        /// 循环函数
        /// </summary>
        private void RunWhile()
        {
            // 连接断开时停止检查
            if (!DryRun && !OvenIsConnect())
            {
                if (CurConnectState)
                {
                    Sleep(3000);
                    if (!DryOvenConnect())
                    {
                        CurConnectState = false;
                        ShowMessageBox((int)MsgID.OvenConnectAlarm + nOvenID, RunName + "\r\n通讯连接已断开！！！", "请检查干燥炉通讯是否正常", MessageType.MsgWarning);
                    }
                }
                return;
            }

            // 更新数据
            if (!UpdateOvenData( bgCavityData))
            {
                Sleep(20);
                return;
            }

            bHeartBeat = !bHeartBeat;

            // 随机故障（测试用）
            if (Def.IsNoHardware() || DryRun)
            {
                RandomFaultState(bgCavityData);
            }

            // 干燥炉状态监控
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                //int nCurCol = (0 == nOvenGroup) ? nCavityIdx : (1 - nCavityIdx);
                if (CavityState.Work == GetCavityState(nCavityIdx))
                {
                    string strAlarmInfo = "";

                    //if (nRunTime[nCavityIdx] == 0)
                    //{
                    //    nRunTime[nCavityIdx] = unDryTime;
                    //}
                    string strErr = "";
                    nRunTime[nCavityIdx] = (int)(bgCavityData[nCavityIdx].ProcessParam.UnVacHeatTime + bgCavityData[nCavityIdx].ProcessParam.UnPreHeatTime);

                    if (!Def.IsNoHardware() && OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                    {
                        DateTime dwTime = DateTime.Now;
                        while ((DateTime.Now - dwTime).TotalSeconds < 10)
                        {
                            UpdateOvenData( bgCavityData);
                            if (OvenWorkState.Stop != bgCavityData[nCavityIdx].WorkState)
                            {
                                if (OvenWorkState.Invalid != bgCavityData[nCavityIdx].WorkState)
                                {
                                    MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "1", "", ref strErr);
                                }
                                else
                                {
                                    MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "2", "", ref strErr);
                                }
                                break;
                            }
                            Sleep(1);
                        }
                    }
                    
                    //过程参数
                    if ((DateTime.Now - processData[nCavityIdx]).TotalSeconds > 60)
                    {
                        processData[nCavityIdx] = DateTime.Now;
                        FTPUploadFile(bgCavityData[nCavityIdx], nCavityIdx, nOvenID);
                    }

                    // 破真空报警
                    if (bgCavityData[nCavityIdx].UnWorkTime < nRunTime[nCavityIdx] &&
                        OvenBlowAlarm.Alarm == bgCavityData[nCavityIdx].BlowAlarm && CavityDataSource[nCavityIdx].OvenEnable)
                    {
                        if (OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                        {
                            if(!BlowAlarm) 
                            {
                                BlowAlarm = true;
                                MesUpLoadAlarm("破真空报警"); 
                            }
                            CavityDataSource[nCavityIdx].OvenEnable = false;                  // 设置为禁用状态
                            SetCavityState(nCavityIdx, CavityState.Standby);
                            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "4", "4", ref strErr);
                        }
                        string msg = string.Format("{0}层破真空异常报警", nCavityIdx + 1);
                        ShowMessageBox((int)MsgID.AnomalyAlarm, msg, "请查看干燥炉真空状态是否正常", MessageType.MsgWarning, 10);
                    }
                    else if (BlowAlarm)
                    {
                        BlowAlarm = false;
                        MesUpLoadResetAlarm("破真空报警");
                    }
                    

                    // 真空报警
                    if (OvenVacAlarm.Alarm == bgCavityData[nCavityIdx].VacAlarm
                        && bgCavityData[nCavityIdx].unWorkTime < nRunTime[nCavityIdx] && CavityDataSource[nCavityIdx].OvenEnable)
                    {
                        if (OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                        {
                            if (!VacAlarm)
                            {
                                VacAlarm = true;
                                MesUpLoadAlarm("真空报警");
                            }
                            CavityDataSource[nCavityIdx].OvenEnable = false;                  // 设置为禁用状态
                            SetCavityState(nCavityIdx, CavityState.Standby);
                            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "4", "4", ref strErr);
                        }
                        strAlarmInfo = string.Format("干燥炉{0}\r\n第{1}层抽真空异常报警", nOvenID + 1, nCavityIdx + 1);
                        SaveParameter();
                        ShowMessageBox((int)MsgID.AnomalyAlarm, strAlarmInfo, "请查看干燥炉真空或真空泵状态是否正常", MessageType.MsgWarning, 10);
                    }
                    else if (VacAlarm)
                    {
                        VacAlarm = false;
                        MesUpLoadResetAlarm("真空报警");
                    }

                    // 真空开始时间, 真空第一次小于100Pa值[1] = {2020/9/13 9:39:58}
                    if (bgCavityData[nCavityIdx].UnVacPressure < 100 && arrStartTime[nCavityIdx] == arrVacStartTime[nCavityIdx])
                    {
                        arrVacStartTime[nCavityIdx] = DateTime.Now;
                        arrVacStartValue[nCavityIdx] = (int)bgCavityData[nCavityIdx].UnVacPressure;
                        SaveRunData(SaveType.Variables);
                    }

                    // 炉子报警
                    if (bgCavityData[nCavityIdx].UnWorkTime < nRunTime[nCavityIdx] &&
                        CheckOvenAlarm(nCavityIdx, bgCavityData[nCavityIdx], ref strAlarmInfo))
                    {
                        if (OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                        {
                            CavityDataSource[nCavityIdx].OvenEnable = false;     //设置为禁用状态
                            SaveParameter();
                            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "4", "4", ref strErr);
                        }
                        MySQLMesBakeAlarm(nCavityIdx, strAlarmInfo);
                        SetCavityState(nCavityIdx, CavityState.Standby);
                        ShowMessageBox((int)MsgID.AnomalyAlarm, strAlarmInfo, "请检查炉子！", MessageType.MsgWarning, 10);
                    }
                    // 发热板温度报警
                    else if (bgCavityData[nCavityIdx].UnWorkTime < nRunTime[nCavityIdx] &&
                        CheckTempAlarm(nCavityIdx, bgCavityData[nCavityIdx], ref strAlarmInfo))
                    {
                        if (OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                        {
                            CavityDataSource[nCavityIdx].OvenEnable = false;
                            SaveParameter();
                            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "4", "4", ref strErr);
                        }
                        MySQLMesBakeAlarm(nCavityIdx, strAlarmInfo);
                        SetCavityState(nCavityIdx, CavityState.Standby);
                        SaveRunData(SaveType.Variables);
                        ShowMessageBox((int)MsgID.AnomalyAlarm, strAlarmInfo, "请检查炉子！", MessageType.MsgAlarm, 60);

                    }
                    // 工作停止
                    else if (OvenWorkState.Stop == bgCavityData[nCavityIdx].WorkState)
                    {
                        if ((bgCavityData[nCavityIdx].UnWorkTime >= nRunTime[nCavityIdx] - 1 /*&& (bgCavityData[nCurCol].unVacTime == 0 ||MaxMinValueJudge(nCavityIdx))*/) || DryRun)
                        {
                            // 干燥完成
                            if (unVacBkBTime <= bgCavityData[nCavityIdx].unVacBkBTime)
                            {
                                if (MesOvenStartAndEnd(nCavityIdx, "02", "", ref strAlarmInfo))
                                {
                                    for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                                    {
                                        if (GetPlt(nCavityIdx, nRowIdx).IsType(PltType.OK))
                                        {
                                            // 切换托盘状态
                                            int nPltIdx = nCavityIdx * (int)ModuleDef.PalletMaxRow + nRowIdx;
                                            if (Pallet[nPltIdx].IsEmpty())
                                            {
                                                Pallet[nPltIdx].Type = PltType.OK;
                                            }
                                            else
                                            {
                                                Pallet[nPltIdx].Type = PltType.Detect;
                                            }
                                            Pallet[nPltIdx].EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                            SaveRunData(SaveType.Pallet, nPltIdx);

                                        }
                                    }

                                    //while (true)
                                    //{
                                    //    Sleep(1);
                                    //    if (MesOvenStartAndEnd(nCurOperatCol, "02", Convert.ToString(bgCavityData[nCavityIdx].unWorkTime), ref strAlarmInfo))
                                    //    {
                                    //        break;
                                    //    }
                                    //}
                                    // 切换腔体状态
                                    nBakCount[nCavityIdx]++;
                                    nBakingType[nCavityIdx] = (int)BakingType.Invalid;
                                    SetCavityState(nCavityIdx, CavityState.Detect);
                                    if (fWaterContentValue[nCavityIdx, 0] < 0 && fWaterContentValue[nCavityIdx, 1] < 0
                                        && fWaterContentValue[nCavityIdx, 2] < 0)
                                    {
                                        SetWCUploadStatus(nCavityIdx, WCState.WCStateUpLoad);
                                    }
                                    //自动上传水含量
                                    if (MachineCtrl.GetInstance().AutoUploadWaterValue)
                                    {
                                        fWaterContentValue[nCavityIdx, 0] = bgCavityData[nCavityIdx].fRealWCValue;
                                        fWaterContentValue[nCavityIdx, 1] = bgCavityData[nCavityIdx].fRealWCValue;
                                        fWaterContentValue[nCavityIdx, 2] = bgCavityData[nCavityIdx].fRealWCValue;
                                    }
                                    SaveRunData(SaveType.Variables);
                                    FTPUploadRealTimeTemp(nCavityIdx);
                                }
                            }
                            else
                            {
                                // 切换腔体状态
                                if (MesOvenStartAndEnd(nCavityIdx, "04", "", ref strAlarmInfo))
                                {
                                    nBakingType[nCavityIdx] = (int)BakingType.Rebaking;
                                    SetCavityState(nCavityIdx, CavityState.Standby);
                                    SaveRunData(SaveType.Variables);
                                    strAlarmInfo = RunName + "\r\n真空小于100PA时间低于标准值！";
                                    ShowMessageBox((int)MsgID.AnomalyAlarm, strAlarmInfo, "请检查炉子！", MessageType.MsgWarning, 10);
                                }
                            }

                            ///更新非烘烤时温度信息
                            CavityDataSource[nCavityIdx].Plts.ForEach((Pallet pallet, int index) =>
                                    RealDataHelp.AddOvenPositionInfo(
                                        new OvenPositionInfo(DateTime.Now, nRunTime[nCavityIdx], this.strResourceID, this.GetOvenID(), (nCavityIdx + 1), pallet.Code, index))
                                );
                        }
                        else
                        {
                            // 切换腔体状态
                            if (MesOvenStartAndEnd(nCavityIdx, "04", "", ref strAlarmInfo))
                            {
                                nBakingType[nCavityIdx] = (int)BakingType.Rebaking;
                                SetCavityState(nCavityIdx, CavityState.Standby);
                                SaveRunData(SaveType.Variables);
                                strAlarmInfo = RunName + $"\r\n烘烤时间{bgCavityData[nCavityIdx].UnWorkTime}小于设置烘烤总时长{nRunTime[nCavityIdx]}！";
                                ShowMessageBox((int)MsgID.AnomalyAlarm, strAlarmInfo, "请检查炉子！", MessageType.MsgWarning, 10);
                            }
                        }
                        MySQLMesOperationRecord(nCavityIdx, "02", "待机");
                        MachineCtrl.GetInstance().MesStateAndStopReasonUpload(this.strResourceID, "5", "", ref strErr);
                    }
                   /* else if ((Def.IsNoHardware() || OvenWorkState.Start == bgCavityData[nCurCol].WorkState)
                        && bgCavityData[nCurCol].unVacTime > 0) // 真空阶段时间 > 0
                    {
                        if ((DateTime.Now - dtTempStartTime[nCavityIdx]).TotalSeconds > nResouceUploadTime)
                        {
                            dtTempStartTime[nCavityIdx] = DateTime.Now;
                            UploadTempInfo(nCavityIdx, bgCavityData[nCurCol]);
                        }
                    }*/
                }
                // 维修状态
                else if (CavityState.Maintenance == GetCavityState(nCavityIdx))
                {
                    if (bClearMaintenance[nCavityIdx])
                    {
                        SetCavityState(nCavityIdx, CavityState.Standby);
                        bClearMaintenance[nCavityIdx] = false;
                        SaveRunData(SaveType.Variables);
                    }
                }
                //判断夹具是否有效并且不为空  //Baking完成后继续记录温度直到出炉 20210324
                bool bRes = (OvenWorkState.Start == bgCavityData[nCavityIdx].WorkState);
                for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                {
                    if (((bRes && Pallet[nCavityIdx * 8 + nRowIdx].Type == PltType.OK) || Pallet[nCavityIdx * 8 + nRowIdx].Type > PltType.NG) && !Pallet[nCavityIdx * 8 + nRowIdx].IsEmpty())
                    {
                        if ((DateTime.Now - dtResouceStartTime[nCavityIdx]).TotalSeconds > nResouceUploadTime)
                        {
                            //保存详细温度数据
                            SaveRealTimeTemp(nCavityIdx, (int)bgCavityData[nCavityIdx].unWorkTime, bgCavityData[nCavityIdx]);
                            dtResouceStartTime[nCavityIdx] = DateTime.Now;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 随机故障（测试用）
        /// </summary>
        private void RandomFaultState(CavityData[] data)
        {
            for (int nColIdx = 0; nColIdx < (int)ModuleRowCol.DryingOvenCol; nColIdx++)
            {
                if (CavityState.Work == GetCavityState(nColIdx))
                {
                    if (((TimeSpan)(DateTime.Now - arrStartTime[nColIdx])).TotalMilliseconds > 1 * 1000)
                    {
                        //int nCurCol = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
                        data[nColIdx].WorkState = OvenWorkState.Stop;
                    }
                }
            }
        }

        /// <summary>
        /// 检查炉子报警
        /// </summary>
        private bool CheckOvenAlarm(int nIndex, CavityData data, ref string strAlarmMsg)
        {
            if (nIndex < 0 || data == null)
            {
                return false;
            }

            bool bReturn = false;
            string strMsg;
            strMsg = string.Format("干燥炉{0}\r\n", nOvenID + 1);

            if (data.DoorAlarm == OvenDoorAlarm.Alarm)
            {
                bReturn = true;
                strMsg += "炉门报警";
            }
            else if (data.TakeOutBlowAlarm == OvenTakeOutBlowAlarm.Alarm)
            {
                bReturn = true;
                strMsg += "抽充报警";
            }
            else if (data.RasterAlarm == OvenRasterAlarm.Alarm)
            {
                bReturn = true;
                strMsg += "光栅报警";
            }
            else if (data.TempConlAlarm == OvenTempConlAlarm.Alarm)
            {
                bReturn = true;
                strMsg += "温控报警";
            }
            else if (data.PressStopAlarm == OvenPressStopAlarm.Alarm)
            {
                bReturn = true;
                strMsg += "压力急停报警";
            }
            //else if (data.OtherAlarm[0] == OvenOtherAlarm.OpenCloseBright)
            //{
            //    bReturn = true;
            //    strMsg += "开关门传感器同时亮";
            //}
            //else if (data.OtherAlarm[1] == OvenOtherAlarm.VacZero)
            //{
            //    bReturn = true;
            //    strMsg += "真空变送器电路异常报警";
            //}

            strAlarmMsg = strMsg;
            return bReturn;
        }

        /// <summary>
        /// 检查温度报警
        /// </summary>
        private bool CheckTempAlarm(int nIndex, CavityData data, ref string strAlarmMsg)
        {
            if (nIndex < 0 || data == null)
            {
                return false;
            }

            bool bReturn = false;
            string strTmp;
            string strMsg;
            strMsg = string.Format("干燥炉{0}\r\n", nOvenID + 1);

            if (data.unAlarmTempState != null)
            {
                for (int nAlarmType = 0; nAlarmType < 5; nAlarmType++)
                {
                    for (int nPltIdx = 0; nPltIdx < (int)ModuleRowCol.DryingOvenRow; nPltIdx++)
                    {
                        for (int nPanelIdx = 0; nPanelIdx < (int)DryOvenNumDef.HeatPanelNum; nPanelIdx++)
                        {
                            string strCode = "";
                            string strJigIndex = "";
                            double dCurAlarmTemp = data.unAlarmTempValue[nPltIdx][nPanelIdx];  //查询温度报警故障UnAlarmTempValue
                            OvenTempAlarm tempAlarmState = data.unAlarmTempState[nPltIdx][nPanelIdx][nAlarmType];//UnAlarmTempState
                            if (tempAlarmState == (OvenTempAlarm)1 && dCurAlarmTemp >= 0)
                            {
                                //int Idx = nOvenGroup == 0 ? nIndex : 1 - nIndex;
                                int nnPltIdx = nIndex * (int)ModuleDef.PalletMaxRow + nPltIdx;
                                strCode = Pallet[nnPltIdx].Code;
                                strJigIndex = string.Format("{0}号治具", nPltIdx + 1);
                                strTmp = string.Format("第{0}列--{1}{2}第{3}块发热板 温度--{4}({5}报警)\r\n", nIndex + 1, strJigIndex, strCode, nPanelIdx + 1, dCurAlarmTemp, DryingOvenDef.OvenAlarmTypeName[nAlarmType]);

                                strMsg += strTmp;
                                bReturn = true;
                                //bOvenCavityNg[nIndex, nPltIdx] = true;// 设置为NG状态
                                //SaveParameter();
                                Pallet[nnPltIdx].Type = PltType.NG;
                                SaveRunData(SaveType.Pallet, nnPltIdx);
                            }
                            if (bReturn) break;
                        }
                        if (bReturn) break;
                    }
                    if (bReturn) break;
                }
            }
            else if (TempAlarm)
            {
                TempAlarm = false;
                MesUpLoadResetAlarm("低温报警");
            }

            if (bReturn)
            {
                //发送停止信号TempAlarm
                if (setCavityData[nIndex].WorkState == OvenWorkState.Start)
                {
                    setCavityData[nIndex].WorkState = OvenWorkState.Stop;
                    OvenStartOperate(nIndex, setCavityData[nIndex], false);
                }
                if (!TempAlarm)
                {
                    TempAlarm = true;
                    MesUpLoadAlarm("低温报警");
                }
            }
            strAlarmMsg = strMsg;
            return bReturn;
        }

        /// <summary>
        /// 电池打NG
        /// </summary>
        private bool JudgeBatteryIsNG(int nIndex, Pallet pPallet, CavityData data, int nPanelIdx, bool bNgTurnTable = true)
        {
            if (nIndex < 0 || pPallet == null || data == null)
            {
                return false;
            }

            int nPltRow = 0;
            int nPltCol = 0;
            int nPltFakeRow = 0;
            int nPltFakeCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);


            if (bNgTurnTable)
            {
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                    {
                        if (pPallet.Bat[nRowIdx, nColIdx].Type == BatType.OK)
                        {
                            pPallet.Bat[nRowIdx, nColIdx].Type = BatType.NG;
                        }

                        if (pPallet.Bat[nRowIdx, nColIdx].Type == BatType.Fake)
                        {
                            CavityDataSource[nIndex].OvenEnable = false;  // 有假电池，设置为禁用状态 
                            SetCavityState(nIndex, CavityState.Standby);
                        }
                    }
                }
                pPallet.Type = PltType.NG;
            }

            SaveRunData(SaveType.Pallet, nIndex);
            return true;
        }

        /// <summary>
        /// 腔体禁用
        /// </summary>
        private bool CavityForbid(int nIndex, Pallet pPallet, CavityData data)
        {
            if (nIndex < 0 || pPallet == null || data == null)
            {
                return false;
            }

            int nPltRow = 0;
            int nPltCol = 0;
            int nPltFakeRow = 0;
            int nPltFakeCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);


            for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                {
                    if (pPallet.Bat[nRowIdx, nColIdx].Type == BatType.Fake)
                    {
                        CavityDataSource[nIndex].OvenEnable = false;  // 有假电池，设置为禁用状态 
                        SetCavityState(nIndex, CavityState.Standby);
                    }
                }
            }
            return true;
        }

        #endregion


        #region // 干燥炉操作

        /// <summary>
        /// 干燥炉连接
        /// </summary>
        public bool DryOvenConnect(bool bConnect = true)
        {
            if (bConnect)
            {
                if (ovenClient.Connect(strOvenIP, nOvenPort, nLocalNode))
                {
                    CurConnectState = true;
                    return true;
                }
                return false;
            }
            else
            {
                CurConnectState = false;
                return ovenClient.Disconnect();
            }
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool OvenIsConnect()
        {
            return ovenClient.IsConnect();
        }

        /// <summary>
        /// 干燥炉IP
        /// </summary>
        public bool OvenIPInfo(ref string strIP, ref int nPort)
        {
            strIP = strOvenIP;
            nPort = nOvenPort;
            return true;
        }

        /// <summary>
        /// 炉门操作
        /// </summary>
        public bool OvenDoorOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.DoorOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < unOpenDoorDelayTime)
                {
                    UpdateOvenData(curCavityData);
                    if (data.DoorState == CurCavityData(nColIdx).DoorState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenDoorState.Open == data.DoorState);
                    strDisp = "请检查干燥炉炉门状态";
                    strMsg = string.Format("{0}列炉门{1}超时", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
            }

            return false;
        }

        /// <summary>
        /// 抽真空
        /// </summary>
        public bool OvenVacOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.VacOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if (data.VacState == CurCavityData(nColIdx).VacState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenVacState.Open == data.VacState);
                    strDisp = "请检查干燥炉真空阀状态";
                    strMsg = string.Format("{0}列真空阀{1}超时", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }

        /// <summary>
        /// 破真空
        /// </summary>
        public bool OvenBreakVacOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.BreakVacOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if (data.BlowState == CurCavityData(nColIdx).BlowState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenBlowState.Open == data.BlowState);
                    strDisp = "请检查干燥炉破真空阀状态";
                    strMsg = string.Format("{0}列破真空阀{1}超时", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }

        /// <summary>
        /// 保压操作
        /// </summary>
        public bool OvenPressureOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (!ovenClient.SetDryOvenData(DryOvenCmd.PressureOperate, nColIdx, data))
            {
                if (bAlarm)
                {
                    string strMsg, strDisp;
                    bool bOpen = (OvenPressureState.Open == data.PressureState);
                    strDisp = "请检查干燥炉状态";
                    strMsg = string.Format("{0}列保压{1}失败", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 故障复位操作
        /// </summary>
        public bool OvenFaultResetOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (!ovenClient.SetDryOvenData(DryOvenCmd.FaultReset, nColIdx, data))
            {
                if (bAlarm)
                {
                    string strMsg = "故障复位失败";
                    string strDisp = "请检查干燥炉状态";
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 预热呼吸
        /// </summary>
        public bool OvenPreHeatBreathOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenRow || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;


            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.PreHeatBreathOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if (data.PreHeatBreathState == CurCavityData(nColIdx).PreHeatBreathState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenPreHeatBreathState.Open == data.PreHeatBreathState);
                    strDisp = "请检查干燥炉预热呼吸状态";
                    strMsg = string.Format("{0}层预热呼吸{1}超时", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OvenPreHeatBreathState, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }

        /// <summary>
        /// 真空呼吸
        /// </summary>
        public bool OvenVacBreathOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenRow || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;


            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.VacBreathOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if (data.VacBreathState == CurCavityData(nColIdx).VacBreathState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenVacBreathState.Open == data.VacBreathState);
                    strDisp = "请检查干燥真空呼吸状态";
                    strMsg = string.Format("{0}层真空呼吸{1}超时", nColIdx + 1, bOpen ? "打开" : "关闭");
                    ShowMessageBox((int)MsgID.OvenVacBreathState, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }

        /// <summary>
        /// 上位机
        /// 设置
        /// </summary>
        public bool OvenPcSafeDoorState(PCSafeDoorState nState)
        {
            if (DryRun || !IsModuleEnable())
            {
                return true;
            }

            int nIndex = (int)ModuleRowCol.DryingOvenCol - 1;
            setCavityData[nIndex].PcSafeDoorState = nState;
            if (OvenPcSafeDoorOperate(nIndex, setCavityData[nIndex]))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 上位机安全门操作
        /// </summary>
        public bool OvenPcSafeDoorOperate(int nIndex, CavityData data, bool bAlarm = true)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            if (!ovenClient.SetDryOvenData(DryOvenCmd.PCSafeDoorState, nIndex, data))
            {
                if (bAlarm)
                {
                    setOvenCount++;
                    string strMsg = "安全门状态设置失败";
                    string strDisp = "请检查干燥炉状态";
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning, 10);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 配方设置
        /// </summary>
        public bool OvenFormulaOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            UpdateOvenData( bgCavityData);
            if (OvenWorkState.Start == bgCavityData[nColIdx].WorkState)
            {
                strDisp = "干燥炉工作中";
                strMsg = string.Format("{0}列炉腔禁止参数设置", nColIdx + 1);
                ShowMessageBox((int)MsgID.CheckOperate, strMsg, strDisp, MessageType.MsgWarning);
                return false;
            }

            if (!ovenClient.SetDryOvenData(DryOvenCmd.WriteParam, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if ((uint)data.FormulaSet == CurCavityData(nColIdx).UnCurFormulaNo)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    strDisp = "请检查干燥炉";
                    strMsg = string.Format("{0}列炉腔配方设置失败", nColIdx + 1);
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }

        /// <summary>
        /// 启动操作
        /// </summary>
        public bool OvenStartOperate(int nColIdx, CavityData data, bool bAlarm = true)
        {
            if (nColIdx < 0 || nColIdx >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            //int nIndex = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            if (ovenClient.SetDryOvenData(DryOvenCmd.StartOperate, nColIdx, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 20)
                {
                    UpdateOvenData(curCavityData);
                    if (data.WorkState == CurCavityData(nColIdx).WorkState)
                    {
                        return true;
                    }
                    Sleep(1);
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenWorkState.Start == data.WorkState);
                    strDisp = "请检查干燥炉工作状态";
                    strMsg = string.Format("{0}列{1}超时", nColIdx + 1, bOpen ? "启动" : "停止");
                    ShowMessageBox((int)MsgID.OperateFail, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }
        /// <summary>
        /// 参数操作
        /// </summary>
        public bool OvenParamOperate(int nIndex, CavityData data, bool bAlarm = true)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleRowCol.DryingOvenCol || null == data)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            UpdateOvenData(bgCavityData);
            if (OvenWorkState.Start == bgCavityData[nIndex].WorkState)
            {
                strDisp = "干燥炉工作中";
                strMsg = string.Format("{0}层炉腔禁止参数设置", nIndex + 1);
                ShowMessageBox((int)MsgID.CheckOperate + nOvenID, strMsg, strDisp, MessageType.MsgWarning);
                return false;
            }

            if (ovenClient.SetDryOvenData(DryOvenCmd.WriteParam, nIndex, data))
            {
                while ((DateTime.Now - startTime).TotalSeconds < 15)
                {
                    //UpdateOvenData(ref curCavityData);
                    var cavityData = CurCavityData(nIndex);
                    //var ispropSet = false;
                    if (propListInfo.All(cavitydata =>
                    {
                        var ovenprop = CavityData.PropListInfo.First(reinfo => reinfo.att.IndexPrarmeter == cavitydata.att.IndexPrarmeter);
                        return Convert.ChangeType(cavitydata.info.GetValue(data.ProcessParam), ovenprop.info.PropertyType).Equals(ovenprop.info.GetValue(cavityData.ProcessParam));
                    }))
                        return true;
                }

                if (bAlarm)
                {
                    bool bOpen = (OvenWorkState.Start == data.WorkState);
                    strDisp = "请检查上位机参数是否符合干燥炉本地参数上下限";
                    strMsg = string.Format("{0}层炉腔参数设置超时", nIndex + 1);
                    ShowMessageBox((int)MsgID.OperateFail + nOvenID, strMsg, strDisp, MessageType.MsgWarning);
                }
            }
            return false;
        }
        /// <summary>
        /// 托盘检查
        /// </summary>
        public override bool CheckPallet(int nPltIdx, bool bHasPlt, bool bAlarm = true)
        {
            if (Def.IsNoHardware() || DryRun)
            {
                return true;
            }

            if (nPltIdx < 0 || nPltIdx >= (int)ModuleMaxPallet.DryingOven)
            {
                return false;
            }

            string strMsg, strDisp;
            DateTime startTime = DateTime.Now;

            int nRowIdx = nPltIdx % (int)ModuleDef.PalletMaxRow;
            int nColIdx = nPltIdx / (int)ModuleDef.PalletMaxRow;
            //int nCurCol = (0 == nOvenGroup) ? nColIdx : (1 - nColIdx);
            OvenPalletState pltState = bHasPlt ? OvenPalletState.Have : OvenPalletState.Not;

            // 1秒的检查超时
            while ((DateTime.Now - startTime).TotalSeconds < 1)
            {
                UpdateOvenData(curCavityData);
                if (pltState == CurCavityData(nColIdx).PltState[nRowIdx])
                {
                    return true;
                }
                Sleep(1);
            }

            if (bAlarm)
            {
                bool bHas = (OvenPalletState.Have == pltState);
                strDisp = "请检查干燥炉托盘状态";
                strMsg = string.Format("检查{0}列{1}#托盘超时", nRowIdx + 1, nColIdx + 1);
                ShowMessageBox((int)MsgID.CheckPallet, strMsg, strDisp, MessageType.MsgAlarm);
            }
            return false;
        }

        #endregion

        #region 界面操作炉子
        public void ManualAction(int curCavityIdx, DryOvenCmd cmd, bool param)
        {
            bool result = false;
            string strInfo = "";
            CavityData cavityData = new();

            if (!this.OvenIsConnect())
            {
                ShowMsgBox.ShowDialog("请先连接干燥炉！", MessageType.MsgWarning);
                return;
            }

            // 联机检查
            if (this.CurCavityData(curCavityIdx).OnlineState != OvenOnlineState.Have)
            {
                strInfo = string.Format("\r\n检测到{0}第{1}层为本地状态..禁止操作！", this.RunName, curCavityIdx + 1);
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgMessage);
                return;
            }

            switch (cmd)
            {
                // 工艺参数（写）
                case DryOvenCmd.WriteParam:
                    {
                        int nFromNo;
                        strInfo = string.Format("点击【确定】将选择正常流程，点击【取消】将选择返工流程!");
                        if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
                        {
                            nFromNo = this.nNormalFormNo;
                        }
                        else
                        {
                            nFromNo = this.nReWorkFormNo;
                        }

                        var allFormula = UserHelp.Db.Queryable<FormulaDb>().ToArray();
                        var nFormulaList = allFormula.GroupBy(f => f.Formulaid);
                        foreach (var fs in nFormulaList)
                        {
                            if (fs.Key == nFromNo)
                            {
                                CavityData.PropListInfo.ForEach(fdb =>
                                {
                                    var curfdb = fs.FirstOrDefault(f => f.ParamIndex == fdb.att.IndexPrarmeter);
                                    fdb.info.SetValue(cavityData.ProcessParam, Convert.ChangeType(curfdb.ParameterValue, fdb.info.PropertyType));
                                });
                            }
                        }

                        result = this.OvenParamOperate(curCavityIdx, cavityData, false);
                        strInfo = "设置参数" + (result ? "成功" : "失败");
                        break;

                    }
                // 启动操作启动/停止（写）
                case DryOvenCmd.StartOperate:
                    {
                        OvenWorkState ovenCmdState = param ? OvenWorkState.Start : OvenWorkState.Stop;
                        if (OvenWorkState.Start == ovenCmdState)
                        {
                            if (this.GetCavityState(curCavityIdx) != CavityState.Standby)
                            {
                                strInfo = string.Format("炉腔状态不在等待加热状态，禁止启动！");
                                break;
                            }
                        }
                        cavityData.WorkState = ovenCmdState;
                        result = this.OvenStartOperate(curCavityIdx, cavityData, false);
                        strInfo = ((OvenWorkState.Start == ovenCmdState) ? "干燥炉启动" : "干燥炉停止") + (result ? "成功" : "失败");
                        break;
                    }
                // 炉门操作打开/关闭（写）
                case DryOvenCmd.DoorOperate:
                    {
                        cavityData.DoorState = param ? OvenDoorState.Open : OvenDoorState.Close;
                        result = this.OvenDoorOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开炉门" : "关闭炉门") + (result ? "成功" : "失败");
                        break;
                    }
                // 真空操作打开/关闭（写）
                case DryOvenCmd.VacOperate:
                    {
                        cavityData.VacState = param ? OvenVacState.Open : OvenVacState.Close;
                        result = this.OvenVacOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开真空" : "关闭真空") + (result ? "成功" : "失败");
                        break;
                    }
                // 破真空操作打开/关闭（写）
                case DryOvenCmd.BreakVacOperate:
                    {
                        cavityData.BlowState = param ? OvenBlowState.Open : OvenBlowState.Close;
                        result = this.OvenBreakVacOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开破真空" : "关闭破真空") + (result ? "成功" : "失败");
                        break;
                    }
                // 保压打开/关闭（写）
                case DryOvenCmd.PressureOperate:
                    {
                        cavityData.PressureState = param ? OvenPressureState.Open : OvenPressureState.Close;
                        result = this.OvenPressureOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开保压" : "关闭保压") + (result ? "成功" : "失败");
                        if (result)
                        {
                            this.SetPressure(curCavityIdx, param);
                        }
                        break;
                    }
                // 故障复位（写）
                case DryOvenCmd.FaultReset:
                    {
                        cavityData.FaultReset = OvenResetState.Reset;
                        result = this.OvenFaultResetOperate(curCavityIdx, cavityData, false);
                        System.Threading.Thread.Sleep(1000);
                        cavityData.FaultReset = OvenResetState.Reset0;
                        result = this.OvenFaultResetOperate(curCavityIdx, cavityData, false);
                        strInfo = "故障复位" + (result ? "成功" : "失败");
                        break;
                    }
/*                // 预热呼吸打开/关闭（写）
                case DryOvenCmd.PreHeatBreathOperate:
                    {
                        cavityData.PreHeatBreathState = param ? OvenPreHeatBreathState.Open : OvenPreHeatBreathState.Close;
                        result = this.OvenPreHeatBreathOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开预热呼吸" : "关闭预热呼吸") + (result ? "成功" : "失败");
                        break;
                    }
                // 真空呼吸打开/关闭（写）
                case DryOvenCmd.VacBreathOperate:
                    {
                        cavityData.VacBreathState = param ? OvenVacBreathState.Open : OvenVacBreathState.Close;
                        result = this.OvenVacBreathOperate(curCavityIdx, cavityData, false);
                        strInfo = (param ? "打开真空呼吸" : "关闭真空呼吸") + (result ? "成功" : "失败");
                        break;
                    }*/
            }
        /*    DryingOvenOpetateDateCsv(strInfo);*/
            ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
        }
        #endregion

        #region // 干燥炉数据操作

        /// <summary>
        /// 更新干燥炉数据
        /// </summary>
        public bool UpdateOvenData(CavityData[] cavityData)
        {
            Sleep(100);
            if (null == cavityData)
            {
                return false;
            }

            for (int nCavityIdx = 0; nCavityIdx < cavityData.Length; nCavityIdx++)
            {
                ovenClient.GetDryOvenData(nCavityIdx, cavityData[nCavityIdx]);
            }
            ObservableCollection<ObservableCollection<float>> s = null;
            if(GetOvenID() == 0)
                 s = cavityData[0].UnBaseTempValue[0];

            return true;
        }

        /// <summary>
        /// 当前腔体数据
        /// </summary>
        public CavityData CurCavityData(int nIndex)
        {
            if (nIndex < 0 || nIndex >= curCavityData.Length)
            {
                return null;
            }
            UpdateOvenData(curCavityData);
            return curCavityData[nIndex];
        }

        /// <summary>
        /// 设置的腔体数据
        /// </summary>
        private CavityData SetCavityData(int nIndex)
        {
            if (nIndex < 0 || nIndex >= setCavityData.Length)
            {
                return null;
            }
            return setCavityData[nIndex];
        }

        /// <summary>
        /// 获取托盘数据
        /// </summary>
        public Pallet GetPlt(int nColIdx, int nRowIdx)
        {
            if (nRowIdx < 0 || nRowIdx >= (int)ModuleDef.PalletMaxRow ||
                nColIdx < 0 || nColIdx >= (int)ModuleDef.PalletMaxCol)
            {
                return null;
            }
            return Pallet[nColIdx * (int)ModuleDef.PalletMaxRow + nRowIdx];
        }


        /// <summary>
        /// 获取参数（调试界面用）
        /// </summary>
        public bool GetOvenParam(ref CavityData data)
        {
            foreach (var (info, att) in CavityData.PropListInfo)
            {
                info.SetValue(data.ProcessParam, propListInfo.First(_info => _info.att.IndexPrarmeter == att.IndexPrarmeter).info.GetValue(this.processParam));
            }
            return true;

        }

        #endregion


        #region // 状态检查

        /// <summary>
        /// 检查水含量
        /// </summary>
        private bool CheckWaterContent(float[,] fTestValue, ref int nIndex)
        {
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                //int nCol = (0 == nOvenGroup) ? nCavityIdx : (1 - nCavityIdx);
                // 测试使用
                if (true && Def.IsNoHardware() && CavityState.WaitRes == GetCavityState(nCavityIdx) && MachineCtrl.GetInstance().AutoUploadWaterValue)
                {
                    //fTestValue[nCavityIdx, 0] = 101.0f;
                    //fTestValue[nCavityIdx, 1] = 102.0f;
                    //fTestValue[nCavityIdx, 2] = 103.0f;
                    //fTestValue[nCavityIdx, 0] = curCavityData[nCol].fRealWCValue;
                    //fTestValue[nCavityIdx, 1] = curCavityData[nCol].fRealWCValue;
                    //fTestValue[nCavityIdx, 2] = curCavityData[nCol].fRealWCValue;
                }
                bool bRes = false;
                switch (MachineCtrl.GetInstance().eWaterMode)
                {
                    case WaterMode.混合型:
                        {
                            bRes = fTestValue[nCavityIdx, 0] > 0.0f;
                            break;
                        }
                    case WaterMode.阳极:
                        {
                            bRes = fTestValue[nCavityIdx, 1] > 0.0f;
                            break;
                        }
                    case WaterMode.阴极:
                        {
                            bRes = fTestValue[nCavityIdx, 2] > 0.0f;
                            break;
                        }
                    case WaterMode.阴阳极:
                        {
                            bRes = ((fTestValue[nCavityIdx, 1] > 0.0f) && (fTestValue[nCavityIdx, 2] > 0.0f));
                            break;
                        }
                    default:
                        break;
                }

                if (CavityState.WaitRes == GetCavityState(nCavityIdx) && bRes)
                {
                    for (int nRowIdx = 0; nRowIdx < (int)ModuleDef.PalletMaxRow; nRowIdx++)
                    {
                        if (GetPlt(nCavityIdx, nRowIdx).IsType(PltType.WaitRes) &&
                            GetPlt(nCavityIdx, nRowIdx).IsStage(PltStage.Onload))
                        {
                            nIndex = nCavityIdx;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查水含量是否超标
        /// </summary>
        private bool CheckWater(float[,] fTestValue, int nCavityIdx)
        {
            bool bRes = false;
            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        bRes = (fTestValue[nCavityIdx, 0] + fTestValue[nCavityIdx, 1]) <= dWaterStandard[0];
                        break;
                    }
                case WaterMode.阳极:
                    {
                        bRes = fTestValue[nCavityIdx, 1] <= dWaterStandard[1];
                        break;
                    }
                case WaterMode.阴极:
                    {
                        bRes = fTestValue[nCavityIdx, 2] <= dWaterStandard[2];
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        bRes = ((fTestValue[nCavityIdx, 1] <= dWaterStandard[1]) && (fTestValue[nCavityIdx, 2] <= dWaterStandard[2]));
                        break;
                    }
                default:
                    break;
            }
            return bRes;
        }
        /// <summary>
        /// 设置水含量值（界面调用）
        /// </summary>
        public bool SetWaterContent(int nIndex, float[] fTestValue)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return false;
            }
            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        fWaterContentValue[nIndex, 0] = fTestValue[0];
                        fWaterContentValue[nIndex, 1] = fTestValue[1];
                        fWaterContentValue[nIndex, 2] = fTestValue[2];
                        fWaterContentValue[nIndex, 3] = fTestValue[3];
                        break;
                    }
                case WaterMode.阳极:
                    {
                        fWaterContentValue[nIndex, 1] = fTestValue[0];
                        break;
                    }
                case WaterMode.阴极:
                    {
                        fWaterContentValue[nIndex, 2] = fTestValue[0];
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        fWaterContentValue[nIndex, 1] = fTestValue[0];
                        fWaterContentValue[nIndex, 2] = fTestValue[1];
                        fWaterContentValue[nIndex, 3] = fTestValue[2];
                        fWaterContentValue[nIndex, 4] = fTestValue[3];
                        break;
                    }
                default:
                    break;
            }
            return true;
        }

        /// <summary>
        /// 获取水含量值（界面调用）
        /// </summary>
        public float GetWaterContent(int nIndex)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return -1;
            }
            float fWaterValue = -1;
            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        fWaterValue = fWaterContentValue[nIndex, 0];
                        break;
                    }
                case WaterMode.阳极:
                    {
                        fWaterValue = fWaterContentValue[nIndex, 1];
                        break;
                    }
                case WaterMode.阴极:
                    {
                        fWaterValue = fWaterContentValue[nIndex, 2];
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        fWaterValue = fWaterContentValue[nIndex, 1] + fWaterContentValue[nIndex, 2];
                        break;
                    }
                default:
                    break;
            }
            return fWaterValue;
        }

        /// <summary>
        /// 获取当前烘烤次数
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        public int GetBakingCount(int nIndex)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return -1;
            }

            return nBakCount[nIndex];
        }

        /// <summary>
        /// 获取当前干燥次数
        /// </summary>
        public int GetCurBakingTimes(int nIndex)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return -1;
            }

            return nCurBakingTimes[nIndex];
        }

        /// <summary>
        /// 获取循环干燥次数
        /// </summary>
        public int GetCirBakingTimes(int nIndex)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return -1;
            }

            return nCirBakingTimes[nIndex];
        }

        /// <summary>
        /// 获取开始时间
        /// </summary>
        public DateTime GetStartTime(int nFloor)
        {
            return arrStartTime[nFloor];
        }

        /// <summary>
        /// 获取水含量条码
        /// </summary>
        public string GetWaterContentCode(int nFloor)
        {
            string strCode = "";
            if (nFloor < 0 || nFloor >= (int)ModuleDef.PalletMaxCol)
            {
                return strCode;
            }

            int nPltRow = 0;
            int nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);

            for (int nIndex = 0; nIndex < (int)ModuleDef.PalletMaxRow; nIndex++)
            {
                for (int i = 0; i < nPltRow; i++)
                {
                    for (int j = 0; j < nPltCol; j++)
                    {
                        if (BatType.Fake == Pallet[nFloor * 2 + nIndex].Bat[i, j].Type)
                        {
                            strCode = Pallet[nFloor * 2 + nIndex].Bat[i, j].Code;
                            return strCode;
                        }
                    }
                }
            }
            return strCode;
        }

        /// <summary>
        /// 获取水含量上传状态
        /// </summary>
        public WCState GetWCUploadStatus(int nFloor)
        {
            if (nFloor < 0 || nFloor >= (int)ModuleDef.PalletMaxCol)
            {
                return WCState.WCStateInvalid;
            }

            return WCUploadStatus[nFloor];
        }

        /// <summary>
        /// 设置水含量上传状态
        /// </summary>
        public bool SetWCUploadStatus(int nFloor, WCState nStatus)
        {
            if (nFloor < 0 || nFloor >= (int)ModuleDef.PalletMaxCol)
            {
                return false;
            }
            WCUploadStatus[nFloor] = nStatus;
            return true;
        }

        /// <summary>
        /// 获取腔体状态
        /// </summary>
        public CavityState GetCavityState(int nIndex)
        {
            if (nIndex < 0 || nIndex >= (int)ModuleDef.PalletMaxCol)
            {
                return CavityState.Invalid;
            }
            return CavityDataSource[nIndex].State;
        }

        /// <summary>
        /// 设置腔体状态
        /// </summary>
        public bool SetCavityState(int nIndex, CavityState state)
        {
            if (nIndex >= 0 && nIndex < (int)ModuleDef.PalletMaxCol)
            {
                CavityDataSource[nIndex].State = state;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置解除维修状态
        /// </summary>
        public bool SetClearMaintenance(int nIndex)
        {
            if (nIndex >= 0 && nIndex < (int)ModuleDef.PalletMaxCol)
            {
                bClearMaintenance[nIndex] = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 有等待工作的腔体
        /// </summary>
        public bool HasWaitWorkCavity(ref int nIndex)
        {
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                if (CavityState.Standby == GetCavityState(nCavityIdx) && IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx))
                {
                    if (GetCavityPallet(nCavityIdx).All(A => A.IsType(PltType.OK) && A.IsStage(PltStage.Onload) && !PltIsEmpty(A))
                        && GetCavityPallet(nCavityIdx).Any(A=> A.IsOnloadFake))
                    {
                        nIndex = nCavityIdx;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 有异常工作的腔体
        /// </summary>
        public bool HasAnomalyWorkCavity(ref int nIndex)
        {
            bool[] bHasFake = { false, false };
            bool[] bHasFullPlt = { false, false };
            bool[] bHasInv = { false, false };
            // 有NG层&&有假电池烘烤&&没有无效 
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                if (CavityState.Standby == GetCavityState(nCavityIdx))
                {
                    if (IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx))
                    {
                        int nPltIdx = nCavityIdx * (int)ModuleDef.PalletMaxRow;
                        int NGCount = 0;
                        for (int nRow = 0; nRow < (int)ModuleDef.PalletMaxRow; nRow++)
                        {
                            if (IsCavityNg(nCavityIdx, nRow))
                            {
                                NGCount++;
                            }
                            if (!IsTransfer(nCavityIdx, nRow) && (Pallet[nPltIdx + nRow].IsType(PltType.OK) && Pallet[nPltIdx + nRow].IsStage(PltStage.Onload) && !PltIsEmpty(Pallet[nPltIdx + nRow]))
                                || (!IsTransfer(nCavityIdx, nRow) && IsCavityNg(nCavityIdx, nRow) && PltIsEmpty(Pallet[nPltIdx + nRow])))
                            {
                                if (Pallet[nPltIdx + nRow].Bat[0, 0].Type == BatType.Fake)
                                {
                                    bHasFake[nCavityIdx] = true;
                                }

                                if (!PltIsEmpty(Pallet[nPltIdx + nRow]))
                                {
                                    bHasFullPlt[nCavityIdx] = true;
                                }

                                if (!bHasInv[nCavityIdx] && bHasFake[nCavityIdx] && (nRow == (int)ModuleDef.PalletMaxRow - 1) && NGCount < 8)
                                {
                                    nIndex = nCavityIdx;
                                    return true;
                                }
                            }
                            if (!IsCavityNg(nCavityIdx, nRow) && (Pallet[nPltIdx + nRow].Type == PltType.Invalid || PltIsEmpty(Pallet[nPltIdx + nRow])))
                            {
                                bHasInv[nCavityIdx] = true;
                            }
                        }

                    }
                }
            }

            // 清尾料
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                if (CavityState.Standby == GetCavityState(nCavityIdx) && bHasFullPlt[nCavityIdx] && bHasFake[nCavityIdx] && bCavityClear[nCavityIdx])
                {
                    if (IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx))
                    {
                        bCavityClear[nCavityIdx] = false;
                        SaveParameter();
                        nIndex = nCavityIdx;
                        return true;
                    }
                }
                if (bCavityClear[nCavityIdx])
                {
                    bCavityClear[nCavityIdx] = false;
                    SaveParameter();
                }
            }
            return false;
        }

        /// <summary>
        /// 腔体清尾料
        /// </summary>
        public void CavityTailing()
        {
            bool[] bHasFake = { false, false };
            bool[] bHasFullPlt = { false, false };
            bool[] bHasInv = { false, false };
            string strMsg = "";
            // 有NG层&&有假电池烘烤&&没有无效 
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleDef.PalletMaxCol; nCavityIdx++)
            {
                if (CavityState.Standby == GetCavityState(nCavityIdx))
                {
                    if (!IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx) && bCavityClear[nCavityIdx])
                    {
                        int nPltIdx = nCavityIdx * (int)ModuleDef.PalletMaxRow;
                        for (int nRow = 0; nRow < (int)ModuleDef.PalletMaxRow; nRow++)
                        {
                            if (IsCavityNg(nCavityIdx, nRow))
                            {
                                strMsg = "有NG层请,检查炉腔托盘!";
                            }
                            if (!IsCavityNg(nCavityIdx, nRow) && (Pallet[nPltIdx + nRow].Type == PltType.Invalid))
                            {
                                bHasInv[nCavityIdx] = true;
                                strMsg = "有无效托盘,请检查炉腔托盘!";
                            }
                            if (!IsTransfer(nCavityIdx, nRow) && (Pallet[nPltIdx + nRow].IsType(PltType.OK) && Pallet[nPltIdx + nRow].IsStage(PltStage.Onload) && !PltIsEmpty(Pallet[nPltIdx + nRow]))
                                || (!IsTransfer(nCavityIdx, nRow) && IsCavityNg(nCavityIdx, nRow) && PltIsEmpty(Pallet[nPltIdx + nRow])))
                            {
                                if (Pallet[nPltIdx + nRow].Bat[0, 0].Type == BatType.Fake)
                                {
                                    bHasFake[nCavityIdx] = true;
                                }

                                if (!PltIsEmpty(Pallet[nPltIdx + nRow]))
                                {
                                    bHasFullPlt[nCavityIdx] = true;
                                }
                            }
                        }
                        if (bHasFullPlt[nCavityIdx] && bHasFake[nCavityIdx] && !bHasInv[nCavityIdx] && bCavityClear[nCavityIdx])
                        {
                            for (int nRow = 0; nRow < (int)ModuleDef.PalletMaxRow; nRow++)
                            {
                                if (!Pallet[nPltIdx + nRow].IsFull())
                                {
                                    if (Pallet[nPltIdx + nRow].IsStage(PltStage.Invalid)) Pallet[nPltIdx + nRow].Stage = PltStage.Onload;
                                    Pallet[nPltIdx + nRow].FillPltBat();
                                }
                            }
                            bCavityClear[nCavityIdx] = false;
                            SaveRunData(SaveType.Pallet);
                            SaveParameter();
                            strMsg = "清尾料成功!";
                            ShowMsgBox.ShowDialog(strMsg, MessageType.MsgMessage);
                        }
                        else
                        {
                            if (bCavityClear[nCavityIdx])
                            {
                                if (!bHasFake[nCavityIdx]) strMsg = "无假电芯托盘,请检查炉腔托盘!";
                                if (!bHasFullPlt[nCavityIdx]) strMsg = "无满托盘,请检查炉腔托盘!";
                                bCavityClear[nCavityIdx] = false;
                                SaveParameter();
                                ShowMsgBox.ShowDialog(strMsg, MessageType.MsgMessage);
                            }
                        }
                    }
                }
                if (bCavityClear[nCavityIdx])
                {
                    bCavityClear[nCavityIdx] = false;
                    SaveParameter();
                }
            }
        }

        /// <summary>
        /// 有放托盘位
        /// </summary>
        public bool HasPlacePos()
        {
            return HasXXPlt(CavityState.Standby, new Func<Pallet, bool>(A => A.IsType(PltType.Invalid)));
        }

        /// <summary>
        /// 有等待结果托盘位置（已取待测假电池的托盘）
        /// </summary>
        public bool HasPlaceWiatResPltPos()
        {
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                if (IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx) && CavityState.Detect == GetCavityState(nCavityIdx))
                {
                    Pallet[] tempList = GetCavityPallet(nCavityIdx);
                    bool b1 = tempList.Where(A => A.IsType(PltType.Detect) && A.IsStage(PltStage.Onload)).Count() == (int)ModuleRowCol.DryingOvenRow - 1;
                    bool b2 = tempList.Where(A => A.IsType(PltType.Invalid)).Count() == 1;
                    if(b1 && b2)
                        return true;
                }
            }
            return false;
        }

        public Pallet[] GetCavityPallet(int nCavityIdx)
        {
            return Pallet.Skip(nCavityIdx * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
        }

        /// <summary>
        /// 有回炉托盘位置（已放回假电池的夹具）
        /// </summary>
        public bool HasPlaceRebakingPltPos()
        {
            return HasXXPlt(CavityState.Rebaking, new Func<Pallet, bool>(A => A.IsType(PltType.Invalid)));
        }

        /// <summary>
        /// 有空托盘
        /// </summary>
        public bool HasEmptyPlt()
        {
            return HasXXPlt(CavityState.Standby,
                            new Func<Pallet, bool>(A =>
                                    A.IsType(PltType.OK) && PltIsEmpty(A)));
        }

        /// <summary>
        /// 有NG非空托盘
        /// </summary>
        public bool HasNGPlt()
        {
            return HasXXPlt(CavityState.Standby,
                new Func<Pallet, bool>(A =>
                        A.IsType(PltType.NG) && !PltIsEmpty(A)));
        }

        /// <summary>
        /// 有NG空托盘
        /// </summary>
        public bool HasNGEmptyPlt()
        {
            return HasXXPlt(CavityState.Standby,
                            new Func<Pallet, bool>(A =>
                                    A.IsType(PltType.NG) && PltIsEmpty(A)));
        }

        /// <summary>
        /// 有待检查托盘（未取走假电池的托盘）
        /// </summary>
        public bool HasDetectPlt()
        {
            return HasXXPlt(CavityState.Detect,
                new Func<Pallet, bool>(A =>
                        A.IsType(PltType.Detect) && PltHasTypeBat(A, BatType.Fake)));
        }

        /// <summary>
        /// 有待回炉托盘（已取走假电池，待重新放回假电池的托盘）
        /// </summary>
        public bool HasRebakingPlt()
        {
            return HasXXPlt(CavityState.Rebaking, 
                            new Func<Pallet, bool>(A => 
                                    A.IsType(PltType.WaitRebakeBat) && PltHasTypeBat(A, BatType.Fake)));
        }

        /// <summary>
        /// 有待冷却托盘
        /// </summary>
        public bool HasWaitCooling()
        {
            return HasXXPlt(CavityState.Standby,
                            new Func<Pallet, bool>(A =>
                                    A.IsType(PltType.WaitCooling)));
        }

        /// <summary>
        /// 有待下料托盘
        /// </summary>
        /// <remarks>腔体待机，托盘状态待下料，托盘不为空</remarks>
        public bool HasOffloadPlt()
        {
            return HasXXPlt(CavityState.Standby,
                            new Func<Pallet, bool>(A=> 
                                    A.IsType(PltType.WaitOffload) && !PltIsEmpty(A) ));
        }

        /// <summary>
        /// 判断是否有满足指定条件的托盘
        /// </summary>
        /// <param name="cavityState">炉腔状态</param>
        /// <param name="func">托盘状态类型判断</param>
        /// <returns>bool</returns>
        private bool HasXXPlt(CavityState cavityState,Func<Pallet,bool> func)
        {
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                if (IsCavityEN(nCavityIdx) && !IsPressure(nCavityIdx) && cavityState == GetCavityState(nCavityIdx))
                {
                    var tempList = Pallet.Skip(nCavityIdx * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
                    if(tempList.Any(A => func(A)))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 有转移满料
        /// </summary>
        public bool HasTransferFullPlt(Pallet[] plt)
        {
            for (int nCavityIdx = 0; nCavityIdx < (int)ModuleRowCol.DryingOvenCol; nCavityIdx++)
            {
                if (IsCavityEN(nCavityIdx) && IsPressure(nCavityIdx) && CavityState.Standby == GetCavityState(nCavityIdx) )
                {
                    var tempList = Pallet.Skip(nCavityIdx * (int)ModuleRowCol.DryingOvenRow).Take((int)ModuleRowCol.DryingOvenRow).ToArray();
                    return !(tempList.Select(
                        (Pallet A,int index)=> 
                            A.IsType(PltType.OK) && !PltIsEmpty(A) && A.IsStage(PltStage.Onload)
                            && !IsCavityNg(nCavityIdx, index)).Count()>0);
                }
            }
            return false;
        }

        /// <summary>
        /// 电池计数
        /// </summary>
        /// <param name="炉列"></param>
        private int CalBatCount(int col, PltType pltType, BatType batType)
        {
            int nPltRow = 0, nPltCol = 0;
            int batCount = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);
            for (int j = 0; j < (int)ModuleDef.PalletMaxRow; j++)
            {
                if ((Pallet[col * (int)ModuleDef.PalletMaxRow + j].Type == pltType))
                {
                    for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                    {
                        for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                        {
                            if (Pallet[col * (int)ModuleDef.PalletMaxRow + j].Bat[nRowIdx, nColIdx].IsType(batType))
                            {
                                batCount++;
                            }
                        }
                    }
                }
            }
            return batCount;
        }

        public void ReleaseBatCount()
        {
            NBakingOverBat = 0;
           
        }
        #endregion


        #region // 设置信息

        /// <summary>
        /// 腔体使能
        /// </summary>
        public bool IsCavityEN(int nIndex)
        {
            if (nIndex > -1 && nIndex < (int)ModuleDef.PalletMaxCol)
            {
                return CavityDataSource[nIndex].OvenEnable;
            }
            return false;
        }

        /// <summary>
        /// 腔体保压
        /// </summary>
        public bool IsPressure(int nIndex)
        {
            if (nIndex > -1 && nIndex < (int)ModuleDef.PalletMaxCol)
            {
                return CavityDataSource[nIndex].Pressure;
            }
            return false;
        }


        /// <summary>
        /// 设置保压
        /// </summary>
        public bool SetPressure(int nIndex, bool bOpen)
        {
            if (nIndex > -1 && nIndex < (int)ModuleDef.PalletMaxCol)
            {
                CavityDataSource[nIndex].Pressure = bOpen;
                SaveParameter();
            }
            return false;
        }

        /// <summary>
        /// 腔体转移
        /// </summary>
        public bool IsTransfer(int nCol, int nRow)
        {
            if (nCol > -1 && nCol < (int)ModuleDef.PalletMaxCol
                && nRow > -1 && nRow < (int)ModuleDef.PalletMaxRow)
            {
                return bTransfer[nCol, nRow];
            }
            return false;
        }

        /// <summary>
        /// 腔体NG
        /// </summary>
        public bool IsCavityNg(int nCol, int nRow)
        {
            if (nCol > -1 && nCol < (int)ModuleDef.PalletMaxCol
                && nRow > -1 && nRow < (int)ModuleDef.PalletMaxRow)
            {
                return bOvenCavityNg[nCol, nRow];
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉组号
        /// </summary>
        public int GetOvenGroup()
        {
            return nOvenGroup;
        }

        /// <summary>
        /// 获取干燥炉界面组号
        /// </summary>
        public int GetOvenDisplayGroup()
        {
            return nOvenDisplayGroup;
        }

        /// <summary>
        /// 获取干燥炉ID号
        /// </summary>
        public int GetOvenID()
        {
            return nOvenID;
        }

        /// <summary>
        /// 获取干燥炉资源号
        /// </summary>
        public string GetstrResourceID()
        {
            return strResourceID;
        }
        /// <summary>
        /// 曲线值初始化
        /// </summary>
        public void TempValueRelease(int nCol)
        {
            for (int n1DIdx = 0; n1DIdx < unTempValue.GetLength(1); n1DIdx++)
            {
                for (int n2DIdx = 0; n2DIdx < unTempValue.GetLength(2); n2DIdx++)
                {
                    for (int n3DIdx = 0; n3DIdx < unTempValue.GetLength(3); n3DIdx++)
                    {
                        for (int n4DIdx = 0; n4DIdx < unTempValue.GetLength(4); n4DIdx++)
                        {
                            unTempValue[nCol, n1DIdx, n2DIdx, n3DIdx, n4DIdx] = 0;
                        }
                    }
                }
            }
            for (int n1DIdx = 0; n1DIdx < unVacPressure.GetLength(1); n1DIdx++)
            {
                unVacPressure[nCol, n1DIdx] = 0;
            }
            nGraphPosCount = 0;

            nMinVacm[nCol] = 100000;
            nMaxVacm[nCol] = 0;
            nMinTemp[nCol] = 120;
            nMaxTemp[nCol] = 0;
        }

        /// <summary>
        /// 最大最小值判断
        /// </summary>
        public bool MaxMinValueJudge(int nCol)
        {
            if (nMinVacm[nCol] == 100000
                || nMaxVacm[nCol] == 0
                || nMinTemp[nCol] == 120
                || nMaxTemp[nCol] == 0)
            {
                string strAlarmInfo = string.Format("{0}{1}列\r\n最小真空{2},最大真空{3},最小温度{4},最大温度{5} ,等于初始值！"
                    , RunName, Convert.ToString((nCol + 10), 16).ToUpper(), nMinVacm[nCol], nMaxVacm[nCol], nMinTemp[nCol], nMaxTemp[nCol]);
                ShowMessageBox((int)MsgID.CheckValue, strAlarmInfo, "请检查！", MessageType.MsgWarning, 10);
                return false;
            }
            return true;
        }


        /// <summary>
        /// 清除最大最小值
        /// </summary>
        public bool ClearMaxMinValue(int nCavityIdx)
        {
            if (nCavityIdx < 0 || nCavityIdx >= (int)ModuleDef.PalletMaxCol)
            {
                return false;
            }
            nMinVacm[nCavityIdx] = 0;
            nMaxVacm[nCavityIdx] = 0;
            nMinTemp[nCavityIdx] = 0;
            nMaxTemp[nCavityIdx] = 0;
            return true;
        }

        #endregion

        #region // mes接口

        /// <summary>
        /// 上传水含量
        /// </summary>
        private bool UploadBatWaterStatus(int nFloorIndex, ref bool bRetest, ref string strErr)
        {
            if (nFloorIndex < 0 || nFloorIndex > (int)ModuleDef.PalletMaxCol)
            {
                return false;
            }

            if (bUploadWaterStatus[nFloorIndex])
            {
                return true;
            }

            bool bRelust = false;
            int nMaxRow = 0, nMaxCol = 0;
            string strInfo = "";

            float[] fWaterValue = new float[2];

            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 0];
                        break;
                    }
                case WaterMode.阳极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 1];
                        break;
                    }
                case WaterMode.阴极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 2];
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 1];
                        fWaterValue[1] = fWaterContentValue[nFloorIndex, 2];
                        break;
                    }
                default:
                    break;
            }

            MachineCtrl.GetInstance().GetPltRowCol(ref nMaxRow, ref nMaxCol);

            string sSend = "";
            string sRecv = "";
            WaterCollect waterCollect = new WaterCollect();
            if (!MesUploadBatWaterStatus(nFloorIndex, fWaterValue, ref waterCollect, ref strErr))
            {
                strInfo = string.Format("{0}号干燥炉上传水含量失败", (nOvenID + 1));
                strErr += strInfo;
                if (waterCollect.status_code == "1" && waterCollect.operate_code == "1")
                {
                    Task.Run(() => {
                        //FTPUploadFile(nFloorIndex, fWaterValue);
                    });
                }
                return bRelust;
            }
            else
            {
                bRetest = waterCollect.status_code == "1" && waterCollect.operate_code == "0";
            }
            Task.Run(() => {
                //FTPUploadFile(nFloorIndex, fWaterValue);
            });
            return true;
        }

        /// <summary>
        /// 注销
        /// </summary>
        private bool MesmiCloseNcAndProcess(int nFloorIndex)
        {
            string strErr = "";

            //for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            //{
            //    string strJigCode = Pallet[nFloorIndex * (int)ModuleDef.PalletMaxRow + i].Code;
            //    if (Pallet[nFloorIndex * (int)ModuleDef.PalletMaxRow + i].Bat[0, 0].Type != BatType.BKFill && !OvenmiCloseNcAndProcess(nFloorIndex, strJigCode, ref strErr))
            //    {
            //        ShowMessageBox((int)MsgID.CheckMES, strErr, "请查询MesLog", MessageType.MsgWarning);
            //        return false;
            //    }
            //}
            return true;
        }

        /// <summary>
        /// 检查发热板温度
        /// </summary>
        private void CheckBoardTemp(int nOvenFlowId, CavityData cavityData)
        {
            if (cavityData == null)
            {
                return;
            }
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        private void UploadTempInfo(int nFloorIndex, CavityData cavityData)
        {
            int nAVERTemp = 0, nTempValue = 0, nVacmValue = 0;
            bool bSave = false;

            nVacmValue = (int)cavityData.unVacPressure;
            if (nMaxVacm[nFloorIndex] < nVacmValue)
            {
                bSave = true;
                nMaxVacm[nFloorIndex] = nVacmValue;
            }
            if (nMinVacm[nFloorIndex] > nVacmValue)
            {
                bSave = true;
                nMinVacm[nFloorIndex] = nVacmValue;
            }

            nOvenVacm[nFloorIndex] = nVacmValue;

            for (int nPanelIdx = 0; nPanelIdx < (int)DryOvenNumDef.HeatPanelNum; nPanelIdx++)
            {
                for (int nPltIdx = 0; nPltIdx < (int)ModuleRowCol.DryingOvenRow; nPltIdx++)
                {
                    for (int nTempType = 0; nTempType < (int)DryOvenNumDef.TempTypeNum; nTempType++)
                    {
                        nTempValue = (int)cavityData.unTempValue[nPltIdx, nTempType, nPanelIdx];
                        if (nMaxTemp[nFloorIndex] < nTempValue)
                        {
                            bSave = true;
                            nMaxTemp[nFloorIndex] = nTempValue;
                        }
                        if (nMinTemp[nFloorIndex] > nTempValue)
                        {
                            bSave = true;
                            nMinTemp[nFloorIndex] = nTempValue;
                        }
                        nAVERTemp += nTempValue;
                        nOvenTemp[nFloorIndex] = nTempValue;
                    }
                }
            }
            if (bSave)
            {
                SaveRunData(SaveType.MaxMinValue);
            }
            nAVERTemp /= ((int)DryOvenNumDef.HeatPanelNum * (int)ModuleRowCol.DryingOvenRow * (int)DryOvenNumDef.TempTypeNum);
        }

        /// <summary>
        /// 保存实时温度
        /// </summary>
        private void SaveRealTimeTemp(int nOvenFlowId, int nRunTime, CavityData cavityData)
        {
            string strLog = "", strTemp = "";
            float nCurTemp = 0;
            DateTime strCurDate = DateTime.Now;

            strLog = string.Format("{0},{1},{2},{3},{4},{5}"
            , this.strResourceID//干燥炉资源号
            , nOvenID + 1//干燥炉ID号
            , Convert.ToString((nOvenFlowId + 10), 16).ToUpper()//炉列1A-2B
            , strCurDate.ToString("yyyy/MM/dd HH:mm:ss")//当前时间
            , nRunTime//运行时间
            , cavityData.unVacPressure);//真空压力
            if (cavityData.unVacPressure == 0) return;
            for (int nJig = 0; nJig < (int)ModuleRowCol.DryingOvenRow; nJig++)//循环托盘
            {
                int nPltIdx = nJig;
                strLog += string.Format(",{0}", Pallet[(nOvenFlowId * 8) + nPltIdx].Code);//托盘号
                for (int nPanel = 0; nPanel < (int)DryOvenNumDef.HeatPanelNum; nPanel++)//循环发热板
                {
                    for (int nTempType = 0; nTempType < (int)DryOvenNumDef.TempTypeNum; nTempType++)//循环温度类型
                    {
                        nCurTemp = cavityData.unBaseTempValue[nTempType][nPltIdx][nPanel];//实时温度[类型(巡检或温度)][托盘数][发热板]
                        strTemp = string.Format(",{0}", nCurTemp);
                        strLog += strTemp;
                    }
                }
            }
            MachineCtrl.GetInstance().SaveTempData(strLog.Replace("\r", "").Replace("\n", ""), (this.nOvenID + 1), nOvenFlowId, Pallet[nOvenFlowId * 8].StartTime);
            return;
        }

        /// <summary>
        /// 上传实时温度数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool FTPUploadRealTimeTemp(int cavityIdx)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            string strFilePath = "", strFileName = "";
            string sProcessNo = MachineCtrl.GetInstance().strHCMesParam[0];
            /*string sLine = MachineCtrl.GetInstance().strLineNum;*/
            string sDeviceNo = StrResourceID;
            //DateTime startTime = Convert.ToDateTime(Pallet[cavityIdx * 8].StartTime);

            for (int nRow = 0; nRow < (int)ModuleDef.PalletMaxRow; nRow++)
            {
                if (!string.IsNullOrEmpty(Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + nRow].StartTime))
                {
                    DateTime startTime = Convert.ToDateTime(Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + nRow].StartTime);
                    strFileName = sDeviceNo + "_" + startTime.ToString("MM_dd_HHmmss") + ".CSV";
                    break;

                }
            }

            //string file = $"D:\\MESLogEx\\20)实时温度保存(MesRealTimeTemp)\\{nOvenID + 1}号炉" + "\\" + strFileName;
            string file = string.Format("{0}\\MESLogEx\\20)实时温度保存(MesRealTimeTemp)\\{1}号炉\\{2}", MachineCtrl.GetInstance().ProductionFilePath, nOvenID + 1, strFileName);
            if (File.Exists(file))
            {
                FileInfo fileInfo = new FileInfo(file);
                // 创建文件目录
                string ftpDir = MachineCtrl.GetInstance().strHCMesParam[2];
                string ftpUser = MachineCtrl.GetInstance().strHCMesParam[11];
                string ftpPW = MachineCtrl.GetInstance().strHCMesParam[12];

/*                if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                {
                    ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                    return false;
                }

                ftpDir += $@"/{DateTime.Now.ToString("yyyy-MM")}";
                if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                {
                    ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                    return false;
                }
                ftpDir += $@"/{DateTime.Now.ToString("dd")}";
                if (!FTPClient.FtpDirIsExists(ftpDir, ftpUser, ftpPW) && !FTPClient.MakeDirectory(ftpDir, ftpUser, ftpPW))
                {
                    ShowMessageBox((int)MsgID.FTPUploadErr, $"创建FTP目录{ftpDir}失败", "请检查是否有权限创建FTP目录", MessageType.MsgWarning);
                    return false;
                }
                if (!FTPClient.UploadFile(fileInfo, ftpDir, ftpUser, ftpPW))
                {
                    ShowMessageBox((int)MsgID.FTPUploadErr, $"上传文件{ftpDir}至FTP服务器失败", "请检查FTP服务器是否启动", MessageType.MsgWarning);
                    return false;
                }*/

            }

            return true;
        }

        /// <summary>
        /// 水含量数据NG
        /// </summary>
        private bool UploadBatWaterNG(int nFloorIndex)
        {
            string strJigCode = "";
            string strBatteryCode = "";
            int nPos = 0;

            int nMaxRow = 0, nMaxCol = 0;

            MachineCtrl.GetInstance().GetPltRowCol(ref nMaxRow, ref nMaxCol);

            for (int nPalletPos = nFloorIndex * (int)ModuleDef.PalletMaxRow; nPalletPos < (nFloorIndex * (int)ModuleDef.PalletMaxRow + (int)ModuleDef.PalletMaxRow); nPalletPos++)
            {
                for (int nRow = 0; nRow < nMaxRow; nRow++)
                {
                    for (int nCol = 0; nCol < nMaxCol; nCol++)
                    {
                        if (Pallet[nPalletPos].Bat[nRow, nCol].Type == BatType.Fake)
                        {
                            nPos = nMaxCol * nRow + (nCol+1);
                            strJigCode = Pallet[nPalletPos].Code;
                            strBatteryCode = Pallet[nPalletPos].Bat[nRow, nCol].Code;
                        }

                    }
                }
            }

            string strLog = "";

            float[] fWaterValue = new float[2] { -1, -1 };
            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 0];
                        break;
                    }
                case WaterMode.阳极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 1];
                        break;
                    }
                case WaterMode.阴极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 2];
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        fWaterValue[0] = fWaterContentValue[nFloorIndex, 1];
                        fWaterValue[1] = fWaterContentValue[nFloorIndex, 2];
                        break;
                    }
                default:
                    break;
            }

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}"
                , StrResourceID
                , nOvenID + 1
                , Convert.ToString((nFloorIndex + 10), 16).ToUpper()
                , strJigCode
                , strBatteryCode
                , "NG"
                , fWaterValue[0]
                , fWaterValue[1]
                , nPos);

            string strFileName = DateTime.Now.ToString("yyyyMMdd") + "水含量NG.CSV";
            
            //string strFilePath = "D:\\MESLog\\水含量NG";
            string strFilePath = string.Format("{0}\\MESLog\\水含量NG", MachineCtrl.GetInstance().ProductionFilePath);
            string strColHead = "干燥炉资源号,干燥炉编号(ID),炉列1A-2B,夹具条码,假电芯条码,返回代码(Code),阳极水含量值,阴极水含量值,电芯位置信息";
            MachineCtrl.GetInstance().WriteCSV(strFilePath, strFileName, strColHead, strLog);

            return true;
        }

        /// <summary>
        /// 烘箱数据采集
        /// </summary>
        private bool MesOvenStartAndEnd(int nCurFlowID, int nOvenId, string sType, string sTime, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            bool bOvenStart = false;
            string[] strJigCodeArray = new string[(int)ModuleDef.PalletMaxRow];
            string strLog = "";
            string[] mesParam = new string[16];
            string strProDuctDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string strCallMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            DateTime dtStartTime = DateTime.Now;
            StartAndEnd startAndEnd = new StartAndEnd();
            TimeSpan timeSpan;
            timeSpan = DateTime.Now - dtStartTime;
            string strCallMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}"
                , mesParam[0]
                , mesParam[1]
                , mesParam[2]
                , nOvenID + 1
                , Convert.ToString((nCurFlowID + 10), 16).ToUpper()
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 0].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 1].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 2].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 3].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 4].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 5].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 6].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 7].Code
                , strCallMESTime_Start
                , strCallMESTime_End
                , timeSpan.TotalMilliseconds.ToString("f0")
                , startAndEnd.status_code
                , startAndEnd.message
                , mesParam[3]
                , mesParam[4]);

            int nPltMaxRow = 0;
            int nPltMaxCol = 0;
            //List<string> stringList = new List<string>();
            List<Battery_CodeList> wipList = new List<Battery_CodeList>();
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltMaxRow, ref nPltMaxCol);
            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            {
                var PltNum = (int)ModuleDef.PalletMaxRow * nCurFlowID + i;
                strJigCodeArray[i] = Pallet[PltNum].Code;
                if (!pallet[PltNum].HasTypeBat(BatType.OK) && !pallet[PltNum].HasTypeBat(BatType.Fake)) continue;
                for (int nPltRow = 0; nPltRow < nPltMaxRow; nPltRow++)
                {
                    for (int nPltCol = 0; nPltCol < nPltMaxCol; nPltCol++)
                    {
                        //是否更新假电池绑定
                        if (!MachineCtrl.GetInstance().updataFakeBindingMES && pallet[PltNum].Bat[nPltRow, nPltCol].Type == BatType.Fake) continue;

                        if ((pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.OK
                            && pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.Fake)
                            || pallet[PltNum].Bat[nPltRow, nPltCol].Code.Length <= 8) continue;
                        int nBind = nPltRow * nPltMaxCol + (nPltCol + 1);
                        string sBatCode = pallet[PltNum].Bat[nPltRow, nPltCol].Code;
                        sBatCode = sBatCode.Replace("\r", "").Replace("\n", "");
                        //stringList.Add(sBatCode);
                        wipList.Add(new Battery_CodeList { Cell_Code = sBatCode, position = nBind.ToString() });
                    }
                }
                if (!MachineCtrl.GetInstance().MesBakeDataUpload(sType, ref strErr, wipList, nOvenId, PltNum, Pallet))
                {
                    bOvenStart = false;
                    ShowMessageBox((int)MsgID.OvenConnectAlarm, strErr, "请联系工程师", MessageType.MsgWarning);
                    return false;
                }
                else bOvenStart = true;
                //stringList.Clear();
                Sleep(100);
            }
            return bOvenStart;
        }

        /// <summary>
        /// 烘箱数据采集-界面调用
        /// </summary>
        public bool MesOvenStartAndEndData(int nCurFlowID, int nOvenId, string sType, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            bool bOvenStart = false;
            string[] strJigCodeArray = new string[(int)ModuleDef.PalletMaxRow];
            string strLog = "";
            string[] mesParam = new string[16];
            string strProDuctDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string strCallMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            DateTime dtStartTime = DateTime.Now;
            StartAndEnd startAndEnd = new StartAndEnd();
            TimeSpan timeSpan;
            timeSpan = DateTime.Now - dtStartTime;
            string strCallMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}"
                , mesParam[0]
                , mesParam[1]
                , mesParam[2]
                , nOvenID + 1
                , Convert.ToString((nCurFlowID + 10), 16).ToUpper()
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 0].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 1].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 2].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 3].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 4].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 5].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 6].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 7].Code
                , strCallMESTime_Start
                , strCallMESTime_End
                , timeSpan.TotalMilliseconds.ToString("f0")
                , startAndEnd.status_code
                , startAndEnd.message
                , mesParam[3]
                , mesParam[4]);

            int nPltMaxRow = 0;
            int nPltMaxCol = 0;
            //List<string> stringList = new List<string>();
            List<Battery_CodeList> wipList = new List<Battery_CodeList>();
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltMaxRow, ref nPltMaxCol);
            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            {
                var PltNum = (int)ModuleDef.PalletMaxRow * nCurFlowID + i;
                strJigCodeArray[i] = Pallet[PltNum].Code;
                //if (!pallet[PltNum].HasTypeBat(BatType.OK) && !pallet[PltNum].HasTypeBat(BatType.Fake)) continue;
                for (int nPltRow = 0; nPltRow < nPltMaxRow; nPltRow++)
                {
                    for (int nPltCol = 0; nPltCol < nPltMaxCol; nPltCol++)
                    {
                        //是否更新假电池绑定
                        //if (!MachineCtrl.GetInstance().updataFakeBindingMES && pallet[PltNum].Bat[nPltRow, nPltCol].Type == BatType.Fake) continue;

                        //if ((pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.OK
                        //    && pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.Fake)
                        //    || pallet[PltNum].Bat[nPltRow, nPltCol].Code.Length <= 8) continue;
                        int nBind = nPltRow * nPltMaxCol + (nPltCol+1);
                        string sBatCode = pallet[PltNum].Bat[nPltRow, nPltCol].Code;
                        sBatCode = sBatCode.Replace("\r", "").Replace("\n", "");
                        //stringList.Add(sBatCode);
                        wipList.Add(new Battery_CodeList { Cell_Code = sBatCode, position = nBind.ToString() });
                    }
                }
                if (!MachineCtrl.GetInstance().MesBakeDataUpload(sType, ref strErr, wipList, nOvenId, PltNum, Pallet,true))
                {
                    bOvenStart = false;
                    ShowMessageBox((int)MsgID.OvenConnectAlarm, strErr, "请联系工程师", MessageType.MsgWarning);
                    return false;
                }
                else bOvenStart = true;
                //stringList.Clear();
                Sleep(100);
            }
            return bOvenStart;
        }

        /// <summary>
        /// 托盘开始与结束
        /// </summary>
        private bool MesOvenStartAndEnd(int nCurFlowID, string sType, string sTime, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            int nCode = 0;
            bool bOvenStart = false;

            string[] strJigCodeArray = new string[(int)ModuleDef.PalletMaxRow];
            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            {
                strJigCodeArray[i] = Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + i].Code;
            }

            string strLog = "";
            string[] mesParam = new string[16];
            string strProDuctDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string strCallMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            DateTime dtStartTime = DateTime.Now;

            StartAndEnd startAndEnd = new StartAndEnd();
          /*  bOvenStart = MachineCtrl.GetInstance().HCMESStartAndEnd((int)HCMESINDEX.StartAndEnd, nOvenID, nCurFlowID, strJigCodeArray, sType, sTime, ref startAndEnd, ref strErr, ref mesParam);*/

            TimeSpan timeSpan;
            timeSpan = DateTime.Now - dtStartTime;
            string strCallMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}"
                , mesParam[0]
                , mesParam[1]
                , mesParam[2]
                , nOvenID + 1
                , Convert.ToString((nCurFlowID + 10), 16).ToUpper()
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 0].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 1].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 2].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 3].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 4].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 5].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 6].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 7].Code
                , strCallMESTime_Start
                , strCallMESTime_End
                , timeSpan.TotalMilliseconds.ToString("f0")
                , startAndEnd.status_code
                , startAndEnd.message
                , mesParam[3]
                , mesParam[4]);

           /* MachineCtrl.GetInstance().HCMesReport(HCMESINDEX.StartAndEnd, strLog);*/

            //return (bOvenStart && startAndEnd.status_code == "0");
            return true;
        }
        /// <summary>
        /// 手动调用托盘开始与结束
        /// </summary>
        public bool MesOvenHandStartAndEnd(int nHnadOvenID, int nCurFlowID, string sType, string sTime, ref string strErr1)
        {
            //if (!MachineCtrl.GetInstance().UpdataMES)
            //{
            //    strErr1 = "MES使能未打开！";
            //    ShowMsgBox.ShowDialog(strErr1, MessageType.MsgAlarm);
            //    return false;
            //}
            int nCode = 0;
            bool bOvenStart = false;

            string[] strJigCodeArray = new string[(int)ModuleDef.PalletMaxRow];
            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            {
                strJigCodeArray[i] = Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + i].Code;
            }

            string strLog = "";
            string[] mesParam = new string[16];
            string strProDuctDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string strCallMESTime_Start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            DateTime dtStartTime = DateTime.Now;

            StartAndEnd startAndEnd = new StartAndEnd();
          /*  bOvenStart = MachineCtrl.GetInstance().HCMESStartAndEnd((int)HCMESINDEX.StartAndEnd, nHnadOvenID, nCurFlowID, strJigCodeArray, sType, sTime, ref startAndEnd, ref strErr1, ref mesParam);*/

            TimeSpan timeSpan;
            timeSpan = DateTime.Now - dtStartTime;
            string strCallMESTime_End = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            UserFormula uesr = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref uesr);

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20}"
               , uesr.userName
               , mesParam[0]
               , mesParam[1]
               , mesParam[2]
               , nOvenID + 1
               , Convert.ToString((nCurFlowID + 10), 16).ToUpper()
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 0].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 1].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 2].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 3].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 4].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 5].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 6].Code
               , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 7].Code
               , strCallMESTime_Start
               , strCallMESTime_End
               , timeSpan.TotalMilliseconds.ToString("f0")
               , startAndEnd.status_code
               , startAndEnd.message
               , mesParam[3]
               , mesParam[4]);

            //MachineCtrl.GetInstance().HCMesReport(HCMESINDEX.StartAndEnd, strLog);
           /* MachineCtrl.GetInstance().HCMesReport(HCMESINDEX.HandStartAndEnd, strLog);*/


            return (bOvenStart && startAndEnd.status_code == "0");
        }
        /// <summary>
        /// 水含量数据采集
        /// </summary>
        private bool MesUploadBatWaterStatus(int nCurFlowID, float[] fWater, ref WaterCollect resultDataref, ref string strErr)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            return true;//MES暂未提供水含量接口
            int nCode = 0;
            bool bWaterCollect = false;

            string strLog = "";
            string[] mesParam = new string[23];
            string strCallMESTime_Start = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime dtStartTime = DateTime.Now;

            string result = "01";

            int nMaxRow = 0;
            int nMaxCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nMaxRow, ref nMaxCol);
            string[] strArrBatCode = new string[800];
            string[] strArrname = new string[3] { "SC-BKD003", "SC-BKD010", "SC-BKD011" };  // 烘烤时间，水含量1，水含量2
            string[] strArrvalue = new string[3];
            //int nCurCol = (0 == nOvenGroup) ? nCurFlowID : (1 - nCurFlowID);
            strArrvalue[0] = Convert.ToString(bgCavityData[nCurFlowID].unWorkTime);
            strArrvalue[1] = Convert.ToString(fWater[0]);
            strArrvalue[2] = Convert.ToString(fWater[1]);

            for (int nPltIdx = nCurFlowID * (int)ModuleRowCol.DryingOvenRow; nPltIdx < nCurFlowID * (int)ModuleRowCol.DryingOvenRow + (int)ModuleRowCol.DryingOvenRow; nPltIdx++)
            {
                if (Pallet[nPltIdx].Type == PltType.WaitRes)
                {
                    for (int nCol = 0; nCol < nMaxCol; nCol++)
                    {
                        for (int nRow = 0; nRow < nMaxRow; nRow++)
                        {
                            if (Pallet[nPltIdx].Bat[nRow, nCol].Type == BatType.OK)
                            {
                                strArrBatCode[(nPltIdx - nCurFlowID * (int)ModuleRowCol.DryingOvenRow) * nMaxCol * nMaxRow + nCol * nMaxRow + nRow] = Pallet[nPltIdx].Bat[nRow, nCol].Code;
                            }
                        }
                    }
                }
            }

   /*         bWaterCollect = MachineCtrl.GetInstance().HCMESWaterCollect((int)HCMESINDEX.WaterCollect, nOvenID, nCurFlowID, strArrBatCode, strArrname, strArrvalue, result, ref resultDataref, ref strErr, ref mesParam);*/

            TimeSpan timeSpan;
            timeSpan = DateTime.Now - dtStartTime;
            string strCallMESTime_End = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23}"
                , mesParam[0]
                , mesParam[1]
                , mesParam[2]
                , nOvenID + 1
                , Convert.ToString((nCurFlowID + 10), 16).ToUpper()
                , strArrvalue[0]
                , strArrvalue[1]
                , strArrvalue[2]
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 0].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 1].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 2].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 3].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 4].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 5].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 6].Code
                , Pallet[(int)ModuleDef.PalletMaxRow * nCurFlowID + 7].Code
                , strCallMESTime_Start
                , strCallMESTime_End
                , timeSpan.TotalMilliseconds.ToString("f0")
                , resultDataref.status_code
                , resultDataref.operate_code == null ? "-1" : resultDataref.operate_code
                , resultDataref.message
                , mesParam[3]
                , mesParam[4]);
           /* MachineCtrl.GetInstance().HCMesReport(HCMESINDEX.WaterCollect, strLog);*/

            return bWaterCollect;
        }

        /// <summary>
        /// 托盘结束
        /// </summary>
        /*
        public bool MesUploadOvenFinish(int nCurFinishFlowID, ref string strErr, string PltCode0 = "", string PltCode1 = "")
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            int nCode = 0;
            bool bOvenFinish = false;

            string strLog = "";
            string[] mesParam = new string[16];


            RemovePlt removePlt = new RemovePlt();
            string[] strJigCode = new string[(int)ModuleDef.PalletMaxRow];

            for (int i = 0; i < strJigCode.Length; i++)
            {
                string strCallMESTime_Start = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime dtStartTime = DateTime.Now;

                strJigCode[i] = Pallet[nCurFinishFlowID * (int)ModuleDef.PalletMaxRow + i].Code;
                if (string.IsNullOrEmpty(strJigCode[i]))
                {
                    continue;
                }
                //bOvenFinish = MachineCtrl.GetInstance().HCMESRemovePlt((int)HCMESINDEX.RemovePlt, strJigCode[i], "01", ref removePlt, ref strErr, ref mesParam);

                TimeSpan timeSpan;
                timeSpan = DateTime.Now - dtStartTime;
                string strCallMESTime_End = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                strLog = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}"
                , mesParam[0]
                , mesParam[1]
                , mesParam[2]
                , mesParam[3]
                , nOvenID + 1
                , Convert.ToString((nCurFinishFlowID + 10), 16).ToUpper()
                , strCallMESTime_Start
                , strCallMESTime_End
                , timeSpan.TotalMilliseconds.ToString("f0")
                , removePlt.status_code
                , removePlt.message
                , mesParam[4]
                , mesParam[5]);

               // MachineCtrl.GetInstance().HCMesReport(HCMESINDEX.RemovePlt, strLog);

                if (!bOvenFinish || removePlt.status_code != "0")
                {
                    return false;
                }
            }
            return (bOvenFinish && removePlt.status_code == "0");
        }*/
        
 

        /// <summary>
        /// 产品结果数据 EIP042
        /// </summary>
        public bool MesUploadOvenFinish(int nOvenID, float[,] fTestValue, int nCurFinishFlowID, ref string strErr, string PltCode0 = "", string PltCode1 = "")
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            bool bOvenFinish = false;

            string[] mesParam = new string[16];

            int nPltMaxRow = 0;
            int nPltMaxCol = 0;
            bool isCheckOK = false;
            RemovePlt removePlt = new RemovePlt();
            string[] strJigCode = new string[(int)ModuleDef.PalletMaxRow];
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltMaxRow, ref nPltMaxCol);
            var bakeTimes = GetBakingCount(nCurFinishFlowID);
            string[] strWaterValue = new string[4];
            string[] strWater = new string[2];
            string strPalletCode = "";

            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        strWaterValue[0] = fTestValue[nCurFinishFlowID, 2].ToString();//正极烘烤水分测试
                        strWaterValue[1] = fTestValue[nCurFinishFlowID, 0].ToString();//正极烘烤水分测试结果
                        strWaterValue[2] = fTestValue[nCurFinishFlowID, 3].ToString();//负极烘烤水分测试
                        strWaterValue[3] = fTestValue[nCurFinishFlowID, 1].ToString();//负极烘烤水分测试结果
                        break;
                    }
                case WaterMode.阳极:
                    {
                        break;
                    }
                case WaterMode.阴极:
                    {
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        strWaterValue[0] = fTestValue[nCurFinishFlowID, 3].ToString();//正极烘烤水分测试
                        strWaterValue[1] = fTestValue[nCurFinishFlowID, 1].ToString();//正极烘烤水分测试结果
                        strWaterValue[2] = fTestValue[nCurFinishFlowID, 4].ToString();//负极烘烤水分测试
                        strWaterValue[3] = fTestValue[nCurFinishFlowID, 2].ToString();//负极烘烤水分测试结果
                        break;
                    }
                default:
                    break;
            }

            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)//寻找假电池托盘号
            {
                var PltNum = (int)ModuleDef.PalletMaxRow * nCurFinishFlowID + i;
                if (pallet[PltNum].IsOnloadFake == true)
                {
                    strPalletCode = pallet[PltNum].Code; break;
                }
            }

            for (int i = 0; i < (int)ModuleDef.PalletMaxRow; i++)
            {
                var PltNum = (int)ModuleDef.PalletMaxRow * nCurFinishFlowID + i;
                if (!pallet[PltNum].HasTypeBat(BatType.OK) && !pallet[PltNum].HasTypeBat(BatType.Fake)) continue;
                strWater[0] = strWaterValue[0];
                strWater[1] = strWaterValue[1];
                string batPosition = (nOvenID + 1).ToString() + "炉" + (nCurFinishFlowID + 1).ToString() + "列" + (i + 1).ToString() + "层";
                Dictionary<string, string> data = new Dictionary<string, string>
                {
                    {"RZKHK001", bakeTimes.ToString()},//烘烤次数
                    {"RZKHK002", bgCavityData[nCurFinishFlowID].ProcessParam.UnSetTempValue.ToString()},//烘箱温度/℃  设定温度
                    {"RZKHK003", bgCavityData[nCurFinishFlowID].UnWorkTime.ToString()},//烘箱时长
                    {"RZKHK004", bgCavityData[nCurFinishFlowID].ProcessParam.UnPressureLowerLimit.ToString()},//烘箱真空度/最小真空压力
                    {"RZKHK005", strWaterValue[0]},//正极烘烤水分测试
                    {"RZKHK006", strWaterValue[1]},//正极烘烤水分测试结果
                    {"RZKHK007", strWaterValue[2]},//负极烘烤水分测试
                    {"RZKHK008", strWaterValue[3]},//负极烘烤水分测试结果
                    {"RZKHK009", strPalletCode},//假电池托盘号
                    {"RZKHK010", batPosition},//烘箱号
                    {"RZKHK011", strWater[0]},//混合型烘烤水分结果
                    {"RZKHK012", strWater[1]},//混合型烘烤水分测试结果
                };

                List<OUT_ParamListItem> paramList = new List<OUT_ParamListItem>();

                foreach (var item in data)
                {
                    paramList.Add(new OUT_ParamListItem { Param_Code = item.Key, Param_Value = item.Value });
                }

                for (int nPltRow = 0; nPltRow < nPltMaxRow; nPltRow++)
                {

                    List<OUT_LISTItem> oUT_LISTItems = new List<OUT_LISTItem>();
                    for (int nPltCol = 0; nPltCol < nPltMaxCol; nPltCol++)
                    {
                        //是否更新假电池绑定
                        if (!MachineCtrl.GetInstance().updataFakeBindingMES && pallet[PltNum].Bat[nPltRow, nPltCol].Type == BatType.Fake) continue;


                        if ((pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.OK
                            && pallet[PltNum].Bat[nPltRow, nPltCol].Type != BatType.Fake)
                            || pallet[PltNum].Bat[nPltRow, nPltCol].Code.Length <= 8) continue;
                        int nBind = nPltRow * nPltMaxCol + nPltCol;
                        string sBatCode = pallet[PltNum].Bat[nPltRow, nPltCol].Code;
                        sBatCode = sBatCode.Replace("\r", "").Replace("\n", "");
                        PltCode1 = ((nPltRow + 1).ToString() + "-" + (nPltCol + 1).ToString()).Replace("\r", "").Replace("\n", "");
                        oUT_LISTItems.Add(new OUT_LISTItem
                        {
                            OUT_LOT_NO = sBatCode,//电池条码  产出在制品序号
                            OUT_TRAY_NO = "111",//产出在制品关联载具
                            BATCH_CODE = GetIn_Code(sBatCode),//电池条码  原材料投料批次（如何获取）
                            IN_LOT_NO = GetIn_Code(sBatCode),//电池条码  在制品投料批次（如何获取）
                            IN_TRAY_NO = GetIn_Code(pallet[PltNum].Code),//载具条码  在制品投料载具
                            Is_NG = "0",//0：OK，1：NG  是否合格
                            NG_Code = GetIn_Code(""),//异常代码
                            REMARK = PltCode1,
                            ParamList = paramList//参数清单
                        });
                    }

                    if (!MachineCtrl.GetInstance().MesResultDataUploadAssembly(GetstrResourceID(), nCurFinishFlowID, pallet, oUT_LISTItems, ref strErr))
                    {
                        return false;
                    }

                }

            }

            static List<string> GetIn_Code(string st)
            {
                List<string> wipList = new List<string>();
                wipList.Add(st);
                return wipList;
            }


            return true;
        }
        /// <summary>
        /// 产品结果数据 EIP042  界面手动调用
        /// </summary>
        public bool MesUploadOvenFinishData(int nOvenID, float[,] fTestValue, int nCurFinishFlowID, ref string strErr, string PltCode0 = "", string PltCode1 = "0")
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }

            bool bOvenFinish = false;

            string[] mesParam = new string[16];

            int nPltMaxRow = 0;
            int nPltMaxCol = 0;
            bool isCheckOK = false;
            RemovePlt removePlt = new RemovePlt();
            string[] strJigCode = new string[(int)ModuleDef.PalletMaxRow];
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltMaxRow, ref nPltMaxCol);
            var bakeTimes = GetBakingCount(nCurFinishFlowID);
            string[] strWaterValue = new string[4];
            string[] strWater = new string[2];
            string strPalletCode = "";

            switch (MachineCtrl.GetInstance().eWaterMode)
            {
                case WaterMode.混合型:
                    {
                        strWaterValue[0] = fTestValue[nCurFinishFlowID, 2].ToString();//正极烘烤水分测试
                        strWaterValue[1] = fTestValue[nCurFinishFlowID, 0].ToString();//正极烘烤水分测试结果
                        strWaterValue[2] = fTestValue[nCurFinishFlowID, 3].ToString();//负极烘烤水分测试
                        strWaterValue[3] = fTestValue[nCurFinishFlowID, 1].ToString();//负极烘烤水分测试结果
                        break;
                    }
                case WaterMode.阳极:
                    {
                        break;
                    }
                case WaterMode.阴极:
                    {
                        break;
                    }
                case WaterMode.阴阳极:
                    {
                        strWaterValue[0] = fTestValue[nCurFinishFlowID, 3].ToString();//正极烘烤水分测试
                        strWaterValue[1] = fTestValue[nCurFinishFlowID, 1].ToString();//正极烘烤水分测试结果
                        strWaterValue[2] = fTestValue[nCurFinishFlowID, 4].ToString();//负极烘烤水分测试
                        strWaterValue[3] = fTestValue[nCurFinishFlowID, 2].ToString();//负极烘烤水分测试结果
                        break;
                    }
                default:
                    break;
            }
            strWater[0] = "";
            strWater[1] = "";
            string batPosition = (nOvenID + 1).ToString() + "炉" + (nCurFinishFlowID + 1).ToString() + "列" + (1).ToString() + "层";
            Dictionary<string, string> data = new Dictionary<string, string>
                {
                    {"RZKHK001", bakeTimes.ToString()},//烘烤次数
                    {"RZKHK002", bgCavityData[nCurFinishFlowID].ProcessParam.UnSetTempValue.ToString()},//烘箱温度/℃  设定温度
                    {"RZKHK003", bgCavityData[nCurFinishFlowID].UnWorkTime.ToString()},//烘箱时长
                    {"RZKHK004", nMinVacm[nCurFinishFlowID].ToString()},//烘箱真空度/最小真空压力
                    {"RZKHK005", strWaterValue[0]},//正极烘烤水分测试
                    {"RZKHK006", strWaterValue[1]},//正极烘烤水分测试结果
                    {"RZKHK007", strWaterValue[2]},//负极烘烤水分测试
                    {"RZKHK008", strWaterValue[3]},//负极烘烤水分测试结果
                    {"RZKHK009", strPalletCode},//假电池托盘号
                    {"RZKHK010", batPosition},//烘箱号
                    {"RZKHK011", strWater[0]},//混合型烘烤水分结果
                    {"RZKHK012", strWater[1]},//混合型烘烤水分测试结果
                };

            List<OUT_ParamListItem> paramList = new List<OUT_ParamListItem>();

            foreach (var item in data)
            {
                paramList.Add(new OUT_ParamListItem { Param_Code = item.Key, Param_Value = item.Value });
            }
            List<OUT_LISTItem> oUT_LISTItems = new List<OUT_LISTItem>();
            string sBatCode = PltCode0;
            sBatCode = sBatCode.Replace("\r", "").Replace("\n", "");
            oUT_LISTItems.Add(new OUT_LISTItem
            {
                OUT_LOT_NO = sBatCode,//电池条码  产出在制品序号
                OUT_TRAY_NO = "111",//产出在制品关联载具
                BATCH_CODE = GetIn_Code(sBatCode),//电池条码  原材料投料批次（如何获取）
                IN_LOT_NO = GetIn_Code(sBatCode),//电池条码  在制品投料批次（如何获取）
                IN_TRAY_NO = GetIn_Code(PltCode1),//载具条码  在制品投料载具
                Is_NG = "0",//0：OK，1：NG  是否合格
                NG_Code = GetIn_Code(""),//异常代码
                REMARK = PltCode1.Replace("\r", "").Replace("\n", ""),
                ParamList = paramList//参数清单
            });
            if (!MachineCtrl.GetInstance().MesResultDataUploadAssembly(GetstrResourceID(), nCurFinishFlowID, pallet, oUT_LISTItems, ref strErr, true))
            {
                return false;
            }
            
            static List<string> GetIn_Code(string st)
            {
                List<string> wipList = new List<string>();
                wipList.Add(st);
                return wipList;
            }
            return true;
        }
        //FTP日志
        private void FtpLog(int cavityIdx, int row, string sFilePath, string sFileName, float[] fWater)
        {
            string sProcessNo = MachineCtrl.GetInstance().strHCMesParam[0];
            string sDeviceNo = StrResourceID;
            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            string sColHead = "";
            string sLog = "";
            string strFilePathEx = string.Format(@"{0}\Back\干燥过程数据\{1}-{2}列"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1));

            UpdateOvenData( bgCavityData);
            //int nbgIdx = (0 == nOvenGroup) ? cavityIdx : (1 - cavityIdx);

            int nMaxRow = 0, nMaxCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nMaxRow, ref nMaxCol);
            for (int nRow = 0; nRow < nMaxRow; nRow++)
            {
                for (int nCol = 0; nCol < nMaxCol; nCol++)
                {
                    if (this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].Bat[nRow, nCol].Type == BatType.OK)
                    {
                        sColHead = "电芯所在位置,设备编号,工序,操作员,工单号,电芯条码,水含量值1,水含量值2,入站时间,开始时间,结束时间";
                        for (int i = 0; i < DryingOvenDef.OvenParaName.Length; i++)
                        {
                            sColHead += $",{DryingOvenDef.OvenParaName[i]}";
                        }

                        sLog = $"{nRow + 1}行{nCol + 1}列" /*+
                               $",{sDeviceNo},{sProcessNo},{user.userName}" +
                               $",{this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].Bat[nRow, nCol].Bill_no}" +
                               $",{this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].Bat[nRow, nCol].Code}" +
                               $",{fWater[0]},{fWater[1]}" +
                               $",{this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].Bat[nRow, nCol].Scan_time}" +
                               $",{this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].StartTime}" +
                               $",{this.Pallet[cavityIdx * (int)ModuleDef.PalletMaxRow + row].EndTime}" +
                               $",{bgCavityData[nbgIdx].unDryTime},{bgCavityData[nbgIdx].unSetTempValue}" +
                               $",{bgCavityData[nbgIdx].unTempAddSv1},{bgCavityData[nbgIdx].unTempAddSv2},{bgCavityData[nbgIdx].unTempAddSv3},{bgCavityData[nbgIdx].unTempAddSv4},{bgCavityData[nbgIdx].unTempAddSv5}" +
                               $",{bgCavityData[nbgIdx].unStartTimeSv2},{bgCavityData[nbgIdx].unStartTimeSv3},{bgCavityData[nbgIdx].unStartTimeSv4},{bgCavityData[nbgIdx].unStartTimeSv5}" +
                               $",{bgCavityData[nbgIdx].unPreHeatPressLow1},{bgCavityData[nbgIdx].unPreHeatPressUp1},{bgCavityData[nbgIdx].unPreHeatStartTime2},{bgCavityData[nbgIdx].unPreHeatPressLow2}" +
                               $",{bgCavityData[nbgIdx].unPreHeatPressUp2},{bgCavityData[nbgIdx].unHighVacStartTime},{bgCavityData[nbgIdx].unHighVacFirTakeOutVacPress},{bgCavityData[nbgIdx].unHighVacStartTime1}" +
                               $",{bgCavityData[nbgIdx].unHighVacEndTime1},{bgCavityData[nbgIdx].unHighVacEndTime1},{bgCavityData[nbgIdx].unHighVacPressUp1},{bgCavityData[nbgIdx].unHighVacStartTime2}" +
                               $",{bgCavityData[nbgIdx].unHighVacEndTime2},{bgCavityData[nbgIdx].unHighVacTakeOutVacCycle2},{bgCavityData[nbgIdx].unHighVacTakeOutVacTime2},{bgCavityData[nbgIdx].unBreatStartTime}" +
                               $",{bgCavityData[nbgIdx].unBreatEndTime},{bgCavityData[nbgIdx].unBreatTouchPress},{bgCavityData[nbgIdx].unBreatMinInterTime},{bgCavityData[nbgIdx].unBreatAirInfBackPress},{bgCavityData[nbgIdx].unBreatBackKeepTime}"*/
                               ;

                        MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
                        MachineCtrl.GetInstance().WriteCSV(strFilePathEx, sFileName, sColHead, sLog);
                    }
                }
            }
        }

        /// <summary>
        /// 报警上传
        /// </summary>
        /// <param name="cavityData"></param>
        private void MesUpLoadAlarm(string nAlarm)
        {
            var alarms = new AlarmsItem();
            string ALARM_CODE = "";
            if ((nAlarm == null) && (nAlarm == "")) return;
            if (MesDefine.OvenAlarm_CodeMessger != null && MesDefine.OvenAlarm_CodeMessger.ContainsKey(nAlarm))
            {
                ALARM_CODE = MesDefine.OvenAlarm_CodeMessger[nAlarm];
                alarms.ALARM_VALUE = 1;//1:触发报警 0:消除报警
                alarms.ALARM_CODE = nAlarm.ToString();
                alarms.START_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                MachineCtrl.GetInstance().MesSendAlarmInfo(this.strResourceID, alarms);
            }
        }

        /// <summary>
        /// 报警解除上传
        /// </summary>
        /// <param name="cavityData"></param>
        private void MesUpLoadResetAlarm(string nAlarm)
        {
            var alarms = new AlarmsItem();
            string ALARM_CODE = "";
            if ((nAlarm == null) && (nAlarm == "")) return;
            if (MesDefine.OvenAlarm_CodeMessger != null && MesDefine.OvenAlarm_CodeMessger.ContainsKey(nAlarm))
            {
                ALARM_CODE = MesDefine.OvenAlarm_CodeMessger[nAlarm];
                alarms.ALARM_VALUE = 0;//1:触发报警 0:消除报警
                alarms.ALARM_CODE = nAlarm.ToString();
                alarms.START_TIME = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                MachineCtrl.GetInstance().MesSendAlarmInfo(this.strResourceID, alarms);
            }
        }

        /// <summary>
        /// 上传干燥过程数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool FTPUploadFile(CavityData cavityData, int col, int nOvenID)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            
            var isCheckOK = false;
            string strMsg = "";

            ProcessDataUpload processDataUpload = new ProcessDataUpload();
            processDataUpload.ParamList = new List<paramListItem>();
            var strTempValue = cavityData.unBaseTempValue[0][0][0];
            string PZKHK005 = MachineCtrl.GetInstance().M_nOnloadTotal.ToString();
            string PZKHK006 = (MachineCtrl.GetInstance().M_nOnloadTotal - MachineCtrl.GetInstance().M_nNgTotal).ToString();
            Dictionary<string, string> data = new Dictionary<string, string>
            {
            { "PZKHK001", cavityData.UnWorkTime.ToString() },//设备运行时间
            { "PZKHK002", "0" },//设备故障时间
            { "PZKHK003",  arrStartTime[col].ToString() },//设备开机时间
            { "PZKHK004", "12" },//PPM
            { "PZKHK005", PZKHK005 },//生产总量
            { "PZKHK006", PZKHK006 },//良品总量
            { "PZKHK007", cavityData.ProcessParam.UnSetTempValue.ToString() },//烘箱界面设定-设定温度
            { "PZKHK008", cavityData.ProcessParam.UnTempUpperLimit.ToString() },//温度上限
            { "PZKHK009", cavityData.ProcessParam.UnTempLowerLimit.ToString() },//温度下限
            { "PZKHK010", strTempValue.ToString() },//当前烘烤温度
            { "PZKHK011", cavityData.ProcessParam.UnPreHeatTime.ToString() },//预热时间
            { "PZKHK012", cavityData.ProcessParam.UnVacHeatTime.ToString() },//真空加热时间
            { "PZKHK013", cavityData.ProcessParam.UnPressureLowerLimit.ToString() },//真空压力下限
            { "PZKHK014", cavityData.ProcessParam.UnPressureUpperLimit.ToString() },//真空压力上限
            { "PZKHK015", cavityData.unVacPressure.ToString() },//真空度
            { "PZKHK016", nRunTime[col].ToString() },//烘烤时长cavityData.unVacPressure
            { "PZKHK017", (col+1).ToString() },//层数
            { "PZKHK018", cavityData.ProcessParam.UnOpenDoorBlowTime.ToString() },//开门破真空时间
            { "PZKHK019", cavityData.ProcessParam.UnBreathTimeInterval.ToString() },//真空呼吸时间间隔
            { "PZKHK020", "10" },//预热呼吸时间间隔cavityData.ProcessParam.UnPreHeatBreathTimeInterval.ToString()
            { "PZKHK021", "60" },//预热呼吸干燥时间cavityData.ProcessParam.UnPreHeatBreathPreTimes.ToString()
            { "PZKHK022", "1000" },//预热呼吸压力cavityData.ProcessParam.UnPreHeatBreathPre.ToString()
            { "PZKHK023", "115" }//上位机温度上限
            };

            List<paramListItem> paramList = new List<paramListItem>();

            foreach (var item in data)
            {
                paramList.Add(new paramListItem { paramCode = item.Key, paramValue = item.Value });
            }
            try
            {

                isCheckOK = MachineCtrl.GetInstance().MesProcessDataUpload(nOvenID, paramList,ref strMsg);
                return true;
            }
            catch (Exception)
            {
                return false;

            }
        }

        /// <summary>
        /// 上传干燥过程数据-界面调用
        /// </summary>
        public bool FTPUploadFileData(int nCavityIdx, int col, int nOvenID)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            UpdateOvenData(bgCavityData);
            CavityData cavityData = bgCavityData[nCavityIdx];
            var isCheckOK = false;
            string strMsg = "";

            ProcessDataUpload processDataUpload = new ProcessDataUpload();
            processDataUpload.ParamList = new List<paramListItem>();
            var strTempValue = cavityData.unBaseTempValue[0][0][0];
            string PZKHK005 = MachineCtrl.GetInstance().M_nOnloadTotal.ToString();
            string PZKHK006 = (MachineCtrl.GetInstance().M_nOnloadTotal - MachineCtrl.GetInstance().M_nNgTotal).ToString();
            Dictionary<string, string> data = new Dictionary<string, string>
            {
            { "PZKHK001", cavityData.UnWorkTime.ToString() },//设备运行时间
            { "PZKHK002", "0" },//设备故障时间
            { "PZKHK003", "0" },//设备开机时间
            { "PZKHK004", "0" },//PPM
            { "PZKHK005", PZKHK005 },//生产总量
            { "PZKHK006", PZKHK006 },//良品总量
            { "PZKHK007", cavityData.ProcessParam.UnSetTempValue.ToString() },//烘箱界面设定-设定温度
            { "PZKHK008", cavityData.ProcessParam.UnTempUpperLimit.ToString() },//温度上限
            { "PZKHK009", cavityData.ProcessParam.UnTempLowerLimit.ToString() },//温度下限
            { "PZKHK010", strTempValue.ToString() },//当前烘烤温度
            { "PZKHK011", cavityData.ProcessParam.UnPreHeatTime.ToString() },//预热时间
            { "PZKHK012", cavityData.ProcessParam.UnVacHeatTime.ToString() },//真空加热时间
            { "PZKHK013", cavityData.ProcessParam.UnPressureLowerLimit.ToString() },//真空压力下限
            { "PZKHK014", cavityData.ProcessParam.UnPressureUpperLimit.ToString() },//真空压力上限
            { "PZKHK015", nOvenVacm[col].ToString() },//真空度
            { "PZKHK016", nRunTime[col].ToString() },//烘烤时长
            { "PZKHK017", col.ToString() },//层数
            { "PZKHK018", cavityData.ProcessParam.UnOpenDoorBlowTime.ToString() },//开门破真空时间
            { "PZKHK019", cavityData.ProcessParam.UnBreathTimeInterval.ToString() },//真空呼吸时间间隔
            { "PZKHK020", cavityData.ProcessParam.UnPreHeatBreathTimeInterval.ToString() },//预热呼吸时间间隔
            { "PZKHK021", cavityData.ProcessParam.UnPreHeatBreathPreTimes.ToString() },//预热呼吸干燥时间
            { "PZKHK022", cavityData.ProcessParam.UnPreHeatBreathPre.ToString() },//预热呼吸压力
            { "PZKHK023", "115" }//上位机温度上限
            };

            List<paramListItem> paramList = new List<paramListItem>();

            foreach (var item in data)
            {
                paramList.Add(new paramListItem { paramCode = item.Key, paramValue = item.Value });
            }
            try
            {

                isCheckOK = MachineCtrl.GetInstance().MesProcessDataUpload(nOvenID, paramList, ref strMsg);
                return true;
            }
            catch (Exception)
            {
                return false;

            }
        }

        //设备运行记录表
        private bool MySQLMesProductionRecordSheet(int col, string bar_code, string bill_no, string start_date, string end_date)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            string sqlStr = string.Format("insert into production_record_sheet (bar_code,bill_no,start_date,end_date,process_code,equiment_id,number,creator,create_time,pre_process,read_flag)" +
                " values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');"
                       , bar_code
                       , bill_no
                       , start_date
                       , end_date
                       , MachineCtrl.GetInstance().strHCMesParam[0]
                       , StrResourceID
                       , "1"
                       , "system"
                       , DateTime.Now
                       , bar_code
                       , 0);
            /*MachineCtrl.GetInstance().dbMySqlServer.CustomDBQuery(sqlStr);*/
            return true;
        }

        public bool DeleteProductionRecordSheetRecord(DateTime startTime, DateTime endTime)
        {
            bool result = false;

            string sql = @"DELETE FROM production_record_sheet WHERE 进站时间 BETWEEN @startTime AND @endTime";

            /*result = MachineCtrl.GetInstance().dbMySqlServer.CustomDBQuery(sql);*/

            return result;
        }

        //设备状态实时表
        private bool MySQLMesRealData(int col, string stateCode, string stateName)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            string sqlStr = string.Format("insert into equipment_real_data (equipment_id,process_code,state_code,state_name,update_time,ud1,ud2,ud3,read_flag) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}');"
                       , StrResourceID
                       , MachineCtrl.GetInstance().strHCMesParam[0]
                       , stateCode
                       , stateName
                       , DateTime.Now
                       , ""
                       , ""
                       , ""
                       , 0);

           /* MachineCtrl.GetInstance().dbMySqlServer.CustomDBQuery(sqlStr);*/
            return true;
        }

        //设备运行记录表
        private bool MySQLMesOperationRecord(int col, string stateCode, string stateName)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            string sqlStr = string.Format("insert into equipment_operation_record (process_code,equipment_id,state_code,state_name,start_date,end_date,read_flag) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}');"
                       , MachineCtrl.GetInstance().strHCMesParam[0]
                       , StrResourceID
                       , stateCode
                       , stateName
                       , DateTime.Now
                       , DateTime.Now
                       , 0);

           /* MachineCtrl.GetInstance().dbMySqlServer.CustomDBQuery(sqlStr);*/
            return true;
        }

        //报警设备信息
        private bool MySQLMesBakeAlarm(int col, string sMessage)
        {
            if (!MachineCtrl.GetInstance().UpdataMES)
            {
                return true;
            }
            string sqlStr = string.Format("insert into equipment_alarm_record (process_code,equipment_id,alarm_code,alarm_memo,start_date,end_date,read_flag) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}');"
                       , MachineCtrl.GetInstance().strHCMesParam[0]
                       , StrResourceID
                       , "04"
                       , sMessage
                       , DateTime.Now
                       , DateTime.Now
                       , 0);

/*            if (!MachineCtrl.GetInstance().dbMySqlServer.CustomDBQuery(sqlStr))
            {
                string strColHead = "", strFilePath = "D:\\MESLog\\本地数据表\\equipment_alarm_record";
                string strFileName = DateTime.Now.ToString("yyyyMMdd") + ".CSV";

                strColHead = "process_code,equipment_id,alarm_code,alarm_memo,start_date,end_date,read_flag";
                string strLog = string.Format("{0},{1},{2},{3},{4},{5},{6}"
                             , MachineCtrl.GetInstance().strHCMesParam[0]
                             , MachineCtrl.GetInstance().strResourceID[nOvenID, col]
                             , "04"
                             , sMessage
                             , DateTime.Now
                             , DateTime.Now
                             , 0);
                MachineCtrl.GetInstance().WriteCSV(strFilePath, strFileName, strColHead, strLog);
            }*/

            return true;
        }

        /// <summary>
        /// 托盘类型数量
        /// </summary>
        public void PalletTypeCount(int nCol)
        {
            for (int nType = 0; nType < (int)PltType.PltTypeEnd; nType++)
            {
                pltTypeCount[nCol, nType] = 0;
                pltTypePos[nCol, nType] = -1;
            }

            for (int row = (int)ModuleRowCol.DryingOvenRow - 1; row > -1; row--)
            {
                if (!IsCavityNg(nCol, row))
                {
                    if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.Invalid)
                    {
                        pltTypePos[nCol, (int)PltType.Invalid] = row;
                        pltTypeCount[nCol, (int)PltType.Invalid] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.OK)
                    {
                        if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].IsEmpty())
                        {
                            pltTypePos[nCol, (int)PltType.OK] = row;
                            pltTypeCount[nCol, (int)PltType.OK] += 1;
                        }
                        else
                        {
                            pltTypePos[nCol, (int)PltType.FullOK] = row;
                            pltTypeCount[nCol, (int)PltType.FullOK] += 1;
                        }
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.NG)
                    {
                        pltTypePos[nCol, (int)PltType.NG] = row;
                        pltTypeCount[nCol, (int)PltType.NG] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.Detect)
                    {
                        pltTypePos[nCol, (int)PltType.Detect] = row;
                        pltTypeCount[nCol, (int)PltType.Detect] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.WaitRes)
                    {
                        pltTypePos[nCol, (int)PltType.WaitRes] = row;
                        pltTypeCount[nCol, (int)PltType.WaitRes] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.WaitOffload)
                    {
                        pltTypePos[nCol, (int)PltType.WaitOffload] = row;
                        pltTypeCount[nCol, (int)PltType.WaitOffload] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.WaitRebakeBat)
                    {
                        pltTypePos[nCol, (int)PltType.WaitRebakeBat] = row;
                        pltTypeCount[nCol, (int)PltType.WaitRebakeBat] += 1;
                    }
                    else if (Pallet[nCol * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.WaitRebakingToOven)
                    {
                        pltTypePos[nCol, (int)PltType.WaitRebakingToOven] = row;
                        pltTypeCount[nCol, (int)PltType.WaitRebakingToOven] += 1;
                    }
                }
            }
        }

        /// <summary>
        /// 检查炉子有假OK电池托盘
        /// </summary>
        public bool OvenHasFakePlt(int nColIdx)
        {
            for (int row = 0; row < (int)ModuleRowCol.DryingOvenRow; row++)
            {
                if (PltHasTypeBat(Pallet[nColIdx * (int)ModuleRowCol.DryingOvenRow + row], BatType.Fake)
                    && Pallet[nColIdx * (int)ModuleRowCol.DryingOvenRow + row].Type == PltType.OK)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 增删托盘
        /// </summary>
        /// <param name="CurPltIndex"></param>
        /// <param name="add"></param>
        public void AddDeletePallte(int CurfurnaceChamber, int CurPltIndex, bool add)
        {
            int realPltIndex = CurfurnaceChamber  * (int)ModuleDef.PalletMaxRow + CurPltIndex;

            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (state == MCState.MCIdle)
            {
                ShowMsgBox.ShowDialog("设备处于闲置中，不能进行操作", MessageType.MsgWarning);
                return;
            }

            string strInfo;
            if (CurfurnaceChamber < 0)
            {
                return;
            }

            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            int number = GetBakingCount(CurfurnaceChamber);

            if ((number >= 2))
            {
                if (user.userLevel > UserLevelType.USER_ADMIN)
                {
                    strInfo = string.Format("干燥炉{0}第{1}号腔体烘烤次数{2}已大于等于2次,联系管理员,尽快处理干燥炉故障", nOvenID + 1, CurfurnaceChamber + 1, number + 1);
                    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgAlarm);
                    return;
                }
            }
            else
            {
                //if (user.userLevel > UserLevelType.USER_MAINTENANCE)
                //{
                //    ShowMsgBox.ShowDialog("用户权限不够", MessageType.MsgMessage);
                //    return;
                //}
            }

            if (IsCavityEN(CurfurnaceChamber))
            {
                strInfo = string.Format("手动停用干燥炉{0}第{1}号腔体!", nOvenID + 1, CurfurnaceChamber + 1);
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return;
            }

            strInfo = string.Format("危险操作，请慎重！手动{2}托盘数据\r\n点击【确定】将{2}干燥炉{0}第{1}号托盘数据，点击【取消】不执行!", nOvenID + 1, CurPltIndex + 1, add ? "添加" : "删除");
            if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
            {
                if (add)
                {
                    if (this.Pallet[realPltIndex].IsType(PltType.Invalid))
                    {
                        //this.Pallet[realPltIndex].Release();
                        this.Pallet[realPltIndex].Type = PltType.OK;
                        /*this.Pallet[realPltIndex].Bats.ForEach(batType => batType.Type = BatType.OK);*/
                        this.Pallet[realPltIndex].Stage = PltStage.Invalid;
                        _ = this.Pallet[realPltIndex].WhetherThereAreIsEmpty;
                    }
                    else
                    {
                        int m_nMaxJigRow = 0;
                        int m_nMaxJigCol = 0;
                        MachineCtrl.GetInstance().GetPltRowCol(ref m_nMaxJigRow, ref m_nMaxJigCol);
                            for (int nRow = 0; nRow < m_nMaxJigRow; nRow++)
                            {
                                for (int nCol = 0; nCol < m_nMaxJigCol; nCol++)
                                {
                                    if (this.Pallet[realPltIndex].Bat[nRow, nCol].Type == BatType.NG)
                                    {
                                        this.Pallet[realPltIndex].Bat[nRow, nCol].Type = BatType.OK;
                                    }
                                }
                            }
                        this.Pallet[realPltIndex].Type = PltType.OK;
                    }
                }
                else
                {
                    this.Pallet[realPltIndex].Release();
                    float[] fWcValue = new float[4] { -1,-1 ,-1 ,-1};
                   
                    this.SetWaterContent(realPltIndex, fWcValue);
                    strInfo = string.Format("清除干燥炉{0}第{1}号托盘数据成功!\r\n请手动操作将托盘移除！", nOvenID + 1, CurPltIndex + 1);
                    ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                }

              /*  TrolleyChangedCsv(nOvenID, CurPltIndex, add);*/
                this.nBakingType[CurfurnaceChamber] = 0;
                this.SetWCUploadStatus(CurfurnaceChamber, WCState.WCStateInvalid);
                this.SetCavityState(CurfurnaceChamber, CavityState.Standby);
                this.SaveRunData(SaveType.Pallet | SaveType.Battery | SaveType.Variables);
            }
        }
        /// <summary>
        /// 托盘NG
        /// </summary>
        public void PalletNG(int nCavityIdx, int nPltRow)
        {
            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (state == MCState.MCIdle)
            {
                ShowMsgBox.ShowDialog("设备处于闲置中，不能进行操作", MessageType.MsgWarning);
                return;
            }
            if (this.Pallet[nCavityIdx].Type == PltType.Invalid)
            {
                ShowMsgBox.ShowDialog("当前腔体没有小车，不能打NG", MessageType.MsgWarning);
                return;
            }
            string strInfo;
            if (nPltRow < 0 || nCavityIdx < 0)
            {
                return;
            }

            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            if (user.userLevel > UserLevelType.USER_MAINTENANCE)
            {
                //ShowMsgBox.ShowDialog("用户权限不够，请登陆管理员", MessageType.MsgMessage);
                //return;
            }
            if (this.IsCavityEN(nCavityIdx))
            {
                strInfo = string.Format("手动停用干燥炉{0}第{1}层!", nOvenID + 1, nCavityIdx + 1);
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return;
            }

            int m_nMaxJigRow = 0;
            int m_nMaxJigCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref m_nMaxJigRow, ref m_nMaxJigCol);

            strInfo = string.Format($"确定：将{this.RunName}，{nCavityIdx + 1}腔体中小车【第{nPltRow + 1}层托盘】打NG");
            if (ButtonResult.OK != ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
            {
                for (int nRow = 0; nRow < m_nMaxJigRow; nRow++)
                {
                    for (int nCol = 0; nCol < m_nMaxJigCol; nCol++)
                    {
                        if (this.Pallet[nCavityIdx].Bat[nRow, nCol].Type == BatType.OK)
                        {
                            this.Pallet[nCavityIdx].Bat[nRow, nCol].Type = BatType.NG;
                        }
                    }
                    this.Pallet[nCavityIdx].Type = PltType.NG;
                }
                //PalletChangedCsv(nOvenID, nCavityIdx, nPltRow, true);
                this.nBakingType[nCavityIdx] = 0;
                this.SetWCUploadStatus(nCavityIdx, WCState.WCStateInvalid);
                this.SetCavityState(nCavityIdx, CavityState.Standby);
                this.SaveRunData(SaveType.Pallet | SaveType.Battery | SaveType.Variables);
            }
        }



        /// <summary>
        /// 清除干燥炉任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClearOvenTask()
        {
            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if ((MCState.MCInitializing == state) || (MCState.MCRunning == state))
            {
                ShowMsgBox.ShowDialog("设备运行中不能修改", MessageType.MsgWarning);
                return;
            }

            UserFormula user = new UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user);
            if (user.userLevel > UserLevelType.USER_MAINTENANCE)
            {
                //ShowMsgBox.ShowDialog("用户权限不够，请登陆维护人员账号", MessageType.MsgMessage);
                //return;
            }
            //调度等待开始信号才能清除
            RunProTransferRobot transferRobot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
            if (!transferRobot.CheckTransRobotStep())
            {
                ShowMsgBox.ShowDialog($"{transferRobot.RunName}任务未完成，请等待调度任务完成后再清除炉子任务", MessageType.MsgMessage);
                return;
            }

            string strInfo = string.Format("危险操作，请慎重！数据删除将不可恢复\r\n点击【确定】将清除数据，点击【取消】不执行!");
            if (ButtonResult.OK == ShowMsgBox.ShowDialog(strInfo, MessageType.MsgQuestion).Result)
            {
                if (InitRunDataB())
                {
                    string strMsg = string.Format($"清除干燥炉【{this.RunName}】任务成功");
                    ShowMsgBox.ShowDialog(strMsg, MessageType.MsgMessage);

                    ClearDateCsv(strMsg);
                    return;
                }
            }
        }

        /// <summary>
        /// 清除数据CSV
        /// </summary>
        private void ClearDateCsv(string section)
        {
            DataBaseRecord.UserFormula curUser = new DataBaseRecord.UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref curUser);
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\ClearDate", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "清除数据.CSV";
            string sColHead = "清除时间,用户,模组名称";
            string sLog = string.Format("{0},{1},{2}"
                , DateTime.Now
                , curUser.userPassword
                , section);
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }

        /// <summary>
        /// 清除模组数据
        /// </summary>
        /// <returns></returns>
        public bool InitRunDataB()
        {

            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            // 信号初始化
            if (null != ArrEvent)
            {
                for (int nEventIdx = 0; nEventIdx < ArrEvent.Length; nEventIdx++)
                {
                    this.ArrEvent[nEventIdx].SetEvent((ModuleEvent)nEventIdx, (RunID)this.GetRunID());
                }
            }
            for (int nCavityIdx = 0; nCavityIdx < (int)DryingOvenCount.DryingOvenNum; nCavityIdx++)
            {
                SetCavityData(nCavityIdx).DoorState = OvenDoorState.Invalid;
            }
            SaveRunData(SaveType.AutoStep | SaveType.Variables);
            return true;

        }
        #endregion
    }
}
