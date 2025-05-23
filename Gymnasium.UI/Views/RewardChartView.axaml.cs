using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Gymnasium.UI.Views;

public partial class RewardChartView : UserControl
{
    private Canvas? _canvas;
    public RewardChartView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _canvas = this.FindControl<Canvas>("ChartCanvas");
    }

    public void RenderRewards(IReadOnlyList<double> rewards, int movingAvgWindow = 20)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        if (rewards.Count < 2) return;
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double maxReward = Math.Max(1.0, Math.Max(Math.Abs(rewards[0]), Math.Abs(rewards[^1])));

        // Draw raw reward curve
        for (int i = 1; i < rewards.Count; i++)
        {
            double x1 = (i - 1) * width / (rewards.Count - 1);
            double y1 = height - (rewards[i - 1] / maxReward) * height * 0.9;
            double x2 = i * width / (rewards.Count - 1);
            double y2 = height - (rewards[i] / maxReward) * height * 0.9;
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(x1, y1),
                EndPoint = new Avalonia.Point(x2, y2),
                Stroke = Brushes.Blue, StrokeThickness = 2
            };
            _canvas.Children.Add(line);
        }

        // Draw moving average
        if (rewards.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < rewards.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += rewards[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxReward) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxReward) * height * 0.9;
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
