using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScottPlot.Plottable;
using SqlSugar.Extensions;

namespace ScottPlot.Demo.WPF.WpfDemos
{
    /// <summary>
    /// Interaction logic for MouseTracker.xaml
    /// </summary>
    public partial class MouseTracker : Window
    {
        Crosshair Crosshair;

        public MouseTracker()
        {
            InitializeComponent();

            TimeSpan ts = TimeSpan.FromSeconds(6); // time between data points
            double sampleRate = (double)TimeSpan.TicksPerDay / ts.Ticks;
            double[] ys = DataGen.RandomWalk(10000,3);
            double[] ys1 = DataGen.RandomWalk(10000,2);
            double[] ys2 = DataGen.RandomWalk(10000,6);

            var signalPlot =  wpfPlot1.Plot.AddSignal(ys, sampleRate,label:"bb");
            var signalPlot1 = wpfPlot1.Plot.AddSignal(ys1, sampleRate,label:"aa");
            var signalPlot2 = wpfPlot1.Plot.AddSignal(ys2, sampleRate,label:"cc");
            Crosshair = wpfPlot1.Plot.AddCrosshair(new DateTime(1970, 10, 1).ToOADate(), 0);

            wpfPlot1.Plot.XAxis.DateTimeFormat(true);


            var legend = wpfPlot1.Plot.Legend(enable: true);
            legend.Orientation = Orientation.Horizontal;

            // Set start date
            signalPlot.OffsetX = new DateTime(1970, 10, 1).ToOADate();
            signalPlot1.OffsetX = new DateTime(1970, 10, 1).ToOADate();
            signalPlot2.OffsetX = new DateTime(1970, 10, 1).ToOADate();
            wpfPlot1.Refresh();
        }
        

        public MouseTracker(List<ChartLine>  chartPs)
        {
            InitializeComponent();

            foreach (ChartLine chartLine in chartPs)
            {
                //计算转换时间间隔
                TimeSpan ts = TimeSpan.FromSeconds(chartLine.SampleRate); // time between data points
                double sampleRate = (double)TimeSpan.TicksPerDay / ts.Ticks;
                //设置线段的点位，间隔，线段标签
                var signalPlot = wpfPlot1.Plot.AddSignal(chartLine.Ys, sampleRate, label: chartLine.Label);

                //设置悬浮十字虚线起始位置
                Crosshair = wpfPlot1.Plot.AddCrosshair(chartLine.OffsetX, 0);

                //开启x转日期
                wpfPlot1.Plot.XAxis.DateTimeFormat(true);

                //图例（线段说明）
                var legend = wpfPlot1.Plot.Legend(enable: true);
                legend.Orientation = Orientation.Horizontal;

                //起始地址
                signalPlot.OffsetX = chartLine.OffsetX;
            }
            wpfPlot1.Refresh();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            int pixelX = (int)e.MouseDevice.GetPosition(wpfPlot1).X;
            int pixelY = (int)e.MouseDevice.GetPosition(wpfPlot1).Y;

            (double coordinateX, double coordinateY) = wpfPlot1.GetMouseCoordinates();

            XPixelLabel.Content = $"{pixelX:0.000}";
            YPixelLabel.Content = $"{pixelY:0.000}";

            //var s = new DateTime(1970, 10, 1).a + wpfPlot1.Plot.GetCoordinateX(pixelX);

            XCoordinateLabel.Content = $"{wpfPlot1.Plot.GetCoordinateX(pixelX):0.00000000}";
            YCoordinateLabel.Content = $"{wpfPlot1.Plot.GetCoordinateY(pixelY):0.00000000}";

            Crosshair.X = coordinateX;
            Crosshair.Y = coordinateY;

            wpfPlot1.Refresh();
        }

        private void wpfPlot1_MouseEnter(object sender, MouseEventArgs e)
        {
            MouseTrackLabel.Content = "Mouse ENTERED the plot";
            Crosshair.IsVisible = true;
        }

        private void wpfPlot1_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseTrackLabel.Content = "Mouse LEFT the plot";
            XPixelLabel.Content = "--";
            YPixelLabel.Content = "--";
            XCoordinateLabel.Content = "--";
            YCoordinateLabel.Content = "--";

            Crosshair.IsVisible = false;
            wpfPlot1.Refresh();
        }
    }
}
