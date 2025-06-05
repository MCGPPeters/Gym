using System;
using System.Collections.Generic;
using Gymnasium.Spaces;
using Gymnasium;

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

    private static readonly (int row, int col)[] _locs =
    {
        (0,0), (0,4), (4,0), (4,3)
    };

    private static readonly string[] _map =
    {
        "+---------+",
        "|R: | : :G|",
        "| : | : : |",
        "| : : : : |",
        "| | : | : |",
        "|Y| : |B: |",
        "+---------+"
    };

    private static void DecodeState(int state, out int taxiRow, out int taxiCol, out int passIdx, out int destIdx)
    {
        destIdx = state % 4;
        state /= 4;
        passIdx = state % 5;
        state /= 5;
        taxiCol = state % 5;
        taxiRow = state / 5;
    }

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
        ConsoleRenderer.RenderHeader("Taxi");

        var grid = new char[_map.Length, _map[0].Length];
        for (int r = 0; r < _map.Length; r++)
        for (int c = 0; c < _map[r].Length; c++)
            grid[r, c] = _map[r][c];

        DecodeState(_state, out int taxiRow, out int taxiCol, out int passIdx, out int destIdx);

        var (dr, dc) = _locs[destIdx];
        grid[1 + dr, 2 * dc + 1] = _map[1 + dr][2 * dc + 1];

        if (passIdx < 4)
        {
            var (pr, pc) = _locs[passIdx];
            grid[1 + pr, 2 * pc + 1] = 'P';
        }

        grid[1 + taxiRow, 2 * taxiCol + 1] = 'T';

        ConsoleRenderer.RenderGrid(grid, spaced: false);
    }

    public override void Close() { }
}
