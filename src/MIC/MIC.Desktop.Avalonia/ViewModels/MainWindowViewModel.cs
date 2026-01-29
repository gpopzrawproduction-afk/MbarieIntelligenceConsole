using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Main window view model handling navigation and application-level state.
/// </summary>
public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private object? _currentView;
    private string _currentViewName = "Dashboard";
    private string _connectionStatus = "Connected";
    private string _lastUpdateTime = string.Empty;
    private bool _isConnected = true;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
    {
        // Initialize command palette
        CommandPalette = new CommandPaletteViewModel();
        
        // Initialize navigation commands
        NavigateToDashboardCommand = new SimpleCommand(() => NavigateTo("Dashboard"));
        NavigateToAlertsCommand = new SimpleCommand(() => NavigateTo("Alerts"));
        NavigateToMetricsCommand = new SimpleCommand(() => NavigateTo("Metrics"));
        NavigateToPredictionsCommand = new SimpleCommand(() => NavigateTo("Predictions"));
        NavigateToAIChatCommand = new SimpleCommand(() => NavigateTo("AI Chat"));
        NavigateToSettingsCommand = new SimpleCommand(() => NavigateTo("Settings"));
        NavigateToEmailCommand = new SimpleCommand(() => NavigateTo("Email"));

        OpenCommandPaletteCommand = new SimpleCommand(() => CommandPalette.Toggle());
        UserMenuCommand = new SimpleCommand(ShowUserMenu);
        NotificationsCommand = new SimpleCommand(ShowNotifications);

        // Initialize with dashboard view
        NavigateTo("Dashboard");
        
        // Update time
        LastUpdateTime = DateTime.Now.ToString("HH:mm");
        
        // Load notifications
        LoadNotifications();
    }

    #region Properties

    public string Greeting => "Mbarie Intelligence Console";
    
    /// <summary>
    /// Command Palette for quick command access (Ctrl+K).
    /// </summary>
    public CommandPaletteViewModel CommandPalette { get; }

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string CurrentViewName
    {
        get => _currentViewName;
        set => SetProperty(ref _currentViewName, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public string LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    // User session properties
    public string UserName => MIC.Desktop.Avalonia.Services.UserSessionService.Instance.CurrentUserName;
    public string UserInitials => MIC.Desktop.Avalonia.Services.UserSessionService.Instance.CurrentUserInitials;
    public string UserRole => MIC.Desktop.Avalonia.Services.UserSessionService.Instance.CurrentRole.ToString();

    public bool IsDashboardActive => CurrentViewName == "Dashboard";
    public bool IsAlertsActive => CurrentViewName == "Alerts";
    public bool IsMetricsActive => CurrentViewName == "Metrics";
    public bool IsPredictionsActive => CurrentViewName == "Predictions";
    public bool IsAIChatActive => CurrentViewName == "AI Chat";
    public bool IsSettingsActive => CurrentViewName == "Settings";
    public bool IsEmailActive => CurrentViewName == "Email";

    public ObservableCollection<NotificationItem> Notifications { get; } = new();
    public int UnreadNotificationCount => Notifications.Count;

    #endregion

    #region Commands

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToAlertsCommand { get; }
    public ICommand NavigateToMetricsCommand { get; }
    public ICommand NavigateToPredictionsCommand { get; }
    public ICommand NavigateToAIChatCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToEmailCommand { get; }
    public ICommand OpenCommandPaletteCommand { get; }
    public ICommand UserMenuCommand { get; }
    public ICommand NotificationsCommand { get; }

    #endregion

    /// <summary>
    /// Simple ICommand implementation used for UI actions in MainWindowViewModel.
    /// </summary>
    internal sealed class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public SimpleCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #region Navigation

    private void NavigateTo(string viewName)
    {
        CurrentViewName = viewName;
        LastUpdateTime = DateTime.Now.ToString("HH:mm");
        
        // Show toast notification for navigation
        NotificationService.Instance.ShowInfo($"Navigated to {viewName}");

        
        // Notify active state changes
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsAlertsActive));
        OnPropertyChanged(nameof(IsMetricsActive));
        OnPropertyChanged(nameof(IsPredictionsActive));
        OnPropertyChanged(nameof(IsAIChatActive));
        OnPropertyChanged(nameof(IsSettingsActive));
        OnPropertyChanged(nameof(IsEmailActive));

        // Set the current view based on navigation
        CurrentView = viewName switch
        {
            "Dashboard" => CreateDashboardViewModel(), // Need to create DashboardViewModel instance
            "Alerts" => CreateAlertListViewModel(),
            "Metrics" => CreateMetricsDashboardViewModel(),
            "Predictions" => CreatePredictionsViewModel(),
            "AI Chat" => CreateChatViewModel(),
            "Settings" => CreateSettingsViewModel(),
            "Email" => CreateEmailInboxViewModel(),
            _ => null
        };
    }

    private AlertListViewModel? CreateAlertListViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        if (serviceProvider is null) return null;
        
        return serviceProvider.GetService<AlertListViewModel>();
    }

    private MetricsDashboardViewModel? CreateMetricsDashboardViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        if (serviceProvider is null) return null;
        
        return serviceProvider.GetService<MetricsDashboardViewModel>();
    }

    private PredictionsViewModel CreatePredictionsViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        return serviceProvider?.GetService<PredictionsViewModel>() 
               ?? throw new InvalidOperationException("PredictionsViewModel is not registered in the service container.");
    }

    private ChatViewModel CreateChatViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        return serviceProvider?.GetService<ChatViewModel>() 
               ?? throw new InvalidOperationException("ChatViewModel is not registered in the service container.");
    }

    private SettingsViewModel CreateSettingsViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        return serviceProvider?.GetService<SettingsViewModel>() 
               ?? throw new InvalidOperationException("SettingsViewModel is not registered in the service container.");
    }

    private EmailInboxViewModel CreateEmailInboxViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        return serviceProvider?.GetService<EmailInboxViewModel>() 
               ?? throw new InvalidOperationException("EmailInboxViewModel is not registered in the service container.");
    }

    private DashboardViewModel CreateDashboardViewModel()
    {
        var serviceProvider = Program.ServiceProvider;
        return serviceProvider?.GetService<DashboardViewModel>() ?? throw new InvalidOperationException("DashboardViewModel is not registered in the service container.");
    }

    #endregion

    #region Methods

    private void ShowUserMenu()
    {
        // TODO: Show user menu flyout with options: Profile, Preferences, Logout
        Console.WriteLine("User menu clicked - implement flyout");
    }

    private void ShowNotifications()
    {
        // TODO: Show notifications panel/flyout
        Console.WriteLine("Notifications clicked - implement panel");
    }

    private void LoadNotifications()
    {
        Notifications.Clear();
        Notifications.Add(new NotificationItem
        {
            Title = "System Update Available",
            Message = "Version 2.1.0 is ready to install",
            TimeAgo = "5 min ago",
            Type = "Info"
        });
        Notifications.Add(new NotificationItem
        {
            Title = "New Alert Detected",
            Message = "High priority alert from Server-01",
            TimeAgo = "12 min ago",
            Type = "Warning"
        });
        OnPropertyChanged(nameof(UnreadNotificationCount));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

#region Supporting Models

public class NotificationItem
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
}

#endregion

