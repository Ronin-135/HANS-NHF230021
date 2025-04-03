using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class OvenPositionInfo : ObservableObject
    {
        [ConditionAttribute("唯一ID", false, false)]
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Guid { get; set; }

        [ConditionAttribute("烘烤id", false, false)]
        public long BakingGuid1 { get => bakingGuid; set => bakingGuid = value; }

        [ConditionAttribute("开始时间", true, false)]
        public DateTime StatrTime1 { get => statrTime; set => statrTime = value; }

        [ConditionAttribute("运行时间", false, false)]
        [SugarColumn(IsNullable = true)]
        public int RunTimel1 { get => runTimel; set => runTimel = value; }

        [ConditionAttribute("干燥炉资源号", false, false)]
        [SugarColumn(IsNullable = true)]
        public string OvenResourceNumber1 { get => ovenResourceNumber; set => ovenResourceNumber = value; }

        [ConditionAttribute("干燥炉id", false, false)]
        public int OvenID1 { get => ovenID; set => ovenID = value; }

        [ConditionAttribute("炉腔", false, false)]
        public int Cavity1 { get => cavity; set => cavity = value; }

        [ConditionAttribute("托盘号", true, false)]
        [SugarColumn(IsNullable = true)]
        public string PalltNumber1 { get => palltNumber; set => palltNumber = value; }

        [ConditionAttribute("托盘层", true, false)]
        public int PalltRow { get => palltRow; set => palltRow = value; }


        /// <summary>
        /// 烘烤id（烘烤id+托盘）
        /// </summary>
        private long bakingGuid;

        /// <summary>
        /// 开始时间
        /// </summary>
        private DateTime statrTime;

        /// <summary>
        /// 运行时间
        /// </summary>
        private int runTimel;

        /// <summary>
        /// 干燥炉资源号
        /// </summary>
        private String ovenResourceNumber;

        /// <summary>
        /// 干燥炉id
        /// </summary>
        private int ovenID;

        /// <summary>
        /// 炉腔
        /// </summary>
        private int cavity;
        
        /// <summary>
        /// 托盘号
        /// </summary>
        private string palltNumber;
        
        /// <summary>
        /// 托盘层
        /// </summary>
        private int palltRow;


        public OvenPositionInfo() { }

        public OvenPositionInfo(DateTime statrTime1, int runTimel1, string ovenResourceNumber1, int ovenID1, int cavity1, string palltNumber1, int palltRow)
        {
            StatrTime1 = statrTime1;
            RunTimel1 = runTimel1;
            OvenResourceNumber1 = ovenResourceNumber1;
            OvenID1 = ovenID1;
            Cavity1 = cavity1;
            PalltNumber1 = palltNumber1;
            PalltRow = palltRow;
        }
    }
}
