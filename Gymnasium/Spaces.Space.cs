using System;

namespace Gymnasium.Spaces;

/// <summary>
/// Abstract base class for all space types (Discrete, Box, etc.).
/// </summary>
public abstract class Space<T>
{
    public abstract int[] Shape { get; }
    public abstract T Sample();
    public abstract bool Contains(T x);
}
