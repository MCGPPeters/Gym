using Gymnasium.UI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gymnasium.UI.Views;

public static class ReportPdfExporter
{
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
                page.Margin(30);
                page.Header().Text("Gymnasium Training Report").FontSize(24).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Date: {date}").FontSize(12);
                    col.Item().PaddingTop(10).Text("Configuration").FontSize(16).Bold();
                    col.Item().Text($"Environment: {environment}\nAgent: {agent}\nEpisodes: {episodes}\nSteps per Episode: {stepsPerEpisode}").FontSize(12);
                    col.Item().PaddingTop(10).Text("Summary Statistics (last 100)").FontSize(16).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                        });
                        table.Cell().Element(CellStyle).Text("Metric").Bold();
                        table.Cell().Element(CellStyle).Text("Value").Bold();
                        foreach (var kv in rewardStats)
                        {
                            table.Cell().Element(CellStyle).Text($"Reward {kv.Key}");
                            table.Cell().Element(CellStyle).Text(kv.Value.ToString("F2"));
                        }
                        foreach (var kv in lengthStats)
                        {
                            table.Cell().Element(CellStyle).Text($"Length {kv.Key}");
                            table.Cell().Element(CellStyle).Text(kv.Value.ToString("F2"));
                        }
                        table.Cell().Element(CellStyle).Text("Success Rate");
                        table.Cell().Element(CellStyle).Text(successRate.ToString("P1"));
                    });
                    col.Item().PaddingTop(10).Text("Reward Curve").FontSize(16).Bold();
                    col.Item().Image(SvgToPng(rewardChartSvg)).FitWidth();
                    col.Item().PaddingTop(10).Text("Episode Length Curve").FontSize(16).Bold();
                    col.Item().Image(SvgToPng(lengthChartSvg)).FitWidth();
                    if (!string.IsNullOrWhiteSpace(lossChartSvg))
                    {
                        col.Item().PaddingTop(10).Text("Loss Curve").FontSize(16).Bold();
                        col.Item().Image(SvgToPng(lossChartSvg)).FitWidth();
                    }
                    if (perEpisodeStats != null && perEpisodeStats.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Per-Episode Table").FontSize(16).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60); // Ep
                                columns.ConstantColumn(80); // Reward
                                columns.ConstantColumn(60); // Length
                                columns.ConstantColumn(60); // Loss
                            });
                            table.Cell().Element(CellStyle).Text("Ep").Bold();
                            table.Cell().Element(CellStyle).Text("Reward").Bold();
                            table.Cell().Element(CellStyle).Text("Length").Bold();
                            table.Cell().Element(CellStyle).Text("Loss").Bold();
                            foreach (var ep in perEpisodeStats)
                            {
                                table.Cell().Element(CellStyle).Text(ep.Episode.ToString());
                                table.Cell().Element(CellStyle).Text(ep.Reward.ToString("F2"));
                                table.Cell().Element(CellStyle).Text(ep.Length.ToString());
                                table.Cell().Element(CellStyle).Text(ep.Loss?.ToString("F4") ?? "");
                            }
                        });
                    }
                    if (bestTrajectory != null && bestTrajectory.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Best Episode Trajectory").FontSize(16).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60); // Step
                                columns.ConstantColumn(120); // State
                                columns.ConstantColumn(60); // Action
                                columns.ConstantColumn(60); // Reward
                            });
                            table.Cell().Element(CellStyle).Text("Step").Bold();
                            table.Cell().Element(CellStyle).Text("State").Bold();
                            table.Cell().Element(CellStyle).Text("Action").Bold();
                            table.Cell().Element(CellStyle).Text("Reward").Bold();
                            foreach (var t in bestTrajectory)
                            {
                                table.Cell().Element(CellStyle).Text(t.Step.ToString());
                                table.Cell().Element(CellStyle).Text(t.State?.ToString() ?? "");
                                table.Cell().Element(CellStyle).Text(t.Action?.ToString() ?? "");
                                table.Cell().Element(CellStyle).Text(t.Reward.ToString("F2"));
                            }
                        });
                    }
                    if (worstTrajectory != null && worstTrajectory.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("Worst Episode Trajectory").FontSize(16).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60); // Step
                                columns.ConstantColumn(120); // State
                                columns.ConstantColumn(60); // Action
                                columns.ConstantColumn(60); // Reward
                            });
                            table.Cell().Element(CellStyle).Text("Step").Bold();
                            table.Cell().Element(CellStyle).Text("State").Bold();
                            table.Cell().Element(CellStyle).Text("Action").Bold();
                            table.Cell().Element(CellStyle).Text("Reward").Bold();
                            foreach (var t in worstTrajectory)
                            {
                                table.Cell().Element(CellStyle).Text(t.Step.ToString());
                                table.Cell().Element(CellStyle).Text(t.State?.ToString() ?? "");
                                table.Cell().Element(CellStyle).Text(t.Action?.ToString() ?? "");
                                table.Cell().Element(CellStyle).Text(t.Reward.ToString("F2"));
                            }
                        });
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated by Gymnasium - ").FontSize(10).Italic();
                    t.Span(date).FontSize(10);
                });
            });
        }).GeneratePdf(filePath);
    }

    private static IContainer CellStyle(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(4);

    // Converts SVG to PNG byte[] for embedding in PDF (using SkiaSharp or similar)
    private static byte[] SvgToPng(string svg)
    {
        // For simplicity, use a placeholder image if SVG conversion is not implemented
        // In production, use SkiaSharp.Svg or similar to render SVG to PNG
        return Placeholders.Image(400, 120);
    }
}
