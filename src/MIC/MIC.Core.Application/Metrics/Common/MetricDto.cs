using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Metrics.Common;

/// <summary>
/// Data transfer object for metric information.
/// </summary>
public record MetricDto
{
    public Guid Id { get; init; }
    public string MetricName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public MetricSeverity Severity { get; init; }
    public double? TargetValue { get; init; }
    public double? PreviousValue { get; init; }
    public double ChangePercent { get; init; }

    // Display helpers
    public string SeverityDisplay => Severity.ToString();
    
    public string SeverityColor => Severity switch
    {
        MetricSeverity.Normal => "#39FF14",
        MetricSeverity.Warning => "#FF6B00",
        MetricSeverity.Critical => "#FF0055",
        _ => "#607D8B"
    };

    public string TrendIcon => ChangePercent switch
    {
        > 0 => "?",
        < 0 => "?",
        _ => "?"
    };

    public string TrendColor => ChangePercent switch
    {
        > 5 => "#39FF14",
        < -5 => "#FF0055",
        _ => "#FF6B00"
    };

    public string FormattedValue => Unit switch
    {
        "%" => $"{Value:F1}%",
        "$" => $"${Value:N0}",
        "K" => $"{Value:N0}K",
        "M" => $"{Value:N1}M",
        _ => $"{Value:N2} {Unit}"
    };

    public string FormattedChange => ChangePercent >= 0 
        ? $"+{ChangePercent:F1}%" 
        : $"{ChangePercent:F1}%";
}

/// <summary>
/// Represents a single data point in a time series.
/// </summary>
public record MetricDataPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public double? TargetValue { get; init; }
    public double? PredictedValue { get; init; }
    public double? ConfidenceLow { get; init; }
    public double? ConfidenceHigh { get; init; }
}

/// <summary>
/// Represents a metric trend over time.
/// </summary>
public record MetricTrendDto
{
    public string MetricName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public List<MetricDataPoint> DataPoints { get; init; } = new();
    public double CurrentValue { get; init; }
    public double? TargetValue { get; init; }
    public double AverageValue { get; init; }
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public double TrendSlope { get; init; }
    public string TrendDirection => TrendSlope switch
    {
        > 0.01 => "Upward",
        < -0.01 => "Downward",
        _ => "Stable"
    };
}

/// <summary>
/// Summary of metrics grouped by category.
/// </summary>
public record MetricCategorySummary
{
    public string Category { get; init; } = string.Empty;
    public int TotalMetrics { get; init; }
    public int OnTarget { get; init; }
    public int Warning { get; init; }
    public int Critical { get; init; }
    public double OverallHealth => TotalMetrics > 0 
        ? (double)OnTarget / TotalMetrics * 100 
        : 0;
}

/// <summary>
/// Extension methods for mapping between domain entities and DTOs.
/// </summary>
public static class MetricMappingExtensions
{
    public static MetricDto ToDto(this OperationalMetric metric, double? previousValue = null, double? targetValue = null)
    {
        ArgumentNullException.ThrowIfNull(metric);

        var changePercent = previousValue.HasValue && previousValue.Value != 0
            ? (metric.Value - previousValue.Value) / previousValue.Value * 100
            : 0;

        return new MetricDto
        {
            Id = metric.Id,
            MetricName = metric.MetricName,
            Category = metric.Category,
            Source = metric.Source,
            Value = metric.Value,
            Unit = metric.Unit,
            Timestamp = metric.Timestamp,
            Severity = metric.Severity,
            TargetValue = targetValue,
            PreviousValue = previousValue,
            ChangePercent = changePercent
        };
    }

    public static IEnumerable<MetricDto> ToDtos(this IEnumerable<OperationalMetric> metrics)
    {
        return metrics.Select(m => m.ToDto());
    }
}
