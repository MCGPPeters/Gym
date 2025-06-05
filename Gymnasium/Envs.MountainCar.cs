using System;
using System.Collections.Generic;
using Gymnasium.Spaces;
using Gymnasium;

namespace Gymnasium.Envs;

/// <summary>
/// MountainCar environment: classic control problem with a car on a hill.
/// </summary>
public class MountainCar : Env<(float, float), int>
{
    public Discrete ActionSpace { get; } = new(3);
    public Box ObservationSpace { get; } = new(
        new float[] { -1.2f, -0.07f },
        new float[] { 0.6f, 0.07f }
    );

    private (float, float) _state;
    private readonly Random _rng = new();
    private int _steps;

    public override (float, float) Reset()
    {
        _state = (
            (float)(_rng.NextDouble() * 0.2 - 0.6), // position in [-0.6, -0.4]
            0.0f
        );
        _steps = 0;
        return _state;
    }

    public override ((float, float) state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        var (position, velocity) = _state;
        float force = action - 1; // -1, 0, 1
        velocity += 0.001f * force - 0.0025f * (float)Math.Cos(3 * position);
        velocity = Math.Clamp(velocity, -0.07f, 0.07f);
        position += velocity;
        if (position < -1.2f)
        {
            position = -1.2f;
            velocity = 0.0f;
        }
        bool done = position >= 0.5f;
        double reward = done ? 0.0 : -1.0;
        _state = (position, velocity);
        _steps++;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("MountainCar");
        var (pos, vel) = _state;
        int carPos = (int)Math.Round((pos + 1.2) / 1.8 * 20);
        carPos = Math.Clamp(carPos, 0, 20);
        string track = new string('-', 21);
        string line = track.Substring(0, carPos) + 'C';
        if (carPos + 1 < track.Length)
            line += track.Substring(carPos + 1);
        Console.WriteLine(line);
        Console.WriteLine($"Vel: {vel:F3}");
    }

    public override void Close() { }
}
