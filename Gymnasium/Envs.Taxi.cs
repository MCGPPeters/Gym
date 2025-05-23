using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// Taxi environment: toy text gridworld taxi problem.
/// </summary>
public class Taxi : Env<int, int>
{
    public Discrete ActionSpace { get; } = new(6);
    public Discrete ObservationSpace { get; } = new(500);
    private int _state;
    private readonly Random _rng = new();
    private int _steps;

    public override int Reset()
    {
        _state = _rng.Next(500);
        _steps = 0;
        return _state;
    }

    public override (int state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        // This is a stub. Full Taxi logic is complex; see OpenAI Gym for reference.
        _steps++;
        bool done = _steps >= 200;
        double reward = -1.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        Console.WriteLine($"State: {_state}");
    }

    public override void Close() { }
}
