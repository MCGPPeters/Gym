using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Gymnasium;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Gymnasium.UI.Models;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Previewer;
using QuestPDF.Elements;

namespace Gymnasium.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public string Greeting { get; } = "Welcome to Avalonia!";    public ObservableCollection<string> Environments { get; } = new()
    {
        "CartPole-v1", "MountainCar-v0", "MountainCarContinuous-v0", "Acrobot-v1", "Pendulum-v1",
        "FrozenLake-v1", "Taxi-v3", "Blackjack-v1", "CliffWalking-v0",
        "LunarLander-v2", "BipedalWalker-v3", "CarRacing-v2", 
        "Pong-v4", "Breakout-v4", "SpaceInvaders-v4", "AtariStub-v0", "MujocoStub-v0"
    };
    private string? _selectedEnvironment;
    public string? SelectedEnvironment
    {
        get => _selectedEnvironment;
        set
        {
            if (SetProperty(ref _selectedEnvironment, value))
            {
                try
                {
                    System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: SelectedEnvironment changed to {value ?? "null"}\n");
                    System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CanStartTraining is now {CanStartTraining}\n");
                }
                catch { /* Ignore logging errors */ }
                
                UpdateEnvironmentInfo();
                (StartTrainingCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<string> Agents { get; } = new() { "RandomAgent (Built-in)" };
    private string? _selectedAgent;
    public string? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (SetProperty(ref _selectedAgent, value))
            {
                try
                {
                    System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: SelectedAgent changed to {value ?? "null"}\n");
                    System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CanStartTraining is now {CanStartTraining}\n");
                }
                catch { /* Ignore logging errors */ }
                
                (StartTrainingCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                // UpdateAgentInfo not needed as this info is set elsewhere
            }
        }
    }

    public int Episodes { get; set; } = 100;
    public int StepsPerEpisode { get; set; } = 200;

    public ICommand StartTrainingCommand { get; }

    public object? EnvironmentView { get; set; } = new Views.EnvironmentRenderView();
    public object? TrainingStatsView { get; set; } = new Views.TrainingStatsView();
    public object? RewardChartView { get; set; } = new Views.RewardChartView();
    public object? EpisodeLengthChartView { get; set; } = new Views.EpisodeLengthChartView();
    public object? LossChartView { get; set; } = new Views.LossChartView();
    public object? PerEpisodeTableView { get; set; } = new Views.PerEpisodeTableView();
    public object? BestTrajectoryTableView { get; set; } = new Views.TrajectoryTableView();
    public object? WorstTrajectoryTableView { get; set; } = new Views.TrajectoryTableView();

    private bool _isTraining;
    public bool IsTraining
    {
        get => _isTraining;
        set 
        { 
            SetProperty(ref _isTraining, value);
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: IsTraining changed to {value}\n");
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CanStartTraining is now {CanStartTraining}\n");
        }
    }

    public bool CanStartTraining 
    { 
        get 
        {
            bool canStart = !IsTraining && !string.IsNullOrEmpty(SelectedEnvironment) && !string.IsNullOrEmpty(SelectedAgent);
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CanStartTraining calculated: {canStart} (IsTraining={IsTraining}, SelectedEnv={SelectedEnvironment ?? "null"}, SelectedAgent={SelectedAgent ?? "null"})\n");
            return canStart;
        }
    }

    private string? _pluginError;
    public string? PluginError
    {
        get => _pluginError;
        set => SetProperty(ref _pluginError, value);
    }
    public IRelayCommand AddPluginDllCommand => new AsyncRelayCommand(AddPluginDllAsync);

    private string _trainingStatsSummary = string.Empty;
    public string TrainingStatsSummary
    {
        get => _trainingStatsSummary;
        set => SetProperty(ref _trainingStatsSummary, value);
    }

    private List<bool> _episodeSuccesses = new();
    private string _reportExportError = string.Empty;
    public string ReportExportError
    {
        get => _reportExportError;
        set => SetProperty(ref _reportExportError, value);
    }
    public IRelayCommand ExportReportCommand => new AsyncRelayCommand(ExportReportAsync);
    public IRelayCommand ExportPdfReportCommand => new AsyncRelayCommand(ExportPdfReportAsync);

    private int? _bestEpisodeIndex = null;
    private int? _worstEpisodeIndex = null;
    private List<EpisodeTrajectory>? _bestEpisodeTrajectory = null;
    private List<EpisodeTrajectory>? _worstEpisodeTrajectory = null;    
    public MainWindowViewModel()
    {
        try
        {
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: MainWindowViewModel constructor starting\n");
              // Register all environments first!
            Gymnasium.GymnasiumRegistration.RegisterAll();
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: Environments registered\n");
            
            // Test if registration worked
            try
            {
                var testEnv = Gymnasium.EnvRegistry.Make("CartPole-v1");
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CartPole-v1 registration test: SUCCESS\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: CartPole-v1 registration test: FAILED - {ex.Message}\n");
            }
            
            DiscoverAgentPlugins();
            StartTrainingCommand = new AsyncRelayCommand(StartTraining, () => CanStartTraining);
            
            // Default selections
            if (Environments.Count > 0 && SelectedEnvironment == null)
            {
                SelectedEnvironment = Environments[0];
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: Default environment set to {SelectedEnvironment}\n");
            }
            
            if (Agents.Count > 0 && SelectedAgent == null)
            {
                SelectedAgent = Agents[0];
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: Default agent set to {SelectedAgent}\n");
            }
            
            // Setup duration timer
            durationTimer = new System.Timers.Timer(1000);
            durationTimer.Elapsed += (s, e) => 
            {
                if (sessionStartTime.HasValue && IsTraining)
                {
                    TimeSpan duration = DateTime.Now - sessionStartTime.Value;
                    Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        SessionDuration = $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
                    });
                }
            };
            
            // Set default environment info
            UpdateEnvironmentInfo();
            
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: MainWindowViewModel constructor completed. CanStartTraining={CanStartTraining}\n");
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: Command CanExecute={StartTrainingCommand.CanExecute(null)}\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: ERROR in constructor: {ex.Message}\n{ex.StackTrace}\n");
        }
    }

    private void SetIsTraining(bool value)
    {
        if (_isTraining != value)
        {
            _isTraining = value;
            OnPropertyChanged(nameof(IsTraining));
            OnPropertyChanged(nameof(CanStartTraining));
            (StartTrainingCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private List<double> _rewardHistory = new();
    private List<int> _episodeLengths = new();
    private List<double> _lossHistory = new();
    private List<EpisodeStats> _perEpisodeStats = new();

    private async Task StartTraining()
    {
        try
        {
            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: StartTraining method called\n");
            
            if (string.IsNullOrEmpty(SelectedEnvironment) || string.IsNullOrEmpty(SelectedAgent))
            {
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Missing environment or agent selection\n");
                return;
            }
            
            SetIsTraining(true);
            StatusMessage = "Initializing training...";
            sessionStartTime = DateTime.Now;
            durationTimer?.Start();
            TrainingProgress = 0;
            CurrentEpisode = "0";
            LastReward = "N/A";
            SuccessRate = "N/A";

            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Training initialization complete\n");

            _rewardHistory.Clear();
            _episodeLengths.Clear();
            _episodeSuccesses.Clear();
            _lossHistory.Clear();
            _perEpisodeStats.Clear();
            _bestEpisodeIndex = null;
            _worstEpisodeIndex = null;
            _bestEpisodeTrajectory = null;
            _worstEpisodeTrajectory = null;
            double bestReward = double.MinValue;
            double worstReward = double.MaxValue;
            List<EpisodeTrajectory>? bestTraj = null;
            List<EpisodeTrajectory>? worstTraj = null;
            try
            {
                // Extra debug info for troubleshooting
                StatusMessage = $"Training started. SelectedAgent: {SelectedAgent}, SelectedEnvironment: {SelectedEnvironment}. Agents loaded: {string.Join(", ", _agentPlugins.Keys)}";
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: {StatusMessage}\n");                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Creating environment: {SelectedEnvironment}\n");
                
                dynamic env = EnvRegistry.Make(SelectedEnvironment);
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Environment created: {(env != null ? "success" : "FAILED")}\n");
                
                if (!_agentPlugins.ContainsKey(SelectedAgent))
                {
                    StatusMessage = $"Agent plugin not found: {SelectedAgent}. Available: {string.Join(", ", _agentPlugins.Keys)}";
                    System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: ERROR - {StatusMessage}\n");
                    SetIsTraining(false);
                    return;
                }
                
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Getting agent plugin: {SelectedAgent}\n");
                var agentPlugin = _agentPlugins[SelectedAgent];
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Agent plugin retrieved: {(agentPlugin != null ? "success" : "FAILED")}\n");
                
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Creating agent\n");
                var agent = agentPlugin.CreateAgent(env);
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Agent created: {(agent != null ? "success" : "FAILED")}\n");
                
                var getLoss = agentPlugin.GetLossFetcher(agent);
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Loss fetcher retrieved: {(getLoss != null ? "success" : "FAILED")}\n");
                
                int totalEpisodes = Episodes;
                int maxSteps = StepsPerEpisode;
                var rewards = new double[totalEpisodes];
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Starting episodes loop. TotalEpisodes={totalEpisodes}, MaxSteps={maxSteps}\n");
                
                for (int ep = 0; ep < totalEpisodes; ep++)
                {
                    System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Starting episode {ep+1}/{totalEpisodes}\n");

                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        CurrentEpisode = $"{ep + 1}/{totalEpisodes}";
                        TrainingProgress = (double)(ep + 1) / totalEpisodes * 100;
                        StatusMessage = $"Running episode {ep + 1}...";
                    });

                    dynamic state = env.Reset();
                    double totalReward = 0;
                    int steps = 0;
                    bool success = false;
                    double episodeLoss = 0;
                    int lossCount = 0;
                    var trajectory = new List<EpisodeTrajectory>();
                    for (int step = 0; step < maxSteps; step++)
                    {
                        dynamic action = agent.Act(state);                        var result = env.Step(action);
                        var nextState = result.Item1;  // state
                        var reward = result.Item2;     // reward
                        var done = result.Item3;       // done
                        var info = result.Item4;       // info
                        totalReward += reward;
                        
                        // Log environment rendering attempts
                        System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Step {step} - Action taken, reward={reward}, done={done}\n");
                        
                        // CartPole rendering example
                        if (SelectedEnvironment == "CartPole-v1" && EnvironmentView is Views.EnvironmentRenderView renderView)
                        {
                            try 
                            {
                                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Rendering CartPole\n");
                                float x = 0, theta = 0;
                                if (nextState is ValueTuple<float, float, float, float> tuple)
                                {
                                    x = tuple.Item1;
                                    theta = tuple.Item3;
                                }
                                await Dispatcher.UIThread.InvokeAsync(() => {
                                    try
                                    {
                                        renderView.RenderCartPole(x, theta);
                                        System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: CartPole rendered successfully\n");
                                    }
                                    catch (Exception renderEx)
                                    {
                                        System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: CartPole render ERROR: {renderEx.Message}\n{renderEx.StackTrace}\n");
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: CartPole rendering preparation ERROR: {ex.Message}\n{ex.StackTrace}\n");
                            }
                        }
                        // MountainCar rendering
                        if ((SelectedEnvironment == "MountainCar-v0" || SelectedEnvironment == "MountainCarContinuous-v0") && EnvironmentView is Views.EnvironmentRenderView renderView2)
                        {
                            float position = 0, velocity = 0;
                            if (nextState is ValueTuple<float, float> tuple)
                            {
                                position = tuple.Item1;
                                velocity = tuple.Item2;
                            }
                            await Dispatcher.UIThread.InvokeAsync(() => renderView2.RenderMountainCar(position, velocity));
                        }
                        // Acrobot rendering
                        if (SelectedEnvironment == "Acrobot-v1" && EnvironmentView is Views.EnvironmentRenderView renderView3)
                        {
                            if (nextState is float[] arr && arr.Length >= 6)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView3.RenderAcrobot(arr));
                            }
                        }
                        // Pendulum rendering
                        if (SelectedEnvironment == "Pendulum-v1" && EnvironmentView is Views.EnvironmentRenderView renderView4)
                        {
                            if (nextState is float[] arr && arr.Length >= 3)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView4.RenderPendulum(arr));
                            }
                        }
                        // FrozenLake rendering
                        if (SelectedEnvironment == "FrozenLake-v1" && EnvironmentView is Views.EnvironmentRenderView renderView5)
                        {
                            int nrow = 4, ncol = 4, goal = 15;
                            var holes = new HashSet<int> { 5, 7, 11, 12 };
                            if (nextState is int s)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView5.RenderFrozenLake(s, nrow, ncol, holes, goal));
                            }
                        }
                        // Taxi rendering
                        if (SelectedEnvironment == "Taxi-v3" && EnvironmentView is Views.EnvironmentRenderView renderView6)
                        {
                            if (nextState is int s)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView6.RenderTaxi(s));
                            }
                        }
                        // CliffWalking rendering
                        if (SelectedEnvironment == "CliffWalking-v0" && EnvironmentView is Views.EnvironmentRenderView renderView7)
                        {
                            int nrow = 4, ncol = 12, goal = 47;
                            var cliff = new HashSet<int>();
                            for (int c = 1; c < 11; c++) cliff.Add(36 + c);
                            if (nextState is int s)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView7.RenderCliffWalking(s, nrow, ncol, cliff, goal));
                            }
                        }
                        // Blackjack rendering
                        if (SelectedEnvironment == "Blackjack-v1" && EnvironmentView is Views.EnvironmentRenderView renderView8)
                        {
                            if (nextState is ValueTuple<int, int, bool> tuple)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView8.RenderBlackjack(tuple));
                            }
                        }
                        // LunarLander rendering
                        if (SelectedEnvironment == "LunarLander-v2" && EnvironmentView is Views.EnvironmentRenderView renderView9)
                        {
                            if (nextState is float[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView9.RenderLunarLander(arr));
                            }
                        }
                        // BipedalWalker rendering
                        if (SelectedEnvironment == "BipedalWalker-v3" && EnvironmentView is Views.EnvironmentRenderView renderView10)
                        {
                            if (nextState is float[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView10.RenderBipedalWalker(arr));
                            }
                        }                        // CarRacing rendering
                        if (SelectedEnvironment == "CarRacing-v2" && EnvironmentView is Views.EnvironmentRenderView renderView11)
                        {
                            if (nextState is float[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView11.RenderCarRacing(arr));
                            }
                        }
                        // Pong rendering
                        if (SelectedEnvironment == "Pong-v4" && EnvironmentView is Views.EnvironmentRenderView renderViewPong)
                        {
                            if (nextState is byte[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderViewPong.RenderPong(arr));
                            }
                        }
                        // Breakout rendering
                        if (SelectedEnvironment == "Breakout-v4" && EnvironmentView is Views.EnvironmentRenderView renderViewBreakout)
                        {
                            if (nextState is byte[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderViewBreakout.RenderBreakout(arr));
                            }
                        }
                        // SpaceInvaders rendering
                        if (SelectedEnvironment == "SpaceInvaders-v4" && EnvironmentView is Views.EnvironmentRenderView renderViewSpaceInvaders)
                        {
                            if (nextState is byte[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderViewSpaceInvaders.RenderSpaceInvaders(arr));
                            }
                        }
                        // AtariStub rendering
                        if (SelectedEnvironment == "AtariStub-v0" && EnvironmentView is Views.EnvironmentRenderView renderView12)
                        {
                            if (nextState is int[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView12.RenderAtariStub(arr));
                            }
                        }
                        // MujocoStub rendering
                        if (SelectedEnvironment == "MujocoStub-v0" && EnvironmentView is Views.EnvironmentRenderView renderView13)
                        {
                            if (nextState is float[] arr)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => renderView13.RenderMujocoStub(arr));
                            }
                        }
                        trajectory.Add(new EpisodeTrajectory {
                            Step = step + 1,
                            State = state,
                            Action = action,
                            Reward = reward                        });
                        
                        // CRITICAL: Call agent.Learn() so the agent actually learns from the experience
                        try
                        {
                            agent.Learn(state, action, reward, nextState, done);
                            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Agent.Learn() called successfully\n");
                        }
                        catch (Exception learnEx)
                        {
                            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Agent.Learn() ERROR: {learnEx.Message}\n{learnEx.StackTrace}\n");
                        }
                        
                        steps++;
                        if (done)
                        {
                            // Heuristic: success if reward > 0 or done at goal (customize per env)
                            success = reward > 0 || (SelectedEnvironment?.Contains("FrozenLake") == true && reward == 1.0);
                            break;
                        }
                        state = nextState;
                    }
                    rewards[ep] = totalReward;
                    _rewardHistory.Add(totalReward);
                    _episodeLengths.Add(steps);
                    _episodeSuccesses.Add(success);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            LastReward = totalReward.ToString("F2");
                            var last100Successes = _episodeSuccesses.Count > 100 ? _episodeSuccesses.GetRange(_episodeSuccesses.Count - 100, 100) : _episodeSuccesses;
                            SuccessRate = $"{CalculateSuccessRate(last100Successes):P1}";
                            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Episode {ep+1} UI updated - LastReward={LastReward}, SuccessRate={SuccessRate}\n");
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Episode UI update ERROR: {ex.Message}\n{ex.StackTrace}\n");
                        }
                    });
                    
                    System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Episode {ep+1} completed - Steps={steps}, TotalReward={totalReward}, Success={success}\n");
                    
                    _perEpisodeStats.Add(new EpisodeStats {
                        Episode = ep + 1,
                        Reward = totalReward,
                        Length = steps,
                        Loss = (lossCount > 0) ? (double?)(episodeLoss / lossCount) : null
                    });
                    // Compute stats
                    var rewardsWindow = _rewardHistory.Count > 100 ? _rewardHistory.GetRange(_rewardHistory.Count - 100, 100) : _rewardHistory;
                    var lengthsWindow = _episodeLengths.Count > 100 ? _episodeLengths.GetRange(_episodeLengths.Count - 100, 100) : _episodeLengths;
                    string stats = $"Episode {ep+1}/{totalEpisodes}, Reward: {totalReward}, Steps: {steps}\n" +
                        $"Reward (last 100): mean={Mean(rewardsWindow):F2}, min={Min(rewardsWindow):F2}, max={Max(rewardsWindow):F2}, std={Std(rewardsWindow):F2}\n" +
                        $"Length (last 100): mean={Mean(lengthsWindow.Select(x=>(double)x).ToList()):F2}, min={Min(lengthsWindow.Select(x=>(double)x).ToList()):F2}, max={Max(lengthsWindow.Select(x=>(double)x).ToList()):F2}, std={Std(lengthsWindow.Select(x=>(double)x).ToList()):F2}\n" +
                        $"Success rate (last 100): {CalculateSuccessRate(_episodeSuccesses):P1}";
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        TrainingStatsView = stats;
                        TrainingStatsSummary = stats;
                    });
                    if (RewardChartView is Views.RewardChartView chartView)
                    {
                        var last100 = _rewardHistory.Count > 100 ? _rewardHistory.GetRange(_rewardHistory.Count - 100, 100) : _rewardHistory;
                        await Dispatcher.UIThread.InvokeAsync(() => chartView.RenderRewards(last100));
                    }
                    if (EpisodeLengthChartView is Views.EpisodeLengthChartView lenChartView)
                    {
                        var last100 = _episodeLengths.Count > 100 ? _episodeLengths.GetRange(_episodeLengths.Count - 100, 100) : _episodeLengths;
                        await Dispatcher.UIThread.InvokeAsync(() => lenChartView.RenderLengths(last100));
                    }
                    if (LossChartView is Views.LossChartView lossChartView)
                    {
                        var last100 = _lossHistory.Count > 100 ? _lossHistory.GetRange(_lossHistory.Count - 100, 100) : _lossHistory;
                        await Dispatcher.UIThread.InvokeAsync(() => lossChartView.RenderLosses(last100));
                    }
                    if (PerEpisodeTableView is Views.PerEpisodeTableView tableView)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => tableView.SetEpisodes(_perEpisodeStats));
                    }
                }
                _bestEpisodeTrajectory = bestTraj;
                _worstEpisodeTrajectory = worstTraj;
                StatusMessage = "Training completed.";
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Training successfully completed\n");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Training error: {ex.Message}";
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: TRAINING ERROR: {ex.Message}\n{ex.StackTrace}\n");
                
                // Try to get more context on the error
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Error context - EnvironmentView null? {EnvironmentView == null}\n");
                if (EnvironmentView != null)
                {
                    System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: EnvironmentView type: {EnvironmentView.GetType().FullName}\n");
                }
                
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Inner exception: {ex.InnerException?.Message ?? "none"}\n");
                
                if (ex.InnerException != null)
                {
                    System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Inner stack trace: {ex.InnerException.StackTrace}\n");
                }
            }
            finally
            {
                SetIsTraining(false);
                durationTimer?.Stop();
                if (sessionStartTime.HasValue)
                {
                    TimeSpan finalDuration = DateTime.Now - sessionStartTime.Value;
                    SessionDuration = $"{finalDuration.Hours:00}:{finalDuration.Minutes:00}:{finalDuration.Seconds:00}";
                }
                System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: Training process finished and cleaned up\n");
            }
        }
        catch (Exception ex)
        {            System.IO.File.AppendAllText("training_debug.log", $"{DateTime.Now}: CRITICAL ERROR in StartTraining: {ex.Message}\n{ex.StackTrace}\n");
            StatusMessage = $"Critical error: {ex.Message}";
            SetIsTraining(false);
        }
    }

    private async Task AddPluginDllAsync()
    {
        var dlg = new OpenFileDialog { AllowMultiple = false, Filters = { new FileDialogFilter { Name = "DLL", Extensions = { "dll" } } } };
        var window = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var paths = await dlg.ShowAsync(window);
        if (paths != null && paths.Length > 0 && System.IO.File.Exists(paths[0]))
        {
            string pluginDir = Path.Combine(AppContext.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);
            string dest = Path.Combine(pluginDir, Path.GetFileName(paths[0]));
            try
            {
                File.Copy(paths[0], dest, true);
                PluginError = null;
                ReloadPlugins();
            }
            catch (Exception ex)
            {
                PluginError = $"Failed to copy DLL: {ex.Message}";
            }
        }
    }

    public void DiscoverAgentPlugins()
    {
        var plugins = new List<IAgentPlugin>();
        var assemblies = new List<Assembly> { typeof(MainWindowViewModel).Assembly };
        string pluginDir = Path.Combine(AppContext.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginDir))
            Directory.CreateDirectory(pluginDir);
        PluginError = null;
        if (Directory.Exists(pluginDir))
        {
            foreach (var dll in Directory.GetFiles(pluginDir, "*.dll"))
            {
                try { assemblies.Add(Assembly.LoadFrom(dll)); }
                catch (Exception ex) { PluginError = $"Failed to load {Path.GetFileName(dll)}: {ex.Message}"; }
            }
        }
        try
        {
            var configuration = new ContainerConfiguration().WithAssemblies(assemblies);
            using var container = configuration.CreateContainer();
            plugins.AddRange(container.GetExports<IAgentPlugin>());
            // Always add built-in RandomAgent if not present
            if (!plugins.Any(p => p.Name == "RandomAgent (Built-in)"))
            {
                plugins.Add(new Gymnasium.UI.Agents.RandomAgentPlugin());
            }
            Agents.Clear();
            foreach (var plugin in plugins)
                Agents.Add(plugin.Name);
            _agentPlugins = plugins.ToDictionary(p => p.Name);
        }
        catch (Exception ex)
        {
            PluginError = $"Plugin discovery error: {ex.Message}";
        }
    }

    private Dictionary<string, IAgentPlugin> _agentPlugins = new();

    public IRelayCommand SaveSessionCommand => new AsyncRelayCommand(SaveSessionAsync);
    public IRelayCommand LoadSessionCommand => new AsyncRelayCommand(LoadSessionAsync);
    public IRelayCommand ReloadPluginsCommand => new RelayCommand(ReloadPlugins);

    private async Task SaveSessionAsync()
    {
        var session = new TrainingSession
        {
            Environment = SelectedEnvironment,
            Agent = SelectedAgent,
            Episodes = Episodes,
            StepsPerEpisode = StepsPerEpisode,
            RewardHistory = new List<double>(_rewardHistory),
            EpisodeLengths = new List<int>(_episodeLengths),
            LossHistory = new List<double>(_lossHistory),
            PerEpisodeStats = new List<EpisodeStats>(_perEpisodeStats),
            Timestamp = DateTime.Now,
            BestEpisodeIndex = _bestEpisodeIndex,
            WorstEpisodeIndex = _worstEpisodeIndex,
            BestEpisodeTrajectory = _bestEpisodeTrajectory,
            WorstEpisodeTrajectory = _worstEpisodeTrajectory,
        };
        var dlg = new SaveFileDialog { Filters = { new FileDialogFilter { Name = "Session", Extensions = { "json" } } }, DefaultExtension = "json" };
        var window = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var path = await dlg.ShowAsync(window);
        if (!string.IsNullOrEmpty(path))
        {
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(path, json);
        }
    }
    private async Task LoadSessionAsync()
    {
        var dlg = new OpenFileDialog { AllowMultiple = false, Filters = { new FileDialogFilter { Name = "Session", Extensions = { "json" } } } };
        var window = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var paths = await dlg.ShowAsync(window);
        if (paths != null && paths.Length > 0 && System.IO.File.Exists(paths[0]))
        {
            var json = await System.IO.File.ReadAllTextAsync(paths[0]);
            var session = JsonSerializer.Deserialize<TrainingSession>(json);
            if (session != null)
            {
                SelectedEnvironment = session.Environment;
                SelectedAgent = session.Agent;
                Episodes = session.Episodes;
                StepsPerEpisode = session.StepsPerEpisode;
                _rewardHistory = session.RewardHistory ?? new List<double>();
                _episodeLengths = session.EpisodeLengths ?? new List<int>(); // Added this line
                _lossHistory = session.LossHistory ?? new List<double>();
                _perEpisodeStats = session.PerEpisodeStats ?? new List<EpisodeStats>();
                _episodeSuccesses = _perEpisodeStats.Select(s => s.Reward > 0).ToList(); // Basic heuristic for now

                _bestEpisodeIndex = session.BestEpisodeIndex;
                _worstEpisodeIndex = session.WorstEpisodeIndex;
                _bestEpisodeTrajectory = session.BestEpisodeTrajectory;
                _worstEpisodeTrajectory = session.WorstEpisodeTrajectory;
                
                StatusMessage = "Session loaded.";
                TrainingProgress = 0; // Or calculate based on loaded data if applicable
                SessionDuration = "N/A"; // Or load/calculate if stored in session
                CurrentEpisode = _perEpisodeStats.Count > 0 ? _perEpisodeStats.Count.ToString() : "0";
                LastReward = _rewardHistory.LastOrDefault().ToString("F2");
                var successesToCalc = _episodeSuccesses.Count > 100 ? _episodeSuccesses.GetRange(_episodeSuccesses.Count - 100, 100) : _episodeSuccesses;
                SuccessRate = $"{CalculateSuccessRate(successesToCalc):P1}";

                OnPropertyChanged(nameof(SelectedEnvironment));
                OnPropertyChanged(nameof(SelectedAgent));
                OnPropertyChanged(nameof(Episodes));
                OnPropertyChanged(nameof(StepsPerEpisode));
                if (RewardChartView is Views.RewardChartView chartView)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => chartView.RenderRewards(_rewardHistory));
                }
                if (EpisodeLengthChartView is Views.EpisodeLengthChartView lenChartView) // Added this block
                {
                    await Dispatcher.UIThread.InvokeAsync(() => lenChartView.RenderLengths(_episodeLengths));
                }
                if (LossChartView is Views.LossChartView lossChartView)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => lossChartView.RenderLosses(_lossHistory));
                }
                if (PerEpisodeTableView is Views.PerEpisodeTableView tableView)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => tableView.SetEpisodes(_perEpisodeStats));
                }
                if (BestTrajectoryTableView is Views.TrajectoryTableView bestTable)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => bestTable.SetTrajectory(_bestEpisodeTrajectory));
                }
                if (WorstTrajectoryTableView is Views.TrajectoryTableView worstTable)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => worstTable.SetTrajectory(_worstEpisodeTrajectory));
                }
            }
        }
    }

    private void ReloadPlugins()
    {
        DiscoverAgentPlugins();
        UpdateEnvironmentInfo(); // Ensure info is updated after plugin reload
    }

    private static double CalculateSuccessRate(IReadOnlyList<bool> successes)
    {
        if (successes.Count == 0) return 0;
        return successes.Count(s => s) / (double)successes.Count;
    }

    private static double Mean(IReadOnlyList<double> data) => data.Count == 0 ? 0 : data.Average();
    private static double Min(IReadOnlyList<double> data) => data.Count == 0 ? 0 : data.Min();
    private static double Max(IReadOnlyList<double> data) => data.Count == 0 ? 0 : data.Max();
    private static double Std(IReadOnlyList<double> data)
    {
        if (data.Count == 0) return 0;
        double mean = data.Average();
        double sumSq = data.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sumSq / data.Count);
    }
    private static double Median(IReadOnlyList<double> data)
    {
        if (data.Count == 0) return 0;
        var sorted = data.OrderBy(x => x).ToArray();
        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }
    private static double Percentile(IReadOnlyList<double> data, double percentile)
    {
        if (data.Count == 0) return 0;
        var sorted = data.OrderBy(x => x).ToArray();
        double pos = (percentile / 100.0) * (sorted.Length - 1);
        int idx = (int)pos;
        if (idx >= sorted.Length - 1) return sorted[^1];
        double frac = pos - idx;
        return sorted[idx] * (1 - frac) + sorted[idx + 1] * frac;
    }
    private static double Median(IReadOnlyList<int> data) => Median(data.Select(x => (double)x).ToList());
    private static double Percentile(IReadOnlyList<int> data, double percentile) => Percentile(data.Select(x => (double)x).ToList(), percentile);

    private async Task ExportReportAsync()
    {
        try
        {
            var dlg = new SaveFileDialog { Filters = { new FileDialogFilter { Name = "HTML", Extensions = { "html" } } }, DefaultExtension = "html" };
            var window = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var path = await dlg.ShowAsync(window);
            if (string.IsNullOrEmpty(path)) return;
            // Generate SVGs for charts
            string rewardSvg = Views.ChartExportHelper.RenderRewardChartSvg(_rewardHistory);
            string lengthSvg = Views.ChartExportHelper.RenderEpisodeLengthChartSvg(_episodeLengths);
            string lossSvg = Views.ChartExportHelper.RenderLossChartSvg(_lossHistory);
            // Compose HTML
            string html = $@"<html><head><title>Gymnasium Training Report</title><style>body{{font-family:sans-serif}}pre{{background:#f8f8f8;padding:8px}} table,th,td{{border:1px solid #ccc;border-collapse:collapse;padding:2px 6px}}</style></head><body>
<h1>Gymnasium Training Report</h1>
<p><b>Date:</b> {DateTime.Now}</p>
<h2>Configuration</h2>
<pre>Environment: {SelectedEnvironment}
Agent: {SelectedAgent}
Episodes: {Episodes}
Steps per Episode: {StepsPerEpisode}</pre>
<h2>Summary Statistics (last 100)</h2>
<pre>Reward: mean={Mean(_rewardHistory):F2}, min={Min(_rewardHistory):F2}, max={Max(_rewardHistory):F2}, std={Std(_rewardHistory):F2}
Length: mean={Mean(_episodeLengths.Select(x=>(double)x).ToList()):F2}, min={Min(_episodeLengths.Select(x=>(double)x).ToList()):F2}, max={Max(_episodeLengths.Select(x=>(double)x).ToList()):F2}, std={Std(_episodeLengths.Select(x=>(double)x).ToList()):F2}
Success rate: {CalculateSuccessRate(_episodeSuccesses):P1}</pre>
<h2>Reward Curve</h2>{rewardSvg}
<h2>Episode Length Curve</h2>{lengthSvg}
<h2>Loss Curve</h2>{lossSvg}
<h2>Per-Episode Table</h2><table><tr><th>Ep</th><th>Reward</th><th>Length</th><th>Loss</th></tr>";
            foreach (var ep in _perEpisodeStats)
            {
                html += $"<tr><td>{ep.Episode}</td><td>{ep.Reward:F2}</td><td>{ep.Length}</td><td>{(ep.Loss.HasValue ? ep.Loss.Value.ToString("F4") : "")}</td></tr>";
            }
            html += "</table>";
            if (_bestEpisodeTrajectory != null && _bestEpisodeTrajectory.Count > 0)
            {
                html += "<h2>Best Episode Trajectory</h2><table><tr><th>Step</th><th>State</th><th>Action</th><th>Reward</th></tr>";
                foreach (var t in _bestEpisodeTrajectory)
                {
                    html += $"<tr><td>{t.Step}</td><td>{t.State}</td><td>{t.Action}</td><td>{t.Reward:F2}</td></tr>";
                }
                html += "</table>";
            }
            if (_worstEpisodeTrajectory != null && _worstEpisodeTrajectory.Count > 0)
            {
                html += "<h2>Worst Episode Trajectory</h2><table><tr><th>Step</th><th>State</th><th>Action</th><th>Reward</th></tr>";
                foreach (var t in _worstEpisodeTrajectory)
                {
                    html += $"<tr><td>{t.Step}</td><td>{t.State}</td><td>{t.Action}</td><td>{t.Reward:F2}</td></tr>";
                }
                html += "</table>";
            }
            html += "</body></html>";
            await File.WriteAllTextAsync(path, html);
            ReportExportError = string.Empty;
        }
        catch (Exception ex)
        {
            ReportExportError = $"Export failed: {ex.Message}";
        }
    }
    private async Task ExportPdfReportAsync()
    {
        try
        {
            var dlg = new SaveFileDialog { Filters = { new FileDialogFilter { Name = "PDF", Extensions = { "pdf" } } }, DefaultExtension = "pdf" };
            var window = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var path = await dlg.ShowAsync(window);
            if (string.IsNullOrEmpty(path)) return;
            // Generate SVGs for charts
            string rewardSvg = Views.ChartExportHelper.RenderRewardChartSvg(_rewardHistory);
            string lengthSvg = Views.ChartExportHelper.RenderEpisodeLengthChartSvg(_episodeLengths);
            string lossSvg = Views.ChartExportHelper.RenderLossChartSvg(_lossHistory);
            // Compute stats
            var rewardsWindow = _rewardHistory.Count > 100 ? _rewardHistory.GetRange(_rewardHistory.Count - 100, 100) : _rewardHistory;
            var lengthsWindow = _episodeLengths.Count > 100 ? _episodeLengths.GetRange(_episodeLengths.Count - 100, 100) : _episodeLengths;
            var rewardStats = new Dictionary<string, double>
            {
                {"mean", Mean(rewardsWindow)},
                {"min", Min(rewardsWindow)},
                {"max", Max(rewardsWindow)},
                {"std", Std(rewardsWindow)},
                {"median", Median(rewardsWindow)},
                {"p25", Percentile(rewardsWindow, 25)},
                {"p75", Percentile(rewardsWindow, 75)}
            };
            var lengthStats = new Dictionary<string, double>
            {
                {"mean", Mean(lengthsWindow.Select(x=>(double)x).ToList())},
                {"min", Min(lengthsWindow.Select(x=>(double)x).ToList())},
                {"max", Max(lengthsWindow.Select(x=>(double)x).ToList())},
                {"std", Std(lengthsWindow.Select(x=>(double)x).ToList())},
                {"median", Median(lengthsWindow.Select(x=>(double)x).ToList())},
                {"p25", Percentile(lengthsWindow.Select(x=>(double)x).ToList(), 25)},
                {"p75", Percentile(lengthsWindow.Select(x=>(double)x).ToList(), 75)}
            };
            double successRate = CalculateSuccessRate(_episodeSuccesses);
            Views.ReportPdfExporter.Export(
                path,
                SelectedEnvironment ?? "",
                SelectedAgent ?? "",
                Episodes,
                StepsPerEpisode,
                _rewardHistory,
                _episodeLengths,
                _episodeSuccesses,
                rewardSvg,
                lengthSvg,
                lossSvg,
                DateTime.Now.ToString("u"),
                rewardStats,
                lengthStats,
                successRate,
                _perEpisodeStats,
                _bestEpisodeTrajectory,
                _worstEpisodeTrajectory
            );
            ReportExportError = string.Empty;
        }
        catch (Exception ex)
        {
            ReportExportError = $"PDF export failed: {ex.Message}";
        }
    }    // Properties for UI status    [ObservableProperty]
    private bool isTraining = false;

    [ObservableProperty]
    private bool hasLossData = false;

    [ObservableProperty]
    private string statusMessage = "Ready to start training";

    [ObservableProperty]
    private double trainingProgress = 0;
    
    [ObservableProperty]
    private int selectedTabIndex = 0;
    
    [ObservableProperty]
    private bool showDetailsPanel = true;
    
    [ObservableProperty]
    private string lastReward = "0.0";
    
    [ObservableProperty]
    private string currentEpisode = "0";
    
    [ObservableProperty]
    private string successRate = "0%";
    
    [ObservableProperty]
    private string sessionDuration = "00:00:00";
    
    [ObservableProperty]
    private string environmentInfo = "";
    
    [ObservableProperty]
    private string agentInfo = "";
    
    [ObservableProperty]
    private bool showTutorial = false;
    
    private DateTime? sessionStartTime;
    private System.Timers.Timer? durationTimer;
    
    public IRelayCommand SwitchToChartsTabCommand => new RelayCommand(() => SelectedTabIndex = 0);
    public IRelayCommand SwitchToEpisodeDataTabCommand => new RelayCommand(() => SelectedTabIndex = 1);
    public IRelayCommand ToggleDetailsPanelCommand => new RelayCommand(() => ShowDetailsPanel = !ShowDetailsPanel);
    public IRelayCommand ToggleTutorialCommand => new RelayCommand(() => ShowTutorial = !ShowTutorial);

    private void UpdateEnvironmentInfo()
    {
        if (string.IsNullOrEmpty(SelectedEnvironment))
        {
            EnvironmentInfo = "Select an environment to begin";
            return;
        }
        
        switch (SelectedEnvironment)
        {
            case "CartPole-v1":
                EnvironmentInfo = "A pole is attached to a cart moving along a frictionless track. The goal is to prevent the pole from falling over by moving the cart left or right.";
                break;
            case "MountainCar-v0":
                EnvironmentInfo = "A car positioned between two mountains. The goal is to drive up the mountain on the right by building momentum from going back and forth.";
                break;
            case "MountainCarContinuous-v0":
                EnvironmentInfo = "Continuous version of MountainCar. The goal is to drive up the mountain on the right.";
                break;
            case "Acrobot-v1":
                EnvironmentInfo = "A two-link robot with the goal to swing the end of the lower link up to a given height.";
                break;
            case "Pendulum-v1":
                EnvironmentInfo = "A single-link pendulum with the goal to keep it upright.";
                break;
            case "FrozenLake-v1":
                EnvironmentInfo = "Agent must navigate a frozen lake from start to goal without falling in holes.";
                break;
            case "Taxi-v3":
                EnvironmentInfo = "A taxi must pick up and drop off passengers at designated locations.";
                break;
            case "Blackjack-v1":
                EnvironmentInfo = "Player must get as close as possible to 21 without going over, competing against a dealer.";
                break;            case "CliffWalking-v0":
                EnvironmentInfo = "Agent must navigate from start to goal along a cliff edge without falling off.";
                break;
            case "LunarLander-v2":
                EnvironmentInfo = "Spacecraft must land safely on a landing pad using thruster control.";
                break;
            case "BipedalWalker-v3":
                EnvironmentInfo = "Humanoid robot must learn to walk forward on rough terrain.";
                break;
            case "CarRacing-v2":
                EnvironmentInfo = "Racing car must complete laps around a randomly generated track.";
                break;
            case "Pong-v4":
                EnvironmentInfo = "Classic Atari Pong game. Control the right paddle to hit the ball past the opponent.";
                break;
            case "Breakout-v4":
                EnvironmentInfo = "Classic Atari Breakout game. Control the paddle to bounce the ball and destroy all bricks.";
                break;
            case "SpaceInvaders-v4":
                EnvironmentInfo = "Classic Atari Space Invaders game. Control the cannon to shoot alien invaders.";
                break;
            case "AtariStub-v0":
                EnvironmentInfo = "Placeholder for Atari game environments (development stub).";
                break;
            case "MujocoStub-v0":
                EnvironmentInfo = "Placeholder for MuJoCo physics environments (development stub).";
                break;
            default:
                EnvironmentInfo = "Advanced environment with complex dynamics.";
                break;
        }
        
        if (!string.IsNullOrEmpty(SelectedAgent))
        {
            AgentInfo = SelectedAgent.EndsWith("(Built-in)") 
                ? "Simple agent that takes random actions in the environment."
                : "Custom agent plugin that implements advanced decision-making strategies.";
        }
        else
        {
            AgentInfo = "Select an agent to begin training.";
        }
    }
}
