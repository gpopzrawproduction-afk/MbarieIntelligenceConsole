using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Service for managing user settings including theme preferences.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MbarieIntelligenceConsole",
        "settings.json");

    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private UserSettings _settings = new();

    private SettingsService()
    {
        LoadSettings();
    }

    /// <summary>
    /// Current user settings.
    /// </summary>
    public UserSettings Settings => _settings;

    /// <summary>
    /// Gets or sets the OpenAI API Key.
    /// </summary>
    public string? OpenAIApiKey { get; set; }

    /// <summary>
    /// Event raised when theme changes.
    /// </summary>
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Gets or sets the current theme.
    /// </summary>
    public AppTheme CurrentTheme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme != value)
            {
                var oldTheme = _settings.Theme;
                _settings.Theme = value;
                SaveSettings();
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, value));
            }
        }
    }

    /// <summary>
    /// Toggles between dark and light themes.
    /// </summary>
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
    }

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch
        {
            _settings = new UserSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail - settings are not critical
        }
    }
}

/// <summary>
/// User settings model.
/// </summary>
public class UserSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool ShowSplashScreen { get; set; } = true;
    public int AutoRefreshIntervalSeconds { get; set; } = 30;
    public bool EnableAnimations { get; set; } = true;
    public string LastViewedPage { get; set; } = "Dashboard";
}

/// <summary>
/// Application theme options.
/// </summary>
public enum AppTheme
{
    Dark,
    Light,
    System
}

/// <summary>
/// Event args for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public AppTheme OldTheme { get; }
    public AppTheme NewTheme { get; }

    public ThemeChangedEventArgs(AppTheme oldTheme, AppTheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }
}
