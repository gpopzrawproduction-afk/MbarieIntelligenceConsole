using ErrorOr;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Core.Application.Metrics.Queries.GetMetricTrend;

/// <summary>
/// Handler for retrieving metric trend data.
/// </summary>
public class GetMetricTrendQueryHandler : IQueryHandler<GetMetricTrendQuery, MetricTrendDto>
{
    private readonly IMetricsRepository _metricsRepository;

    public GetMetricTrendQueryHandler(IMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<ErrorOr<MetricTrendDto>> Handle(
        GetMetricTrendQuery request,
        CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddDays(-request.Days);
        var endDate = DateTime.UtcNow;

        var metrics = await _metricsRepository.GetFilteredMetricsAsync(
            category: request.Category,
            metricName: request.MetricName,
            startDate: startDate,
            endDate: endDate,
            take: null,
            latestOnly: false,
            cancellationToken: cancellationToken);

        var metricList = metrics.ToList();

        if (metricList.Count == 0)
        {
            return Error.NotFound(
                code: "Metric.NotFound",
                description: $"No data found for metric '{request.MetricName}'.");
        }

        var firstMetric = metricList.First();
        var dataPoints = metricList
            .OrderBy(m => m.Timestamp)
            .Select(m => new MetricDataPoint
            {
                Timestamp = m.Timestamp,
                Value = m.Value,
                TargetValue = null // Would come from targets table
            })
            .ToList();

        // Calculate statistics
        var values = dataPoints.Select(p => p.Value).ToList();
        var currentValue = values.LastOrDefault();
        var avgValue = values.Average();
        var minValue = values.Min();
        var maxValue = values.Max();

        // Calculate trend slope using simple linear regression
        var trendSlope = CalculateTrendSlope(dataPoints);

        return new MetricTrendDto
        {
            MetricName = firstMetric.MetricName,
            Category = firstMetric.Category,
            Unit = firstMetric.Unit,
            DataPoints = dataPoints,
            CurrentValue = currentValue,
            TargetValue = null,
            AverageValue = avgValue,
            MinValue = minValue,
            MaxValue = maxValue,
            TrendSlope = trendSlope
        };
    }

    private static double CalculateTrendSlope(List<MetricDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return 0;

        var n = dataPoints.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (var i = 0; i < n; i++)
        {
            var x = i;
            var y = dataPoints[i].Value;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        
        // Normalize slope to percentage of average value
        var avgY = sumY / n;
        return avgY != 0 ? slope / avgY : 0;
    }
}
