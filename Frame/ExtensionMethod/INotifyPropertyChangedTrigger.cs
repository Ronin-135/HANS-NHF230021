using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.ExtensionMethod
{
    public interface INotifyPropertyChangedTrigger : INotifyPropertyChanged
    {
        public void StartPropertyChanged(string name);
    }
}
