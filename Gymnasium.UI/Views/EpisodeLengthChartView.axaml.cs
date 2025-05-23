using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Gymnasium.UI.Views;

public partial class EpisodeLengthChartView : UserControl
{
    private Canvas? _canvas;
    public EpisodeLengthChartView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _canvas = this.FindControl<Canvas>("ChartCanvas");
    }

    public void RenderLengths(IReadOnlyList<int> lengths, int movingAvgWindow = 20)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        if (lengths.Count < 2) return;
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double maxLen = Math.Max(1.0, Math.Max(lengths[0], lengths[^1]));
        // Draw raw episode length curve
        for (int i = 1; i < lengths.Count; i++)
        {
            double x1 = (i - 1) * width / (lengths.Count - 1);
            double y1 = height - (lengths[i - 1] / maxLen) * height * 0.9;
            double x2 = i * width / (lengths.Count - 1);
            double y2 = height - (lengths[i] / maxLen) * height * 0.9;
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(x1, y1),
                EndPoint = new Avalonia.Point(x2, y2),
                Stroke = Brushes.Green, StrokeThickness = 2
            };
            _canvas.Children.Add(line);
        }
        // Draw moving average
        if (lengths.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < lengths.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += lengths[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxLen) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxLen) * height * 0.9;
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(x1, y1),
                    EndPoint = new Avalonia.Point(x2, y2),
                    Stroke = Brushes.Orange, StrokeThickness = 2, StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 2 }
                };
                _canvas.Children.Add(line);
            }
        }
    }
}
