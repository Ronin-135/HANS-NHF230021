using System.Drawing;

namespace ScottPlot.Demo.WPF.WpfDemos
{
    public class ChartLine
    {
        double[] ys;
        double sampleRate = 1;
        Color ? color = null;
        string label = null;
        double offsetX;
        public ChartLine(double[] ys = null, double sampleRate = 0, Color? color = null, string label = null, double offsetX = 0)
        {
            this.ys = ys;
            this.sampleRate = sampleRate;
            this.color = color;
            this.label = label;
            this.offsetX = offsetX;
        }

        public double[] Ys { get => ys; set => ys = value; }
        public double SampleRate { get => sampleRate; set => sampleRate = value; }
        public Color? Color { get => color; set => color = value; }
        public string Label { get => label; set => label = value; }
        public double OffsetX { get => offsetX; set => offsetX = value; }
    }
}