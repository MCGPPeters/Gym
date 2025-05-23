using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Gymnasium.UI.Views;

public partial class LossChartView : UserControl
{
    private Canvas? _canvas;
    public LossChartView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _canvas = this.FindControl<Canvas>("ChartCanvas");
    }

    public void RenderLosses(IReadOnlyList<double> losses, int movingAvgWindow = 20)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        if (losses.Count < 2) return;
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double maxLoss = Math.Max(1.0, Math.Abs(losses[0]));
        for (int i = 1; i < losses.Count; i++)
        {
            maxLoss = Math.Max(maxLoss, Math.Abs(losses[i]));
        }
        // Draw raw loss curve
        for (int i = 1; i < losses.Count; i++)
        {
            double x1 = (i - 1) * width / (losses.Count - 1);
            double y1 = height - (losses[i - 1] / maxLoss) * height * 0.9;
            double x2 = i * width / (losses.Count - 1);
            double y2 = height - (losses[i] / maxLoss) * height * 0.9;
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(x1, y1),
                EndPoint = new Avalonia.Point(x2, y2),
                Stroke = Brushes.Red, StrokeThickness = 2
            };
            _canvas.Children.Add(line);
        }
        // Draw moving average
        if (losses.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < losses.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += losses[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxLoss) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxLoss) * height * 0.9;
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(x1, y1),
                    EndPoint = new Avalonia.Point(x2, y2),
                    Stroke = Brushes.Orange, StrokeThickness = 2, StrokeDashArray = new Avalonia.Media.DoubleCollection { 4, 2 }
                };
                _canvas.Children.Add(line);
            }
        }
    }
}
