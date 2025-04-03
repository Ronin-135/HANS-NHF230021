using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImTools;
using Machine;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemControlLibrary;
using WPFMachine.Frame.BindingCorrelation;
using WPFMachine.Frame.Userlib;
using WPFMachine.Frame.Userlib.MVVM;
using static SystemControlLibrary.DataBaseRecord;
using System.Windows.Input;
using System.Windows;
using System.ComponentModel;

namespace WPFMachine.ViewModels
{
    partial class AuthorityManagementViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// 用户表
        /// </summary>
        [ObservableProperty]
        private BindingList<User> users = new();

        /// <summary>
        /// 选中用户
        /// </summary>
        [ObservableProperty]
        private User curUser;

        /// <summary>
        /// 等级表
        /// </summary>
        [ObservableProperty]
        private BindingList<LevelBox> levels = new();
        /// <summary>
        /// 选中等级表
        /// </summary>
        [ObservableProperty]
        private LevelBox curLeveBox;

        /// <summary>
        /// 可管理权限表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Authority> authoritys = new();

        [ObservableProperty]

        /// <summary>
        /// 映射关系
        /// </summary>
        private ObservableCollection<FuncMapLevelCheck> leveFunc = new();

        /// <summary>
        /// 当前选中权限
        /// </summary>
        [ObservableProperty]
        private Authority curAuthoritys;

        [ObservableProperty]
        private BitEnumerationSet runningState;


        /// <summary>
        /// 添加user
        /// </summary>
        [ObservableProperty]
        private string addUser;


        /// <summary>
        /// 添加密码
        /// </summary>
        [ObservableProperty]
        private string addPwd;

        /// <summary>
        /// 添加用户等级
        /// </summary>
        [ObservableProperty]
        private string addUserLevelName;

        /// <summary>
        /// 权限名字筛选
        /// </summary>
        [ObservableProperty]
        private string nameFiltering;

        /// <summary>
        /// 新建权限时是否Copy权限
        /// </summary>
        [ObservableProperty]
        private LevelBox copyLeve;

        private MachineCtrl ctrl;
        private SqlSugar.ISqlSugarClient db;

        public AuthorityManagementViewModel(MachineCtrl ctrl, SqlSugar.ISqlSugarClient db)
        {
            this.ctrl = ctrl;
            this.db = db;

            db.Ado.BeginTran();
            db.Updateable(UserHelp.AuthorityDict.Select(kv => kv.Value).ToArray());
            Users.AddRange(db.Queryable<User>().ToArray());
            db.Ado.CommitTran();
            Users.ListChanged += (s, e) =>
            {
                if (e.ListChangedType != ListChangedType.ItemDeleted)
                    db.Updateable(Users[e.NewIndex]).ExecuteCommand();


            };



        }
        /// <summary>
        /// 用户搜索更改时
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotImplementedException"></exception>
        partial void OnNameFilteringChanged(string value)
        {
            RefreshControllablePermissions();
        }

        /// <summary>
        /// 选择用户时
        /// </summary>
        /// <param name="value"></param>
        partial void OnCurUserChanged(User value)
        {
            RemoveUserCommand.NotifyCanExecuteChanged();
            if (value == null)
            {
                return;
            }
            PermissionRefresh();

        }

        /// <summary>
        /// 用户等级刷新
        /// </summary>
        private void PermissionRefresh()
        {
            Levels.Clear();

            var userlevels = db.Queryable<UserLevel>().Includes(x => x.Users).Includes(x => x.Authoriys).ToList();

            var userCheckBoxs = userlevels.Select(leve => new LevelBox() { UserLevel = leve, user = CurUser, Name = leve.Name }).ToList();

            userCheckBoxs.ForEach(userCheckBox =>
            {
                if (userCheckBox.user != null)
                {
                    userCheckBox.levels = userCheckBoxs;
                    userCheckBox.IsCheck = userCheckBox.user.LevelID == userCheckBox.UserLevel.Id;
                    userCheckBox.UserLevel.PropertyChanged +=
                    (s, e) =>
                        db.Updateable(userCheckBox.UserLevel).ExecuteCommand();
                }
            });
            Levels.AddRange(userCheckBoxs);
        }

        /// <summary>
        /// 选择权限变幻适
        /// </summary>
        /// <param name="value"></param>
        partial void OnCurLeveBoxChanged(LevelBox value)
        {
            RemoveLevelCommand.NotifyCanExecuteChanged();

            if (value == null)
                return;
            var levle = value.UserLevel;
            RefreshControllablePermissions();

        }

        /// <summary>
        /// 刷新可管控的权限
        /// </summary>
        private void RefreshControllablePermissions()
        {
            if (Authoritys.Count > 0)
                this.Authoritys.Clear();
            var authoritys = db.Queryable<Authority>().Includes(x => x.UserLevels)
                .ToArray()
                .Where(propitem => string.IsNullOrEmpty(NameFiltering) ? true : propitem.Name.Contains(NameFiltering));
            this.Authoritys.AddRange(authoritys);
        }

        /// <summary>
        /// 选择具体的权限时
        /// </summary>
        /// <param name="value"></param>
        partial void OnCurAuthoritysChanged(Authority value)
        {
            LeveFunc.Clear();
            if (value == null)
            {
                RunningState = null;
                return;
            }
            RunningState = new BitEnumerationSet(typeof(MCState), (int)value.MCState);
            RunningState.OutValueChanged += (s, valuen) =>
            {
                value.MCState = (MCState)valuen;
                db.Updateable(value).ExecuteCommand();
            };
            db.Ado.BeginTran();

            var leves = db.Queryable<UserLevel>()
                    .Includes(userleve => userleve.Authoriys)
                    .ToArray();

            var authority = db.Queryable<Authority>().Includes(x => x.UserLevels).InSingle(value.Guid);
            value.UserLevels = authority.UserLevels;

            db.Ado.CommitTran();
            LeveFunc.AddRange(leves.Select(userlevel => new FuncMapLevelCheck()
            {
                Func = value,
                Level = userlevel,
                Name = userlevel.Name,
                IsEditor = value.UserLevels?.Any(level => level.Id == userlevel.Id) ?? false
            }));


        }

        #region 导航相关
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (Authoritys.Count > 0)
                this.Authoritys.Clear();
            var authoritys = db.Queryable<Authority>().Includes(x => x.UserLevels).ToList();
            this.Authoritys.AddRange(authoritys);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            UserHelp.DataBaseSynchronization();
        }
        #endregion

        [RelayCommand]
        private void UserAddDialogClosed(bool? isadd)
        {
            if (isadd == false || string.IsNullOrEmpty(AddUser) || string.IsNullOrEmpty(AddPwd)) return;

            var User = new User() { UserName = AddUser, Password = AddPwd, Name = "New User" };

            User.Id = db.Insertable(User).ExecuteReturnIdentity();

            Users.Add(User);

            User.PropertyChanged += (s, e) => db.Updateable(s);
            AddUser = "";
            AddPwd = "";
        }

        [RelayCommand]
        private void AddUserLevel(bool? isadd)
        {
            if (isadd == false || string.IsNullOrEmpty(AddUserLevelName)) return;

            var leve = new UserLevel { Name = AddUserLevelName };
            db.Ado.BeginTran();

            leve.Id = db.Insertable(leve).ExecuteReturnIdentity();

            if (CopyLeve != null)
            {
                var copyleve = db.Queryable<UserLevel>().Where(x => x.Id == CopyLeve.UserLevel.Id).Includes(x => x.Authoriys).First();

                foreach (var item in copyleve.Authoriys)
                {
                    FuncMapLevelCheck.AddRelation(item, leve);

                }
            }



            db.Ado.CommitTran();


            AddUserLevelName = "";

            PermissionRefresh();

        }

        [RelayCommand(CanExecute = nameof(CanGreetUser))]
        private void RemoveUser(User user)
        {
            db.Deleteable(user).ExecuteCommand();
            Users.Remove(user);


        }
        private bool CanGreetUser(User user)
        {
            return CurUser is not null;
        }

        [RelayCommand(CanExecute = nameof(CanGreetLevel))]
        private void RemoveLevel(LevelBox level)
        {
            db.Ado.BeginTran();
            var dbleve = db.Queryable<UserLevel>().Includes(x => x.Users).Includes(x => x.Authoriys).InSingle(level.UserLevel.Id);

            foreach (var user in dbleve.Users)
            {
                user.LevelID = 0;
                db.Updateable(user).ExecuteCommand();
            }
            foreach (var authoriys in dbleve.Authoriys)
            {
                FuncMapLevelCheck.RemoveRelation(authoriys, dbleve);
            }
            db.Deleteable(dbleve).ExecuteCommand();

            RefreshControllablePermissions();


            db.Ado.CommitTran();
            PermissionRefresh();





        }

        private bool CanGreetLevel(LevelBox level)
        {
            return CurLeveBox is not null;
        }



    }


    public partial class AuthorityCheckBox : ObservableObject
    {
        public static event EventHandler<Authority> EventHandler;



        [ObservableProperty]
        private List<AuthorityCheckBox> checkBoxes;

        [ObservableProperty]
        private string name;


        public AuthorityCheckBox()
        {



        }
    }
}
