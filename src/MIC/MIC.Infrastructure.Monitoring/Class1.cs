namespace MIC.Infrastructure.Monitoring;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Provides lightweight tracing and metrics primitives for MIC without external dependencies.
/// </summary>
public sealed class MonitoringService
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _requestDurationMs;

    /// <summary>
    /// Creates a new monitoring service using the shared telemetry primitives.
    /// </summary>
    public MonitoringService() : this(Telemetry.ActivitySource, Telemetry.Meter) { }

    /// <summary>
    /// Creates a new monitoring service with custom telemetry primitives.
    /// </summary>
    public MonitoringService(ActivitySource activitySource, Meter meter)
    {
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));

        _requestCounter = _meter.CreateCounter<long>("mic.requests", description: "Number of logical requests processed");
        _errorCounter = _meter.CreateCounter<long>("mic.errors", description: "Number of errors recorded");
        _requestDurationMs = _meter.CreateHistogram<double>("mic.request.duration.ms", unit: "ms", description: "Duration of logical requests in milliseconds");
    }

    /// <summary>
    /// Starts an activity for the given operation name. Dispose the returned activity to stop it.
    /// </summary>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Activity name is required", nameof(name));

        var activity = _activitySource.StartActivity(name, kind);
        if (activity is null) return null;

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    /// <summary>
    /// Records a logical request occurrence and optional duration.
    /// </summary>
    public void RecordRequest(string? route = null, string? method = null, int? statusCode = null, double? durationMs = null, ReadOnlySpan<KeyValuePair<string, object?>> extraTags = default)
    {
        TagList tagList = default;
        if (!string.IsNullOrWhiteSpace(route)) tagList.Add("route", route);
        if (!string.IsNullOrWhiteSpace(method)) tagList.Add("method", method);
        if (statusCode is not null) tagList.Add("status_code", statusCode);
        AddExtraTags(ref tagList, extraTags);

        _requestCounter.Add(1, tagList);
        if (durationMs is double d)
        {
            _requestDurationMs.Record(d, tagList);
        }

        Activity.Current?.AddEvent(new ActivityEvent("request.recorded"));
    }

    /// <summary>
    /// Records an error occurrence and attaches it to the current activity if present.
    /// </summary>
    public void RecordError(Exception exception, string? operation = null, ReadOnlySpan<KeyValuePair<string, object?>> extraTags = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TagList tagList = default;
        if (!string.IsNullOrWhiteSpace(operation)) tagList.Add("operation", operation);
        tagList.Add("exception.type", exception.GetType().FullName);
        tagList.Add("exception.message", exception.Message);
        AddExtraTags(ref tagList, extraTags);

        _errorCounter.Add(1, tagList);

        var activity = Activity.Current;
        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection(new[]
            {
                new KeyValuePair<string, object?>("exception.type", exception.GetType().FullName),
                new KeyValuePair<string, object?>("exception.message", exception.Message),
                new KeyValuePair<string, object?>("exception.stacktrace", exception.StackTrace)
            })));
        }
    }

    /// <summary>
    /// Creates a disposable timer that, when disposed, will record request count and duration.
    /// </summary>
    public RequestTimer Measure(string? route = null, string? method = null)
        => new(this, route, method);

    private static void AddExtraTags(ref TagList tagList, ReadOnlySpan<KeyValuePair<string, object?>> extraTags)
    {
        if (!extraTags.IsEmpty)
        {
            foreach (var kv in extraTags)
            {
                tagList.Add(kv);
            }
        }
    }

    /// <summary>
    /// A disposable timing scope to measure duration of logical requests.
    /// </summary>
    public readonly struct RequestTimer : IDisposable
    {
        private readonly MonitoringService _owner;
        private readonly string? _route;
        private readonly string? _method;
        private readonly long _start;
        private readonly Activity? _activity;
        private readonly bool _hasActivity;

        internal RequestTimer(MonitoringService owner, string? route, string? method)
        {
            _owner = owner;
            _route = route;
            _method = method;
            _start = Stopwatch.GetTimestamp();
            _activity = owner.StartActivity("request", ActivityKind.Internal, new[]
            {
                new KeyValuePair<string, object?>("route", route ?? string.Empty),
                new KeyValuePair<string, object?>("method", method ?? string.Empty)
            });
            _hasActivity = _activity is not null;
        }

        /// <summary>
        /// Completes the timer, recording metrics and tagging the activity with the result.
        /// </summary>
        public void Complete(int statusCode)
        {
            var duration = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            _owner.RecordRequest(_route, _method, statusCode, duration);
            if (_hasActivity)
            {
                _activity!.SetTag("status_code", statusCode);
            }
        }

        public void Dispose()
        {
            _activity?.Dispose();
        }
    }
}

