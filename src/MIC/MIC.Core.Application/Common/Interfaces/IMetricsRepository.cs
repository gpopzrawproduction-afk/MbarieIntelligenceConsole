using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for operational metrics.
/// </summary>
public interface IMetricsRepository : IRepository<OperationalMetric>
{
    /// <summary>
    /// Gets metrics with optional filtering.
    /// </summary>
    Task<IReadOnlyList<OperationalMetric>> GetFilteredMetricsAsync(
        string? category = null,
        string? metricName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? take = null,
        bool latestOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest value for each unique metric.
    /// </summary>
    Task<IReadOnlyList<OperationalMetric>> GetLatestMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metric categories with counts.
    /// </summary>
    Task<Dictionary<string, int>> GetMetricCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metrics by category.
    /// </summary>
    Task<IReadOnlyList<OperationalMetric>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);
}
