using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Prediction service contract for generating short-term forecasts for metrics.
/// </summary>
public interface IPredictionService
{
    /// <summary>
    /// Generates forecast data points for the specified metric over the given horizon (days).
    /// </summary>
    Task<IReadOnlyList<ForecastPoint>> GenerateForecastAsync(string metricName, int days, CancellationToken cancellationToken = default);
}

public sealed class ForecastPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public double? LowerBound { get; set; }
    public double? UpperBound { get; set; }
}
