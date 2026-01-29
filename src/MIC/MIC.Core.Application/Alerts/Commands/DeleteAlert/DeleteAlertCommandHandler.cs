using ErrorOr;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Alerts.Commands.DeleteAlert;

/// <summary>
/// Handler for soft deleting alerts.
/// </summary>
public class DeleteAlertCommandHandler : ICommandHandler<DeleteAlertCommand, bool>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAlertCommandHandler(IAlertRepository alertRepository, IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<bool>> Handle(
        DeleteAlertCommand request,
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
                code: "Alert.AlreadyDeleted",
                description: "Alert is already deleted.");
        }

        // Soft delete
        alert.MarkAsDeleted(request.DeletedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
