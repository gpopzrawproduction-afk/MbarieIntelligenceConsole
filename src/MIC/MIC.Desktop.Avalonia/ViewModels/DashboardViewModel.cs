using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MediatR;
using ReactiveUI;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Application.Common.Interfaces;
using Serilog;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Enterprise-grade dashboard with real-time intelligence and glassmorphism design.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger _logger;
    private readonly DispatcherTimer _refreshTimer;
    private int _activeAlerts;
    private int _totalMetrics;
    private int _predictions;
    private int _initiatives;
    private int _totalEmails;
    private int _unreadCount;
    private int _highPriorityCount;
    private int _requiresResponseCount;
    private string _lastUpdated;
    private string _aiInsightsSummary = string.Empty;
    private bool _isLoading;
    private bool _autoRefreshEnabled = true;
    private int _refreshIntervalSeconds = 30;
    private string _refreshStatus = string.Empty;

    public DashboardViewModel(IMediator mediator, INavigationService navigationService, ISessionService sessionService, IEmailRepository emailRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _emailRepository = emailRepository ?? throw new ArgumentNullException(nameof(emailRepository));
        _logger = Log.ForContext<DashboardViewModel>();
        _lastUpdated = DateTime.Now.ToString("HH:mm:ss");

        RecentAlerts = new ObservableCollection<DashboardAlertViewModel>();
        RecentPredictions = new ObservableCollection<DashboardPredictionViewModel>();
        RecentEmails = new ObservableCollection<DashboardEmailViewModel>();

        RefreshCommand = ReactiveCommand.CreateFromTask(LoadDashboardDataAsync);
        CheckInboxCommand = ReactiveCommand.Create(CheckInbox);
        ViewUrgentItemsCommand = ReactiveCommand.Create(ViewUrgentItems);
        AIChatCommand = ReactiveCommand.Create(AIChat);
        ToggleAutoRefreshCommand = ReactiveCommand.Create(ToggleAutoRefresh);

        // Setup auto-refresh timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds)
        };
        _refreshTimer.Tick += async (s, e) => await OnAutoRefreshTick();
        
        // Start timer if auto-refresh is enabled
        if (_autoRefreshEnabled)
        {
            _refreshTimer.Start();
            _logger.Information("Dashboard auto-refresh started - interval: {Interval}s", _refreshIntervalSeconds);
        }
    }

    public int ActiveAlerts
    {
        get => _activeAlerts;
        set => this.RaiseAndSetIfChanged(ref _activeAlerts, value);
    }

    public int TotalMetrics
    {
        get => _totalMetrics;
        set => this.RaiseAndSetIfChanged(ref _totalMetrics, value);
    }

    public int Predictions
    {
        get => _predictions;
        set => this.RaiseAndSetIfChanged(ref _predictions, value);
    }

    public int Initiatives
    {
        get => _initiatives;
        set => this.RaiseAndSetIfChanged(ref _initiatives, value);
    }

    public int TotalEmails
    {
        get => _totalEmails;
        set => this.RaiseAndSetIfChanged(ref _totalEmails, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set => this.RaiseAndSetIfChanged(ref _unreadCount, value);
    }

    public int HighPriorityCount
    {
        get => _highPriorityCount;
        set => this.RaiseAndSetIfChanged(ref _highPriorityCount, value);
    }

    public int RequiresResponseCount
    {
        get => _requiresResponseCount;
        set => this.RaiseAndSetIfChanged(ref _requiresResponseCount, value);
    }

    public string AIInsightsSummary
    {
        get => _aiInsightsSummary;
        set => this.RaiseAndSetIfChanged(ref _aiInsightsSummary, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoRefreshEnabled, value);
            if (value)
            {
                _refreshTimer.Start();
                RefreshStatus = $"Auto-refresh: ON ({_refreshIntervalSeconds}s)";
                _logger.Information("✅ Dashboard auto-refresh ENABLED");
            }
            else
            {
                _refreshTimer.Stop();
                RefreshStatus = "Auto-refresh: OFF";
                _logger.Information("⏸️  Dashboard auto-refresh DISABLED");
            }
        }
    }

    public string RefreshStatus
    {
        get => _refreshStatus;
        set => this.RaiseAndSetIfChanged(ref _refreshStatus, value);
    }

    public string LastUpdatedText => $"Last updated: {LastUpdated}";

    public ObservableCollection<DashboardAlertViewModel> RecentAlerts { get; }

    public ObservableCollection<DashboardPredictionViewModel> RecentPredictions { get; }

    public ObservableCollection<DashboardEmailViewModel> RecentEmails { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CheckInboxCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ViewUrgentItemsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AIChatCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleAutoRefreshCommand { get; }

    private async Task OnAutoRefreshTick()
    {
        if (!AutoRefreshEnabled || IsLoading)
            return;

        try
        {
            _logger.Debug("🔄 Auto-refresh tick - refreshing dashboard data");
            RefreshStatus = $"Refreshing... ({DateTime.Now:HH:mm:ss})";
            await LoadDashboardDataAsync();
            RefreshStatus = $"Auto-refresh: ON ({_refreshIntervalSeconds}s)";
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "⚠️ Auto-refresh failed");
            RefreshStatus = "Auto-refresh: ERROR";
        }
    }

    private void ToggleAutoRefresh()
    {
        AutoRefreshEnabled = !AutoRefreshEnabled;
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                IsLoading = true;
                RefreshStatus = "Loading...";
            });

            var alertsQuery = new GetAllAlertsQuery();
            var alertsResult = await _mediator.Send(alertsQuery).ConfigureAwait(false);

            if (!alertsResult.IsError && alertsResult.Value is { } alerts)
            {
                var alertList = alerts.ToList();

                var activeAlertsCount = alertList.Count(a =>
                    a.Status is MIC.Core.Domain.Entities.AlertStatus.Active or MIC.Core.Domain.Entities.AlertStatus.Acknowledged);

                var recentAlerts = alertList.Take(3).Select(alert => new DashboardAlertViewModel
                {
                    Title = alert.AlertName,
                    Source = alert.Source,
                    TimeAgo = GetRelativeTime(alert.TriggeredAt),
                    SeverityText = alert.Severity.ToString(),
                    SeverityColor = GetSeverityColor(alert.Severity.ToString()),
                    SeverityBadgeBackground = GetSeverityBadgeBackground(alert.Severity.ToString())
                }).ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ActiveAlerts = activeAlertsCount;
                    RecentAlerts.Clear();
                    foreach (var alert in recentAlerts)
                    {
                        RecentAlerts.Add(alert);
                    }
                });
            }

            var metricsQuery = new GetMetricsQuery();
            var metricsResult = await _mediator.Send(metricsQuery).ConfigureAwait(false);

            if (!metricsResult.IsError && metricsResult.Value is { } metrics)
            {
                await Dispatcher.UIThread.InvokeAsync(() => TotalMetrics = metrics.Count);
            }

            await LoadEmailDataAsync();

            // Predictions are not available until a real prediction service is configured
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Predictions = 0;
                RecentPredictions.Clear();
            });

            await GenerateAIInsightsSummaryAsync();

            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
                RefreshStatus = AutoRefreshEnabled ? $"Auto-refresh: ON ({_refreshIntervalSeconds}s)" : "Auto-refresh: OFF";
            });
            
            _logger.Information("✅ Dashboard data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Dashboard load error");
            await Dispatcher.UIThread.InvokeAsync(() => RefreshStatus = "Error refreshing");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task LoadEmailDataAsync()
    {
        var userId = _sessionService.GetUser().Id;
        if (userId == Guid.Empty)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalEmails = 0;
                UnreadCount = 0;
                HighPriorityCount = 0;
                RequiresResponseCount = 0;
                RecentEmails.Clear();
            });
            return;
        }

        var recentEmails = await _emailRepository.GetEmailsAsync(
            userId,
            folder: MIC.Core.Domain.Entities.EmailFolder.Inbox,
            isUnread: null,
            skip: 0,
            take: 5);

        var unreadCount = await _emailRepository.GetUnreadCountAsync(userId);
        var requiresResponseCount = await _emailRepository.GetRequiresResponseCountAsync(userId);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TotalEmails = recentEmails.Count;
            UnreadCount = unreadCount;
            HighPriorityCount = recentEmails.Count(e => e.AIPriority is MIC.Core.Domain.Entities.EmailPriority.High or MIC.Core.Domain.Entities.EmailPriority.Urgent);
            RequiresResponseCount = requiresResponseCount;

            RecentEmails.Clear();
            foreach (var email in recentEmails)
            {
                RecentEmails.Add(new DashboardEmailViewModel
                {
                    Subject = email.Subject,
                    Sender = email.FromAddress,
                    TimeAgo = GetRelativeTime(email.ReceivedDate),
                    PriorityIcon = email.AIPriority is MIC.Core.Domain.Entities.EmailPriority.Urgent ? "🚨" : "⚠️",
                    PriorityColor = email.AIPriority is MIC.Core.Domain.Entities.EmailPriority.Urgent ? "#EF4444" : "#F59E0B"
                });
            }
        });
    }

    private async Task GenerateAIInsightsSummaryAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AIInsightsSummary = string.Empty;
        });
    }

    private void CheckInbox()
    {
        // Navigate to email inbox view
        _navigationService.NavigateTo("Email");
    }

    private void ViewUrgentItems()
    {
        // Navigate to urgent items (Alerts) view
        _navigationService.NavigateTo("Alerts");
    }

    private void AIChat()
    {
        // Navigate to AI chat view
        _navigationService.NavigateTo("AI Chat");
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";

        return dateTime.ToString("MMM dd, HH:mm");
    }

    private static string GetSeverityColor(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "emergency" => "#EF4444",
            "critical" => "#EF4444",
            "warning" => "#F59E0B",
            "info" => "#3B82F6",
            _ => "#64748B"
        };
    }

    private static string GetSeverityBadgeBackground(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "emergency" => "#EF444420",
            "critical" => "#EF444420",
            "warning" => "#F59E0B20",
            "info" => "#3B82F620",
            _ => "#64748B20"
        };
    }
}

/// <summary>
/// Alert card for dashboard display.
/// </summary>
public class DashboardAlertViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string SeverityText { get; set; } = string.Empty;
    public string SeverityColor { get; set; } = "#64748B";
    public string SeverityBadgeBackground { get; set; } = "#64748B20";
}

/// <summary>
/// Prediction card for dashboard display.
/// </summary>
public class DashboardPredictionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string ConfidenceText => $"{Confidence}%";
    public string TrendText { get; set; } = string.Empty;
    public string TrendColor { get; set; } = "#64748B";
}

/// <summary>
/// Email card for dashboard display.
/// </summary>
public class DashboardEmailViewModel
{
    public string Subject { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string PriorityIcon { get; set; } = string.Empty;
    public string PriorityColor { get; set; } = "#64748B";
}


