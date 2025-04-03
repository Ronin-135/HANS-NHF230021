using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using WPFMachine.Views.AdditionalAttribute;
using System.Diagnostics;
using Machine;

namespace WPFMachine.Views
{
    class SchedulingAnimationTransition : DependencyObject, IMultiValueConverter
    {

        private double AbsoluteDifference = double.MinValue;

        public static Point GetAbsolutePosition(UIElement element, Visual visual)
        {
            try
            {
                var transform = element.TransformToAncestor(visual).
                Transform(new Point(0, 0)); ;

                return transform;
            }
            catch (Exception ex)
            {
                string strTmp = "SchedulingAnimationTransition界面异常：\r\n" + ex.ToString();
                MachineCtrl.GetInstance().WriteLog(strTmp, $"{MachineCtrl.GetInstance().ProductionFilePath}", "SchedulingAnimationTransition.log");
                throw;
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var goalElement = DispatchLocationHelper.GetPublicStaticGet(values[0].ToString(), values[1].ToString(), values[2].ToString());
                var goal = goalElement.Item1;
                int col = goalElement.Item2;
                var element = DispatchLocationHelper.GetPublicStaticGet("this").Item1;

                FrameworkElement Win = element;
                while (Win is not Viewbox && Win.Name != "Animated")
                {
                    Win = (FrameworkElement)VisualTreeHelper.GetParent(Win);
                }

                if (goal == null) return new Thickness(0);

                var transferPos = GetAbsolutePosition(element, (Visual)Win);
                var goalPos = GetAbsolutePosition(goal, (Visual)Win);
                double transferCenterPos = (transferPos.X + element.RenderSize.Width) / 2;


                double goalColLen = (double)(goal.RenderSize.Width / col);

                double goalCenterPos = (goalPos.X + goalColLen * ((int)values[2]));

                AbsoluteDifference = goalCenterPos - transferCenterPos;
                var res = new Thickness { Left = transferCenterPos + AbsoluteDifference - 50 };
                return res;
            }
            catch (Exception ex)
            {
                string strTmp = "SchedulingAnimationTransition界面异常：\r\n" + ex.ToString();
                MachineCtrl.GetInstance().WriteLog(strTmp, $"{MachineCtrl.GetInstance().ProductionFilePath}", "SchedulingAnimationTransition.log");
                throw;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
