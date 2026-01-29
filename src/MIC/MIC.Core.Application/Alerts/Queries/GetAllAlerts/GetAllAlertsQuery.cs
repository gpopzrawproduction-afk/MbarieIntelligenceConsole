using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Queries.GetAllAlerts;

/// <summary>
/// Query to retrieve all alerts with optional filtering.
/// </summary>
public record GetAllAlertsQuery : IQuery<IReadOnlyList<AlertDto>>
{
    /// <summary>
    /// Filter by severity level.
    /// </summary>
    public AlertSeverity? Severity { get; init; }

    /// <summary>
    /// Filter by status.
    /// </summary>
    public AlertStatus? Status { get; init; }

    /// <summary>
    /// Filter by start date (TriggeredAt >= StartDate).
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter by end date (TriggeredAt <= EndDate).
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Search text to filter by alert name or description.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Maximum number of results to return (default: 100).
    /// </summary>
    public int? Take { get; init; } = 100;

    /// <summary>
    /// Number of results to skip for pagination.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// If true, include soft-deleted alerts.
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;
}
