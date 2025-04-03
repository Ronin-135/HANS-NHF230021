using CommunityToolkit.Mvvm.ComponentModel;
using ImTools;
using Machine;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.DataStructure.Event;

namespace WPFMachine.Frame.DataStructure
{
    public partial class MEvent : ObservableObjectResourceDictionary
    {
        private ModuleEventName moduleEventName;

        private ModuleEvent modEvent;    // 事件
        [ObservableProperty]
        private EventState state;        // 状态
        [ObservableProperty]
        private int rowIdx;              // 行号
        [ObservableProperty]
        private int colIdx;              // 列号
        private int param;              // 参数
        [ObservableProperty]
        private string station;         //交互模组工位
        [ObservableProperty]
        private string runName;         //交互模组名
        [ObservableProperty]
        private string modEventName;    // 事件名
        [ObservableProperty]
        private string startTime;       //开始时间
        private long runTime;       //开始时间


        public ModuleEvent ModEvent { get => modEvent; set => modEvent = value; }

        //public EventState State { get => state; set => state = value; }
        //public int RowIdx { get => rowIdx; set => rowIdx = value; }
        //public int ColIdx { get => colIdx; set => colIdx = value; }
        public int Param { get => param; set => param = value; }
        //public string Station { get => this.RowIdx + "行-" + this.ColIdx + "列"; }
        //public string RunName { get => runName; set => runName = value; }
        //public long RunTime { get => (long)(DateTime.Now - StartTime).TotalSeconds ; }
        //public DateTime StartTime { get => startTime; set => startTime = value; }
        //public string ModEventName { get => modEventName; set => modEventName = value; }
        private ModuleEventName GetModuleEventName { 
            get{ 
                    //if (this.moduleEventName != null) 
                        return this.moduleEventName;
                    //else return new ModuleEventName(); 
                }
        }
        public void SetEvent(ModuleEvent modEvent, EventState state = EventState.Invalid, int nRowIdx = -1, int nColIdx = -1, int nParam = -1, RunID runID = RunID.RunIDEnd,string strRunName ="")
        {
            this.ModEvent = modEvent;
            this.State = state;
            this.RowIdx = nRowIdx;
            this.ColIdx = nColIdx;
            this.Param = nParam;
            this.Station = (this.RowIdx+1) + "行-" + (this.ColIdx+1) + "列";
            this.StartTime = DateTime.Now.ToString("dd - HH:mm:ss");
            this.RunName = strRunName;
            this.ModEventName = GetRunNamefromModuleEvent(runID);
        }

        public void SetEvent(ModuleEvent modEvent, RunID runID)
        {
            this.ModEvent = modEvent;
            this.State = EventState.Invalid;
            this.RowIdx = -1;
            this.ColIdx = -1;
            this.Param = -1;
            this.StartTime = DateTime.Now.ToString("dd - HH:mm:ss");
            //this.RunName = strRunName;
            this.ModEventName = GetRunNamefromModuleEvent(runID);
        }

        public void GetEvent(ref ModuleEvent modEvent, ref EventState state, ref int nRowIdx, ref int nColIdx, ref int nParam)
        {
            modEvent = this.ModEvent;
            state = this.State;
            nRowIdx = this.RowIdx;
            nColIdx = this.ColIdx;
            nParam = this.Param;
        }

        public string GetRunNamefromModuleEvent(RunID runID)
        {
            string v = "";

            switch (runID)
            {
                case RunID.OnloadLineScan:
                    v = ((ModuleEventName)((int)ModuleEventName.来料扫码发送电池 + (int)ModEvent)).ToString();
                    break;
                case RunID.OnloadLine:
                    v = ((ModuleEventName)((int)ModuleEventName.来料线发送取电池 + (int)ModEvent)).ToString();
                    break;
                case RunID.OnloadBuffer:
                    v = ((ModuleEventName)((int)ModuleEventName.取上料缓存 + (int)ModEvent)).ToString();
                    break;
                case RunID.OnloadFake:
                    v = ((ModuleEventName)((int)ModuleEventName.假电池输入线 + (int)ModEvent)).ToString();
                    break;
                case RunID.OnloadNG:
                    v = ((ModuleEventName)((int)ModuleEventName.NG输出线 + (int)ModEvent)).ToString();
                    break;
                case RunID.OnloadRobot:
                    v = ((ModuleEventName)((int)ModuleEventName.上料区放空托盘 + (int)ModEvent)).ToString();
                    break;
                case RunID.DryOven0:
                case RunID.DryOven1:
                case RunID.DryOven2:
                case RunID.DryOven3:
                case RunID.DryOven4:
                case RunID.DryOven5:
                    v = ((ModuleEventName)((int)ModuleEventName.干燥炉放空托盘 + (int)ModEvent)).ToString();
                    break;
                case RunID.ManualOperate:
                    v = ((ModuleEventName)((int)ModuleEventName.人工操作平台放NG空托盘 + (int)ModEvent)).ToString();
                    break;
                case RunID.PalletBuf:
                    v = ((ModuleEventName)((int)ModuleEventName.缓存架放空托盘 + (int)ModEvent)).ToString();
                    break;
                case RunID.Transfer:
                    v = ((ModuleEventName)((int)ModuleEventName.缓存架结束 + (int)ModEvent)).ToString();
                    break;
                case RunID.OffloadBuffer:
                    v = ((ModuleEventName)((int)ModuleEventName.取下料缓存 + (int)ModEvent)).ToString();
                    break;
                case RunID.OffloadLine:
                    v = ((ModuleEventName)((int)ModuleEventName.下料物流线 + (int)ModEvent)).ToString();
                    break;
                case RunID.OffloadRobot:
                    v = ((ModuleEventName)((int)ModuleEventName.下料区放干燥完成托盘 + (int)ModEvent)).ToString();
                    break;
                case RunID.OffloadFake:
                    v = ((ModuleEventName)((int)ModuleEventName.假电池输出线 + (int)ModEvent)).ToString();
                    break;
                case RunID.CoolingStove:
                    v = ((ModuleEventName)((int)ModuleEventName.冷却炉动作 + (int)ModEvent)).ToString();
                    break;
            }

            //var names = GetModuleEventName.keyValuePairs.ForEach(
            //    modEvent =>
            //    {
            //        if (modEvent.Value.RunName == strRunName && modEvent.Value.index == (int)ModEvent)
            //            v = modEvent.Value.EventName;
            //    });
            return v;
        }
    }
}
