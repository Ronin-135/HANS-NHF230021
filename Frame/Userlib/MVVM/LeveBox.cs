using CommunityToolkit.Mvvm.ComponentModel;
using Machine;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.Userlib.MVVM
{
    public partial class LevelBox : ObservableObject
    {
        public static event EventHandler<User> EventHandler;
        public User user { get; set; }

        [ObservableProperty]
        public bool isCheck;

        public ICollection<LevelBox> levels { get; set; }

        public UserLevel UserLevel { get; set; }

        [ObservableProperty]
        public string name;


        partial void OnIsCheckChanged(bool value)
        {
            if (value == true)
            {
                user.LevelID = UserLevel.Id;
                IsCheck = true;
                levels.Where(leve => leve.UserLevel != UserLevel).ForEach(UserLevel => UserLevel.IsCheck = false);
                EventHandler?.Invoke(this, user);
            }

        }
        partial void OnNameChanged(string value)
        {
            UserLevel.Name = value;
        }

    }
}
