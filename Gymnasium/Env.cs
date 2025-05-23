namespace Gymnasium;

/// <summary>
/// Abstract base class for all Gymnasium environments.
/// Defines the interface for reset, step, render, close, seeding, and metadata.
/// </summary>
public abstract class Env<TState, TAction>
{
    /// <summary>
    /// Reset the environment to an initial state.
    /// </summary>
    public abstract TState Reset();

    /// <summary>
    /// Step the environment by one timestep.
    /// </summary>
    public abstract (TState state, double reward, bool done, IDictionary<string, object> info) Step(TAction action);

    /// <summary>
    /// Render the environment (optional).
    /// </summary>
    public abstract void Render(string mode = "human");

    /// <summary>
    /// Close the environment and release resources.
    /// </summary>
    public abstract void Close();

    /// <summary>
    /// Seed the random number generator for reproducibility.
    /// </summary>
    public virtual void Seed(int? seed = null)
    {
        // Optionally override in derived classes for deterministic behavior
    }

    /// <summary>
    /// Specification of the observation space.
    /// </summary>
    public virtual object? Spec => null;

    /// <summary>
    /// Metadata about the environment.
    /// </summary>
    public virtual object? Metadata => null;

    /// <summary>
    /// The range of possible rewards.
    /// </summary>
    public virtual (double, double)? RewardRange => null;
}
