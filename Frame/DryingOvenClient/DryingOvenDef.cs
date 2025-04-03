using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    /// <summary>
    /// 炉门状态
    /// </summary>
    enum OvenDoorState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
        Action,                     // 动作中
    }

    /// <summary>
    /// 工作状态
    /// </summary>
    enum OvenWorkState
    {
        Invalid = 0,                // 未知
        Stop,                       // 停止
        Start,                      // 启动
    }

    /// <summary>
    /// 真空状态
    /// </summary>
    enum OvenVacState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 破真空状态
    /// </summary>
    enum OvenBlowState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 破真空常压状态
    /// </summary>
    enum OvenBlowUsPreState
    {
        Invalid = 0,                // 未知
        Not,                        // 无
        Have,                       // 有
    }

    /// <summary>
    /// 光幕状态
    /// </summary>
    enum OvenScreenState
    {
        Invalid = 0,                // 未知
        Not,                        // 无光幕
        Have,                       // 有光幕
    }

    /// <summary>
    /// 屏蔽状态
    /// </summary>
    enum OvenShieldState
    {
        Invalid = 0,                // 未知
        Not,                        // 未屏蔽
        Have,                       // 有屏蔽
    }

    /// <summary>
    /// 加热状态
    /// </summary>
    enum OvenWarmState
    {
        Invalid = 0,                // 未知
        Not,                        // 未加热
        Have,                       // 有加热
    }

    /// <summary>
    /// 烘烤完成状态
    /// </summary>
    enum OvenBakeOverState
    {
        Invalid = 0,                // 未知
        Not,                        // 烘烤中
        Have,                       // 烘烤完成
    }

    /// <summary>
    /// 炉层NG状态
    /// </summary>
    enum OvenRowNgState
    {
        Invalid = 0,                // 未知
        Not,                        // 没NG
        Have,                       // 有NG
    }

    /// <summary>
    /// 托盘状态
    /// </summary>
    enum OvenPalletState
    {
        Invalid = 0,                // 未知
        Not,                        // 无托盘
        Have,                       // 有托盘
    }
    /// <summary>
    /// 联机状态
    /// </summary>
    enum OvenOnlineState
    {
        Invalid = 0,                // 未知
        Not,                        // 本地
        Have,                       // 联机
    }

    /// <summary>
    /// 保压状态
    /// </summary>
    enum OvenPressureState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 预热呼吸状态
    /// </summary>
    enum OvenPreHeatBreathState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 真空呼吸状态
    /// </summary>
    enum OvenVacBreathState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 上位机安全门状态
    /// </summary>
    enum PCSafeDoorState
    {
        Invalid = 0,                // 未知
        Close,                      // 关闭
        Open,                       // 打开
    }

    /// <summary>
    /// 托盘状态
    /// </summary>
    enum OvenPallteState
    {
        Invalid = 0,                // 未知
        Not,                        // 无托盘
        Have,                       // 有托盘
    }

    

    /// <summary>
    /// 配方设置
    /// </summary>
    enum FormulaSet
    {
        Invalid = 0,                // 未知
        Formula1,                   // 配方1
        Formula2,                   // 配方2
    }

    /// <summary>
    /// 复位状态
    /// </summary>
    enum OvenResetState
    {
        Invalid = 0,                // 无效
        Reset0  = 0,            // 复位0
        Reset,                      // 复位
    }

    /// <summary>
    /// 炉门报警
    /// </summary>
    enum OvenDoorAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 真空报警
    /// </summary>
    enum OvenVacAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 破真空报警
    /// </summary>
    enum OvenBlowAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 真空表报警
    /// </summary>
    enum OvenVacGaugeAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 呼吸排队报警
    /// </summary>
    enum OvenPreHBreathAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }


    /// <summary>
    /// 温度报警类型
    /// </summary>
    [Flags]
    enum OvenTempAlarm
    {
        Invalid = 0,        
        OverheatTmp = 0x01 << 0,    // 超温
        LowTmp = 0x01 << 1,         // 低温
        DifTmp = 0x01 << 2,         // 温差异常
        ExcTmp = 0x01 << 3,         // 信号异常
        ConTmp = 0x01 << 4,         // 温度不变
    }

    /// <summary>
    /// 炉子报警类型
    /// </summary>
    enum OvenAlarmType
    {
        Invalid = 0,                    // 无效
        OK = 0,                         // 正常
        HeatUpSlow = 0x01 << 0,         // 升温慢
        TempJump = 0x01 << 1,           // 温度跳变
        ElectBig = 0x01 << 2,           // 电流太大
        ElectSmall = 0x01 << 3,         // 电流太小
        CircuitDis = 0x01 << 4,         // 电路断开
        SecLowTemp = 0x01 << 5,         // 二级低温
        SecHighTemp = 0x01 << 6,        // 二级高温
        SolStateBreakDowm = 0x01 << 7,  // 固态击穿
        OneLowTemp = 0x01 << 8,         // 一级低温
        OneHighTemp = 0x01 << 9,        // 一级高温
        OverHeat = 0x01 << 10,          // 异常超温
        TempDiffBig = 0x01 << 11,       // 控检温差大
    }

    /// <summary>
    /// 温控报警
    /// </summary>
    enum OvenTempConlAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 光栅报警
    /// </summary>
    enum OvenRasterAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 压力急停报警
    /// </summary>
    enum OvenPressStopAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 其它报警
    /// </summary>
    enum OvenOtherAlarm
    {
        Invalid = 0,                    // 未知
        OK = 0,                         // 正常
        OpenCloseBright = 0x01 << 0,    // 开关门传感器同时亮
        VacZero = 0x01 << 1,            // 真空变送器电路异常报警
    }

    /// <summary>
    /// 抽充报警
    /// </summary>
    enum OvenTakeOutBlowAlarm
    {
        Invalid = 0,                // 未知
        Not,
        Alarm,
    }

    /// <summary>
    /// 干燥炉类型定义
    /// </summary>
    enum DryOvenNumDef
    {
        HeatPanelNum = 3,           // 发热板数量
        TempTypeNum = 2,            // 温度类型（0：实际温度 1：巡检温度）

        GraphMaxCount = 120 * 10,    //  温度曲线最大数
    }

    /// <summary>
    /// 干燥炉命令索引
    /// </summary>
    public enum DryOvenCmd
    {
        AlarmState = 0,             // 报警状态（读）
        SignalState,                // 信号状态（读）
        EquiData,                   // 设备数据（读）
        ReadParam,                  // 工艺参数（读）

        WriteParam,                 // 工艺参数（写）
        StartOperate,               // 启动操作启动/停止（写）
        DoorOperate,                // 炉门操作打开/关闭（写）
        VacOperate,                 // 真空操作打开/关闭（写）
        BreakVacOperate,            // 破真空操作打开/关闭（写）
        PressureOperate,            // 保压打开/关闭（写）
        FaultReset,                 // 故障复位（写）
        PreHeatBreathOperate,       // 预热呼吸
        VacBreathOperate,           // 真空呼吸
        PCSafeDoorState,            // 上位机安全门状态打开/关闭（写）
        FormulaSet,                 // 配方设置

        End,
    }

    /// <summary>
    /// 命令地址
    /// </summary>
    public struct DryOvenCmdAddr
    {
        public int area;            // 区域代码
        public int wordAddr;        // 字起首地址
        public int bitAddr;         // 位起首地址
        public int count;           // 数量
        public int interval;        // 地址间隔

        public DryOvenCmdAddr(int nArea, int nWordAddr, int nBitAddr, int nCount, int nInterval)
        {
            this.area = nArea;
            this.wordAddr = nWordAddr;
            this.bitAddr = nBitAddr;
            this.count = nCount;
            this.interval = nInterval;
        }
    };

    public class DryingOvenDef
    {
        #region // 中文名称描述

        /// <summary>
        /// 炉子报警类型
        /// </summary>
        public static string[] OvenAlarmTypeName = new string[]
        {
            "超温",
            "低温",
            "温差",
            "温度信号异常",
            "温度不变化",
            "二级低温",
            "二级高温",
            "固态击穿",
            "一级低温",
            "一级高温",
            "异常超温",
            "控检温差大",
        };

        /// <summary>
        /// 炉子报警类型
        /// </summary>
        public static string[] OvenParaName = new string[]
        {
            "烘烤时间(分钟)",
            "电池烘烤温度(℃)",
            "SV1温度增加量(℃)",
            "SV2温度增加量(℃)",
            "SV3温度增加量(℃)",
            "SV4温度增加量(℃)",
            "SV5温度增加量(℃)",
            "SV2开始时间(分钟)",
            "SV3开始时间(分钟)",
            "SV4开始时间(分钟)",
            "SV5开始时间(分钟)",
            "预热段1压力下限(Pa)",
            "预热段1压力上限(Pa)",
            "预热段2开始时间(分钟)",
            "预热段2压力下限(Pa)",
            "预热段2压力上限(Pa)",
            "高真空段开始时间(分钟)",
            "高真空段首抽真空压力(Pa)",
            "高真空段1开始时间(分钟)",
            "高真空段1结束时间(分钟)",
            "高真空段1压力下限(Pa)",
            "高真空段1压力上限(Pa)",
            "高真空段2开始时间(分钟)",
            "高真空段2结束时间(分钟)",
            "高真空段2抽真空周期(秒)",
            "高真空段2抽真空时间(秒)",
            "呼吸开始时间(分钟)",
            "呼吸结束时间(分钟)",
            //"呼吸触发压力(Pa)",
            "目标呼吸次数",
            "呼吸最小间隔时间(分钟)",
            "呼吸充气后压力(Pa)",
            "呼吸后保持时间(秒)",
        };




        #endregion
    }
}
