using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using Gymnasium;
using Gymnasium.Spaces;
using Gymnasium.UI.Models;
using RevoLution.Hybrid;
using RevoLution.Neural;

namespace Gymnasium.UI.Agents;

[Export(typeof(IAgentPlugin))]
public class RevoLutionAgentPlugin : IAgentPlugin
{
    public string Name => "RevoLution Hybrid Agent";
    public string Description => "Hybrid neuroevolution and reinforcement learning agent using the RevoLution algorithm.";

    public object CreateAgent(object env, object? config = null)
    {
        if (env is null) throw new ArgumentNullException(nameof(env));
        var baseType = GetEnvBaseType(env.GetType()) ??
                       throw new ArgumentException("Environment must derive from Env<TState,TAction>");
        var args = baseType.GetGenericArguments();
        var agentType = typeof(RevoLutionAgent<,>).MakeGenericType(args);
        return Activator.CreateInstance(agentType, env)!;
    }

    private static Type? GetEnvBaseType(Type type)
    {
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Env<,>))
                return type;
            type = type.BaseType;
        }
        return null;
    }

    public Func<double>? GetLossFetcher(object agent) => null; // Algorithm does not expose loss
}

public class RevoLutionAgent<TState, TAction>
{
    private readonly Env<TState, TAction> _env;
    private readonly HybridLearner _learner = new();
    private NeuralNetwork? _network;
    private bool _trained;

    private readonly Func<TState, List<double>> _stateConverter;
    private readonly Func<List<double>, TAction> _actionConverter;
    private readonly Func<TAction> _sampleAction;

    public RevoLutionAgent(Env<TState, TAction> env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _stateConverter = CreateStateConverter();
        _actionConverter = CreateActionConverter();
        _sampleAction = CreateSampleAction();
    }

    public TAction Act(TState state)
    {
        if (!_trained)
        {
            Train();
            _trained = true;
        }

        if (_network == null)
            return _sampleAction();

        var input = _stateConverter(state);
        var outputs = _network.FeedForward(input);
        return _actionConverter(outputs);
    }

    public void Learn(TState state, TAction action, double reward, TState nextState, bool done)
    {
        // Learning happens during Train()
    }

    public void Reset() => _env.Reset();

    private void Train()
    {
        int inputSize = GetObservationSize();
        int outputSize = GetActionSize();

        _learner.Initialize(inputSize, outputSize, initialHiddenNodes: 2);

        Func<NeuralNetwork, (double, double[])> evalFunc = net => EvaluateNetwork(net);
        Func<List<double>, List<double>, (List<double>, double, bool)> envFunc = (s, a) => EnvironmentStep(s, a);

        _learner.Learn(evalFunc, envFunc, cycles: 1);
        _network = _learner.GetBestNetwork();
    }

    private (double, double[]) EvaluateNetwork(NeuralNetwork net)
    {
        var state = _env.Reset();
        var obs = _stateConverter(state);
        double totalReward = 0;
        bool done = false;
        int steps = 0;
        while (!done && steps < 200)
        {
            var action = net.FeedForward(obs);
            var (nextState, reward, isDone, _) = _env.Step(_actionConverter(action));
            obs = _stateConverter(nextState);
            totalReward += reward;
            done = isDone;
            steps++;
        }
        double[] behavior = { totalReward, steps };
        return (totalReward, behavior);
    }

    private (List<double>, double, bool) EnvironmentStep(List<double> state, List<double> action)
    {
        if (state.Count == 0)
        {
            var reset = _env.Reset();
            return (_stateConverter(reset), 0.0, false);
        }

        var (nextState, reward, done, _) = _env.Step(_actionConverter(action));
        return (_stateConverter(nextState), reward, done);
    }

    private int GetObservationSize()
    {
        return _env.ObservationSpace switch
        {
            Box box => box.Dimension,
            Discrete => 1,
            MultiBinary mb => mb.N,
            MultiDiscrete md => md.Nvec.Length,
            _ => throw new NotSupportedException("Unsupported observation space")
        };
    }

    private int GetActionSize()
    {
        return _env.ActionSpace switch
        {
            Discrete d => d.N,
            Box b => b.Dimension,
            MultiBinary mb => mb.N,
            MultiDiscrete md => md.Nvec.Length,
            _ => throw new NotSupportedException("Unsupported action space")
        };
    }

    private Func<TState, List<double>> CreateStateConverter()
    {
        return state =>
        {
            switch (state)
            {
                case null:
                    return new List<double>();
                case float f:
                    return new List<double> { f };
                case double d:
                    return new List<double> { d };
                case int i:
                    return new List<double> { i };
                case float[] fa:
                    return fa.Select(v => (double)v).ToList();
                case double[] da:
                    return da.ToList();
                case int[] ia:
                    return ia.Select(v => (double)v).ToList();
                case IEnumerable<float> fe:
                    return fe.Select(v => (double)v).ToList();
                case IEnumerable<double> de:
                    return de.ToList();
                case IEnumerable<int> ie:
                    return ie.Select(v => (double)v).ToList();
                default:
                    if (state is ValueType)
                    {
                        return state.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Select(f => Convert.ToDouble(f.GetValue(state)!))
                            .ToList();
                    }
                    throw new NotSupportedException($"Unsupported state type {typeof(TState)}");
            }
        };
    }

    private Func<List<double>, TAction> CreateActionConverter()
    {
        if (_env.ActionSpace is Discrete && typeof(TAction) == typeof(int))
        {
            return list => (TAction)(object)list.IndexOf(list.Max());
        }
        if (_env.ActionSpace is Box box)
        {
            if (typeof(TAction) == typeof(float[]))
            {
                return list => (TAction)(object)list.Select(v => (float)v).ToArray();
            }
            if (typeof(TAction) == typeof(double[]))
            {
                return list => (TAction)(object)list.ToArray();
            }
            if (typeof(TAction) == typeof(float))
            {
                return list => (TAction)(object)(float)list[0];
            }
            if (typeof(TAction) == typeof(double))
            {
                return list => (TAction)(object)list[0];
            }
        }
        throw new NotSupportedException($"Unsupported action type {typeof(TAction)} for space {_env.ActionSpace.GetType()}");
    }

    private Func<TAction> CreateSampleAction()
    {
        if (_env.ActionSpace is Discrete d && typeof(TAction) == typeof(int))
        {
            return () => (TAction)(object)d.Sample();
        }
        if (_env.ActionSpace is Box b)
        {
            if (typeof(TAction) == typeof(float[]))
            {
                return () => (TAction)(object)b.Sample();
            }
            if (typeof(TAction) == typeof(double[]))
            {
                return () => (TAction)(object)b.Sample().Select(x => (double)x).ToArray();
            }
            if (typeof(TAction) == typeof(float))
            {
                return () => (TAction)(object)b.Sample()[0];
            }
            if (typeof(TAction) == typeof(double))
            {
                return () => (TAction)(object)(double)b.Sample()[0];
            }
        }
        throw new NotSupportedException("Unsupported action space for sampling");
    }
}
