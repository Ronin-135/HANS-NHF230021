using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;

namespace WPFMachine.Frame.DataStructure.Enumeration
{
    public enum PltMaxCount
    {
        PltCount = 8,
    }
    public enum DryingOvenCount
    {
        DryingOvenNum = RunID.RunIDEnd - RunID.DryOven0,
    }
    /// <summary>
    /// 模组ID
    /// </summary>
    public enum RunID
    {
        OnloadLineScan = 0,             // 来料扫码
        OnloadLine,                     // 来料线
        OnloadFake,                     // 假电池输入线
        OnloadNG,                       // 上料NG线
        OnloadRobot,                    // 上料机器人
        OnloadBuffer,                   // 上料配对
        Transfer,                       // 调度机器人
        PalletBuf,                      // 托盘缓存
        ManualOperate,                  // 人工操作台
        OffloadLine,                    // 下料物流线
        OffloadFake,                    // 下料假电池输出线
        OffloadRobot,                   // 下料机器人
        OffloadBuffer,                  // 下料配对
        CoolingStove,                   // 冷却炉
        DryOven0,                       // 干燥炉1
        DryOven1,                       // 干燥炉2
        DryOven2,                       // 干燥炉3
        DryOven3,                       // 干燥炉4
        DryOven4,                       // 干燥炉5
        DryOven5,                       // 干燥炉6
        RunIDEnd,
    }

    /// <summary>
    /// 运行数据保存类型
    /// </summary>
    public enum SaveType
    {
        AutoStep = 0x01 << 0,           // 步骤（自动流程步骤）
        Variables = 0x01 << 1,          // 变量（成员变量）
        SignalEvent = 0x01 << 2,        // 信号
        Battery = 0x01 << 3,            // 电池（抓手||缓存||假电池||NG||暂存）
        Pallet = 0x01 << 4,             // 治具（托盘||料框）
        Cylinder = 0x01 << 5,           // 气缸状态
        Motor = 0x01 << 6,              // 电机位置
        Robot = 0x01 << 7,              // 机器人位置
        Cavity = 0x01 << 8,             // 干燥炉腔体数据
        MaxMinValue = 0x01 << 9,        // 当前值最大最小值
    };
    

    /// <summary>
    /// 水含量类型
    /// </summary>
    public enum WaterMode
    {
        混合型 = 0,    // 混合型
        阳极,          // 阳极
        阴极,          // 阴极
        阴阳极,        // 阴阳极
        //阴阳隔膜级,    //阴阳隔膜级
    }

    /// <summary>
    /// 模组中电机点位
    /// </summary>
    public enum MotorPosition
    {
        Invalid = -1,

        // 上料机器人
        Onload_LinePickPos = 0,         // 来料取料位间距
        Onload_RedeliveryPickPos,       // 复投线取料位
        Onload_ScanPalletPos,           // 托盘扫码位间距
        Onload_MarBufPos,               // 边缘暂存位间距
        Onload_MidBufPos,               // 中间暂存位间距
        Onload_PalletPos,               // 托盘放料位间距
        Onload_FakePos,                 // 假电池取料位间距
        Onload_NGPos,                   // NG输出放料位间距
        Onload_Pos_End,                 // 结束

        // 下料机器人
        Offload_LinePos = 0,            // 下料放料位间距
        Offload_MarBufPos,              // 边缘暂存位间距
        Offload_MidBufPos,              // 中间暂存位间距
        Offload_PalletPos,              // 托盘取料位间距
        Offload_FakePos,                // 假电池放料位间距
        Offload_Pos_End,                // 结束
    }

    /// <summary>
    /// 模组中的最大托盘数
    /// </summary>
    public enum ModuleMaxPallet
    {
        OnloadRobot = 3,
        TransferRobot = 1,
        DryingOven = 16,
        PalletBuf = 4,
        ManualOperat = 1,
        OffloadRobot = 2,
    }

    /// <summary>
    /// 模组报警ID范围
    /// </summary>
    enum ModuleMsgID
    {
        // 模组其实ID在库ID后开始
        SystemStartID = LibMsgID.MsgLibIDEnd,
        SystemEndID = SystemStartID + 99,

        // RunProOnloadLineScan
        OnloadLineScanMsgStartID,
        OnloadLineScanMsgEndID = OnloadLineScanMsgStartID + 99,

        // RunProcessOnloadLine
        OnloadLineMsgStartID,
        OnloadLineMsgEndID = OnloadLineMsgStartID + 9,

        // RunProcessOnloadFake
        OnloadFakeMsgStartID,
        OnloadFakeMsgEndID = OnloadFakeMsgStartID + 9,

        // RunProcessOnloadNG
        OnloadNGMsgStartID,
        OnloadNGMsgEndID = OnloadNGMsgStartID + 9,

        // RunProcessOnloadRobot
        OnloadRobotMsgStartID,
        OnloadRobotMsgEndID = OnloadRobotMsgStartID + 49,

        // RunProOnloadBuffer
        OnloadBufferMsgStartID,
        OnloadBufferMsgMsgEndID = OnloadBufferMsgStartID + 9,

        // RunProTransferRobot
        TransferRobotMsgStartID,
        TransferRobotMsgEndID = TransferRobotMsgStartID + 49,

        // RunProPalletBuf
        PalletBufferMsgStartID,
        PalletBufferMsgEndID = PalletBufferMsgStartID + 9,

        // RunProManualOperat
        ManualOperatMsgStartID,
        ManualOperatMsgEndID = ManualOperatMsgStartID + 9,

        // RunProOffloadLine
        OffloadLineMsgStartID,
        OffloadLineMsgEndID = OffloadLineMsgStartID + 9,

        // RunProOffloadFake
        OffloadFakeMsgStartID,
        OffloadFakeMsgEndID = OffloadFakeMsgStartID + 9,

        // RunProcessOffloadRobot
        OffloadRobotMsgStartID,
        OffloadRobotMsgEndID = OffloadRobotMsgStartID + 49,

        // RunProOffloadBuffer
        OffloadBufferMsgStartID,
        OffloadBufferMsgMsgEndID = OffloadBufferMsgStartID + 9,

        // RunProDryingOven
        DryingOvenMsgStartID,
        DryingOvenMsgEndID = DryingOvenMsgStartID + 99,
    }

    /// <summary>
    /// 模组的行列数量
    /// </summary>
    public enum ModuleRowCol
    {
        // 上料机器人
        OnloadRobotRow = 1,
        OnloadRobotCol = 3,
        // 调度机器人
        TransferRobotRow = 1,
        TransferRobotCol = 1,
        // 干燥炉
        DryingOvenRow = 8,
        DryingOvenCol = 2,
        // 缓存架
        PalletBufRow = 4,
        PalletBufCol = 1,
        // 人工平台
        ManualOperatRow = 1,
        ManualOperatCol = 1,
        // 下料机器人
        OffloadRobotRow = 1,
        OffloadRobotCol = 2,
    }

    /// <summary>
    /// 电池数组行列
    /// </summary>
    public enum ArrBatRowCol
    {
        MaxRow = 12,
        MaxCol = 4,
    }

    /// <summary>
    /// 干燥炉腔体状态
    /// </summary>
    public enum CavityState
    {
        Invalid = 0,                    // 无效状态
        Standby,                        // 待机状态
        Work,                           // 工作状态
        Detect,                         // 待检测状态
        WaitRes,                        // 等待结果
        Rebaking,                       // 假电池回炉状态
        Maintenance,                    // 维修状态
        Transfer,                       // 转移状态
    }

    /// <summary>
    /// 设备系统IO组数量
    /// </summary>
    enum SystemIOGroup
    {
        PanelButton = 2,                // 面板按钮组
        LightTower = 2,                 // 灯塔组
        SafeDoor = 3,                   // 安全门组
        HeartBeat = 2,                  // 心跳
        OnOffLoadRobot = 4,             // 上下料机器人报警
        TransferRobot = 2,              // 调度机器人报警
        RobotCrash = 2,                 // 机器人碰撞
        IEStopNum = 5                   // 急停按钮
    }

    /// <summary>
    /// 自动水含量状态
    /// </summary>
    enum WCState
    {
        WCStateInvalid = 0,      // 无效状态
        WCStateUpLoad,           // 上传状态
        WCStateWaitFinish,       // 等待上传完成
    };

    /// <summary>
    /// 真空泵数量
    /// </summary>
    enum PumpCount
    {
        pumpCount = 3,    // 运行状态
    };

    /// <summary>
    /// 真空泵运行状态
    /// </summary>
    enum PumpRuntate
    {
        PumpStateRun = 1,    // 运行状态
        PumpStateStop,       // 停止状态
    };

    /// <summary>
    /// 真空泵报警状态
    /// </summary>
    enum PumpAlarmState
    {
        PumpNoAlarm = 0,              // 无报警
        PumpDigitalAlarm = 1,         // 数字报警
        PumpLowWarning = 9,           // 低警告
        PumpLowAlarm = 10,            // 低报警
        PumpHigeWarning = 11,         // 高警告
        PumpHigeAlarm = 12,           // 高报警
        PumpDeivceError = 13,         // 设备错误
        PumpDeivceNotPresent = 14,    // 设备不存在
    };

}
