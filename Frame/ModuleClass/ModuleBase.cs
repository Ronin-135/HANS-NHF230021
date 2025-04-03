using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFMachine.Frame.ExtensionMethod;

namespace WPFMachine.Frame.ModuleClass
{
    internal abstract class ModuleBase : DependencyObject, INotifyPropertyChangedTrigger
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string msg;

        public string Msg
        {
            get { return msg; }
            set { this.SetProperty(ref msg, value); }
        }

        private string status = default(ModuleStatus).ToString();

        public ModuleStatus Status
        {
            get
            {
                Enum.TryParse(typeof(ModuleStatus), status, out var res);
                return res == null ? default : (ModuleStatus)res;
            }
            set
            {
                this.SetPropertyResources(ref status, value.ToString(), () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Statusinfo))));

            }
        }

        public string Statusinfo => status;

        private bool IsUpData;

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }


        public event EventHandler BeforeRun;
        internal void StartBeforeRun() => BeforeRun?.Invoke(this, EventArgs.Empty);

        public event EventHandler Alarm;
        internal void StartAlarm() => Alarm?.Invoke(this, EventArgs.Empty);

        public event EventHandler BeforeStop;
        internal void StartBeforStop() => BeforeStop?.Invoke(this, EventArgs.Empty);

        public event EventHandler BeforeOff;
        internal void StartBeforeOff() => BeforeStop.Invoke(this, EventArgs.Empty);



        /// <summary>
        /// 设置重资源字典里面读取出来的属性
        /// </summary>
        /// <param name="storage">旧值</param>
        /// <param name="value">新值</param>
        /// <param name="callBack">发送事件之前回调</param>
        /// <param name="propName">要跟新的属性</param>






        public ModuleBase()
        {
            Status = ModuleStatus.停止;
        }


        /// <summary>
        /// 运行
        /// </summary>
        protected abstract void Run();



        /// <summary>
        /// 停止
        /// </summary>
        protected abstract void Stop();

        public void StartPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    enum ModuleStatus
    {
        停止,
        启动,

    }
}
