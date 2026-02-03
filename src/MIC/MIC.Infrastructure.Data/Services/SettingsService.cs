using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Implementation of ISettingsService that stores settings in both 
/// local app data (for desktop) and database (for user-specific settings).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _appDataSettingsPath;
    private readonly MicDbContext _dbContext;
    private readonly ISessionService? _sessionService;
    private AppSettings _currentSettings;
    
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    
    public SettingsService(MicDbContext dbContext, ISessionService? sessionService = null)
    {
        _dbContext = dbContext;
        _sessionService = sessionService;
        
        // Determine app data path
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var micFolder = Path.Combine(appData, "MIC");
        Directory.CreateDirectory(micFolder);
        _appDataSettingsPath = Path.Combine(micFolder, "settings.json");
        
        _currentSettings = LoadSettings();
    }
    
    public AppSettings GetSettings() => _currentSettings;
    
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        
        // Update current settings
        _currentSettings = settings;
        
        // Save to app data (for desktop persistence)
        await SaveToAppDataAsync(settings);
        
        // Also save to database if user is logged in
        await SaveToDatabaseAsync(settings);
        
        // Notify subscribers
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(settings));
    }
    
    public async Task<AppSettings> LoadUserSettingsAsync(Guid userId)
    {
        // Try to load from database first
        var userSettings = await _dbContext.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId);
        
        if (userSettings != null && !string.IsNullOrEmpty(userSettings.SettingsJson))
        {
            try
            {
                return JsonSerializer.Deserialize<AppSettings>(userSettings.SettingsJson) 
                    ?? new AppSettings();
            }
            catch (JsonException)
            {
                // If JSON is corrupted, return default settings
                return new AppSettings();
            }
        }
        
        // Fall back to app data settings
        return LoadSettings();
    }
    
    public async Task SaveUserSettingsAsync(Guid userId, AppSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        
        await SaveToDatabaseAsync(userId, settings);
    }
    
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_appDataSettingsPath))
            {
                var json = File.ReadAllText(_appDataSettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to load settings from {_appDataSettingsPath}: {ex.Message}");
        }
        
        return new AppSettings();
    }
    
    private async Task SaveToAppDataAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_appDataSettingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save settings to {_appDataSettingsPath}: {ex.Message}");
        }
    }
    
    private async Task SaveToDatabaseAsync(AppSettings settings)
    {
        try
        {
            if (_sessionService == null || !_sessionService.IsAuthenticated)
            {
                return;
            }

            var user = _sessionService.GetUser();
            if (user.Id == Guid.Empty)
            {
                return;
            }

            await SaveToDatabaseAsync(user.Id, settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save settings to database: {ex.Message}");
        }
    }
    
    private async Task SaveToDatabaseAsync(Guid userId, AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            var userSettings = await _dbContext.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);
            
            if (userSettings == null)
            {
                userSettings = new MIC.Core.Domain.Entities.UserSettings(userId, json);
                _dbContext.UserSettings.Add(userSettings);
            }
            else
            {
                userSettings.UpdateSettings(json);
            }
            
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ Settings saved to database for user {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save settings to database for user {userId}: {ex.Message}");
        }
    }
}