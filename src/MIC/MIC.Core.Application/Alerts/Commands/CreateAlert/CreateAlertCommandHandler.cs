using ErrorOr;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Alerts.Commands.CreateAlert;

/// <summary>
/// Handler for creating new intelligence alerts
/// </summary>
public class CreateAlertCommandHandler : ICommandHandler<CreateAlertCommand, Guid>
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAlertCommandHandler(IAlertRepository alertRepository, IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var alert = new IntelligenceAlert(
                request.AlertName,
                request.Description,
                request.Severity,
                request.Source);

            await _alertRepository.AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return alert.Id;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "CreateAlert.Failed",
                description: $"Failed to create alert: {ex.Message}");
        }
    }
}
