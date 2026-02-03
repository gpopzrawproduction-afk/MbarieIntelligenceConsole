using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MediatR;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAlertById;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Alert Details dialog for viewing and editing alerts.
/// </summary>
public class AlertDetailsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    private Guid? _alertId;
    private string _alertName = string.Empty;
    private string _description = string.Empty;
    private AlertSeverity _severity = AlertSeverity.Info;
    private AlertStatus _status = AlertStatus.Active;
    private string _source = string.Empty;
    private DateTime _triggeredAt = DateTime.UtcNow;
    private DateTime? _acknowledgedAt;
    private string? _acknowledgedBy;
    private DateTime? _resolvedAt;
    private string? _resolvedBy;
    private string? _resolution;
    private string _notes = string.Empty;
    private string _resolutionNotes = string.Empty;

    private bool _isEditMode;
    private bool _isLoading;
    private bool _isNewAlert;
    private string _errorMessage = string.Empty;
    private string _windowTitle = "Alert Details";

    public AlertDetailsViewModel(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        // Initialize collections
        SeverityOptions = new ObservableCollection<AlertSeverity>
        {
            AlertSeverity.Info,
            AlertSeverity.Warning,
            AlertSeverity.Critical,
            AlertSeverity.Emergency
        };

        StatusOptions = new ObservableCollection<AlertStatus>
        {
            AlertStatus.Active,
            AlertStatus.Acknowledged,
            AlertStatus.Resolved,
            AlertStatus.Escalated
        };

        // Initialize commands
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CancelCommand = ReactiveCommand.Create(Cancel);
        ToggleEditModeCommand = ReactiveCommand.Create(ToggleEditMode);
        AcknowledgeCommand = ReactiveCommand.CreateFromTask(AcknowledgeAsync);
        ResolveCommand = ReactiveCommand.CreateFromTask(ResolveAsync);
    }

    #region Properties

    public ObservableCollection<AlertSeverity> SeverityOptions { get; }
    public ObservableCollection<AlertStatus> StatusOptions { get; }

    public Guid? AlertId
    {
        get => _alertId;
        private set => this.RaiseAndSetIfChanged(ref _alertId, value);
    }

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

    public AlertSeverity Severity
    {
        get => _severity;
        set => this.RaiseAndSetIfChanged(ref _severity, value);
    }

    public AlertStatus Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public string Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    public DateTime TriggeredAt
    {
        get => _triggeredAt;
        set => this.RaiseAndSetIfChanged(ref _triggeredAt, value);
    }

    public DateTime? AcknowledgedAt
    {
        get => _acknowledgedAt;
        set => this.RaiseAndSetIfChanged(ref _acknowledgedAt, value);
    }

    public string? AcknowledgedBy
    {
        get => _acknowledgedBy;
        set => this.RaiseAndSetIfChanged(ref _acknowledgedBy, value);
    }

    public DateTime? ResolvedAt
    {
        get => _resolvedAt;
        set => this.RaiseAndSetIfChanged(ref _resolvedAt, value);
    }

    public string? ResolvedBy
    {
        get => _resolvedBy;
        set => this.RaiseAndSetIfChanged(ref _resolvedBy, value);
    }

    public string? Resolution
    {
        get => _resolution;
        set => this.RaiseAndSetIfChanged(ref _resolution, value);
    }

    public string Notes
    {
        get => _notes;
        set => this.RaiseAndSetIfChanged(ref _notes, value);
    }

    public string ResolutionNotes
    {
        get => _resolutionNotes;
        set => this.RaiseAndSetIfChanged(ref _resolutionNotes, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool IsNewAlert
    {
        get => _isNewAlert;
        set => this.RaiseAndSetIfChanged(ref _isNewAlert, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }

    public bool CanAcknowledge => Status == AlertStatus.Active;
    public bool CanResolve => Status != AlertStatus.Resolved;
    public bool HasResolution => !string.IsNullOrEmpty(Resolution);
    public bool IsViewMode => !IsEditMode;

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleEditModeCommand { get; }
    public ReactiveCommand<Unit, Unit> AcknowledgeCommand { get; }
    public ReactiveCommand<Unit, Unit> ResolveCommand { get; }

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    #endregion

    #region Methods

    /// <summary>
    /// Initializes the view model for creating a new alert.
    /// </summary>
    public void InitializeForNew()
    {
        IsNewAlert = true;
        IsEditMode = true;
        WindowTitle = "Create New Alert";
        AlertId = null;
        AlertName = string.Empty;
        Description = string.Empty;
        Severity = AlertSeverity.Warning;
        Status = AlertStatus.Active;
        Source = string.Empty;
        TriggeredAt = DateTime.UtcNow;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Initializes the view model for viewing/editing an existing alert.
    /// </summary>
    public async Task InitializeForEditAsync(Guid alertId)
    {
        IsNewAlert = false;
        IsEditMode = false;
        WindowTitle = "Alert Details";
        ErrorMessage = string.Empty;

        await LoadAlertAsync(alertId);
    }

    /// <summary>
    /// Loads the alert from the provided DTO.
    /// </summary>
    public void LoadFromDto(AlertDto dto)
    {
        IsNewAlert = false;
        IsEditMode = false;
        WindowTitle = "Alert Details";
        ErrorMessage = string.Empty;

        AlertId = dto.Id;
        AlertName = dto.AlertName;
        Description = dto.Description;
        Severity = dto.Severity;
        Status = dto.Status;
        Source = dto.Source;
        TriggeredAt = dto.TriggeredAt;
        AcknowledgedAt = dto.AcknowledgedAt;
        AcknowledgedBy = dto.AcknowledgedBy;
        ResolvedAt = dto.ResolvedAt;
        ResolvedBy = dto.ResolvedBy;
        Resolution = dto.Resolution;

        this.RaisePropertyChanged(nameof(CanAcknowledge));
        this.RaisePropertyChanged(nameof(CanResolve));
        this.RaisePropertyChanged(nameof(HasResolution));
        this.RaisePropertyChanged(nameof(IsViewMode));
    }

    private async Task LoadAlertAsync(Guid alertId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var query = new GetAlertByIdQuery(alertId);
            var result = await _mediator.Send(query);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            LoadFromDto(result.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load alert: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Validate
            if (string.IsNullOrWhiteSpace(AlertName))
            {
                ErrorMessage = "Alert name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ErrorMessage = "Description is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Source))
            {
                ErrorMessage = "Source is required.";
                return;
            }

            if (IsNewAlert)
            {
                // Create new alert
                var createCommand = new CreateAlertCommand(
                    AlertName,
                    Description,
                    Severity,
                    Source);

                var createResult = await _mediator.Send(createCommand);

                if (createResult.IsError)
                {
                    ErrorMessage = createResult.FirstError.Description;
                    return;
                }

                AlertId = createResult.Value;
            }
            else if (AlertId.HasValue)
            {
                // Update existing alert
                var updateCommand = new UpdateAlertCommand
                {
                    AlertId = AlertId.Value,
                    NewStatus = Status,
                    UpdatedBy = UserSessionService.Instance.CurrentUserName,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    ResolutionNotes = Status == AlertStatus.Resolved 
                        ? ResolutionNotes 
                        : null
                };

                var updateResult = await _mediator.Send(updateCommand);

                if (updateResult.IsError)
                {
                    ErrorMessage = updateResult.FirstError.Description;
                    return;
                }
            }

            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
        this.RaisePropertyChanged(nameof(IsViewMode));
    }

    private async Task AcknowledgeAsync()
    {
        if (!AlertId.HasValue) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var command = new UpdateAlertCommand
            {
                AlertId = AlertId.Value,
                NewStatus = AlertStatus.Acknowledged,
                UpdatedBy = UserSessionService.Instance.CurrentUserName
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            // Update local state
            Status = AlertStatus.Acknowledged;
            AcknowledgedAt = DateTime.UtcNow;
            AcknowledgedBy = "CurrentUser";

            this.RaisePropertyChanged(nameof(CanAcknowledge));
            this.RaisePropertyChanged(nameof(CanResolve));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to acknowledge: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResolveAsync()
    {
        if (!AlertId.HasValue) return;

        if (string.IsNullOrWhiteSpace(ResolutionNotes))
        {
            ErrorMessage = "Resolution notes are required to resolve an alert.";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var command = new UpdateAlertCommand
            {
                AlertId = AlertId.Value,
                NewStatus = AlertStatus.Resolved,
                UpdatedBy = UserSessionService.Instance.CurrentUserName,
                ResolutionNotes = ResolutionNotes
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            // Update local state
            Status = AlertStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
            ResolvedBy = "CurrentUser";
            Resolution = ResolutionNotes;

            this.RaisePropertyChanged(nameof(CanAcknowledge));
            this.RaisePropertyChanged(nameof(CanResolve));
            this.RaisePropertyChanged(nameof(HasResolution));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to resolve: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
