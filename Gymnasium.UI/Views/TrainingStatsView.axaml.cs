using Avalonia.Controls;
using System;

namespace Gymnasium.UI.Views;

public partial class TrainingStatsView : UserControl
{
    public TrainingStatsView()
    {
        InitializeComponent();
        DataContext = this;
    }

    // Properties for binding in the XAML
    public int CurrentEpisode { get; set; }
    public double LastReward { get; set; }
    public double AvgReward { get; set; }
    public double SuccessRate { get; set; }
    public double BestReward { get; set; }
    public double WorstReward { get; set; }
    public double AvgLength { get; set; }
    public string TrainingTime { get; set; } = "00:00:00";

    // Method to update the stats from outside
    public void UpdateStats(int episode, double lastReward, double avgReward, double successRate,
                           double bestReward, double worstReward, double avgLength, TimeSpan trainingTime)
    {
        CurrentEpisode = episode;
        LastReward = lastReward;
        AvgReward = avgReward;
        SuccessRate = successRate;
        BestReward = bestReward;
        WorstReward = worstReward;
        AvgLength = avgLength;
        TrainingTime = trainingTime.ToString(@"hh\:mm\:ss");

        // Trigger UI update
        this.InvalidateVisual();
    }
}
