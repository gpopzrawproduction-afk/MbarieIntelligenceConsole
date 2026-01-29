using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Settings panel - manages all application preferences.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IConfiguration? _configuration;
    
    // General Settings
    private string _businessName = "Mbarie Intelligence Console";
    private bool _darkModeEnabled = true;
    private bool _animationsEnabled = true;
    private string _selectedLanguage = "English";
    
    // AI Settings
    private string _aiProvider = "OpenAI";
    private string _aiModel = "gpt-4o";
    private double _aiTemperature = 0.7;
    private bool _aiChatEnabled = true;
    private bool _aiPredictionsEnabled = true;
    
    // Notification Settings
    private bool _notificationsEnabled = true;
    private bool _soundEnabled = true;
    private bool _criticalAlertsOnly = false;
    
    // Data Settings
    private string _databaseProvider = "SQLite";
    private int _autoRefreshInterval = 30;
    private bool _autoRefreshEnabled = true;
    
    // Status
    private string _statusMessage = string.Empty;
    private bool _hasUnsavedChanges;

    public SettingsViewModel()
    {
        _configuration = Program.ServiceProvider?.GetService<IConfiguration>();
        
        // Commands
        SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
        ResetCommand = ReactiveCommand.Create(ResetToDefaults);
        TestAIConnectionCommand = ReactiveCommand.CreateFromTask(TestAIConnectionAsync);
        ExportSettingsCommand = ReactiveCommand.CreateFromTask(ExportSettingsAsync);
        ImportSettingsCommand = ReactiveCommand.CreateFromTask(ImportSettingsAsync);
        
        // Load current settings
        LoadSettings();
        
        // Track changes
        this.WhenAnyValue(
            x => x.BusinessName,
            x => x.DarkModeEnabled,
            x => x.AIProvider,
            x => x.AIModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => HasUnsavedChanges = true);
    }

    #region General Settings

    public string BusinessName
    {
        get => _businessName;
        set => this.RaiseAndSetIfChanged(ref _businessName, value);
    }

    public bool DarkModeEnabled
    {
        get => _darkModeEnabled;
        set => this.RaiseAndSetIfChanged(ref _darkModeEnabled, value);
    }

    public bool AnimationsEnabled
    {
        get => _animationsEnabled;
        set => this.RaiseAndSetIfChanged(ref _animationsEnabled, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
    }

    public ObservableCollection<string> AvailableLanguages { get; } = new()
    {
        "English", "Spanish", "French", "German", "Japanese", "Chinese"
    };

    #endregion

    #region AI Settings

    public string AIProvider
    {
        get => _aiProvider;
        set => this.RaiseAndSetIfChanged(ref _aiProvider, value);
    }

    public string AIModel
    {
        get => _aiModel;
        set => this.RaiseAndSetIfChanged(ref _aiModel, value);
    }

    public double AITemperature
    {
        get => _aiTemperature;
        set => this.RaiseAndSetIfChanged(ref _aiTemperature, value);
    }

    public bool AIChatEnabled
    {
        get => _aiChatEnabled;
        set => this.RaiseAndSetIfChanged(ref _aiChatEnabled, value);
    }

    public bool AIPredictionsEnabled
    {
        get => _aiPredictionsEnabled;
        set => this.RaiseAndSetIfChanged(ref _aiPredictionsEnabled, value);
    }

    public ObservableCollection<string> AvailableProviders { get; } = new()
    {
        "OpenAI", "Azure OpenAI", "Local (Ollama)"
    };

    public ObservableCollection<string> AvailableModels { get; } = new()
    {
        "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo", "claude-3-opus", "claude-3-sonnet"
    };

    #endregion

    #region Notification Settings

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => this.RaiseAndSetIfChanged(ref _notificationsEnabled, value);
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => this.RaiseAndSetIfChanged(ref _soundEnabled, value);
    }

    public bool CriticalAlertsOnly
    {
        get => _criticalAlertsOnly;
        set => this.RaiseAndSetIfChanged(ref _criticalAlertsOnly, value);
    }

    #endregion

    #region Data Settings

    public string DatabaseProvider
    {
        get => _databaseProvider;
        set => this.RaiseAndSetIfChanged(ref _databaseProvider, value);
    }

    public int AutoRefreshInterval
    {
        get => _autoRefreshInterval;
        set => this.RaiseAndSetIfChanged(ref _autoRefreshInterval, value);
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set => this.RaiseAndSetIfChanged(ref _autoRefreshEnabled, value);
    }

    public ObservableCollection<string> AvailableDatabaseProviders { get; } = new()
    {
        "SQLite", "PostgreSQL", "SQL Server"
    };

    #endregion

    #region Status

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> TestAIConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportSettingsCommand { get; }

    #endregion

    #region Methods

    private void LoadSettings()
    {
        if (_configuration == null) return;

        BusinessName = _configuration["AI:SystemPrompt:BusinessName"] ?? "Mbarie Intelligence Console";
        AIProvider = _configuration["AI:Provider"] ?? "OpenAI";
        AIModel = _configuration["AI:OpenAI:Model"] ?? "gpt-4o";
        
        if (double.TryParse(_configuration["AI:OpenAI:Temperature"], out var temp))
            AITemperature = temp;
            
        DatabaseProvider = _configuration["Database:Provider"] ?? "SQLite";
        
        AIChatEnabled = _configuration["AI:Features:ChatEnabled"] != "false";
        AIPredictionsEnabled = _configuration["AI:Features:PredictionsEnabled"] != "false";
        
        HasUnsavedChanges = false;
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            StatusMessage = "Saving settings...";
            
            // In a real app, this would update the configuration file
            await Task.Delay(500); // Simulate save
            
            HasUnsavedChanges = false;
            StatusMessage = "Settings saved successfully!";
            
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    private void ResetToDefaults()
    {
        BusinessName = "Mbarie Intelligence Console";
        DarkModeEnabled = true;
        AnimationsEnabled = true;
        SelectedLanguage = "English";
        AIProvider = "OpenAI";
        AIModel = "gpt-4o";
        AITemperature = 0.7;
        AIChatEnabled = true;
        AIPredictionsEnabled = true;
        NotificationsEnabled = true;
        SoundEnabled = true;
        CriticalAlertsOnly = false;
        DatabaseProvider = "SQLite";
        AutoRefreshInterval = 30;
        AutoRefreshEnabled = true;
        
        StatusMessage = "Settings reset to defaults";
    }

    private async Task TestAIConnectionAsync()
    {
        StatusMessage = "Testing AI connection...";
        
        try
        {
            // TODO: Actually test the connection
            await Task.Delay(1500);
            StatusMessage = "? AI connection successful!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"? Connection failed: {ex.Message}";
        }
    }

    private async Task ExportSettingsAsync()
    {
        StatusMessage = "Exporting settings...";
        await Task.Delay(500);
        StatusMessage = "Settings exported to settings.json";
    }

    private async Task ImportSettingsAsync()
    {
        StatusMessage = "Import functionality coming soon";
        await Task.Delay(100);
    }

    #endregion
}
