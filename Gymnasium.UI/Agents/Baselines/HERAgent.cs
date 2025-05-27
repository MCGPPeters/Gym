using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Gymnasium.UI.Models;

namespace Gymnasium.UI.Agents.Baselines
{
    /// <summary>
    /// Hindsight Experience Replay (HER) agent plugin
    /// For goal-conditioned reinforcement learning
    /// </summary>
    [Export(typeof(IAgentPlugin))]
    public class HERAgentPlugin : IAgentPlugin
    {
        public string Name => "HER (Hindsight Experience Replay)";
        public string Description => "Goal-conditioned learning with hindsight experience replay";
        public object CreateAgent(object env, object? config = null) => new HERAgent(env);
        public Func<double>? GetLossFetcher(object agent) => () => ((HERAgent)agent).GetLoss();
    }
      /// <summary>
    /// HER agent implementation
    /// </summary>
    public class HERAgent : BaselineAgent
    {
        private SimpleNeuralNetwork _qNetwork;
        private SimpleNeuralNetwork _targetQNetwork;
        private HERReplayBuffer _replayBuffer;
        private bool _isDiscrete;
        
        // Hyperparameters
        private readonly float _learningRate;
        private readonly float _gamma;
        private float _epsilon;
        private readonly float _epsilonDecay;
        private readonly float _epsilonMin;
        private readonly int _batchSize;
        private readonly int _bufferSize;
        private readonly int _targetUpdateFreq;
        private readonly int _warmupSteps;
        private readonly float _futureRewardRatio;
        
        // Goal-related parameters
        private float[] _currentGoal;
        private List<HERTransition> _currentEpisode;
        private int _stepCount;
        private int _updateCount;
        
        public HERAgent(object env) : base(env)
        {            _learningRate = 0.001f;
            _gamma = 0.98f;
            _epsilon = 1.0f;
            _epsilonDecay = 0.995f;
            _epsilonMin = 0.02f;
            _batchSize = 256;
            _bufferSize = 1000000;
            _targetUpdateFreq = 40;
            _warmupSteps = 1000;
            _futureRewardRatio = 0.8f;
            
            _currentEpisode = new List<HERTransition>();
            _stepCount = 0;
            _updateCount = 0;
            
            Initialize();
        }        private void Initialize()
        {
            // Get environment dimensions
            dynamic actionSpace = _env.ActionSpace;
            dynamic observationSpace = _env.ObservationSpace;
            
            int stateSize = observationSpace.Shape[0]; // Assuming state includes goal
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
            
            // Initialize Q-networks (state+goal -> Q-values)
            _qNetwork = new SimpleNeuralNetwork(stateSize, 256, actionSize, _rng);
            _targetQNetwork = new SimpleNeuralNetwork(stateSize, 256, actionSize, _rng);
            
            // Copy weights to target network
            CopyWeights(_qNetwork, _targetQNetwork);
            
            // Initialize HER replay buffer
            _replayBuffer = new HERReplayBuffer(_bufferSize);
            
            // Sample initial goal
            _currentGoal = SampleGoal();
        }        public override object Act(object state)
        {
            var stateArray = ConvertToFloatArray(state);
            var stateWithGoal = CombineStateAndGoal(stateArray, _currentGoal);
            
            object action;
              // Epsilon-greedy action selection
            if (_rng.NextDouble() < _epsilon)
            {
                // Random action
                dynamic actionSpace = _env.ActionSpace;
                if (_isDiscrete)
                {
                    action = _rng.Next(GetActionSpaceSize(actionSpace));
                }
                else
                {
                    // Random continuous action
                    var actionSize = GetActionSpaceSize(actionSpace);
                    if (actionSize == 1)
                    {
                        action = (float)(_rng.NextDouble() * 2.0 - 1.0); // [-1, 1]
                    }
                    else
                    {
                        var continuousAction = new float[actionSize];
                        for (int i = 0; i < actionSize; i++)
                        {
                            continuousAction[i] = (float)(_rng.NextDouble() * 2.0 - 1.0); // [-1, 1]
                        }
                        action = continuousAction;
                    }
                }
            }
            else
            {
                // Greedy action
                var qValues = _qNetwork.Forward(stateWithGoal);
                if (_isDiscrete)
                {
                    action = Array.IndexOf(qValues, qValues.Max());
                }
                else
                {
                    // For continuous actions, use Q-values directly with some noise
                    if (qValues.Length == 1)
                    {
                        action = Math.Max(-1f, Math.Min(1f, qValues[0])); // Clamp to [-1, 1]
                    }
                    else
                    {
                        var continuousAction = new float[qValues.Length];
                        for (int i = 0; i < qValues.Length; i++)
                        {
                            continuousAction[i] = Math.Max(-1f, Math.Min(1f, qValues[i])); // Clamp to [-1, 1]
                        }
                        action = continuousAction;
                    }
                }
            }
            
            // Store transition (will be completed in Learn method)
            var transition = new HERTransition
            {
                state = stateArray,
                action = action,
                goal = (float[])_currentGoal.Clone()
            };
            _currentEpisode.Add(transition);
            
            _stepCount++;
            return action;
        }

        public override void Learn(object state, object action, double reward, object nextState, bool done)
        {
            var nextStateArray = ConvertToFloatArray(nextState);
            
            // Complete the last transition
            if (_currentEpisode.Count > 0)
            {
                var lastTransition = _currentEpisode[_currentEpisode.Count - 1];
                lastTransition.nextState = nextStateArray;
                lastTransition.reward = (float)reward;
                lastTransition.done = done;
                _currentEpisode[_currentEpisode.Count - 1] = lastTransition;
            }
            
            // If episode ended, process with HER
            if (done)
            {
                ProcessEpisodeWithHER();
                _currentEpisode.Clear();
                _currentGoal = SampleGoal(); // Sample new goal for next episode
                
                // Decay epsilon
                _epsilon = Math.Max(_epsilonMin, _epsilon * _epsilonDecay);
            }
            
            // Update networks if we have enough data
            if (_replayBuffer.Count >= _batchSize && _stepCount >= _warmupSteps)
            {
                Update();
                
                // Update target network periodically
                if (_updateCount % _targetUpdateFreq == 0)
                {
                    CopyWeights(_qNetwork, _targetQNetwork);
                }
                _updateCount++;
            }
        }

        public override void Reset()
        {
            _currentEpisode.Clear();
            _currentGoal = SampleGoal();
        }

        public double GetLoss() => _currentLoss;

        private void ProcessEpisodeWithHER()
        {
            // Add original episode transitions
            foreach (var transition in _currentEpisode)
            {
                _replayBuffer.Add(transition);
            }
            
            // Add HER transitions with future states as goals
            for (int i = 0; i < _currentEpisode.Count; i++)
            {
                // Sample future states as hindsight goals
                for (int k = i; k < _currentEpisode.Count; k++)
                {
                    if (_rng.NextDouble() < _futureRewardRatio)
                    {
                        var herTransition = new HERTransition
                        {
                            state = _currentEpisode[i].state,
                            action = _currentEpisode[i].action,
                            nextState = _currentEpisode[i].nextState,
                            goal = ExtractGoalFromState(_currentEpisode[k].nextState), // Use future state as goal
                            reward = ComputeReward(_currentEpisode[i].nextState, ExtractGoalFromState(_currentEpisode[k].nextState)),
                            done = _currentEpisode[i].done
                        };
                        
                        _replayBuffer.Add(herTransition);
                    }
                }
            }
        }

        private void Update()
        {
            var batch = _replayBuffer.Sample(_batchSize, _rng);
            float totalLoss = 0;
            
            foreach (var transition in batch)
            {
                var stateWithGoal = CombineStateAndGoal(transition.state, transition.goal);
                var nextStateWithGoal = CombineStateAndGoal(transition.nextState, transition.goal);                
                // Current Q-value
                var qValues = _qNetwork.Forward(stateWithGoal);
                float currentQ;
                
                if (_isDiscrete)
                {
                    var actionIndex = (int)transition.action;
                    currentQ = qValues[actionIndex];
                }
                else
                {
                    // For continuous actions, use first Q-value as approximation
                    currentQ = qValues[0]; // Simplified for continuous case
                }
                
                // Target Q-value
                var nextQValues = _targetQNetwork.Forward(nextStateWithGoal);
                var maxNextQ = nextQValues.Max();
                var targetQ = transition.reward + _gamma * (transition.done ? 0 : maxNextQ);
                
                // TD error
                var tdError = targetQ - currentQ;
                totalLoss += tdError * tdError;
            }
            
            _currentLoss = totalLoss / batch.Count;
            
            // Apply gradients (simplified - in practice use proper backpropagation)
        }

        private float[] SampleGoal()
        {
            // Sample a random goal from the goal space
            // This is environment-specific - for now return a random vector
            return new float[] { (float)_rng.NextDouble(), (float)_rng.NextDouble() };
        }

        private float[] ExtractGoalFromState(float[] state)
        {
            // Extract goal information from state
            // This is environment-specific - for now return last 2 elements
            if (state.Length >= 2)
                return state.Skip(state.Length - 2).ToArray();
            return state;
        }

        private float[] CombineStateAndGoal(float[] state, float[] goal)
        {
            var combined = new float[state.Length + goal.Length];
            Array.Copy(state, 0, combined, 0, state.Length);
            Array.Copy(goal, 0, combined, state.Length, goal.Length);
            return combined;
        }

        private float ComputeReward(float[] achievedGoal, float[] desiredGoal)
        {
            // Compute reward based on goal achievement
            // This is environment-specific - for now use negative distance
            float distance = 0;
            for (int i = 0; i < Math.Min(achievedGoal.Length, desiredGoal.Length); i++)
            {
                distance += (achievedGoal[i] - desiredGoal[i]) * (achievedGoal[i] - desiredGoal[i]);
            }
            
            return distance < 0.05f ? 0.0f : -1.0f; // Sparse reward
        }

        private void CopyWeights(SimpleNeuralNetwork source, SimpleNeuralNetwork target)
        {
            // Copy weights from source to target network
            // This is simplified - in practice you'd copy the actual weights
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
    /// Transition structure for HER
    /// </summary>
    public struct HERTransition
    {
        public float[] state;
        public object action;  // Changed from int to object to support both discrete and continuous actions
        public float[] nextState;
        public float[] goal;
        public float reward;
        public bool done;
    }
    
    /// <summary>
    /// Replay buffer specialized for HER
    /// </summary>
    public class HERReplayBuffer
    {
        private readonly List<HERTransition> _buffer;
        private readonly int _maxSize;
        
        public HERReplayBuffer(int maxSize)
        {
            _maxSize = maxSize;
            _buffer = new List<HERTransition>();
        }
        
        public int Count => _buffer.Count;
        
        public void Add(HERTransition transition)
        {
            if (_buffer.Count >= _maxSize)
            {
                _buffer.RemoveAt(0);
            }
            _buffer.Add(transition);
        }
        
        public List<HERTransition> Sample(int batchSize, Random rng)
        {
            var batch = new List<HERTransition>();
            for (int i = 0; i < batchSize; i++)
            {
                var index = rng.Next(_buffer.Count);
                batch.Add(_buffer[index]);
            }
            return batch;
        }
    }
}
