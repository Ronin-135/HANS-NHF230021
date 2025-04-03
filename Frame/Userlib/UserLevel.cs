using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.Frame.Userlib
{
    public partial class UserLevel : ObservableObject
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }


        [ObservableProperty]
        private string name;

        [Navigate(NavigateType.OneToMany, nameof(User.LevelID))]
        public List<User> Users { get; set; }

        [Navigate(typeof(FuncMapLeven), nameof(FuncMapLeven.LevelId), nameof(FuncMapLeven.FuncID))]
        public List<Authority> Authoriys { get; set; }


    }
}
