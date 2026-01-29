namespace MIC.Core.Domain.Entities;

/// <summary>
/// Minimal OperationalMetric placeholder to allow infrastructure compilation.
/// Replace with full implementation as already present in project context.
/// </summary>
public class OperationalMetric : MIC.Core.Domain.Abstractions.BaseEntity
{
    public string MetricName { get; private set; }
    public string Category { get; private set; }
    public string Source { get; private set; }
    public double Value { get; private set; }
    public string Unit { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; private set; } = new();
    public MetricSeverity Severity { get; private set; } = MetricSeverity.Normal;

    private OperationalMetric() 
    { 
        // EF Core constructor - initialize required strings
        MetricName = string.Empty;
        Category = string.Empty;
        Source = string.Empty;
        Unit = string.Empty;
    }

    public OperationalMetric(string metricName, string category, string source, double value, string unit, MetricSeverity severity)
    {
        MetricName = metricName;
        Category = category;
        Source = source;
        Value = value;
        Unit = unit;
        Severity = severity;
    }
}

public enum MetricSeverity
{
    Normal,
    Warning,
    Critical
}
