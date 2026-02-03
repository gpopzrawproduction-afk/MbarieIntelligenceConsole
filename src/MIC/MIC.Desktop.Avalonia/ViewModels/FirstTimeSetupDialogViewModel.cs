using System;
using System.Reactive;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Serilog;

namespace MIC.Desktop.Avalonia.ViewModels;

public class FirstTimeSetupDialogViewModel : ViewModelBase
{
    private readonly ILogger _logger;

    // Email Configuration
    private int _emailHistoryMonthsIndex = 2; // Default to 6 months
    public int EmailHistoryMonthsIndex
    {
        get => _emailHistoryMonthsIndex;
        set => this.RaiseAndSetIfChanged(ref _emailHistoryMonthsIndex, value);
    }

    private bool _downloadAttachments = true;
    public bool DownloadAttachments
    {
        get => _downloadAttachments;
        set => this.RaiseAndSetIfChanged(ref _downloadAttachments, value);
    }

    private bool _includeSentFolder = true;
    public bool IncludeSentFolder
    {
        get => _includeSentFolder;
        set => this.RaiseAndSetIfChanged(ref _includeSentFolder, value);
    }

    // AI Configuration
    private bool _enablePredictiveAnalytics = true;
    public bool EnablePredictiveAnalytics
    {
        get => _enablePredictiveAnalytics;
        set => this.RaiseAndSetIfChanged(ref _enablePredictiveAnalytics, value);
    }

    private bool _enableSentimentAnalysis = true;
    public bool EnableSentimentAnalysis
    {
        get => _enableSentimentAnalysis;
        set => this.RaiseAndSetIfChanged(ref _enableSentimentAnalysis, value);
    }

    private bool _enableAutoCategorization = true;
    public bool EnableAutoCategorization
    {
        get => _enableAutoCategorization;
        set => this.RaiseAndSetIfChanged(ref _enableAutoCategorization, value);
    }

    private int _predictionHorizonIndex = 1; // Default to 30 days
    public int PredictionHorizonIndex
    {
        get => _predictionHorizonIndex;
        set => this.RaiseAndSetIfChanged(ref _predictionHorizonIndex, value);
    }

    // Notification Preferences
    private bool _enableDesktopNotifications = true;
    public bool EnableDesktopNotifications
    {
        get => _enableDesktopNotifications;
        set => this.RaiseAndSetIfChanged(ref _enableDesktopNotifications, value);
    }

    private bool _enableEmailDigest = true;
    public bool EnableEmailDigest
    {
        get => _enableEmailDigest;
        set => this.RaiseAndSetIfChanged(ref _enableEmailDigest, value);
    }

    private bool _enableProactiveInsights = true;
    public bool EnableProactiveInsights
    {
        get => _enableProactiveInsights;
        set => this.RaiseAndSetIfChanged(ref _enableProactiveInsights, value);
    }

    public ICommand ContinueCommand { get; }
    public ICommand SkipCommand { get; }

    public FirstTimeSetupDialogViewModel()
    {
        _logger = Log.ForContext<FirstTimeSetupDialogViewModel>();

        ContinueCommand = ReactiveCommand.Create(OnContinue);
        SkipCommand = ReactiveCommand.Create(OnSkip);
    }

    private void OnContinue()
    {
        try
        {
            // Calculate months based on index
            int months = EmailHistoryMonthsIndex switch
            {
                0 => 1,
                1 => 3,
                2 => 6,
                3 => 12,
                4 => 120, // 10 years for "all history"
                _ => 6
            };

            int predictionDays = PredictionHorizonIndex switch
            {
                0 => 7,
                1 => 30,
                2 => 90,
                3 => 180,
                4 => 365,
                _ => 30
            };

            _logger.Information("First-time setup configured: Email history={Months} months, Predictions={Days} days ahead",
                months, predictionDays);

            // TODO: Save preferences with SettingsService when implemented

            // Close dialog with success
            CloseDialog(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving first-time setup preferences");
            CloseDialog(true);
        }
    }

    private void OnSkip()
    {
        _logger.Information("??  First-time setup skipped - using default settings");
        CloseDialog(false);
    }

    private void CloseDialog(bool result)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Find the FirstTimeSetupDialog window
            foreach (var window in desktop.Windows)
            {
                if (window is Views.Dialogs.FirstTimeSetupDialog setupDialog)
                {
                    setupDialog.Close(result);
                    return;
                }
            }
        }
    }
}
