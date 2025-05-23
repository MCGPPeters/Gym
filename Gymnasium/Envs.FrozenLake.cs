using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// FrozenLake environment: toy text gridworld navigation problem.
/// </summary>
public class FrozenLake : Env<int, int>
{
    public Discrete ActionSpace { get; } = new(4); // 0: Left, 1: Down, 2: Right, 3: Up
    public Discrete ObservationSpace { get; }
    private readonly string[] _map;
    private int _state;
    private readonly Random _rng = new();
    private int _steps;
    private readonly int _nrow;
    private readonly int _ncol;
    private readonly int _goal;
    private readonly HashSet<int> _holes;

    public FrozenLake(string[] map = null)
    {
        _map = map ?? new[] {
            "SFFF",
            "FHFH",
            "FFFH",
            "HFFG"
        };
        _nrow = _map.Length;
        _ncol = _map[0].Length;
        ObservationSpace = new Discrete(_nrow * _ncol);
        _goal = -1;
        _holes = new HashSet<int>();
        for (int r = 0; r < _nrow; r++)
        for (int c = 0; c < _ncol; c++)
        {
            char cell = _map[r][c];
            if (cell == 'G') _goal = r * _ncol + c;
            if (cell == 'H') _holes.Add(r * _ncol + c);
        }
    }

    public override int Reset()
    {
        _state = 0;
        _steps = 0;
        return _state;
    }

    public override (int state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        int row = _state / _ncol;
        int col = _state % _ncol;
        switch (action)
        {
            case 0: col = Math.Max(col - 1, 0); break; // Left
            case 1: row = Math.Min(row + 1, _nrow - 1); break; // Down
            case 2: col = Math.Min(col + 1, _ncol - 1); break; // Right
            case 3: row = Math.Max(row - 1, 0); break; // Up
        }
        _state = row * _ncol + col;
        _steps++;
        bool done = _state == _goal || _holes.Contains(_state) || _steps >= 100;
        double reward = _state == _goal ? 1.0 : 0.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        for (int r = 0; r < _nrow; r++)
        {
            for (int c = 0; c < _ncol; c++)
            {
                int idx = r * _ncol + c;
                if (idx == _state)
                    Console.Write("A ");
                else
                    Console.Write(_map[r][c] + " ");
            }
            Console.WriteLine();
        }
    }

    public override void Close() { }
}
