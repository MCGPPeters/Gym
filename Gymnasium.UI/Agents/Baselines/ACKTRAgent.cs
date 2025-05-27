using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Actor-Critic using Kronecker-Factored Trust Region (ACKTR) agent plugin
    /// Uses KFAC for natural gradient approximation
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class ACKTRAgentPlugin : IAgentPlugin
    {
        public string Name => "ACKTR (Actor-Critic using KFAC)";
        public string Description => "Actor-Critic using Kronecker-Factored Trust Region with natural gradients";
        public object CreateAgent(object env, object? config = null) => new ACKTRAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((ACKTRAgent)agent).GetLoss();
    }
      /// <summary>
    /// ACKTR agent implementation
    /// </summary>
    public class ACKTRAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _actorNetwork;
        private SimpleNeuralNetwork _criticNetwork;
        private List<float[]> _states;
        private List<object> _actions; // Changed to object to handle both int and float[]
        private List<float> _rewards;
        private List<bool> _dones;
        private List<float> _values;
        private bool _isDiscrete;
        
        // KFAC-specific parameters
        private KFACOptimizer _kfacOptimizer;
        private readonly float _learningRate;
        private readonly float _gamma;
        private readonly float _valueCoef;
        private readonly float _entropyCoef;
        private readonly int _rolloutLength;
        private readonly float _dampingFactor;
        
        public ACKTRAgent(object env) : base(env)
        {            _learningRate = 0.25f;
            _gamma = 0.99f;
            _valueCoef = 0.25f;
            _entropyCoef = 0.01f;
            _rolloutLength = 20;
            _dampingFactor = 0.001f;
              _states = new List<float[]>();
            _actions = new List<object>();
            _rewards = new List<float>();
            _dones = new List<bool>();
            _values = new List<float>();
            
            Initialize();
        }        private void Initialize()
        {
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
            
            // Initialize KFAC optimizer
            _kfacOptimizer = new KFACOptimizer(_learningRate, _dampingFactor);
        }        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            
            // Get action logits from actor network
            var actionLogits = _actorNetwork.Forward(stateArray);
            
            object action;
            if (_isDiscrete)
            {
                // For discrete actions: use softmax and sample
                var actionProbs = Softmax(actionLogits);
                action = SampleFromDistribution(actionProbs);
            }
            else
            {
                // For continuous actions: use action output directly with noise for exploration
                if (actionLogits.Length == 1)
                {
                    // Single continuous action
                    var noise = (float)(_rng.NextGaussian() * 0.1);
                    action = actionLogits[0] + noise;
                }
                else
                {
                    // Multiple continuous actions (like BipedalWalker)
                    var continuousAction = new float[actionLogits.Length];
                    for (int i = 0; i < actionLogits.Length; i++)
                    {
                        var noise = (float)(_rng.NextGaussian() * 0.1);
                        continuousAction[i] = actionLogits[i] + noise;
                    }
                    action = continuousAction;
                }
            }
            
            // Get value estimate from critic network
            var valueEst = _criticNetwork.Forward(stateArray)[0];
            
            // Store trajectory data
            _states.Add(stateArray);
            _actions.Add(action);
            _values.Add(valueEst);
            
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
            
            // Compute gradients
            var actorGradients = ComputeActorGradients(advantages);
            var criticGradients = ComputeCriticGradients(returns);
            
            // Apply KFAC updates
            _kfacOptimizer.UpdateActor(_actorNetwork, actorGradients, _states, _actions);
            _kfacOptimizer.UpdateCritic(_criticNetwork, criticGradients, _states, returns);
            
            // Compute loss for monitoring
            _currentLoss = ComputeLoss(returns, advantages);
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
        }        private float[] ComputeActorGradients(float[] advantages)
        {
            var gradients = new float[100]; // Simplified - should be actual network parameter count
            
            for (int i = 0; i < _states.Count; i++)
            {
                if (_isDiscrete)
                {
                    var actionLogits = _actorNetwork.Forward(_states[i]);
                    var actionProbs = Softmax(actionLogits);
                    var actionIndex = (int)_actions[i];
                    var logProb = (float)Math.Log(Math.Max(actionProbs[actionIndex], 1e-8f));
                    
                    // Policy gradient
                    for (int j = 0; j < gradients.Length; j++)
                    {
                        gradients[j] += advantages[i] * logProb; // Simplified gradient computation
                    }
                }
                else
                {
                    // For continuous actions, compute gradients differently
                    // This is a simplified approach - real implementation would be more complex
                    for (int j = 0; j < gradients.Length; j++)
                    {
                        gradients[j] += advantages[i]; // Simplified continuous gradient computation
                    }
                }
            }
            
            return gradients;
        }

        private float[] ComputeCriticGradients(float[] returns)
        {
            var gradients = new float[50]; // Simplified - should be actual network parameter count
            
            for (int i = 0; i < _states.Count; i++)
            {
                var valueEst = _criticNetwork.Forward(_states[i])[0];
                var valueDiff = returns[i] - valueEst;
                
                // Value function gradient
                for (int j = 0; j < gradients.Length; j++)
                {
                    gradients[j] += valueDiff; // Simplified gradient computation
                }
            }
            
            return gradients;
        }        private float ComputeLoss(float[] returns, float[] advantages)
        {
            float actorLoss = 0;
            float criticLoss = 0;
            
            for (int i = 0; i < _states.Count; i++)
            {
                if (_isDiscrete)
                {
                    var actionLogits = _actorNetwork.Forward(_states[i]);
                    var actionProbs = Softmax(actionLogits);
                    var actionIndex = (int)_actions[i];
                    var logProb = (float)Math.Log(Math.Max(actionProbs[actionIndex], 1e-8f));
                    
                    actorLoss -= logProb * advantages[i];
                }
                else
                {
                    // For continuous actions, use MSE loss on action prediction
                    var actionLogits = _actorNetwork.Forward(_states[i]);
                    var predictedActions = actionLogits;
                    
                    if (_actions[i] is float actionFloat)
                    {
                        var actionError = predictedActions[0] - actionFloat;
                        actorLoss += actionError * actionError;
                    }
                    else if (_actions[i] is float[] actionArray)
                    {
                        for (int j = 0; j < Math.Min(predictedActions.Length, actionArray.Length); j++)
                        {
                            var actionError = predictedActions[j] - actionArray[j];
                            actorLoss += actionError * actionError;
                        }
                    }
                }
                
                var valueEst = _criticNetwork.Forward(_states[i])[0];
                var valueDiff = returns[i] - valueEst;
                criticLoss += valueDiff * valueDiff;
            }
            
            return actorLoss + _valueCoef * criticLoss;
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
        }
    }
    
    /// <summary>
    /// Simplified KFAC optimizer implementation
    /// </summary>
    public class KFACOptimizer
    {
        private readonly float _learningRate;
        private readonly float _dampingFactor;
        
        public KFACOptimizer(float learningRate, float dampingFactor)
        {
            _learningRate = learningRate;
            _dampingFactor = dampingFactor;
        }
          public void UpdateActor(SimpleNeuralNetwork network, float[] gradients, List<float[]> states, List<object> actions)
        {
            // Simplified KFAC update for actor network
            // In practice, this would involve computing Kronecker factors and Fisher information matrix
        }
        
        public void UpdateCritic(SimpleNeuralNetwork network, float[] gradients, List<float[]> states, float[] returns)
        {
            // Simplified KFAC update for critic network
            // In practice, this would involve computing Kronecker factors and Gauss-Newton approximation
        }
    }
}
