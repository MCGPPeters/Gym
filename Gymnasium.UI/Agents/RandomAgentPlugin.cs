using System;
using System.Composition;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents;

[Export(typeof(IAgentPlugin))]
public class RandomAgentPlugin : IAgentPlugin
{
    public string Name => "RandomAgent (Built-in)";
    public string Description => "Selects random actions from the environment's action space.";
    public object CreateAgent(object env, object? config = null) => new RandomAgent(env);
    public Func<double>? GetLossFetcher(object agent) => null; // RandomAgent does not support loss
}

public class RandomAgent
{
    private readonly dynamic _env;
    private readonly Random _rng = new();
    public RandomAgent(object env) { _env = env; }
    public object Act(object state) => _env.ActionSpace.Sample();
}
