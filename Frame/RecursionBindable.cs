using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame
{
    public class RecursionBindable : BindableBase
    {
        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            return SetProperty<T>(ref storage, value, null, propertyName);
        }
        protected override bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            onChanged?.Invoke();
            if (storage is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += (s, e) => RaisePropertyChanged(propertyName);
            }
            RaisePropertyChanged(propertyName);
            return true;
        }


    }
}
