using System;

namespace Gymnasium.Spaces;

/// <summary>
/// Box space: n-dimensional continuous space with lower and upper bounds.
/// </summary>
public class Box : Space<float[]>
{
    public float[] Low { get; }
    public float[] High { get; }
    public int Dimension { get; }
    private readonly Random _rng = new();

    public Box(float[] low, float[] high)
    {
        if (low.Length != high.Length)
            throw new ArgumentException("Low and high must have the same length.");
        Low = low;
        High = high;
        Dimension = low.Length;
    }

    public override float[] Sample()
    {
        var sample = new float[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            sample[i] = (float)(_rng.NextDouble() * (High[i] - Low[i]) + Low[i]);
        }
        return sample;
    }

    public override bool Contains(float[] x)
    {
        if (x.Length != Dimension) return false;
        for (int i = 0; i < Dimension; i++)
        {
            if (x[i] < Low[i] || x[i] > High[i]) return false;
        }
        return true;
    }
}
