using System;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines;

[Export(typeof(IAgentPlugin))]
public class PPOAgentPlugin : IAgentPlugin
{
    public string Name => "PPO (Proximal Policy Optimization)";
    public string Description => "Proximal Policy Optimization algorithm for both discrete and continuous action spaces. Uses clipped surrogate objective for stable policy updates.";
    
    public object CreateAgent(object env, object? config = null)
    {
        var cfg = config as PPOConfig ?? new PPOConfig();
        return new PPOAgent(env, cfg);
    }
    
    public Func<double>? GetLossFetcher(object agent)
    {
        if (agent is PPOAgent ppoAgent)
            return () => ppoAgent.GetLoss();
        return null;
    }
}

/// <summary>
/// Configuration for PPO agent
/// </summary>
public class PPOConfig
{
    public double LearningRate { get; set; } = 0.0003;
    public double Gamma { get; set; } = 0.99;
    public double Lambda { get; set; } = 0.95; // GAE lambda
    public double ClipEpsilon { get; set; } = 0.2;
    public double ValueLossCoeff { get; set; } = 0.5;
    public double EntropyCoeff { get; set; } = 0.01;
    public int HiddenSize { get; set; } = 128;
    public int BatchSize { get; set; } = 64;
    public int EpochsPerUpdate { get; set; } = 4;
    public int StepsPerUpdate { get; set; } = 2048;
}

/// <summary>
/// Proximal Policy Optimization (PPO) agent implementation
/// Reference: https://github.com/openai/baselines/tree/master/baselines/ppo2
/// </summary>
public class PPOAgent : BaselineAgent
{
    private readonly PPOConfig _config;
    private readonly SimpleNeuralNetwork _policyNetwork;
    private readonly SimpleNeuralNetwork _valueNetwork;
    private readonly int _stateSize;
    private readonly int _actionSize;
    private readonly bool _isDiscrete;
    
    // Trajectory buffer
    private readonly TrajectoryBuffer _trajectoryBuffer;
    
    public PPOAgent(object env, PPOConfig config) : base(env)
    {
        _config = config;
        
        // Determine state and action sizes
        _stateSize = GetStateSize(env);
        _actionSize = GetActionSize(env);
        _isDiscrete = IsDiscreteActionSpace(env);
        
        // Initialize networks
        _policyNetwork = new SimpleNeuralNetwork(_stateSize, config.HiddenSize, _actionSize, _rng);
        _valueNetwork = new SimpleNeuralNetwork(_stateSize, config.HiddenSize, 1, _rng);
        
        // Initialize trajectory buffer
        _trajectoryBuffer = new TrajectoryBuffer(config.StepsPerUpdate);
        
        Console.WriteLine($"PPO Agent initialized: State size={_stateSize}, Action size={_actionSize}, Discrete={_isDiscrete}");
    }
    
    public override object Act(object state)
    {
        _stepCount++;
        
        var stateVector = StateToVector(state);
        
        if (_isDiscrete)
        {
            // Discrete action space - use softmax policy
            var logits = _policyNetwork.Forward(stateVector);
            var probs = Softmax(logits);
            
            // Sample action from probability distribution
            var action = SampleFromDistribution(probs);
            
            // Store state and action for later learning
            var value = _valueNetwork.Forward(stateVector)[0];
            var logProb = Math.Log(probs[action]);
            _trajectoryBuffer.Add(state, action, 0.0, value, logProb); // Reward will be added in Learn()
            
            return action;
        }
        else
        {
            // Continuous action space - use Gaussian policy
            var mean = _policyNetwork.Forward(stateVector);
            var action = new float[mean.Length];
            
            // Sample from Gaussian distribution
            for (int i = 0; i < mean.Length; i++)
            {
                action[i] = mean[i] + (float)_rng.NextGaussian() * 0.1f; // Fixed std for simplicity
            }
            
            var value = _valueNetwork.Forward(stateVector)[0];
            var logProb = ComputeGaussianLogProb(action, mean, 0.1f);
            _trajectoryBuffer.Add(state, action, 0.0, value, logProb);
            
            return action;
        }
    }
    
    public override void Learn(object state, object action, double reward, object nextState, bool done)
    {
        // Update the last trajectory step with the reward
        _trajectoryBuffer.SetLastReward(reward);
        
        if (done)
        {
            // Compute advantages and update policy at end of episode
            var nextStateVector = StateToVector(nextState);
            var nextValue = done ? 0.0f : _valueNetwork.Forward(nextStateVector)[0];
            
            var trajectories = _trajectoryBuffer.GetTrajectories(nextValue, _config.Gamma, _config.Lambda);
            
            if (trajectories.Length >= _config.BatchSize)
            {
                UpdatePolicy(trajectories);
                _trajectoryBuffer.Clear();
            }
        }
    }
    
    public override void Reset()
    {
        _episodeCount++;
    }
    
    private void UpdatePolicy(Trajectory[] trajectories)
    {
        for (int epoch = 0; epoch < _config.EpochsPerUpdate; epoch++)
        {
            // Shuffle trajectories
            var shuffled = trajectories.OrderBy(x => _rng.Next()).ToArray();
            
            float totalPolicyLoss = 0;
            float totalValueLoss = 0;
            
            for (int i = 0; i < shuffled.Length; i += _config.BatchSize)
            {
                var batch = shuffled.Skip(i).Take(_config.BatchSize).ToArray();
                
                foreach (var trajectory in batch)
                {
                    var stateVector = StateToVector(trajectory.State);
                    
                    // Compute current policy and value
                    var currentLogits = _policyNetwork.Forward(stateVector);
                    var currentValue = _valueNetwork.Forward(stateVector)[0];
                    
                    // Compute policy loss (clipped surrogate objective)
                    float currentLogProb, ratio;
                    
                    if (_isDiscrete)
                    {
                        var probs = Softmax(currentLogits);
                        var actionIndex = (int)trajectory.Action;
                        currentLogProb = (float)Math.Log(probs[actionIndex]);
                        ratio = (float)Math.Exp(currentLogProb - trajectory.LogProb);
                    }
                    else
                    {
                        var action = (float[])trajectory.Action;
                        currentLogProb = ComputeGaussianLogProb(action, currentLogits, 0.1f);
                        ratio = (float)Math.Exp(currentLogProb - trajectory.LogProb);
                    }
                    
                    // PPO clipped loss
                    var advantage = trajectory.Advantage;
                    var clippedRatio = Math.Max(Math.Min(ratio, 1 + _config.ClipEpsilon), 1 - _config.ClipEpsilon);
                    var policyLoss = -Math.Min(ratio * advantage, clippedRatio * advantage);
                    
                    // Value loss
                    var valueLoss = Math.Pow(currentValue - trajectory.Return, 2);
                    
                    totalPolicyLoss += (float)policyLoss;
                    totalValueLoss += (float)valueLoss;
                    
                    // Update networks (simplified gradient descent)
                    if (_isDiscrete)
                    {
                        var targetLogits = currentLogits.ToArray();
                        var actionIndex = (int)trajectory.Action;
                        targetLogits[actionIndex] += (float)(_config.LearningRate * advantage);
                        _policyNetwork.UpdateWeights(stateVector, targetLogits, (float)_config.LearningRate);
                    }
                    
                    var targetValue = new float[] { trajectory.Return };
                    _valueNetwork.UpdateWeights(stateVector, targetValue, (float)_config.LearningRate);
                }
            }
            
            _currentLoss = (totalPolicyLoss + totalValueLoss * _config.ValueLossCoeff) / trajectories.Length;
        }
    }
    
    private float[] Softmax(float[] logits)
    {
        var maxLogit = logits.Max();
        var exp = logits.Select(x => Math.Exp(x - maxLogit)).ToArray();
        var sum = exp.Sum();
        return exp.Select(x => (float)(x / sum)).ToArray();
    }
    
    private int SampleFromDistribution(float[] probs)
    {
        var rand = _rng.NextDouble();
        var cumulative = 0.0;
        
        for (int i = 0; i < probs.Length; i++)
        {
            cumulative += probs[i];
            if (rand <= cumulative)
                return i;
        }
        
        return probs.Length - 1;
    }
    
    private float ComputeGaussianLogProb(float[] action, float[] mean, float std)
    {
        var logProb = 0.0;
        for (int i = 0; i < action.Length; i++)
        {
            var diff = action[i] - mean[i];
            logProb += -0.5 * Math.Pow(diff / std, 2) - Math.Log(std * Math.Sqrt(2 * Math.PI));
        }
        return (float)logProb;
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
        try
        {
            var obsSpace = ((dynamic)env).ObservationSpace;
            if (obsSpace?.Shape != null)
            {
                var shape = obsSpace.Shape as int[];
                return shape?.Aggregate(1, (a, b) => a * b) ?? 128;
            }
            if (obsSpace?.N != null)
            {
                return (int)obsSpace.N;
            }
        }
        catch { }
        return 128;
    }
      private int GetActionSize(object env)
    {
        try
        {
            var actionSpace = ((dynamic)env).ActionSpace;
            
            if (actionSpace is Gymnasium.Spaces.Discrete)
            {
                return (int)actionSpace.N;
            }
            else if (actionSpace is Gymnasium.Spaces.Box)
            {
                return actionSpace.Dimension;
            }
            else if (actionSpace?.Shape != null)
            {
                var shape = actionSpace.Shape as int[];
                return shape?.Aggregate(1, (a, b) => a * b) ?? 4;
            }
        }
        catch { }
        return 4;
    }
      private bool IsDiscreteActionSpace(object env)
    {
        try
        {
            var actionSpace = ((dynamic)env).ActionSpace;
            return actionSpace is Gymnasium.Spaces.Discrete;
        }
        catch { }
        return true; // Default to discrete
    }
}

/// <summary>
/// Trajectory data for PPO
/// </summary>
public record Trajectory(object State, object Action, float Return, float Advantage, float LogProb);

/// <summary>
/// Buffer for storing trajectory data
/// </summary>
public class TrajectoryBuffer
{
    private readonly int _capacity;
    private readonly TrajectoryStep[] _steps;
    private int _position;
    
    public TrajectoryBuffer(int capacity)
    {
        _capacity = capacity;
        _steps = new TrajectoryStep[capacity];
        _position = 0;
    }
    
    public void Add(object state, object action, double reward, float value, double logProb)
    {
        if (_position < _capacity)
        {
            _steps[_position] = new TrajectoryStep(state, action, (float)reward, value, (float)logProb);
            _position++;
        }
    }
    
    public void SetLastReward(double reward)
    {
        if (_position > 0)
        {
            var lastStep = _steps[_position - 1];
            _steps[_position - 1] = lastStep with { Reward = (float)reward };
        }
    }
    
    public Trajectory[] GetTrajectories(float nextValue, double gamma, double lambda)
    {
        if (_position == 0) return Array.Empty<Trajectory>();
        
        var trajectories = new Trajectory[_position];
        var returns = new float[_position];
        var advantages = new float[_position];
        
        // Compute returns and advantages using GAE
        var lastGae = 0.0f;
        for (int i = _position - 1; i >= 0; i--)
        {
            var step = _steps[i];
            var nextValueEst = i == _position - 1 ? nextValue : _steps[i + 1].Value;
            var delta = step.Reward + gamma * nextValueEst - step.Value;
            lastGae = (float)(delta + gamma * lambda * lastGae);
            advantages[i] = lastGae;
            returns[i] = advantages[i] + step.Value;
        }
        
        // Normalize advantages
        var meanAdv = advantages.Average();
        var stdAdv = Math.Sqrt(advantages.Select(a => Math.Pow(a - meanAdv, 2)).Average());
        if (stdAdv > 0)
        {
            for (int i = 0; i < advantages.Length; i++)
            {
                advantages[i] = (float)((advantages[i] - meanAdv) / stdAdv);
            }
        }
        
        for (int i = 0; i < _position; i++)
        {
            var step = _steps[i];
            trajectories[i] = new Trajectory(step.State, step.Action, returns[i], advantages[i], step.LogProb);
        }
        
        return trajectories;
    }
    
    public void Clear()
    {
        _position = 0;
    }
}

public record TrajectoryStep(object State, object Action, float Reward, float Value, float LogProb);
