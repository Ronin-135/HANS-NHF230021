using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.DataStructure
{
    /// <summary>
    /// 可观测的KV对象
    /// </summary>
    public partial class ReadObsName<TKey, TVal> : ObservableObject
    {
        [ObservableProperty]
        private TKey key;

        [ObservableProperty]
        private TVal value;
    }
}
