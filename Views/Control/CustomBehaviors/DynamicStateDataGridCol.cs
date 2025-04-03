using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFMachine.Views
{
    class DynamicStateDataGridCol : Behavior<DataGrid>
    {


        public IEnumerable<DataGridColumn> Column
        {
            get { return (IEnumerable<DataGridColumn>)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register("Column", typeof(IEnumerable<DataGridColumn>), typeof(DynamicStateDataGridCol), new PropertyMetadata(null, up));

        private static void up(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = (DynamicStateDataGridCol)d;
            if (e.NewValue == null) return;
            dataGrid.AssociatedObject.Columns.Clear();
            dataGrid.AssociatedObject.Columns.AddRange(e.NewValue as IEnumerable<DataGridColumn>);
        }
    }
}
