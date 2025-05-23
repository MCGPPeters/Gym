using System;

namespace Gymnasium.UI.Models;

public interface IAgentPlugin
{
    string Name { get; }
    string Description { get; }
    object CreateAgent(object env, object? config = null);
    /// <summary>
    /// If the agent supports reporting per-episode or per-step loss, returns a function that retrieves the current loss value.
    /// Otherwise, returns null.
    /// </summary>
    Func<double>? GetLossFetcher(object agent) => null;
}
