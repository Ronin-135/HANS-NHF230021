using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.RealTimeTemperature
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class ConditionAttribute: Attribute
    {
        public ConditionAttribute(string chName, bool isConditionP,bool isShow)
        {
            ChName = chName;
            IsConditionP = isConditionP;
            IsShow = isShow;
        }

        public string ChName { get; set; }
        public bool IsConditionP { get; set; }
        public bool IsShow { get; set; }
    }
}
