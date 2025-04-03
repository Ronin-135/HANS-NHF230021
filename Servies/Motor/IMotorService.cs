using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.Frame.Server.Motor
{
    /// <summary>
    /// 提供和调试界面交互的电机模组
    /// </summary>
    internal interface IMotorService
    {
        /// <summary>
        /// 电机组
        /// </summary>
        public ICollection<SystemControlLibrary.Motor> Motors { get; }

        /// <summary>
        /// 电机点位
        /// </summary>
        public ICollection<MotorFormula> Motorposs { get; }

        /// <summary>
        /// 当前选择电机
        /// </summary>
        public SystemControlLibrary.Motor CurMotor { get; set; }

        /// <summary>
        /// 当前选择点位
        /// </summary>
        public MotorFormula CurMotorpos { get; set; }

        /// <summary>
        /// 是否可以操作电机调试界面
        /// </summary>
        public bool IsShowMotorDebugging { get; }

        /// <summary>
        /// 是否可以操作点位
        /// </summary>
        public bool WhetherAPointCanBeManipulated { get; }

        #region 电机IO
        /// <summary>
        /// 电机准备
        /// </summary>
        public bool MotorPreparation { get; }
        /// <summary>
        /// 电机报警
        /// </summary>
        public bool MotorAlarm { get; }
        /// <summary>
        /// 电机正限位
        /// </summary>
        public bool MotorPositiveLimit { get; }
        /// <summary>
        /// 电机负限位
        /// </summary>
        public bool MotorNegativeLimit { get; }
        /// <summary>
        /// 电机原点
        /// </summary>
        public bool MotorOrigin { get; }
        /// <summary>
        /// 电机使能
        /// </summary>
        public bool MotorEnable { get; }

        /// <summary>
        /// 电机运行状态
        /// </summary>
        public bool MotorOperatingState { get; }


        /// <summary>
        /// 电机当前转矩
        /// </summary>
        public double CurrentTorqueOfTheMotor { get; }
        /// <summary>
        /// 电机当前速度
        /// </summary>
        public double CurrentMotorSpeed { get; }
        /// <summary>
        /// 电机当前位置
        /// </summary>
        public double CurrentPositionOfMotor { get; }

        #endregion

        /// <summary>
        /// 跟新电机
        /// </summary>
        public void UpMotors();

        /// <summary>
        /// 移除
        /// </summary>
        public IRelayCommand<object> ReMovePosCommand { get; }

        /// <summary>
        /// 添加
        /// </summary>
        public IRelayCommand AddPosCommand { get; }
    }
}
