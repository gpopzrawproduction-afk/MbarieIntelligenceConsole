using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Generates sample metric data for testing and development.
/// </summary>
public static class MetricDataGenerator
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates 90 days of sample metrics for all business KPIs.
    /// </summary>
    public static IEnumerable<OperationalMetric> GenerateSampleMetrics()
    {
        var metrics = new List<OperationalMetric>();
        var startDate = DateTime.UtcNow.AddDays(-90);

        // Revenue metrics
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Revenue", "Financial", "ERP System",
            baseValue: 125000, unit: "$",
            seasonalAmplitude: 15000, trendSlope: 500,
            startDate: startDate, daysCount: 90));

        // Cost metrics  
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Operating Costs", "Financial", "ERP System",
            baseValue: 85000, unit: "$",
            seasonalAmplitude: 8000, trendSlope: 200,
            startDate: startDate, daysCount: 90));

        // Efficiency metrics
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Operational Efficiency", "Operations", "Process Monitor",
            baseValue: 78, unit: "%",
            seasonalAmplitude: 5, trendSlope: 0.15,
            startDate: startDate, daysCount: 90));

        // Customer Satisfaction
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Customer Satisfaction", "Customer", "Survey System",
            baseValue: 4.2, unit: "/5",
            seasonalAmplitude: 0.3, trendSlope: 0.01,
            startDate: startDate, daysCount: 90));

        // Uptime metrics
        metrics.AddRange(GenerateTimeSeriesMetric(
            "System Uptime", "Operations", "Infrastructure Monitor",
            baseValue: 99.5, unit: "%",
            seasonalAmplitude: 0.3, trendSlope: 0.005,
            startDate: startDate, daysCount: 90));

        // Response Time
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Avg Response Time", "Performance", "APM System",
            baseValue: 145, unit: "ms",
            seasonalAmplitude: 25, trendSlope: -1.5,
            startDate: startDate, daysCount: 90));

        // Transactions per second
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Transactions/sec", "Performance", "Transaction Monitor",
            baseValue: 1250, unit: "TPS",
            seasonalAmplitude: 200, trendSlope: 15,
            startDate: startDate, daysCount: 90));

        // Active Users
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Active Users", "Customer", "Analytics Platform",
            baseValue: 8500, unit: "",
            seasonalAmplitude: 1500, trendSlope: 50,
            startDate: startDate, daysCount: 90));

        // Error Rate
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Error Rate", "Performance", "APM System",
            baseValue: 0.8, unit: "%",
            seasonalAmplitude: 0.3, trendSlope: -0.01,
            startDate: startDate, daysCount: 90));

        // Profit Margin
        metrics.AddRange(GenerateTimeSeriesMetric(
            "Profit Margin", "Financial", "ERP System",
            baseValue: 32, unit: "%",
            seasonalAmplitude: 4, trendSlope: 0.08,
            startDate: startDate, daysCount: 90));

        return metrics;
    }

    /// <summary>
    /// Generates a time series of metric values with trends and seasonality.
    /// </summary>
    private static IEnumerable<OperationalMetric> GenerateTimeSeriesMetric(
        string metricName,
        string category,
        string source,
        double baseValue,
        string unit,
        double seasonalAmplitude,
        double trendSlope,
        DateTime startDate,
        int daysCount)
    {
        var metrics = new List<OperationalMetric>();

        for (int day = 0; day < daysCount; day++)
        {
            var timestamp = startDate.AddDays(day);
            
            // Calculate value with trend, seasonality, and noise
            var trendValue = baseValue + (trendSlope * day);
            var seasonal = seasonalAmplitude * Math.Sin(2 * Math.PI * day / 30); // 30-day cycle
            var weeklyPattern = seasonalAmplitude * 0.3 * Math.Sin(2 * Math.PI * day / 7); // Weekly cycle
            var noise = (Random.NextDouble() - 0.5) * seasonalAmplitude * 0.5;
            
            var value = trendValue + seasonal + weeklyPattern + noise;
            
            // Ensure value stays reasonable
            value = Math.Max(0, value);
            if (unit == "%")
            {
                value = Math.Min(100, value);
            }

            // Determine severity based on value relative to base
            var deviation = Math.Abs(value - baseValue) / baseValue;
            var severity = deviation switch
            {
                > 0.3 => MetricSeverity.Critical,
                > 0.15 => MetricSeverity.Warning,
                _ => MetricSeverity.Normal
            };

            metrics.Add(new OperationalMetric(
                metricName,
                category,
                source,
                Math.Round(value, 2),
                unit,
                severity)
            {
                // Note: Timestamp is set via reflection since it's private set
            });

            // Use reflection to set timestamp
            var prop = typeof(OperationalMetric).GetProperty("Timestamp");
            if (prop != null)
            {
                prop.SetValue(metrics.Last(), timestamp);
            }
        }

        return metrics;
    }

    /// <summary>
    /// Gets the target values for each metric.
    /// </summary>
    public static Dictionary<string, double> GetMetricTargets()
    {
        return new Dictionary<string, double>
        {
            ["Revenue"] = 150000,
            ["Operating Costs"] = 80000,
            ["Operational Efficiency"] = 85,
            ["Customer Satisfaction"] = 4.5,
            ["System Uptime"] = 99.9,
            ["Avg Response Time"] = 100,
            ["Transactions/sec"] = 1500,
            ["Active Users"] = 10000,
            ["Error Rate"] = 0.5,
            ["Profit Margin"] = 35
        };
    }
}
