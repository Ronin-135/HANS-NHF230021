using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperLibrary;
using Machine;
using Machine.Framework.ExtensionMethod;
using Prism.Ioc;
using ScottPlot.Demo.WPF.WpfDemos;
using ScottPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.RealTimeTemperature;
using WPFMachine.Frame.Userlib;
using System.Reflection;
using ImTools;
using Condition = WPFMachine.Frame.RealTimeTemperature.Condition;
using SystemControlLibrary;
using System.Xml.Linq;

namespace WPFMachine.ViewModels
{
    internal partial class OvenChartWinModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<RunProDryingOven> ovens = new ObservableCollection<RunProDryingOven>();

        [ObservableProperty]
        private RunProDryingOven curOven;

     
        [ObservableProperty]
        private BakingRealData bakingRealData = new BakingRealData();

        [ObservableProperty]
        public List<Condition> conditions;

        [ObservableProperty]
        public Dictionary<string,  int> curOvenDataConditionsTree;

        [ObservableProperty]
        public List<int> curCavityDataConditions = new List<int>();

        [ObservableProperty]
        public List<int> curPalletDataConditions = new List<int>();

        [ObservableProperty]
        public int curCavity =1;

        [ObservableProperty]
        public int curPallet =1;
        

        public OvenChartWinModel(IEnumerable<RunProDryingOven> dryingOvens)
        {
            ovens.AddRange(dryingOvens);

            for (int j = 0; j < (int)ModuleRowCol.DryingOvenCol; j++)
            {
                int CavityName =(j + 1);
                curCavityDataConditions.Add(CavityName);
            }
            
            for (int j = 0; j < (int)ModuleRowCol.DryingOvenRow; j++)
            {
                int PalletName = (j + 1);
                curPalletDataConditions.Add(PalletName);
            }

            Conditions = bakingRealData.CavityShowDataConditions;
            Conditions.AddRange(bakingRealData.PalltShowDataConditions);
            //for(int i = 0; i<800; i++ )
            //    testSql();
            //ShowChartForCondithion();
        }

        [RelayCommand]
        public void ShowChartForCondithion()
        {

            /// 查询数据
            OvenPositionInfo ovenPositionInfo = new OvenPositionInfo();
            ovenPositionInfo.OvenID1 = CurOven.GetOvenID();
            ovenPositionInfo.Cavity1 = CurCavity;
            ovenPositionInfo.PalltRow = CurPallet;

            var PalltShowDatas =  RealDataHelp.SelectRealData(ovenPositionInfo);

            if (PalltShowDatas == null || PalltShowDatas.Count == 0)
            {
                ShowMsgBox.ShowDialog("当前选择条件下，无干燥数据！", MessageType.MsgMessage);
                return;
            }

            ///根据条件筛选要显示的内容
            Dictionary<String, PropertyInfo> propertyInfos = new Dictionary<string, PropertyInfo>();
            var ShowPropertys = PalltShowData.GetShowProperty();

            Conditions.ForEach(condition => 
            {
                if (condition.IsShow && ShowPropertys.ContainsKey(condition.ChName))
                    propertyInfos.Add(condition.ChName, ShowPropertys[condition.ChName]);
            });

            ///将搜索结果数据整合成 名字为key 值列表为value的结构
            List<ChartLine> lines = new List<ChartLine>();
            double sampleRate = 6;
            Dictionary<string,List<double>> keyValuePairs = new Dictionary<string, List<double>>();

            PalltShowDatas.ForEach(PalltShowData => 
            {
                propertyInfos.ForEach(Property =>
                {
                    if (keyValuePairs.ContainsKey(Property.Key))
                        keyValuePairs[Property.Key].Add((double)Property.Value.GetValue(PalltShowData));
                    else
                        keyValuePairs.Add(Property.Key,new List<double>() { (double)Property.Value.GetValue(PalltShowData) });
                });
            });

            ///将处理好的键值对打包成图表显示用的线
            keyValuePairs.ForEach(lineData => 
                lines.Add(new ChartLine(ys: lineData.Value.ToArray(), sampleRate: sampleRate, label: lineData.Key,offsetX: DateTime.Now.ToOADate()))
            );

            ///创建弹窗放入曲线数据显示图表
            var win = Activator.CreateInstance(typeof(MouseTracker), lines) as Window;
            win.Width = 1000;
            win.Height = 600;
            win.Title = "干燥炉"+(CurOven.GetOvenID()+1)+"炉腔"+ CurCavity+"托盘"+ CurPallet + "图表";
            win.Show();
            ///导入图表

        }

        public void testSql()
        {


            var FilePath = "E:\\you\\海四达\\参考项目\\自动烘烤";
            var FileName = "20231209-3D.CSV";

            try
            {
                FilePath = Path.Combine(FilePath, FileName);
                StreamReader sr = new StreamReader(FilePath, System.Text.Encoding.UTF8);
                string line;

                bool i = true;
                List<string> titleList = new List<string>();
                Dictionary<string, List<string>> titleDic = new Dictionary<string, List<string>>();
                OvenPositionInfo opiP = new OvenPositionInfo();
                CavityShowData csdP = new CavityShowData();


                while ((line = sr.ReadLine()) != null)
                {
                    if (i)
                    {
                        line = line.Substring(0, line.Length-1);
                        titleList = line.Split(',').ToList();
                        foreach (var item in titleList)
                            titleDic.TryAdd(item.Trim(), new List<string>());
                        i= false;
                        continue;
                    }
                    var values = line.Split(",");
                    titleList.ForEach((string item,int index)=>titleDic[item].Add(values[index]));
                }

                string code = "";
                long BakingGuid = 12;
                DateTime CurTime = DateTime.Now;
                List<PalltShowData> palltShowDatas = new List<PalltShowData>();
                titleDic["小车条码(A)"].ForEach((string palletCode,int index) =>
                {
                    PalltShowData psdP = new PalltShowData();

                    if (code != palletCode)
                    {
                        BakingGuid = psdP.BakingGuid+1;
                        CurTime = DateTime.Now;
                        code = palletCode;
                    }

                    psdP.BakingGuid = 9;
                    psdP.CurTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    psdP.RunTimel = int.Parse(titleDic["当前运行时间(Minute)"][index]);
                    psdP.TemperatureControl1 = float.Parse(titleDic["1#底板控温1"][index]);
                    psdP.Inspection1 = float.Parse(titleDic["1#底板巡检1"][index]);
                    psdP.Inspection2 = float.Parse(titleDic["1#底板巡检2"][index]);
                    psdP.Inspection3 = float.Parse(titleDic["1#底板巡检3"][index]);

                    palltShowDatas.Add(psdP);
                });

                RealDataHelp.AddRealPalltShowData(palltShowDatas);

                sr.Close();
                return;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("文件：{0}导出失败！\r\n{1}", FilePath, ex.Message));
            }
        }


    }
}
