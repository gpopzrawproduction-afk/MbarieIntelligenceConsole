using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Alerts.Commands.DeleteAlert;

/// <summary>
/// Command to soft delete an alert.
/// </summary>
public record DeleteAlertCommand(
    Guid AlertId,
    string DeletedBy
) : ICommand<bool>;
