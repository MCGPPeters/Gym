using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gymnasium.UI.Views;

public partial class EnvironmentRenderView : UserControl
{
    private Canvas? _canvas;
    public EnvironmentRenderView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (s, e) => _canvas = this.FindControl<Canvas>("RenderCanvas");
    }

    public void RenderCartPole(float x, float theta)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        // Simple cart-pole visualization
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double cartX = width / 2 + x * 100;
        double cartY = height * 0.75;
        double poleLen = 100;
        double poleX2 = cartX + poleLen * Math.Sin(theta);
        double poleY2 = cartY - poleLen * Math.Cos(theta);
        var cart = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = 60, Height = 30, Fill = Brushes.Gray,
            [Canvas.LeftProperty] = cartX - 30,
            [Canvas.TopProperty] = cartY - 15
        };
        var pole = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(cartX, cartY),
            EndPoint = new Avalonia.Point(poleX2, poleY2),
            Stroke = Brushes.Red, StrokeThickness = 6
        };
        _canvas.Children.Add(cart);
        _canvas.Children.Add(pole);
    }

    public void RenderMountainCar(float position, float velocity)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        // MountainCar: position in [-1.2, 0.6], velocity in [-0.07, 0.07]
        double minX = -1.2, maxX = 0.6;
        double carY = height * 0.7;
        double carWidth = 40, carHeight = 20;
        // Map position to canvas X
        double carX = (position - minX) / (maxX - minX) * (width - carWidth);
        // Draw ground (hill as a simple sine wave)
        var ground = new Avalonia.Controls.Shapes.Polyline
        {
            Stroke = Brushes.ForestGreen,
            StrokeThickness = 3
        };
        var points = new Avalonia.Collections.AvaloniaList<Avalonia.Point>();
        for (int i = 0; i <= 100; i++)
        {
            double px = i / 100.0 * width;
            double envX = minX + (maxX - minX) * (i / 100.0);
            double py = carY + 40 * Math.Sin(3 * envX);
            points.Add(new Avalonia.Point(px, py));
        }
        ground.Points = points;
        _canvas.Children.Add(ground);
        // Draw car
        var car = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = carWidth,
            Height = carHeight,
            Fill = Brushes.Blue,
            [Canvas.LeftProperty] = carX,
            [Canvas.TopProperty] = carY - carHeight - 10
        };
        _canvas.Children.Add(car);
        // Optionally, draw a velocity arrow
        double arrowLen = velocity * 1000; // scale for visibility
        var arrow = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX + carWidth / 2, carY - carHeight),
            EndPoint = new Avalonia.Point(carX + carWidth / 2 + arrowLen, carY - carHeight - 10),
            Stroke = Brushes.Orange,
            StrokeThickness = 3
        };
        _canvas.Children.Add(arrow);
    }

    public void RenderAcrobot(float[] state)
    {
        if (_canvas == null || state == null || state.Length < 6) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double originX = width / 2;
        double originY = height * 0.7;
        double link1 = 80, link2 = 80;
        double theta1 = Math.Atan2(state[1], state[0]);
        double theta2 = Math.Atan2(state[3], state[2]);
        double x1 = originX + link1 * Math.Sin(theta1);
        double y1 = originY - link1 * Math.Cos(theta1);
        double x2 = x1 + link2 * Math.Sin(theta1 + theta2);
        double y2 = y1 - link2 * Math.Cos(theta1 + theta2);
        var line1 = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(originX, originY),
            EndPoint = new Avalonia.Point(x1, y1),
            Stroke = Brushes.DarkBlue, StrokeThickness = 6
        };
        var line2 = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(x1, y1),
            EndPoint = new Avalonia.Point(x2, y2),
            Stroke = Brushes.CadetBlue, StrokeThickness = 6
        };
        var joint1 = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 12, Height = 12, Fill = Brushes.Black,
            [Canvas.LeftProperty] = originX - 6,
            [Canvas.TopProperty] = originY - 6
        };
        var joint2 = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 12, Height = 12, Fill = Brushes.Black,
            [Canvas.LeftProperty] = x1 - 6,
            [Canvas.TopProperty] = y1 - 6
        };
        var tip = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 10, Height = 10, Fill = Brushes.Red,
            [Canvas.LeftProperty] = x2 - 5,
            [Canvas.TopProperty] = y2 - 5
        };
        _canvas.Children.Add(line1);
        _canvas.Children.Add(line2);
        _canvas.Children.Add(joint1);
        _canvas.Children.Add(joint2);
        _canvas.Children.Add(tip);
    }

    public void RenderPendulum(float[] state)
    {
        if (_canvas == null || state == null || state.Length < 3) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double originX = width / 2;
        double originY = height * 0.7;
        double length = 100;
        double theta = Math.Atan2(state[1], state[0]);
        double x2 = originX + length * Math.Sin(theta);
        double y2 = originY - length * Math.Cos(theta);
        var rod = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(originX, originY),
            EndPoint = new Avalonia.Point(x2, y2),
            Stroke = Brushes.DarkRed, StrokeThickness = 6
        };
        var pivot = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 14, Height = 14, Fill = Brushes.Black,
            [Canvas.LeftProperty] = originX - 7,
            [Canvas.TopProperty] = originY - 7
        };
        var tip = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 12, Height = 12, Fill = Brushes.Orange,
            [Canvas.LeftProperty] = x2 - 6,
            [Canvas.TopProperty] = y2 - 6
        };
        _canvas.Children.Add(rod);
        _canvas.Children.Add(pivot);
        _canvas.Children.Add(tip);
    }

    public void RenderFrozenLake(int state, int nrow = 4, int ncol = 4, HashSet<int>? holes = null, int goal = -1)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double cellW = width / ncol;
        double cellH = height / nrow;
        for (int r = 0; r < nrow; r++)
        for (int c = 0; c < ncol; c++)
        {
            int idx = r * ncol + c;
            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = cellW - 2, Height = cellH - 2,
                [Canvas.LeftProperty] = c * cellW + 1,
                [Canvas.TopProperty] = r * cellH + 1,
                Fill = (holes != null && holes.Contains(idx)) ? Brushes.DarkBlue : Brushes.LightBlue
            };
            if (goal == idx) rect.Fill = Brushes.Gold;
            if (state == idx) rect.Fill = Brushes.Red;
            _canvas.Children.Add(rect);
        }
    }

    public void RenderTaxi(int state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        int nrow = 5, ncol = 5;
        double cellW = width / ncol;
        double cellH = height / nrow;
        for (int r = 0; r < nrow; r++)
        for (int c = 0; c < ncol; c++)
        {
            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = cellW - 2, Height = cellH - 2,
                [Canvas.LeftProperty] = c * cellW + 1,
                [Canvas.TopProperty] = r * cellH + 1,
                Fill = Brushes.LightYellow
            };
            _canvas.Children.Add(rect);
        }
        // Taxi position (approximate)
        int taxiRow = (state / 25) / ncol;
        int taxiCol = (state / 25) % ncol;
        var taxi = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = cellW * 0.7, Height = cellH * 0.7, Fill = Brushes.Yellow,
            [Canvas.LeftProperty] = taxiCol * cellW + cellW * 0.15,
            [Canvas.TopProperty] = taxiRow * cellH + cellH * 0.15
        };
        _canvas.Children.Add(taxi);
    }

    public void RenderCliffWalking(int state, int nrow = 4, int ncol = 12, HashSet<int>? cliff = null, int goal = 47)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double cellW = width / ncol;
        double cellH = height / nrow;
        for (int r = 0; r < nrow; r++)
        for (int c = 0; c < ncol; c++)
        {
            int idx = r * ncol + c;
            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = cellW - 2, Height = cellH - 2,
                [Canvas.LeftProperty] = c * cellW + 1,
                [Canvas.TopProperty] = r * cellH + 1,
                Fill = (cliff != null && cliff.Contains(idx)) ? Brushes.Black : Brushes.LightGreen
            };
            if (goal == idx) rect.Fill = Brushes.Gold;
            if (state == idx) rect.Fill = Brushes.Red;
            _canvas.Children.Add(rect);
        }
    }

    public void RenderBlackjack((int, int, bool) state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = $"Player: {state.Item1}\nDealer: {state.Item2}\nUsable Ace: {state.Item3}",
            Foreground = Brushes.White,
            FontSize = 32,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }

    public void RenderLunarLander(float[] state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = "LunarLander (stub):\nNo physics visualization",
            Foreground = Brushes.LightGray,
            FontSize = 28,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }
    public void RenderBipedalWalker(float[] state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = "BipedalWalker (stub):\nNo physics visualization",
            Foreground = Brushes.LightGray,
            FontSize = 28,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }
    public void RenderCarRacing(float[] state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = "CarRacing (stub):\nNo physics visualization",
            Foreground = Brushes.LightGray,
            FontSize = 28,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }
    public void RenderAtariStub(int[] state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = "AtariStub: [image data]",
            Foreground = Brushes.LightGray,
            FontSize = 28,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }
    public void RenderMujocoStub(float[] state)
    {
        if (_canvas == null) return;
        _canvas.Children.Clear();
        var text = new Avalonia.Controls.TextBlock
        {
            Text = "MujocoStub: [state data]",
            Foreground = Brushes.LightGray,
            FontSize = 28,
            [Canvas.LeftProperty] = 20.0,
            [Canvas.TopProperty] = 40.0
        };
        _canvas.Children.Add(text);
    }
}
