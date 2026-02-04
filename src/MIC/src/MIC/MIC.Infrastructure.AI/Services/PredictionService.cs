using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Basic prediction service that uses historical metric trends to produce a simple linear forecast.
/// This is intentionally lightweight and suitable as a default implementation until a more advanced
/// AI-driven model is plugged in.
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

        // Fetch historical data (use up to 2x horizon or minimum 60 days)
        var lookbackDays = Math.Max(60, days * 2);
        var startDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var historical = await _metricsRepository.GetFilteredMetricsAsync(
            metricName: metricName,
            startDate: startDate,
            endDate: DateTime.UtcNow,
            latestOnly: false,
            cancellationToken: cancellationToken);

        if (historical == null || historical.Count < 7)
        {
            // Fallback to empty forecast when insufficient data
            return Array.Empty<ForecastPoint>();
        }

        // Order by date
        var series = historical.OrderBy(m => m.Timestamp).ToList();

        // Apply simple seasonal + smoothing model (additive Holt-Winters approximation)
        // Seasonality: weekly (7 days) if data appears daily; otherwise fallback to linear trend
        var seasonLength = 7;
        if (series.Count < seasonLength * 2)
        {
            // Not enough data for seasonality, use linear projection
            return LinearProjection(series, days);
        }

        // Prepare values
        var values = series.Select(s => s.Value).ToArray();

        // Initial estimates
        double alpha = 0.3; // level
        double beta = 0.05; // trend
        double gamma = 0.2; // seasonal

        var n = values.Length;
        var seasons = seasonLength;

        // Initialize seasonals by averaging
        var seasonAverages = new double[n / seasons];
        for (int j = 0; j < seasonAverages.Length; j++)
        {
            var sum = 0.0;
            for (int i = 0; i < seasons; i++)
            {
                sum += values[j * seasons + i];
            }
            seasonAverages[j] = sum / seasons;
        }

        var seasonals = new double[seasons];
        for (int i = 0; i < seasons; i++)
        {
            double sum = 0;
            int count = seasonAverages.Length;
            for (int j = 0; j < count; j++)
            {
                sum += values[j * seasons + i] - seasonAverages[j];
            }
            seasonals[i] = sum / count;
        }

        // Initialize level and trend
        double level = values.Take(seasons).Average();
        double trend = 0.0;
        for (int i = 0; i < seasons; i++)
        {
            trend += (values[i + seasons] - values[i]) / seasons;
        }
        trend /= seasons;

        // Holt-Winters additive
        for (int i = 0; i < n; i++)
        {
            var val = values[i];
            var seasonal = seasonals[i % seasons];
            var lastLevel = level;
            level = alpha * (val - seasonal) + (1 - alpha) * (level + trend);
            trend = beta * (level - lastLevel) + (1 - beta) * trend;
            seasonals[i % seasons] = gamma * (val - level) + (1 - gamma) * seasonal;
        }

        var results = new List<ForecastPoint>();
        var lastDate = series.Last().Timestamp;

        // Generate forecasts
        for (int m = 1; m <= days; m++)
        {
            var seasonal = seasonals[(n + m - 1) % seasons];
            var projected = (level + m * trend) + seasonal;
            // Provide conservative bounds
            var margin = Math.Abs(projected) * 0.12; // 12% uncertainty
            results.Add(new ForecastPoint
            {
                Date = lastDate.AddDays(m),
                Value = projected,
                LowerBound = projected - margin,
                UpperBound = projected + margin
            });
        }

        return results;
    }

    private static IReadOnlyList<ForecastPoint> LinearProjection(IReadOnlyList<Core.Domain.Entities.OperationalMetric> series, int days)
    {
        var list = series.OrderBy(s => s.Timestamp).ToList();
        var n = list.Count;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var y = list.Select(s => s.Value).ToArray();

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

        var lastDate = list.Last().Timestamp;
        var results = new List<ForecastPoint>();
        for (int i = 1; i <= days; i++)
        {
            var idx = n - 1 + i;
            var projected = intercept + slope * idx;
            var margin = Math.Abs(projected) * 0.15;
            results.Add(new ForecastPoint
            {
                Date = lastDate.AddDays(i),
                Value = projected,
                LowerBound = projected - margin,
                UpperBound = projected + margin
            });
        }

        return results;
    }
}
