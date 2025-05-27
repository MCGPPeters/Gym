using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Actor-Critic with Experience Replay (ACER) agent plugin
    /// Combines on-policy learning with off-policy experience replay
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class ACERAgentPlugin : IAgentPlugin
    {
        public string Name => "ACER (Actor-Critic with Experience Replay)";
        public string Description => "Combines on-policy learning with off-policy experience replay for sample efficiency";
        public object CreateAgent(object env, object? config = null) => new ACERAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((ACERAgent)agent).GetLoss();
    }
      /// <summary>
    /// Actor-Critic with Experience Replay (ACER) agent implementation
    /// </summary>
    public class ACERAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _actorNetwork;
        private SimpleNeuralNetwork _criticNetwork;
        private List<ACERExperience> _replayBuffer;
        private List<ACERExperience> _onPolicyBuffer;
        private bool _isDiscrete;
        
        // Hyperparameters
        private readonly float _learningRate;
        private readonly float _gamma;
        private readonly float _lambda; // GAE parameter
        private readonly float _c; // Truncation parameter
        private readonly float _alpha; // Trust region parameter
        private readonly int _batchSize;
        private readonly int _bufferSize;
        private readonly int _onPolicySteps;
        private readonly float _offPolicyWeight;
        
        public ACERAgent(object env) : base(env)
        {            _learningRate = 0.001f;
            _gamma = 0.99f;
            _lambda = 0.95f;
            _c = 10.0f;
            _alpha = 0.99f;
            _batchSize = 32;
            _bufferSize = 50000;
            _onPolicySteps = 20;
            _offPolicyWeight = 0.5f;
            
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
            
            // Initialize buffers
            _replayBuffer = new List<ACERExperience>();
            _onPolicyBuffer = new List<ACERExperience>();        }

        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            
            // Get action probabilities from actor network
            var actionLogits = _actorNetwork.Forward(stateArray);
            
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
            }
            
            // Store current policy for experience
            var experience = new ACERExperience
            {
                state = stateArray,
                action = action,
                actionProbs = actionProbs,
                value = _criticNetwork.Forward(stateArray)[0]
            };
            
            _onPolicyBuffer.Add(experience);
            
            return action;
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            // Update the last experience with reward and next state
            if (_onPolicyBuffer.Count > 0)
            {
                var lastExp = _onPolicyBuffer[_onPolicyBuffer.Count - 1];
                lastExp.reward = (float)reward;
                lastExp.nextState = ConvertToFloatArray(nextState);
                lastExp.done = done;                _onPolicyBuffer[_onPolicyBuffer.Count - 1] = lastExp;
                  // Add to replay buffer
                _replayBuffer.Add(lastExp);
                
                // Maintain buffer size
                if (_replayBuffer.Count > _bufferSize)
                {
                    _replayBuffer.RemoveAt(0);
                }
            }
            
            // Update if we have enough on-policy data or episode ended
            if (done || _onPolicyBuffer.Count >= _onPolicySteps)
            {
                Update();
                _onPolicyBuffer.Clear();
            }
        }

        public override void Reset()
        {
            _onPolicyBuffer.Clear();
        }

        public double GetLoss() => _currentLoss;

        private void Update()
        {
            if (_onPolicyBuffer.Count == 0) return;
            
            // On-policy update
            var onPolicyLoss = UpdateOnPolicy();
            
            // Off-policy update using replay buffer
            var offPolicyLoss = UpdateOffPolicy();
            
            _currentLoss = onPolicyLoss + _offPolicyWeight * offPolicyLoss;
        }

        private float UpdateOnPolicy()
        {
            var advantages = ComputeAdvantages(_onPolicyBuffer);
            var returns = ComputeReturns(_onPolicyBuffer);
            
            float totalLoss = 0;
            
            for (int i = 0; i < _onPolicyBuffer.Count; i++)
            {                var exp = _onPolicyBuffer[i];
                
                // Current policy
                var currentLogits = _actorNetwork.Forward(exp.state);
                var currentProbs = Softmax(currentLogits);
                
                float currentLogProb;
                float oldLogProb;
                
                if (_isDiscrete)
                {
                    var actionIndex = (int)exp.action;
                    currentLogProb = (float)Math.Log(Math.Max(currentProbs[actionIndex], 1e-8f));
                    oldLogProb = (float)Math.Log(Math.Max(exp.actionProbs[actionIndex], 1e-8f));
                }
                else
                {
                    // For continuous actions, use a different approach for computing log probabilities
                    // This is a simplified version - in practice, you'd use the actual policy distribution
                    currentLogProb = 0.0f; // Placeholder for continuous case
                    oldLogProb = 0.0f; // Placeholder for continuous case
                }
                
                // Importance sampling ratio
                var rho = (float)Math.Exp(currentLogProb - oldLogProb);
                
                // Truncated importance sampling
                var truncatedRho = Math.Min(_c, rho);
                
                // Actor loss (ACER objective)
                var actorLoss = -truncatedRho * advantages[i] * currentLogProb;
                
                // Critic loss
                var valueEst = _criticNetwork.Forward(exp.state)[0];
                var criticLoss = (returns[i] - valueEst) * (returns[i] - valueEst);
                
                totalLoss += actorLoss + criticLoss;
            }
            
            return totalLoss / _onPolicyBuffer.Count;
        }

        private float UpdateOffPolicy()        {
            if (_replayBuffer.Count < _batchSize) return 0;
            
            var batch = _replayBuffer.OrderBy(x => _rng.Next()).Take(_batchSize).ToArray();
            float totalLoss = 0;
            
            foreach (var exp in batch)
            {
                // Q-learning style update for off-policy data
                var qValue = _criticNetwork.Forward(exp.state)[0];
                var nextQValue = exp.nextState != null ? _criticNetwork.Forward(exp.nextState)[0] : 0;
                var target = exp.reward + _gamma * nextQValue;
                
                var tdError = target - qValue;
                totalLoss += tdError * tdError;
            }
            
            return totalLoss / batch.Length;
        }

        private float[] ComputeAdvantages(List<ACERExperience> experiences)
        {
            var advantages = new float[experiences.Count];
            
            float gae = 0;
            for (int i = experiences.Count - 1; i >= 0; i--)
            {
                var delta = experiences[i].reward;
                if (i < experiences.Count - 1)
                {
                    delta += _gamma * experiences[i + 1].value - experiences[i].value;
                }
                else
                {
                    delta -= experiences[i].value;
                }
                
                gae = delta + _gamma * _lambda * gae * (experiences[i].done ? 0 : 1);
                advantages[i] = gae;
            }
            
            return advantages;
        }

        private float[] ComputeReturns(List<ACERExperience> experiences)
        {
            var returns = new float[experiences.Count];
            
            float returnValue = 0;
            for (int i = experiences.Count - 1; i >= 0; i--)
            {
                returnValue = experiences[i].reward + _gamma * returnValue * (experiences[i].done ? 0 : 1);
                returns[i] = returnValue;
            }
            
            return returns;
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
    /// Experience structure for ACER algorithm
    /// </summary>
    public struct ACERExperience
    {
        public float[] state;
        public object action;  // Changed from int to object to support both discrete and continuous actions
        public float[] actionProbs;
        public float value;
        public float reward;
        public float[] nextState;
        public bool done;
    }
}
