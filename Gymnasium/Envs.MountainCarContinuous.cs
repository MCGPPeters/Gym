using System;
using System.Collections.Generic;
using Gymnasium.Spaces;
using Gymnasium;

namespace Gymnasium.Envs;

/// <summary>
/// MountainCarContinuous environment: continuous action version of MountainCar.
/// </summary>
public class MountainCarContinuous : Env<(float, float), float>
{
    public Box ActionSpace { get; } = new(new float[] { -1.0f }, new float[] { 1.0f });
    public Box ObservationSpace { get; } = new(new float[] { -1.2f, -0.07f }, new float[] { 0.6f, 0.07f });

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

    public override ((float, float) state, double reward, bool done, IDictionary<string, object> info) Step(float action)
    {
        var (position, velocity) = _state;
        float force = Math.Clamp(action, -1.0f, 1.0f);
        velocity += force * 0.0015f - 0.0025f * (float)Math.Cos(3 * position);
        velocity = Math.Clamp(velocity, -0.07f, 0.07f);
        position += velocity;
        if (position < -1.2f)
        {
            position = -1.2f;
            velocity = 0.0f;
        }
        bool done = position >= 0.45f;
        double reward = velocity * 0.1;
        if (done && position >= 0.45f)
            reward += 100.0;
        _state = (position, velocity);
        _steps++;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("MountainCarContinuous");
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
