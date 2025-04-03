using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFMachine.Frame.ExtensionMethod
{
    public static class INotifyPropertyChangedTriggerEx 
    {
        public static void SetPropertyResources(this INotifyPropertyChangedTrigger obj, ref string storage, string value, Action callBack = null, [CallerMemberName] string propName = "")
        {
            var Language = Application.Current.Resources[value];
            if (Language != null)
                SetProperty(obj, ref storage, Language.ToString(), callBack, propName);
        }


        public static void SetProperty<T>(this INotifyPropertyChangedTrigger obj, ref T storage, T value, Action callBack = null, [CallerMemberName] string propName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value) || propName == "") return;
            storage = value;
            callBack?.Invoke();
            obj.StartPropertyChanged(propName);
        }
    }
}
