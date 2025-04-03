using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFMachine.Frame.BindingCorrelation;

namespace WPFMachine.Frame.Userlib.MVVM
{


    internal class BitEnumerationSet : Collection<ObsKeyValue>
    {
        private Type EnumType;
        public event EventHandler<int> OutValueChanged;
        public int OutValue { get; set; }

        public new void Add(ObsKeyValue item)
        {
            base.Add(item);
            item.PropertyChanged += ItemPropertyChanged;
        }

        private void ItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObsKeyValue.Value))
            {
                var obsKV = (ObsKeyValue)sender;
                var enumNumer = (int)Enum.Parse(EnumType, obsKV.Key);
                if (obsKV.Value)
                    OutValue |= enumNumer;
                else
                {
                    OutValue &= ~enumNumer;
                }
                OutValueChanged?.Invoke(this, OutValue);
            }
        }

        public BitEnumerationSet(Type enumType, int value) 
        {
            this.EnumType = enumType;
            this.OutValue = value;
            foreach (var key in Enum.GetNames(EnumType))
            {
                var enumNumer = (int)Enum.Parse(EnumType, key);


                Add(new ObsKeyValue { Key = key, Value = (enumNumer & OutValue) > 0 });
            }
        }
    }
    public partial class ObsKeyValue : ObservableObjectResourceDictionary 
    {
        [ObservableProperty]
        public string key;

        [ObservableProperty] 
        public bool value;

        [ObservableProperty]
        public string name;

        partial void OnKeyChanged(string value)
        {
            SetPropertyResourceDictionary(ref name, value, nameof(Name));
        }
    }


}
