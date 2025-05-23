using System;

namespace Gymnasium;

/// <summary>
/// Base class for environment wrappers, allowing composition and extension of environments.
/// </summary>
public abstract class EnvWrapper<TState, TAction> : Env<TState, TAction>
{
    protected Env<TState, TAction> Env;

    protected EnvWrapper(Env<TState, TAction> env)
    {
        Env = env;
    }

    public override TState Reset() => Env.Reset();
    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action) => Env.Step(action);
    public override void Render(string mode = "human") => Env.Render(mode);
    public override void Close() => Env.Close();
}

public class RecordEpisodeStatistics<TState, TAction> : EnvWrapper<TState, TAction>
{
    public List<double> EpisodeRewards { get; } = new();
    public List<int> EpisodeLengths { get; } = new();
    private double _currentReward = 0;
    private int _currentLength = 0;

    public RecordEpisodeStatistics(Env<TState, TAction> env) : base(env) { }

    public override TState Reset()
    {
        if (_currentLength > 0)
        {
            EpisodeRewards.Add(_currentReward);
            EpisodeLengths.Add(_currentLength);
        }
        _currentReward = 0;
        _currentLength = 0;
        return base.Reset();
    }

    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action)
    {
        var (state, reward, done, info) = base.Step(action);
        _currentReward += reward;
        _currentLength++;
        if (done)
        {
            EpisodeRewards.Add(_currentReward);
            EpisodeLengths.Add(_currentLength);
            _currentReward = 0;
            _currentLength = 0;
        }
        return (state, reward, done, info);
    }
}

public class ObservationWrapper<TState, TAction> : EnvWrapper<TState, TAction>
{
    public ObservationWrapper(Env<TState, TAction> env) : base(env) { }
    public override TState Reset() => TransformObservation(base.Reset());
    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action)
    {
        var (state, reward, done, info) = base.Step(action);
        return (TransformObservation(state), reward, done, info);
    }
    protected virtual TState TransformObservation(TState observation) => observation;
}

public class ActionWrapper<TState, TAction> : EnvWrapper<TState, TAction>
{
    public ActionWrapper(Env<TState, TAction> env) : base(env) { }
    public override TState Reset() => base.Reset();
    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action)
    {
        return base.Step(TransformAction(action));
    }
    protected virtual TAction TransformAction(TAction action) => action;
}

public class RewardWrapper<TState, TAction> : EnvWrapper<TState, TAction>
{
    public RewardWrapper(Env<TState, TAction> env) : base(env) { }
    public override TState Reset() => base.Reset();
    public override (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action)
    {
        var (state, reward, done, info) = base.Step(action);
        return (state, TransformReward(reward), done, info);
    }
    protected virtual double TransformReward(double reward) => reward;
}
