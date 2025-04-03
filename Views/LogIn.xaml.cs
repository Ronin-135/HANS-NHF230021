
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using WPFMachine.ViewModels;

namespace WPFMachine.Views
{
    /// <summary>
    /// LogIn.xaml 的交互逻辑
    /// </summary>
    public partial class LogIn : UserControl
    {
        public LogIn()
        {
            InitializeComponent();
            DataContext = new LogInViewModel(Close);
        }
        public async Task<object> Show()
        {
            return await DialogHost.Show(this, "MainDialog", null, null, null);

        }

        public void Close(object obj)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                //DialogHost.Close(obj);
                DialogHost.CloseDialogCommand.Execute(this, null);

            });
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
