using System;
using System.Collections.Generic;
using Gymnasium.Spaces;
using Gymnasium;

namespace Gymnasium.Envs;

/// <summary>
/// Acrobot environment: classic control problem with a two-link pendulum.
/// </summary>
public class Acrobot : Env<float[], int>
{
    public Discrete ActionSpace { get; } = new(3);
    public Box ObservationSpace { get; } = new(
        new float[] { -1f, -1f, -1f, -1f, -6f, -6f },
        new float[] { 1f, 1f, 1f, 1f, 6f, 6f }
    );

    private float[] _state = new float[6];
    private readonly Random _rng = new();
    private int _steps;

    public override float[] Reset()
    {
        for (int i = 0; i < 6; i++)
            _state[i] = (float)(_rng.NextDouble() * 0.1 - 0.05);
        _steps = 0;
        return _state;
    }

    public override (float[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        // --- Acrobot physics constants (from OpenAI Gymnasium) ---
        const float LINK_LENGTH_1 = 1.0f; // [m]
        const float LINK_LENGTH_2 = 1.0f; // [m]
        const float LINK_MASS_1 = 1.0f;   // [kg]
        const float LINK_MASS_2 = 1.0f;   // [kg]
        const float LINK_COM_POS_1 = 0.5f; // [m]
        const float LINK_COM_POS_2 = 0.5f; // [m]
        const float LINK_MOI_1 = 1.0f;    // moment of inertia
        const float LINK_MOI_2 = 1.0f;
        const float MAX_VEL_1 = 4 * (float)Math.PI;
        const float MAX_VEL_2 = 9 * (float)Math.PI;
        const float dt = 0.2f;
        const float g = 9.8f;
        float[] torque = { -1.0f, 0.0f, 1.0f };

        float theta1 = (float)Math.Atan2(_state[1], _state[0]);
        float theta2 = (float)Math.Atan2(_state[3], _state[2]);
        float dtheta1 = _state[4];
        float dtheta2 = _state[5];
        float u = torque[action];

        // Equations of motion (from OpenAI Gym)
        float d1 = LINK_MASS_1 * LINK_COM_POS_1 * LINK_COM_POS_1 +
                   LINK_MASS_2 * (LINK_LENGTH_1 * LINK_LENGTH_1 + LINK_COM_POS_2 * LINK_COM_POS_2 +
                   2 * LINK_LENGTH_1 * LINK_COM_POS_2 * (float)Math.Cos(theta2)) + LINK_MOI_1 + LINK_MOI_2;
        float d2 = LINK_MASS_2 * (LINK_COM_POS_2 * LINK_COM_POS_2 +
                   LINK_LENGTH_1 * LINK_COM_POS_2 * (float)Math.Cos(theta2)) + LINK_MOI_2;
        float phi2 = LINK_MASS_2 * LINK_COM_POS_2 * g * (float)Math.Cos(theta1 + theta2 - Math.PI / 2.0);
        float phi1 = -LINK_MASS_2 * LINK_LENGTH_1 * LINK_COM_POS_2 * dtheta2 * dtheta2 * (float)Math.Sin(theta2)
                     - 2 * LINK_MASS_2 * LINK_LENGTH_1 * LINK_COM_POS_2 * dtheta2 * dtheta1 * (float)Math.Sin(theta2)
                     + (LINK_MASS_1 * LINK_COM_POS_1 + LINK_MASS_2 * LINK_LENGTH_1) * g * (float)Math.Cos(theta1 - Math.PI / 2.0)
                     + phi2;
        float ddtheta2 = (u + d2 / d1 * phi1 - LINK_MASS_2 * LINK_LENGTH_1 * LINK_COM_POS_2 * dtheta1 * dtheta1 * (float)Math.Sin(theta2) - phi2)
                         / (LINK_MASS_2 * LINK_COM_POS_2 * LINK_COM_POS_2 + LINK_MOI_2 - d2 * d2 / d1);
        float ddtheta1 = -(d2 * ddtheta2 + phi1) / d1;

        // Integrate
        dtheta1 += dt * ddtheta1;
        dtheta2 += dt * ddtheta2;
        dtheta1 = Math.Clamp(dtheta1, -MAX_VEL_1, MAX_VEL_1);
        dtheta2 = Math.Clamp(dtheta2, -MAX_VEL_2, MAX_VEL_2);
        theta1 += dt * dtheta1;
        theta2 += dt * dtheta2;

        // Update state (cos/sin for angles, velocities)
        _state[0] = (float)Math.Cos(theta1);
        _state[1] = (float)Math.Sin(theta1);
        _state[2] = (float)Math.Cos(theta2);
        _state[3] = (float)Math.Sin(theta2);
        _state[4] = dtheta1;
        _state[5] = dtheta2;
        _steps++;

        // Terminal condition: tip height > 1.0
        float tip_y = -LINK_LENGTH_1 * (float)Math.Cos(theta1) - LINK_LENGTH_2 * (float)Math.Cos(theta1 + theta2);
        bool done = tip_y > 1.0f || _steps >= 500;
        double reward = done ? 0.0 : -1.0;
        var info = new Dictionary<string, object>();
        return (_state, reward, done, info);
    }

    public override void Render(string mode = "human")
    {
        ConsoleRenderer.RenderHeader("Acrobot");
        Console.WriteLine($"State: [{string.Join(", ", _state)}]");
    }

    public override void Close() { }
}
