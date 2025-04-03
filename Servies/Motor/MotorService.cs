using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastDeepCloner;
using Machine;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shell;
using SystemControlLibrary;
using WPFMachine.Frame.Server.Motor;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.Frame.Server
{
    internal partial class MotorService : ObservableObject, IMotorService
    {
        public MachineCtrl Ctrl { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowMotorDebugging))]
        public ICollection<SystemControlLibrary.Motor> motors;

        public ICollection<MotorFormula> Motorposs => motorposs;

        private BindingList<MotorFormula> motorposs;

        public bool IsShowMotorDebugging => Motors?.Any() == true;

        public bool WhetherAPointCanBeManipulated => CurMotor != null;

        /// <summary>
        /// 当前选择电机
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WhetherAPointCanBeManipulated))]
        [NotifyCanExecuteChangedFor(nameof(ReMovePosCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddPosCommand))]
        public SystemControlLibrary.Motor curMotor;

        [ObservableProperty]
        public MotorFormula curMotorpos;

        

        public MotorService(MachineCtrl ctrl)
        {
            Ctrl = ctrl;
            var bindingList = new BindingList<MotorFormula>();
            motorposs = bindingList;
            bindingList.ListChanged += Alter;
            Synchronization();


        }

        #region IO更新定时器
        private System.Threading.Timer motorIoTime;

        private System.Threading.Timer MotorIoTimer => motorIoTime ??= new System.Threading.Timer(UpdataMotor);

        private int timerIoRun;

        /// <summary>
        /// 关闭定时器服务
        /// </summary>
        private void StopIoTime()
        {
            // 关闭定时器
            MotorIoTimer.Change(-1, -1);
        }

        /// <summary>
        /// 开启定时器服务
        /// </summary>
        private void StartIoTime()
        {
            MotorIoTimer.Change(50, 50);

        }
        #endregion

        /// <summary>
        /// 选择电机更新时
        /// </summary>
        /// <param name="value"></param>
        partial void OnCurMotorChanged(SystemControlLibrary.Motor value)
        {
            Motorposs.Clear();
            if (CurMotor == null) return;
            motorposs.ListChanged -= Alter;
            var collection = new Collection<MotorFormula>();
            Ctrl.dbRecord.GetMotorPosList(Def.GetProductFormula(), CurMotor.MotorIdx, collection);
            motorposs.AddRange(collection.OrderBy(pos => pos.posID));
            collection.OrderBy(pos => pos.posID);
            StartIoTime();
            Synchronization();
            motorposs.ListChanged += Alter;



        }
        /// <summary>
        /// 电机要切换前
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <exception cref="NotImplementedException"></exception>
        partial void OnCurMotorChanging(SystemControlLibrary.Motor oldValue, SystemControlLibrary.Motor newValue)
        {
            // 停止定时器服务
            StopIoTime();
        }



        #region 电机IO
        /// <summary>
        /// 电机准备
        /// </summary>
        [ObservableProperty]
        private bool motorPreparation;

        /// <summary>
        /// 电机报警
        /// </summary>
        [ObservableProperty]
        private bool motorAlarm;



        /// <summary>
        /// 电机正限位
        /// </summary>
        [ObservableProperty]
        private bool motorPositiveLimit;

        /// <summary>
        /// 电机负限位
        /// </summary>
        [ObservableProperty]
        private bool motorNegativeLimit;

        /// <summary>
        /// 原点
        /// </summary>
        [ObservableProperty]
        private bool motorOrigin;

        /// <summary>
        /// 使能
        /// </summary>
        [ObservableProperty]
        private bool motorEnable;


        /// <summary>
        /// 电机当前位置
        /// </summary>
        [ObservableProperty]

        private double currentPositionOfMotor;

        /// <summary>
        /// 电机当前速度
        /// </summary>
        [ObservableProperty]
        private double currentMotorSpeed;

        /// <summary>
        /// 电机当前转矩
        /// </summary>
        [ObservableProperty]
        private double currentTorqueOfTheMotor;

        /// <summary>
        /// 电机运行状态
        /// </summary>
        [ObservableProperty]
        private bool motorOperatingState;
        #endregion

        [RelayCommand(CanExecute = nameof(IsAddRemove))]
        public void ReMovePos(object obj)
        {
            if (obj is not MotorFormula motorformula)
            {
                return;
            }
            Motorposs.Remove(motorformula);
            Ctrl.dbRecord.DeleteMotorPos(motorformula);
            Synchronization();

        }

        private bool IsAddRemove()
        {
            return WhetherAPointCanBeManipulated;
        }

        [RelayCommand(CanExecute = nameof(IsAddRemove))]
        public void AddPos()
        {
            var pos = new MotorFormula() { posID = GetID(), motorID = CurMotor.MotorIdx, formulaID = Def.GetProductFormula() };
            Motorposs.Add(pos);
            Ctrl.dbRecord.AddMotorPos(pos);
            CurMotor.AddLocation(pos.posID, pos.posName, pos.posValue);
        }


        private int GetID()
        {
            if (Motorposs.Count == 0) return 0;

            var id = 1;
            var max = Motorposs.Select(p => p.posID).Max();
            var poss = Motorposs.OrderBy(p => p.posID).ToArray();

            if (poss[0].posID != 0) return 0;
            for (int i = 0; i < max - 1; i++)
            {
                if (poss[i].posID < id && poss[i + 1].posID > id)
                    return id;
                id++;
            }
            return max + 1;
        }

        public void UpMotors()
        {
            Motors = ModuleManager.Modules(ModuleManager.GetInstance().GetCurModule()).lstMotors.Select(outIndex => DeviceManager.Motors(outIndex)).ToArray();

        }

        private void UpdataMotor(object state)
        {
            var CurMotor = this.CurMotor;
            if (CurMotor == null) return;
            // 使用原子操作来保证同时只能有一个线程在执行IO更新
            if (Interlocked.CompareExchange(ref timerIoRun, 1, 0) == 1)
            {
                return;
            }
            var curPos = 0f;
            CurMotor.GetCurPos(ref curPos);
            CurrentPositionOfMotor = curPos;
            var mstate = false;
            CurMotor.GetMotorStatus(ref mstate);
            CurMotor.GetCurSpeed(ref curPos);
            CurrentMotorSpeed = curPos;
            CurMotor.GetCurTorque(ref curPos);
            CurrentTorqueOfTheMotor = curPos;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_RDY, ref mstate);
            MotorPreparation = mstate;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_ALM, ref mstate);
            MotorAlarm = mstate;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_NEL, ref mstate);
            MotorNegativeLimit = mstate;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_PEL, ref mstate);
            MotorPositiveLimit = mstate;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_ORG, ref mstate);
            MotorOrigin = mstate;
            CurMotor.GetIOStatus((int)MotorIO.MotorIO_SVON, ref mstate);
            MotorEnable = mstate;
            Interlocked.CompareExchange(ref timerIoRun, 0, 1);

        }

        private void Alter(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                var pos = motorposs[e.NewIndex];
                Ctrl.dbRecord.ModifyMotorPos(pos);
                CurMotor.SetLocation(pos.posID, pos.posName, pos.posValue);
            }
        }

        private void Synchronization()
        {
            if (CurMotor == null) return;
            CurMotor.DeleteAllLoc();
            Motorposs.ForEach(pos => CurMotor.AddLocation(pos.posID, pos.posName, pos.posValue));
        }
    }
}
