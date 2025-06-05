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
/// CarRacing environment: Box2D-based car racing simulation with real physics.
/// </summary>
public class CarRacing : Env<float[], float[]>
{
    public Box ActionSpace { get; } = new(new float[] { -1, 0, 0 }, new float[] { 1, 1, 1 });
    public Box ObservationSpace { get; } = new(new float[8], new float[8]); // Simplified observation space
    
    private float[] _state = new float[8]; // Simplified state: [x, y, angle, vel_x, vel_y, angular_vel, wheel_angle, speed]
    private readonly Random _rng = new();
    private int _steps;
    
    // Physics simulation
    private World? _world;
    private Body? _car;
    private float _wheelAngle;
    private float _enginePower;
    private float _braking;
    
    // Constants
    private const float VIEWPORT_W = 600f;
    private const float VIEWPORT_H = 400f;
    private const float SCALE = 30f;
    private const float CAR_WIDTH = 40f;
    private const float CAR_HEIGHT = 80f;
    private const float ENGINE_POWER = 300f;
    private const float BRAKE_FORCE = 15f;
    private const float MAX_STEER_ANGLE = 0.4f;

    public override float[] Reset()
    {
        InitializePhysics();
        _steps = 0;
        _wheelAngle = 0f;
        _enginePower = 0f;
        _braking = 0f;
        
        UpdateState();
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(float[] action)
    {
        if (_world == null || _car == null || action.Length < 3) 
            return (_state, 0, true, new Dictionary<string, object>());
        
        // Parse actions: [steering, gas, brake]
        float steering = Math.Clamp(action[0], -1f, 1f);
        float gas = Math.Clamp(action[1], 0f, 1f);
        float brake = Math.Clamp(action[2], 0f, 1f);
        
        // Update wheel angle
        _wheelAngle = steering * MAX_STEER_ANGLE;
        
        // Apply engine force
        float carAngle = _car.Rotation;
        Vector2 forwardDir = new Vector2((float)Math.Cos(carAngle), (float)Math.Sin(carAngle));
        
        if (gas > 0)
        {
            Vector2 engineForce = forwardDir * gas * ENGINE_POWER;
            _car.ApplyForce(engineForce);
        }
          // Apply braking
        if (brake > 0)
        {
            Vector2 currentVelocity = _car.LinearVelocity;
            Vector2 brakeForce = -currentVelocity * brake * BRAKE_FORCE;
            _car.ApplyForce(brakeForce);
        }
        
        // Apply steering torque
        _car.ApplyTorque(steering * gas * 50f);
        
        // Step physics
        _world.Step(1f / 50f);
        
        UpdateState();
        
        // Calculate reward based on forward progress and staying on track
        double reward = 0;
        Vector2 velocity = _car.LinearVelocity;
        float speed = velocity.Length();
        reward = speed * 0.1; // Reward for speed
        
        // Penalty for going off track (simplified)
        if (Math.Abs(_car.Position.X) > VIEWPORT_W / SCALE / 2)
            reward -= 100;
            
        bool done = false;
        _steps++;
        
        if (_steps >= 1000 || Math.Abs(_car.Position.X) > VIEWPORT_W / SCALE / 2)
            done = true;
            
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }
    
    private void InitializePhysics()
    {
        _world = new World(new Vector2(0, 0)); // No gravity for top-down car racing
        
        // Create track boundaries
        Body leftWall = BodyFactory.CreateBody(_world);
        leftWall.BodyType = BodyType.Static;
        var leftWallShape = new EdgeShape(
            new Vector2(-VIEWPORT_W / SCALE / 2, -VIEWPORT_H / SCALE / 2),
            new Vector2(-VIEWPORT_W / SCALE / 2, VIEWPORT_H / SCALE / 2)
        );
        leftWall.CreateFixture(leftWallShape);
        
        Body rightWall = BodyFactory.CreateBody(_world);
        rightWall.BodyType = BodyType.Static;
        var rightWallShape = new EdgeShape(
            new Vector2(VIEWPORT_W / SCALE / 2, -VIEWPORT_H / SCALE / 2),
            new Vector2(VIEWPORT_W / SCALE / 2, VIEWPORT_H / SCALE / 2)
        );
        rightWall.CreateFixture(rightWallShape);
        
        // Create car
        _car = BodyFactory.CreateBody(_world);
        _car.Position = new Vector2(0, 0);
        _car.BodyType = BodyType.Dynamic;
        
        var carVertices = new Vertices();
        carVertices.Add(new Vector2(-CAR_WIDTH / SCALE / 2, -CAR_HEIGHT / SCALE / 2));
        carVertices.Add(new Vector2(CAR_WIDTH / SCALE / 2, -CAR_HEIGHT / SCALE / 2));
        carVertices.Add(new Vector2(CAR_WIDTH / SCALE / 2, CAR_HEIGHT / SCALE / 2));
        carVertices.Add(new Vector2(-CAR_WIDTH / SCALE / 2, CAR_HEIGHT / SCALE / 2));
        
        var carShape = new PolygonShape(carVertices, 1f);
        var fixture = _car.CreateFixture(carShape);
        fixture.Friction = 0.7f;
        fixture.Restitution = 0.1f;
        
        // Add some damping
        _car.LinearDamping = 0.4f;
        _car.AngularDamping = 0.3f;
    }
    
    private void UpdateState()
    {
        if (_car == null) return;
        
        Vector2 pos = _car.Position;
        Vector2 vel = _car.LinearVelocity;
        float angle = _car.Rotation;
        float angularVel = _car.AngularVelocity;
        float speed = vel.Length();
        
        // Normalize and fill state vector
        _state[0] = pos.X / (VIEWPORT_W / SCALE / 2); // Normalized X position
        _state[1] = pos.Y / (VIEWPORT_H / SCALE / 2); // Normalized Y position
        _state[2] = angle / (float)Math.PI; // Normalized angle
        _state[3] = vel.X / 10f; // Normalized X velocity
        _state[4] = vel.Y / 10f; // Normalized Y velocity
        _state[5] = angularVel / 5f; // Normalized angular velocity
        _state[6] = _wheelAngle / MAX_STEER_ANGLE; // Normalized wheel angle
        _state[7] = speed / 10f; // Normalized speed
        
        // Clamp values
        for (int i = 0; i < 8; i++)
        {
            _state[i] = Math.Clamp(_state[i], -1f, 1f);
        }
    }

    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("CarRacing");
        Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    }
    
    public override void Close() 
    {
        _world = null;
        _car = null;
    }
}
