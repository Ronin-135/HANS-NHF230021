using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    internal partial class ProcessParam : ObservableObject
    {


        private float unSetTempValue;
        /// <summary>
        /// 设定温度
        /// </summary>
        [OvenParameter(0, "设定温度")]
        public float UnSetTempValue
        {
            get { return unSetTempValue; }
            set { SetProperty(ref unSetTempValue, value); }
        }

        /// <summary>
        /// 温度上限
        /// </summary>
        private float unTempUpperLimit;
        [OvenParameter(1, "温度上限")]

        public float UnTempUpperLimit
        {
            get { return unTempUpperLimit; }
            set { SetProperty(ref unTempUpperLimit, value); }
        }

        private float unTempLowerLimit;
        /// <summary>
        /// 温度下限
        /// </summary>
        [OvenParameter(2, "温度下限")]
        public float UnTempLowerLimit
        {
            get { return unTempLowerLimit; }
            set { SetProperty(ref unTempLowerLimit, value); }
        }

        private uint unPreHeatTime;
        /// <summary>
        /// 预热时间
        /// </summary>

        [OvenParameter(3, "预热时间")]
        public uint UnPreHeatTime
        {
            get { return unPreHeatTime; }
            set { SetProperty(ref unPreHeatTime, value); }
        }

        private uint unVacHeatTime;
        /// <summary>
        /// 真空时间
        /// </summary>
        [OvenParameter(4, "真空时间")]
        public uint UnVacHeatTime
        {
            get { return unVacHeatTime; }
            set { SetProperty(ref unVacHeatTime, value); }
        }

        private uint unPressureUpperLimit;
        /// <summary>
        /// 真空压力上限
        /// </summary>
        [OvenParameter(5, "真空压力上限")]
        public uint UnPressureUpperLimit
        {
            get { return unPressureUpperLimit; }
            set { SetProperty(ref unPressureUpperLimit, value); }
        }

        private uint unPressureLowerLimit;
        /// <summary>
        /// 真空压力下限
        /// </summary>
        [OvenParameter(6, "真空压力下限")]
        public uint UnPressureLowerLimit
        {
            get { return unPressureLowerLimit; }
            set { SetProperty(ref unPressureLowerLimit, value); }
        }

        private uint unOpenDoorBlowTime;
        /// <summary>
        /// 开门破真空时长
        /// </summary>
        [OvenParameter(7, "开门破真空时长")]
        public uint UnOpenDoorBlowTime
        {
            get { return unOpenDoorBlowTime; }
            set { SetProperty(ref unOpenDoorBlowTime, value); }
        }

        private uint unAStateVacTime;
        /// <summary>
        /// A状态真空时间
        /// </summary>
        [OvenParameter(8, "A状态真空时间")]
        public uint UnAStateVacTime
        {
            get { return unAStateVacTime; }
            set { SetProperty(ref unAStateVacTime, value); }
        }

        private uint unAStateVacPressure;
        /// <summary>
        /// A状态真空压力
        /// </summary>
        [OvenParameter(9, "A状态真空压力")]
        public uint UnAStateVacPressure
        {
            get { return unAStateVacPressure; }
            set { SetProperty(ref unAStateVacPressure, value); }
        }

        private uint unBStateBlowAirTime;
        /// <summary>
        /// B状态充干燥气时间
        /// </summary>
        [OvenParameter(10, "B状态充干燥气时间")]
        public uint UnBStateBlowAirTime
        {
            get { return unBStateBlowAirTime; }
            set { SetProperty(ref unBStateBlowAirTime, value); }
        }

        private uint unBStateBlowAirPressure;
        /// <summary>
        /// B状态充干燥气压力
        /// </summary>
        [OvenParameter(11, "B状态充干燥气压力")]
        public uint UnBStateBlowAirPressure
        {
            get { return unBStateBlowAirPressure; }
            set { SetProperty(ref unBStateBlowAirPressure, value); }
        }

        private uint unBStateBlowAirKeepTime;
        /// <summary>
        /// B状态充干燥气保持时间
        /// </summary>
        [OvenParameter(12, "B状态充干燥气保持时间")]
        public uint UnBStateBlowAirKeepTime
        {
            get { return unBStateBlowAirKeepTime; }
            set { SetProperty(ref unBStateBlowAirKeepTime, value); }
        }


        private uint unBStateVacPressure;
        /// <summary>
        /// B状态真空压力
        /// </summary>
        [OvenParameter(13, "B状态真空压力")]
        public uint UnBStateVacPressure
        {
            get { return unBStateVacPressure; }
            set { SetProperty(ref unBStateVacPressure, value); }
        }

        private uint unBStateVacTime;
        /// <summary>
        /// B状态抽真空时间
        /// </summary>
        [OvenParameter(14, "B状态抽真空时间")]
        public uint UnBStateVacTime
        {
            get { return unBStateVacTime; }
            set { SetProperty(ref unBStateVacTime, value); }
        }

        private uint unBreathTimeInterval;
        /// <summary>
        /// 真空呼吸时间间隔
        /// </summary>
        [OvenParameter(15, "真空呼吸时间间隔")]
        public uint UnBreathTimeInterval
        {
            get { return unBreathTimeInterval; }
            set { SetProperty(ref unBreathTimeInterval, value); }
        }

        private uint unPreHeatBreathTimeInterval;
        /// <summary>
        /// 预热呼吸时间间隔
        /// </summary>
        [OvenParameter(16, "预热呼吸时间间隔")]
        public uint UnPreHeatBreathTimeInterval
        {
            get { return unPreHeatBreathTimeInterval; }
            set { SetProperty(ref unPreHeatBreathTimeInterval, value); }
        }

        private uint unPreHeatBreathPreTimes;
        /// <summary>
        /// 预热呼吸保持时间
        /// </summary>
        [OvenParameter(17, "预热呼吸保持时间")]
        public uint UnPreHeatBreathPreTimes
        {
            get { return unPreHeatBreathPreTimes; }
            set { SetProperty(ref unPreHeatBreathPreTimes, value); }
        }

        private uint unPreHeatBreathPre;
        /// <summary>
        /// 预热呼吸真空压力
        /// </summary>
        [OvenParameter(18, "预热呼吸真空压力")]
        public uint UnPreHeatBreathPre
        {
            get { return unPreHeatBreathPre; }
            set { SetProperty(ref unPreHeatBreathPre, value); }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class OvenParameterAttribute : Attribute
    {
        public int IndexPrarmeter { get; }
        public string Name { get; }
        public OvenParameterAttribute(int index, string propName)
        {
            IndexPrarmeter = index;
            Name = propName;
        }
    }

    internal static class RemarkExtension
    {
        //public static (FieldInfo info, OvenParameterAttribute att)[] propListInfo = typeof(RunProDryingOven).
        //    GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).
        //    Where(fileInfo => fileInfo.GetCustomAttributes(typeof(OvenParameterAttribute), false).Length > 0).
        //    Select(info => (info, info.GetCustomAttributes(typeof(OvenParameterAttribute), false)[0] as OvenParameterAttribute)).
        //    OrderBy(a => a.Item2.IndexPrarmeter).
        //    ToArray();
        public static (PropertyInfo info, OvenParameterAttribute att)[] GetPropListInfo<T>(this T t)
        {
            Type type = t.GetType();
            var fields = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fileInfos = fields.Where(fileInfo => fileInfo.GetCustomAttributes(typeof(OvenParameterAttribute), false).Length > 0);
            var propListInfo = fileInfos.Select(info => (info, info.GetCustomAttributes(typeof(OvenParameterAttribute), false)[0] as OvenParameterAttribute)).OrderBy(a => a.Item2.IndexPrarmeter);
            return propListInfo.ToArray();
        }
        public static uint GetRunTime<T>(this T t)
        {
            Type type = t.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            IEnumerable<FieldInfo> fileInfos = fields.Where(fileInfo => fileInfo.GetCustomAttributes(typeof(OvenParameterAttribute), false).Length > 0);
            IEnumerable<(FieldInfo, OvenParameterAttribute)> propListInfo = fileInfos.Select(info => (info, info.GetCustomAttributes(typeof(OvenParameterAttribute), false)[0] as OvenParameterAttribute))
                .Where(a => a.Item2.IndexPrarmeter == 3 || a.Item2.IndexPrarmeter == 4);
            return (uint)propListInfo.Sum(p => (uint)p.Item1.GetValue(t));
            //return (uint)fields[3].GetValue(t) + (uint)fields[4].GetValue(t);
        }
    }
}



