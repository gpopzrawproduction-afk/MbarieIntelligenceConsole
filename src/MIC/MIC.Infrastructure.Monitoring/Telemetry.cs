namespace MIC.Infrastructure.Monitoring;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Centralized telemetry primitives for tracing and metrics across MIC components.
/// </summary>
internal static class Telemetry
{
    public const string ServiceName = "MIC";

    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static readonly Meter Meter = new(ServiceName);
}
