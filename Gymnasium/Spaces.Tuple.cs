using System;
using System.Collections.Generic;

namespace Gymnasium.Spaces;

/// <summary>
/// Tuple space: composite space with ordered subspaces.
/// </summary>
public class Tuple : Space<object[]>
{
    public List<Space<object>> Spaces { get; }
    public override int[] Shape => new int[] { Spaces.Count }; // Tuple shape is number of elements
    private readonly Random _rng = new();

    public Tuple(List<Space<object>> spaces)
    {
        Spaces = spaces;
    }

    public override object[] Sample()
    {
        var sample = new object[Spaces.Count];
        for (int i = 0; i < Spaces.Count; i++)
        {
            sample[i] = Spaces[i].Sample();
        }
        return sample;
    }

    public override bool Contains(object[] x)
    {
        if (x.Length != Spaces.Count) return false;
        for (int i = 0; i < Spaces.Count; i++)
        {
            if (!Spaces[i].Contains(x[i])) return false;
        }
        return true;
    }
}
