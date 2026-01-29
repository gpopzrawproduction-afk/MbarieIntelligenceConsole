using ErrorOr;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Alerts.Queries.GetAllAlerts;

/// <summary>
/// Handler for retrieving all alerts with filtering.
/// </summary>
public class GetAllAlertsQueryHandler : IQueryHandler<GetAllAlertsQuery, IReadOnlyList<AlertDto>>
{
    private readonly IAlertRepository _alertRepository;

    public GetAllAlertsQueryHandler(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<AlertDto>>> Handle(
        GetAllAlertsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var alerts = await _alertRepository.GetFilteredAlertsAsync(
                severity: request.Severity,
                status: request.Status,
                startDate: request.StartDate,
                endDate: request.EndDate,
                searchText: request.SearchText,
                take: request.Take,
                skip: request.Skip,
                includeDeleted: request.IncludeDeleted,
                cancellationToken: cancellationToken);

            var dtos = alerts.ToDtos().ToList();
            return dtos;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "GetAllAlerts.Failed",
                description: $"Failed to retrieve alerts: {ex.Message}");
        }
    }
}
