using MaterialDesignThemes.Wpf;
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

namespace WPFMachine.Views
{
    /// <summary>
    /// HistoricalRecord.xaml 的交互逻辑
    /// </summary>
    public partial class HistoricalRecord : UserControl
    {




        public static DateTime GetStartTime(DependencyObject obj)
        {
            return (DateTime)obj.GetValue(StartTimeProperty);
        }

        public static void SetStartTime(DependencyObject obj, DateTime value)
        {
            obj.SetValue(StartTimeProperty, value);
        }

        // Using a DependencyProperty as the backing store for StartTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.RegisterAttached("StartTime", typeof(DateTime), typeof(HistoricalRecord), new PropertyMetadata(DateTime.Now));



        public static DateTime GetEndTime(DependencyObject obj)
        {
            return (DateTime)obj.GetValue(EndTimeProperty);
        }

        public static void SetEndTime(DependencyObject obj, DateTime value)
        {
            obj.SetValue(EndTimeProperty, value);
        }

        // Using a DependencyProperty as the backing store for EndTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.RegisterAttached("EndTime", typeof(DateTime), typeof(HistoricalRecord), new PropertyMetadata(DateTime.Now));





        public HistoricalRecord()
        {
            InitializeComponent();
        }
        public void CombinedDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            var startTime = GetStartTime(this);
            var endTime = GetEndTime(this);
            CombinedCalendarStart.SelectedDate = startTime;
            CombinedClockStart.Time = startTime;
            CombinedCalendarEnd.SelectedDate = endTime;
            CombinedClockEnd.Time = endTime;

        }

        public void CombinedDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (Equals(eventArgs.Parameter, "1") &&
                CombinedCalendarStart.SelectedDate is DateTime selectedDate)
            {
                var combined = selectedDate.AddSeconds(CombinedClockStart.Time.TimeOfDay.TotalSeconds);
                SetStartTime(this, combined);
            }
            if (Equals(eventArgs.Parameter, "1") &&
    CombinedCalendarEnd.SelectedDate is DateTime selectedDateend)
            {
                var combined = selectedDateend.AddSeconds(CombinedClockEnd.Time.TimeOfDay.TotalSeconds);
                SetEndTime(this, combined);
            }
        }
        public void CombinedDialogClosingEventHandlerEnd(object sender, DialogClosingEventArgs eventArgs)
        {
            if (Equals(eventArgs.Parameter, "1") &&
    CombinedCalendarEnd.SelectedDate is DateTime selectedDateend)
            {
                var combined = selectedDateend.AddSeconds(CombinedClockEnd.Time.TimeOfDay.TotalSeconds);
                SetEndTime(this, combined);
            }
        }
    }
}
