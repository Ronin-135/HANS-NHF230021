using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FastDeepCloner;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.ExtensionMethod;
using WPFMachine.Frame.Userlib;

namespace Machine
{
    partial class CavityData : ObservableObject
    {
        #region // 字段

        /// <summary>
        /// 参数
        /// </summary>
        [ObservableProperty]
        private ProcessParam processParam = new ProcessParam();

        public static (PropertyInfo info, OvenParameterAttribute att)[] PropListInfo;

        [ObservableProperty]
        public uint breathCycleCount;                 // 呼吸循环次数
                                                      //
        [ObservableProperty]
        public uint formulaSet;                       //配方设置
        
        [ObservableProperty]
        public uint unCurFormulaNo;                   //当前配方
        public uint unBakingOverBat { get; set; }                         // 烘烤完成电芯数量


        // 【状态数据】
        /// <summary>
        /// 炉门状态
        /// </summary>
        [ObservableProperty]
        private OvenDoorState doorState;
        //public OvenOpenDoorState OpenDoorState;                       // 开门炉门状态
        /// <summary>
        /// 工作状态
        /// </summary>
        [ObservableProperty]
        private OvenWorkState workState;
        /// <summary>
        /// 真空阀状态
        /// </summary>
        [ObservableProperty]
        private OvenVacState vacState;
        /// <summary>
        /// 破真空阀状态
        /// </summary>
        [ObservableProperty]
        private OvenBlowState blowState;
        /// <summary>
        /// 破真空常压状态
        /// </summary>
        [ObservableProperty]
        private OvenBlowUsPreState blowUsPreState;
        /// <summary>
        /// 故障复位
        /// </summary>
        [ObservableProperty]
        private OvenResetState faultReset;

        /// <summary>
        /// 保压状态
        /// </summary>
        [ObservableProperty]
        private OvenPressureState pressureState;
        /// <summary>
        /// 预热呼吸状态
        /// </summary>
        [ObservableProperty]
        public OvenPreHeatBreathState preHeatBreathState;

        /// <summary>
        /// 真空呼吸状态
        /// </summary>
        [ObservableProperty]

        public OvenVacBreathState vacBreathState;

        /// <summary>
        /// 上位机安全门状态
        /// </summary>
        [ObservableProperty]
        public PCSafeDoorState pcSafeDoorState;


        /// <summary>
        /// 工作时间
        /// </summary>
        [ObservableProperty]
        public uint unWorkTime;

        /// <summary>
        // 真空压力
        /// </summary>
        [ObservableProperty]
        public float unVacPressure;


        /// <summary>
        // 实时温度
        // 结构：报警温度值[类型(巡检或温度)][托盘数][发热板]
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ObservableCollection<ObservableCollection<float>>> unBaseTempValue = new();


        /// <summary>
        // 温度报警值
        // 结构：报警温度值[托盘数][发热板]
        /// </summary>        
        [ObservableProperty]
        public ObservableCollection<ObservableCollection<float>> unAlarmTempValue = new();

        /// <summary>
        // 温度报警状态
        // 结构：报警状态[托盘，报警类型]
        /// </summary>        
        [ObservableProperty]
        public ObservableCollection<ObservableCollection<ObservableCollection<OvenTempAlarm>>> unAlarmTempState = new(); // 报警状态[托盘，报警类型]

        //public float[,,][] unSideTempValue = new float[(int)PltMaxCount.PltCount, (int)DryOvenNumDef.SidePlate, (int)DryOvenNumDef.TempType][]; // 温度值[托盘数, 侧板数，类型][控温/巡检]
        /* [ObservableProperty]
         public ObservableCollection<float> unDoorTempValue = new (Enumerable.Repeat(default(float), (int)DryOvenNumDef.DoorPlank)); // 温度值[门板]*/



        /// <summary>
        /// 光幕状态
        /// </summary>
        [ObservableProperty]
        public OvenScreenState screenState;                   // 

        [ObservableProperty]
        public ObservableCollection<OvenWarmState> warmState = new() { OvenWarmState.Invalid };                    // 加热状态
        [ObservableProperty]
        public OvenOnlineState onlineState;                  // 联机状态

        public uint unRealPower { get; set; }                             // 实时电量
        public uint unVacBkBTime { get; set; }                             // 真空小于100PA时间

        public uint unPreBreatheCount { get; set; }                        // 预热呼吸次数
        public uint unVacBreatheCount { get; set; }                        // 真空呼吸次数
        // 【报警信息】                                      
        public OvenDoorAlarm DoorAlarm { get; set; }                       // 炉门报警
        public OvenVacAlarm VacAlarm { get; set; }                         // 真空报警
        public OvenBlowAlarm BlowAlarm { get; set; }                       // 破真空报警
        public OvenVacGaugeAlarm VacGauge { get; set; }                    // 真空表报警
        public OvenPreHBreathAlarm PreHeatBreathAlarm { get; set; }        // 预热呼吸排队报警                           
        public OvenTakeOutBlowAlarm TakeOutBlowAlarm { get; set; }          // 抽充报警
        public OvenRasterAlarm RasterAlarm { get; set; }                    // 光栅报警
        public OvenTempConlAlarm TempConlAlarm { get; set; }                // 温控报警
        public OvenPressStopAlarm PressStopAlarm { get; set; }              // 压力急停报警
        public OvenOtherAlarm[] OtherAlarm { get; set; }                    // 其他报警
        public OvenTempAlarm[] TempAlarmState { get; set; }                 // 温度报警
        

        //public ObservableCollection<ObservableCollection<OvenTempAlarm>> BaseTempAlarmState { get; set; } = new OvenTempAlarm[(int)PltMaxCount.PltCount, (int)DryOvenNumDef.BasePlate];// 报警状态[托盘数, 底板数，控温，巡检]
        /*        [ObservableProperty]
               public ObservableCollection<ObservableCollection<OvenTempAlarm>> baseTempAlarmState = new(Enumerable.Range(0, (int)PltMaxCount.PltCount).Select(i => new ObservableCollection<OvenTempAlarm>(Enumerable.Repeat(default(OvenTempAlarm), (int)DryOvenNumDef.HeatPanelNum))));// 报警状态[托盘数, 底板数，控温，巡检]
              [ObservableProperty]
               public ObservableCollection<ObservableCollection<OvenTempAlarm>> sideTempAlarmState = new(Enumerable.Range(0, (int)PltMaxCount.PltCount).Select(i => new ObservableCollection<OvenTempAlarm>(Enumerable.Repeat(default(OvenTempAlarm), (int)DryOvenNumDef.SidePlate)))); // 报警状态[托盘数, 侧板数，巡检]

               public ObservableCollection<OvenTempAlarm> DoorTempAlarmState { get; set; } = new(Enumerable.Repeat(default(OvenTempAlarm), (int)DryOvenNumDef.DoorPlank)); // 报警状态[门板]*/
        public uint[] unVacAlarmValue { get; set; } = new uint[2];                        // 真空报警值[2个]
        public float[,,] unTempValue { get; set; } = new float[(int)PltMaxCount.PltCount,2 ,(int)DryOvenNumDef.HeatPanelNum]; // 报警温度值[托盘数, 温度类型, 底板数]
     /*   public float[,] SideTempAlarmValue { get; set; } = new float[(int)PltMaxCount.PltCount, (int)DryOvenNumDef.HeatPanelNum]; // 报警温度值[托盘数, 侧板数]*/
       /* public float[] DoorTempAlarmValue { get; set; } = new float[(int)DryOvenNumDef.DoorPlank]; // 报警温度值[门板]*/

        // 【整炉参数】                                      
        public float unOneDayEnergy { get; set; }                          // 单日耗能
        public float unBatAverEnergy { get; set; }                         // 电芯平均能耗

        public float fEnergySum { get; set; }                        // 总电量
        public float fMinutesEnergy { get; set; }                   // 五分钟耗电量
        public float fWatts { get; set; }                            // 功率
        public float fEnergyTime { get; set; }                       // 耗电时间  


        /// <summary>
        /// 托盘状态[托盘数]
        /// </summary>
        [ObservableProperty]
        public OvenPalletState[] pltState = new OvenPalletState[(int)PltMaxCount.PltCount];              // 托盘状态[托盘数]
        public OvenShieldState[] shieldState;           // 屏蔽状态[托盘数]
        public OvenBakeOverState[] bakeOverState;       // 烘烤完成状态[托盘数]
        public OvenRowNgState[] rowNgState;             // 炉层NG状态[托盘数]

        // 【腔体数据锁】
        public object dataLock = new object();

        public float fRealWCValue { get; set; }

        #endregion


        #region // 构造函数

        public CavityData()
        {
            PropListInfo = processParam.GetPropListInfo();
            for (int nTempType = 0; nTempType < (int)DryOvenNumDef.TempTypeNum; nTempType++)
            {
                var unBaselist = new ObservableCollection<ObservableCollection<float>>();
                for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
                {
                    var add = new ObservableCollection<float>();
                    if (nTempType == 0)
                    {
                        //add.Add(0f);
                        add.AddRange(Enumerable.Repeat(0f, (int)DryOvenNumDef.HeatPanelNum));

                        unBaselist.Add(add);
                        continue;
                    }
                    add.AddRange(Enumerable.Repeat(0f, (int)DryOvenNumDef.HeatPanelNum));
                    unBaselist.Add(add);
                }
                UnBaseTempValue.Add(unBaselist);
            }

            for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++) 
            {
                ObservableCollection<ObservableCollection<OvenTempAlarm>> basePlts = new ObservableCollection<ObservableCollection<OvenTempAlarm>>();
                for (int basePlt = 0; basePlt < (int)DryOvenNumDef.HeatPanelNum; basePlt++)
                {
                    ObservableCollection<OvenTempAlarm> Baselist = new ObservableCollection<OvenTempAlarm>();
                    Baselist.AddRange(Enumerable.Repeat(OvenTempAlarm.Invalid, 5));
                    basePlts.Add(Baselist);
                }
                unAlarmTempState.Add(basePlts);
            }

            for (int nPlt = 0; nPlt < (int)PltMaxCount.PltCount; nPlt++)
            {
                ObservableCollection<float> Baselist = new ObservableCollection<float>();
                Baselist.AddRange(Enumerable.Repeat(0f, (int)DryOvenNumDef.HeatPanelNum));
                unAlarmTempValue.Add(Baselist);
            }
            //unAlarmTempValue.AddRange(Enumerable.Repeat(0f, (int)PltMaxCount.PltCount));





            // 创建对象

        }

        #endregion


        #region // 方法

        public bool CopyFrom(CavityData cavityData)
        {
            if (null != cavityData)
            {
                if (this == cavityData)
                {
                    return true;
                }

                lock (this.dataLock)
                {
                    lock (cavityData.dataLock)
                    {
                        DeepCloner.CloneTo(cavityData, this);
                        #region
                        //// 【设置参数】
                        //unBreathCycleCount = cavityData.unBreathCycleCount;
                        //unBakingOverBat = cavityData.unBakingOverBat;

                        //// 【状态数据】
                        //DoorState = cavityData.DoorState;
                        //WorkState = cavityData.WorkState;
                        //unWorkTime = cavityData.unWorkTime;
                        //unVacBkBTime = cavityData.unVacBkBTime;
                        //VacState = cavityData.VacState;
                        //BlowState = cavityData.BlowState;
                        //BlowUsPreState = cavityData.BlowUsPreState;
                        //FaultReset = cavityData.FaultReset;
                        //PressureState = cavityData.PressureState;
                        //PreHeatBreathState = cavityData.PreHeatBreathState;
                        //VacBreathState = cavityData.VacBreathState;
                        //PcSafeDoorState = cavityData.PcSafeDoorState;
                        //ScreenState = cavityData.ScreenState;
                        //OnlineState = cavityData.OnlineState;
                        //unRealPower = cavityData.unRealPower;
                        //unPreBreatheCount = cavityData.unPreBreatheCount;
                        //unVacBreatheCount = cavityData.unVacBreatheCount;
                        //// 【报警信息】
                        //DoorAlarm = cavityData.DoorAlarm;
                        //BlowAlarm = cavityData.BlowAlarm;
                        //VacGauge = cavityData.VacGauge;
                        //VacAlarm = cavityData.VacAlarm;
                        //PreHeatBreathAlarm = cavityData.PreHeatBreathAlarm;

                        //for (int nPltIdx = 0; nPltIdx < TrolleyState.GetLength(0); nPltIdx++)
                        //{
                        //    TrolleyState[nPltIdx] = cavityData.TrolleyState[nPltIdx];
                        //    unVacPressure[nPltIdx] = cavityData.unVacPressure[nPltIdx];
                        //    unVacAlarmValue[nPltIdx] = cavityData.unVacAlarmValue[nPltIdx];
                        //}

                        //for (int n1DIdx = 0; n1DIdx < unTempValue.GetLength(0); n1DIdx++)
                        //{
                        //    for (int n2DIdx = 0; n2DIdx < unTempValue.GetLength(1); n2DIdx++)
                        //    {
                        //        for (int n3DIdx = 0; n3DIdx < unTempValue.GetLength(2); n3DIdx++)
                        //        {
                        //            TempAlarmState[n1DIdx, n2DIdx, n3DIdx] = cavityData.TempAlarmState[n1DIdx, n2DIdx, n3DIdx];
                        //            unTempAlarmValue[n1DIdx, n2DIdx, n3DIdx] = cavityData.unTempAlarmValue[n1DIdx, n2DIdx, n3DIdx];
                        //            unTempValue[n1DIdx, n2DIdx, n3DIdx] = cavityData.unTempValue[n1DIdx, n2DIdx, n3DIdx];
                        //        }
                        //    }
                        //}

                        //for (int nWarmIdx = 0; nWarmIdx < WarmState.GetLength(0); nWarmIdx++)
                        //{
                        //    WarmState[nWarmIdx] = cavityData.WarmState[nWarmIdx];
                        //}

                        //fEnergySum = cavityData.fEnergySum;
                        //unOneDayEnergy = cavityData.unOneDayEnergy;
                        //unBatAverEnergy = cavityData.unBatAverEnergy;
                        #endregion
                    }
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// 清除数据
        /// </summary>
        public void Release()
        {
            //new CavityData().DeepCloneTo(this);

            #region
            //// 【设置参数】

            //unBreathCycleCount = 0;
            //unBakingOverBat = 0;
            //// 【状态数据】
            //DoorState = OvenDoorState.Invalid;
            //WorkState = OvenWorkState.Invalid;
            //unWorkTime = 0;
            //VacState = OvenVacState.Invalid;
            //BlowState = OvenBlowState.Invalid;
            //BlowUsPreState = OvenBlowUsPreState.Invalid;
            //PreHeatBreathState = OvenPreHeatBreathState.Invalid;
            //VacBreathState = OvenVacBreathState.Invalid;
            //PcSafeDoorState = PCSafeDoorState.Invalid;
            //FaultReset = OvenResetState.Invalid;
            //PressureState = OvenPressureState.Invalid;
            //ScreenState = OvenScreenState.Invalid;                       
            //OnlineState = OvenOnlineState.Invalid;
            //unRealPower = 0;

            //unPreBreatheCount = 0;
            //unVacBreatheCount = 0;
            //// 【报警信息】
            //DoorAlarm = OvenDoorAlarm.Invalid;
            //BlowAlarm = OvenBlowAlarm.Invalid;
            //VacGauge = OvenVacGaugeAlarm.Invalid;
            //VacAlarm = OvenVacAlarm.Invalid;
            //PreHeatBreathAlarm = OvenPreHBreathAlarm.Invalid;

            //for (int nPltIdx = 0; nPltIdx < TrolleyState.GetLength(0); nPltIdx++)
            //{
            //    TrolleyState[nPltIdx] = 0;
            //    unVacPressure[nPltIdx] = 100000;
            //    unVacAlarmValue[nPltIdx] = 0;
            //}

            //for (int n1DIdx = 0; n1DIdx < unTempValue.GetLength(0); n1DIdx++)
            //{
            //    for (int n2DIdx = 0; n2DIdx < unTempValue.GetLength(1); n2DIdx++)
            //    {
            //        for (int n3DIdx = 0; n3DIdx < unTempValue.GetLength(2); n3DIdx++)
            //        {
            //            TempAlarmState[n1DIdx, n2DIdx, n3DIdx] = OvenTempAlarm.Invalid;
            //            unTempAlarmValue[n1DIdx, n2DIdx, n3DIdx] = 0;
            //            unTempValue[n1DIdx, n2DIdx, n3DIdx] = 0;
            //        }
            //    }
            //}

            //for (int nWarmIdx = 0; nWarmIdx < WarmState.GetLength(0); nWarmIdx++)
            //{
            //    WarmState[nWarmIdx] = 0;
            //}

            //// 【整炉参数】
            //fEnergySum = 0;                   
            //unOneDayEnergy = 0;                    
            //unBatAverEnergy = 0;                    
            #endregion
        }
    }

    #endregion
}
