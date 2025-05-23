using System;
using System.Collections.Generic;

namespace Gymnasium.Wrappers;

public class TimeLimit<TState, TAction> : EnvWrapper<TState, TAction>
{
    private readonly int _maxSteps;
    private int _steps;

    public TimeLimit(Env<TState, TAction> env, int maxSteps) : base(env)
    {
        _maxSteps = maxSteps;
        _steps = 0;
    }

    public override TState Reset()
    {
        _steps = 0;
        return base.Reset();
    }

    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action)
    {
        var (state, reward, done, info) = base.Step(action);
        _steps++;
        if (_steps >= _maxSteps)
        {
            done = true;
            info["TimeLimit.truncated"] = true;
        }
        return (state, reward, done, info);
    }
}
