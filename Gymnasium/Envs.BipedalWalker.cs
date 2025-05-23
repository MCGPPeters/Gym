using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// BipedalWalker environment: Box2D-based bipedal walking simulation (stub).
/// </summary>
public class BipedalWalker : Env<float[], float[]>
{
    public Box ActionSpace { get; } = new(new float[] { -1, -1, -1, -1 }, new float[] { 1, 1, 1, 1 });
    public Box ObservationSpace { get; } = new(new float[24], new float[24]);
    private float[] _state = new float[24];
    private readonly Random _rng = new();
    private int _steps;

    public override float[] Reset()
    {
        for (int i = 0; i < 24; i++)
            _state[i] = (float)(_rng.NextDouble() * 2 - 1);
        _steps = 0;
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(float[] action)
    {
        // Stub: Full Box2D physics not implemented
        _steps++;
        bool done = _steps >= 1600;
        double reward = 0.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human") => Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    public override void Close() { }
}
