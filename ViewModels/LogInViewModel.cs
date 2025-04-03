using CommunityToolkit.Mvvm.Input;
using Machine;
using Prism.Mvvm;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFMachine.Frame.Userlib;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.ViewModels
{
    internal partial class LogInViewModel : BindableBase
    {
        private string name;
        private readonly Timer timer;
        private readonly MachineCtrl ctrl;

        public Action<object> Close { get; }

        private readonly ISqlSugarClient Db;

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value, () => 
            timer.Change(1000, 1000)); }
        }



        public LogInViewModel(Action<object> Close)
        {
            this.Db = UserHelp.Db;
            this.timer = new Timer(Logon);
            this.ctrl = MachineCtrl.MachineCtrlInstance;
            this.Close = Close;
        }

        public void Logon(object o)
        {
            var userList = Db.Queryable<User>().Includes(u => u.Level).ToList();
            userList.Add(UserHelp.RootUser);
            if (userList.FirstOrDefault(u => u.UserName == Name) is not User user)
            {
                Name = "";
                return;
            }
            Name = "";
            MachineCtrl.MachineCtrlInstance.CurUser = user;
            timer.Change(-1, -1);
            Close?.Invoke(true);

        }

    }
}
