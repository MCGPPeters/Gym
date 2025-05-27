using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Generative Adversarial Imitation Learning (GAIL) agent plugin
    /// Learns from expert demonstrations using adversarial training
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class GAILAgentPlugin : IAgentPlugin
    {
        public string Name => "GAIL (Generative Adversarial Imitation Learning)";
        public string Description => "Learns from expert demonstrations using adversarial training";
        public object CreateAgent(object env, object? config = null) => new GAILAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((GAILAgent)agent).GetLoss();
    }
      /// <summary>
    /// GAIL agent implementation
    /// </summary>
    public class GAILAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _policyNetwork;
        private SimpleNeuralNetwork _discriminatorNetwork;
        private List<GAILTransition> _trajectoryBuffer;
        private List<GAILTransition> _expertDemonstrations;
        private bool _isDiscrete;
        
        // Hyperparameters
        private readonly float _policyLearningRate;
        private readonly float _discriminatorLearningRate;
        private readonly float _gamma;
        private readonly float _lambda; // GAE parameter
        private readonly float _clipRatio;
        private readonly int _trajectoryLength;
        private readonly int _policyEpochs;
        private readonly int _discriminatorEpochs;
        private readonly int _batchSize;
        private readonly float _entropyCoef;
        
        public GAILAgent(object env) : base(env)
        {            _policyLearningRate = 0.0003f;
            _discriminatorLearningRate = 0.0003f;
            _gamma = 0.99f;
            _lambda = 0.95f;
            _clipRatio = 0.2f;
            _trajectoryLength = 2048;
            _policyEpochs = 10;
            _discriminatorEpochs = 5;
            _batchSize = 64;
            _entropyCoef = 0.01f;
            
            _trajectoryBuffer = new List<GAILTransition>();
            _expertDemonstrations = new List<GAILTransition>();
            
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
            _discriminatorNetwork = new SimpleNeuralNetwork(stateSize + actionSize, 128, 1, _rng); // state + action -> probability
            
            // Load or generate expert demonstrations
            LoadExpertDemonstrations();
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
            }
            
            // Store transition (will be completed in Learn method)
            var transition = new GAILTransition
            {
                state = stateArray,
                action = action,
                actionProbs = actionProbs
            };
            _trajectoryBuffer.Add(transition);
            
            return action;
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            // Complete the last transition
            if (_trajectoryBuffer.Count > 0)
            {
                var lastTransition = _trajectoryBuffer[_trajectoryBuffer.Count - 1];
                lastTransition.nextState = ConvertToFloatArray(nextState);
                lastTransition.reward = (float)reward; // This will be replaced by discriminator reward
                lastTransition.done = done;
                _trajectoryBuffer[_trajectoryBuffer.Count - 1] = lastTransition;
            }
            
            // Update if we have enough data or episode ended
            if (done || _trajectoryBuffer.Count >= _trajectoryLength)
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
            
            // Generate discriminator rewards
            GenerateDiscriminatorRewards();
            
            // Update discriminator
            var discriminatorLoss = UpdateDiscriminator();
            
            // Update policy using PPO with discriminator rewards
            var policyLoss = UpdatePolicy();
            
            _currentLoss = policyLoss + discriminatorLoss;
        }

        private void GenerateDiscriminatorRewards()
        {
            // Replace environment rewards with discriminator rewards
            for (int i = 0; i < _trajectoryBuffer.Count; i++)
            {
                var transition = _trajectoryBuffer[i];
                var discriminatorInput = CreateDiscriminatorInput(transition.state, transition.action);
                var discriminatorOutput = _discriminatorNetwork.Forward(discriminatorInput)[0];
                
                // GAIL reward: log(D(s,a)) - log(1-D(s,a))
                var discriminatorReward = (float)Math.Log(Math.Max(discriminatorOutput, 1e-8f)) - 
                                        (float)Math.Log(Math.Max(1 - discriminatorOutput, 1e-8f));
                
                transition.reward = discriminatorReward;
                _trajectoryBuffer[i] = transition;
            }
        }

        private float UpdateDiscriminator()
        {
            float totalLoss = 0;
            
            for (int epoch = 0; epoch < _discriminatorEpochs; epoch++)
            {
                // Sample from policy trajectories and expert demonstrations
                var policyBatch = SampleBatch(_trajectoryBuffer, _batchSize / 2);
                var expertBatch = SampleBatch(_expertDemonstrations, _batchSize / 2);
                
                float epochLoss = 0;
                
                // Train discriminator to distinguish expert (1) from policy (0)
                foreach (var transition in policyBatch)
                {
                    var input = CreateDiscriminatorInput(transition.state, transition.action);
                    var output = _discriminatorNetwork.Forward(input)[0];
                    var loss = -(float)Math.Log(Math.Max(1 - output, 1e-8f)); // Policy should get label 0
                    epochLoss += loss;
                }
                
                foreach (var transition in expertBatch)
                {
                    var input = CreateDiscriminatorInput(transition.state, transition.action);
                    var output = _discriminatorNetwork.Forward(input)[0];
                    var loss = -(float)Math.Log(Math.Max(output, 1e-8f)); // Expert should get label 1
                    epochLoss += loss;
                }
                
                totalLoss += epochLoss / _batchSize;
            }
            
            return totalLoss / _discriminatorEpochs;
        }

        private float UpdatePolicy()
        {
            // Compute advantages using GAE
            var advantages = ComputeAdvantages();
            var returns = ComputeReturns();
            
            float totalLoss = 0;
            
            for (int epoch = 0; epoch < _policyEpochs; epoch++)
            {
                var batches = CreateMiniBatches(_batchSize);
                
                foreach (var batch in batches)
                {
                    float batchLoss = 0;
                    
                    foreach (var index in batch)
                    {                        var transition = _trajectoryBuffer[index];
                        
                        // Current policy
                        var currentLogits = _policyNetwork.Forward(transition.state);
                        var currentProbs = Softmax(currentLogits);
                        
                        float currentLogProb;
                        float oldLogProb;
                        
                        if (_isDiscrete)
                        {
                            var actionIndex = (int)transition.action;
                            currentLogProb = (float)Math.Log(Math.Max(currentProbs[actionIndex], 1e-8f));
                            oldLogProb = (float)Math.Log(Math.Max(transition.actionProbs[actionIndex], 1e-8f));
                        }
                        else
                        {
                            // For continuous actions, simplified approach
                            currentLogProb = 0.0f; // Placeholder for continuous case
                            oldLogProb = 0.0f; // Placeholder for continuous case
                        }
                        
                        // PPO clipped objective
                        var ratio = (float)Math.Exp(currentLogProb - oldLogProb);
                        var surr1 = ratio * advantages[index];
                        var surr2 = Math.Max(1 - _clipRatio, Math.Min(1 + _clipRatio, ratio)) * advantages[index];
                        var policyLoss = -Math.Min(surr1, surr2);
                        
                        // Entropy bonus
                        float entropy = 0;
                        for (int i = 0; i < currentProbs.Length; i++)
                        {
                            entropy -= currentProbs[i] * (float)Math.Log(Math.Max(currentProbs[i], 1e-8f));
                        }
                        
                        batchLoss += policyLoss - _entropyCoef * entropy;
                    }
                    
                    totalLoss += batchLoss / batch.Count;
                }
            }
            
            return totalLoss / _policyEpochs;
        }

        private float[] ComputeAdvantages()
        {
            var advantages = new float[_trajectoryBuffer.Count];
            
            float gae = 0;
            for (int i = _trajectoryBuffer.Count - 1; i >= 0; i--)
            {
                var delta = _trajectoryBuffer[i].reward;
                if (i < _trajectoryBuffer.Count - 1)
                {
                    // Simplified value estimation (no separate value network for this example)
                    delta += _gamma * 0 - 0; // Would use actual value estimates
                }
                
                gae = delta + _gamma * _lambda * gae * (_trajectoryBuffer[i].done ? 0 : 1);
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
            var returns = new float[_trajectoryBuffer.Count];
            
            float returnValue = 0;
            for (int i = _trajectoryBuffer.Count - 1; i >= 0; i--)
            {
                returnValue = _trajectoryBuffer[i].reward + _gamma * returnValue * (_trajectoryBuffer[i].done ? 0 : 1);
                returns[i] = returnValue;
            }
            
            return returns;
        }        private float[] CreateDiscriminatorInput(float[] state, object action)
        {
            if (_isDiscrete)
            {
                var actionIndex = (int)action;
                var input = new float[state.Length + 1];
                Array.Copy(state, 0, input, 0, state.Length);
                input[state.Length] = actionIndex; // One-hot encoding would be better
                return input;
            }
            else
            {
                // For continuous actions
                if (action is float singleAction)
                {
                    var input = new float[state.Length + 1];
                    Array.Copy(state, 0, input, 0, state.Length);
                    input[state.Length] = singleAction;
                    return input;
                }
                else if (action is float[] actionArray)
                {
                    var input = new float[state.Length + actionArray.Length];
                    Array.Copy(state, 0, input, 0, state.Length);
                    Array.Copy(actionArray, 0, input, state.Length, actionArray.Length);
                    return input;
                }
                else
                {
                    throw new ArgumentException($"Unsupported action type: {action.GetType()}");
                }
            }
        }

        private List<GAILTransition> SampleBatch(List<GAILTransition> source, int batchSize)
        {
            var batch = new List<GAILTransition>();
            for (int i = 0; i < batchSize && i < source.Count; i++)
            {
                var index = _rng.Next(source.Count);
                batch.Add(source[index]);
            }
            return batch;
        }

        private List<List<int>> CreateMiniBatches(int batchSize)
        {
            var indices = Enumerable.Range(0, _trajectoryBuffer.Count).ToList();
            var batches = new List<List<int>>();
            
            for (int i = 0; i < indices.Count; i += batchSize)
            {
                var batch = indices.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }
            
            return batches;
        }

        private void LoadExpertDemonstrations()
        {
            // Load expert demonstrations from file or generate synthetic ones
            // For this example, generate some random expert demonstrations
            for (int i = 0; i < 1000; i++)
            {
                var expertTransition = new GAILTransition
                {
                    state = GenerateRandomState(),
                    action = _rng.Next(4), // Assuming 4 actions
                    actionProbs = new float[] { 0.25f, 0.25f, 0.25f, 0.25f },
                    reward = 1.0f,
                    done = false
                };
                _expertDemonstrations.Add(expertTransition);
            }
        }

        private float[] GenerateRandomState()
        {
            // Generate a random state for demonstration purposes
            return new float[] { (float)_rng.NextDouble(), (float)_rng.NextDouble(), (float)_rng.NextDouble() };
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
    /// Transition structure for GAIL
    /// </summary>
    public struct GAILTransition
    {
        public float[] state;
        public object action;  // Changed from int to object to support both discrete and continuous actions
        public float[] actionProbs;
        public float[] nextState;
        public float reward;
        public bool done;
    }
}
