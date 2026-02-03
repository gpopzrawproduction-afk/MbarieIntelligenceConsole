using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Commands.UpdateAlert;

/// <summary>
/// Command to update an existing alert.
/// </summary>
public record UpdateAlertCommand : ICommand<AlertDto>
{
    /// <summary>
    /// The ID of the alert to update.
    /// </summary>
    public Guid AlertId { get; init; }

    /// <summary>
    /// The new status for the alert.
    /// </summary>
    public AlertStatus? NewStatus { get; init; }

    /// <summary>
    /// The user performing the update.
    /// </summary>
    public string UpdatedBy { get; init; } = string.Empty;

    /// <summary>
    /// Resolution notes (required when resolving an alert).
    /// </summary>
    public string? ResolutionNotes { get; init; }

    /// <summary>
    /// Additional notes to add to the alert context.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Updated alert name (optional).
    /// </summary>
    public string? AlertName { get; init; }

    /// <summary>
    /// Updated alert description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Updated alert source (optional).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Updated alert severity (optional).
    /// </summary>
    public AlertSeverity? Severity { get; init; }
}