using System;
using System.Collections.Generic;

namespace Gymnasium.UI.Models;

public class TrainingSession
{
    public string? Environment { get; set; }
    public string? Agent { get; set; }
    public int Episodes { get; set; }
    public int StepsPerEpisode { get; set; }
    public List<double> RewardHistory { get; set; } = new();
    public List<int> EpisodeLengths { get; set; } = new();
    public List<double> LossHistory { get; set; } = new();
    public List<EpisodeStats> PerEpisodeStats { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int? BestEpisodeIndex { get; set; }
    public int? WorstEpisodeIndex { get; set; }
    public List<EpisodeTrajectory>? BestEpisodeTrajectory { get; set; }
    public List<EpisodeTrajectory>? WorstEpisodeTrajectory { get; set; }
}

public class EpisodeStats
{
    public int Episode { get; set; }
    public double Reward { get; set; }
    public int Length { get; set; }
    public double? Loss { get; set; }
}

public class EpisodeTrajectory
{
    public int Step { get; set; }
    public object? State { get; set; }
    public object? Action { get; set; }
    public double Reward { get; set; }
}
