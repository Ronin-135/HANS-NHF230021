using MaterialDesignThemes.Wpf;
using System;
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

namespace WPFMachine.Views
{
    /// <summary>
    /// LoadBox.xaml 的交互逻辑
    /// </summary>
    public partial class LoadBox : UserControl
    {
        public LoadBox()
        {
            InitializeComponent();
        }


        public async void Show()
        {
            await DialogHost.Show(this, "RootDialog", null, null, null);
        }

        public void Close()
        {
            DialogHost.CloseDialogCommand.Execute(this, null);
        }
    }
}
