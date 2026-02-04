using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace MIC.Infrastructure.Monitoring;

/// <summary>
/// Centralized telemetry primitives for tracing and metrics across MIC components.
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "MIC";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static readonly Meter Meter = new(ServiceName);

    private static TelemetryClient? _telemetryClient;

    /// <summary>
    /// Initializes Application Insights telemetry
    /// </summary>
    public static void InitializeApplicationInsights(string? instrumentationKey = null)
    {
        if (!string.IsNullOrEmpty(instrumentationKey))
        {
            var configuration = new TelemetryConfiguration
            {
                InstrumentationKey = instrumentationKey
            };

            _telemetryClient = new TelemetryClient(configuration);

            // Enable auto-collection of telemetry
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
        }
    }

    /// <summary>
    /// Tracks an event
    /// </summary>
    public static void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        _telemetryClient?.TrackEvent(eventName, properties, metrics);
    }

    /// <summary>
    /// Tracks an exception
    /// </summary>
    public static void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackException(exception, properties);
    }

    /// <summary>
    /// Tracks a metric
    /// </summary>
    public static void TrackMetric(string name, double value, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackMetric(name, value, properties);
    }

    /// <summary>
    /// Tracks a page view
    /// </summary>
    public static void TrackPageView(string pageName)
    {
        _telemetryClient?.TrackPageView(pageName);
    }

    /// <summary>
    /// Tracks a dependency call
    /// </summary>
    public static void TrackDependency(string dependencyType, string target, string dependencyName, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        _telemetryClient?.TrackDependency(dependencyType, target, dependencyName, dependencyName, startTime, duration, null, success);
    }

    /// <summary>
    /// Creates a new activity for tracing
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    /// <summary>
    /// Records a metric value
    /// </summary>
    public static void RecordMetric(string name, double value, KeyValuePair<string, object?>[]? tags = null)
    {
        var counter = Meter.CreateCounter<double>(name);
        counter.Add(value, tags ?? Array.Empty<KeyValuePair<string, object?>>());
    }

    /// <summary>
    /// Records a histogram value
    /// </summary>
    public static void RecordHistogram(string name, double value, KeyValuePair<string, object?>[]? tags = null)
    {
        var histogram = Meter.CreateHistogram<double>(name);
        histogram.Record(value, tags ?? Array.Empty<KeyValuePair<string, object?>>());
    }

    /// <summary>
    /// Flushes telemetry data
    /// </summary>
    public static void Flush()
    {
        _telemetryClient?.Flush();
    }
}
