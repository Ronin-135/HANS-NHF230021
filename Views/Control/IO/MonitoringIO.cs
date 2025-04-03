using CommunityToolkit.Mvvm.Input;
using Machine;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;
using WPFMachine.Frame.BindingCorrelation;

namespace WPFMachine.Frame.IO
{
    partial class MonitoringIO : ObservableObjectResourceDictionary
    {

        private IReadIO readIO;
        /// <summary>
        /// 点位名
        /// </summary>
        public string Num => readIO.Num;

        /// <summary>
        /// 状态
        /// </summary>
        public string Name => readIO.Name;

        private bool status;
        public bool Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        public MonitoringIO(IReadIO readIO)
        {
            this.readIO = readIO;

        }
        #region 静态资源
        static List<MonitoringIO> IOList = new List<MonitoringIO>();

        static System.Threading.Timer Timer;

        public static void SetMonitoring(IEnumerable<MonitoringIO> monitoringIOs)
        {
            lock (IOList)
            {
                IOList.Clear();
                IOList.AddRange(monitoringIOs);
            }
        }


        static MonitoringIO()
        {
            Timer = new(UpStatus);
            Timer.Change(50, 50);
        }

        static void UpStatus(object s)
        {
            if (IOList.Count < 0) return;
            lock(IOList)
            {
                foreach (MonitoringIO io in IOList)
                {
                    io.Status = io.readIO.IsOn();
                }
            }
        }
        #endregion

        [RelayCommand]
        void OutAction()
        {
            if (readIO is not Output output) return;
            if (!output.MonitorEN) return;

            string[] outputType = output.Num.Split('Y');
            int outputNum = Convert.ToInt32(outputType[1]);
            int stationType = outputNum < 2000 ? 0 : 1;
            if (MachineCtrl.GetInstance().PlcIsAuto(stationType))
            {
                var res = output.IsOn() ? output.Off() : output.On();
            }
        }


    }
}
