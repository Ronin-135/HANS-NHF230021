using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using WPFMachine.Frame.Userlib;

namespace WPFMachine.Views.Control.CustomMarkupExtension
{
    /// <summary>
    /// 用户权限标记扩展
    /// </summary>
    class UserLimitsOfAuthorityBind : System.Windows.Markup.MarkupExtension

    {

        [ConstructorArgument("Name")]
        public string UserName { get; set; }

        #region Binding
        public IValueConverter Converter { get; set; }

        public object ConverterParameter { get; set; }

        #endregion

        public UserLimitsOfAuthorityBind() { }
        public UserLimitsOfAuthorityBind(string Name)
        {
            UserName = Name;
        }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Application.Current is not App) return new Binding().ProvideValue(serviceProvider);

            IProvideValueTarget provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

            Binding binding = UserHelp.CreatePermissionOperation(UserName);

            FastDeepCloner.DeepCloner.CloneTo(this, binding);

            return binding.ProvideValue(serviceProvider);
        }
}
}
