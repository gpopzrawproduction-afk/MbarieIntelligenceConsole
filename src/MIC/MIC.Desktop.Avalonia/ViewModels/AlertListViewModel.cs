using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MediatR;
using MIC.Core.Application.Alerts.Commands.DeleteAlert;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Alert List view displaying all alerts with filtering.
/// </summary>
public class AlertListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    private bool _isLoading;
    private string _searchText = string.Empty;
    private AlertSeverity? _selectedSeverity;
    private AlertStatus? _selectedStatus;
    private AlertDto? _selectedAlert;
    private string _statusMessage = string.Empty;

    public AlertListViewModel(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        // Initialize collections
        Alerts = new ObservableCollection<AlertDto>();
        SeverityOptions = new ObservableCollection<SeverityFilterItem>
        {
            new("All", null),
            new("Info", AlertSeverity.Info),
            new("Warning", AlertSeverity.Warning),
            new("Critical", AlertSeverity.Critical),
            new("Emergency", AlertSeverity.Emergency)
        };
        StatusOptions = new ObservableCollection<StatusFilterItem>
        {
            new("All", null),
            new("Active", AlertStatus.Active),
            new("Acknowledged", AlertStatus.Acknowledged),
            new("Resolved", AlertStatus.Resolved),
            new("Escalated", AlertStatus.Escalated)
        };

        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadAlertsAsync);
        CreateAlertCommand = ReactiveCommand.Create(CreateNewAlert);
        EditAlertCommand = ReactiveCommand.Create<AlertDto>(EditAlert);
        DeleteAlertCommand = ReactiveCommand.CreateFromTask<AlertDto>(DeleteAlertAsync);
        ViewDetailsCommand = ReactiveCommand.Create<AlertDto>(ViewAlertDetails);
        AcknowledgeAlertCommand = ReactiveCommand.CreateFromTask<AlertDto>(AcknowledgeAlertAsync);
        ExportCommand = ReactiveCommand.CreateFromTask(ExportAlertsAsync);

        // Set up automatic refresh when filters change
        this.WhenAnyValue(
                x => x.SearchText,
                x => x.SelectedSeverity,
                x => x.SelectedStatus)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(_ => Unit.Default)
            .InvokeCommand(RefreshCommand);

        // Load initial data
        _ = LoadAlertsAsync();
    }

    #region Properties

    public ObservableCollection<AlertDto> Alerts { get; }
    public ObservableCollection<SeverityFilterItem> SeverityOptions { get; }
    public ObservableCollection<StatusFilterItem> StatusOptions { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public AlertSeverity? SelectedSeverity
    {
        get => _selectedSeverity;
        set => this.RaiseAndSetIfChanged(ref _selectedSeverity, value);
    }

    public AlertStatus? SelectedStatus
    {
        get => _selectedStatus;
        set => this.RaiseAndSetIfChanged(ref _selectedStatus, value);
    }

    public AlertDto? SelectedAlert
    {
        get => _selectedAlert;
        set => this.RaiseAndSetIfChanged(ref _selectedAlert, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateAlertCommand { get; }
    public ReactiveCommand<AlertDto, Unit> EditAlertCommand { get; }
    public ReactiveCommand<AlertDto, Unit> DeleteAlertCommand { get; }
    public ReactiveCommand<AlertDto, Unit> ViewDetailsCommand { get; }
    public ReactiveCommand<AlertDto, Unit> AcknowledgeAlertCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    /// <summary>
    /// Event raised when a new alert should be created.
    /// </summary>
    public event EventHandler? CreateAlertRequested;

    /// <summary>
    /// Event raised when an alert should be edited.
    /// </summary>
    public event EventHandler<AlertDto>? EditAlertRequested;

    /// <summary>
    /// Event raised when alert details should be viewed.
    /// </summary>
    public event EventHandler<AlertDto>? ViewDetailsRequested;

    #endregion

    #region Methods

    private async Task LoadAlertsAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                StatusMessage = "Loading alerts...";
            });

            var query = new GetAllAlertsQuery
            {
                Severity = SelectedSeverity,
                Status = SelectedStatus,
                SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Take = 100
            };

            var result = await _mediator.Send(query);

            if (result.IsError)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"Error loading alerts: {result.FirstError.Description}";
                });
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Alerts.Clear();
                foreach (var alert in result.Value)
                {
                    Alerts.Add(alert);
                }
                StatusMessage = $"Loaded {Alerts.Count} alert(s)";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private void CreateNewAlert()
    {
        CreateAlertRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditAlert(AlertDto alert)
    {
        if (alert is null) return;
        EditAlertRequested?.Invoke(this, alert);
    }

    private void ViewAlertDetails(AlertDto alert)
    {
        if (alert is null) return;
        SelectedAlert = alert;
        ViewDetailsRequested?.Invoke(this, alert);
    }

    private async Task DeleteAlertAsync(AlertDto alert)
    {
        if (alert is null) return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                StatusMessage = "Deleting alert...";
            });

            var command = new DeleteAlertCommand(alert.Id, "CurrentUser"); // TODO: Get actual user
            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"Error deleting alert: {result.FirstError.Description}";
                });
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Alerts.Remove(alert);
                StatusMessage = "Alert deleted successfully";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private async Task AcknowledgeAlertAsync(AlertDto alert)
    {
        if (alert is null) return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                StatusMessage = "Acknowledging alert...";
            });

            var command = new Core.Application.Alerts.Commands.UpdateAlert.UpdateAlertCommand
            {
                AlertId = alert.Id,
                NewStatus = AlertStatus.Acknowledged,
                UpdatedBy = "CurrentUser" // TODO: Get actual user
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = $"Error: {result.FirstError.Description}";
                });
                return;
            }

            // Refresh the list
            await LoadAlertsAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = "Alert acknowledged";
            });
            
            NotificationService.Instance.ShowSuccess("Alert acknowledged");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }
    
    private async Task ExportAlertsAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);
            var filepath = await ExportService.Instance.ExportAlertsToCsvAsync(Alerts);
            ExportService.Instance.OpenFile(filepath);
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Export Alerts");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    #endregion
}

/// <summary>
/// Filter item for severity dropdown.
/// </summary>
public record SeverityFilterItem(string Display, AlertSeverity? Value);

/// <summary>
/// Filter item for status dropdown.
/// </summary>
public record StatusFilterItem(string Display, AlertStatus? Value);
