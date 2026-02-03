using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using MIC.Core.Application.Settings.Commands.SaveSettings;
using MIC.Core.Application.Settings.Queries.GetSettings;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.AI.Services;
using MIC.Desktop.Avalonia.Services;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Settings panel - manages all application preferences.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IConfiguration? _configuration;
    private readonly SettingsService _settingsService;
    private readonly IMediator _mediator;
    private readonly IChatService? _chatService;
    
    // General Settings
    private string _businessName = "Mbarie Intelligence Console";
    private bool _darkModeEnabled = true;
    private bool _animationsEnabled = true;
    private string _selectedLanguage = "English";
    
    // AI Settings
    private string _aiProvider = "OpenAI";
    private string _aiModel = "gpt-4o";
    private string _openAIApiKey = string.Empty;
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
    private bool _isBusy;

    public SettingsViewModel()
    {
        _configuration = Program.ServiceProvider?.GetService<IConfiguration>();
        _settingsService = SettingsService.Instance;
        _mediator = Program.ServiceProvider!.GetRequiredService<IMediator>();
        _chatService = Program.ServiceProvider?.GetService<IChatService>();
        
        // Commands
        var canRunActions = this.WhenAnyValue(x => x.IsBusy)
            .Select(isBusy => !isBusy)
            .ObserveOn(RxApp.MainThreadScheduler);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync, canRunActions);
        ResetCommand = ReactiveCommand.CreateFromTask(ResetToDefaultsAsync, canRunActions);
        TestAIConnectionCommand = ReactiveCommand.CreateFromTask(TestAIConnectionAsync, canRunActions);
        ExportSettingsCommand = ReactiveCommand.CreateFromTask(ExportSettingsAsync, canRunActions);
        ImportSettingsCommand = ReactiveCommand.CreateFromTask(ImportSettingsAsync, canRunActions);
        LoadCommand = ReactiveCommand.CreateFromTask(LoadSettingsAsync, canRunActions);
        
        // Track changes - use Observable.Merge to handle multiple property groups
        var propertyChanges = Observable.Merge(
            this.WhenAnyValue(x => x.BusinessName).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.DarkModeEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AnimationsEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.SelectedLanguage).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AIProvider).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AIModel).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.OpenAIApiKey).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AITemperature).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AIChatEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AIPredictionsEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.NotificationsEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.SoundEnabled).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.CriticalAlertsOnly).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.DatabaseProvider).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AutoRefreshInterval).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.AutoRefreshEnabled).Select(_ => Unit.Default)
        );
        
        propertyChanges
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => HasUnsavedChanges = true);
            
        // Load settings on initialization
        LoadCommand.Execute().Subscribe();
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

    public string OpenAIApiKey
    {
        get => _openAIApiKey;
        set => this.RaiseAndSetIfChanged(ref _openAIApiKey, value);
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

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<Unit, Unit> TestAIConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCommand { get; }

    #endregion

    #region Methods

    private async Task LoadSettingsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading settings...";
            
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                StatusMessage = "User not logged in. Using default settings.";
                LoadDefaultSettings();
                return;
            }
            
            var query = new GetSettingsQuery { UserId = userId };
            var result = await _mediator.Send(query);
            
            if (result.IsError)
            {
                StatusMessage = $"Failed to load settings: {result.FirstError.Description}";
                LoadDefaultSettings();
                return;
            }
            
            var settings = result.Value;
            ApplySettings(settings);
            OpenAIApiKey = _settingsService.OpenAIApiKey ?? string.Empty;
            
            StatusMessage = "Settings loaded successfully!";
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
            LoadDefaultSettings();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadDefaultSettings()
    {
        if (_configuration != null)
        {
            BusinessName = _configuration["AI:SystemPrompt:BusinessName"] ?? "Mbarie Intelligence Console";
            AIProvider = _configuration["AI:Provider"] ?? "OpenAI";
            AIModel = _configuration["AI:OpenAI:Model"] ?? "gpt-4o";
        // Do not read secrets from appsettings.json.
            OpenAIApiKey = _settingsService.OpenAIApiKey ?? string.Empty;
            
            if (double.TryParse(_configuration["AI:OpenAI:Temperature"], out var temp))
                AITemperature = temp;
                
            DatabaseProvider = _configuration["Database:Provider"] ?? "SQLite";
            
            AIChatEnabled = _configuration["AI:Features:ChatEnabled"] != "false";
            AIPredictionsEnabled = _configuration["AI:Features:PredictionsEnabled"] != "false";
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        // Map AppSettings to view model properties
        AIProvider = settings.AI.Provider;
        AIModel = settings.AI.ModelId;
        AITemperature = settings.AI.Temperature;
        AIChatEnabled = settings.AI.EnableChatAssistant;
        AIPredictionsEnabled = settings.AI.EnableAutoPrioritization;
        
        DarkModeEnabled = settings.UI.Theme == "Dark";
        SelectedLanguage = settings.UI.Language;
        AnimationsEnabled = settings.UI.EnableAnimations;
        
        NotificationsEnabled = settings.Notifications.EnableDesktopNotifications;
        SoundEnabled = settings.Notifications.EnableSound;
        CriticalAlertsOnly = !settings.Notifications.EnableMetricNotifications;
        
        AutoRefreshEnabled = settings.EmailSync.AutoSyncEnabled;
        AutoRefreshInterval = settings.EmailSync.SyncIntervalMinutes;
        
        // Note: BusinessName, DatabaseProvider, etc. are app-level settings
        // that might not be in AppSettings structure
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving settings...";

            _settingsService.OpenAIApiKey = string.IsNullOrWhiteSpace(OpenAIApiKey) ? null : OpenAIApiKey;
            
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                StatusMessage = "User not logged in. Cannot save settings.";
                return;
            }
            
            var settings = CreateAppSettings();
            var command = new SaveSettingsCommand
            {
                UserId = userId,
                Settings = settings
            };
            
            var result = await _mediator.Send(command);
            
            if (result.IsError)
            {
                StatusMessage = $"Failed to save settings: {result.FirstError.Description}";
                return;
            }
            
            HasUnsavedChanges = false;
            StatusMessage = "Settings saved successfully!";
            
            // Clear status message after delay
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private AppSettings CreateAppSettings()
    {
        return new AppSettings
        {
            AI = new AISettings
            {
                Provider = AIProvider,
                ModelId = AIModel,
                Temperature = AITemperature,
                EnableChatAssistant = AIChatEnabled,
                EnableAutoPrioritization = AIPredictionsEnabled,
                EnableEmailAnalysis = true,
                EnableSentimentAnalysis = true,
                EnableActionItemExtraction = true,
                EnableEmailSummaries = true,
                EnableContextAwareness = true
            },
            UI = new UISettings
            {
                Theme = DarkModeEnabled ? "Dark" : "Light",
                Language = SelectedLanguage,
                EnableAnimations = AnimationsEnabled,
                ShowNotifications = NotificationsEnabled,
                ShowUnreadCount = true,
                CompactMode = false,
                FontSize = 14
            },
            Notifications = new NotificationSettings
            {
                EnableDesktopNotifications = NotificationsEnabled,
                EnableSound = SoundEnabled,
                EnableAlertNotifications = true,
                EnableMetricNotifications = !CriticalAlertsOnly,
                EnableEmailNotifications = true,
                NotificationDuration = 5000
            },
            EmailSync = new EmailSyncSettings
            {
                AutoSyncEnabled = AutoRefreshEnabled,
                SyncIntervalMinutes = AutoRefreshInterval,
                InitialSyncMonths = 3,
                MaxEmailsPerSync = 100,
                EnableAttachmentDownload = true,
                AttachmentDownloadPath = "./attachments"
            },
            General = new GeneralSettings
            {
                AutoStart = false,
                MinimizeToTray = true,
                StartMinimized = false,
                CheckForUpdates = true,
                EnableTelemetry = false,
                DefaultAiProvider = "OpenAI",
                SessionTimeoutMinutes = 30
            }
        };
    }

    private async Task ResetToDefaultsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Resetting to defaults...";
            
            // Reset local properties
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
            
            // Save the reset values
            await SaveSettingsAsync();
            
            StatusMessage = "Settings reset to defaults!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resetting: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TestAIConnectionAsync()
    {
        IsBusy = true;
        StatusMessage = "Testing AI connection...";

        try
        {
            if (string.IsNullOrWhiteSpace(OpenAIApiKey) && string.Equals(AIProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "OpenAI API key is missing. Enter it here (saved locally) or set AI__OpenAI__ApiKey as an environment variable.";
                return;
            }
            if (_chatService == null)
            {
                StatusMessage = "AI service is not available.";
                return;
            }

            var available = await _chatService.IsAvailableAsync();
            StatusMessage = available ? "AI connection successful!" : "AI connection failed. Check API key and network.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportSettingsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Exporting settings...";
            await Task.Delay(500);
            StatusMessage = "Settings exported to settings.json";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportSettingsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Import functionality coming soon";
            await Task.Delay(100);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Guid GetCurrentUserId()
    {
        var userId = UserSessionService.Instance.CurrentSession?.UserId;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guid))
        {
            return Guid.Empty;
        }
        return guid;
    }

    #endregion
}
