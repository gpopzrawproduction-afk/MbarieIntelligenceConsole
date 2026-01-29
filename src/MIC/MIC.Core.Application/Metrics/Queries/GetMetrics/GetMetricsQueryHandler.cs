using ErrorOr;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Core.Application.Metrics.Queries.GetMetrics;

/// <summary>
/// Handler for retrieving metrics with filtering.
/// </summary>
public class GetMetricsQueryHandler : IQueryHandler<GetMetricsQuery, IReadOnlyList<MetricDto>>
{
    private readonly IMetricsRepository _metricsRepository;

    public GetMetricsQueryHandler(IMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<MetricDto>>> Handle(
        GetMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var metrics = await _metricsRepository.GetFilteredMetricsAsync(
            category: request.Category,
            metricName: request.MetricName,
            startDate: request.StartDate,
            endDate: request.EndDate,
            take: request.Take,
            latestOnly: request.LatestOnly,
            cancellationToken: cancellationToken);

        var dtos = metrics.ToDtos().ToList();
        return dtos;
    }
}
