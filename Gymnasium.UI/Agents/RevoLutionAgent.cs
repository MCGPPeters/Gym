using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;
using RevoLution.Hybrid;
using RevoLution.Neural;

namespace Gymnasium.UI.Agents;

[Export(typeof(IAgentPlugin))]
public class RevoLutionAgentPlugin : IAgentPlugin
{
    public string Name => "RevoLution Hybrid Agent";
    public string Description => "Hybrid neuroevolution and reinforcement learning agent using the RevoLution algorithm.";

    public object CreateAgent(object env, object? config = null) => new RevoLutionAgent(env);

    public Func<double>? GetLossFetcher(object agent) => null; // Algorithm does not expose loss directly
}

public class RevoLutionAgent
{
    private readonly dynamic _env;
    private readonly HybridLearner _learner = new();
    private NeuralNetwork? _network;
    private bool _trained;

    public RevoLutionAgent(object env)
    {
        _env = env;
    }

    public object Act(object state)
    {
        // Train on first call if not already done
        if (!_trained)
        {
            Train();
            _trained = true;
        }

        if (_network == null)
            return _env.ActionSpace.Sample();

        var input = StateToList(state);
        var outputs = _network.FeedForward(input);

        if (_env.ActionSpace is Gymnasium.Spaces.Discrete)
        {
            int idx = outputs.IndexOf(outputs.Max());
            return idx;
        }
        else
        {
            return outputs.ToArray();
        }
    }

    public void Learn(object state, object action, double reward, object nextState, bool done)
    {
        // Learning handled in Train()
    }

    public void Reset()
    {
        _env.Reset();
    }

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
        var obs = StateToList(state);
        double totalReward = 0;
        bool done = false;
        int steps = 0;
        while (!done && steps < 200)
        {
            var action = net.FeedForward(obs);
            var (nextState, reward, isDone, _) = _env.Step(ConvertAction(action));
            obs = StateToList(nextState);
            totalReward += reward;
            done = isDone;
            steps++;
        }
        double[] behavior = { totalReward, steps };
        return (totalReward, behavior);
    }

    private (List<double>, double, bool) EnvironmentStep(List<double> state, List<double> action)
    {
        if (state == null || state.Count == 0)
        {
            var reset = _env.Reset();
            return (StateToList(reset), 0.0, false);
        }

        var (nextState, reward, done, _) = _env.Step(ConvertAction(action));
        return (StateToList(nextState), reward, done);
    }

    private int GetObservationSize()
    {
        dynamic obsSpace = _env.ObservationSpace;
        return obsSpace.Shape[0];
    }

    private int GetActionSize()
    {
        dynamic actSpace = _env.ActionSpace;
        if (actSpace is Gymnasium.Spaces.Discrete) return actSpace.N;
        if (actSpace is Gymnasium.Spaces.Box) return actSpace.Dimension;
        throw new NotSupportedException($"Unsupported action space {actSpace}");
    }

    private List<double> StateToList(object state)
    {
        switch (state)
        {
            case float[] fa: return fa.Select(x => (double)x).ToList();
            case int[] ia: return ia.Select(x => (double)x).ToList();
            case ValueTuple<float,float,float,float> t:
                return new List<double> { t.Item1, t.Item2, t.Item3, t.Item4 };
            default:
                return ((IEnumerable<object>)state).Select(Convert.ToDouble).ToList();
        }
    }

    private List<double> ConvertAction(List<double> action)
    {
        if (_env.ActionSpace is Gymnasium.Spaces.Discrete)
        {
            int idx = action.IndexOf(action.Max());
            return new List<double> { idx };
        }
        return action;
    }
}
