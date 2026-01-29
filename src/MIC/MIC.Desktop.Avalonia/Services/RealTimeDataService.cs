using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Domain.Entities;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Real-time data service that provides automatic data refresh and event streaming.
/// Simulates SignalR-like functionality for desktop application.
/// </summary>
public class RealTimeDataService : IDisposable
{
    private static RealTimeDataService? _instance;
    public static RealTimeDataService Instance => _instance ??= new RealTimeDataService();

    private readonly Subject<DataUpdateEvent> _dataUpdates = new();
    private readonly Subject<AlertEvent> _alertEvents = new();
    private readonly Subject<MetricEvent> _metricEvents = new();
    
    private CancellationTokenSource? _refreshCts;
    private bool _isRunning;
    private int _refreshIntervalSeconds = 30;

    public IObservable<DataUpdateEvent> DataUpdates => _dataUpdates.AsObservable();
    public IObservable<AlertEvent> AlertEvents => _alertEvents.AsObservable();
    public IObservable<MetricEvent> MetricEvents => _metricEvents.AsObservable();

    public bool IsRunning => _isRunning;
    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            _refreshIntervalSeconds = Math.Max(5, Math.Min(300, value));
            if (_isRunning)
            {
                Stop();
                Start();
            }
        }
    }

    public event Action<string>? OnStatusChanged;
    public event Action<int>? OnAlertCountChanged;
    public event Action<double>? OnMetricValueChanged;

    /// <summary>
    /// Starts the real-time data refresh service.
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _refreshCts = new CancellationTokenSource();
        
        OnStatusChanged?.Invoke("Connected");
        NotificationService.Instance.ShowSuccess("Real-time updates enabled", "Connected");

        // Start background refresh loop
        _ = RefreshLoopAsync(_refreshCts.Token);
    }

    /// <summary>
    /// Stops the real-time data refresh service.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;

        OnStatusChanged?.Invoke("Disconnected");
    }

    /// <summary>
    /// Forces an immediate data refresh.
    /// </summary>
    public async Task RefreshNowAsync()
    {
        await FetchAndPublishDataAsync();
        NotificationService.Instance.ShowInfo("Data refreshed", "Sync Complete");
    }

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await FetchAndPublishDataAsync();
                await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorHandlingService.Instance.HandleException(ex, "Real-time data refresh");
                await Task.Delay(TimeSpan.FromSeconds(5), ct); // Retry after 5 seconds
            }
        }
    }

    private async Task FetchAndPublishDataAsync()
    {
        var serviceProvider = Program.ServiceProvider;
        if (serviceProvider == null) return;

        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetService<IMediator>();
        if (mediator == null) return;

        try
        {
            // Fetch alerts
            var alertsResult = await mediator.Send(new GetAllAlertsQuery { Take = 100 });
            if (!alertsResult.IsError)
            {
                var alerts = alertsResult.Value.ToList();
                var activeCount = alerts.Count(a => a.Status != AlertStatus.Resolved);
                
                _alertEvents.OnNext(new AlertEvent
                {
                    TotalCount = alerts.Count,
                    ActiveCount = activeCount,
                    CriticalCount = alerts.Count(a => a.Severity == AlertSeverity.Critical),
                    Timestamp = DateTime.Now
                });

                OnAlertCountChanged?.Invoke(activeCount);
            }

            // Fetch metrics
            var metricsResult = await mediator.Send(new GetMetricsQuery { Take = 50 });
            if (!metricsResult.IsError)
            {
                var metrics = metricsResult.Value.ToList();
                
                _metricEvents.OnNext(new MetricEvent
                {
                    TotalCount = metrics.Count,
                    Timestamp = DateTime.Now
                });

                // Publish individual metric updates
                foreach (var metric in metrics.Take(10))
                {
                    OnMetricValueChanged?.Invoke(metric.Value);
                }
            }

            // Publish general update event
            _dataUpdates.OnNext(new DataUpdateEvent
            {
                Source = "Database",
                Timestamp = DateTime.Now,
                Message = "Data synchronized"
            });
        }
        catch (Exception ex)
        {
            _dataUpdates.OnNext(new DataUpdateEvent
            {
                Source = "Error",
                Timestamp = DateTime.Now,
                Message = ex.Message,
                IsError = true
            });
        }
    }

    /// <summary>
    /// Publishes a custom alert event.
    /// </summary>
    public void PublishAlert(string title, string message, string severity)
    {
        _alertEvents.OnNext(new AlertEvent
        {
            Title = title,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.Now
        });

        // Show toast notification for critical alerts
        if (severity == "Critical")
        {
            NotificationService.Instance.ShowError(message, title);
        }
        else if (severity == "Warning")
        {
            NotificationService.Instance.ShowWarning(message, title);
        }
    }

    public void Dispose()
    {
        Stop();
        _dataUpdates.Dispose();
        _alertEvents.Dispose();
        _metricEvents.Dispose();
    }
}

public class DataUpdateEvent
{
    public string Source { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsError { get; init; }
}

public class AlertEvent
{
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? Severity { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int CriticalCount { get; init; }
    public DateTime Timestamp { get; init; }
}

public class MetricEvent
{
    public string? MetricName { get; init; }
    public double Value { get; init; }
    public int TotalCount { get; init; }
    public DateTime Timestamp { get; init; }
}
