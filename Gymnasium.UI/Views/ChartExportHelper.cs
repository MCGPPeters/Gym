using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Gymnasium.UI.Views;

public static class ChartExportHelper
{
    public static string RenderRewardChartSvg(IReadOnlyList<double> rewards, int movingAvgWindow = 20)
    {
        if (rewards.Count < 2) return "<svg width='600' height='300' style='background-color:#f8f9fa'><text x='50%' y='50%' text-anchor='middle' fill='#9e9e9e' font-family='Arial' font-size='14'>No data available</text></svg>";
        
        double width = 600, height = 300;
        double padding = 40; // Padding for axes
        double chartWidth = width - (padding * 2);
        double chartHeight = height - (padding * 2);
        
        // Find min and max for better scaling
        double maxReward = double.MinValue;
        double minReward = double.MaxValue;
        foreach (var reward in rewards)
        {
            maxReward = Math.Max(maxReward, reward);
            minReward = Math.Min(minReward, reward);
        }
        
        // Add some padding to the range
        double range = maxReward - minReward;
        maxReward += range * 0.1;
        minReward -= range * 0.1;
        if (Math.Abs(range) < 0.001) // Handle flat lines
        {
            maxReward = maxReward + 1;
            minReward = minReward - 1;
        }

        var sb = new StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background-color:#f8f9fa; font-family:Arial, sans-serif;'>");
        
        // Draw grid
        sb.Append("<g style='stroke:#e0e0e0;stroke-width:1'>");
        // Horizontal grid lines (5 lines)
        for (int i = 0; i <= 5; i++)
        {
            double y = padding + (i * chartHeight / 5);
            sb.Append($"<line x1='{padding}' y1='{y}' x2='{width - padding}' y2='{y}' />");
            double value = maxReward - (i * (maxReward - minReward) / 5);
            sb.Append($"<text x='{padding - 5}' y='{y + 4}' text-anchor='end' fill='#757575' font-size='10'>{value:F1}</text>");
        }
        
        // Vertical grid lines (10 lines)
        for (int i = 0; i <= 10; i++)
        {
            double x = padding + (i * chartWidth / 10);
            sb.Append($"<line x1='{x}' y1='{padding}' x2='{x}' y2='{height - padding}' />");
            
            // Only show labels for every other line to avoid clutter
            if (i % 2 == 0)
            {
                int episode = (int)(i * rewards.Count / 10);
                if (episode < rewards.Count)
                    sb.Append($"<text x='{x}' y='{height - padding + 15}' text-anchor='middle' fill='#757575' font-size='10'>{episode}</text>");
            }
        }
        sb.Append("</g>");
        
        // Axes labels
        sb.Append($"<text x='{width/2}' y='{height - 10}' text-anchor='middle' fill='#616161' font-size='12'>Episode</text>");
        sb.Append($"<text x='{15}' y='{height/2}' text-anchor='middle' fill='#616161' font-size='12' transform='rotate(-90, 15, {height/2})'>Reward</text>");
        
        // Raw reward curve - using a smooth path with better styling
        sb.Append("<path d='");
        for (int i = 0; i < rewards.Count; i++)
        {
            double x = padding + (i * chartWidth / (rewards.Count - 1));
            double y = padding + chartHeight - ((rewards[i] - minReward) / (maxReward - minReward) * chartHeight);
            sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
        }
        sb.Append($"' fill='none' stroke='#1976D2' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' />");
        
        // Draw points for key data points
        for (int i = 0; i < rewards.Count; i += Math.Max(1, rewards.Count / 20)) // Show about 20 points max
        {
            double x = padding + (i * chartWidth / (rewards.Count - 1));
            double y = padding + chartHeight - ((rewards[i] - minReward) / (maxReward - minReward) * chartHeight);
            sb.Append($"<circle cx='{x:F1}' cy='{y:F1}' r='3' fill='#1976D2' />");
        }
        
        // Moving average with improved styling
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
            
            sb.Append("<path d='");
            for (int i = 0; i < avg.Count; i++)
            {
                double x = padding + (i * chartWidth / (rewards.Count - 1));
                double y = padding + chartHeight - ((avg[i] - minReward) / (maxReward - minReward) * chartHeight);
                sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
            }
            sb.Append($"' fill='none' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' stroke-linejoin='round' />");
        }
        
        // Chart title
        sb.Append($"<text x='{width/2}' y='20' text-anchor='middle' fill='#424242' font-size='14' font-weight='bold'>Reward History</text>");
        
        // Legend
        double legendX = width - padding - 100;
        double legendY = padding + 20;
        sb.Append($"<rect x='{legendX - 10}' y='{legendY - 15}' width='110' height='50' rx='5' ry='5' fill='white' stroke='#e0e0e0' stroke-width='1' />");
        sb.Append($"<line x1='{legendX}' y1='{legendY}' x2='{legendX + 20}' y2='{legendY}' stroke='#1976D2' stroke-width='2.5' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 4}' fill='#616161' font-size='12'>Reward</text>");
        sb.Append($"<line x1='{legendX}' y1='{legendY + 20}' x2='{legendX + 20}' y2='{legendY + 20}' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 24}' fill='#616161' font-size='12'>Moving Avg</text>");
        
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string RenderEpisodeLengthChartSvg(IReadOnlyList<int> lengths, int movingAvgWindow = 20)
    {
        if (lengths.Count < 2) return "<svg width='600' height='300' style='background-color:#f8f9fa'><text x='50%' y='50%' text-anchor='middle' fill='#9e9e9e' font-family='Arial' font-size='14'>No data available</text></svg>";
        
        double width = 600, height = 300;
        double padding = 40; // Padding for axes
        double chartWidth = width - (padding * 2);
        double chartHeight = height - (padding * 2);
        
        // Find min and max for better scaling
        double maxLen = double.MinValue;
        double minLen = double.MaxValue;
        foreach (var len in lengths)
        {
            maxLen = Math.Max(maxLen, len);
            minLen = Math.Min(minLen, len);
        }
        
        // Add some padding to the range
        double range = maxLen - minLen;
        maxLen += range * 0.1;
        minLen = Math.Max(0, minLen - range * 0.05); // Don't go below 0 for episode lengths
        if (Math.Abs(range) < 0.001) // Handle flat lines
        {
            maxLen = maxLen + 1;
            minLen = Math.Max(0, minLen - 1);
        }

        var sb = new StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background-color:#f8f9fa; font-family:Arial, sans-serif;'>");
        
        // Draw grid
        sb.Append("<g style='stroke:#e0e0e0;stroke-width:1'>");
        // Horizontal grid lines (5 lines)
        for (int i = 0; i <= 5; i++)
        {
            double y = padding + (i * chartHeight / 5);
            sb.Append($"<line x1='{padding}' y1='{y}' x2='{width - padding}' y2='{y}' />");
            double value = maxLen - (i * (maxLen - minLen) / 5);
            sb.Append($"<text x='{padding - 5}' y='{y + 4}' text-anchor='end' fill='#757575' font-size='10'>{value:F0}</text>");
        }
        
        // Vertical grid lines (10 lines)
        for (int i = 0; i <= 10; i++)
        {
            double x = padding + (i * chartWidth / 10);
            sb.Append($"<line x1='{x}' y1='{padding}' x2='{x}' y2='{height - padding}' />");
            
            // Only show labels for every other line to avoid clutter
            if (i % 2 == 0)
            {
                int episode = (int)(i * lengths.Count / 10);
                if (episode < lengths.Count)
                    sb.Append($"<text x='{x}' y='{height - padding + 15}' text-anchor='middle' fill='#757575' font-size='10'>{episode}</text>");
            }
        }
        sb.Append("</g>");
        
        // Axes labels
        sb.Append($"<text x='{width/2}' y='{height - 10}' text-anchor='middle' fill='#616161' font-size='12'>Episode</text>");
        sb.Append($"<text x='{15}' y='{height/2}' text-anchor='middle' fill='#616161' font-size='12' transform='rotate(-90, 15, {height/2})'>Steps</text>");
        
        // Episode length curve - using a smooth path with better styling
        sb.Append("<path d='");
        for (int i = 0; i < lengths.Count; i++)
        {
            double x = padding + (i * chartWidth / (lengths.Count - 1));
            double y = padding + chartHeight - ((lengths[i] - minLen) / (maxLen - minLen) * chartHeight);
            sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
        }
        sb.Append($"' fill='none' stroke='#4CAF50' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' />");
        
        // Draw points for key data points
        for (int i = 0; i < lengths.Count; i += Math.Max(1, lengths.Count / 20)) // Show about 20 points max
        {
            double x = padding + (i * chartWidth / (lengths.Count - 1));
            double y = padding + chartHeight - ((lengths[i] - minLen) / (maxLen - minLen) * chartHeight);
            sb.Append($"<circle cx='{x:F1}' cy='{y:F1}' r='3' fill='#4CAF50' />");
        }
        
        // Moving average with improved styling
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
            
            sb.Append("<path d='");
            for (int i = 0; i < avg.Count; i++)
            {
                double x = padding + (i * chartWidth / (lengths.Count - 1));
                double y = padding + chartHeight - ((avg[i] - minLen) / (maxLen - minLen) * chartHeight);
                sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
            }
            sb.Append($"' fill='none' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' stroke-linejoin='round' />");
        }
        
        // Chart title
        sb.Append($"<text x='{width/2}' y='20' text-anchor='middle' fill='#424242' font-size='14' font-weight='bold'>Episode Length History</text>");
        
        // Legend
        double legendX = width - padding - 100;
        double legendY = padding + 20;
        sb.Append($"<rect x='{legendX - 10}' y='{legendY - 15}' width='110' height='50' rx='5' ry='5' fill='white' stroke='#e0e0e0' stroke-width='1' />");
        sb.Append($"<line x1='{legendX}' y1='{legendY}' x2='{legendX + 20}' y2='{legendY}' stroke='#4CAF50' stroke-width='2.5' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 4}' fill='#616161' font-size='12'>Length</text>");
        sb.Append($"<line x1='{legendX}' y1='{legendY + 20}' x2='{legendX + 20}' y2='{legendY + 20}' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 24}' fill='#616161' font-size='12'>Moving Avg</text>");
        
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string RenderLossChartSvg(IReadOnlyList<double> losses, int movingAvgWindow = 20)
    {
        if (losses.Count < 2) return "<svg width='600' height='300' style='background-color:#f8f9fa'><text x='50%' y='50%' text-anchor='middle' fill='#9e9e9e' font-family='Arial' font-size='14'>No data available</text></svg>";
        
        double width = 600, height = 300;
        double padding = 40; // Padding for axes
        double chartWidth = width - (padding * 2);
        double chartHeight = height - (padding * 2);
        
        // Find min and max for better scaling
        double maxLoss = double.MinValue;
        double minLoss = double.MaxValue;
        foreach (var loss in losses)
        {
            maxLoss = Math.Max(maxLoss, loss);
            minLoss = Math.Min(minLoss, loss);
        }
        
        // Add some padding to the range
        double range = maxLoss - minLoss;
        maxLoss += range * 0.1;
        minLoss = Math.Max(0, minLoss - range * 0.05); // Loss is typically non-negative
        if (Math.Abs(range) < 0.001) // Handle flat lines
        {
            maxLoss = maxLoss + 1;
            minLoss = Math.Max(0, minLoss - 1);
        }

        var sb = new StringBuilder();
        sb.Append($"<svg width='{width}' height='{height}' style='background-color:#f8f9fa; font-family:Arial, sans-serif;'>");
        
        // Draw grid
        sb.Append("<g style='stroke:#e0e0e0;stroke-width:1'>");
        // Horizontal grid lines (5 lines)
        for (int i = 0; i <= 5; i++)
        {
            double y = padding + (i * chartHeight / 5);
            sb.Append($"<line x1='{padding}' y1='{y}' x2='{width - padding}' y2='{y}' />");
            double value = maxLoss - (i * (maxLoss - minLoss) / 5);
            sb.Append($"<text x='{padding - 5}' y='{y + 4}' text-anchor='end' fill='#757575' font-size='10'>{value:F3}</text>");
        }
        
        // Vertical grid lines (10 lines)
        for (int i = 0; i <= 10; i++)
        {
            double x = padding + (i * chartWidth / 10);
            sb.Append($"<line x1='{x}' y1='{padding}' x2='{x}' y2='{height - padding}' />");
            
            // Only show labels for every other line to avoid clutter
            if (i % 2 == 0)
            {
                int episode = (int)(i * losses.Count / 10);
                if (episode < losses.Count)
                    sb.Append($"<text x='{x}' y='{height - padding + 15}' text-anchor='middle' fill='#757575' font-size='10'>{episode}</text>");
            }
        }
        sb.Append("</g>");
        
        // Axes labels
        sb.Append($"<text x='{width/2}' y='{height - 10}' text-anchor='middle' fill='#616161' font-size='12'>Episode</text>");
        sb.Append($"<text x='{15}' y='{height/2}' text-anchor='middle' fill='#616161' font-size='12' transform='rotate(-90, 15, {height/2})'>Loss</text>");
        
        // Loss curve - using a smooth path with better styling
        sb.Append("<path d='");
        for (int i = 0; i < losses.Count; i++)
        {
            double x = padding + (i * chartWidth / (losses.Count - 1));
            double y = padding + chartHeight - ((losses[i] - minLoss) / (maxLoss - minLoss) * chartHeight);
            sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
        }
        sb.Append($"' fill='none' stroke='#E53935' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round' />");
        
        // Draw points for key data points
        for (int i = 0; i < losses.Count; i += Math.Max(1, losses.Count / 20)) // Show about 20 points max
        {
            double x = padding + (i * chartWidth / (losses.Count - 1));
            double y = padding + chartHeight - ((losses[i] - minLoss) / (maxLoss - minLoss) * chartHeight);
            sb.Append($"<circle cx='{x:F1}' cy='{y:F1}' r='3' fill='#E53935' />");
        }
        
        // Moving average with improved styling
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
            
            sb.Append("<path d='");
            for (int i = 0; i < avg.Count; i++)
            {
                double x = padding + (i * chartWidth / (losses.Count - 1));
                double y = padding + chartHeight - ((avg[i] - minLoss) / (maxLoss - minLoss) * chartHeight);
                sb.Append(i == 0 ? $"M {x:F1} {y:F1} " : $"L {x:F1} {y:F1} ");
            }
            sb.Append($"' fill='none' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' stroke-linejoin='round' />");
        }
        
        // Chart title
        sb.Append($"<text x='{width/2}' y='20' text-anchor='middle' fill='#424242' font-size='14' font-weight='bold'>Training Loss History</text>");
        
        // Legend
        double legendX = width - padding - 100;
        double legendY = padding + 20;
        sb.Append($"<rect x='{legendX - 10}' y='{legendY - 15}' width='110' height='50' rx='5' ry='5' fill='white' stroke='#e0e0e0' stroke-width='1' />");
        sb.Append($"<line x1='{legendX}' y1='{legendY}' x2='{legendX + 20}' y2='{legendY}' stroke='#E53935' stroke-width='2.5' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 4}' fill='#616161' font-size='12'>Loss</text>");
        sb.Append($"<line x1='{legendX}' y1='{legendY + 20}' x2='{legendX + 20}' y2='{legendY + 20}' stroke='#FF9800' stroke-width='2.5' stroke-dasharray='6,3' stroke-linecap='round' />");
        sb.Append($"<text x='{legendX + 25}' y='{legendY + 24}' fill='#616161' font-size='12'>Moving Avg</text>");
        
        sb.Append("</svg>");
        return sb.ToString();
    }
}
