using System;
using System.Collections.Generic;

namespace Gymnasium;

/// <summary>
/// Environment registry for registering and creating environments by id.
/// Similar to gymnasium's registration and gym.make functionality.
/// </summary>
public static class EnvRegistry
{
    private static readonly Dictionary<string, Func<object>> _registry = new();

    public static void Register(string id, Func<object> factory)
    {
        _registry[id] = factory;
    }

    public static object Make(string id)
    {
        if (!_registry.TryGetValue(id, out var factory))
            throw new ArgumentException($"No environment registered with id '{id}'");
        return factory();
    }

    public static Env<TState, TAction> Make<TState, TAction>(string id)
    {
        var env = EnvRegistry.Make(id);
        if (env is Env<TState, TAction> typedEnv)
            return typedEnv;
        throw new InvalidCastException($"Registered environment '{id}' does not match requested types.");
    }

    public static IEnumerable<string> List() => _registry.Keys;
}
