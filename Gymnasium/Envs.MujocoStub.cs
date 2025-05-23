using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// MujocoStub environment: stub for MuJoCo simulation environments.
/// </summary>
public class MujocoStub : Env<float[], float[]>
{
    public Box ActionSpace { get; } = new(new float[] { -1, -1 }, new float[] { 1, 1 });
    public Box ObservationSpace { get; } = new(new float[17], new float[17]);
    private float[] _state = new float[17];
    private int _steps;
    public override float[] Reset() { _steps = 0; return _state; }
    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(float[] action) { _steps++; return (_state, 0.0, _steps >= 1000, new Dictionary<string, object>()); }
    public override void Render(string mode = "human") => Console.WriteLine("MujocoStub: [state data]");
    public override void Close() { }
}
