using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class PalltShowData : ObservableObject
    {
        [ConditionAttribute("唯一ID", false, false)]
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public uint Guid { get; set; }

        [ConditionAttribute("烘烤ID", false, false)]
        public long BakingGuid { get => bakingGuid; set => bakingGuid = value; }

        [ConditionAttribute("当前时间", false, false)]
        public string CurTime { get => curTime; set => curTime = value; }

        [ConditionAttribute("运行时间", false, true)]
        public double RunTimel { get => runTimel; set => runTimel = value; }

        [ConditionAttribute("控温1", false, true)]
        public double TemperatureControl1 { get => temperatureControl1; set => temperatureControl1 = value; }

        [ConditionAttribute("巡检1", false, true)]
        public double Inspection1 { get => inspection1; set => inspection1 = value; }

        [ConditionAttribute("巡检2", false, true)]
        public double Inspection2 { get => inspection2; set => inspection2 = value; }
        
        [ConditionAttribute("巡检3", false, true)]
        public double Inspection3 { get => inspection3; set => inspection3 = value; }


        /// <summary>
        /// 烘烤id（烘烤开始与托盘区分）
        /// </summary>
        private long bakingGuid;

        /// <summary>
        /// 当前时间
        /// </summary>
        private string curTime;

        /// <summary>
        /// 运行时间
        /// </summary>
        
        private double runTimel;

        
        /// <summary>
        /// 温控1
        /// </summary>
        private double temperatureControl1;

        
        /// <summary>
        /// 巡检1
        /// </summary>
        private double inspection1;

        /// <summary>
        /// 巡检2
        /// </summary>
        private double inspection2;

        /// <summary>
        /// 温控3
        /// </summary>
        private double inspection3;

        public static  Dictionary<String,PropertyInfo> GetShowProperty()
        {
            Dictionary<String, PropertyInfo> propertyInfos = new Dictionary<String, PropertyInfo>(); 
            Type type = typeof(PalltShowData);
            foreach (PropertyInfo p in type.GetProperties())
            {
                foreach (object attribute in p.GetCustomAttributes(true)) //2.通过映射，找到成员属性上关联的特性类实例，
                {
                    if (attribute is ConditionAttribute)//3.如果找到了限定长度的特性类对象，就用这个特性类对象验证该成员
                    {
                        ConditionAttribute attr = (ConditionAttribute)attribute;
                        if (attr.IsConditionP || attr.IsShow)
                            propertyInfos.Add(attr.ChName, p);
                    }
                }
            }
            return propertyInfos;
        }
        
    }
}
