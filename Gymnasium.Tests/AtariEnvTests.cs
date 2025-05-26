using System;
using Xunit;
using Gymnasium.Envs;

namespace Gymnasium.Tests;

public class AtariEnvTests
{
    [Fact]
    public void Pong_CanCreateAndReset()
    {
        var env = new Pong();
        
        Assert.NotNull(env.ActionSpace);
        Assert.NotNull(env.ObservationSpace);
        Assert.Equal(4, env.ActionSpace.N); // NOOP, FIRE, UP, DOWN
        
        var observation = env.Reset();
        Assert.NotNull(observation);
        Assert.Equal(210 * 160 * 3, observation.Length); // RGB image
    }
    
    [Fact]
    public void Pong_CanStep()
    {
        var env = new Pong();
        env.Reset();
        
        var (state, reward, done, info) = env.Step(0); // NOOP action
        
        Assert.NotNull(state);
        Assert.Equal(210 * 160 * 3, state.Length);
        Assert.True(reward >= -1 && reward <= 1);
        Assert.NotNull(info);
    }
    
    [Fact]
    public void Breakout_CanCreateAndReset()
    {
        var env = new Breakout();
        
        Assert.NotNull(env.ActionSpace);
        Assert.NotNull(env.ObservationSpace);
        Assert.Equal(4, env.ActionSpace.N); // NOOP, FIRE, RIGHT, LEFT
        
        var observation = env.Reset();
        Assert.NotNull(observation);
        Assert.Equal(210 * 160 * 3, observation.Length); // RGB image
    }
    
    [Fact]
    public void Breakout_CanStep()
    {
        var env = new Breakout();
        env.Reset();
        
        var (state, reward, done, info) = env.Step(0); // NOOP action
        
        Assert.NotNull(state);
        Assert.Equal(210 * 160 * 3, state.Length);
        Assert.True(reward >= 0);
        Assert.NotNull(info);
    }
    
    [Fact]
    public void SpaceInvaders_CanCreateAndReset()
    {
        var env = new SpaceInvaders();
        
        Assert.NotNull(env.ActionSpace);
        Assert.NotNull(env.ObservationSpace);
        Assert.Equal(6, env.ActionSpace.N); // NOOP, FIRE, RIGHT, LEFT, RIGHTFIRE, LEFTFIRE
        
        var observation = env.Reset();
        Assert.NotNull(observation);
        Assert.Equal(210 * 160 * 3, observation.Length); // RGB image
    }
    
    [Fact]
    public void SpaceInvaders_CanStep()
    {
        var env = new SpaceInvaders();
        env.Reset();
        
        var (state, reward, done, info) = env.Step(0); // NOOP action
        
        Assert.NotNull(state);
        Assert.Equal(210 * 160 * 3, state.Length);
        Assert.True(reward >= 0);
        Assert.NotNull(info);
    }
}
