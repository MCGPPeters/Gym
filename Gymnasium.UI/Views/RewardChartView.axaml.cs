using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;

namespace Gymnasium.UI.Views;

public partial class RewardChartView : UserControl
{
    private Canvas? _canvas;
    private readonly IBrush _gridLineBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));
    private readonly IBrush _axisFontBrush = new SolidColorBrush(Color.Parse("#757575"));
    private readonly IBrush _rewardLineBrush = new SolidColorBrush(Color.Parse("#1976D2"));
    private readonly IBrush _avgLineBrush = new SolidColorBrush(Color.Parse("#FF9800"));
    
    public RewardChartView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _canvas = this.FindControl<Canvas>("ChartCanvas");
    }

    public void RenderRewards(IReadOnlyList<double> rewards, int movingAvgWindow = 20)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        if (rewards.Count < 2) 
        {
            DrawNoDataMessage();
            return;
        }
        
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double padding = 40; // Padding for axes
        double chartWidth = width - (padding * 2);
        double chartHeight = height - (padding * 2);
        
        // Find min and max for better scaling
        double maxReward = double.MinValue;
        double minReward = double.MaxValue;
        foreach (var reward in rewards)
        {
            maxReward = Math.Max(maxReward, reward);
            minReward = Math.Min(minReward, reward);
        }
        
        // Add some padding to the range
        double range = maxReward - minReward;
        maxReward += range * 0.1;
        minReward -= range * 0.1;
        if (Math.Abs(range) < 0.001) // Handle flat lines
        {
            maxReward = maxReward + 1;
            minReward = minReward - 1;
        }
        
        // Draw grid
        DrawGrid(padding, chartWidth, chartHeight, maxReward, minReward);
        
        // Draw reward curve
        var rewardPoints = new List<Avalonia.Point>();
        for (int i = 0; i < rewards.Count; i++)
        {
            double x = padding + (i * chartWidth / (rewards.Count - 1));
            double y = padding + chartHeight - ((rewards[i] - minReward) / (maxReward - minReward) * chartHeight);
            rewardPoints.Add(new Avalonia.Point(x, y));
        }
        
        // Add polyline for rewards
        var rewardLine = new Polyline
        {
            Points = new Avalonia.Points(rewardPoints),
            Stroke = _rewardLineBrush,
            StrokeThickness = 2.5,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Fill = null
        };
        _canvas.Children.Add(rewardLine);
        
        // Draw key points
        for (int i = 0; i < rewards.Count; i += Math.Max(1, rewards.Count / 20))
        {
            double x = padding + (i * chartWidth / (rewards.Count - 1));
            double y = padding + chartHeight - ((rewards[i] - minReward) / (maxReward - minReward) * chartHeight);
            
            var point = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = _rewardLineBrush
            };
            Canvas.SetLeft(point, x - 3);
            Canvas.SetTop(point, y - 3);
            _canvas.Children.Add(point);
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
            
            var avgPoints = new List<Avalonia.Point>();
            for (int i = 0; i < avg.Count; i++)
            {
                double x = padding + (i * chartWidth / (avg.Count - 1));
                double y = padding + chartHeight - ((avg[i] - minReward) / (maxReward - minReward) * chartHeight);
                avgPoints.Add(new Avalonia.Point(x, y));
            }
            
            var avgLine = new Polyline
            {
                Points = new Avalonia.Points(avgPoints),
                Stroke = _avgLineBrush,
                StrokeThickness = 2.5,
                StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 6, 3 },
                StrokeLineCap = PenLineCap.Round,
                StrokeJoin = PenLineJoin.Round,
                Fill = null
            };
            _canvas.Children.Add(avgLine);
        }
    }
    
    private void DrawGrid(double padding, double chartWidth, double chartHeight, double maxValue, double minValue)
    {
        // Horizontal grid lines
        for (int i = 0; i <= 5; i++)
        {
            double y = padding + (i * chartHeight / 5);
            var gridLine = new Line
            {
                StartPoint = new Avalonia.Point(padding, y),
                EndPoint = new Avalonia.Point(padding + chartWidth, y),
                Stroke = _gridLineBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(gridLine);
            
            // Y-axis labels
            double value = maxValue - (i * (maxValue - minValue) / 5);
            var label = new TextBlock
            {
                Text = value.ToString("F1"),
                Foreground = _axisFontBrush,
                FontSize = 10
            };
            Canvas.SetLeft(label, padding - 30);
            Canvas.SetTop(label, y - 10);
            _canvas.Children.Add(label);
        }
        
        // Vertical grid lines
        for (int i = 0; i <= 10; i++)
        {
            double x = padding + (i * chartWidth / 10);
            var gridLine = new Line
            {
                StartPoint = new Avalonia.Point(x, padding),
                EndPoint = new Avalonia.Point(x, padding + chartHeight),
                Stroke = _gridLineBrush,
                StrokeThickness = 1
            };
            _canvas.Children.Add(gridLine);
            
            // X-axis labels (every other line)
            if (i % 2 == 0)
            {
                var label = new TextBlock
                {
                    Text = (i * 10).ToString(),
                    Foreground = _axisFontBrush,
                    FontSize = 10
                };
                Canvas.SetLeft(label, x - 10);
                Canvas.SetTop(label, padding + chartHeight + 5);
                _canvas.Children.Add(label);
            }
        }
    }
    
    private void DrawNoDataMessage()
    {
        if (_canvas == null) return;
        
        var message = new TextBlock
        {
            Text = "No data available",
            Foreground = new SolidColorBrush(Color.Parse("#9E9E9E")),
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        
        Canvas.SetLeft(message, (_canvas.Bounds.Width / 2) - 50);
        Canvas.SetTop(message, (_canvas.Bounds.Height / 2) - 10);
        _canvas.Children.Add(message);
    }
}
