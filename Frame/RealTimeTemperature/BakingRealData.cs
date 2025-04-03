using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class BakingRealData : ObservableObject
    {
        /// <summary>
        /// 干燥炉位置信息
        /// </summary>
        [ObservableProperty]
        private OvenPositionInfo ovenPositionInfo;

        /// <summary>
        /// 托盘级数据
        /// </summary>
        [ObservableProperty]
        private List<PalltShowData> palltShowDatas;

        /// <summary>
        /// 炉腔级数据
        /// </summary>
        [ObservableProperty]
        private CavityShowData cavityShowDatas;

        /// <summary>
        /// 干燥炉显示条件
        /// </summary>
        [ObservableProperty]
        public static List<Condition> ovenPositionConditions;

        /// <summary>
        /// 炉腔显示条件
        /// </summary>
        [ObservableProperty]
        public static List<Condition> cavityShowDataConditions;

        /// <summary>
        /// 托盘显示条件
        /// </summary>
        [ObservableProperty]
        public static List<Condition> palltShowDataConditions;

        public BakingRealData(OvenPositionInfo ovenPositionInfo, List<PalltShowData> palltShowDatas, CavityShowData cavityShowDatas)
        {
            this.ovenPositionInfo = ovenPositionInfo;
            this.palltShowDatas = palltShowDatas;
            this.cavityShowDatas = cavityShowDatas;
            GetConditions();
        }
        
        public BakingRealData()
        {
            GetConditions();
        }

        public void GetConditions()
        {
            if (CavityShowDataConditions == null)
            {
                CavityShowDataConditions = new List<Condition>();
                createConditions(typeof(CavityShowData), CavityShowDataConditions);
            }
            if (PalltShowDataConditions == null)
            {
                PalltShowDataConditions = new List<Condition>();
                createConditions(typeof(PalltShowData), PalltShowDataConditions);
            }
            if (OvenPositionConditions == null)
            {
                OvenPositionConditions = new List<Condition>();
                createConditions(typeof(OvenPositionInfo), OvenPositionConditions);
            }
        }

        private void createConditions(Type type, List<Condition> Conditions)
        {
            foreach (PropertyInfo p in type.GetProperties())
            {
                foreach (object attribute in p.GetCustomAttributes(true)) //2.通过映射，找到成员属性上关联的特性类实例，
                {
                    if (attribute is ConditionAttribute)//3.如果找到了限定长度的特性类对象，就用这个特性类对象验证该成员
                    {
                        ConditionAttribute attr = (ConditionAttribute)attribute;
                        if (attr.IsConditionP || attr.IsShow)
                            Conditions.Add(new Condition(attr));
                    }
                }
            }
        }
    }
}
