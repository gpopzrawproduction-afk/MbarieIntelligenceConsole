using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for operational metrics.
/// </summary>
public class MetricsRepository : Repository<OperationalMetric>, IMetricsRepository
{
    public MetricsRepository(MicDbContext context) : base(context) { }

    public async Task<IReadOnlyList<OperationalMetric>> GetFilteredMetricsAsync(
        string? category = null,
        string? metricName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? take = null,
        bool latestOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Filter by category
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(m => m.Category == category);
        }

        // Filter by metric name
        if (!string.IsNullOrWhiteSpace(metricName))
        {
            query = query.Where(m => m.MetricName == metricName);
        }

        // Filter by date range
        if (startDate.HasValue)
        {
            query = query.Where(m => m.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Timestamp <= endDate.Value);
        }

        if (latestOnly)
        {
            // Get only the latest value for each unique metric
            query = query
                .GroupBy(m => new { m.MetricName, m.Category })
                .Select(g => g.OrderByDescending(m => m.Timestamp).First());
        }
        else
        {
            // Order by timestamp
            query = query.OrderByDescending(m => m.Timestamp);
        }

        // Apply take limit
        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalMetric>> GetLatestMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .GroupBy(m => new { m.MetricName, m.Category })
            .Select(g => g.OrderByDescending(m => m.Timestamp).First())
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetMetricCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .GroupBy(m => m.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalMetric>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.Category == category)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
