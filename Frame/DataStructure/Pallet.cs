using Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using WPFMachine.Frame.BindingCorrelation;
using System.Windows.Media;
using System.ComponentModel;

namespace WPFMachine.Frame.DataStructure
{

    /// <summary>
    /// 托盘类型
    /// </summary>
    public enum PltType
    {
        Invalid = 0,                 // 无效
        OK,                          // OK托盘
        NG,                          // NG托盘
        Detect,                      // 待检测托盘
        WaitRes,                     // 等待结果（已取走假电池）
        WaitOffload,                 // 等待下料（检测已合格）
        WaitRebakeBat,               // 等待回炉电池（水含量超标，待放回假电池）
        WaitRebakingToOven,          // 等待托盘回炉（水含量超标，已放回假电池）
        FullOK,                      // 满电池
        /// <summary>
        /// 等待冷却完成
        /// </summary>
        WaitCooling,
        PltTypeEnd
    }

    /// <summary>
    /// 托盘阶段
    /// </summary>
    public enum PltStage
    {
        Invalid = 0,             // 无效阶段
        Onload = 1,         // 上料阶段
        Baking = 2,         // 烘烤阶段
        Offload = 3,        // 下料阶段
    }


    public class Pallet : ObservableObjectResourceDictionary
    {
        #region // 字段

        private string code;                // 托盘条码
        private bool isOnloadFake;          // 上假电池
        private bool pltPos;                // 托盘位置
        private PltType type;               // 托盘类型
        private PltStage stage;             // 托盘阶段
        private Battery[,] bat;             // 电池数组
        private int srcStation;             // 来源工位
        private int srcRow;                 // 来源工位行号
        private int srcCol;                 // 来源工位列号
        private int rowCount;               // 行数量
        private int colCount;               // 列数量
        private object lockPlt;             // 数据锁

        private string startTime;           // 开始时间
        private string endTime;             // 结束时间
        private PositionInOven posInOven;   // 料盘在炉区的具体位置

        #endregion

        #region 界面绑定

        private int borderThickness = 1;
        public int BorderThickness => borderThickness;

        public object ToolTipData => this;



        public IEnumerable<Battery> Bats =>
            Bat.OfType<Battery>().Take(MachineCtrl.MachineCtrlInstance.PltMaxRow * MachineCtrl.MachineCtrlInstance.PltMaxCol);

        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }
        private bool isThereABattery;
        /// <summary>
        /// 是否有电池
        /// </summary>
        public bool IsThereABattery
        {
            get { return isThereABattery; }
            set { SetProperty(ref isThereABattery, value); }
        }
        private bool whetherThereAreFakeBatteries;
        /// <summary>
        /// 是否有假电池
        /// </summary>
        public bool WhetherThereAreFakeBatteries
        {
            get { return whetherThereAreFakeBatteries; }
            set { SetProperty(ref whetherThereAreFakeBatteries, value); }
        }
        
        private bool whetherThereAreIsEmpty;
        /// <summary>
        /// 是否为空托盘
        /// </summary>
        public bool WhetherThereAreIsEmpty
        {
            get { return whetherThereAreIsEmpty; }
            set { SetProperty(ref whetherThereAreIsEmpty, value); }
        }
        private bool whetherThereAreFakePosBatteries;
        /// <summary>
        /// 是否取走假电池
        /// </summary>
        public bool WhetherThereAreFakePosBatteries
        {
            get { return whetherThereAreFakePosBatteries; }
            set { SetProperty(ref whetherThereAreFakePosBatteries, value); }
        }

        private bool whetherThereAreRBFakePosBatteries;
        /// <summary>
        /// 是否有回炉假电池
        /// </summary>
        public bool WhetherThereAreRBFakeBatteries
        {
            get { return whetherThereAreRBFakePosBatteries; }
            set { SetProperty(ref whetherThereAreRBFakePosBatteries, value); }
        }
        #endregion

        #region // 属性

        /// <summary>
        /// 托盘条码
        /// </summary>
        public string Code
        {
            get
            {
                return this.code;
            }

            set
            {
                this.code = value;
            }
        }

        /// <summary>
        /// 上假电池标志
        /// </summary>
        public bool IsOnloadFake
        {
            get
            {
                return this.isOnloadFake;
            }

            set
            {
                this.isOnloadFake = value;
            }
        }
        /// <summary>
        /// 托盘位置(是否在小车里面(true:在，false:不在))
        /// </summary>
        public bool PltPos
        {
            get
            {
                return this.pltPos;
            }

            set
            {
                SetProperty(ref pltPos, value);
            }
        }

        /// <summary>
        /// 托盘类型
        /// </summary>
        public PltType Type
        {
            get
            {
                return this.type;
            }

            set
            {
                SetProperty(ref this.type, value);
                UpStly();
            }
        }

        /// <summary>
        /// 托盘阶段
        /// </summary>
        public PltStage Stage
        {
            get
            {
                return this.stage;
            }

            set
            {
                this.stage = value;
            }
        }

        /// <summary>
        /// 行数量
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.rowCount;
            }

            set
            {
                this.rowCount = value;
            }
        }

        /// <summary>
        /// 列数量
        /// </summary>
        public int ColCount
        {
            get
            {
                return this.colCount;
            }

            set
            {
                this.colCount = value;
            }
        }

        /// <summary>
        /// 托盘电池锁
        /// </summary>
        public object LockPlt
        {
            get
            {
                return this.lockPlt;
            }
        }

        /// <summary>
        /// 托盘电池列表
        /// </summary>
        public Battery[,] Bat
        {
            get
            {
                return this.bat;
            }

            set
            {
                this.bat = value;
            }
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public string StartTime
        {
            get
            {
                return startTime;
            }

            set
            {
                this.startTime = value;
            }
        }
        /// <summary>
        /// 结束时间
        /// </summary>
        public string EndTime
        {
            get
            {
                return endTime;
            }

            set
            {
                this.endTime = value;
            }
        }

        /// <summary>
        /// 料盘在炉区的具体位置
        /// </summary>
        public PositionInOven PosInOven
        {
            get
            {
                return posInOven;
            }

            set
            {
                this.posInOven = value;
            }
        }

        public int SrcStation { get => srcStation; set => srcStation = value; }
        public int SrcRow { get => srcRow; set => srcRow = value; }
        public int SrcCol { get => srcCol; set => srcCol = value; }
        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数  
        /// </summary>
        public Pallet()
        {
            pltPos = false;
            lockPlt = new object();
            int row, col;
            row = col = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref row, ref col);
            RowCount = row;
            ColCount = col;
            Bat = new Battery[RowCount, ColCount];
            PosInOven = new PositionInOven();

            for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                {
                    var battery = new Battery();
                    Bat[nRowIdx, nColIdx] = battery;
                    battery.PropertyChanged += BatteryChanged;

                }
            }
            MachineCtrl.MachineCtrlInstance.PropertyChanged += MachineCtrlInstancePropertyChanged;
            Release();
        }
        /// <summary>
        /// Machine属性变更时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void MachineCtrlInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MachineCtrl.PltMaxCol) || e.PropertyName == nameof(MachineCtrl.PltMaxRow))
                OnPropertyChanged(nameof(Pallet.Bats));
        }

        public void BatteryChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Battery.Type))
                return;
            UpStly();
        }
        private void UpStly()
        {
            IsThereABattery = Bat.OfType<Battery>().Any(bat => bat.Type > BatType.Invalid);
            WhetherThereAreFakeBatteries = HasFake();
            WhetherThereAreFakePosBatteries = HasTypeBat(BatType.FakePos);
            WhetherThereAreRBFakeBatteries = HasTypeBat(BatType.RBFake);
            WhetherThereAreIsEmpty = IsEmpty();

        }
        public Pallet(int row, int col)
        {
            pltPos = false;
            lockPlt = new object();
            RowCount = row;
            ColCount = col;
            Bat = new Battery[RowCount, ColCount];
            PosInOven = new PositionInOven();

            for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
            {
                for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                {
                    Bat[nRowIdx, nColIdx] = new Battery();
                }
            }

            Release();
        }
        /// <summary>
        /// 检查托盘中某类型电池
        /// </summary>
        public bool PltHasTypeBat(BatType batType)
        {

            int nPltRow = 0;
            int nPltCol = 0;
            MachineCtrl.GetInstance().GetPltRowCol(ref nPltRow, ref nPltCol);

            lock (LockPlt)
            {
                for (int nRowIdx = 0; nRowIdx < nPltRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < nPltCol; nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].IsType(batType))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 复制外部数据到本类
        /// </summary>
        public bool CopyFrom(Pallet plt)
        {
            if (null != plt)
            {
                if (this == plt)
                {
                    return true;
                }

                lock (this.lockPlt)
                {
                    lock (plt.lockPlt)
                    {
                        PltPos = plt.PltPos;
                        Code = plt.Code;
                        IsOnloadFake = plt.IsOnloadFake;
                        Type = plt.Type;
                        Stage = plt.Stage;
                        RowCount = plt.RowCount;
                        ColCount = plt.ColCount;

                        SrcStation = plt.SrcStation;
                        SrcCol = plt.SrcCol;
                        SrcRow = plt.SrcRow;

                        StartTime = plt.StartTime;
                        EndTime = plt.EndTime;
                        PosInOven.CopyFrom(plt.PosInOven);

                        for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
                        {
                            for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                            {
                                Bat[nRowIdx, nColIdx].CopyFrom(plt.Bat[nRowIdx, nColIdx]);
                            }
                        }
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
            lock (this.lockPlt)
            {
                Code = "";
                IsOnloadFake = false;
                Type = PltType.Invalid;
                Stage = PltStage.Invalid;
                PltPos = true;
                StartTime = "";
                EndTime = "";
                PosInOven.Release();

                for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                    {
                        Bat[nRowIdx, nColIdx].Release();
                    }
                }
            }
        }

        /// <summary>
        /// 填充托盘电芯
        /// </summary>
        /// <summary>
        /// 填充托盘电芯
        /// </summary>
        public bool FillPltBat()
        {
            lock (lockPlt)
            {
                int m_nMaxJigRow = 0;
                int m_nMaxJigCol = 0;
                MachineCtrl.GetInstance().GetPltRowCol(ref m_nMaxJigRow, ref m_nMaxJigCol);
                for (int nRowIdx = 0; nRowIdx < m_nMaxJigRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < m_nMaxJigCol; nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].Type == BatType.Invalid)
                        {
                            Bat[nRowIdx, nColIdx].Type = BatType.BKFill;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 清除托盘电芯
        /// </summary>
        /// <returns></returns>
        public bool ClearFillPltBat()
        {
            lock (lockPlt)
            {
                int m_nMaxJigRow = 0;
                int m_nMaxJigCol = 0;
                MachineCtrl.GetInstance().GetPltRowCol(ref m_nMaxJigRow, ref m_nMaxJigCol);
                for (int nRowIdx = 0; nRowIdx < m_nMaxJigRow; nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < m_nMaxJigCol; nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].Type == BatType.BKFill)
                        {
                            Bat[nRowIdx, nColIdx].Release();
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 满托盘检查
        /// </summary>
        public bool IsFull()
        {
            lock (lockPlt)
            {
                foreach (Battery tmpBat in Bat)
                {
                    if (tmpBat.IsType(BatType.Invalid))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 空托盘检查
        /// </summary>
        public bool IsEmpty()
        {
            lock (lockPlt)
            {
                foreach (Battery tmpBat in Bat)
                {
                    if (tmpBat.Type > BatType.Invalid)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 假电池检查
        /// </summary>
        public bool HasFake()
        {
            lock (lockPlt)
            {
                foreach (Battery tmpBat in Bat)
                {
                    if (tmpBat.IsType(BatType.Fake))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查假电池并返回位置
        /// </summary>
        public bool HasFake(ref int nRow, ref int nCol)
        {
            lock (lockPlt)
            {
                for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].IsType(BatType.Fake))
                        {
                            nRow = nRowIdx;
                            nCol = nColIdx;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查某类型电池
        /// </summary>
        public bool HasTypeBat(BatType batType)
        {
            lock (lockPlt)
            {
                foreach (Battery tmpBat in Bat)
                {
                    if (tmpBat.IsType(batType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查某类型电池，并返回位置
        /// </summary>
        public bool HasTypeBat(BatType batType, ref int nRow, ref int nCol)
        {
            lock (lockPlt)
            {
                for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].IsType(batType))
                        {
                            nRow = nRowIdx;
                            nCol = nColIdx;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查某类型电池，并返回个数
        /// </summary>
        public int HasTypeBatCount(BatType batType)
        {
            int batNum = 0;
            lock (lockPlt)
            {
                for (int nRowIdx = 0; nRowIdx < Bat.GetLength(0); nRowIdx++)
                {
                    for (int nColIdx = 0; nColIdx < Bat.GetLength(1); nColIdx++)
                    {
                        if (Bat[nRowIdx, nColIdx].IsType(batType))
                        {
                            batNum++;
                        }
                    }
                }
            }
            return batNum;
        }

        /// <summary>
        /// 托盘类型检查
        /// </summary>
        public bool IsType(PltType pltType)
        {
            return (pltType == Type);
        }

        /// <summary>
        /// 托盘阶段检查
        /// </summary>
        public bool IsStage(PltStage pltStage)
        {
            if (PltStage.Invalid == pltStage)
            {
                return (PltStage.Invalid == Stage);
            }
            else
            {
                PltStage tmpStage = (Stage & pltStage);
                return (tmpStage == pltStage);
            }
        }

        ///// <summary>
        ///// 托盘是否在小车里面(true:在，false:不在)
        ///// </summary>
        //public bool IsInTrolley()
        //{
        //    return this.PltPos;
        //}

        /// <summary>
        /// 计算小车电池总数
        /// </summary>
        /// <param name="batType"></param>
        /// <returns></returns>
        public int PalletBatCount(BatType batType)
        {
            lock (lockPlt)
            {
                int batCount = 0;
                {
                    for (int row = 0; row < Bat.GetLength(0); row++)
                    {
                        for (int col = 0; col <Bat.GetLength(1); col++)
                        {
                            if (Bat[row, col].IsType(batType))
                            {
                                batCount++;
                            }
                        }
                    }
                }
                return batCount;
            }
        }

        #endregion
    }
}
