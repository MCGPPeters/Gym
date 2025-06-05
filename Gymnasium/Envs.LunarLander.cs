using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gymnasium.Spaces;
using Gymnasium;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Collision.Shapes;
using VelcroPhysics.Factories;
using VelcroPhysics.Shared;

namespace Gymnasium.Envs;

/// <summary>
/// LunarLander environment: Box2D-based lunar landing simulation with real physics.
/// </summary>
public class LunarLander : Env<float[], int>
{
    public Discrete ActionSpace { get; } = new(4);
    public Box ObservationSpace { get; } = new(new float[] { -1, -1, -1, -1, -1, -1, 0, 0 }, new float[] { 1, 1, 1, 1, 1, 1, 1, 1 });
    
    private float[] _state = new float[8];
    private readonly Random _rng = new();
    private int _steps;
      // Physics simulation
    private World? _world;
    private Body? _lander;
    private Vector2 _initialPosition;
    private Vector2 _landingPad1, _landingPad2;
    private bool _gameEnded;
    private float _prevShaping;
    
    // Constants
    private const float VIEWPORT_W = 600f;
    private const float VIEWPORT_H = 400f;
    private const float SCALE = 30f; // affects how fast-paced the game is, forces should be adjusted as well
    private const float MAIN_ENGINE_POWER = 13f;
    private const float SIDE_ENGINE_POWER = 0.6f;
    
    private const float INITIAL_RANDOM = 1000f; // Set 1500 to make game harder
    private const float LANDER_POLY = 14f;
    private const float LEG_AWAY = 20f;
    private const float LEG_DOWN = 18f;
    private const float LEG_W = 2f;
    private const float LEG_H = 8f;
    private const float LEG_SPRING_TORQUE = 40f;
    
    private const float SIDE_ENGINE_HEIGHT = 14f;
    private const float SIDE_ENGINE_AWAY = 12f;
    private const float MAIN_ENGINE_Y_LOCATION = 4f;

    public override float[] Reset()
    {
        InitializePhysics();
        _steps = 0;
        _gameEnded = false;
        _prevShaping = 0f;
        
        // Initial random velocity
        _lander.LinearVelocity = new Vector2(
            (float)(_rng.NextDouble() - 0.5) * INITIAL_RANDOM / SCALE,
            (float)(_rng.NextDouble() - 0.5) * INITIAL_RANDOM / SCALE
        );
        
        UpdateState();
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        if (_gameEnded)
        {
            return (_state, 0, true, new Dictionary<string, object>());
        }
        
        // Apply forces based on action
        float tip = (float)(Math.Sin(_lander.Rotation) * 4 / SCALE);
        float side = (float)(Math.Cos(_lander.Rotation) * 4 / SCALE);
        
        Vector2 dispersion = new Vector2((float)(_rng.NextDouble() - 0.5), (float)(_rng.NextDouble() - 0.5));
        dispersion *= 0.05f;
        
        float m_power = 0f;
        if (action == 2) // Main engine
        {
            m_power = 1.0f;
            Vector2 impulse = new Vector2(-side * MAIN_ENGINE_POWER / SCALE, tip * MAIN_ENGINE_POWER / SCALE);
            impulse += dispersion;
            _lander.ApplyLinearImpulse(impulse, _lander.WorldCenter + new Vector2(-side * MAIN_ENGINE_Y_LOCATION / SCALE, tip * MAIN_ENGINE_Y_LOCATION / SCALE));
        }
        
        float s_power = 0f;
        if (action == 1) // Left engine
        {
            s_power = 1.0f;
            Vector2 impulse = new Vector2(-side * SIDE_ENGINE_POWER / SCALE, tip * SIDE_ENGINE_POWER / SCALE);
            impulse += dispersion;
            _lander.ApplyLinearImpulse(impulse, _lander.WorldCenter + new Vector2(-side * SIDE_ENGINE_AWAY / SCALE, tip * SIDE_ENGINE_HEIGHT / SCALE));
        }
        else if (action == 3) // Right engine
        {
            s_power = 1.0f;
            Vector2 impulse = new Vector2(side * SIDE_ENGINE_POWER / SCALE, -tip * SIDE_ENGINE_POWER / SCALE);
            impulse += dispersion;
            _lander.ApplyLinearImpulse(impulse, _lander.WorldCenter + new Vector2(side * SIDE_ENGINE_AWAY / SCALE, -tip * SIDE_ENGINE_HEIGHT / SCALE));
        }
        
        // Step physics
        _world.Step(1f / 50f);
        
        UpdateState();
        
        // Calculate reward
        double reward = 0;
        double shaping = -100 * Math.Sqrt(_state[0] * _state[0] + _state[1] * _state[1]) // Distance from landing pad
                        - 100 * Math.Sqrt(_state[2] * _state[2] + _state[3] * _state[3]) // Velocity
                        - 100 * Math.Abs(_state[4]) // Angle
                        + 10 * _state[6] + 10 * _state[7]; // Legs touching ground
        
        if (_prevShaping != 0)
        {
            reward = shaping - _prevShaping;
        }
        _prevShaping = (float)shaping;
        
        reward -= m_power * 0.30; // Less fuel spent is better
        reward -= s_power * 0.03;
        
        bool done = false;
        if (_lander.Position.Y < 0 || Math.Abs(_lander.Position.X) > VIEWPORT_W / SCALE / 2)
        {
            _gameEnded = true;
            done = true;
            reward = -100;
        }
        
        if (!_lander.Awake)
        {
            done = true;
            reward = +100;
        }
        
        _steps++;
        if (_steps >= 1000)
        {
            done = true;
        }
        
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }
      private void InitializePhysics()
    {
        _world = new World(new Vector2(0, -10f));
        
        // Create ground using BodyFactory
        Body ground = BodyFactory.CreateBody(_world);
        ground.BodyType = BodyType.Static;
        
        var groundShape = new EdgeShape(
            new Vector2(-VIEWPORT_W / SCALE / 2, 0),
            new Vector2(VIEWPORT_W / SCALE / 2, 0)
        );
        ground.CreateFixture(groundShape);
        
        // Landing pad positions
        _landingPad1 = new Vector2(-VIEWPORT_W / SCALE / 4, 0);
        _landingPad2 = new Vector2(VIEWPORT_W / SCALE / 4, 0);
        
        // Create lander using BodyFactory
        _initialPosition = new Vector2(0, VIEWPORT_H / SCALE);
        _lander = BodyFactory.CreateBody(_world);
        _lander.Position = _initialPosition;
        _lander.BodyType = BodyType.Dynamic;        // Create box shape for lander using Vertices
        var halfWidth = LANDER_POLY / SCALE / 2;
        var halfHeight = LANDER_POLY / SCALE / 2;
        
        var vertices = new Vertices();
        vertices.Add(new Vector2(-halfWidth, -halfHeight));
        vertices.Add(new Vector2(halfWidth, -halfHeight));
        vertices.Add(new Vector2(halfWidth, halfHeight));
        vertices.Add(new Vector2(-halfWidth, halfHeight));
        
        var landerShape = new PolygonShape(vertices, 1f);
        var fixture = _lander.CreateFixture(landerShape);
        fixture.Restitution = 0f;
        fixture.Friction = 0.1f;
    }
    
    private void UpdateState()
    {
        Vector2 pos = _lander.Position;
        Vector2 vel = _lander.LinearVelocity;
        float angle = _lander.Rotation;
        float angularVel = _lander.AngularVelocity;
        
        // Normalize position
        _state[0] = (pos.X - VIEWPORT_W / SCALE / 2) / (VIEWPORT_W / SCALE / 2);
        _state[1] = (pos.Y - (VIEWPORT_H / SCALE / 2)) / (VIEWPORT_H / SCALE / 2);
        _state[2] = vel.X * (VIEWPORT_W / SCALE / 2) / 5f;
        _state[3] = vel.Y * (VIEWPORT_H / SCALE / 2) / 5f;
        _state[4] = angle;
        _state[5] = 20f * angularVel / 5f;
        _state[6] = 1.0f; // Left leg contact (simplified)
        _state[7] = 1.0f; // Right leg contact (simplified)
        
        // Clamp values
        for (int i = 0; i < 6; i++)
        {
            _state[i] = Math.Clamp(_state[i], -1f, 1f);
        }
    }

    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("LunarLander");
        Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    }

    public override void Close()
    {
        // VelcroPhysics doesn't need explicit disposal
        _world = null;
        _lander = null;
    }
}
