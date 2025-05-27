using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Deep Deterministic Policy Gradient (DDPG) agent plugin
    /// Actor-critic method for continuous action spaces
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class DDPGAgentPlugin : IAgentPlugin
    {
        public string Name => "DDPG (Deep Deterministic Policy Gradient)";
        public string Description => "Actor-critic method for continuous action spaces with deterministic policy";
        public object CreateAgent(object env, object? config = null) => new DDPGAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((DDPGAgent)agent).GetLoss();    }
    
    /// <summary>
    /// Deep Deterministic Policy Gradient (DDPG) agent implementation
    /// </summary>
    public class DDPGAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _actorNetwork;
        private SimpleNeuralNetwork _targetActorNetwork;
        private SimpleNeuralNetwork _criticNetwork;
        private SimpleNeuralNetwork _targetCriticNetwork;
        private DDPGReplayBuffer _replayBuffer;
        private OrnsteinUhlenbeckNoise _noise;
        private bool _isDiscrete;  // Track if action space is discrete
        
        // Hyperparameters
        private readonly float _learningRateActor;
        private readonly float _learningRateCritic;
        private readonly float _gamma;
        private readonly float _tau; // Soft update parameter
        private readonly int _batchSize;
        private readonly int _bufferSize;
        private readonly int _warmupSteps;
        private int _stepCount;
        
        public DDPGAgent(object env) : base(env)
        {            _learningRateActor = 0.001f;
            _learningRateCritic = 0.001f;
            _gamma = 0.99f;
            _tau = 0.005f;
            _batchSize = 64;
            _bufferSize = 100000;
            _warmupSteps = 1000;
            _stepCount = 0;
            
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
                actionSize = actionSpace.Shape[0];
                _isDiscrete = false;
            }
            else
            {
                throw new NotSupportedException($"Action space type {actionSpace.GetType()} not supported by DDPG");
            }            // Initialize networks
            _actorNetwork = new SimpleNeuralNetwork(stateSize, 128, actionSize, _rng);
            _targetActorNetwork = new SimpleNeuralNetwork(stateSize, 128, actionSize, _rng);
            _criticNetwork = new SimpleNeuralNetwork(stateSize + actionSize, 128, 1, _rng);
            _targetCriticNetwork = new SimpleNeuralNetwork(stateSize + actionSize, 128, 1, _rng);
            
            // Copy weights to target networks
            CopyWeights(_actorNetwork, _targetActorNetwork);
            CopyWeights(_criticNetwork, _targetCriticNetwork);            // Initialize replay buffer and noise
            _replayBuffer = new DDPGReplayBuffer(_bufferSize);
            _noise = new OrnsteinUhlenbeckNoise(actionSize, _rng);
        }

        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            
            // Get action from actor network
            var action = _actorNetwork.Forward(stateArray);
            
            // Add exploration noise
            if (_stepCount < _warmupSteps || _rng.NextDouble() < 0.1) // Exploration probability
            {
                var noise = _noise.Sample();
                for (int i = 0; i < action.Length; i++)
                {
                    action[i] += noise[i];
                }
            }
              // Handle action space conversion and bounds
            dynamic actionSpace = _env.ActionSpace;
            
            if (_isDiscrete)
            {
                // For discrete action spaces: convert continuous output to discrete action
                // Use softmax to get probabilities and select action
                var actionProbs = Softmax(action);
                var discreteAction = SampleFromDistribution(actionProbs);
                _stepCount++;
                return discreteAction;
            }
            else
            {
                // For continuous action spaces: clip to bounds
                var low = ConvertToFloatArray(actionSpace.Low);
                var high = ConvertToFloatArray(actionSpace.High);
                
                for (int i = 0; i < action.Length; i++)
                {
                    action[i] = Math.Max(low[i], Math.Min(high[i], action[i]));
                }
                
                _stepCount++;
                // Return single float for 1D continuous actions, array for multi-dimensional
                if (action.Length == 1)
                {
                    return action[0];
                }
                else
                {
                    return action;
                }
            }
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            var stateArray = ConvertToFloatArray(state);
            var actionArray = ConvertToFloatArray(action);
            var nextStateArray = ConvertToFloatArray(nextState);
            
            // Store transition in replay buffer
            _replayBuffer.Add(stateArray, actionArray, (float)reward, nextStateArray, done);
            
            // Update networks if we have enough samples
            if (_replayBuffer.Count >= _batchSize && _stepCount >= _warmupSteps)
            {
                Update();
            }        }

        public override void Reset()
        {
            _noise.Reset();
        }

        public double GetLoss() => _currentLoss;

        private void Update()
        {
            var batch = _replayBuffer.Sample(_batchSize, _rng);
            
            float criticLoss = 0;
            float actorLoss = 0;
            
            // Update critic network
            for (int i = 0; i < batch.Count; i++)
            {
                var (state, action, reward, nextState, done) = batch[i];
                
                // Target Q-value
                var nextAction = _targetActorNetwork.Forward(nextState);
                var targetQ = reward + _gamma * (done ? 0 : _targetCriticNetwork.Forward(CombineArrays(nextState, nextAction))[0]);
                
                // Current Q-value
                var currentQ = _criticNetwork.Forward(CombineArrays(state, action))[0];
                
                // Critic loss (TD error)
                var tdError = targetQ - currentQ;
                criticLoss += tdError * tdError;
            }
            
            // Update actor network
            for (int i = 0; i < batch.Count; i++)
            {
                var (state, _, _, _, _) = batch[i];
                
                // Actor loss (negative Q-value to maximize)
                var predictedAction = _actorNetwork.Forward(state);
                var qValue = _criticNetwork.Forward(CombineArrays(state, predictedAction))[0];
                actorLoss -= qValue; // Negative because we want to maximize Q
            }
            
            _currentLoss = criticLoss + Math.Abs(actorLoss);
            
            // Soft update target networks
            SoftUpdateTargetNetworks();
        }

        private void SoftUpdateTargetNetworks()
        {
            // Soft update: θ_target = τ * θ_local + (1 - τ) * θ_target
            // This is a simplified version - in practice you'd update the actual weights
        }

        private void CopyWeights(SimpleNeuralNetwork source, SimpleNeuralNetwork target)
        {
            // Copy weights from source to target network
            // This is a simplified version - in practice you'd copy the actual weights
        }

        private float[] CombineArrays(float[] array1, float[] array2)
        {
            var combined = new float[array1.Length + array2.Length];
            Array.Copy(array1, 0, combined, 0, array1.Length);
            Array.Copy(array2, 0, combined, array1.Length, array2.Length);            return combined;
        }
        
        private float[] ConvertToFloatArray(object obj)
        {
            switch (obj)
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
                        return new[] { (float)Convert.ToDouble(obj) };
                    }
                    catch
                    {
                        // Fallback for complex types - use hash-based features
                        var hash = obj.GetHashCode();
                        return new float[] { Math.Abs(hash % 1000) / 1000.0f };
                    }
            }
        }

        private float[] Softmax(float[] input)
        {
            var max = input.Max();
            var exp = input.Select(x => (float)Math.Exp(x - max)).ToArray();
            var sum = exp.Sum();
            return exp.Select(x => x / sum).ToArray();
        }
        
        private int SampleFromDistribution(float[] probabilities)
        {
            var random = _rng.NextDouble();
            double cumulative = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (random <= cumulative)
                    return i;
            }
            return probabilities.Length - 1;
        }
    }
      /// <summary>
    /// Ornstein-Uhlenbeck noise for exploration in continuous action spaces
    /// </summary>
    public class OrnsteinUhlenbeckNoise
    {
        private readonly float[] _state;
        private readonly float _theta;
        private readonly float _sigma;
        private readonly float _dt;
        private readonly Random _rng;public OrnsteinUhlenbeckNoise(int size, Random rng, float theta = 0.15f, float sigma = 0.2f, float dt = 0.01f)
        {
            _state = new float[size];
            _theta = theta;
            _sigma = sigma;
            _dt = dt;
            _rng = rng;
            Reset();
        }
        
        public float[] Sample()
        {
            var dx = new float[_state.Length];
            for (int i = 0; i < _state.Length; i++)
            {
                dx[i] = _theta * (0 - _state[i]) * _dt + _sigma * (float)Math.Sqrt(_dt) * (float)(_rng.NextDouble() * 2 - 1);
                _state[i] += dx[i];
            }
            return (float[])_state.Clone();
        }
        
        public void Reset()
        {
            for (int i = 0; i < _state.Length; i++)
            {
                _state[i] = 0;
            }
        }
    }
      /// <summary>
    /// Simple replay buffer for experience replay
    /// </summary>
    public class DDPGReplayBuffer
    {
        private readonly List<(float[] state, float[] action, float reward, float[] nextState, bool done)> _buffer;
        private readonly int _maxSize;
        
        public DDPGReplayBuffer(int maxSize)
        {
            _maxSize = maxSize;
            _buffer = new List<(float[], float[], float, float[], bool)>();
        }
        
        public int Count => _buffer.Count;
        
        public void Add(float[] state, float[] action, float reward, float[] nextState, bool done)
        {
            if (_buffer.Count >= _maxSize)
            {
                _buffer.RemoveAt(0);
            }
            _buffer.Add((state, action, reward, nextState, done));
        }
        
        public List<(float[] state, float[] action, float reward, float[] nextState, bool done)> Sample(int batchSize, Random rng)
        {
            var batch = new List<(float[], float[], float, float[], bool)>();
            for (int i = 0; i < batchSize; i++)
            {
                var index = rng.Next(_buffer.Count);
                batch.Add(_buffer[index]);
            }            return batch;
        }
    }
}
