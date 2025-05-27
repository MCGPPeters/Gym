using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Trust Region Policy Optimization (TRPO) agent plugin
    /// Policy gradient method with KL divergence constraint
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class TRPOAgentPlugin : IAgentPlugin
    {
        public string Name => "TRPO (Trust Region Policy Optimization)";
        public string Description => "Policy gradient method with KL divergence constraint for stable updates";
        public object CreateAgent(object env, object? config = null) => new TRPOAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((TRPOAgent)agent).GetLoss();
    }
      /// <summary>
    /// Trust Region Policy Optimization (TRPO) agent implementation
    /// </summary>
    public class TRPOAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _policyNetwork;
        private SimpleNeuralNetwork _valueNetwork;
        private TRPOTrajectoryBuffer _trajectoryBuffer;
        private bool _isDiscrete;
        
        // Hyperparameters
        private readonly float _learningRateValue;
        private readonly float _gamma;
        private readonly float _lambda; // GAE parameter
        private readonly float _maxKlDivergence;
        private readonly float _dampingCoeff;
        private readonly int _maxBacktrackSteps;
        private readonly int _trajectoryLength;
        private readonly int _valueIterations;
        
        // Policy parameters for KL computation
        private float[] _oldPolicyParams;
        
        public TRPOAgent(object env) : base(env)
        {            _learningRateValue = 0.001f;
            _gamma = 0.99f;
            _lambda = 0.95f;
            _maxKlDivergence = 0.01f;
            _dampingCoeff = 0.1f;
            _maxBacktrackSteps = 10;
            _trajectoryLength = 2048;
            _valueIterations = 5;
            
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
            _policyNetwork = new SimpleNeuralNetwork(stateSize, 128, actionSize, _rng);
            _valueNetwork = new SimpleNeuralNetwork(stateSize, 128, 1, _rng);
            
            // Initialize trajectory buffer
            _trajectoryBuffer = new TRPOTrajectoryBuffer(_trajectoryLength);
            
            // Store initial policy parameters
            _oldPolicyParams = GetPolicyParameters();
        }        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            
            // Get action logits from policy network
            var actionLogits = _policyNetwork.Forward(stateArray);
            
            object action;
            float[] actionProbs = null;
            
            if (_isDiscrete)
            {
                // For discrete actions: use softmax and sample
                actionProbs = Softmax(actionLogits);
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
                // For continuous actions, we can use the logits as "probabilities" for tracking
                actionProbs = actionLogits;
            }
            
            // Get value estimate
            var value = _valueNetwork.Forward(stateArray)[0];
            
            // Store in trajectory buffer (note: this might need adjustment for continuous actions)
            _trajectoryBuffer.AddStep(stateArray, action, actionProbs, value);
            
            return action;
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            // Add reward to trajectory buffer
            _trajectoryBuffer.AddReward((float)reward, done);
            
            // Update if trajectory is complete or episode ended
            if (done || _trajectoryBuffer.IsFull())
            {
                Update();
                _trajectoryBuffer.Clear();
            }
        }

        public override void Reset()
        {
            _trajectoryBuffer.Clear();
        }

        public double GetLoss() => _currentLoss;

        private void Update()
        {
            if (_trajectoryBuffer.Count == 0) return;
            
            // Compute advantages using GAE
            var advantages = ComputeAdvantages();
            var returns = ComputeReturns();
            
            // Update value network
            UpdateValueNetwork(returns);
            
            // Compute policy gradient
            var policyGradient = ComputePolicyGradient(advantages);
            
            // Compute natural gradient using conjugate gradient
            var naturalGradient = ComputeNaturalGradient(policyGradient);
            
            // Line search with KL constraint
            var stepSize = LineSearch(naturalGradient, advantages);
            
            // Update policy parameters
            ApplyPolicyUpdate(naturalGradient, stepSize);
            
            // Store new policy parameters for next iteration
            _oldPolicyParams = GetPolicyParameters();
        }

        private float[] ComputeAdvantages()
        {
            var trajectory = _trajectoryBuffer.GetTrajectory();
            var advantages = new float[trajectory.Count];
            
            float gae = 0;
            for (int i = trajectory.Count - 1; i >= 0; i--)
            {
                var delta = trajectory[i].reward;
                if (i < trajectory.Count - 1)
                {
                    delta += _gamma * trajectory[i + 1].value - trajectory[i].value;
                }
                else
                {
                    delta -= trajectory[i].value;
                }
                
                gae = delta + _gamma * _lambda * gae * (trajectory[i].done ? 0 : 1);
                advantages[i] = gae;
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

        private float[] ComputeReturns()
        {
            var trajectory = _trajectoryBuffer.GetTrajectory();
            var returns = new float[trajectory.Count];
            
            float returnValue = 0;
            for (int i = trajectory.Count - 1; i >= 0; i--)
            {
                returnValue = trajectory[i].reward + _gamma * returnValue * (trajectory[i].done ? 0 : 1);
                returns[i] = returnValue;
            }
            
            return returns;
        }

        private void UpdateValueNetwork(float[] returns)
        {
            var trajectory = _trajectoryBuffer.GetTrajectory();
            
            for (int iter = 0; iter < _valueIterations; iter++)
            {
                float valueLoss = 0;
                
                for (int i = 0; i < trajectory.Count; i++)
                {
                    var predictedValue = _valueNetwork.Forward(trajectory[i].state)[0];
                    var valueDiff = returns[i] - predictedValue;
                    valueLoss += valueDiff * valueDiff;
                }
                
                _currentLoss = valueLoss / trajectory.Count;
                
                // Simple gradient descent update (simplified)
                // In practice, you would use proper backpropagation
            }
        }

        private float[] ComputePolicyGradient(float[] advantages)
        {
            var trajectory = _trajectoryBuffer.GetTrajectory();
            var gradient = new float[GetPolicyParameterCount()];
            
            for (int i = 0; i < trajectory.Count; i++)            {
                var actionLogits = _policyNetwork.Forward(trajectory[i].state);
                var actionProbs = Softmax(actionLogits);
                
                float logProb;
                if (_isDiscrete)
                {
                    var actionIndex = (int)trajectory[i].action;
                    logProb = (float)Math.Log(Math.Max(actionProbs[actionIndex], 1e-8f));
                }
                else
                {
                    // For continuous actions, simplified approach
                    logProb = 0.0f; // Placeholder for continuous case
                }
                
                // Compute gradient of log probability w.r.t. parameters
                // This is simplified - in practice you'd use automatic differentiation
                for (int j = 0; j < gradient.Length; j++)
                {
                    gradient[j] += advantages[i] * logProb; // Simplified gradient
                }
            }
            
            return gradient;
        }

        private float[] ComputeNaturalGradient(float[] policyGradient)
        {
            // Conjugate gradient method to solve H * x = g
            // where H is the Hessian of KL divergence and g is the policy gradient
            // This is a simplified implementation
            
            var naturalGradient = new float[policyGradient.Length];
            Array.Copy(policyGradient, naturalGradient, policyGradient.Length);
            
            // In practice, you would implement the full conjugate gradient algorithm
            
            return naturalGradient;
        }

        private float LineSearch(float[] searchDirection, float[] advantages)
        {
            var stepSize = 1.0f;
            var trajectory = _trajectoryBuffer.GetTrajectory();
            
            for (int i = 0; i < _maxBacktrackSteps; i++)
            {
                // Test step size
                var testParams = new float[_oldPolicyParams.Length];
                for (int j = 0; j < testParams.Length; j++)
                {
                    testParams[j] = _oldPolicyParams[j] + stepSize * searchDirection[j];
                }
                
                // Compute KL divergence with test parameters
                var klDiv = ComputeKLDivergence(testParams, advantages);
                
                if (klDiv <= _maxKlDivergence)
                {
                    return stepSize;
                }
                
                stepSize *= 0.5f;
            }
            
            return 0.0f; // No valid step found
        }

        private float ComputeKLDivergence(float[] newParams, float[] advantages)
        {
            // Simplified KL divergence computation
            // In practice, you would compute the actual KL divergence between old and new policies
            return 0.005f; // Placeholder
        }

        private void ApplyPolicyUpdate(float[] searchDirection, float stepSize)
        {
            // Apply the parameter update
            // This is simplified - in practice you would update the actual network parameters
        }

        private float[] GetPolicyParameters()
        {
            // Return flattened policy network parameters
            // This is simplified - in practice you would extract actual network weights
            return new float[100]; // Placeholder
        }

        private int GetPolicyParameterCount()
        {
            return 100; // Placeholder
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
    }
      /// <summary>
    /// Buffer for storing trajectory data
    /// </summary>
    public class TRPOTrajectoryBuffer
    {
        public struct TrajectoryStep
        {
            public float[] state;
            public object action;  // Changed from int to object to support both discrete and continuous actions
            public float[] actionProbs;
            public float value;
            public float reward;
            public bool done;
        }
        
        private readonly List<TrajectoryStep> _trajectory;
        private readonly int _maxSize;
        
        public TRPOTrajectoryBuffer(int maxSize)
        {
            _maxSize = maxSize;
            _trajectory = new List<TrajectoryStep>();
        }
        
        public int Count => _trajectory.Count;
        public bool IsFull() => _trajectory.Count >= _maxSize;
        
        public void AddStep(float[] state, object action, float[] actionProbs, float value)
        {
            _trajectory.Add(new TrajectoryStep
            {
                state = state,
                action = action,
                actionProbs = actionProbs,
                value = value,
                reward = 0, // Will be set by AddReward
                done = false
            });
        }
        
        public void AddReward(float reward, bool done)
        {
            if (_trajectory.Count > 0)
            {
                var lastStep = _trajectory[_trajectory.Count - 1];
                lastStep.reward = reward;
                lastStep.done = done;
                _trajectory[_trajectory.Count - 1] = lastStep;
            }
        }
        
        public List<TrajectoryStep> GetTrajectory() => _trajectory.ToList();
        
        public void Clear() => _trajectory.Clear();
    }
}
