using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// Pendulum environment: classic control problem with a single pendulum.
/// </summary>
public class Pendulum : Env<float[], float>
{
    public Box ActionSpace { get; } = new(new float[] { -2.0f }, new float[] { 2.0f });
    public Box ObservationSpace { get; } = new(new float[] { -1f, -1f, -8f }, new float[] { 1f, 1f, 8f });

    private float[] _state = new float[3];
    private readonly Random _rng = new();
    private int _steps;

    public override float[] Reset()
    {
        float theta = (float)(_rng.NextDouble() * 2 * Math.PI - Math.PI);
        float thetaDot = (float)(_rng.NextDouble() * 1 - 0.5);
        _state[0] = (float)Math.Cos(theta);
        _state[1] = (float)Math.Sin(theta);
        _state[2] = thetaDot;
        _steps = 0;
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(float action)
    {
        // --- Pendulum physics constants (from OpenAI Gymnasium) ---
        const float max_speed = 8.0f;
        const float max_torque = 2.0f;
        const float dt = 0.05f;
        const float g = 10.0f;
        const float m = 1.0f;
        const float l = 1.0f;

        float theta = (float)Math.Atan2(_state[1], _state[0]);
        float thetaDot = _state[2];
        float u = Math.Clamp(action, -max_torque, max_torque);

        // Physics update
        float new_thetaDot = thetaDot + (-3 * g / (2 * l) * (float)Math.Sin(theta + Math.PI) + 3.0f / (m * l * l) * u) * dt;
        new_thetaDot = Math.Clamp(new_thetaDot, -max_speed, max_speed);
        float new_theta = theta + new_thetaDot * dt;

        // Update state (cos/sin for angle, velocity)
        _state[0] = (float)Math.Cos(new_theta);
        _state[1] = (float)Math.Sin(new_theta);
        _state[2] = new_thetaDot;
        _steps++;

        // Reward: -(theta^2 + 0.1*thetaDot^2 + 0.001*u^2)
        double reward = -(theta * theta + 0.1 * thetaDot * thetaDot + 0.001 * u * u);
        bool done = false; // Pendulum never ends by default
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    }

    public override void Close() { }
}
