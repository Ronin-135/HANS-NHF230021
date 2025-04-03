using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperLibrary;
using ImTools;
using Machine;
using Prism.Services.Dialogs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;
using WPFMachine.Frame.DataStructure;
using WPFMachine.Frame.DataStructure.Enumeration;
using WPFMachine.Frame.Userlib;
using WPFMachine.Views;

namespace WPFMachine.ViewModels
{
    internal partial class MesInterfaceViewModel : ObservableObject
    {
        #region 水含量相关
        [ObservableProperty]
        private ObservableCollection<RunProDryingOven> runs = new ObservableCollection<RunProDryingOven>();

        [ObservableProperty]
        private RunProDryingOven curOven;

        [ObservableProperty]
        private int[] furnaceChambers;

        [ObservableProperty]
        private int curfurnaceChamber;


        [ObservableProperty]
        private int[] pltsIndex;

        [ObservableProperty]
        private int curPltIndex;

        [ObservableProperty]
        private WaterMode[] waterModes;

        [ObservableProperty]
        private WaterMode curWaterMode;

        [ObservableProperty]
        private ObservableCollection<WaterUpType> wateUpTypes = new();

        [ObservableProperty]
        private float reworkRecord;

        [ObservableProperty]
        private float environmentalDewPoint;

        #endregion

        #region 配方相关
        /// <summary>
        /// 单个配方
        /// </summary>
        [ObservableProperty]
        private KeyValuePair<int, FormulaDb[]> curParams;

        /// <summary>
        /// 当前炉子
        /// </summary>
        [ObservableProperty]
        private RunProDryingOven curFormulaOven;
        /// <summary>
        /// 配方
        /// </summary>
        [ObservableProperty]
        private BindingList<KeyValuePair<int, FormulaDb[]>> formulas = new();

        /// <summary>
        /// 配方总数据
        /// </summary>
        private readonly BindingList<FormulaDb> totalFormula = new();

        [ObservableProperty]
        private ObservableCollection<int> recipeNumber = new ObservableCollection<int>();
        #endregion

        #region 删除任务相关

        [ObservableProperty]
        private ObservableCollection<RunProcess> runProcesses =new ObservableCollection<RunProcess>();

        [ObservableProperty]
        private RunProcess curDeleTaskRun;
        #endregion

        ISqlSugarClient db;

        #region MES
        [ObservableProperty]
        private ObservableCollection<MesConfig> mesInterface = new ObservableCollection<MesConfig>();

        [ObservableProperty]
        private MesConfig curMesInterface;


        #endregion
      
        public MesInterfaceViewModel(IEnumerable<RunProcess> runModes, IEnumerable<RunProDryingOven> ovens, ISqlSugarClient sugarClient)
        {
            Runs.AddRange(ovens);
            db = sugarClient;

            WaterModes = Enum.GetNames(typeof(WaterMode)).Select(name => (WaterMode)Enum.Parse(typeof(WaterMode), name)).ToArray();

            // 初始化配方数据
            InitFormulasDb();

            MesInterface.AddRange(Enum.GetValues<Machine.MesInterface>().Select(val => MesDefine.GetMesCfg(val)));

            CurWaterMode = MachineCtrl.GetInstance().eWaterMode;

            runProcesses.AddRange(runModes);
        }

        #region 配方
        /// <summary>
        /// 初始化配方
        /// </summary>
        private void InitFormulasDb()
        {
            db.CodeFirst.InitTables<FormulaDb>();
            totalFormula.AddRange(db.Queryable<FormulaDb>().ToArray());

            var g = totalFormula.GroupBy(f => f.Formulaid);
            db.Ado.BeginTran();

            // 检查配方
            if (!g.Any())
            {
                // 默认给10个配方
                for (int i = 0; i < 10; i++)
                {
                    CavityData.PropListInfo.ForEach(f =>
                    {
                        var fdb = new FormulaDb { Formulaid = i + 1, Name = f.att.Name, ParamIndex = f.att.IndexPrarmeter };
                        totalFormula.Add(fdb);
                        fdb.Id = db.Insertable(fdb).ExecuteReturnIdentity();

                    });
                }
                g = totalFormula.GroupBy(f => f.Formulaid);
            }

            // 检查配方
            foreach (var fs in g)
            {
                CavityData.PropListInfo.ForEach(fdb =>
                {
                    var curfdb = fs.FirstOrDefault(f => f.ParamIndex == fdb.att.IndexPrarmeter);
                    // 缺少就添加
                    if (curfdb == null)
                    {
                        curfdb = new FormulaDb { Formulaid = fs.Key, Name = fdb.att.Name, ParamIndex = fdb.att.IndexPrarmeter };
                        totalFormula.Add(curfdb);
                        curfdb.Id = db.Insertable(curfdb).ExecuteReturnIdentity();

                    }
                    // 除了索引，和值，全部跟新
                    curfdb.Name = fdb.att.Name;
                });
            }
            totalFormula.ForEach(f => db.Updateable(f));

            db.Ado.CommitTran();

            // 最终分组
            g = totalFormula.GroupBy(f => f.Formulaid);

            // 绑定跟新事件

            totalFormula.ListChanged += (s, e) =>
            {
                db.Updateable(totalFormula[e.NewIndex]).ExecuteCommand();
                ManualChangedtotalFormulaCsv(totalFormula[e.NewIndex].Formulaid
                    , totalFormula[e.NewIndex].Name
                    , MachineCtrl.GetInstance().parameterOldValue.ToString()
                    , totalFormula[e.NewIndex].ParameterValue.ToString());
            };
            foreach (var fg in g)
            {
                Formulas.Add(new System.Collections.Generic.KeyValuePair<int, FormulaDb[]>(fg.Key, fg.OrderBy(d => d.ParamIndex).ToArray()));
            }
        }

        protected void ManualChangedtotalFormulaCsv(int formulaIndex, string strName, string oldStr, string newStr)
        {
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\ParameterChanged", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "配方数据修改记录.CSV";
            string sColHead = "修改时间,账号,配方号, 参数名,参数旧值,参数新值";
            string sLog = string.Format("{0},{1},{2},{3},{4},{5}"
                , DateTime.Now
                , MachineCtrl.MachineCtrlInstance.CurUser?.UserName ?? "未登录"
                , formulaIndex
                , strName
                , oldStr
                , newStr
               );
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }
        #endregion

        #region 水含量上传相关

        partial void OnCurOvenChanged(RunProDryingOven oldValue, RunProDryingOven newValue)
        {
            FurnaceChambers = Enumerable.Range(1, newValue.CavityDataSource.Length).ToArray();
            PltsIndex = Enumerable.Range(1, newValue.CavityDataSource[CurfurnaceChamber].Plts.Count).ToArray();
            if (oldValue != null)
                oldValue.CavityDataSource[CurfurnaceChamber].PropertyChanged -= WaterGridPropertyChange;
            newValue.CavityDataSource[CurfurnaceChamber].PropertyChanged += WaterGridPropertyChange;

            UpWaterGrid();


        }

        partial void OnCurWaterModeChanged(WaterMode newValue)
        {
            MachineCtrl.GetInstance().SaveWaterMode(newValue);
        }

        /// <summary>
        /// 增删托盘
        /// </summary>
        /// <param name="isAdd"></param>
        [RelayCommand]
        private void AddDeletePallte(bool isAdd)
        {
            CurOven.AddDeletePallte(CurfurnaceChamber,CurPltIndex, isAdd);
        }

        /// <summary>
        /// 托盘打NG
        /// </summary>
        /// <param name="isAdd"></param>
        [RelayCommand]
        private void PalletNG()
        {
           /* CurOven.PalletNG(CurfurnaceChamber, CurPltIndex);*/
        }

        /// <summary>
        /// 清除模组任务
        /// </summary>
        [RelayCommand]
        private void ClearTask()
        {
            //CurDeleTaskRun.DeleteRunData();
            MCState mCState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (mCState == MCState.MCRunning || mCState == MCState.MCInitializing || mCState == MCState.MCIdle)
            {
                ShowMsgBox.ShowDialog("设备处于运行中或闲置中，禁止清任务", MessageType.MsgWarning);
                return;
            }
            //干燥炉清任务检查调度机器人位置
            if ((CurDeleTaskRun.GetRunID()>= (int)RunID.DryOven0) && (CurDeleTaskRun.GetRunID()<= (int)RunID.DryOven5))
            {
                RunProTransferRobot runTransfer = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProTransferRobot;
                int nTransferRobotStation = CurDeleTaskRun.GetRunID() - 13;
                if (runTransfer.CheckRobotPos(nTransferRobotStation))
                {
                    ShowMsgBox.ShowDialog("调度在取料，请移至安全位", MessageType.MsgMessage);
                    return;
                }
            }
            if (CurDeleTaskRun.ClearModuleTask())
            {
                ShowMsgBox.ShowDialog($"清除{CurDeleTaskRun.RunName}任务成功", MessageType.MsgWarning);
                ClearDateCsv(CurDeleTaskRun.RunName);

                return;
            }
        }

        /// <summary>
        /// 清除数据CSV
        /// </summary>
        private void ClearDateCsv(string section)
        {
            //DataBaseRecord.UserFormula curUser = new DataBaseRecord.UserFormula();
            //MachineCtrl.GetInstance().dbRecord.GetCurUser(ref curUser);

            User curUser = MachineCtrl.GetInstance().CurUser;
            //string sFilePath = "D:\\InterfaceOpetate\\ClearDate";
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\ClearDate", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "清除数据.CSV";
            string sColHead = "清除时间,账号,模组名称";
            string sLog = string.Format("{0},{1},{2}"
                , DateTime.Now
                , curUser.Name
                , section);
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }
        //private void ClearTask()
        //{
        //    CurDeleTaskRun.DeleteRunData();
        //}

        [RelayCommand]
        private void UpWater()
        {
            if (CavityState.WaitRes != CurOven.GetCavityState(CurfurnaceChamber))
            {
                string strInfo = string.Format("{0}\r\n{1}层腔体非等待水含量结果状态，不能上传", CurOven.RunName, CurfurnaceChamber + 1);
                ShowMsgBox.ShowDialog(strInfo, MessageType.MsgWarning);
                return;
            }

            //if (MachineCtrl.GetInstance().UpdataMES && !MachineCtrl.GetInstance().MesIsLogin())
            //{
            //    return;
            //}

         /*   if (!CurOven.SetMesUpLoadWaterParam(EnvironmentalDewPoint, ReworkRecord))
            {
                return;
            }*/

            string waterMsg = "";
            bool bRes = false;
            if (MachineCtrl.GetInstance().eWaterMode == WaterMode.阴阳极)
            {
                foreach (var water in WateUpTypes)
                {
                    if (!(water.BKCUWaterValue > 0) || !(water.BKAIWaterValue > 0))
                    {
                        WaterMode mode = water.BKCUWaterValue < 0 ? WaterMode.阳极 : WaterMode.阴极;
                        ShowMsgBox.ShowDialog($"托盘第{water.RowPos}层，【{mode}】水含量值不能小于等于0", MessageType.MsgWarning);
                        return;
                    }

                    if (water.BKAIWaterValue > CurOven.dWaterStandard[(int)WaterMode.阴极])
                    {
                        bRes = true;
                        waterMsg += $"托盘第{water.RowPos}层，【{WaterMode.阴极}】水含量值超标：{water.BKAIWaterValue}" + "\r\n";
                    }
                    else
                    {
                        waterMsg += $"托盘{water.RowPos}层, {WaterMode.阴极}水含量值：{water.BKAIWaterValue}" + "\r\n";
                    }

                    if (water.BKCUWaterValue > CurOven.dWaterStandard[(int)WaterMode.阳极])
                    {
                        bRes = true;
                        waterMsg += $"托盘第{water.RowPos}层，【{WaterMode.阳极}】水含量值超标：{water.BKCUWaterValue}" + "\r\n";
                    }
                    else
                    {
                        waterMsg += $"托盘{water.RowPos}层, {WaterMode.阳极}水含量值：{water.BKCUWaterValue}" + "\r\n";
                    }
                }
            }
            else
            {
                foreach (var water in WateUpTypes)
                {
                    /*if (!(water.WaterValue > 0))
                    {
                        ShowMsgBox.ShowDialog($"托盘第{water.RowPos}层，【{MachineCtrl.GetInstance().eWaterMode}】水含量值不能小于等于0", MessageType.MsgWarning);
                        return;
                    }
                    else
                    {
                        if (water.WaterValue > CurOven.dWaterStandard[(int)MachineCtrl.GetInstance().eWaterMode])
                        {
                            bRes = true;
                            waterMsg += $"托盘第{water.RowPos}层，【{MachineCtrl.GetInstance().eWaterMode}】水含量值超标：{water.WaterValue}" + "\r\n";
                        }
                        else
                        {
                            waterMsg += $"托盘{water.RowPos}层, {MachineCtrl.GetInstance().eWaterMode}水含量值：{water.WaterValue}" + "\r\n";
                        }
                    }*/

                    if (!(water.BKCUWaterValue > 0) || !(water.BKAIWaterValue > 0))
                    {
                        ShowMsgBox.ShowDialog($"托盘第{water.RowPos}层，【{MachineCtrl.GetInstance().eWaterMode}】水含量值不能小于等于0", MessageType.MsgWarning);
                        return;
                    }
                    else
                    {
                        if ((water.BKCUWaterValue + water.BKAIWaterValue) > CurOven.dWaterStandard[(int)MachineCtrl.GetInstance().eWaterMode])
                        {
                            bRes = true;
                            waterMsg += $"托盘第{water.RowPos}层，【{MachineCtrl.GetInstance().eWaterMode}】水含量值超标：{water.BKCUWaterValue},{water.BKAIWaterValue}" + "\r\n";
                        }
                        else
                        {
                            waterMsg += $"托盘{water.RowPos}层, {MachineCtrl.GetInstance().eWaterMode}水含量值：{water.BKCUWaterValue},{water.BKAIWaterValue}" + "\r\n";
                        }
                    }
                }
            }
            if (bRes)
            {
                waterMsg += $"是否上传至【{CurOven.RunName}】-【{CurfurnaceChamber + 1}层】腔体？";
            }
            if (ButtonResult.OK == ShowMsgBox.ShowDialog(waterMsg, MessageType.MsgQuestion).Result)
            {
                string waterInfo = string.Empty;
                if (MachineCtrl.GetInstance().eWaterMode == WaterMode.阴阳极)
                {
                    /*float[] waterModeValue = new float[2];
                    foreach (var water in WateUpTypes)
                    {
                        waterInfo = string.Empty;
                        waterModeValue[0] = water.BKCUWaterValue;
                        waterModeValue[1] = water.BKAIWaterValue;
                        CurOven.SetWaterContent(CurfurnaceChamber, waterModeValue);
                        waterInfo += $"{CurOven.RunName},{CurfurnaceChamber + 1},{water.RowPos},{MachineCtrl.GetInstance().eWaterMode},{water.BKCUWaterValue},{water.BKAIWaterValue}";
                        UploadWaterContentCsv(waterInfo);
                    }*/

                    float[] waterModeValue = new float[4];
                    foreach (var water in WateUpTypes)
                    {
                        waterInfo = string.Empty;
                        waterModeValue[0] = water.BKCUWaterValue;
                        waterModeValue[1] = water.BKAIWaterValue;
                        waterModeValue[2] = water.BKCUWaterTest;
                        waterModeValue[3] = water.BKAIWaterTest;
                        CurOven.SetWaterContent(CurfurnaceChamber, waterModeValue);
                        waterInfo += $"{CurOven.RunName},{CurfurnaceChamber + 1},{water.RowPos},{MachineCtrl.GetInstance().eWaterMode}," +
                            $"{water.BKCUWaterValue},{water.BKAIWaterValue},{water.BKCUWaterTest},{water.BKAIWaterTest}";
                        UploadWaterContentCsv(waterInfo);
                    }
                }
                else
                {
                    /*float[] waterModeValue = new float[1];
                    foreach (var water in WateUpTypes)
                    {
                        waterInfo = string.Empty;
                        waterModeValue[0] = water.WaterValue;
                        CurOven.SetWaterContent(CurfurnaceChamber, waterModeValue);
                        waterInfo += $"{CurfurnaceChamber + 1},{CurfurnaceChamber + 1},{water.RowPos},{MachineCtrl.GetInstance().eWaterMode},{water.WaterValue}";
                        UploadWaterContentCsv(waterInfo);
                    }*/
                    float[] waterModeValue = new float[4];
                    foreach (var water in WateUpTypes)
                    {
                        waterInfo = string.Empty;
                        waterModeValue[0] = water.BKCUWaterValue;
                        waterModeValue[1] = water.BKAIWaterValue;
                        waterModeValue[2] = water.BKCUWaterTest;
                        waterModeValue[3] = water.BKAIWaterTest;
                        CurOven.SetWaterContent(CurfurnaceChamber, waterModeValue);
                        waterInfo += $"{CurOven.RunName},{CurfurnaceChamber + 1},{water.RowPos},{MachineCtrl.GetInstance().eWaterMode},{water.BKCUWaterValue},{water.BKAIWaterValue},{water.BKCUWaterTest},{water.BKAIWaterTest}";
                        UploadWaterContentCsv(waterInfo);
                    }
                }
                WateUpTypes.Clear();
                EnvironmentalDewPoint = 0;
                ReworkRecord = 0;
                CurOven.SetWCUploadStatus(CurfurnaceChamber, WCState.WCStateInvalid);
                CurOven.ClearMaxMinValue(CurfurnaceChamber);
                CurOven.SaveRunData(SaveType.Variables);

            }

        }
        private void UploadWaterContentCsv(string waterInfo)
        {
            DataBaseRecord.UserFormula curUser = new DataBaseRecord.UserFormula();
            MachineCtrl.GetInstance().dbRecord.GetCurUser(ref curUser);
            string sFilePath = string.Format("{0}\\InterfaceOpetate\\UploadWaterContent", MachineCtrl.GetInstance().ProductionFilePath);
            string sFileName = DateTime.Now.ToString("yyyyMMdd") + "上传水含量.CSV";
            string sColHead = "上传时间,用户,干燥炉,炉腔,托盘,水含量模式,阳极水含量值,,阴极水含量值,阳极水含量测试，阴极水含量测试";
            string sLog = string.Format("{0},{1},{2}"
                , DateTime.Now
                , curUser.userName
                , waterInfo
                );
            MachineCtrl.GetInstance().WriteCSV(sFilePath, sFileName, sColHead, sLog);
        }
        partial void OnCurfurnaceChamberChanged(int oldValue, int newValue)
        {
            CurOven.CavityDataSource[oldValue].PropertyChanged -= WaterGridPropertyChange;
            CurOven.CavityDataSource[newValue].PropertyChanged += WaterGridPropertyChange;
            UpWaterGrid();
        }

        private void WaterGridPropertyChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CavityRowData.State))
                return;

            UpWaterGrid();

        }

        private void UpWaterGrid()
        {
            WateUpTypes = new();
            if (CavityState.WaitRes != CurOven.GetCavityState(CurfurnaceChamber)) return;

            CurOven.GetCavityPallet(CurfurnaceChamber).ForEach((plt, index) =>
            {
                if (plt.HasTypeBat(BatType.Fake))
                {
                    WateUpTypes.Add(new WaterUpType(index + 1));
                }
            });
        }

        #endregion

        #region MES
        /// <summary>
        /// MES调用
        /// </summary>
        /// <param></param>
        [RelayCommand]
        private void MesInterfaceInvoke()
        {
            if (CurMesInterface.Name == null || CurMesInterface.MesUrl == "")
            {
                return;
            }
            MachineCtrl.GetInstance().MESInvoke(CurMesInterface.MesInterface); 
        }
        #endregion

    }
    public class WaterUpType
    {
        private int nRowPos;
        /// <summary>
        /// 水含量值
        /// </summary>
        private float waterValue;
        /// <summary>
        /// 阴极/负极水含量值
        /// </summary>
        private float fBKAIWaterValue;
        /// <summary>
        /// 阳极/正极水含量值
        /// </summary>
        private float fBKCUWaterValue;
        /// <summary>
        /// 阴极/负极水含量测试
        /// </summary>
        private float fBKAIWaterTest;
        /// <summary>
        /// 阳极/正极水含量测试
        /// </summary>
        private float fBKCUWaterTest;
        public int RowPos
        {
            get
            {
                return nRowPos;
            }

            set
            {
                nRowPos = value;
            }
        }

        public float WaterValue
        {
            get
            {
                return waterValue;
            }

            set
            {

                waterValue = value;

            }
        }

        /// <summary>
        /// 阴极
        /// </summary>
        public float BKAIWaterValue
        {
            get
            {
                return fBKAIWaterValue;
            }

            set
            {

                fBKAIWaterValue = value;

            }
        }
        /// <summary>
        /// 阳极
        /// </summary>
        public float BKCUWaterValue
        {
            get
            {
                return fBKCUWaterValue;
            }

            set
            {
                fBKCUWaterValue = value;
            }
        }
        /// <summary>
        /// 阴极测试
        /// </summary>
        public float BKAIWaterTest
        {
            get
            {
                return fBKAIWaterTest;
            }

            set
            {

                fBKAIWaterTest = value;

            }
        }
        /// <summary>
        /// 阳极测试
        /// </summary>
        public float BKCUWaterTest
        {
            get
            {
                return fBKCUWaterTest;
            }

            set
            {
                fBKCUWaterTest = value;
            }
        }


        public WaterUpType(int rowPos)
        {
            nRowPos = rowPos;
        }
        public WaterUpType()
        {
        }
    }

    public partial class FormulaDb : ObservableObject
    {

        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [ObservableProperty]
        public int formulaid;

        /// <summary>
        /// 参数名
        /// </summary>
        [ObservableProperty]
        private string name;

        /// <summary>
        /// 参数值
        /// </summary>
        [ObservableProperty]
        private double parameterValue;

        partial void OnParameterValueChanging(double oldValue, double newValue)
        {
            MachineCtrl.GetInstance().parameterOldValue = oldValue;
        }

        /// <summary>
        /// 参数索引
        /// </summary>
        [ObservableProperty]
        private int paramIndex;
    }
}
