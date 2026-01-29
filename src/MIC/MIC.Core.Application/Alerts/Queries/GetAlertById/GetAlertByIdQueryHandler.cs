using ErrorOr;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Alerts.Queries.GetAlertById;

/// <summary>
/// Handler for retrieving a single alert by ID.
/// </summary>
public class GetAlertByIdQueryHandler : IQueryHandler<GetAlertByIdQuery, AlertDto>
{
    private readonly IAlertRepository _alertRepository;

    public GetAlertByIdQueryHandler(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<ErrorOr<AlertDto>> Handle(
        GetAlertByIdQuery request,
        CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.AlertId, cancellationToken);

        if (alert is null)
        {
            return Error.NotFound(
                code: "Alert.NotFound",
                description: $"Alert with ID '{request.AlertId}' was not found.");
        }

        return alert.ToDto();
    }
}
