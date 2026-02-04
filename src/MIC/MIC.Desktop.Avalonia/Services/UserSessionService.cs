using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// User session service for managing application user state.
/// For a desktop app, this provides local user preferences and identity.
/// </summary>
public class UserSessionService : ISessionService
{
    private static UserSessionService? _instance;
    public static UserSessionService Instance => _instance ??= new UserSessionService();

    private readonly string _sessionFilePath;
    private UserSession? _currentSession;
    private string? _token;

    public event Action<UserSession>? OnSessionChanged;
    public event Action? OnLogout;

    public UserSessionService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MIC");
        Directory.CreateDirectory(appDataPath);
        _sessionFilePath = Path.Combine(appDataPath, "session.json");
        
        LoadSession();
    }

    /// <summary>
    /// Convenience property for bindings in XAML to access the current user.
    /// Returns an empty UserDto when no session is present.
    /// </summary>
    public UserDto CurrentUser => GetUser();

    #region Properties

    public UserSession? CurrentSession => _currentSession;
    public bool IsLoggedIn => _currentSession != null;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_token);
    public string CurrentUserName => _currentSession?.DisplayName ?? "Unknown";
    public string CurrentUserInitials => GetInitials(_currentSession?.DisplayName ?? "?");
    public string CurrentUserEmail => _currentSession?.Email ?? string.Empty;
    public UserRole CurrentRole => _currentSession?.Role ?? UserRole.Guest;

    #endregion

    #region ISessionService Implementation

    /// <summary>
    /// Sets the authentication token for the current session.
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
    }

    /// <summary>
    /// Sets the current user information.
    /// </summary>
    public void SetUser(UserDto user)
    {
        if (_currentSession == null)
        {
            _currentSession = new UserSession
            {
                UserId = user.Id.ToString(),
                Username = user.Username,
                DisplayName = user.FullName ?? FormatDisplayName(user.Username),
                Email = user.Email,
                Position = user.JobPosition,
                Department = user.Department,
                Role = ConvertUserRole(user.Role),
                LoginTime = DateTime.Now,
                LastActivity = DateTime.Now,
                Preferences = new UserPreferences()
            };
        }
        else
        {
            _currentSession.UserId = user.Id.ToString();
            _currentSession.Username = user.Username;
            _currentSession.DisplayName = user.FullName ?? FormatDisplayName(user.Username);
            _currentSession.Email = user.Email;
            _currentSession.Position = user.JobPosition;
            _currentSession.Department = user.Department;
            _currentSession.Role = ConvertUserRole(user.Role);
            _currentSession.LastActivity = DateTime.Now;
        }
    }

    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    public string GetToken() => _token ?? string.Empty;

    /// <summary>
    /// Gets the current user information.
    /// </summary>
    public UserDto GetUser()
    {
        if (_currentSession == null)
        {
            return new UserDto();
        }

        return new UserDto
        {
            Id = Guid.TryParse(_currentSession.UserId, out var id) ? id : Guid.Empty,
            Username = _currentSession.Username,
            Email = _currentSession.Email,
            FullName = _currentSession.DisplayName,
            Role = ConvertUserRole(_currentSession.Role),
            IsActive = true,
            CreatedAt = DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now,
            JobPosition = _currentSession.Position,
            Department = _currentSession.Department,
            SeniorityLevel = null // Not stored in UserSession
        };
    }

    /// <summary>
    /// Clears the current session (logout).
    /// </summary>
    public void Clear()
    {
        _currentSession = null;
        _token = null;
        
        if (File.Exists(_sessionFilePath))
        {
            File.Delete(_sessionFilePath);
        }

        OnLogout?.Invoke();
    }

    #endregion

    #region Authentication

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        _currentSession = null;
        _token = null;
        
        if (File.Exists(_sessionFilePath))
        {
            File.Delete(_sessionFilePath);
        }

        OnLogout?.Invoke();
        NotificationService.Instance.ShowInfo("You have been logged out");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sets the current session from an authenticated user and JWT token.
    /// </summary>
    public void SetSession(string userId, string username, string email, string displayName, string? token, string? position = null, string? department = null)
    {
        _currentSession = new UserSession
        {
            UserId = userId,
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? FormatDisplayName(username) : displayName,
            Email = email,
            Position = position,
            Department = department,
            Role = DetermineRole(username),
            LoginTime = DateTime.Now,
            LastActivity = DateTime.Now,
            Preferences = new UserPreferences()
        };

        _token = string.IsNullOrWhiteSpace(token) ? null : token;
        OnSessionChanged?.Invoke(_currentSession);
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// </summary>
    public void UpdateActivity()
    {
        if (_currentSession != null)
        {
            _currentSession.LastActivity = DateTime.Now;
        }
    }

    #endregion

    #region Preferences

    /// <summary>
    /// Gets a user preference value.
    /// </summary>
    public T GetPreference<T>(string key, T defaultValue)
    {
        if (_currentSession?.Preferences?.Settings == null)
            return defaultValue;

        if (_currentSession.Preferences.Settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? defaultValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a user preference value.
    /// </summary>
    public async Task SetPreferenceAsync(string key, object value)
    {
        if (_currentSession == null) return;

        _currentSession.Preferences ??= new UserPreferences();
        _currentSession.Preferences.Settings ??= new Dictionary<string, object>();
        _currentSession.Preferences.Settings[key] = value;

        await SaveSessionAsync();
    }

    #endregion

    #region Permissions

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        if (_currentSession == null) return false;

        return _currentSession.Role switch
        {
            UserRole.Admin => true, // Admin has all permissions
            UserRole.User => permission is Permission.ViewDashboard or Permission.ViewAlerts 
                               or Permission.ViewMetrics or Permission.ViewPredictions 
                               or Permission.UseAI or Permission.ExportData,
            UserRole.Guest => permission is Permission.ViewDashboard or Permission.ViewAlerts 
                              or Permission.ViewMetrics,
            _ => false
        };
    }

    /// <summary>
    /// Checks if user can perform an action and shows error if not.
    /// </summary>
    public bool CanPerform(Permission permission, bool showError = true)
    {
        if (!IsLoggedIn)
        {
            if (showError) NotificationService.Instance.ShowWarning("Please log in to continue");
            return false;
        }

        if (!HasPermission(permission))
        {
            if (showError) NotificationService.Instance.ShowWarning("You don't have permission for this action");
            return false;
        }

        return true;
    }

    #endregion

    #region Private Methods

    private void LoadSession()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                var json = File.ReadAllText(_sessionFilePath);
                _currentSession = JsonSerializer.Deserialize<UserSession>(json);
                
                // Check if session is still valid (e.g., not older than 30 days)
                if (_currentSession != null && 
                    (DateTime.Now - _currentSession.LoginTime).TotalDays > 30)
                {
                    _currentSession = null;
                    File.Delete(_sessionFilePath);
                }
            }
        }
        catch
        {
            _currentSession = null;
        }
    }

    private async Task SaveSessionAsync()
    {
        if (_currentSession == null) return;

        var json = JsonSerializer.Serialize(_currentSession, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(_sessionFilePath, json);
    }

    private static string FormatDisplayName(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return "User";
        
        // Capitalize first letter of each word
        var parts = username.Split(new[] { ' ', '.', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Select(p => 
            char.ToUpper(p[0]) + (p.Length > 1 ? p[1..].ToLower() : "")));
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        
        return name.Length >= 2 
            ? $"{char.ToUpper(name[0])}{char.ToUpper(name[1])}" 
            : char.ToUpper(name[0]).ToString();
    }

    private static UserRole DetermineRole(string username)
    {
        // Default to least-privileged role unless set from authenticated user data.
        return UserRole.Guest;
    }

    private static UserRole ConvertUserRole(MIC.Core.Domain.Entities.UserRole domainRole)
    {
        return domainRole switch
        {
            MIC.Core.Domain.Entities.UserRole.Admin => UserRole.Admin,
            MIC.Core.Domain.Entities.UserRole.User => UserRole.User,
            MIC.Core.Domain.Entities.UserRole.Guest => UserRole.Guest,
            _ => UserRole.Guest
        };
    }

    private static MIC.Core.Domain.Entities.UserRole ConvertUserRole(UserRole localRole)
    {
        return localRole switch
        {
            UserRole.Admin => MIC.Core.Domain.Entities.UserRole.Admin,
            UserRole.User => MIC.Core.Domain.Entities.UserRole.User,
            UserRole.Guest => MIC.Core.Domain.Entities.UserRole.Guest,
            _ => MIC.Core.Domain.Entities.UserRole.Guest
        };
    }

    #endregion
}

#region Models

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Position { get; set; } // Added position field
    public string? Department { get; set; } // Added department field
    public UserRole Role { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
    public UserPreferences? Preferences { get; set; }
}

public class UserPreferences
{
    public bool DarkMode { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public int RefreshInterval { get; set; } = 30;
    public string DefaultView { get; set; } = "Dashboard";
    public Dictionary<string, object>? Settings { get; set; }
}

public enum UserRole
{
    Admin = 0,
    User = 1,
    Guest = 2
}

public enum Permission
{
    ViewDashboard,
    ViewAlerts,
    ViewMetrics,
    ViewPredictions,
    CreateAlerts,
    EditAlerts,
    DeleteAlerts,
    AcknowledgeAlerts,
    UseAI,
    ExportData,
    ManageUsers,
    SystemSettings
}

#endregion
