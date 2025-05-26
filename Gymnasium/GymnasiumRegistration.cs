using Gymnasium;
using Gymnasium.Envs;

namespace Gymnasium;

public static class GymnasiumRegistration
{
    public static void RegisterAll()
    {
        EnvRegistry.Register("CartPole-v1", () => new CartPole());
        EnvRegistry.Register("MountainCar-v0", () => new MountainCar());
        EnvRegistry.Register("MountainCarContinuous-v0", () => new MountainCarContinuous());
        EnvRegistry.Register("Acrobot-v1", () => new Acrobot());
        EnvRegistry.Register("Pendulum-v1", () => new Pendulum());
        EnvRegistry.Register("FrozenLake-v1", () => new FrozenLake());
        EnvRegistry.Register("Taxi-v3", () => new Taxi());
        EnvRegistry.Register("Blackjack-v1", () => new Blackjack());
        EnvRegistry.Register("CliffWalking-v0", () => new CliffWalking());        EnvRegistry.Register("LunarLander-v2", () => new LunarLander());
        EnvRegistry.Register("BipedalWalker-v3", () => new BipedalWalker());
        EnvRegistry.Register("CarRacing-v2", () => new CarRacing());
        
        // Atari environments
        EnvRegistry.Register("Pong-v4", () => new Pong());
        EnvRegistry.Register("Breakout-v4", () => new Breakout());
        EnvRegistry.Register("SpaceInvaders-v4", () => new SpaceInvaders());
        EnvRegistry.Register("AtariStub-v0", () => new AtariStub()); // Keep for compatibility
        
        EnvRegistry.Register("MujocoStub-v0", () => new MujocoStub());
        // Add more environments here as needed
    }
}
