using System;
using System.IO;
using System.Collections.Generic;
using Gymnasium;
using Gymnasium.UI.Agents.Baselines;
using Xunit;

namespace Gymnasium.Tests
{
    public class ComprehensiveCompatibilityTest
    {        [Fact]
        public void TestAllAgentEnvironmentCombinations()
        {
            string[] args = new string[0];
            
            // CRITICAL: Register all environments first
            GymnasiumRegistration.RegisterAll();
            
            Console.WriteLine("=== COMPREHENSIVE AGENT-ENVIRONMENT COMPATIBILITY TEST ===");
            var logFile = "comprehensive_compatibility_log.txt";
            var resultsFile = "compatibility_results.json";
            File.WriteAllText(logFile, $"{DateTime.Now}: Starting comprehensive compatibility tests...\n");
            
            // All baseline agents
            var agents = new[]
            {
                "A2CAgent", "ACERAgent", "ACKTRAgent", "DDPGAgent", "TRPOAgent", 
                "HERAgent", "GAILAgent", "DQNAgent", "PPOAgent"
            };
              // All environments (including BipedalWalker for investigation)
            var environments = new[]
            {
                "CartPole-v1", "MountainCar-v0", "MountainCarContinuous-v0",
                "Acrobot-v1", "Pendulum-v1", "LunarLander-v2", "BipedalWalker-v3"
            };
            
            var results = new Dictionary<string, Dictionary<string, object>>();
            
            foreach (var agentName in agents)
            {
                results[agentName] = new Dictionary<string, object>();
                
                foreach (var envName in environments)
                {
                    Console.WriteLine($"\n=== Testing {agentName} with {envName} ===");
                    File.AppendAllText(logFile, $"{DateTime.Now}: Testing {agentName} with {envName}\n");
                    
                    var testResult = new Dictionary<string, object>
                    {
                        ["environment_creation"] = "FAIL",
                        ["agent_creation"] = "FAIL", 
                        ["state_conversion"] = "FAIL",
                        ["action_generation"] = "FAIL",
                        ["learning_call"] = "FAIL",
                        ["multiple_steps"] = "FAIL",
                        ["error_details"] = new List<string>()
                    };
                    
                    try
                    {
                        // Step 1: Create environment
                        var env = EnvRegistry.Make(envName);
                        if (env == null)
                        {
                            testResult["error_details"] = new List<string> { $"Environment {envName} not found in registry" };
                            results[agentName][envName] = testResult;
                            continue;
                        }
                        testResult["environment_creation"] = "PASS";
                        Console.WriteLine($"✓ Environment {envName} created");
                          // Step 2: Create agent
                        object agent = null;
                        switch (agentName)
                        {
                            case "A2CAgent": agent = new A2CAgent(env); break;
                            case "ACERAgent": agent = new ACERAgent(env); break;
                            case "ACKTRAgent": agent = new ACKTRAgent(env); break;
                            case "DDPGAgent": agent = new DDPGAgent(env); break;
                            case "TRPOAgent": agent = new TRPOAgent(env); break;
                            case "HERAgent": agent = new HERAgent(env); break;
                            case "GAILAgent": agent = new GAILAgent(env); break;
                            case "DQNAgent": agent = new DQNAgent(env, new DQNConfig()); break;
                            case "PPOAgent": agent = new PPOAgent(env, new PPOConfig()); break;
                            default:
                                testResult["error_details"] = new List<string> { $"Unknown agent: {agentName}" };
                                results[agentName][envName] = testResult;
                                continue;
                        }
                        testResult["agent_creation"] = "PASS";
                        Console.WriteLine($"✓ Agent {agentName} created");
                        
                        // Step 3: Reset environment and test state conversion
                        var state = ((dynamic)env).Reset();
                        Console.WriteLine($"✓ Environment reset, state type: {state.GetType()}, value: {state}");
                        
                        // Step 4: Test action generation
                        var action = ((dynamic)agent).Act(state);
                        testResult["state_conversion"] = "PASS";
                        testResult["action_generation"] = "PASS";
                        Console.WriteLine($"✓ Action generated: {action} (type: {action.GetType()})");
                          // Step 5: Test environment step
                        var stepResult = ((dynamic)env).Step(action);
                        var nextState = stepResult.Item1;  // state
                        var reward = stepResult.Item2;     // reward
                        var done = stepResult.Item3;       // done
                        Console.WriteLine($"✓ Environment step successful. Reward: {reward}, Done: {done}");
                        
                        // Step 6: Test learning call
                        ((dynamic)agent).Learn(state, action, reward, nextState, done);
                        testResult["learning_call"] = "PASS";
                        Console.WriteLine($"✓ Learning call successful");
                        
                        // Step 7: Test multiple steps
                        for (int i = 0; i < 5; i++)
                        {
                            state = nextState;
                            if (done)
                            {
                                state = ((dynamic)env).Reset();
                                ((dynamic)agent).Reset();
                            }
                              action = ((dynamic)agent).Act(state);
                            stepResult = ((dynamic)env).Step(action);
                            nextState = stepResult.Item1;  // state
                            reward = stepResult.Item2;     // reward
                            done = stepResult.Item3;       // done
                            
                            ((dynamic)agent).Learn(state, action, reward, nextState, done);
                        }
                        testResult["multiple_steps"] = "PASS";
                        Console.WriteLine($"✓ Multiple steps successful");
                        
                        Console.WriteLine($"🎉 {agentName} + {envName} = FULLY COMPATIBLE");
                        
                    }
                    catch (Exception ex)
                    {
                        var errorList = (List<string>)testResult["error_details"];
                        errorList.Add($"Exception: {ex.Message}");
                        if (ex.InnerException != null)
                            errorList.Add($"Inner: {ex.InnerException.Message}");
                        errorList.Add($"StackTrace: {ex.StackTrace}");
                        
                        Console.WriteLine($"❌ FAILED: {ex.Message}");
                        File.AppendAllText(logFile, $"{DateTime.Now}: ERROR in {agentName}+{envName}: {ex.Message}\n{ex.StackTrace}\n");
                    }
                    
                    results[agentName][envName] = testResult;
                }
            }
            
            // Generate compatibility matrix
            Console.WriteLine("\n=== COMPATIBILITY MATRIX ===");
            Console.Write("Agent\t\t");
            foreach (var env in environments)
            {
                Console.Write($"{env}\t");
            }
            Console.WriteLine();
            
            foreach (var agent in agents)
            {
                Console.Write($"{agent}\t");
                foreach (var env in environments)
                {
                    var envResults = (Dictionary<string, object>)results[agent][env];
                    bool allPass = (string)envResults["multiple_steps"] == "PASS";
                    Console.Write(allPass ? "✓\t\t" : "❌\t\t");
                }
                Console.WriteLine();
            }
            
            // Save detailed results to JSON
            var jsonResults = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(resultsFile, jsonResults);
            
            Console.WriteLine($"\nDetailed results saved to: {resultsFile}");
            Console.WriteLine($"Log saved to: {logFile}");
        }
    }
}
