using Gymnasium.UI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gymnasium.UI.Views;

public static class ReportPdfExporter
{
    // Color scheme for consistent branding
    private static readonly string PrimaryColor = "#1976D2";
    private static readonly string SecondaryColor = "#4CAF50";
    private static readonly string AccentColor = "#FF9800";
    private static readonly string HeaderBgColor = "#F5F7FA";
    private static readonly string TextColor = "#424242";
    private static readonly string SubtitleColor = "#757575";
    private static readonly string BorderColor = "#E0E0E0";
    
    public static void Export(
        string filePath,
        string environment,
        string agent,
        int episodes,
        int stepsPerEpisode,
        IReadOnlyList<double> rewards,
        IReadOnlyList<int> episodeLengths,
        IReadOnlyList<bool> successes,
        string rewardChartSvg,
        string lengthChartSvg,
        string lossChartSvg,
        string date,
        Dictionary<string, double> rewardStats,
        Dictionary<string, double> lengthStats,
        double successRate,
        IReadOnlyList<EpisodeStats>? perEpisodeStats = null,
        IReadOnlyList<EpisodeTrajectory>? bestTrajectory = null,
        IReadOnlyList<EpisodeTrajectory>? worstTrajectory = null)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(TextColor));
                
                page.Header().Container().Column(column => 
                {
                    // Header with logo and title
                    column.Item().Row(row => 
                    {
                        row.RelativeItem().AlignLeft().Text("Gymnasium").FontSize(24).FontColor(PrimaryColor).Bold();
                        row.ConstantItem(180).AlignRight().Text(date).FontSize(10).FontColor(SubtitleColor);
                    });
                    
                    column.Item().BorderBottom(1).BorderColor(BorderColor).PaddingBottom(5)
                        .Text("Training Report").FontSize(16).FontColor(TextColor).SemiBold();
                });
                
                page.Content().Container().Column(col =>
                {
                    // Training Configuration Section
                    col.Item().PaddingVertical(10).Container().Background(HeaderBgColor)
                        .Padding(10)
                        .Column(configCol => 
                        {
                            configCol.Item().Text("Configuration").FontSize(14).SemiBold().FontColor(PrimaryColor);
                            
                            configCol.Item().Grid(grid => 
                            {
                                grid.Columns(2);
                                grid.Item().Text("Environment:").SemiBold();
                                grid.Item().Text(environment);
                                grid.Item().Text("Agent:").SemiBold();
                                grid.Item().Text(agent);
                                grid.Item().Text("Episodes:").SemiBold();
                                grid.Item().Text(episodes.ToString());
                                grid.Item().Text("Steps per Episode:").SemiBold();
                                grid.Item().Text(stepsPerEpisode.ToString());
                            });
                        });
                    
                    // Summary Statistics Section
                    col.Item().PaddingTop(15).Container().Background(HeaderBgColor)
                        .Padding(10)
                        .Column(statsCol =>
                        {
                            statsCol.Item().Text("Summary Statistics (last 100)").FontSize(14).SemiBold().FontColor(PrimaryColor);
                            
                            // Rewards statistics
                            statsCol.Item().PaddingTop(5).Text("Reward Metrics").FontSize(12).SemiBold().FontColor(TextColor);
                            statsCol.Item().Table(table => 
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                });
                                
                                foreach (var kv in rewardStats)
                                {
                                    table.Cell().Element(CellStyle).Text(FormatStatName(kv.Key)).SemiBold();
                                    table.Cell().Element(CellStyle).Text(FormatValue(kv.Value, "F2"));
                                }
                            });
                            
                            // Length statistics
                            statsCol.Item().PaddingTop(5).Text("Episode Length Metrics").FontSize(12).SemiBold().FontColor(TextColor);
                            statsCol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                });
                                
                                foreach (var kv in lengthStats)
                                {
                                    table.Cell().Element(CellStyle).Text(FormatStatName(kv.Key)).SemiBold();
                                    table.Cell().Element(CellStyle).Text(FormatValue(kv.Value, "F1"));
                                }
                            });
                            
                            // Success rate
                            statsCol.Item().PaddingTop(5).Grid(grid => 
                            {
                                grid.Columns(2);
                                grid.Item().Text("Success Rate:").SemiBold();
                                grid.Item().Text(successRate.ToString("P1"));
                            });
                        });
                    
                    // Charts Section
                    col.Item().PaddingTop(15).Container()
                        .Column(chartsCol =>
                        {
                            // Reward Chart
                            chartsCol.Item().Container().Border(1).BorderColor(BorderColor).Padding(5)
                                .Column(columnItem =>
                                {
                                    columnItem.Item().Text("Reward Curve").FontSize(14).SemiBold().FontColor(PrimaryColor);
                                    columnItem.Item().Image(SvgToPng(rewardChartSvg)).FitWidth();
                                });
                            
                            // Episode Length Chart
                            chartsCol.Item().PaddingTop(10).Container().Border(1).BorderColor(BorderColor).Padding(5)
                                .Column(columnItem =>
                                {
                                    columnItem.Item().Text("Episode Length Curve").FontSize(14).SemiBold().FontColor(PrimaryColor);
                                    columnItem.Item().Image(SvgToPng(lengthChartSvg)).FitWidth();
                                });
                            
                            // Loss Chart (if available)
                            if (!string.IsNullOrWhiteSpace(lossChartSvg))
                            {
                                chartsCol.Item().PaddingTop(10).Container().Border(1).BorderColor(BorderColor).Padding(5)
                                    .Column(columnItem =>
                                    {
                                        columnItem.Item().Text("Loss Curve").FontSize(14).SemiBold().FontColor(PrimaryColor);
                                        columnItem.Item().Image(SvgToPng(lossChartSvg)).FitWidth();
                                    });
                            }
                        });
                    
                    // Per-Episode Table (if available)
                    if (perEpisodeStats != null && perEpisodeStats.Count > 0)
                    {
                        col.Item().PaddingTop(15).Container().Border(1).BorderColor(BorderColor).Padding(5)
                            .Column(tableCol =>
                            {
                                tableCol.Item().Text("Per-Episode Statistics").FontSize(14).SemiBold().FontColor(PrimaryColor);
                                tableCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(60); // Ep
                                        columns.ConstantColumn(80); // Reward
                                        columns.ConstantColumn(80); // Length
                                        columns.ConstantColumn(80); // Loss
                                    });
                                    
                                    // Header row
                                    table.Cell().Element(HeaderCellStyle).Text("Episode").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Reward").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Length").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Loss").SemiBold();
                                    
                                    // Only show a reasonable number of rows (first 10, last 10)
                                    var firstRows = perEpisodeStats.Take(10).ToList();
                                    var lastRows = perEpisodeStats.Count > 20 
                                        ? perEpisodeStats.Skip(perEpisodeStats.Count - 10).Take(10).ToList() 
                                        : new List<EpisodeStats>();
                                    
                                    foreach (var ep in firstRows)
                                    {
                                        table.Cell().Element(CellStyle).Text(ep.Episode.ToString());
                                        table.Cell().Element(CellStyle).Text(ep.Reward.ToString("F2"));
                                        table.Cell().Element(CellStyle).Text(ep.Length.ToString());
                                        table.Cell().Element(CellStyle).Text(ep.Loss?.ToString("F4") ?? "");
                                    }
                                    
                                    if (perEpisodeStats.Count > 20)
                                    {
                                        // Add separator row
                                        table.Cell().Element(CellStyle).Text("...");
                                        table.Cell().Element(CellStyle).Text("...");
                                        table.Cell().Element(CellStyle).Text("...");
                                        table.Cell().Element(CellStyle).Text("...");
                                    }
                                    
                                    foreach (var ep in lastRows)
                                    {
                                        table.Cell().Element(CellStyle).Text(ep.Episode.ToString());
                                        table.Cell().Element(CellStyle).Text(ep.Reward.ToString("F2"));
                                        table.Cell().Element(CellStyle).Text(ep.Length.ToString());
                                        table.Cell().Element(CellStyle).Text(ep.Loss?.ToString("F4") ?? "");
                                    }
                                });
                            });
                    }
                    
                    // Best Episode Trajectory (if available)
                    if (bestTrajectory != null && bestTrajectory.Count > 0)
                    {
                        col.Item().PaddingTop(15).PageBreak();
                        col.Item().Container().Border(1).BorderColor(BorderColor).Padding(5)
                            .Column(tableCol =>
                            {
                                tableCol.Item().Text("Best Episode Trajectory").FontSize(14).SemiBold().FontColor(SecondaryColor);
                                tableCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(60); // Step
                                        columns.RelativeColumn(); // State
                                        columns.ConstantColumn(80); // Action
                                        columns.ConstantColumn(80); // Reward
                                    });
                                    
                                    // Header row
                                    table.Cell().Element(HeaderCellStyle).Text("Step").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("State").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Action").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Reward").SemiBold();
                                    
                                    foreach (var t in bestTrajectory)
                                    {
                                        table.Cell().Element(CellStyle).Text(t.Step.ToString());
                                        table.Cell().Element(CellStyle).Text(t.State?.ToString() ?? "");
                                        table.Cell().Element(CellStyle).Text(t.Action?.ToString() ?? "");
                                        table.Cell().Element(CellStyle).Text(t.Reward.ToString("F2"));
                                    }
                                });
                            });
                    }
                    
                    // Worst Episode Trajectory (if available)
                    if (worstTrajectory != null && worstTrajectory.Count > 0)
                    {
                        col.Item().PaddingTop(15).Container().Border(1).BorderColor(BorderColor).Padding(5)
                            .Column(tableCol =>
                            {
                                tableCol.Item().Text("Worst Episode Trajectory").FontSize(14).SemiBold().FontColor(AccentColor);
                                tableCol.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(60); // Step
                                        columns.RelativeColumn(); // State
                                        columns.ConstantColumn(80); // Action
                                        columns.ConstantColumn(80); // Reward
                                    });
                                    
                                    // Header row
                                    table.Cell().Element(HeaderCellStyle).Text("Step").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("State").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Action").SemiBold();
                                    table.Cell().Element(HeaderCellStyle).Text("Reward").SemiBold();
                                    
                                    foreach (var t in worstTrajectory)
                                    {
                                        table.Cell().Element(CellStyle).Text(t.Step.ToString());
                                        table.Cell().Element(CellStyle).Text(t.State?.ToString() ?? "");
                                        table.Cell().Element(CellStyle).Text(t.Action?.ToString() ?? "");
                                        table.Cell().Element(CellStyle).Text(t.Reward.ToString("F2"));
                                    }
                                });
                            });
                    }
                });
                
                page.Footer().AlignCenter().Column(column => 
                {
                    column.Item().BorderTop(1).BorderColor(BorderColor).PaddingTop(5);
                    column.Item().Text(text =>
                    {
                        text.Span("Generated by Gymnasium.NET").FontSize(9).FontColor(SubtitleColor);
                        text.Span(" | ").FontSize(9).FontColor(SubtitleColor);
                        text.Span(date).FontSize(9).FontColor(SubtitleColor);
                    });
                });
            });
        }).GeneratePdf(filePath);
    }

    private static IContainer HeaderCellStyle(IContainer container) => 
        container.DefaultTextStyle(x => x.SemiBold())
            .PaddingVertical(5).PaddingHorizontal(5)
            .Border(1).BorderColor(BorderColor)
            .Background(HeaderBgColor);

    private static IContainer CellStyle(IContainer container) => 
        container.PaddingVertical(3).PaddingHorizontal(5)
            .Border(1).BorderColor(BorderColor);

    private static string FormatStatName(string key)
    {
        switch (key.ToLower())
        {
            case "mean": return "Mean";
            case "min": return "Minimum";
            case "max": return "Maximum";
            case "std": return "Std. Dev.";
            case "median": return "Median";
            case "p25": return "25th Perc.";
            case "p75": return "75th Perc.";
            default: return key;
        }
    }

    private static string FormatValue(double value, string format)
    {
        return value.ToString(format);
    }

    // Converts SVG to PNG byte[] for embedding in PDF
    private static byte[] SvgToPng(string svg)
    {
        // For simplicity, use a placeholder image if SVG conversion is not implemented
        // In production, use SkiaSharp.Svg or similar to render SVG to PNG
        return Placeholders.Image(600, 300);
    }
}
