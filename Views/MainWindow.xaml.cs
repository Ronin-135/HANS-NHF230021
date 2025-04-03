using System.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFMachine.Frame.Userlib;
using HelperLibrary;
using Machine;
using Prism.Services.Dialogs;

namespace WPFMachine.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }
        #region 点击事件 窗口的关闭缩小和放大
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
    
            Close();

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SystemControlLibrary.MCState mCState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (mCState == SystemControlLibrary.MCState.MCRunning || mCState == SystemControlLibrary.MCState.MCInitializing)
            {
                ShowMsgBox.ShowDialog("设备处于【初始化或运行中】禁止关闭软件", MessageType.MsgWarning, 5);

                e.Cancel = true;
                return;
            }

            var user = MachineCtrl.MachineCtrlInstance.CurUser;
            if (user == null)
            {
                ShowMsgBox.ShowDialog("需登录权限才能关闭软件", MessageType.MsgWarning);
                e.Cancel = true;
                return;
            }

            if (ButtonResult.OK != ShowMsgBox.ShowDialog("是否需要关闭软件？", MessageType.MsgQuestion).Result)
            {
                e.Cancel = true;
            }
        }
        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                ////代码在这
                //this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight-15;
                //this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.MaxWidth = SystemParameters.WorkArea.Width; ;
            }

            else
                WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// s鼠标左键点下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void Menu_Base_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //DragMove();
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

       
    }
    #endregion
}

