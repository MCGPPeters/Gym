using System;
using System.Collections.Generic;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// CartPole environment: classic control problem with pole balancing on a cart.
/// </summary>
public class CartPole : Env<(float, float, float, float), int>
{
    public Discrete ActionSpace { get; } = new(2);
    public Box ObservationSpace { get; } = new(
        new float[] { -2.4f, -3.0f, -0.2095f, -3.0f },
        new float[] { 2.4f, 3.0f, 0.2095f, 3.0f }
    );

    private (float, float, float, float) _state;
    private readonly Random _rng = new();
    private int _steps;

    public override (float, float, float, float) Reset()
    {
        _state = (
            (float)(_rng.NextDouble() * 0.1 - 0.05),
            (float)(_rng.NextDouble() * 0.1 - 0.05),
            (float)(_rng.NextDouble() * 0.1 - 0.05),
            (float)(_rng.NextDouble() * 0.1 - 0.05)
        );
        _steps = 0;
        return _state;
    }

    public override ((float, float, float, float) state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        // --- CartPole physics constants (from OpenAI Gymnasium) ---
        const float gravity = 9.8f;
        const float masscart = 1.0f;
        const float masspole = 0.1f;
        const float total_mass = masscart + masspole;
        const float length = 0.5f; // actually half the pole's length
        const float polemass_length = masspole * length;
        const float force_mag = 10.0f;
        const float tau = 0.02f; // seconds between state updates
        const float theta_threshold_radians = 12 * (float)Math.PI / 180;
        const float x_threshold = 2.4f;

        // --- Unpack state ---
        var (x, xDot, theta, thetaDot) = _state;
        float force = action == 1 ? force_mag : -force_mag;
        float costheta = (float)Math.Cos(theta);
        float sintheta = (float)Math.Sin(theta);

        // --- Physics equations of motion ---
        float temp = (force + polemass_length * thetaDot * thetaDot * sintheta) / total_mass;
        float thetaacc = (gravity * sintheta - costheta * temp) /
            (length * (4.0f / 3.0f - masspole * costheta * costheta / total_mass));
        float xacc = temp - polemass_length * thetaacc * costheta / total_mass;

        // --- Integrate to get new state ---
        x += tau * xDot;
        xDot += tau * xacc;
        theta += tau * thetaDot;
        thetaDot += tau * thetaacc;
        _state = (x, xDot, theta, thetaDot);
        _steps++;

        // --- Check if episode is done ---
        bool done = x < -x_threshold || x > x_threshold ||
                    theta < -theta_threshold_radians || theta > theta_threshold_radians ||
                    _steps >= 500;
        double reward = !done ? 1.0 : 0.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        Console.WriteLine($"State: {_state}");
    }

    public override void Close() { }
}
