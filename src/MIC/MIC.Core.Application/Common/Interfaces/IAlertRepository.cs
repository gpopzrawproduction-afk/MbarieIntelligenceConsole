using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for intelligence alerts
/// </summary>
public interface IAlertRepository : IRepository<IntelligenceAlert>
{
    Task<IEnumerable<IntelligenceAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<IntelligenceAlert>> GetAlertsBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts with optional filtering.
    /// </summary>
    Task<IReadOnlyList<IntelligenceAlert>> GetFilteredAlertsAsync(
        AlertSeverity? severity = null,
        AlertStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchText = null,
        int? take = null,
        int? skip = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active alerts by severity.
    /// </summary>
    Task<Dictionary<AlertSeverity, int>> GetAlertCountsBySeverityAsync(CancellationToken cancellationToken = default);
}
