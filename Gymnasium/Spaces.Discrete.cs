using System;

namespace Gymnasium.Spaces;

/// <summary>
/// Discrete space: a set of n discrete actions or states.
/// </summary>
public class Discrete : Space<int>
{    public int N { get; }
    public override int[] Shape { get; } = new int[] { 1 }; // Discrete space has shape [1]
    private readonly Random _rng = new();

    public Discrete(int n)
    {
        N = n;
    }

    public override int Sample() => _rng.Next(N);
    public override bool Contains(int x) => x >= 0 && x < N;
}
