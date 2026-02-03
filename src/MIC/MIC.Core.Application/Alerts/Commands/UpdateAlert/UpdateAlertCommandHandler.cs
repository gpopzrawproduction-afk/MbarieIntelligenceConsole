using ErrorOr;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Commands.UpdateAlert;

/// <summary>
/// Handler for updating alerts.
/// </summary>
public class UpdateAlertCommandHandler : ICommandHandler<UpdateAlertCommand, AlertDto>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAlertCommandHandler(IAlertRepository alertRepository, IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<AlertDto>> Handle(
        UpdateAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);

        if (alert is null)
        {
            return Error.NotFound(
                code: "Alert.NotFound",
                description: $"Alert with ID '{request.AlertId}' was not found.");
        }

        if (alert.IsDeleted)
        {
            return Error.Conflict(
                code: "Alert.Deleted",
                description: "Cannot update a deleted alert.");
        }

        // Handle metadata updates if any are provided
        if (!string.IsNullOrWhiteSpace(request.AlertName) || 
            !string.IsNullOrWhiteSpace(request.Description) || 
            !string.IsNullOrWhiteSpace(request.Source) || 
            request.Severity.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
            {
                return Error.Validation(
                    code: "Alert.UpdateRequiresUser",
                    description: "A user must be specified when updating alert metadata.");
            }
            
            var alertName = string.IsNullOrWhiteSpace(request.AlertName) ? alert.AlertName : request.AlertName;
            var description = string.IsNullOrWhiteSpace(request.Description) ? alert.Description : request.Description;
            var source = string.IsNullOrWhiteSpace(request.Source) ? alert.Source : request.Source;
            var severity = request.Severity ?? alert.Severity;
            
            alert.UpdateMetadata(alertName, description, severity, source, request.UpdatedBy);
        }

        // Handle status transitions
        if (request.NewStatus.HasValue && request.NewStatus.Value != alert.Status)
        {
            var statusResult = await UpdateAlertStatusAsync(alert, request);
            if (statusResult.IsError)
            {
                return statusResult.Errors;
            }
        }

        // Add notes to context if provided
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var existingNotes = alert.Context.ContainsKey("Notes")
                ? alert.Context["Notes"]?.ToString() ?? string.Empty
                : string.Empty;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var newNote = $"[{timestamp}] {request.UpdatedBy}: {request.Notes}";
            var combinedNotes = string.IsNullOrEmpty(existingNotes)
                ? newNote
                : $"{existingNotes}\n{newNote}";

            alert.AddContextData("Notes", combinedNotes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return alert.ToDto();
    }

    private Task<ErrorOr<bool>> UpdateAlertStatusAsync(
        IntelligenceAlert alert,
        UpdateAlertCommand request)
    {
        try
        {
            switch (request.NewStatus!.Value)
            {
                case AlertStatus.Acknowledged:
                    if (string.IsNullOrWhiteSpace(request.UpdatedBy))
                    {
                        return Task.FromResult<ErrorOr<bool>>(Error.Validation(
                            code: "Alert.AcknowledgeRequiresUser",
                            description: "A user must be specified when acknowledging an alert."));
                    }
                    alert.Acknowledge(request.UpdatedBy);
                    break;

                case AlertStatus.Resolved:
                    if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
                    {
                        return Task.FromResult<ErrorOr<bool>>(Error.Validation(
                            code: "Alert.ResolutionRequired",
                            description: "Resolution notes are required when resolving an alert."));
                    }
                    if (string.IsNullOrWhiteSpace(request.UpdatedBy))
                    {
                        return Task.FromResult<ErrorOr<bool>>(Error.Validation(
                            code: "Alert.ResolveRequiresUser",
                            description: "A user must be specified when resolving an alert."));
                    }
                    alert.Resolve(request.UpdatedBy, request.ResolutionNotes);
                    break;

                case AlertStatus.Active:
                    // Reactivating an alert - only allowed from Acknowledged status
                    if (alert.Status != AlertStatus.Acknowledged)
                    {
                        return Task.FromResult<ErrorOr<bool>>(Error.Validation(
                            code: "Alert.InvalidStatusTransition",
                            description: $"Cannot transition from {alert.Status} to Active."));
                    }
                    // For reactivation, we would need to add a method to the entity
                    break;

                case AlertStatus.Escalated:
                    // Handle escalation if needed
                    alert.AddContextData("EscalatedAt", DateTime.UtcNow);
                    alert.AddContextData("EscalatedBy", request.UpdatedBy);
                    break;

                default:
                    return Task.FromResult<ErrorOr<bool>>(Error.Validation(
                        code: "Alert.InvalidStatus",
                        description: $"Invalid status: {request.NewStatus}"));
            }

            return Task.FromResult<ErrorOr<bool>>(true);
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult<ErrorOr<bool>>(Error.Conflict(
                code: "Alert.StatusTransitionFailed",
                description: ex.Message));
        }
    }
}