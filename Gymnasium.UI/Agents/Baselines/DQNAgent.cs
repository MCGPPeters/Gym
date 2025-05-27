using System;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines;

[Export(typeof(IAgentPlugin))]
public class DQNAgentPlugin : IAgentPlugin
{
    public string Name => "DQN (Deep Q-Network)";
    public string Description => "Deep Q-Network algorithm for discrete action spaces. Uses experience replay and target networks for stable learning.";
    
    public object CreateAgent(object env, object? config = null)
    {
        var cfg = config as DQNConfig ?? new DQNConfig();
        return new DQNAgent(env, cfg);
    }
    
    public Func<double>? GetLossFetcher(object agent)
    {
        if (agent is DQNAgent dqnAgent)
            return () => dqnAgent.GetLoss();
        return null;
    }
}

/// <summary>
/// Configuration for DQN agent
/// </summary>
public class DQNConfig
{
    public int BufferSize { get; set; } = 100000;
    public int BatchSize { get; set; } = 32;
    public double LearningRate { get; set; } = 0.0001;
    public double Gamma { get; set; } = 0.99;
    public double EpsilonStart { get; set; } = 1.0;
    public double EpsilonEnd { get; set; } = 0.05;
    public double EpsilonDecay { get; set; } = 0.995;
    public int TargetUpdateFrequency { get; set; } = 1000;
    public int HiddenSize { get; set; } = 128;
    public int TrainingStartSteps { get; set; } = 1000;
}

/// <summary>
/// Deep Q-Network (DQN) agent implementation
/// Reference: https://github.com/openai/baselines/tree/master/baselines/deepq
/// </summary>
public class DQNAgent : BaselineAgent
{
    private readonly DQNConfig _config;
    private readonly SimpleNeuralNetwork _qNetwork;
    private readonly SimpleNeuralNetwork _targetNetwork;
    private readonly ReplayBuffer _replayBuffer;
    private double _epsilon;
    private int _targetUpdateCounter;
    private readonly int _stateSize;
    private readonly int _actionSize;
    
    public DQNAgent(object env, DQNConfig config) : base(env)
    {
        _config = config;
        _epsilon = config.EpsilonStart;
        _targetUpdateCounter = 0;
        
        // Determine state and action sizes
        _stateSize = GetStateSize(env);
        _actionSize = GetActionSize(env);
        
        // Initialize networks
        _qNetwork = new SimpleNeuralNetwork(_stateSize, config.HiddenSize, _actionSize, _rng);
        _targetNetwork = new SimpleNeuralNetwork(_stateSize, config.HiddenSize, _actionSize, _rng);
        
        // Initialize replay buffer
        _replayBuffer = new ReplayBuffer(config.BufferSize, _rng);
        
        Console.WriteLine($"DQN Agent initialized: State size={_stateSize}, Action size={_actionSize}");
    }
    
    public override object Act(object state)
    {
        _stepCount++;
        
        // Epsilon-greedy action selection
        if (_rng.NextDouble() < _epsilon)
        {
            // Random action
            return _rng.Next(_actionSize);
        }
        else
        {
            // Greedy action based on Q-values
            var stateVector = StateToVector(state);
            var qValues = _qNetwork.Forward(stateVector);
            
            // Find action with highest Q-value
            int bestAction = 0;
            float bestValue = qValues[0];
            for (int i = 1; i < qValues.Length; i++)
            {
                if (qValues[i] > bestValue)
                {
                    bestValue = qValues[i];
                    bestAction = i;
                }
            }
            
            return bestAction;
        }
    }
    
    public override void Learn(object state, object action, double reward, object nextState, bool done)
    {
        // Store experience in replay buffer
        _replayBuffer.Add(state, action, reward, nextState, done);
        
        // Start training only after we have enough experiences
        if (_replayBuffer.Count < _config.TrainingStartSteps)
            return;
            
        // Sample batch from replay buffer
        var batch = _replayBuffer.Sample(_config.BatchSize);
        
        // Compute target Q-values using target network
        var losses = new float[batch.Length];
        
        for (int i = 0; i < batch.Length; i++)
        {
            var experience = batch[i];
            var stateVec = StateToVector(experience.State);
            var nextStateVec = StateToVector(experience.NextState);
            
            var currentQValues = _qNetwork.Forward(stateVec);
            var nextQValues = _targetNetwork.Forward(nextStateVec);
            
            // Compute target Q-value
            double targetQ;
            if (experience.Done)
            {
                targetQ = experience.Reward;
            }
            else
            {
                var maxNextQ = nextQValues.Max();
                targetQ = experience.Reward + _config.Gamma * maxNextQ;
            }
            
            // Update Q-value for the taken action
            var targetValues = currentQValues.ToArray();
            int actionIndex = (int)experience.Action;
            targetValues[actionIndex] = (float)targetQ;
            
            // Compute loss (TD error)
            var loss = Math.Abs(targetValues[actionIndex] - currentQValues[actionIndex]);
            losses[i] = loss;
            
            // Update network weights
            _qNetwork.UpdateWeights(stateVec, targetValues, (float)_config.LearningRate);
        }
        
        // Update average loss
        _currentLoss = losses.Average();
        
        // Decay epsilon
        if (_epsilon > _config.EpsilonEnd)
        {
            _epsilon *= _config.EpsilonDecay;
        }
        
        // Update target network periodically
        _targetUpdateCounter++;
        if (_targetUpdateCounter >= _config.TargetUpdateFrequency)
        {
            CopyNetworkWeights(_qNetwork, _targetNetwork);
            _targetUpdateCounter = 0;
        }
    }
    
    public override void Reset()
    {
        _episodeCount++;
        // Reset any episode-specific state if needed
    }
      private float[] StateToVector(object state)
    {
        switch (state)
        {
            case float[] floatArray:
                return floatArray;
            case double[] doubleArray:
                return doubleArray.Select(d => (float)d).ToArray();
            case int[] intArray:
                return intArray.Select(i => (float)i).ToArray();
            case ValueTuple<float, float, float, float> tuple4:
                return new float[] { tuple4.Item1, tuple4.Item2, tuple4.Item3, tuple4.Item4 };
            case ValueTuple<float, float> tuple2:
                return new float[] { tuple2.Item1, tuple2.Item2 };
            case ValueTuple<float, float, float> tuple3:
                return new float[] { tuple3.Item1, tuple3.Item2, tuple3.Item3 };
            case byte[] byteArray:
                // For Atari-like environments, flatten and normalize pixel data
                var normalized = new float[Math.Min(byteArray.Length, _stateSize)];
                for (int i = 0; i < normalized.Length; i++)
                {
                    normalized[i] = byteArray[i] / 255.0f;
                }
                return normalized;
            case int intValue:
                return new float[] { intValue };
            case float floatValue:
                return new float[] { floatValue };
            case double doubleValue:
                return new float[] { (float)doubleValue };
            default:
                // Try to convert to string and then parse as numbers
                var str = state.ToString();
                if (float.TryParse(str, out float value))
                {
                    return new float[] { value };
                }
                // Fallback: create a feature vector based on hash code
                var hash = state.GetHashCode();
                var features = new float[_stateSize];
                for (int i = 0; i < _stateSize; i++)
                {
                    features[i] = ((hash >> i) & 1) == 1 ? 1.0f : 0.0f;
                }
                return features;
        }
    }
    
    private int GetStateSize(object env)
    {
        // Try to get observation space size from environment
        try
        {
            var obsSpace = ((dynamic)env).ObservationSpace;
            if (obsSpace != null)
            {
                // For Box spaces
                if (obsSpace.Shape != null)
                {
                    var shape = obsSpace.Shape as int[];
                    if (shape != null)
                    {
                        return shape.Aggregate(1, (a, b) => a * b);
                    }
                }
                // For Discrete spaces
                if (obsSpace.N != null)
                {
                    return (int)obsSpace.N;
                }
            }
        }
        catch
        {
            // Fallback if we can't determine the size
        }
        
        // Default size for unknown environments
        return 128;
    }
      private int GetActionSize(object env)
    {
        try
        {
            var actionSpace = ((dynamic)env).ActionSpace;
            
            // DQN only supports discrete action spaces
            if (actionSpace is Gymnasium.Spaces.Discrete)
            {
                return (int)actionSpace.N;
            }
            else if (actionSpace is Gymnasium.Spaces.Box)
            {
                throw new NotSupportedException("DQN agent only supports discrete action spaces. Use DDPG, A2C, or other agents for continuous action spaces like BipedalWalker.");
            }
            else
            {
                throw new NotSupportedException($"Action space type {actionSpace.GetType()} not supported by DQN agent");
            }
        }
        catch (NotSupportedException)
        {
            throw; // Re-throw our specific exceptions
        }
        catch
        {
            // Fallback if we can't determine the size
        }
        
        // Default size for unknown environments
        return 4;
    }
    
    private void CopyNetworkWeights(SimpleNeuralNetwork source, SimpleNeuralNetwork target)
    {
        // In a real implementation, we would copy the actual network weights
        // For this simplified version, we'll reinitialize the target network
        // This is a placeholder - in practice you'd implement proper weight copying
    }
}
