using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFMachine.Views.Control.ParamaterStyleFactor
{
    class ParamaterStyle : PropertyGridControlFactory
    {
        protected override FrameworkElement CreateDefaultControl(PropertyItem property)
        {
            var textbox = (TextBox)base.CreateDefaultControl(property);
            var paramaterStyle = App.Current.Resources["paramaterStyle"];

            textbox.Style = (Style)paramaterStyle;

            return textbox;
        }
        protected override FrameworkElement CreateBoolControl(PropertyItem property) {
            var toggleButton = (ToggleButton)base.CreateBoolControl(property);
            var paramaterStyle = App.Current.Resources["22"];

            toggleButton.Width = 50;

            //toggleButton.Style = (Style)paramaterStyle;
            return toggleButton;
        }
    }
}
