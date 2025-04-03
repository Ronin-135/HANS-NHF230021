using CommunityToolkit.Mvvm.ComponentModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace WPFMachine.Frame.Userlib
{
    public partial class Authority : ObservableObject
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Guid { get; set; }

        /// <summary>
        /// 权限名
        /// </summary>
        [ObservableProperty]
        private string name;


        [Navigate(typeof(FuncMapLeven), nameof(FuncMapLeven.FuncID), nameof(FuncMapLeven.LevelId))]//注意顺序

        public List<UserLevel> UserLevels { get; set; }


        public MCState MCState { get; set; }



        /// <summary>
        /// 是否可见
        /// </summary>

        private bool isVisible;
        /// <summary>
        /// 是否可见
        /// </summary>
        [SugarColumn(IsIgnore = false)]

        public bool IsVisible
        {
            get { return isVisible; }
            set { SetProperty(ref isVisible, value); }
        }


        public void UpVisible()
        {
            IsVisible = true;
        }

    }
}
