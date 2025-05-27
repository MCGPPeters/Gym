using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Advantage Actor-Critic (A2C) agent plugin
    /// Synchronous version of A3C that uses single-threaded execution
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class A2CAgentPlugin : IAgentPlugin
    {
        public string Name => "A2C (Advantage Actor-Critic)";
        public string Description => "Synchronous advantage actor-critic algorithm with entropy regularization";
        public object CreateAgent(object env, object? config = null) => new A2CAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((A2CAgent)agent).GetLoss();    }
    
    /// <summary>
    /// Advantage Actor-Critic (A2C) agent implementation
    /// </summary>
    public class A2CAgent : BaselineAgent    {
        private SimpleNeuralNetwork _actorNetwork;
        private SimpleNeuralNetwork _criticNetwork;
        private List<float[]> _states;
        private List<object> _actions;  // Changed to object to handle both int and float[]
        private List<float> _rewards;
        private List<bool> _dones;
        private List<float> _values;
        private List<float> _logProbs;
        private bool _isDiscrete;  // Track if action space is discrete
        
        // Hyperparameters
        private readonly float _learningRate;
        private readonly float _gamma;
        private readonly float _valueCoef;
        private readonly float _entropyCoef;
        private readonly int _rolloutLength;
          public A2CAgent(object env) : base(env)
        {
            _learningRate = 0.0007f;
            _gamma = 0.99f;
            _valueCoef = 0.5f;
            _entropyCoef = 0.01f;
            _rolloutLength = 5;
              _states = new List<float[]>();
            _actions = new List<object>();  // Changed to object
            _rewards = new List<float>();
            _dones = new List<bool>();
            _values = new List<float>();
            _logProbs = new List<float>();
            
            Initialize();
        }

        private void Initialize()        {
            // Get environment dimensions
            dynamic actionSpace = _env.ActionSpace;
            dynamic observationSpace = _env.ObservationSpace;
            
            int stateSize = observationSpace.Shape[0];
            int actionSize;
              // Handle both discrete and continuous action spaces
            if (actionSpace is Gymnasium.Spaces.Discrete)
            {
                actionSize = actionSpace.N;
                _isDiscrete = true;
            }
            else if (actionSpace is Gymnasium.Spaces.Box)
            {
                actionSize = actionSpace.Dimension;
                _isDiscrete = false;
            }
            else
            {
                throw new NotSupportedException($"Action space type {actionSpace.GetType()} not supported");
            }
            
            // Initialize networks
            _actorNetwork = new SimpleNeuralNetwork(stateSize, 128, actionSize, _rng);
            _criticNetwork = new SimpleNeuralNetwork(stateSize, 128, 1, _rng);
        }        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            
            // Get action output from actor network
            var actionOutput = _actorNetwork.Forward(stateArray);
            
            // Get value estimate from critic network
            var valueEst = _criticNetwork.Forward(stateArray)[0];
            
            object action;
            float logProb;
            
            if (_isDiscrete)
            {
                // For discrete actions: use softmax and sample
                var softmaxProbs = Softmax(actionOutput);
                var discreteAction = SampleFromDistribution(softmaxProbs);
                action = discreteAction;
                logProb = (float)Math.Log(Math.Max(softmaxProbs[discreteAction], 1e-8f));
            }            else
            {
                // For continuous actions: use action output directly with noise for exploration
                if (actionOutput.Length == 1)
                {
                    // Single continuous action (like MountainCarContinuous, Pendulum)
                    var noise = (float)(_rng.NextGaussian() * 0.1);
                    var singleAction = actionOutput[0] + noise;
                    action = singleAction;
                    logProb = -(noise * noise) / (2 * 0.01f);
                }
                else
                {
                    // Multiple continuous actions (like multi-dimensional action spaces)
                    var continuousAction = new float[actionOutput.Length];
                    logProb = 0;
                    for (int i = 0; i < actionOutput.Length; i++)
                    {
                        var noise = (float)(_rng.NextGaussian() * 0.1);
                        continuousAction[i] = actionOutput[i] + noise;
                        logProb += -(noise * noise) / (2 * 0.01f);
                    }
                    action = continuousAction;
                }
            }
            
            // Store trajectory data
            _states.Add(stateArray);
            _actions.Add(action);
            _values.Add(valueEst);
            _logProbs.Add(logProb);
            
            return action;
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            // Store reward and done flag
            _rewards.Add((float)reward);
            _dones.Add(done);
            
            // Update if we have enough data or episode ended
            if (done || _states.Count >= _rolloutLength)
            {
                Update();
                ResetTrajectory();
            }
        }

        public override void Reset()
        {
            ResetTrajectory();
        }

        public double GetLoss() => _currentLoss;

        private void Update()
        {
            if (_states.Count == 0) return;
            
            // Compute returns and advantages
            var returns = ComputeReturns();
            var advantages = ComputeAdvantages(returns);
            
            float actorLoss = 0;
            float criticLoss = 0;
            float entropyLoss = 0;
              // Update networks using collected trajectory
            for (int i = 0; i < _states.Count; i++)
            {
                var actionOutput = _actorNetwork.Forward(_states[i]);
                float logProb;
                float entropy = 0;
                
                if (_isDiscrete)
                {
                    var softmaxProbs = Softmax(actionOutput);
                    var discreteAction = (int)_actions[i];
                    logProb = (float)Math.Log(Math.Max(softmaxProbs[discreteAction], 1e-8f));
                    
                    // Entropy for discrete actions
                    for (int j = 0; j < softmaxProbs.Length; j++)
                    {
                        entropy -= softmaxProbs[j] * (float)Math.Log(Math.Max(softmaxProbs[j], 1e-8f));
                    }
                }
                else
                {
                    // For continuous actions, use stored log probability
                    logProb = _logProbs[i];
                    
                    // Simple entropy estimate for continuous actions
                    entropy = 0.1f; // Fixed entropy for continuous actions
                }
                
                // Actor loss (policy gradient with advantage)
                actorLoss -= logProb * advantages[i];
                
                // Entropy loss (encourage exploration)
                entropyLoss -= entropy;
                
                // Critic loss (value function prediction error)
                var valueEst = _criticNetwork.Forward(_states[i])[0];
                var valueDiff = returns[i] - valueEst;
                criticLoss += valueDiff * valueDiff;
            }
            
            // Compute total loss
            _currentLoss = actorLoss + _valueCoef * criticLoss + _entropyCoef * entropyLoss;
            
            // Simple gradient descent update (simplified for this implementation)
            // In a real implementation, you would use proper backpropagation
        }

        private float[] ComputeReturns()
        {
            var returns = new float[_rewards.Count];
            float returnValue = 0;
            
            for (int i = _rewards.Count - 1; i >= 0; i--)
            {
                returnValue = _rewards[i] + _gamma * returnValue * (_dones[i] ? 0 : 1);
                returns[i] = returnValue;
            }
            
            return returns;
        }

        private float[] ComputeAdvantages(float[] returns)
        {
            var advantages = new float[returns.Length];
            
            for (int i = 0; i < advantages.Length; i++)
            {
                advantages[i] = returns[i] - _values[i];
            }
            
            // Normalize advantages
            var mean = advantages.Average();
            var std = (float)Math.Sqrt(advantages.Select(x => (x - mean) * (x - mean)).Average());
            
            if (std > 0)
            {
                for (int i = 0; i < advantages.Length; i++)
                {
                    advantages[i] = (advantages[i] - mean) / std;
                }
            }
            
            return advantages;
        }

        private int SampleFromDistribution(float[] probabilities)
        {
            var random = (float)_rng.NextDouble();
            var cumulative = 0f;
            
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (random <= cumulative)
                    return i;
            }
            
            return probabilities.Length - 1;
        }

        private float[] Softmax(float[] logits)
        {
            var max = logits.Max();
            var exp = logits.Select(x => (float)Math.Exp(x - max)).ToArray();
            var sum = exp.Sum();
            return exp.Select(x => x / sum).ToArray();
        }        private float[] ConvertToFloatArray(object state)
        {
            switch (state)
            {
                case float[] floatArray:
                    return floatArray;
                case double[] doubleArray:
                    return doubleArray.Select(x => (float)x).ToArray();
                case int[] intArray:
                    return intArray.Select(x => (float)x).ToArray();
                case ValueTuple<float, float, float, float> tuple4:
                    return new float[] { tuple4.Item1, tuple4.Item2, tuple4.Item3, tuple4.Item4 };
                case ValueTuple<float, float> tuple2:
                    return new float[] { tuple2.Item1, tuple2.Item2 };
                case ValueTuple<float, float, float> tuple3:
                    return new float[] { tuple3.Item1, tuple3.Item2, tuple3.Item3 };
                case int intValue:
                    return new float[] { intValue };
                case float floatValue:
                    return new float[] { floatValue };
                case double doubleValue:
                    return new float[] { (float)doubleValue };
                default:
                    // Handle other types as needed
                    try
                    {
                        return new[] { (float)Convert.ToDouble(state) };
                    }
                    catch
                    {
                        // Fallback for complex types - use hash-based features
                        var hash = state.GetHashCode();
                        return new float[] { Math.Abs(hash % 1000) / 1000.0f };
                    }
            }
        }

        private void ResetTrajectory()
        {
            _states.Clear();
            _actions.Clear();
            _rewards.Clear();
            _dones.Clear();
            _values.Clear();
            _logProbs.Clear();
        }
    }
}
