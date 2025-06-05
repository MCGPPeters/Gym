using System;
using System.Collections.Generic;
using Gymnasium.Spaces;
using Gymnasium;

namespace Gymnasium.Envs;

/// <summary>
/// AtariStub environment: stub for Atari game environments.
/// </summary>
public class AtariStub : Env<int[], int>
{
    public Discrete ActionSpace { get; } = new(18);
    public Box ObservationSpace { get; } = new(new float[210 * 160 * 3], new float[210 * 160 * 3]);
    private int[] _state = new int[210 * 160 * 3];
    private int _steps;
    public override int[] Reset() { _steps = 0; return _state; }
    public override (int[] state, double reward, bool done, IDictionary<string, object> info) Step(int action) { _steps++; return (_state, 0.0, _steps >= 1000, new Dictionary<string, object>()); }
    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("AtariStub");
        Console.WriteLine("[image data]");
    }
    public override void Close() { }
}
