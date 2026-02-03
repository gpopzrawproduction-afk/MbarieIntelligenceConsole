using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Core.Application.Metrics.Queries.GetMetrics;

/// <summary>
/// Query to retrieve metrics with optional filtering.
/// </summary>
public record GetMetricsQuery : IQuery<IReadOnlyList<MetricDto>>
{
    /// <summary>
    /// Filter by category.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Filter by metric name.
    /// </summary>
    public string? MetricName { get; init; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Maximum number of results per metric.
    /// </summary>
    public int? Take { get; init; } = 100;

    /// <summary>
    /// If true, only return the latest value for each metric.
    /// </summary>
    public bool LatestOnly { get; init; } = false;
}
