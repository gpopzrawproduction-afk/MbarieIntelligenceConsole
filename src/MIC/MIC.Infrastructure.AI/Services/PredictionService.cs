using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Basic prediction service that uses historical metric trends to produce a simple linear forecast.
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly IMetricsRepository _metricsRepository;

    public PredictionService(IMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
    }

    public async Task<IReadOnlyList<ForecastPoint>> GenerateForecastAsync(string metricName, int days, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metricName)) throw new ArgumentException("metricName is required", nameof(metricName));
        if (days <= 0) return Array.Empty<ForecastPoint>();

        // Fetch last N days of metric history (use double the horizon to get a trend)
        var lookbackDays = Math.Max(30, days * 2);
        var startDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var historical = await _metricsRepository.GetFilteredMetricsAsync(
            metricName: metricName,
            startDate: startDate,
            endDate: DateTime.UtcNow,
            latestOnly: false,
            cancellationToken: cancellationToken);

        if (historical == null || historical.Count == 0)
        {
            return Array.Empty<ForecastPoint>();
        }

        // Convert into time series ordered by date
        var series = historical
            .OrderBy(m => m.Timestamp)
            .Select(m => new { Date = m.Timestamp, Value = m.Value })
            .ToList();

        // Simple linear trend using least squares on time index
        var n = series.Count;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var y = series.Select(s => s.Value).ToArray();

        var xMean = x.Average();
        var yMean = y.Average();
        var numerator = 0.0;
        var denominator = 0.0;
        for (int i = 0; i < n; i++)
        {
            numerator += (x[i] - xMean) * (y[i] - yMean);
            denominator += (x[i] - xMean) * (x[i] - xMean);
        }

        var slope = denominator == 0 ? 0.0 : numerator / denominator;
        var intercept = yMean - slope * xMean;

        var lastIndex = n - 1;
        var lastDate = series.Last().Date;

        var results = new List<ForecastPoint>();
        var confidence = 0.85; // static confidence for now

        for (int i = 1; i <= days; i++)
        {
            var idx = lastIndex + i;
            var projected = intercept + slope * idx;
            var lower = projected * (1 - (1 - confidence));
            var upper = projected * (1 + (1 - confidence));

            results.Add(new ForecastPoint
            {
                Date = lastDate.AddDays(i),
                Value = projected,
                LowerBound = lower,
                UpperBound = upper
            });
        }

        return results;
    }
}
