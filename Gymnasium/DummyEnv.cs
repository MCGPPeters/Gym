using System;
using System.Collections.Generic;

namespace Gymnasium;

/// <summary>
/// DummyEnv: simple demonstration environment for testing.
/// </summary>
public class DummyEnv : Env<int, int>
{
    private int _state;

    public override int Reset()
    {
        _state = 0;
        return _state;
    }

    public override (int state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        _state += action;
        double reward = _state;
        bool done = _state >= 10;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        Console.WriteLine($"State: {_state}");
    }

    public override void Close() { }
}
