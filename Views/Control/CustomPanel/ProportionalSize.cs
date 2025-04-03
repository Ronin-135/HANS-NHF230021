using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFMachine.Views.Control.CustomPanel
{
    class ProportionalSize : Panel
    {


        public double WideScale
        {
            get { return (double)GetValue(WideScaleProperty); }
            set { SetValue(WideScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WideScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WideScaleProperty =
            DependencyProperty.Register("WideScale", typeof(double), typeof(ProportionalSize), new PropertyMetadata(1d));




        public double HighProportion
        {
            get { return (double)GetValue(HighProportionProperty); }
            set { SetValue(HighProportionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighProportionProperty =
            DependencyProperty.Register("HighProportion", typeof(double), typeof(ProportionalSize), new PropertyMetadata(1d));



        protected override Size ArrangeOverride(Size finalSize)
        {
            if (InternalChildren.Count > 0)
                InternalChildren[0].Arrange(new Rect
                {
                    X = 0,
                    Y = 0,
                    Width = finalSize.Width * WideScale,
                    Height = finalSize.Height * HighProportion
                });

            return finalSize;
        }
    }
}
