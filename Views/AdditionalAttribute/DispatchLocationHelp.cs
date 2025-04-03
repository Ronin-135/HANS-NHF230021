using Machine;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WPFMachine.Views.AdditionalAttribute
{
    class DispatchLocationHelper : DependencyObject
    {




        public static string GetDispatch(DependencyObject obj)
        {
            return (string)obj.GetValue(DispatchProperty);
        }

        public static void SetDispatch(DependencyObject obj, string value)
        {
            obj.SetValue(DispatchProperty, value);
        }

        // Using a DependencyProperty as the backing store for Dispatch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DispatchProperty =
            DependencyProperty.RegisterAttached("Dispatch", typeof(string), typeof(DispatchLocationHelper), new PropertyMetadata(null, AddProp));

        private static void AddProp(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement ui || ui is null) return;
            var name = GetDispatch(d);
            string[] strings = name.Split(",");
            try
            {
                if (DispatchDict.ContainsKey(strings[0] + strings[1] + strings[2]))
                {
                    DispatchDict[strings[0] + strings[1] + strings[2]] = (ui, int.Parse(strings[3]));
                }
                else
                {
                    DispatchDict.Add(strings[0] + strings[1] + strings[2], (ui, int.Parse(strings[3])));
                }
            }
            catch (Exception ex)
            {
                string streLog = "";
                foreach (string str in strings)
                {
                    streLog += str + ",";
                }
                string strTmp = "DispatchLocationHelper界面异常：\r\n" + streLog + "\r\n" + ex.ToString();
                MachineCtrl.GetInstance().WriteLog(strTmp, $"{MachineCtrl.GetInstance().ProductionFilePath}", "DispatchLocationHelper.log");
                throw;
            }
            
        }

        static Dictionary<string, (FrameworkElement,int)> DispatchDict = new();



        public static (FrameworkElement,int) GetPublicStaticGet(string pub, string row = "", string col = "")
        {
            var ui = DispatchDict.FirstOrDefault(kv => Regex.IsMatch($"{pub},{row},{col}", kv.Key));
            return ui.Value;
        }
    }
}
