using System;
using System.Collections.Generic;

namespace Gymnasium.Spaces;

/// <summary>
/// Dict space: composite space with named subspaces.
/// </summary>
public class Dict : Space<Dictionary<string, object>>
{
    public Dictionary<string, Space<object>> Spaces { get; }
    public override int[] Shape => new int[] { Spaces.Count }; // Dictionary shape is number of keys
    private readonly Random _rng = new();

    public Dict(Dictionary<string, Space<object>> spaces)
    {
        Spaces = spaces;
    }

    public override Dictionary<string, object> Sample()
    {
        var sample = new Dictionary<string, object>();
        foreach (var kvp in Spaces)
        {
            sample[kvp.Key] = kvp.Value.Sample();
        }
        return sample;
    }

    public override bool Contains(Dictionary<string, object> x)
    {
        foreach (var kvp in Spaces)
        {
            if (!x.ContainsKey(kvp.Key) || !kvp.Value.Contains(x[kvp.Key]))
                return false;
        }
        return true;
    }
}
