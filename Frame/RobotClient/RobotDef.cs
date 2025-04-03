using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPFMachine.Frame.DataStructure.Enumeration;

namespace Machine
{
    // 机器人类型
    public enum RobotType
    {
        ABB = 0,         // ABB
        KUKA,            // KUKA
        FANUC,           // FANUC
        END,
    };

    // 机器人动作
    public enum RobotAction
    {
        HOME = 0,                   // 归位
        MOVE,                       // 移动
        DOWN,                       // 下降
        UP,                         // 上升
        PICKIN,                     // 取进
        PICKOUT,                    // 取出
        PLACEIN,                    // 放进
        PLACEOUT,                   // 放出

        END,                        // 结束符
        MOVING,                     // 移动中
        FINISH,                     // 移动完成
        TIMEOUT,                    // 移动超时
        INVALID,                    // 结果无效
        DISCONNECT,                 // 断开连接
        ERR,                        // 结果错误
        ACTION_END,
    };

    // 机器人命令帧格式
    public enum RobotCmdFrame
    {
        Station = 0,                // 工位
        StationRow,                 // 行
        StationCol,                 // 列
        Speed,                      // 速度
        Action,                     // 动作
        Result,                     // 执行结果
        End,                        // 指令结束
    };

    // 机器人ID
    public enum RobotIndexID
    {
        Invalid = -1,
        OnloadRobot = 0,            // 上料机器人
        TransferRobot,              // 调度机器人
        OffloadRobot,               // 下料机器人
        End,
    };

    // 上料机器人工位
    public enum OnloadRobotStation
    {
        Invalid = 0,                // 无效工位
        [Info("回零位", 1, 1, MotorPosition.Onload_LinePickPos)] Home,                         // 回零位
        [Info("来料取料位", 1, 4, MotorPosition.Onload_LinePickPos)] OnloadLine,               // 来料取料位
        //[Info("复投线", 1, 1, MotorPosition.Onload_RedeliveryPickPos)] RedeliveryLine,               // 复投线
        [Info("托盘扫码0", 1, 1, MotorPosition.Onload_ScanPalletPos)] PltScanCode_0,             // 托盘扫码0
        [Info("托盘扫码1", 1, 1, MotorPosition.Onload_ScanPalletPos)] PltScanCode_1,             // 托盘扫码1
        [Info("托盘扫码2", 1, 1, MotorPosition.Onload_ScanPalletPos)] PltScanCode_2,             // 托盘扫码2
        [Info("上料夹具0", 6, 20, MotorPosition.Onload_PalletPos)] Pallet_0,                  // 上料夹具0
        [Info("上料夹具1", 6, 20, MotorPosition.Onload_PalletPos)] Pallet_1,                  // 上料夹具1
        [Info("上料夹具2", 6, 20, MotorPosition.Onload_PalletPos)] Pallet_2,                  // 上料夹具2
        //[Info("NG转盘工位", 1, 1, MotorPosition.Onload_PalletPos)] NGTurnTable,              // NG转盘工位
        [Info("暂存工位", 1, 8, MotorPosition.Onload_MidBufPos)] BatBuf,                     // 暂存工位
        [Info("NG电池输出工位", 1, 1, MotorPosition.Onload_NGPos)] NGOutput,             // NG电池输出工位
        [Info("假电池输入工位", 1, 4, MotorPosition.Onload_FakePos)] FakeInput,            // 假电池输入工位
        [Info("假电池扫码工位", 1, 4, MotorPosition.Onload_FakePos)] FakeScanCode,         // 假电池扫码工位
        //[Info("复投线扫码工位", 1, 1, MotorPosition.Onload_RedeliveryPickPos)] RedeliveryScanCode,   // 复投线扫码工位
        StationEnd,                 // 结束
    };

    // 调度机器人工位
    public enum TransferRobotStation
    {
        Invalid = 0,                // 无效工位
        [Info("干燥炉1", 8, 2)] DryingOven_0,           // 【4】干燥炉1      8行2列
        [Info("干燥炉2", 8, 2)] DryingOven_1,           // 【5】干燥炉2      8行2列
        [Info("干燥炉3", 8, 2)] DryingOven_2,           // 【6】干燥炉3      8行2列
        [Info("干燥炉4", 8, 2)] DryingOven_3,           // 【7】干燥炉4      8行2列
        [Info("干燥炉5", 8, 2)] DryingOven_4,           // 【8】干燥炉5      8行2列
        [Info("干燥炉6", 8, 2)] DryingOven_5,           // 【9】干燥炉6      8行2列
        [Info("冷却炉", 4, 1)] CooligStove,             // 冷却炉
        [Info("上料区域站点", 1, 3)] OnloadStation,     // 【1】上料区域站点 1行3列
        [Info("下料区域站点", 1, 2)] OffloadStation,    // 【2】下料区域站点 1行2列
        [Info("托盘缓存架", 4, 1)] PalletBuffer,
        [Info("人工操作平台", 1, 1)] ManualOperat,      // 【3】人工操作平台 1行1列
        StationEnd,                 // 结束
    };

    // 下料机器人工位
    public enum OffloadRobotStation
    {
        Invalid = 0,                // 无效工位
        [Info("回零位", 1, 1, MotorPosition.Offload_LinePos)] Home,                       // 回零位
        [Info("下料线放料位", 1, 1, MotorPosition.Offload_LinePos)] OffloadLine,                // 下料线放料位
        [Info("假电池输出工位", 1, 2, MotorPosition.Offload_FakePos)] FakeOutput,             // 假电池输出工位
        [Info("下料托盘0", 6, 20, MotorPosition.Offload_PalletPos)] Pallet_0,                   // 下料托盘0
        [Info("下料托盘1", 6, 20, MotorPosition.Offload_PalletPos)] Pallet_1,                   // 下料托盘1
        [Info("暂存工位", 1, 8, MotorPosition.Offload_MidBufPos)] BatBuf,                 // 暂存工位
        StationEnd,                 // 结束
    };

    /// <summary>
    /// 机器人动作信息
    /// </summary>
    public partial class RobotActionInfo :ObservableObject, IRobotInfoBase
    {
        [ObservableProperty]
        private int station;
        [ObservableProperty]
        private int row;                 // 行
        [ObservableProperty]
        private int col;                 // 列
        public RobotAction action;            // 动作指令
        public string stationName { get; set; }     // 工位名


        int IRobotInfoBase.action => (int)action;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RobotActionInfo()
        {
            Release();
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void Release()
        {
            SetInfo(0, 0, 0, 0, "");
        }

        /// <summary>
        /// 设置信息
        /// </summary>
        public void SetInfo(int curStation, int curRow, int curCol, RobotAction curAction, string curStaionName)
        {
            Station = curStation;
            Row = curRow;
            Col = curCol;
            this.action = curAction;
            this.stationName = curStaionName;
        }
    }
    public class RobotDef
    {
        #region // 中文名称描述

        /// <summary>
        /// 机器人指令名
        /// </summary>
        public static string[] RobotActionName = new string[]
        {
            "归位",
            "移动",
            "下降",
            "上升",
            "取进",
            "取出",
            "放进",
            "放出",
            "查询位置",

            "指令结束标识",

            "动作中",
            "完成",
            "超时",
            "无效",
            "错误",
        };

        /// <summary>
        /// 机器人ID名
        /// </summary>
        public static string[] RobotName = new string[]
        {
            "上料机器人",
            "调度机器人",
            "下料机器人"
        };

        #endregion

    }

        class Info : Attribute
        {
            public string info { get; set; }

            public int MaxRow { get; set; }

            public int MaxCol { get; set; }

            public MotorPosition Motor { get; set; }

            public Info(string name, int maxRow, int maxCol, MotorPosition motor = MotorPosition.Invalid)
            {
                info = name;
                MaxRow = maxRow;
                MaxCol = maxCol;
                Motor = motor;
            }
        }
}
