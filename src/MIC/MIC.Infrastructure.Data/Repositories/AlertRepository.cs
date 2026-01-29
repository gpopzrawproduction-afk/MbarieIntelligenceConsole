using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for intelligence alerts
/// </summary>
public class AlertRepository : Repository<IntelligenceAlert>, IAlertRepository
{
    public AlertRepository(MicDbContext context) : base(context) { }

    public async Task<IEnumerable<IntelligenceAlert>> GetActiveAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Status == AlertStatus.Active)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<IntelligenceAlert>> GetAlertsBySeverityAsync(
        AlertSeverity severity,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Severity == severity)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntelligenceAlert>> GetFilteredAlertsAsync(
        AlertSeverity? severity = null,
        AlertStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null,
        int? take = null,
        int? skip = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Filter by soft delete (if entity supports it)
        if (!includeDeleted)
        {
            query = query.Where(a => !a.IsDeleted);
        }

        // Filter by severity
        if (severity.HasValue)
        {
            query = query.Where(a => a.Severity == severity.Value);
        }

        // Filter by status
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        // Filter by date range
        if (startDate.HasValue)
        {
            query = query.Where(a => a.TriggeredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.TriggeredAt <= endDate.Value);
        }

        // Search by name or description
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchLower = searchText.ToLower();
            query = query.Where(a =>
                a.AlertName.ToLower().Contains(searchLower) ||
                a.Description.ToLower().Contains(searchLower) ||
                a.Source.ToLower().Contains(searchLower));
        }

        // Order by triggered date descending (most recent first)
        query = query.OrderByDescending(a => a.TriggeredAt);

        // Pagination
        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<AlertSeverity, int>> GetAlertCountsBySeverityAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => !a.IsDeleted && a.Status != AlertStatus.Resolved)
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count, cancellationToken);
    }
}
