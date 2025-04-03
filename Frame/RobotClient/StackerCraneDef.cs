using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Machine
{
    // 堆垛机动作
    public enum StackerCraneAction
    {
        INVALID = 0,                // 无效
        MOVE,                       // 移动
        PICKIN,                     // 取进
        PICKOUT,                    // 取出
        PLACEIN,                    // 放进
        PLACEOUT,                   // 放出
        END,                        // 结束

        MOVING,                     // 动作中
        FINISH,                     // 动作完成
        TIMEOUT,                    // 动作超时
        ERR,                        // 指令错误
        ACTION_ERR,                 // 堆垛机报警
        ACTION_END,

    };

    // 堆垛机命令帧格式
    public enum StackerCraneCmdFrame
    {
        Station = 0,                // 工位
        StationRow,                 // 行
        StationCol,                 // 列
        Speed,                      // 速度
        Action,                     // 动作
        Result,                     // 执行结果
        End,                        // 结束
    };

    // 堆垛机工位
    public enum StackerCraneStation
    {
        Invalid = 0,                // 无效工位
        //[Info("托盘缓存架", 1, 2)] PalletBuffer,        
        [Info("干燥炉1", 2, 2)] DryingOven_0,           // 【1】干燥炉1      2行2列
        [Info("干燥炉2", 2, 2)] DryingOven_1,           // 【2】干燥炉2      2行2列
        [Info("干燥炉3", 2, 2)] DryingOven_2,           // 【3】干燥炉3      2行2列
        [Info("干燥炉4", 2, 2)] DryingOven_3,           // 【4】干燥炉4      2行2列
        [Info("干燥炉5", 2, 2)] DryingOven_4,           // 【5】干燥炉5      2行2列
        [Info("干燥炉6", 2, 2)] DryingOven_5,           // 【6】干燥炉6      2行2列
        [Info("上料区域站点", 1, 2)] OnloadStation,     // 【7】上料区域站点 1行2列
        [Info("下料区域站点", 1, 2)] OffloadStation,    // 【8】下料区域站点 1行2列
        [Info("人工操作平台", 1, 1)] ManualOperat,      // 【9】人工操作平台 1行1列
                                                           
        StationEnd,                 // 结束                
    };                                                     
                                                           
    public  interface IRobotInfoBase                       
    {
         int Station { get; }        // 工位
         int Row {get;}                 // 行
         int Col { get; }                 // 列
         int action { get; }      // 动作指令
         string stationName { get; }      // 工位名

    }

    /// <summary>
    /// 堆垛机动作信息
    /// </summary>
    public partial class StackerCraneActionInfo : ObservableObject, IRobotInfoBase
    {
        [ObservableProperty]
        private int speed;           // 速度

        [ObservableProperty]
        private int station;           // 工位

        [ObservableProperty]

        private int row;              // 行

        [ObservableProperty]
        private int col;                // 列


        public StackerCraneAction action;   // 动作指令

        public string stationName { get; set; }       // 工位名

        int IRobotInfoBase.action => (int)action;



        /// <summary>
        /// 构造函数
        /// </summary>
        public StackerCraneActionInfo()
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
        public void SetInfo(int Station, int Row, int Col, StackerCraneAction Action, string StaionName)
        {
            this.Station = Station;
            this.Row = Row;
            this.Col = Col;
            this.action = Action;
            this.stationName = StaionName;
        }

        public bool CopyFrom(StackerCraneActionInfo info)
        {
            if (null == info) return false;
            if (this == info) return true;

            this.Station = info.Station;
            this.Row = info.Row;
            this.Col = info.Col;
            this.action = info.action;
            this.stationName = info.stationName;
            return true;
        }
    }

    /// <summary>
    /// 堆垛机信息表
    /// </summary>
    public class StackerCraneFormula
    {
        public string trolleyName;
        public int stationID;
        public string stationName;
        public int maxRow;
        public int maxCol;

        public StackerCraneFormula()
        {
            this.trolleyName = string.Empty;
            this.stationID = 0;
            this.stationName = string.Empty;
            this.maxRow = 0;
            this.maxCol = 0;
        }

        public StackerCraneFormula(string trolley_name, int station_id, string station_name, int max_row, int max_col)
        {
            this.trolleyName = trolley_name;
            this.stationID = station_id;
            this.stationName = station_name;
            this.maxRow = max_row;
            this.maxCol = max_col;
        }
    }


    public class StackerCraneDef
    {
        #region // 中文名称描述

        /// <summary>
        /// 堆垛机指令名
        /// </summary>
        public static string[] StackerCraneActionName = new string[]
        {
            "待命",
            "移动",
            "取",
            "放",
            "结束",
        };

        /// <summary>
        /// 名称
        /// </summary>
        public static string StackerCraneName = "堆垛机";

        #endregion

    }
}

