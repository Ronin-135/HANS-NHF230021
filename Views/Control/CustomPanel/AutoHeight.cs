using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WPFMachine.Views.Control.CustomPanel
{
    internal class AutoHeight : Panel
    {


        public int ColCount
        {
            get { return (int)GetValue(ColCountProperty); }
            set { SetValue(ColCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ColCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColCountProperty =
            DependencyProperty.Register("ColCount", typeof(int), typeof(AutoHeight), new PropertyMetadata(1));

        protected override Size MeasureOverride(Size availableSize)
        {
            var rects = Arrange(availableSize);

            double maxChildDesiredWidth = 0.0;
            double maxChildDesiredHeight = 0.0;

            //  Measure each child, keeping track of maximum desired width and height.
            for (int i = 0, count = InternalChildren.Count; i < count; ++i)
            {
                UIElement child = InternalChildren[i];
                // Measure the child.
                child.Measure(new Size { Height = rects[i].Height, Width = rects[i].Width });
                Size childDesiredSize = child.DesiredSize;

                if (maxChildDesiredWidth < childDesiredSize.Width)
                {
                    maxChildDesiredWidth = childDesiredSize.Width;
                }
                maxChildDesiredHeight = maxChildDesiredHeight < childDesiredSize.Height ? childDesiredSize.Width : maxChildDesiredHeight;


            }

            return new Size(maxChildDesiredWidth, maxChildDesiredHeight);
        }

        /// <summary>
        /// 计算布局位置
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        private List<Rect> Arrange(Size finalSize)
        {
            var Width = finalSize.Width / ColCount;

            var curx = 0;
            var cury = 0;
            var rects = new List<Rect>();
            var res = new List<Rect>();
            foreach (UIElement item in InternalChildren)
            {
                var count = AutoHeight.GetColSpan(item);
                var rowSpan = AutoHeight.GetRowSpan(item);

                if (rowSpan <= 0) rowSpan = 1;

                if (rowSpan > 1)
                {
                    if (curx != 0)
                    {
                        cury++;
                        curx = 0;
                    }


                    count = int.MaxValue;
                }

                if (count <= 0) count = 1;
                if (count > (ColCount - curx))
                {
                    count = (ColCount - curx);
                }

                rects.Add(new Rect()
                {
                    Width = Width * count,
                    X = curx * Width,
                    Y = cury,
                    Height = rowSpan
                    
                });
                curx += count;
                if (curx == ColCount)
                {
                    curx = 0;
                    cury+=rowSpan;

                }


            }

            var Height = finalSize.Height / (rects[^1].Y + 1);

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var rect = rects[i];
                rect.Height = Height * rect.Height;
                rect.Y = rect.Y * Height;
                res.Add(rect);

            }
            return res;


        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rects = Arrange(finalSize);
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                InternalChildren[i].Arrange(rects[i]);

            }




            return finalSize;
        }




        public static int GetColSpan(DependencyObject obj)
        {
            return (int)obj.GetValue(ColSpanProperty);
        }

        public static void SetColSpan(DependencyObject obj, int value)
        {
            obj.SetValue(ColSpanProperty, value);
        }

        // Using a DependencyProperty as the backing store for ColSpan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColSpanProperty =
            DependencyProperty.RegisterAttached("ColSpan", typeof(int), typeof(AutoHeight), new PropertyMetadata(1));




        public static int GetRowSpan(DependencyObject obj)
        {
            return (int)obj.GetValue(RowSpanProperty);
        }

        public static void SetRowSpan(DependencyObject obj, int value)
        {
            obj.SetValue(RowSpanProperty, value);
        }

        // Using a DependencyProperty as the backing store for RowSpan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RowSpanProperty =
            DependencyProperty.RegisterAttached("RowSpan", typeof(int), typeof(AutoHeight), new PropertyMetadata(1));


    }
}
