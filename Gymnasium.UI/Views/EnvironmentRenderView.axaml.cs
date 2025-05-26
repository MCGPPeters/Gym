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
    }    public void RenderLunarLander(float[] state)
    {
        if (_canvas == null || state == null || state.Length < 8) return;
        _canvas.Children.Clear();
        
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        
        // Extract state: [norm_x, norm_y, vel_x, vel_y, angle, angular_vel, leg1_contact, leg2_contact]
        float normX = state[0]; // Normalized X position [-1, 1]
        float normY = state[1]; // Normalized Y position [-1, 1]
        float angle = state[4]; // Lander angle
        bool leg1Contact = state[6] > 0.5f;
        bool leg2Contact = state[7] > 0.5f;
        
        // Convert to canvas coordinates
        double landerX = width / 2 + normX * width / 3; // Center with some margin
        double landerY = height * 0.8 - normY * height / 3; // Flip Y axis, ground at bottom
        
        // Draw landing pad
        var landingPad = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = 80, Height = 10,
            Fill = Brushes.Green,
            [Canvas.LeftProperty] = width / 2 - 40,
            [Canvas.TopProperty] = height * 0.8
        };
        _canvas.Children.Add(landingPad);
        
        // Draw ground
        var ground = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(0, height * 0.8 + 5),
            EndPoint = new Avalonia.Point(width, height * 0.8 + 5),
            Stroke = Brushes.Brown,
            StrokeThickness = 5
        };
        _canvas.Children.Add(ground);
        
        // Draw lander body (diamond shape)
        var lander = new Avalonia.Controls.Shapes.Polygon
        {
            Fill = Brushes.Silver,
            Stroke = Brushes.DarkGray,
            StrokeThickness = 2
        };
        
        var points = new Avalonia.Collections.AvaloniaList<Avalonia.Point>();
        double landerSize = 20;
        double cosA = Math.Cos(angle);
        double sinA = Math.Sin(angle);
        
        // Diamond shape rotated by angle
        points.Add(new Avalonia.Point(landerX + landerSize * sinA, landerY - landerSize * cosA)); // Top
        points.Add(new Avalonia.Point(landerX + landerSize * cosA, landerY + landerSize * sinA)); // Right
        points.Add(new Avalonia.Point(landerX - landerSize * sinA, landerY + landerSize * cosA)); // Bottom
        points.Add(new Avalonia.Point(landerX - landerSize * cosA, landerY - landerSize * sinA)); // Left
        
        lander.Points = points;
        _canvas.Children.Add(lander);
        
        // Draw legs
        double legLength = 15;
        var leftLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(landerX - 10, landerY + 5),
            EndPoint = new Avalonia.Point(landerX - 15, landerY + 5 + legLength),
            Stroke = leg1Contact ? Brushes.Red : Brushes.Gray,
            StrokeThickness = 3
        };
        
        var rightLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(landerX + 10, landerY + 5),
            EndPoint = new Avalonia.Point(landerX + 15, landerY + 5 + legLength),
            Stroke = leg2Contact ? Brushes.Red : Brushes.Gray,
            StrokeThickness = 3
        };
        
        _canvas.Children.Add(leftLeg);
        _canvas.Children.Add(rightLeg);
        
        // Draw velocity vector
        double velScale = 50;
        if (state.Length >= 4)
        {
            var velVector = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(landerX, landerY),
                EndPoint = new Avalonia.Point(landerX + state[2] * velScale, landerY - state[3] * velScale),
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };
            _canvas.Children.Add(velVector);
        }
        
        // Draw state info
        var stateText = new Avalonia.Controls.TextBlock
        {
            Text = $"Pos: ({normX:F2}, {normY:F2})\nAngle: {angle:F2}\nContact: L{(leg1Contact ? "✓" : "✗")} R{(leg2Contact ? "✓" : "✗")}",
            Foreground = Brushes.White,
            FontSize = 12,
            [Canvas.LeftProperty] = 10.0,
            [Canvas.TopProperty] = 10.0
        };
        _canvas.Children.Add(stateText);
    }    public void RenderBipedalWalker(float[] state)
    {
        if (_canvas == null || state == null || state.Length < 24) return;
        _canvas.Children.Clear();
        
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        double groundY = height * 0.8;
        
        // Extract key state elements: hull position and angles
        float hullAngle = state[0];
        float hullVelX = state[2];
        float hullPosX = state[4];
        float hullPosY = state[5];
        float leg1Angle = state[6];
        float leg2Angle = state[10];
        float lowerLeg1Angle = state[14];
        float lowerLeg2Angle = state.Length > 18 ? state[18] : 0;
        
        // Convert to canvas coordinates
        double centerX = width / 2 + hullPosX * 50; // Scale and center
        double centerY = groundY - hullPosY * 50; // Flip Y, relative to ground
        
        // Draw ground
        var ground = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(0, groundY),
            EndPoint = new Avalonia.Point(width, groundY),
            Stroke = Brushes.Brown,
            StrokeThickness = 5
        };
        _canvas.Children.Add(ground);
        
        // Draw hull (torso)
        var hull = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = 30, Height = 20,
            Fill = Brushes.Blue,
            [Canvas.LeftProperty] = centerX - 15,
            [Canvas.TopProperty] = centerY - 10
        };
        
        // Rotate hull based on angle (simplified)
        hull.RenderTransform = new Avalonia.Media.RotateTransform(hullAngle * 180 / Math.PI, 15, 10);
        _canvas.Children.Add(hull);
        
        // Draw legs
        double legLength = 40;
        double lowerLegLength = 35;
        
        // Left leg (leg1)
        double leg1X = centerX - 10;
        double leg1Y = centerY + 10;
        double leg1EndX = leg1X + legLength * Math.Sin(leg1Angle);
        double leg1EndY = leg1Y + legLength * Math.Cos(leg1Angle);
        
        var leftUpperLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(leg1X, leg1Y),
            EndPoint = new Avalonia.Point(leg1EndX, leg1EndY),
            Stroke = Brushes.DarkBlue,
            StrokeThickness = 4
        };
        _canvas.Children.Add(leftUpperLeg);
        
        // Left lower leg
        double lowerLeg1EndX = leg1EndX + lowerLegLength * Math.Sin(leg1Angle + lowerLeg1Angle);
        double lowerLeg1EndY = leg1EndY + lowerLegLength * Math.Cos(leg1Angle + lowerLeg1Angle);
        
        var leftLowerLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(leg1EndX, leg1EndY),
            EndPoint = new Avalonia.Point(lowerLeg1EndX, lowerLeg1EndY),
            Stroke = Brushes.CadetBlue,
            StrokeThickness = 3
        };
        _canvas.Children.Add(leftLowerLeg);
        
        // Right leg (leg2)
        double leg2X = centerX + 10;
        double leg2Y = centerY + 10;
        double leg2EndX = leg2X + legLength * Math.Sin(leg2Angle);
        double leg2EndY = leg2Y + legLength * Math.Cos(leg2Angle);
        
        var rightUpperLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(leg2X, leg2Y),
            EndPoint = new Avalonia.Point(leg2EndX, leg2EndY),
            Stroke = Brushes.DarkBlue,
            StrokeThickness = 4
        };
        _canvas.Children.Add(rightUpperLeg);
        
        // Right lower leg
        double lowerLeg2EndX = leg2EndX + lowerLegLength * Math.Sin(leg2Angle + lowerLeg2Angle);
        double lowerLeg2EndY = leg2EndY + lowerLegLength * Math.Cos(leg2Angle + lowerLeg2Angle);
        
        var rightLowerLeg = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(leg2EndX, leg2EndY),
            EndPoint = new Avalonia.Point(lowerLeg2EndX, lowerLeg2EndY),
            Stroke = Brushes.CadetBlue,
            StrokeThickness = 3
        };
        _canvas.Children.Add(rightLowerLeg);
        
        // Draw joints
        var hipLeft = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8, Fill = Brushes.Red,
            [Canvas.LeftProperty] = leg1X - 4,
            [Canvas.TopProperty] = leg1Y - 4
        };
        
        var kneeLeft = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 6, Height = 6, Fill = Brushes.Orange,
            [Canvas.LeftProperty] = leg1EndX - 3,
            [Canvas.TopProperty] = leg1EndY - 3
        };
        
        var hipRight = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8, Fill = Brushes.Red,
            [Canvas.LeftProperty] = leg2X - 4,
            [Canvas.TopProperty] = leg2Y - 4
        };
        
        var kneeRight = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 6, Height = 6, Fill = Brushes.Orange,
            [Canvas.LeftProperty] = leg2EndX - 3,
            [Canvas.TopProperty] = leg2EndY - 3
        };
        
        _canvas.Children.Add(hipLeft);
        _canvas.Children.Add(kneeLeft);
        _canvas.Children.Add(hipRight);
        _canvas.Children.Add(kneeRight);
        
        // Draw feet
        var footLeft = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8, Fill = Brushes.Black,
            [Canvas.LeftProperty] = lowerLeg1EndX - 4,
            [Canvas.TopProperty] = lowerLeg1EndY - 4
        };
        
        var footRight = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8, Fill = Brushes.Black,
            [Canvas.LeftProperty] = lowerLeg2EndX - 4,
            [Canvas.TopProperty] = lowerLeg2EndY - 4
        };
        
        _canvas.Children.Add(footLeft);
        _canvas.Children.Add(footRight);
        
        // Draw velocity vector
        var velVector = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(centerX, centerY),
            EndPoint = new Avalonia.Point(centerX + hullVelX * 20, centerY),
            Stroke = Brushes.Yellow,
            StrokeThickness = 3
        };
        _canvas.Children.Add(velVector);
        
        // Draw state info
        var stateText = new Avalonia.Controls.TextBlock
        {
            Text = $"Hull: ({hullPosX:F2}, {hullPosY:F2})\nAngle: {hullAngle:F2}\nVel X: {hullVelX:F2}",
            Foreground = Brushes.White,
            FontSize = 12,
            [Canvas.LeftProperty] = 10.0,
            [Canvas.TopProperty] = 10.0
        };
        _canvas.Children.Add(stateText);
    }    public void RenderCarRacing(float[] state)
    {
        if (_canvas == null || state == null || state.Length < 8) return;
        _canvas.Children.Clear();
        
        double width = _canvas.Bounds.Width;
        double height = _canvas.Bounds.Height;
        
        // Extract state: [norm_x, norm_y, angle, vel_x, vel_y, angular_vel, wheel_angle, speed]
        float normX = state[0]; // Normalized X position [-1, 1]
        float normY = state[1]; // Normalized Y position [-1, 1]
        float angle = state[2] * (float)Math.PI; // Convert back from normalized angle
        float speed = state[7]; // Normalized speed
        float wheelAngle = state[6]; // Normalized wheel angle
        
        // Convert to canvas coordinates (top-down view)
        double carX = width / 2 + normX * width / 3;
        double carY = height / 2 + normY * height / 3;
        
        // Draw track boundaries
        var trackOuter = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = width * 0.9, Height = height * 0.9,
            Stroke = Brushes.White,
            StrokeThickness = 3,
            Fill = Brushes.Transparent,
            [Canvas.LeftProperty] = width * 0.05,
            [Canvas.TopProperty] = height * 0.05
        };
        _canvas.Children.Add(trackOuter);
        
        var trackInner = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = width * 0.6, Height = height * 0.6,
            Stroke = Brushes.White,
            StrokeThickness = 3,
            Fill = Brushes.Transparent,
            [Canvas.LeftProperty] = width * 0.2,
            [Canvas.TopProperty] = height * 0.2
        };
        _canvas.Children.Add(trackInner);
        
        // Draw car body
        var car = new Avalonia.Controls.Shapes.Rectangle
        {
            Width = 20, Height = 12,
            Fill = Brushes.Red,
            Stroke = Brushes.DarkRed,
            StrokeThickness = 1,
            [Canvas.LeftProperty] = carX - 10,
            [Canvas.TopProperty] = carY - 6
        };
        
        // Rotate car based on angle
        car.RenderTransform = new Avalonia.Media.RotateTransform(angle * 180 / Math.PI, 10, 6);
        _canvas.Children.Add(car);
        
        // Draw wheels (simple lines)
        double cosA = Math.Cos(angle);
        double sinA = Math.Sin(angle);
        
        // Front wheels (with steering)
        double frontWheelAngle = angle + wheelAngle * 0.5; // Limit steering visualization
        double frontCosA = Math.Cos(frontWheelAngle);
        double frontSinA = Math.Sin(frontWheelAngle);
        
        var frontLeftWheel = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX - 3 * cosA + 8 * sinA, carY - 3 * sinA - 8 * cosA),
            EndPoint = new Avalonia.Point(carX - 3 * cosA + 8 * sinA + 6 * frontCosA, carY - 3 * sinA - 8 * cosA + 6 * frontSinA),
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        var frontRightWheel = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX + 3 * cosA + 8 * sinA, carY + 3 * sinA - 8 * cosA),
            EndPoint = new Avalonia.Point(carX + 3 * cosA + 8 * sinA + 6 * frontCosA, carY + 3 * sinA - 8 * cosA + 6 * frontSinA),
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        // Rear wheels (straight)
        var rearLeftWheel = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX - 3 * cosA - 8 * sinA, carY - 3 * sinA + 8 * cosA),
            EndPoint = new Avalonia.Point(carX - 3 * cosA - 8 * sinA + 6 * cosA, carY - 3 * sinA + 8 * cosA + 6 * sinA),
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        var rearRightWheel = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX + 3 * cosA - 8 * sinA, carY + 3 * sinA + 8 * cosA),
            EndPoint = new Avalonia.Point(carX + 3 * cosA - 8 * sinA + 6 * cosA, carY + 3 * sinA + 8 * cosA + 6 * sinA),
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        _canvas.Children.Add(frontLeftWheel);
        _canvas.Children.Add(frontRightWheel);
        _canvas.Children.Add(rearLeftWheel);
        _canvas.Children.Add(rearRightWheel);
        
        // Draw velocity vector
        if (Math.Abs(speed) > 0.01f)
        {
            var velVector = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Avalonia.Point(carX, carY),
                EndPoint = new Avalonia.Point(carX + state[3] * 100, carY + state[4] * 100),
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };
            _canvas.Children.Add(velVector);
        }
        
        // Draw direction indicator
        var directionArrow = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(carX, carY),
            EndPoint = new Avalonia.Point(carX + 15 * cosA, carY + 15 * sinA),
            Stroke = Brushes.Lime,
            StrokeThickness = 3
        };
        _canvas.Children.Add(directionArrow);
        
        // Draw state info
        var stateText = new Avalonia.Controls.TextBlock
        {
            Text = $"Pos: ({normX:F2}, {normY:F2})\nAngle: {angle * 180 / Math.PI:F1}°\nSpeed: {speed:F2}\nSteering: {wheelAngle:F2}",
            Foreground = Brushes.White,
            FontSize = 12,
            [Canvas.LeftProperty] = 10.0,
            [Canvas.TopProperty] = 10.0
        };
        _canvas.Children.Add(stateText);
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
