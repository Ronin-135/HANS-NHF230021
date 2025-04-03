using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.RealTimeTemperature
{
    public partial class Condition : ObservableObject
    {
        public Condition(string chName, bool isConditionP,bool isShow)
        {
            ChName = chName;
            IsConditionP = isConditionP;
            IsShow = isShow;
        }
        
        public Condition(ConditionAttribute conditionAttribute)
        {
            ChName = conditionAttribute.ChName;
            IsConditionP = conditionAttribute.IsConditionP;
            IsShow = conditionAttribute.IsShow;
        }

        [ObservableProperty]
        public string chName;
        [ObservableProperty]
        public bool isConditionP;
        [ObservableProperty]
        public bool isShow;

    }
}
