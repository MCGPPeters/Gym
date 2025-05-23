using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// Blackjack environment: toy text card game problem.
/// </summary>
public class Blackjack : Env<(int, int, bool), int>
{
    public Discrete ActionSpace { get; } = new(2); // 0: Stick, 1: Hit
    public Discrete ObservationSpace { get; } = new(32 * 11 * 2);
    private (int, int, bool) _state;
    private readonly Random _rng = new();
    private int _steps;
    private int _playerSum, _dealerCard;
    private bool _usableAce;
    private bool _done;

    public override (int, int, bool) Reset()
    {
        _playerSum = _rng.Next(12, 22);
        _dealerCard = _rng.Next(1, 11);
        _usableAce = _rng.Next(2) == 1;
        _state = (_playerSum, _dealerCard, _usableAce);
        _done = false;
        _steps = 0;
        return _state;
    }

    public override ((int, int, bool) state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        // This is a stub. Full Blackjack logic is complex; see OpenAI Gym for reference.
        _steps++;
        _done = _steps >= 10;
        double reward = _done ? 1.0 : 0.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, _done, info);
    }

    public override void Render(string mode = "human")
    {
        Console.WriteLine($"State: {_state}");
    }

    public override void Close() { }
}
