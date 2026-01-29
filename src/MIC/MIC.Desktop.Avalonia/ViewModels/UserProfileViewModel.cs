using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the user profile panel.
/// </summary>
public class UserProfileViewModel : ViewModelBase
{
    private readonly UserSessionService _session;

    public UserProfileViewModel()
    {
        _session = UserSessionService.Instance;
        
        // Commands
        ViewProfileCommand = ReactiveCommand.Create(() => 
            NotificationService.Instance.ShowInfo("Profile view coming soon"));
        PreferencesCommand = ReactiveCommand.Create(() => OnNavigateToSettings?.Invoke());
        KeyboardShortcutsCommand = ReactiveCommand.Create(ShowKeyboardShortcuts);
        HelpCommand = ReactiveCommand.Create(() => 
            NotificationService.Instance.ShowInfo("Help documentation coming soon"));
        LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync);
    }

    #region Properties

    public string DisplayName => _session.CurrentUserName;
    public string Email => _session.CurrentUserEmail;
    public string UserInitials => _session.CurrentUserInitials;
    public string RoleDisplay => _session.CurrentRole.ToString();
    
    public string SessionInfo
    {
        get
        {
            var session = _session.CurrentSession;
            if (session == null) return "Not signed in";
            
            var duration = DateTime.Now - session.LoginTime;
            return $"Signed in {FormatDuration(duration)} ago";
        }
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> ViewProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> PreferencesCommand { get; }
    public ReactiveCommand<Unit, Unit> KeyboardShortcutsCommand { get; }
    public ReactiveCommand<Unit, Unit> HelpCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    public event Action? OnNavigateToSettings;
    public event Action? OnLogout;

    #endregion

    #region Methods

    private void ShowKeyboardShortcuts()
    {
        var shortcuts = KeyboardShortcutService.Instance.GetShortcutList();
        var message = string.Join("\n", shortcuts.Select(s => $"{s.Shortcut}: {s.Description}"));
        NotificationService.Instance.ShowInfo("Press Ctrl+K to open Command Palette", "Keyboard Shortcuts");
    }

    private async Task LogoutAsync()
    {
        await _session.LogoutAsync();
        OnLogout?.Invoke();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1) return "just now";
        if (duration.TotalMinutes < 60) return $"{(int)duration.TotalMinutes} minutes";
        if (duration.TotalHours < 24) return $"{(int)duration.TotalHours} hours";
        return $"{(int)duration.TotalDays} days";
    }

    #endregion
}
