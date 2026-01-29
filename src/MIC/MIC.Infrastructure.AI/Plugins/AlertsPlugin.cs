using System.ComponentModel;
using MediatR;
using Microsoft.SemanticKernel;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin for accessing and analyzing alerts.
/// Enables the AI to query alert data and provide insights.
/// </summary>
public class AlertsPlugin
{
    private readonly IMediator _mediator;

    public AlertsPlugin(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets active alerts, optionally filtered by severity.
    /// </summary>
    [KernelFunction]
    [Description("Get active alerts from the system. Can filter by severity level.")]
    public async Task<string> GetActiveAlertsAsync(
        [Description("Severity filter: Critical, High, Medium, Low, Info, or leave empty for all")] 
        string? severity = null,
        [Description("Maximum number of alerts to return (default: 10)")] 
        int maxAlerts = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllAlertsQuery();

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return $"Unable to retrieve alerts: {result.FirstError.Description}";
            }

            var alerts = result.Value.AsEnumerable();

            // Filter by severity if specified
            if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<AlertSeverity>(severity, true, out var severityEnum))
            {
                alerts = alerts.Where(a => a.Severity == severityEnum);
            }

            // Filter active alerts only
            alerts = alerts.Where(a => a.Status != AlertStatus.Resolved);

            var alertList = alerts.Take(maxAlerts).ToList();

            if (!alertList.Any())
            {
                return severity != null 
                    ? $"No active {severity} alerts found." 
                    : "No active alerts found.";
            }

            var alertsText = alertList
                .Select(a => $"- [{a.Severity}] {a.AlertName}: {a.Description} (Status: {a.Status})")
                .ToList();

            var summary = $"""
                Active Alerts ({alertList.Count} found):
                {string.Join("\n", alertsText)}
                
                Summary:
                - Critical: {alertList.Count(a => a.Severity == AlertSeverity.Critical)}
                - Warning: {alertList.Count(a => a.Severity == AlertSeverity.Warning)}
                - Info: {alertList.Count(a => a.Severity == AlertSeverity.Info)}
                """;

            return summary;
        }
        catch (Exception ex)
        {
            return $"Error retrieving alerts: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets a summary of alert statistics.
    /// </summary>
    [KernelFunction]
    [Description("Get a summary of alert statistics including counts by severity and status.")]
    public async Task<string> GetAlertSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllAlertsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return $"Unable to retrieve alert summary: {result.FirstError.Description}";
            }

            var alerts = result.Value;
            var total = alerts.Count;
            var active = alerts.Count(a => a.Status != AlertStatus.Resolved);
            var resolved = alerts.Count(a => a.Status == AlertStatus.Resolved);

            var bySeverity = alerts
                .GroupBy(a => a.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            var summary = $"""
                Alert Summary:
                - Total Alerts: {total}
                - Active: {active}
                - Resolved: {resolved}
                
                By Severity:
                - Critical: {bySeverity.GetValueOrDefault(AlertSeverity.Critical, 0)}
                - Warning: {bySeverity.GetValueOrDefault(AlertSeverity.Warning, 0)}
                - Info: {bySeverity.GetValueOrDefault(AlertSeverity.Info, 0)}
                """;

            return summary;
        }
        catch (Exception ex)
        {
            return $"Error generating alert summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets recent alert activity.
    /// </summary>
    [KernelFunction]
    [Description("Get recent alert activity showing alerts created or updated in the specified time period.")]
    public async Task<string> GetRecentAlertActivityAsync(
        [Description("Number of hours to look back (default: 24)")] 
        int hours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllAlertsQuery
            {
                StartDate = DateTime.UtcNow.AddHours(-hours)
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsError)
            {
                return $"Unable to retrieve recent alerts: {result.FirstError.Description}";
            }

            var recentAlerts = result.Value.ToList();

            if (!recentAlerts.Any())
            {
                return $"No alert activity in the last {hours} hours.";
            }

            var alertsText = recentAlerts
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => $"- {a.CreatedAt:HH:mm} [{a.Severity}] {a.AlertName}")
                .ToList();

            return $"""
                Alert Activity (Last {hours} hours):
                {string.Join("\n", alertsText)}
                
                Total: {recentAlerts.Count} alerts
                """;
        }
        catch (Exception ex)
        {
            return $"Error retrieving recent alerts: {ex.Message}";
        }
    }
}
