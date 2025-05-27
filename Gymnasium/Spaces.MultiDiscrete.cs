using System;
using System.Collections.Generic;

namespace Gymnasium.Spaces;

/// <summary>
/// MultiDiscrete space: multi-dimensional discrete space.
/// </summary>
public class MultiDiscrete : Space<int[]>
{
    public int[] Nvec { get; }
    public override int[] Shape => new int[] { Nvec.Length };
    private readonly Random _rng = new();

    public MultiDiscrete(int[] nvec)
    {
        Nvec = nvec;
    }

    public override int[] Sample()
    {
        var sample = new int[Nvec.Length];
        for (int i = 0; i < Nvec.Length; i++)
        {
            sample[i] = _rng.Next(Nvec[i]);
        }
        return sample;
    }

    public override bool Contains(int[] x)
    {
        if (x.Length != Nvec.Length) return false;
        for (int i = 0; i < Nvec.Length; i++)
        {
            if (x[i] < 0 || x[i] >= Nvec[i]) return false;
        }
        return true;
    }
}
