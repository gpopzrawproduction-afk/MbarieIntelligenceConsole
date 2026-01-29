using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MediatR;
using ReactiveUI;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Metrics.Queries.GetMetrics;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Enterprise-grade dashboard with real-time intelligence and glassmorphism design.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
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

    public DashboardViewModel(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _lastUpdated = DateTime.Now.ToString("HH:mm:ss");

        RecentAlerts = new ObservableCollection<DashboardAlertViewModel>();
        RecentPredictions = new ObservableCollection<DashboardPredictionViewModel>();
        RecentEmails = new ObservableCollection<DashboardEmailViewModel>();

        RefreshCommand = ReactiveCommand.CreateFromTask(LoadDashboardDataAsync);
        CheckInboxCommand = ReactiveCommand.Create(CheckInbox);
        ViewUrgentItemsCommand = ReactiveCommand.Create(ViewUrgentItems);
        AIChatCommand = ReactiveCommand.Create(AIChat);

        // Auto-refresh is intentionally started from the view (when attached) to avoid
        // background-thread reactive pipelines during early app startup.
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

    public string LastUpdatedText => $"Last updated: {LastUpdated}";

    public ObservableCollection<DashboardAlertViewModel> RecentAlerts { get; }

    public ObservableCollection<DashboardPredictionViewModel> RecentPredictions { get; }

    public ObservableCollection<DashboardEmailViewModel> RecentEmails { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CheckInboxCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ViewUrgentItemsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AIChatCommand { get; }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);

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

            // Load mock email data for dashboard
            await LoadMockEmailDataAsync();
            
            // Load mock predictions
            await LoadMockPredictionsAsync();

            // Generate AI insights summary
            await GenerateAIInsightsSummaryAsync();

            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            });
        }
        catch (Exception ex)
        {
            // In production, route this through a logging/telemetry abstraction
            Console.WriteLine($"Dashboard load error: {ex.Message}");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task LoadMockEmailDataAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Mock total emails count
            TotalEmails = 1247;
            UnreadCount = 23;
            HighPriorityCount = 5;
            RequiresResponseCount = 8;

            // Clear and populate recent emails
            RecentEmails.Clear();
            var mockEmails = new[]
            {
                new DashboardEmailViewModel
                {
                    Subject = "Q4 Financial Report Review Required",
                    Sender = "CFO@company.com",
                    TimeAgo = "2 min ago",
                    PriorityIcon = "⚠️",
                    PriorityColor = "#EF4444"
                },
                new DashboardEmailViewModel
                {
                    Subject = "Market Analysis Update - Action Needed",
                    Sender = "Analyst@company.com",
                    TimeAgo = "15 min ago",
                    PriorityIcon = "🚨",
                    PriorityColor = "#F59E0B"
                },
                new DashboardEmailViewModel
                {
                    Subject = "Board Meeting Minutes - Follow-up Items",
                    Sender = "Board.Secretary@company.com",
                    TimeAgo = "32 min ago",
                    PriorityIcon = "📝",
                    PriorityColor = "#3B82F6"
                },
                new DashboardEmailViewModel
                {
                    Subject = "Competitor Strategy Shift Identified",
                    Sender = "Intelligence@company.com",
                    TimeAgo = "1 hour ago",
                    PriorityIcon = "🔍",
                    PriorityColor = "#8B5CF6"
                },
                new DashboardEmailViewModel
                {
                    Subject = "New Investment Opportunity - Review",
                    Sender = "Investment.Team@company.com",
                    TimeAgo = "2 hours ago",
                    PriorityIcon = "💰",
                    PriorityColor = "#10B981"
                }
            };

            foreach (var email in mockEmails)
            {
                RecentEmails.Add(email);
            }
        });
    }

    private async Task LoadMockPredictionsAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RecentPredictions.Clear();

            RecentPredictions.Add(new DashboardPredictionViewModel
            {
                Title = "Q4 Revenue Forecast",
                Timestamp = $"Today, {DateTime.Now:HH:mm}",
                Confidence = 87,
                TrendText = "Upward",
                TrendColor = "#10B981"
            });

            RecentPredictions.Add(new DashboardPredictionViewModel
            {
                Title = "Resource Demand Analysis",
                Timestamp = $"Today, {DateTime.Now.AddMinutes(-15):HH:mm}",
                Confidence = 92,
                TrendText = "Stable",
                TrendColor = "#3B82F6"
            });
        });
    }

    private async Task GenerateAIInsightsSummaryAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var insights = new[]
            {
                "Based on your email patterns, you have 5 high-priority items requiring immediate attention, with 3 needing executive decisions by EOD.",
                "Market intelligence suggests 2 emerging threats and 1 significant opportunity in your sector. Recommend scheduling strategy session.",
                "Your response time to critical emails is 23% slower than optimal. Consider delegating routine inquiries to improve efficiency.",
                "AI detected 3 recurring operational bottlenecks across departments. Detailed report available in Intelligence Hub."
            };

            Random random = new Random();
            var selectedInsights = insights.OrderBy(x => random.Next()).Take(2).ToArray();
            AIInsightsSummary = string.Join("\n\n", selectedInsights);
        });
    }

    private void CheckInbox()
    {
        // Navigate to email inbox view
        // This would typically involve raising a navigation event
        Console.WriteLine("Navigating to inbox...");
    }

    private void ViewUrgentItems()
    {
        // Navigate to urgent items view
        // This would typically involve raising a navigation event
        Console.WriteLine("Navigating to urgent items...");
    }

    private void AIChat()
    {
        // Navigate to AI chat view
        // This would typically involve raising a navigation event
        Console.WriteLine("Opening AI chat...");
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


