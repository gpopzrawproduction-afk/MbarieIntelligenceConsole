using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAlertById;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for editing an existing alert.
/// </summary>
public class EditAlertViewModel : ViewModelBase
{
    private readonly IMediator? _mediator;
    private readonly Guid _alertId;
    
    private string _alertName = string.Empty;
    private string _description = string.Empty;
    private string _source = string.Empty;
    private AlertSeverity _selectedSeverity = AlertSeverity.Info;
    private AlertStatus _selectedStatus = AlertStatus.Active;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public EditAlertViewModel(Guid alertId)
    {
        _mediator = Program.ServiceProvider?.GetService<IMediator>();
        _alertId = alertId;
        
        // Commands
        var canUpdate = this.WhenAnyValue(
            x => x.AlertName,
            x => x.Description,
            x => x.Source,
            x => x.IsLoading,
            (name, desc, source, loading) => 
                !string.IsNullOrWhiteSpace(name) && 
                !string.IsNullOrWhiteSpace(desc) && 
                !string.IsNullOrWhiteSpace(source) && 
                !loading);

        canUpdate = canUpdate.ObserveOn(RxApp.MainThreadScheduler);

        UpdateCommand = ReactiveCommand.CreateFromTask(UpdateAlertAsync, canUpdate);
        CancelCommand = ReactiveCommand.Create(() => OnCancel?.Invoke());
        
        // Load alert data
        _ = LoadAlertDataAsync();
    }

    #region Properties

    public string AlertName
    {
        get => _alertName;
        set => this.RaiseAndSetIfChanged(ref _alertName, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public string Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    public AlertSeverity SelectedSeverity
    {
        get => _selectedSeverity;
        set => this.RaiseAndSetIfChanged(ref _selectedSeverity, value);
    }

    public AlertStatus SelectedStatus
    {
        get => _selectedStatus;
        set => this.RaiseAndSetIfChanged(ref _selectedStatus, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ObservableCollection<AlertSeverity> Severities { get; } = new()
    {
        AlertSeverity.Info,
        AlertSeverity.Warning,
        AlertSeverity.Critical,
        AlertSeverity.Emergency
    };

    public ObservableCollection<AlertStatus> Statuses { get; } = new()
    {
        AlertStatus.Active,
        AlertStatus.Acknowledged,
        AlertStatus.Resolved,
        AlertStatus.Escalated
    };

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> UpdateCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action? OnUpdated;
    public event Action? OnCancel;

    #endregion

    #region Methods

    private async Task LoadAlertDataAsync()
    {
        if (_mediator == null)
        {
            ErrorMessage = "Service not available";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var query = new GetAlertByIdQuery(_alertId);
            var result = await _mediator.Send(query);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            var alert = result.Value;
            AlertName = alert.AlertName;
            Description = alert.Description;
            Source = alert.Source;
            SelectedSeverity = alert.Severity;
            SelectedStatus = alert.Status;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ErrorHandlingService.Instance.HandleException(ex, "Load Alert Data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateAlertAsync()
    {
        if (_mediator == null)
        {
            ErrorMessage = "Service not available";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var command = new UpdateAlertCommand
            {
                AlertId = _alertId,
                AlertName = AlertName,
                Description = Description,
                Source = Source,
                Severity = SelectedSeverity,
                NewStatus = SelectedStatus,
                UpdatedBy = UserSessionService.Instance?.CurrentUserName ?? "System",
                Notes = $"Alert metadata updated: Name='{AlertName}', Description='{Description}', Source='{Source}', Severity={SelectedSeverity}, Status={SelectedStatus}"
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            NotificationService.Instance.ShowSuccess("Alert updated successfully");
            OnUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ErrorHandlingService.Instance.HandleException(ex, "Update Alert");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}