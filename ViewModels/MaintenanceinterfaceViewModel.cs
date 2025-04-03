using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Machine;
using Microsoft.VisualBasic;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SystemControlLibrary;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.IO;
using WPFMachine.Frame.Server.Motor;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.ViewModels
{
    internal partial class MaintenanceinterfaceViewModel : ObservableObject
    {
        public IMotorService MotorService { get; init; }

        public ObservableCollection<object> Modules { get; } = new ObservableCollection<object>();
        [ObservableProperty]
        private object selectedValue;

        [ObservableProperty]
        private IEnumerable<object> inputs;

        [ObservableProperty]
        private IEnumerable<object> outPuts;

        [ObservableProperty]
        private IEnumerable<object> motors;


        private Motor CurMotor => MotorService.CurMotor;

        [ObservableProperty]
        private bool isReadGrid = true;

        private MotorFormula CurPos => MotorService.CurMotorpos;

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
        #endregion

        #region 电机速度
        [ObservableProperty]
        private bool speedSettingInterface;

        [ObservableProperty]
        private ObservableCollection<ObservableCollection<float>> velocityParameter = new(Enumerable.Range(0, 4).Select(i => new ObservableCollection<float>(Enumerable.Repeat(0f, 3))));

        [RelayCommand]
        private void OpenSpeed()
        {
            if (CurMotor == null) return;
            float speed, accTime, decTime;
            speed = accTime = decTime = 0f;

            for (int i = 0; i < VelocityParameter.Count; i++)
            {
                CurMotor.GetSpeed(i, ref speed, ref accTime, ref decTime);
                VelocityParameter[i][0] = speed;
                VelocityParameter[i][1] = accTime;
                VelocityParameter[i][2] = decTime;
            }
            SpeedSettingInterface = true;


        }
        [RelayCommand]
        private void SaveSpeed()
        {
            if (CurMotor == null) return;
            for (int i = 0; i < VelocityParameter.Count; i++)
            {
                CurMotor.SetSpeed(i, VelocityParameter[i][0], VelocityParameter[i][1], VelocityParameter[i][2]);
            }
            SpeedSettingInterface = false;

        }

        #endregion
        [RelayCommand]
        void NewMotorPos(MotorFormula motoPos)
        {
            var lastPos = CurMotorPoss?.OrderBy(pos => pos.posID).LastOrDefault(pos => pos != motoPos);
            if (lastPos == null)
            {
                motoPos.posID = 0;
                NewMethodOp(motoPos);

                return;
            }
            var index = 0;

            foreach (var pos in CurMotorPoss.OrderBy(pos => pos.posID))
            {
                if (pos == motoPos) continue;
                if (index != pos.posID)
                {
                    motoPos.posID = index;
                    NewMethodOp(motoPos);

                    return;
                }
                index++;
            }
            motoPos.posID = lastPos.posID + 1;

            NewMethodOp(motoPos);

        }

        private void NewMethodOp(MotorFormula motoPos)
        {
            motoPos.formulaID = Def.GetProductFormula();
            motoPos.motorID = CurMotor.MotorIdx;
            motoPos.PropertyChanged += (s, e) =>
            {
                Ctrl.dbRecord.ModifyMotorPos((MotorFormula)s);
            };
            Ctrl.dbRecord.AddMotorPos(motoPos);
        }

        [ObservableProperty]
        private ObservableCollection<DataBaseRecord.MotorFormula> curMotorPoss;


        public MachineCtrl Ctrl { get; }


        private Timer MotorTimer;

        #region 电机调试界面命令

        /// <summary>
        /// 搜索原点
        /// </summary>
        [RelayCommand]
        void SearchOrigin()
        {
            if (null != CurMotor)
            Ctrl.RunsCtrl.ManualDebugThread.IssueMotorHome(CurMotor);


        }

        [RelayCommand]
        void PosMove()
        {
            if (CurPos == null)
            {
                HelperLibrary.ShowMsgBox.Show("请选择点位", HelperLibrary.MessageType.MsgQuestion);
                return;
            }
            CurMotor.MoveAbs(CurPos._posValue);
        }

        [RelayCommand]
        void Enable()
        {
            if (CurMotor == null)
            {
                return;
            }
            bool servo = false;

            if ((int)MotorCode.MotorOK == CurMotor.GetIOStatus((int)MotorIO.MotorIO_SVON, ref servo))
            {
                CurMotor?.SetSvon(!servo);
            }
        }

        [RelayCommand]
        void Stop()
        {
            CurMotor?.Stop();
        }
        [RelayCommand]
        void Resetting()
        {
            CurMotor?.Reset();
        }

        #endregion

        #region IO按钮命令

        [RelayCommand]
        private void PosMoveAction()
            {
            if (CurMotor == null || CurPos == null) return;
            Ctrl.RunsCtrl.ManualDebugThread.IssueMotorMove(CurMotor, CurPos.posID, CurPos.posValue, MotorMoveType.MotorMoveLocation);

        }
        [RelayCommand]

        private void AddMove(object move)
        {
            if (CurMotor == null) return;

            Ctrl.RunsCtrl.ManualDebugThread.IssueMotorMove(CurMotor, -1, Convert.ToSingle(move), MotorMoveType.MotorMoveForward);

        }
        [RelayCommand]

        private void SubMove(object move)
        {
            if (CurMotor == null) return;

            //Ctrl.RunsCtrl.ManualDebugThread.IssueMotorMove(CurMotor, -1, Convert.ToSingle(move), MotorMoveType.MotorMoveBackward);//相对移动减不动作duanyh2024-1108
            Ctrl.RunsCtrl.ManualDebugThread.IssueMotorMove(CurMotor, -1, -Convert.ToSingle(move), MotorMoveType.MotorMoveForward);

        }


        #endregion

        partial void OnCurMotorPossChanged(ObservableCollection<MotorFormula> value)
        {
            CurMotorPoss.CollectionChanged += CurMotorPossCollectionChanged;

        }


        private void CurMotorPossCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (CurMotor == null) return;
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                e.OldItems.ForEach(item => Ctrl.dbRecord.DeleteMotorPos((MotorFormula)item));
            }
        }

        partial void OnSelectedValueChanged(object value)
        {
            var runId = 0;
            if (SelectedValue is RunProcess run)
            {
                runId = run.ModuleRunID + 1;
            }
            ModuleManager.GetInstance().SetCurModule(runId);


            var inputs = ModuleManager.Modules(ModuleManager.GetInstance().GetCurModule()).lstInputs.Select(inputIndex => new MonitoringIO(DeviceManager.Inputs(inputIndex))).ToArray();

            var outPuts = ModuleManager.Modules(ModuleManager.GetInstance().GetCurModule()).lstOutputs.Select(outIndex => new MonitoringIO(DeviceManager.Outputs(outIndex))).ToArray();

            OutPuts = outPuts;



            MonitoringIO.SetMonitoring(inputs.Concat(outPuts));

            Inputs = inputs;


            //this.Motors = ModuleManager.Modules(ModuleManager.GetInstance().GetCurModule()).lstMotors.Select(outIndex => DeviceManager.Motors(outIndex)).ToArray();

            MotorService.UpMotors();


        }

        //partial void OnCurMotorChanged(Motor value)
        //{
        //    IsReadGrid = value == null;
        //    this.MotorTimer ??= new Timer(UpdataMotor);

        //    if (CurMotor == null)
        //    {
        //        MotorTimer.Change(-1, -1);
        //        CurMotorPoss.Clear();
        //        return;
        //    };
        //    CurMotorPoss.CollectionChanged -= CurMotorPossCollectionChanged;
        //    CurMotorPoss.Clear();
        //    var tempList = new List<MotorFormula>();
        //    Ctrl.dbRecord.GetMotorPosList(Def.GetProductFormula(), CurMotor.MotorIdx, tempList);
        //    tempList.OrderBy(pos => pos.posID).ForEach(pos => CurMotorPoss.Add(pos));
        //    CurMotorPoss.ForEach(motor => motor.PropertyChanged += (s, e) => Ctrl.dbRecord.ModifyMotorPos((MotorFormula)s));
        //    CurMotorPoss.CollectionChanged += CurMotorPossCollectionChanged;
        //    MotorTimer.Change(20, 20);

        //}

        private void UpdataMotor(object state)
        {
            if (CurMotor == null) return;
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


        }

        public MaintenanceinterfaceViewModel(IEnumerable<RunProcess> runProcesses, MachineCtrl ctrl, IMotorService motorService)
        {
            CurMotorPoss = new ObservableCollection<DataBaseRecord.MotorFormula>();
            MotorService = motorService;
            this.Ctrl = ctrl;
            Modules.Add(ctrl);
            Modules.AddRange(runProcesses);


        }



        [RelayCommand]
        public void AbsMove(object pos)
        {
         
            if (pos == null || !float.TryParse(pos.ToString(),out var posfloat)) return ;
            CurMotor?.MoveAbs(posfloat);
        }



    }
}
