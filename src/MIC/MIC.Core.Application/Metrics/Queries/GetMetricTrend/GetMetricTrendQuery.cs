using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Core.Application.Metrics.Queries.GetMetricTrend;

/// <summary>
/// Query to retrieve metric trend data over time.
/// </summary>
public record GetMetricTrendQuery : IQuery<MetricTrendDto>
{
    /// <summary>
    /// The name of the metric to retrieve.
    /// </summary>
    public string MetricName { get; init; } = string.Empty;

    /// <summary>
    /// The category of the metric.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Number of days of historical data.
    /// </summary>
    public int Days { get; init; } = 30;

    /// <summary>
    /// Include predictions if available.
    /// </summary>
    public bool IncludePredictions { get; init; } = true;
}
