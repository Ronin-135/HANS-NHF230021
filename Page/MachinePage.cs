using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Page
{
    public class MachinePage : INotifyPropertyChanged
    {

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; UpProp(); }
        }
        /// <summary>
        /// 导航区域
        /// </summary>
        public string NavigationName { get; set; }
        public object DataContext;

        public event PropertyChangedEventHandler PropertyChanged;

        private void UpProp([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
