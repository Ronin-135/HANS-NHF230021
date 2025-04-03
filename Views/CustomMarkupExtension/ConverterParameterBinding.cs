using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPFMachine.Views
{
    class ConverterParameterBinding : Binding
    {

        IntermediateValue Value { get; } = new IntermediateValue();

        public Binding ParameterBinding
        {
            set
            {
                Value.ValueChanged += ValueValueChanged;
                BindingOperations.SetBinding(Value, IntermediateValue.ValueProperty, value);
            }
        }

        private void ValueValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConverterParameter ??= e.NewValue;
        }
    }

    class IntermediateValue : DependencyObject
    {


        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(IntermediateValue), new PropertyMetadata(null, (s, e) =>
            {
                if (s is IntermediateValue val)
                {
                    val.ValueChanged(s, e);
                }
            }));



        public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;


    }



}
