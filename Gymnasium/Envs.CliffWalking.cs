using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// CliffWalking environment: toy text gridworld cliff navigation problem.
/// </summary>
public class CliffWalking : Env<int, int>
{
    public Discrete ActionSpace { get; } = new(4); // 0: Left, 1: Down, 2: Right, 3: Up
    public Discrete ObservationSpace { get; } = new(48);
    private int _state;
    private readonly int _nrow = 4;
    private readonly int _ncol = 12;
    private readonly int _start = 36;
    private readonly int _goal = 47;
    private readonly HashSet<int> _cliff;
    private int _steps;

    public CliffWalking()
    {
        _cliff = new HashSet<int>();
        for (int c = 1; c < 11; c++)
            _cliff.Add(36 + c);
    }

    public override int Reset()
    {
        _state = _start;
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
        bool done = _state == _goal || _cliff.Contains(_state) || _steps >= 100;
        double reward = _cliff.Contains(_state) ? -100.0 : -1.0;
        if (_state == _goal) reward = 0.0;
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
                else if (_cliff.Contains(idx))
                    Console.Write("C ");
                else if (idx == _goal)
                    Console.Write("G ");
                else
                    Console.Write(". ");
            }
            Console.WriteLine();
        }
    }

    public override void Close() { }
}
