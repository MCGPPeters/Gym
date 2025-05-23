using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gymnasium;

/// <summary>
/// Vectorized environment for parallel execution of multiple environments.
/// </summary>
public class VectorEnv<TState, TAction>
{
    private readonly List<Env<TState, TAction>> _envs;
    public int NumEnvs => _envs.Count;

    public VectorEnv(List<Env<TState, TAction>> envs)
    {
        _envs = envs;
    }

    public List<TState> Reset()
    {
        var states = new List<TState>();
        foreach (var env in _envs)
            states.Add(env.Reset());
        return states;
    }

    public List<(TState state, double reward, bool done, IDictionary<string, object> info)> Step(List<TAction> actions)
    {
        var results = new List<(TState, double, bool, IDictionary<string, object>)>();
        for (int i = 0; i < _envs.Count; i++)
            results.Add(_envs[i].Step(actions[i]));
        return results;
    }
}
