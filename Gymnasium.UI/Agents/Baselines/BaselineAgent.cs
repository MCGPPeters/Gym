using System;
using System.Collections.Generic;
using System.Linq;

namespace Gymnasium.UI.Agents.Baselines;

/// <summary>
/// Base class for all baseline reinforcement learning agents
/// </summary>
public abstract class BaselineAgent
{
    protected readonly dynamic _env;
    protected readonly Random _rng;
    protected double _currentLoss;
    protected int _stepCount;
    protected int _episodeCount;
    
    protected BaselineAgent(object env, int? seed = null)
    {
        _env = env;
        _rng = new Random(seed ?? Environment.TickCount);
        _currentLoss = 0.0;
        _stepCount = 0;
        _episodeCount = 0;
    }
    
    public abstract object Act(object state);
    public abstract void Learn(object state, object action, double reward, object nextState, bool done);
    public abstract void Reset();
      public virtual double GetLoss() => _currentLoss;
    public virtual int GetStepCount() => _stepCount;
    public virtual int GetEpisodeCount() => _episodeCount;
    
    /// <summary>
    /// Helper method to get action space size that works for both Discrete and Box spaces
    /// </summary>
    protected int GetActionSpaceSize(dynamic actionSpace)
    {
        if (actionSpace is Gymnasium.Spaces.Discrete)
        {
            return actionSpace.N;
        }
        else if (actionSpace is Gymnasium.Spaces.Box)
        {
            return actionSpace.Dimension;
        }
        else
        {
            throw new NotSupportedException($"Action space type {actionSpace.GetType()} not supported");
        }
    }
    
    /// <summary>
    /// Helper method to check if action space is discrete
    /// </summary>
    protected bool IsActionSpaceDiscrete(dynamic actionSpace)
    {
        return actionSpace is Gymnasium.Spaces.Discrete;
    }
}

/// <summary>
/// Simple neural network implementation for baseline agents
/// </summary>
public class SimpleNeuralNetwork
{
    private readonly float[,] _weights1;
    private readonly float[] _biases1;
    private readonly float[,] _weights2;
    private readonly float[] _biases2;
    private readonly int _inputSize;
    private readonly int _hiddenSize;
    private readonly int _outputSize;
    private readonly Random _rng;
    
    public SimpleNeuralNetwork(int inputSize, int hiddenSize, int outputSize, Random rng)
    {
        _inputSize = inputSize;
        _hiddenSize = hiddenSize;
        _outputSize = outputSize;
        _rng = rng;
        
        // Initialize weights with Xavier initialization
        float xavier1 = (float)Math.Sqrt(2.0 / inputSize);
        float xavier2 = (float)Math.Sqrt(2.0 / hiddenSize);
        
        _weights1 = new float[inputSize, hiddenSize];
        _biases1 = new float[hiddenSize];
        _weights2 = new float[hiddenSize, outputSize];
        _biases2 = new float[outputSize];
        
        // Initialize weights
        for (int i = 0; i < inputSize; i++)
            for (int j = 0; j < hiddenSize; j++)
                _weights1[i, j] = (float)(_rng.NextGaussian() * xavier1);
                
        for (int i = 0; i < hiddenSize; i++)
            for (int j = 0; j < outputSize; j++)
                _weights2[i, j] = (float)(_rng.NextGaussian() * xavier2);
    }
    
    public float[] Forward(float[] input)
    {
        if (input.Length != _inputSize)
            throw new ArgumentException($"Input size mismatch. Expected {_inputSize}, got {input.Length}");
            
        // Hidden layer
        var hidden = new float[_hiddenSize];
        for (int j = 0; j < _hiddenSize; j++)
        {
            float sum = _biases1[j];
            for (int i = 0; i < _inputSize; i++)
                sum += input[i] * _weights1[i, j];
            hidden[j] = ReLU(sum);
        }
        
        // Output layer
        var output = new float[_outputSize];
        for (int j = 0; j < _outputSize; j++)
        {
            float sum = _biases2[j];
            for (int i = 0; i < _hiddenSize; i++)
                sum += hidden[i] * _weights2[i, j];
            output[j] = sum;
        }
        
        return output;
    }
    
    public void UpdateWeights(float[] input, float[] targetOutput, float learningRate)
    {
        // Simple gradient descent update (simplified for demonstration)
        var output = Forward(input);
        var outputError = new float[_outputSize];
        
        for (int i = 0; i < _outputSize; i++)
            outputError[i] = targetOutput[i] - output[i];
            
        // Update output layer weights (simplified)
        var hidden = ComputeHidden(input);
        for (int i = 0; i < _hiddenSize; i++)
        {
            for (int j = 0; j < _outputSize; j++)
            {
                _weights2[i, j] += learningRate * outputError[j] * hidden[i];
            }
        }
        
        // Update biases
        for (int i = 0; i < _outputSize; i++)
            _biases2[i] += learningRate * outputError[i];
    }
    
    private float[] ComputeHidden(float[] input)
    {
        var hidden = new float[_hiddenSize];
        for (int j = 0; j < _hiddenSize; j++)
        {
            float sum = _biases1[j];
            for (int i = 0; i < _inputSize; i++)
                sum += input[i] * _weights1[i, j];
            hidden[j] = ReLU(sum);
        }
        return hidden;
    }
    
    private static float ReLU(float x) => Math.Max(0, x);
}

/// <summary>
/// Experience replay buffer for off-policy algorithms
/// </summary>
public class ReplayBuffer
{
    private readonly int _capacity;
    private readonly List<Experience> _buffer;
    private int _position;
    private readonly Random _rng;
    
    public ReplayBuffer(int capacity, Random rng)
    {
        _capacity = capacity;
        _buffer = new List<Experience>(capacity);
        _position = 0;
        _rng = rng;
    }
    
    public void Add(object state, object action, double reward, object nextState, bool done)
    {
        var experience = new Experience(state, action, reward, nextState, done);
        
        if (_buffer.Count < _capacity)
        {
            _buffer.Add(experience);
        }
        else
        {
            _buffer[_position] = experience;
            _position = (_position + 1) % _capacity;
        }
    }
    
    public Experience[] Sample(int batchSize)
    {
        if (_buffer.Count < batchSize)
            batchSize = _buffer.Count;
            
        var indices = Enumerable.Range(0, _buffer.Count)
            .OrderBy(x => _rng.Next())
            .Take(batchSize)
            .ToArray();
            
        return indices.Select(i => _buffer[i]).ToArray();
    }
    
    public int Count => _buffer.Count;
}

/// <summary>
/// Experience tuple for replay buffer
/// </summary>
public record Experience(object State, object Action, double Reward, object NextState, bool Done);

/// <summary>
/// Utility extensions for Random class
/// </summary>
public static class RandomExtensions
{
    public static double NextGaussian(this Random rng)
    {
        // Box-Muller transform
        if (rng.NextDouble() > 0.5)
        {
            var u1 = rng.NextDouble();
            var u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
        else
        {
            var u1 = rng.NextDouble();
            var u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
    
    public static float NextFloat(this Random rng) => (float)rng.NextDouble();
    
    public static int Choice(this Random rng, int[] choices)
    {
        return choices[rng.Next(choices.Length)];
    }
}
