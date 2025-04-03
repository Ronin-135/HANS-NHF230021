using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WPFMachine.Frame.Userlib
{
    public partial class User : ObservableObject
    {

        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        [ObservableProperty]
        private string name;

        /// <summary>
        /// 登录用
        /// </summary>
        [ObservableProperty]
        private string userName;

        /// <summary>
        /// 密码
        /// </summary>
        [ObservableProperty]
        private string password;

        /// <summary>
        /// 等级ID
        /// </summary>
        [ObservableProperty]
        private int levelID;

        [Navigate(NavigateType.OneToOne, nameof(LevelID), nameof(UserLevel.Id))]
        public UserLevel Level { get; set; }

    }

    public class FuncMapLeven
    {

        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public int FuncID { get; set; }
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public int LevelId { get; set; }
    }
}
