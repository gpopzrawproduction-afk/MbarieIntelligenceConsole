using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Intelligence.Predictions;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.ViewModels;

public class PredictionsViewModel : ViewModelBase
{
    private readonly IPredictiveAnalyticsService _predictiveService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<PredictionsViewModel> _logger;

    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private int _selectedTimeHorizon = 30; // days

    public PredictionsViewModel(
        IPredictiveAnalyticsService predictiveService,
        ISessionService sessionService,
        ILogger<PredictionsViewModel> logger)
    {
        _predictiveService = predictiveService;
        _sessionService = sessionService;
        _logger = logger;

        Predictions = new ObservableCollection<PredictionViewModel>();

        GeneratePredictionsCommand = ReactiveCommand.CreateFromTask(GeneratePredictionsAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadPredictionsAsync);
        ExportPredictionsCommand = ReactiveCommand.CreateFromTask(ExportPredictionsAsync);

        // Load predictions on initialization
        _ = LoadPredictionsAsync();
    }

    public ObservableCollection<PredictionViewModel> Predictions { get; }

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

    public int SelectedTimeHorizon
    {
        get => _selectedTimeHorizon;
        set => this.RaiseAndSetIfChanged(ref _selectedTimeHorizon, value);
    }

    public ICommand GeneratePredictionsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExportPredictionsCommand { get; }

    private async Task LoadPredictionsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading predictions...";

            var userId = _sessionService.GetUser().Id;
            var predictions = await _predictiveService.GeneratePredictionsAsync(userId, SelectedTimeHorizon);

            Predictions.Clear();
            foreach (var prediction in predictions)
            {
                Predictions.Add(new PredictionViewModel
                {
                    Title = prediction.Title,
                    Description = prediction.Description,
                    Category = prediction.Category,
                    Confidence = prediction.Confidence,
                    OccurrenceDate = prediction.OccurrenceDate,
                    TimeHorizon = $"{prediction.TimeHorizonDays} days",
                    Type = prediction.Type.ToString(),
                    ConfidencePercentage = $"{(int)(prediction.Confidence * 100)}%",
                    ConfidenceColor = GetConfidenceColor(prediction.Confidence)
                });
            }

            StatusMessage = $"Loaded {Predictions.Count} predictions";
            _logger.LogInformation("Loaded {Count} predictions", Predictions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading predictions");
            StatusMessage = "Error loading predictions";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GeneratePredictionsAsync()
    {
        await LoadPredictionsAsync();
    }

    private async Task ExportPredictionsAsync()
    {
        // TODO: Implement PDF export
        _logger.LogInformation("Export predictions requested");
        StatusMessage = "Export feature coming soon";
        await Task.CompletedTask;
    }

    private string GetConfidenceColor(double confidence)
    {
        return confidence switch
        {
            >= 0.8 => "#10B981", // Green - High confidence
            >= 0.6 => "#FFB84D", // Gold - Medium confidence
            _ => "#EF4444"       // Red - Low confidence
        };
    }
}

public class PredictionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime? OccurrenceDate { get; set; }
    public string TimeHorizon { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConfidencePercentage { get; set; } = string.Empty;
    public string ConfidenceColor { get; set; } = string.Empty;
}
