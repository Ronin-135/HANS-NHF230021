using EnumsNET;
using Machine;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using SystemControlLibrary;

namespace WPFMachine.Frame.Userlib
{
    static class UserHelp
    {

        #region 超级管理员
        public static User RootUser = new() { UserName="admin", Name = "HANS", Password = "admin", Level = new() { Name = "超级管理" } };


        #endregion


        public static Dictionary<string, Authority> authorityDict;

        public static Dictionary<string, Authority> AuthorityDict
        {
            get
            {
                return authorityDict ??= _AuthorityList.ToDictionary(authy => authy.Name); ;
            }
        }

        private static List<Authority> _AuthorityList
        {
            get
            {
                var res = Db.Queryable<Authority>().Includes(x => x.UserLevels).ToList();
                var levesDb = Db.Queryable<UserLevel>().Includes(u => u.Authoriys).Includes(u => u.Users).ToArray().ToDictionary(u => u.Id, u => u);

                foreach (var autohority in res)
                {
                    foreach (var leve in autohority.UserLevels)
                    {
                        leve.Users = levesDb[leve.Id].Users;
                    }

                }
                return res;
            }
        }
        public static DateTime UserLogInTime;
        private static Timer OutTimer;

        private static ISqlSugarClient db;
        public static ISqlSugarClient Db => db ??= (ISqlSugarClient)((App)Application.Current).Container.Resolve(typeof(ISqlSugarClient));
        #region 附加属性
        public static string GetName(DependencyObject obj)
        {
            return (string)obj.GetValue(NameProperty);
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        /// <summary>
        /// 权限管控的名字用于索引和显示
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(UserHelp), new System.Windows.PropertyMetadata(null, Callback));



        public static Action<FrameworkElement, Binding> GetBindingOp(DependencyObject obj)
        {
            return (Action<FrameworkElement, Binding>)obj.GetValue(BindingOpProperty);
        }

        public static void SetBindingOp(DependencyObject obj, Action<FrameworkElement, Binding> value)
        {
            obj.SetValue(BindingOpProperty, value);
        }

        /// <summary>
        /// 权限管理操作
        /// </summary>
        public static readonly DependencyProperty BindingOpProperty =
            DependencyProperty.RegisterAttached("BindingOp", typeof(Action<FrameworkElement, Binding>), typeof(UserHelp), new System.Windows.PropertyMetadata(new Action<FrameworkElement, Binding>((e, b) => e.SetBinding(FrameworkElement.IsEnabledProperty, b))));


        #endregion



        private static void Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Application.Current is not App) return;

            var name = GetName(d);

            if (string.IsNullOrEmpty(name) || d is not FrameworkElement element)
                return;

            GetBindingOp(d)(element, CreatePermissionOperation(name));
        }

        /// <summary>
        /// 创建权限
        /// </summary>
        /// <param name="d"></param>
        /// <param name="name"></param>
        /// <param name="element"></param>
        public static Binding CreatePermissionOperation(string name)
        {
            Authority authority = null;

            lock (AuthorityDict)
            {
                ref var author = ref CollectionsMarshal.GetValueRefOrAddDefault(AuthorityDict, name, out var exists);
                if (!exists)
                {
                    author = new Authority { Name = name };
                    Db.Insertable(author).ExecuteCommand();
                }
                authority = author;
            }
            UpLimitsOfAuthority();
            var bind = new Binding(nameof(authority.IsVisible))
            {
                Source = authority,
                Mode = BindingMode.OneWay
            };
            return bind;
        }

        static UserHelp()
        {
            if (Application.Current is not App) return;
            MachineCtrl.MachineCtrlInstance.RunsCtrl.PropertyChanged += RunsCtrlPropertyChanged;
            MachineCtrl.MachineCtrlInstance.PropertyChanged += MachineCtrlInstancePropertyChanged;
            Db.CodeFirst.InitTables(typeof(FuncMapLeven));
            Db.CodeFirst.InitTables(typeof(User));
            Db.CodeFirst.InitTables(typeof(UserLevel));


        }

        private static void MachineCtrlInstancePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MachineCtrl.CurUser))
            {
                UpLimitsOfAuthority();
                TimeOut();

            }
        }

        /// <summary>
        /// 用户退出倒计时
        /// </summary>
        private static void TimeOut()
        {

            if (null == MachineCtrl.MachineCtrlInstance.CurUser)
            {
                OutTimer.Change(-1, -1);

                MachineCtrl.MachineCtrlInstance.UserName = "未登录";
                return;
            }
            UserLogInTime = DateTime.Now;
            OutTimer ??= new Timer(s =>
            {
                var curUser = MachineCtrl.MachineCtrlInstance?.CurUser;
                if (MachineCtrl.MachineCtrlInstance?.CurUser == null) return;

                // 进行倒计时
                var countdown = 180 - (int)((DateTime.Now - UserLogInTime).TotalSeconds);
                MachineCtrl.MachineCtrlInstance.UserName = $"{curUser.Name} [{(curUser.Level == null ? "未选择等级" : curUser.Level.Name)}]({countdown})";
                if (countdown < 0)
                {
                    MachineCtrl.MachineCtrlInstance.CurUser = null;
                    OutTimer.Change(-1, -1);
                }
            });
            OutTimer.Change(0, 100);

        }

        private static void RunsCtrlPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RunCtrl.McState))
            {
                UpLimitsOfAuthority();
            }
        }

        /// <summary>
        /// 刷新权限
        /// </summary>
        private static void UpLimitsOfAuthority()
        {
            var user = MachineCtrl.MachineCtrlInstance.CurUser;
            var mcState = MachineCtrl.MachineCtrlInstance.RunsCtrl.McState;

            lock (AuthorityDict)
            {
                foreach (var item in AuthorityDict)
                {
                    var leves = item.Value.UserLevels;

                    item.Value.IsVisible = (item.Value.MCState.HasAnyFlags(mcState) &&
                        leves.Any(leve => leve.Users?.Any(u => u.Id == user?.Id) ?? false)) ||
                        MachineCtrl.MachineCtrlInstance.CurUser == RootUser;
                }
            }
        }


        /// <summary>
        /// 从数据库同步数据
        /// </summary>
        public static void DataBaseSynchronization()
        {
            var dbData = _AuthorityList.ToDictionary(authy => authy.Name);
            foreach (var (name, dbauthority) in dbData)
            {
                if (!AuthorityDict.TryGetValue(name, out var authority))
                {
                    AuthorityDict.Add(name, dbauthority);
                    continue;
                }

                #region 同步属性
                authority.MCState = dbauthority.MCState;

                authority.UserLevels = dbauthority.UserLevels;
                #endregion

                UpLimitsOfAuthority();
            }
        }


    }
}
