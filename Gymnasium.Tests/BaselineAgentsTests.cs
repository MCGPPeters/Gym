using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Gymnasium.UI.Models;
using Gymnasium.UI.Agents.Baselines;
using Xunit;

namespace Gymnasium.Tests
{
    public class BaselineAgentsTests
    {
        [Fact]
        public void AllBaselineAgents_ShouldBeDiscoverable()
        {
            // Setup MEF composition
            var configuration = new ContainerConfiguration()
                .WithAssembly(Assembly.GetAssembly(typeof(DQNAgentPlugin)));
            
            using var container = configuration.CreateContainer();
              // Get only baseline agent plugins (exclude built-in RandomAgent)
            var plugins = container.GetExports<IAgentPlugin>()
                .Where(p => !p.Name.Contains("Built-in"))
                .ToList();
              // Verify we have all expected baseline agents
            var expectedAgents = new[]
            {
                "A2C (Advantage Actor-Critic)",
                "ACER (Actor-Critic with Experience Replay)",
                "ACKTR (Actor-Critic using KFAC)",
                "DDPG (Deep Deterministic Policy Gradient)",
                "DQN (Deep Q-Network)",
                "GAIL (Generative Adversarial Imitation Learning)",
                "HER (Hindsight Experience Replay)",
                "PPO (Proximal Policy Optimization)",
                "TRPO (Trust Region Policy Optimization)"
            };
            
            Assert.True(plugins.Count >= expectedAgents.Length, 
                $"Expected at least {expectedAgents.Length} agents, but found {plugins.Count}");
            
            foreach (var expectedAgent in expectedAgents)
            {
                var found = plugins.Any(p => p.Name == expectedAgent);
                Assert.True(found, $"Agent '{expectedAgent}' was not found in the discovered plugins");
            }
        }
        
        [Fact]
        public void AllBaselineAgents_ShouldHaveValidDescriptions()
        {
            var configuration = new ContainerConfiguration()
                .WithAssembly(Assembly.GetAssembly(typeof(DQNAgentPlugin)));
            
            using var container = configuration.CreateContainer();
            var plugins = container.GetExports<IAgentPlugin>().ToList();
            
            foreach (var plugin in plugins)
            {
                Assert.False(string.IsNullOrWhiteSpace(plugin.Name), 
                    $"Agent plugin should have a non-empty name");
                Assert.False(string.IsNullOrWhiteSpace(plugin.Description), 
                    $"Agent plugin '{plugin.Name}' should have a non-empty description");
            }
        }
        
        [Fact]
        public void AllBaselineAgents_ShouldCreateValidAgents()
        {
            var configuration = new ContainerConfiguration()
                .WithAssembly(Assembly.GetAssembly(typeof(DQNAgentPlugin)));
            
            using var container = configuration.CreateContainer();
            var plugins = container.GetExports<IAgentPlugin>().ToList();
            
            // Create a dummy environment for testing
            var dummyEnv = new Gymnasium.DummyEnv();
            
            foreach (var plugin in plugins)
            {
                try
                {
                    var agent = plugin.CreateAgent(dummyEnv);
                    Assert.NotNull(agent);
                    
                    // Verify the agent has the expected Act method
                    var actMethod = agent.GetType().GetMethod("Act");
                    Assert.NotNull(actMethod);
                    
                    // Verify the agent has the expected Learn method
                    var learnMethod = agent.GetType().GetMethod("Learn");
                    Assert.NotNull(learnMethod);
                }
                catch (Exception ex)
                {
                    Assert.True(false, $"Failed to create agent '{plugin.Name}': {ex.Message}");
                }
            }
        }
    }
}
