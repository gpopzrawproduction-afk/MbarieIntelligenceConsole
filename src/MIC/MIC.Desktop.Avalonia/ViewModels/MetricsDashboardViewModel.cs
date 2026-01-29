using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Metrics.Common;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Application.Metrics.Queries.GetMetricTrend;
using ReactiveUI;
using SkiaSharp;
using Timer = System.Timers.Timer;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Metrics Dashboard with charts and KPI cards.
/// </summary>
public class MetricsDashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IMediator? _mediator;
    private readonly Timer _refreshTimer;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private string _selectedCategory = "All";
    private int _selectedDays = 30;

    public MetricsDashboardViewModel()
    {
        _mediator = Program.ServiceProvider?.GetService<IMediator>();

        // Initialize collections
        KpiCards = new ObservableCollection<KpiCardViewModel>();
        Categories = new ObservableCollection<string> { "All", "Financial", "Operations", "Performance", "Customer" };
        DaysOptions = new ObservableCollection<int> { 7, 14, 30, 60, 90 };

        // Initialize chart series
        InitializeCharts();

        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
        ExportCommand = ReactiveCommand.CreateFromTask(ExportToCsvAsync);

        // Set up auto-refresh timer (30 seconds)
        _refreshTimer = new Timer(30000);
        _refreshTimer.Elapsed += async (_, _) => 
        {
            // Marshal to UI thread before loading data that updates UI
            await Dispatcher.UIThread.InvokeAsync(async () => 
            {
                await LoadDataAsync();
            });
        };
        _refreshTimer.AutoReset = true;

        // Load initial data
        _ = LoadDataAsync();
    }

    public MetricsDashboardViewModel(IMediator mediator) : this()
    {
        _mediator = mediator;
    }

    #region Properties

    public ObservableCollection<KpiCardViewModel> KpiCards { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<int> DaysOptions { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            _ = LoadDataAsync();
        }
    }

    public int SelectedDays
    {
        get => _selectedDays;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDays, value);
            _ = LoadDataAsync();
        }
    }

    // Revenue Chart
    public ISeries[] RevenueSeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] RevenueXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] RevenueYAxes { get; private set; } = Array.Empty<Axis>();

    // Efficiency Chart
    public ISeries[] EfficiencySeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] EfficiencyXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] EfficiencyYAxes { get; private set; } = Array.Empty<Axis>();

    // Category Comparison Chart
    public ISeries[] CategorySeries { get; private set; } = Array.Empty<ISeries>();
    public Axis[] CategoryXAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] CategoryYAxes { get; private set; } = Array.Empty<Axis>();

    // Performance Gauges Data
    public double UptimeValue { get; private set; }
    public double ResponseTimeValue { get; private set; }
    public double ErrorRateValue { get; private set; }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    #endregion

    #region Chart Initialization

    private void InitializeCharts()
    {
        // Neon cyan color for charts
        var cyanPaint = new SolidColorPaint(SKColor.Parse("#00E5FF"));
        var greenPaint = new SolidColorPaint(SKColor.Parse("#39FF14"));
        var orangePaint = new SolidColorPaint(SKColor.Parse("#FF6B00"));
        var magentaPaint = new SolidColorPaint(SKColor.Parse("#BF40FF"));
        var gridPaint = new SolidColorPaint(SKColor.Parse("#20FFFFFF"));
        var labelPaint = new SolidColorPaint(SKColor.Parse("#607D8B"));

        // Revenue chart setup
        RevenueSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Revenue",
                Values = new ObservableCollection<DateTimePoint>(),
                Stroke = cyanPaint,
                GeometryStroke = cyanPaint,
                GeometrySize = 0,
                Fill = null,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Target",
                Values = new ObservableCollection<DateTimePoint>(),
                Stroke = new SolidColorPaint(SKColor.Parse("#40FFFFFF")) { StrokeThickness = 2 },
                GeometrySize = 0,
                Fill = null
            }
        };

        RevenueXAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM dd"))
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint
            }
        };

        RevenueYAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint,
                Labeler = value => $"${value / 1000:N0}K"
            }
        };

        // Efficiency chart setup
        EfficiencySeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Efficiency",
                Values = new ObservableCollection<DateTimePoint>(),
                Stroke = greenPaint,
                GeometryStroke = greenPaint,
                GeometrySize = 0,
                Fill = null,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Customer Satisfaction",
                Values = new ObservableCollection<DateTimePoint>(),
                Stroke = magentaPaint,
                GeometryStroke = magentaPaint,
                GeometrySize = 0,
                Fill = null,
                LineSmoothness = 0.5
            }
        };

        EfficiencyXAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM dd"))
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint
            }
        };

        EfficiencyYAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint,
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = value => $"{value:N0}%"
            }
        };

        // Category comparison chart
        CategorySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Current",
                Values = new ObservableCollection<double>(),
                Fill = cyanPaint,
                MaxBarWidth = 40
            },
            new ColumnSeries<double>
            {
                Name = "Target",
                Values = new ObservableCollection<double>(),
                Fill = new SolidColorPaint(SKColor.Parse("#40FFFFFF")),
                MaxBarWidth = 40
            }
        };

        CategoryXAxes = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "Revenue", "Efficiency", "Uptime", "Satisfaction" },
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint
            }
        };

        CategoryYAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = gridPaint,
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = value => $"{value:N0}%"
            }
        };
    }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        if (_mediator is null)
        {
            await LoadPlaceholderDataAsync();
            return;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                StatusMessage = "Loading metrics...";
            });

            // Load KPI cards
            await LoadKpiCardsAsync();

            // Load trend charts
            await LoadTrendChartsAsync();

            // Load category comparison
            await LoadCategoryComparisonAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Last updated: {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
            await LoadPlaceholderDataAsync();
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task LoadKpiCardsAsync()
    {
        var query = new GetMetricsQuery
        {
            Category = SelectedCategory == "All" ? null : SelectedCategory,
            LatestOnly = true
        };

        var result = await _mediator!.Send(query);

        if (result.IsError) return;

        var kpiCardViewModels = new List<KpiCardViewModel>();
        var targets = new Dictionary<string, double>
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

        foreach (var metric in result.Value.Take(8))
        {
            targets.TryGetValue(metric.MetricName, out var target);
            
            var progress = target > 0 
                ? Math.Min(100, metric.Value / target * 100) 
                : 50;

            var status = progress switch
            {
                >= 90 => "On Target",
                >= 70 => "Warning",
                _ => "Critical"
            };

            kpiCardViewModels.Add(new KpiCardViewModel
            {
                Title = metric.MetricName,
                Value = metric.FormattedValue,
                Target = target > 0 ? $"Target: {target:N0}" : "",
                Change = metric.FormattedChange,
                TrendIcon = metric.TrendIcon,
                Status = status,
                Progress = progress,
                Color = status switch
                {
                    "On Target" => "#39FF14",
                    "Warning" => "#FF6B00",
                    _ => "#FF0055"
                }
            });
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            KpiCards.Clear();
            foreach (var kpi in kpiCardViewModels)
            {
                KpiCards.Add(kpi);
            }
        });
    }

    private async Task LoadTrendChartsAsync()
    {
        // Load Revenue trend
        var revenueQuery = new GetMetricTrendQuery
        {
            MetricName = "Revenue",
            Days = SelectedDays
        };

        var revenueResult = await _mediator!.Send(revenueQuery);
        if (!revenueResult.IsError)
        {
            var revenueValues = (ObservableCollection<DateTimePoint>)RevenueSeries[0].Values!;
            revenueValues.Clear();
            foreach (var point in revenueResult.Value.DataPoints)
            {
                revenueValues.Add(new DateTimePoint(point.Timestamp, point.Value));
            }

            // Add target line
            var targetValues = (ObservableCollection<DateTimePoint>)RevenueSeries[1].Values!;
            targetValues.Clear();
            var startDate = DateTime.UtcNow.AddDays(-SelectedDays);
            for (int i = 0; i <= SelectedDays; i++)
            {
                targetValues.Add(new DateTimePoint(startDate.AddDays(i), 150000));
            }
        }

        // Load Efficiency trend
        var efficiencyQuery = new GetMetricTrendQuery
        {
            MetricName = "Operational Efficiency",
            Days = SelectedDays
        };

        var efficiencyResult = await _mediator.Send(efficiencyQuery);
        if (!efficiencyResult.IsError)
        {
            var efficiencyValues = (ObservableCollection<DateTimePoint>)EfficiencySeries[0].Values!;
            efficiencyValues.Clear();
            foreach (var point in efficiencyResult.Value.DataPoints)
            {
                efficiencyValues.Add(new DateTimePoint(point.Timestamp, point.Value));
            }
        }

        // Load Customer Satisfaction
        var satisfactionQuery = new GetMetricTrendQuery
        {
            MetricName = "Customer Satisfaction",
            Days = SelectedDays
        };

        var satisfactionResult = await _mediator.Send(satisfactionQuery);
        if (!satisfactionResult.IsError)
        {
            var satisfactionValues = (ObservableCollection<DateTimePoint>)EfficiencySeries[1].Values!;
            satisfactionValues.Clear();
            foreach (var point in satisfactionResult.Value.DataPoints)
            {
                // Scale to percentage (0-5 to 0-100)
                satisfactionValues.Add(new DateTimePoint(point.Timestamp, point.Value * 20));
            }
        }

        // Update gauge values
        var uptimeQuery = new GetMetricTrendQuery { MetricName = "System Uptime", Days = 1 };
        var uptimeResult = await _mediator.Send(uptimeQuery);
        if (!uptimeResult.IsError)
        {
            UptimeValue = uptimeResult.Value.CurrentValue;
            this.RaisePropertyChanged(nameof(UptimeValue));
        }

        var responseQuery = new GetMetricTrendQuery { MetricName = "Avg Response Time", Days = 1 };
        var responseResult = await _mediator.Send(responseQuery);
        if (!responseResult.IsError)
        {
            ResponseTimeValue = responseResult.Value.CurrentValue;
            this.RaisePropertyChanged(nameof(ResponseTimeValue));
        }

        var errorQuery = new GetMetricTrendQuery { MetricName = "Error Rate", Days = 1 };
        var errorResult = await _mediator.Send(errorQuery);
        if (!errorResult.IsError)
        {
            ErrorRateValue = errorResult.Value.CurrentValue;
            this.RaisePropertyChanged(nameof(ErrorRateValue));
        }
    }

    private async Task LoadCategoryComparisonAsync()
    {
        var currentValues = (ObservableCollection<double>)CategorySeries[0].Values!;
        var targetValues = (ObservableCollection<double>)CategorySeries[1].Values!;
        
        currentValues.Clear();
        targetValues.Clear();

        // Revenue (normalized to %)
        var revenueQuery = new GetMetricsQuery { MetricName = "Revenue", LatestOnly = true };
        var revenueResult = await _mediator!.Send(revenueQuery);
        currentValues.Add(revenueResult.IsError ? 0 : revenueResult.Value.FirstOrDefault()?.Value / 1500 ?? 0);
        targetValues.Add(100);

        // Efficiency
        var effQuery = new GetMetricsQuery { MetricName = "Operational Efficiency", LatestOnly = true };
        var effResult = await _mediator.Send(effQuery);
        currentValues.Add(effResult.IsError ? 0 : effResult.Value.FirstOrDefault()?.Value ?? 0);
        targetValues.Add(85);

        // Uptime
        var upQuery = new GetMetricsQuery { MetricName = "System Uptime", LatestOnly = true };
        var upResult = await _mediator.Send(upQuery);
        currentValues.Add(upResult.IsError ? 0 : upResult.Value.FirstOrDefault()?.Value ?? 0);
        targetValues.Add(99.9);

        // Satisfaction (scaled)
        var satQuery = new GetMetricsQuery { MetricName = "Customer Satisfaction", LatestOnly = true };
        var satResult = await _mediator.Send(satQuery);
        currentValues.Add(satResult.IsError ? 0 : (satResult.Value.FirstOrDefault()?.Value ?? 0) * 20);
        targetValues.Add(90);
    }

    private async Task LoadPlaceholderDataAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            KpiCards.Clear();
            
            KpiCards.Add(new KpiCardViewModel
            {
                Title = "Revenue",
                Value = "$127,450",
                Target = "Target: $150,000",
                Change = "+8.3%",
                TrendIcon = "?",
                Status = "Warning",
                Progress = 85,
                Color = "#FF6B00"
            });

            KpiCards.Add(new KpiCardViewModel
            {
                Title = "Efficiency",
                Value = "82.5%",
                Target = "Target: 85%",
                Change = "+2.1%",
                TrendIcon = "?",
                Status = "On Target",
                Progress = 97,
                Color = "#39FF14"
            });

            KpiCards.Add(new KpiCardViewModel
            {
                Title = "Uptime",
                Value = "99.7%",
                Target = "Target: 99.9%",
                Change = "+0.1%",
                TrendIcon = "?",
                Status = "On Target",
                Progress = 99.8,
                Color = "#39FF14"
            });

            KpiCards.Add(new KpiCardViewModel
            {
                Title = "Satisfaction",
                Value = "4.3/5",
                Target = "Target: 4.5",
                Change = "+0.2",
                TrendIcon = "?",
                Status = "On Target",
                Progress = 95,
                Color = "#39FF14"
            });

            UptimeValue = 99.7;
            ResponseTimeValue = 142;
            ErrorRateValue = 0.6;

            StatusMessage = "Showing sample data";
        });
    }

    #endregion

    #region Export

    private Task ExportToCsvAsync()
    {
        // TODO: Implement CSV export
        StatusMessage = "Export feature coming soon...";
        return Task.CompletedTask;
    }

    #endregion

    #region Lifecycle

    public void StartAutoRefresh()
    {
        _refreshTimer.Start();
    }

    public void StopAutoRefresh()
    {
        _refreshTimer.Stop();
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
    }

    #endregion
}

/// <summary>
/// View model for individual KPI card display.
/// </summary>
public class KpiCardViewModel
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Change { get; init; } = string.Empty;
    public string TrendIcon { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Progress { get; init; }
    public string Color { get; init; } = "#00E5FF";
}
