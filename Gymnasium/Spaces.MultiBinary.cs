using System;
using System.Collections.Generic;

namespace Gymnasium.Spaces;

/// <summary>
/// MultiBinary space: n-dimensional binary space.
/// </summary>
public class MultiBinary : Space<bool[]>
{
    public int N { get; }
    public override int[] Shape => new int[] { N };
    private readonly Random _rng = new();

    public MultiBinary(int n)
    {
        N = n;
    }

    public override bool[] Sample()
    {
        var sample = new bool[N];
        for (int i = 0; i < N; i++)
        {
            sample[i] = _rng.Next(2) == 1;
        }
        return sample;
    }

    public override bool Contains(bool[] x)
    {
        return x.Length == N;
    }
}
