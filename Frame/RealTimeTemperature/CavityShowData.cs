using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class CavityShowData : ObservableObject
    {
        [ConditionAttribute("唯一ID", false, false)]
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Guid { get; set; }

        [ConditionAttribute("烘烤ID", false ,false)]
        public long BakingGuid { get => bakingGuid; set => bakingGuid = value; }

        [ConditionAttribute("当前时间", false ,false)]
        public string CurTime { get => curTime; set => curTime = value; }

        [ConditionAttribute("真空值", false,true)]
        public int Vacuum { get => vacuum; set => vacuum = value; }


        private int guid;

        /// <summary>
        /// 烘烤id（烘烤开始与托盘区分）
        /// </summary>
        private long bakingGuid;

        /// <summary>
        /// 当前时间
        /// </summary>
        private string curTime;

        /// <summary>
        /// 真空值
        /// </summary>
        private int vacuum;

        public CavityShowData() { }
        public CavityShowData(long bakingGuid, DateTime curTime, int vacuum)
        {
            this.bakingGuid = bakingGuid;
            this.curTime = curTime.ToString("yyyy-MM-dd HH:mm:ss");
            this.vacuum = vacuum;
        }
    }
}
