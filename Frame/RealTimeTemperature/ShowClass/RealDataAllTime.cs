using CommunityToolkit.Mvvm.ComponentModel;
using Machine;
using ScottPlot.Demo.WPF.WpfDemos;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Views.Control;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class RealDataAllTime : ObservableObject
    {

        /// <summary>
        /// 干燥炉所有炉腔烘烤时间段温度数据
        /// </summary>
        [ObservableProperty]
        public List<BakingRealData> ovenCavityTimeData;

        /// <summary>
        /// 显示条件
        /// </summary>
        /// 结构 干燥炉<炉腔[托盘[条件属性：是否选择]]>
        [ObservableProperty]
        public static  Dictionary<string,Dictionary<string, Dictionary<string, List<Condition>>>> ovenDataConditionsTree = new Dictionary<string, Dictionary<string, Dictionary<string, List<Condition>>>>();

        public RealDataAllTime()
        {
            this.ovenCavityTimeData = new List<BakingRealData>();
            GetConditionsTree();
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, List<Condition>>>> GetConditionsTree()
        {
            if(OvenDataConditionsTree.Count != 0)
                return OvenDataConditionsTree;

            var bakingRealData = new BakingRealData();

            var allShowDataParameter = bakingRealData.PalltShowDataConditions;
            allShowDataParameter.AddRange(bakingRealData.CavityShowDataConditions);

            for (int i = 0; i < (int)DryingOvenCount.DryingOvenNum; i++)
            {
                var ovenC = new Dictionary<string, Dictionary<string, List<Condition>>>();

                string ovenName = "干燥炉" + (i+1);

                if (!OvenDataConditionsTree.ContainsKey(ovenName))
                    OvenDataConditionsTree.Add(ovenName, ovenC);

                for (int j = 0; j < (int)ModuleRowCol.DryingOvenCol; j++)
                {
                    var CavityC = new Dictionary<string, List<Condition>>();
                    string CavityName = "炉腔" + (j+1);
                    if (!OvenDataConditionsTree.ContainsKey(CavityName))
                        OvenDataConditionsTree[ovenName].Add(CavityName, CavityC);

                    for (int k = 0; k < (int)ModuleRowCol.DryingOvenRow; k++)
                    {
                        string PalltName = "托盘" + (k+1);
                        var PalltC = new List<Condition>();

                        if (!OvenDataConditionsTree[ovenName][CavityName].ContainsKey(PalltName))
                            OvenDataConditionsTree[ovenName][CavityName].Add(PalltName, PalltC);
                        OvenDataConditionsTree[ovenName][CavityName][PalltName] = allShowDataParameter;
                    }
                }
            }
            return OvenDataConditionsTree;
        }

        /// <summary>
        /// 获取显示图表线
        /// </summary>
        public List<ChartLine> GetShowChartLines()
        {
            return new List<ChartLine>();
        }

        /// <summary>
        /// 显示表格
        /// </summary>
        public bool ShowTable()
        {
            return true;
        }
        /// <summary>
        /// 导出csv文件
        /// </summary>
        public bool ExportCSV()
        {
            return true;
        }
    }
}
