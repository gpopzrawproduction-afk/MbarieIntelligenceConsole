using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Common;

/// <summary>
/// Data transfer object for alert information.
/// </summary>
public record AlertDto
{
    public Guid Id { get; init; }
    public string AlertName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public AlertStatus Status { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolvedBy { get; init; }
    public string? Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }

    // Display helpers
    public string SeverityDisplay => Severity.ToString();
    public string StatusDisplay => Status.ToString();
    
    public string SeverityColor => Severity switch
    {
        AlertSeverity.Info => "#00E5FF",
        AlertSeverity.Warning => "#FF6B00",
        AlertSeverity.Critical => "#FF0055",
        AlertSeverity.Emergency => "#FF0055",
        _ => "#607D8B"
    };

    public string StatusColor => Status switch
    {
        AlertStatus.Active => "#FF0055",
        AlertStatus.Acknowledged => "#FF6B00",
        AlertStatus.Resolved => "#39FF14",
        AlertStatus.Escalated => "#BF40FF",
        _ => "#607D8B"
    };
}

/// <summary>
/// Extension methods for mapping between domain entities and DTOs.
/// </summary>
public static class AlertMappingExtensions
{
    public static AlertDto ToDto(this IntelligenceAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        string? resolution = null;
        if (alert.Context.TryGetValue("Resolution", out var resolutionObj))
        {
            resolution = resolutionObj?.ToString();
        }

        return new AlertDto
        {
            Id = alert.Id,
            AlertName = alert.AlertName,
            Description = alert.Description,
            Severity = alert.Severity,
            Status = alert.Status,
            Source = alert.Source,
            TriggeredAt = alert.TriggeredAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = alert.AcknowledgedBy,
            ResolvedAt = alert.ResolvedAt,
            ResolvedBy = alert.ResolvedBy,
            Resolution = resolution,
            CreatedAt = alert.CreatedAt,
            ModifiedAt = alert.ModifiedAt
        };
    }

    public static IEnumerable<AlertDto> ToDtos(this IEnumerable<IntelligenceAlert> alerts)
    {
        return alerts.Select(a => a.ToDto());
    }
}
