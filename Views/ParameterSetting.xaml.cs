using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFMachine.Frame.Userlib;

namespace WPFMachine.Views
{
    /// <summary>
    /// ParameterSetting.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterSetting : UserControl
    {
        public ParameterSetting()
        {
            InitializeComponent();
        }

        private void PropertyGridCreateRowCall(object sender, PropertyTools.ItemAttributeInformation e)
        {
            UserHelp.SetName(e.Grid, $"{e.Tab.Header}.{e.Item.DisplayName}");
        }
    }
}
