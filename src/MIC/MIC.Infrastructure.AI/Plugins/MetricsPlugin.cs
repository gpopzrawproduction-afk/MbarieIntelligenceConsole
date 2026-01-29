using System.ComponentModel;
using MediatR;
using Microsoft.SemanticKernel;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Application.Metrics.Queries.GetMetricTrend;

namespace MIC.Infrastructure.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin for accessing business metrics.
/// Enables the AI to query and analyze metric data.
/// </summary>
public class MetricsPlugin
{
    private readonly IMediator _mediator;

    public MetricsPlugin(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets current business metrics, optionally filtered by category.
    /// </summary>
    [KernelFunction]
    [Description("Get current business metrics. Returns latest values for all metrics or filtered by category.")]
    public async Task<string> GetMetricsAsync(
        [Description("Category filter: Financial, Operations, Performance, Customer, or leave empty for all")] 
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetMetricsQuery
            {
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                LatestOnly = true
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return $"Unable to retrieve metrics: {result.FirstError.Description}";
            }

            if (!result.Value.Any())
            {
                return "No metrics data available.";
            }

            var metricsText = result.Value
                .Select(m => $"- {m.MetricName}: {m.FormattedValue} ({m.Category}) - {m.TrendIcon} {m.FormattedChange}")
                .ToList();

            return $"Current Metrics:\n{string.Join("\n", metricsText)}";
        }
        catch (Exception ex)
        {
            return $"Error retrieving metrics: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets trend data for a specific metric over time.
    /// </summary>
    [KernelFunction]
    [Description("Get trend data for a specific metric over a time period. Shows historical values and trend direction.")]
    public async Task<string> GetMetricTrendAsync(
        [Description("Name of the metric to analyze (e.g., Revenue, Efficiency, Customer Satisfaction)")] 
        string metricName,
        [Description("Number of days to analyze (default: 30, max: 90)")] 
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metricName))
            {
                return "Please specify a metric name.";
            }

            days = Math.Clamp(days, 1, 90);

            var query = new GetMetricTrendQuery
            {
                MetricName = metricName,
                Days = days
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return $"Unable to retrieve trend for '{metricName}': {result.FirstError.Description}";
            }

            var trend = result.Value;
            var trendText = $"""
                Trend Analysis for {trend.MetricName} ({days} days):
                - Current Value: {trend.CurrentValue:N2} {trend.Unit}
                - Average: {trend.AverageValue:N2}
                - Min: {trend.MinValue:N2}
                - Max: {trend.MaxValue:N2}
                - Trend Direction: {trend.TrendDirection}
                - Data Points: {trend.DataPoints.Count}
                """;

            return trendText;
        }
        catch (Exception ex)
        {
            return $"Error analyzing metric trend: {ex.Message}";
        }
    }

    /// <summary>
    /// Compares two metrics to identify correlations.
    /// </summary>
    [KernelFunction]
    [Description("Compare two metrics to identify potential correlations or relationships.")]
    public async Task<string> CompareMetricsAsync(
        [Description("First metric name")] string metric1,
        [Description("Second metric name")] string metric2,
        [Description("Number of days to analyze")] int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query1 = new GetMetricTrendQuery { MetricName = metric1, Days = days };
            var query2 = new GetMetricTrendQuery { MetricName = metric2, Days = days };

            var result1 = await _mediator.Send(query1, cancellationToken);
            var result2 = await _mediator.Send(query2, cancellationToken);

            if (result1.IsError || result2.IsError)
            {
                return "Unable to retrieve one or both metrics for comparison.";
            }

            var trend1 = result1.Value;
            var trend2 = result2.Value;

            return $"""
                Comparison: {metric1} vs {metric2} ({days} days)
                
                {metric1}:
                - Current: {trend1.CurrentValue:N2}
                - Trend: {trend1.TrendDirection}
                
                {metric2}:
                - Current: {trend2.CurrentValue:N2}
                - Trend: {trend2.TrendDirection}
                
                Note: Correlation analysis requires more data points for accurate results.
                """;
        }
        catch (Exception ex)
        {
            return $"Error comparing metrics: {ex.Message}";
        }
    }
}
