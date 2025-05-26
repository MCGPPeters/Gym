using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gymnasium.Spaces;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Collision.Shapes;
using VelcroPhysics.Factories;
using VelcroPhysics.Shared;

namespace Gymnasium.Envs;

/// <summary>
/// BipedalWalker environment: Box2D-based bipedal walking simulation with real physics.
/// </summary>
public class BipedalWalker : Env<float[], float[]>
{
    public Box ActionSpace { get; } = new(new float[] { -1, -1, -1, -1 }, new float[] { 1, 1, 1, 1 });
    public Box ObservationSpace { get; } = new(new float[24], new float[24]);
    
    private float[] _state = new float[24];
    private readonly Random _rng = new();
    private int _steps;
    
    // Physics simulation
    private World? _world;
    private Body? _hull;
    private Body? _leg1, _leg2;
    private Body? _lowerLeg1, _lowerLeg2;
    private float _hullAngle;
    
    // Constants
    private const float VIEWPORT_W = 600f;
    private const float VIEWPORT_H = 400f;
    private const float SCALE = 30f;
    private const float MOTORS_TORQUE = 80f;
    private const float SPEED_HIP = 4f;
    private const float SPEED_KNEE = 6f;
    
    private const float HULL_POLY = 30f;
    private const float LEG_W = 8f;
    private const float LEG_H = 34f;

    public override float[] Reset()
    {
        InitializePhysics();
        _steps = 0;
        _hullAngle = 0f;
        
        UpdateState();
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(float[] action)
    {
        if (_world == null || _hull == null) 
            return (_state, 0, true, new Dictionary<string, object>());
        
        // Apply motor torques to joints (simplified)
        if (_leg1 != null && action.Length >= 4)
        {
            // Hip joints
            _leg1.ApplyTorque(action[0] * MOTORS_TORQUE);
            _leg2?.ApplyTorque(action[1] * MOTORS_TORQUE);
            
            // Knee joints  
            _lowerLeg1?.ApplyTorque(action[2] * MOTORS_TORQUE);
            _lowerLeg2?.ApplyTorque(action[3] * MOTORS_TORQUE);
        }
        
        // Step physics
        _world.Step(1f / 50f);
        
        UpdateState();
        
        // Calculate reward based on forward progress and staying upright
        double reward = 0;
        if (_hull != null)
        {
            reward = _hull.LinearVelocity.X * 10; // Reward forward movement
            reward -= Math.Abs(_hull.Rotation) * 0.1; // Penalty for tilting
            reward -= 0.00035f * MOTORS_TORQUE * Math.Abs(action[0]);
            reward -= 0.00035f * MOTORS_TORQUE * Math.Abs(action[1]);  
            reward -= 0.00035f * MOTORS_TORQUE * Math.Abs(action[2]);
            reward -= 0.00035f * MOTORS_TORQUE * Math.Abs(action[3]);
        }
        
        bool done = false;
        _steps++;
        
        // Check if walker fell over
        if (_hull != null && (_hull.Position.Y < 0.8f || Math.Abs(_hull.Rotation) > 1.0f))
        {
            done = true;
            reward = -100;
        }
        
        if (_steps >= 1600)
            done = true;
            
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }
    
    private void InitializePhysics()
    {
        _world = new World(new Vector2(0, -10f));
        
        // Create ground
        Body ground = BodyFactory.CreateBody(_world);
        ground.BodyType = BodyType.Static;
        
        var groundShape = new EdgeShape(
            new Vector2(-VIEWPORT_W / SCALE, 0),
            new Vector2(VIEWPORT_W / SCALE, 0)
        );
        ground.CreateFixture(groundShape);
        
        // Create hull (torso)
        _hull = BodyFactory.CreateBody(_world);
        _hull.Position = new Vector2(0, 4);
        _hull.BodyType = BodyType.Dynamic;
        
        var hullVertices = new Vertices();
        hullVertices.Add(new Vector2(-HULL_POLY / SCALE / 2, -HULL_POLY / SCALE / 2));
        hullVertices.Add(new Vector2(HULL_POLY / SCALE / 2, -HULL_POLY / SCALE / 2));
        hullVertices.Add(new Vector2(HULL_POLY / SCALE / 2, HULL_POLY / SCALE / 2));
        hullVertices.Add(new Vector2(-HULL_POLY / SCALE / 2, HULL_POLY / SCALE / 2));
        
        var hullShape = new PolygonShape(hullVertices, 1f);
        _hull.CreateFixture(hullShape);
        
        // Create legs
        _leg1 = BodyFactory.CreateBody(_world);
        _leg1.Position = new Vector2(-0.2f, 3f);
        _leg1.BodyType = BodyType.Dynamic;
        
        var legVertices = new Vertices();
        legVertices.Add(new Vector2(-LEG_W / SCALE / 2, -LEG_H / SCALE / 2));
        legVertices.Add(new Vector2(LEG_W / SCALE / 2, -LEG_H / SCALE / 2));
        legVertices.Add(new Vector2(LEG_W / SCALE / 2, LEG_H / SCALE / 2));
        legVertices.Add(new Vector2(-LEG_W / SCALE / 2, LEG_H / SCALE / 2));
        
        var legShape = new PolygonShape(legVertices, 1f);
        _leg1.CreateFixture(legShape);
        
        _leg2 = BodyFactory.CreateBody(_world);
        _leg2.Position = new Vector2(0.2f, 3f);
        _leg2.BodyType = BodyType.Dynamic;
        _leg2.CreateFixture(legShape);
        
        // Create lower legs
        _lowerLeg1 = BodyFactory.CreateBody(_world);
        _lowerLeg1.Position = new Vector2(-0.2f, 2f);
        _lowerLeg1.BodyType = BodyType.Dynamic;
        _lowerLeg1.CreateFixture(legShape);
        
        _lowerLeg2 = BodyFactory.CreateBody(_world);
        _lowerLeg2.Position = new Vector2(0.2f, 2f);
        _lowerLeg2.BodyType = BodyType.Dynamic;
        _lowerLeg2.CreateFixture(legShape);
    }
    
    private void UpdateState()
    {
        if (_hull == null) return;
        
        Vector2 hullPos = _hull.Position;
        Vector2 hullVel = _hull.LinearVelocity;
        float hullAngle = _hull.Rotation;
        float hullAngularVel = _hull.AngularVelocity;
        
        // Normalize and fill state vector (simplified)
        _state[0] = hullAngle;
        _state[1] = hullAngularVel;
        _state[2] = hullVel.X;
        _state[3] = hullVel.Y;
        _state[4] = hullPos.X;
        _state[5] = hullPos.Y;
        
        // Leg angles and velocities (simplified)
        if (_leg1 != null)
        {
            _state[6] = _leg1.Rotation;
            _state[7] = _leg1.AngularVelocity;
            _state[8] = _leg1.LinearVelocity.X;
            _state[9] = _leg1.LinearVelocity.Y;
        }
        
        if (_leg2 != null)
        {
            _state[10] = _leg2.Rotation;
            _state[11] = _leg2.AngularVelocity;
            _state[12] = _leg2.LinearVelocity.X;
            _state[13] = _leg2.LinearVelocity.Y;
        }
        
        if (_lowerLeg1 != null)
        {
            _state[14] = _lowerLeg1.Rotation;
            _state[15] = _lowerLeg1.AngularVelocity;
            _state[16] = _lowerLeg1.LinearVelocity.X;
            _state[17] = _lowerLeg1.LinearVelocity.Y;
        }
        
        if (_lowerLeg2 != null)
        {
            _state[18] = _lowerLeg2.Rotation;
            _state[19] = _lowerLeg2.AngularVelocity;
            _state[20] = _lowerLeg2.LinearVelocity.X;
            _state[21] = _lowerLeg2.LinearVelocity.Y;
        }
        
        // Ground contact sensors (simplified)
        _state[22] = 1.0f; // Left foot contact
        _state[23] = 1.0f; // Right foot contact
        
        // Clamp values
        for (int i = 0; i < 24; i++)
        {
            _state[i] = Math.Clamp(_state[i], -5f, 5f);
        }
    }

    public override void Render(string mode = "human") => Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    
    public override void Close() 
    {
        _world = null;
        _hull = null;
        _leg1 = _leg2 = null;
        _lowerLeg1 = _lowerLeg2 = null;
    }
}
