using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Commands.CreateAlert;

/// <summary>
/// Command to create a new intelligence alert
/// </summary>
public record CreateAlertCommand(
    string AlertName,
    string Description,
    AlertSeverity Severity,
    string Source
) : ICommand<Guid>;
