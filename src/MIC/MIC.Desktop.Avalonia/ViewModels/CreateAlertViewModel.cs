using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for creating a new alert.
/// </summary>
public class CreateAlertViewModel : ViewModelBase
{
    private readonly IMediator? _mediator;
    
    private string _alertName = string.Empty;
    private string _description = string.Empty;
    private string _source = string.Empty;
    private AlertSeverity _selectedSeverity = AlertSeverity.Info;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public CreateAlertViewModel()
    {
        _mediator = Program.ServiceProvider?.GetService<IMediator>();
        
        // Commands
        var canCreate = this.WhenAnyValue(
            x => x.AlertName,
            x => x.Description,
            x => x.Source,
            x => x.IsLoading,
            (name, desc, source, loading) => 
                !string.IsNullOrWhiteSpace(name) && 
                !string.IsNullOrWhiteSpace(desc) && 
                !string.IsNullOrWhiteSpace(source) && 
                !loading);

        CreateCommand = ReactiveCommand.CreateFromTask(CreateAlertAsync, canCreate);
        CancelCommand = ReactiveCommand.Create(() => OnCancel?.Invoke());
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

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action? OnCreated;
    public event Action? OnCancel;

    #endregion

    #region Methods

    private async Task CreateAlertAsync()
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

            var command = new CreateAlertCommand(
                AlertName,
                Description,
                SelectedSeverity,
                Source
            );

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                ErrorMessage = result.FirstError.Description;
                return;
            }

            NotificationService.Instance.ShowSuccess("Alert created successfully");
            OnCreated?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ErrorHandlingService.Instance.HandleException(ex, "Create Alert");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
