using System;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Service for managing application settings and user preferences.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings GetSettings();
    
    /// <summary>
    /// Saves the application settings.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);
    
    /// <summary>
    /// Saves user-specific settings to the database.
    /// </summary>
    Task SaveUserSettingsAsync(Guid userId, AppSettings settings);
    
    /// <summary>
    /// Loads user-specific settings from the database.
    /// </summary>
    Task<AppSettings> LoadUserSettingsAsync(Guid userId);
    
    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Application settings container.
/// </summary>
public class AppSettings
{
    public AISettings AI { get; set; } = new();
    public EmailSyncSettings EmailSync { get; set; } = new();
    public UISettings UI { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public GeneralSettings General { get; set; } = new();
}

/// <summary>
/// AI service settings.
/// </summary>
public class AISettings
{
    public string Provider { get; set; } = "OpenAI";
    public string ModelId { get; set; } = "gpt-4-turbo-preview";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4000;
    public bool EnableEmailAnalysis { get; set; } = true;
    public bool EnableChatAssistant { get; set; } = true;
    public bool EnableAutoPrioritization { get; set; } = true;
    public bool EnableSentimentAnalysis { get; set; } = true;
    public bool EnableActionItemExtraction { get; set; } = true;
    public bool EnableEmailSummaries { get; set; } = true;
    public bool EnableContextAwareness { get; set; } = true;
}

/// <summary>
/// Email synchronization settings.
/// </summary>
public class EmailSyncSettings
{
    public int SyncIntervalMinutes { get; set; } = 5;
    public int InitialSyncMonths { get; set; } = 3;
    public int MaxEmailsPerSync { get; set; } = 100;
    public bool EnableAttachmentDownload { get; set; } = true;
    public string AttachmentDownloadPath { get; set; } = "./attachments";
    public bool AutoSyncEnabled { get; set; } = true;
}

/// <summary>
/// User interface settings.
/// </summary>
public class UISettings
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public int FontSize { get; set; } = 14;
    public bool CompactMode { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;
    public bool ShowUnreadCount { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
}

/// <summary>
/// Notification settings.
/// </summary>
public class NotificationSettings
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableDesktopNotifications { get; set; } = true;
    public bool EnableSound { get; set; } = true;
    public bool EnableAlertNotifications { get; set; } = true;
    public bool EnableMetricNotifications { get; set; } = false;
    public int NotificationDuration { get; set; } = 5000; // milliseconds
}

/// <summary>
/// General application settings.
/// </summary>
public class GeneralSettings
{
    public bool AutoStart { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool CheckForUpdates { get; set; } = true;
    public bool EnableTelemetry { get; set; } = false;
    public string DefaultAiProvider { get; set; } = "OpenAI";
    public int SessionTimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Event arguments for settings changed events.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public AppSettings NewSettings { get; }
    
    public SettingsChangedEventArgs(AppSettings newSettings)
    {
        NewSettings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));
    }
}