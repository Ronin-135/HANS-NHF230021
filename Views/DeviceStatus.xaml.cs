using HslCommunication.Enthernet;
using Machine;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using WPFMachine.ViewModels;

namespace WPFMachine.Views
{
    /// <summary>
    /// DeviceStatus.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceStatus : Window
    {
        public DeviceStatus()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeviceStatusViewModel viewModel)
            {
                viewModel.CurrentWindow = this;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            string strErr = "";
            MachineCtrl.GetInstance().MesStateAndStopReasonUpload(null, "5", "0", ref strErr);
        }
    }
}
