using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Gymnasium.UI.Views;

public static class ChartExportHelper
{
    public static string RenderRewardChartSvg(IReadOnlyList<double> rewards, int movingAvgWindow = 20)
    {
        if (rewards.Count < 2) return "<svg width='400' height='120'></svg>";
        double width = 400, height = 120;
        double maxReward = Math.Max(1.0, Math.Max(rewards[0], rewards[^1]));
        var sb = new StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background:#eee'>");
        // Raw reward curve
        for (int i = 1; i < rewards.Count; i++)
        {
            double x1 = (i - 1) * width / (rewards.Count - 1);
            double y1 = height - (rewards[i - 1] / maxReward) * height * 0.9;
            double x2 = i * width / (rewards.Count - 1);
            double y2 = height - (rewards[i] / maxReward) * height * 0.9;
            sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='blue' stroke-width='2'/>");
        }
        // Moving average
        if (rewards.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < rewards.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += rewards[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxReward) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxReward) * height * 0.9;
                sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='orange' stroke-width='2' stroke-dasharray='4,2'/>");
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string RenderEpisodeLengthChartSvg(IReadOnlyList<int> lengths, int movingAvgWindow = 20)
    {
        if (lengths.Count < 2) return "<svg width='400' height='120'></svg>";
        double width = 400, height = 120;
        double maxLen = Math.Max(1.0, Math.Max(lengths[0], lengths[^1]));
        var sb = new StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background:#eee'>");
        // Raw length curve
        for (int i = 1; i < lengths.Count; i++)
        {
            double x1 = (i - 1) * width / (lengths.Count - 1);
            double y1 = height - (lengths[i - 1] / maxLen) * height * 0.9;
            double x2 = i * width / (lengths.Count - 1);
            double y2 = height - (lengths[i] / maxLen) * height * 0.9;
            sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='green' stroke-width='2'/>");
        }
        // Moving average
        if (lengths.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < lengths.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += lengths[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxLen) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxLen) * height * 0.9;
                sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='orange' stroke-width='2' stroke-dasharray='4,2'/>");
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string RenderLossChartSvg(IReadOnlyList<double> losses, int movingAvgWindow = 20)
    {
        if (losses.Count < 2) return "<svg width='400' height='120'></svg>";
        double width = 400, height = 120;
        double maxLoss = Math.Max(1.0, Math.Abs(losses[0]));
        for (int i = 1; i < losses.Count; i++)
            maxLoss = Math.Max(maxLoss, Math.Abs(losses[i]));
        var sb = new System.Text.StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background:#eee'>");
        // Raw loss curve
        for (int i = 1; i < losses.Count; i++)
        {
            double x1 = (i - 1) * width / (losses.Count - 1);
            double y1 = height - (losses[i - 1] / maxLoss) * height * 0.9;
            double x2 = i * width / (losses.Count - 1);
            double y2 = height - (losses[i] / maxLoss) * height * 0.9;
            sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='red' stroke-width='2'/>");
        }
        // Moving average
        if (losses.Count >= movingAvgWindow)
        {
            var avg = new List<double>();
            for (int i = 0; i < losses.Count; i++)
            {
                int start = Math.Max(0, i - movingAvgWindow + 1);
                double sum = 0;
                for (int j = start; j <= i; j++) sum += losses[j];
                avg.Add(sum / (i - start + 1));
            }
            for (int i = 1; i < avg.Count; i++)
            {
                double x1 = (i - 1) * width / (avg.Count - 1);
                double y1 = height - (avg[i - 1] / maxLoss) * height * 0.9;
                double x2 = i * width / (avg.Count - 1);
                double y2 = height - (avg[i] / maxLoss) * height * 0.9;
                sb.Append($"<line x1='{x1:F1}' y1='{y1:F1}' x2='{x2:F1}' y2='{y2:F1}' stroke='orange' stroke-width='2' stroke-dasharray='4,2'/>");
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }
}
