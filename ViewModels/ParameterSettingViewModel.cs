using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Machine;
using Prism.Mvvm;

namespace WPFMachine.ViewModels
{
    class ParameterSettingViewModel : BindableBase
    {
        private List<string> _text;
        public List<string> text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
        }

        private IEnumerable<object> modules;

        public MachineCtrl Ctrl { get; }

        public IEnumerable<object> Modules
        {
            get { return modules; }
            set { SetProperty(ref modules, value); }
        }

        private object curModule;
        public object CurModule
        {
            get { return curModule; }
            set { SetProperty(ref curModule, value, () => PropTabName = value.GetType().GetProperty("RunName").GetValue(value).ToString()); }
        }


        private object selectedObject;
        public object SelectedObject
        {
            get { return selectedObject; }
            set { SetProperty(ref selectedObject, value); }
        }
        private string propTabName;
        public string PropTabName
        {
            get { return propTabName; }
            set { SetProperty(ref propTabName, value); }
        }

        public ParameterSettingViewModel(IEnumerable<RunProcess> modules, MachineCtrl ctrl)
        {
            Ctrl = ctrl;
            var list = new List<object>() { ctrl};
            list.AddRange(modules);
            Modules = list;

        }
    }
}
