using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// LunarLander environment: Box2D-based lunar landing simulation (stub).
/// </summary>
public class LunarLander : Env<float[], int>
{
    public Discrete ActionSpace { get; } = new(4);
    public Box ObservationSpace { get; } = new(new float[] { -1, -1, -1, -1, -1, -1, 0, 0 }, new float[] { 1, 1, 1, 1, 1, 1, 1, 1 });
    private float[] _state = new float[8];
    private readonly Random _rng = new();
    private int _steps;

    public override float[] Reset()
    {
        for (int i = 0; i < 8; i++)
            _state[i] = (float)(_rng.NextDouble() * 2 - 1);
        _steps = 0;
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        // Stub: Full Box2D physics not implemented
        _steps++;
        bool done = _steps >= 1000;
        double reward = 0.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human") => Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    public override void Close() { }
}
