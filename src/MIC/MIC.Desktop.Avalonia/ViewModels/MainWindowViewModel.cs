using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.Views;
using MIC.Desktop.Avalonia.Views.Dialogs;
using ReactiveUI;
using Serilog;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Main window view model handling navigation and application-level state.
/// </summary>
public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    private object? _currentView;
    private string _currentViewName = "Dashboard";
    private string _connectionStatus = "Connected";
    private string _lastUpdateTime = string.Empty;
    private bool _isConnected = true;
    private bool _isSidebarVisible = true;
    private bool _isFullscreen;

    public new event PropertyChangedEventHandler? PropertyChanged;

    // Theme switching
    public enum ThemeType { Light, Dark, System }
    public event EventHandler<ThemeType>? ThemeRequested;

    // ...existing code...
    // Resource-backed menu label properties for localization
    public string MenuFile => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuFile");
    public string MenuEdit => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuEdit");
    public string MenuView => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuView");
    public string MenuHelp => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuHelp");
    public string MenuTheme => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuTheme");
    public string MenuShortcuts => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuShortcuts");
    public string MenuOnboarding => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuOnboarding");
    public string MenuSearchHelp => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("MenuSearchHelp");

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        ISessionService sessionService,
        ILogger logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize command palette
        CommandPalette = serviceProvider.GetRequiredService<CommandPaletteViewModel>();

        // Initialize navigation commands
        var canExecute = this.WhenAnyValue(x => x.IsConnected).Select(_ => true);

        // Edit Menu Commands
        CutCommand = ReactiveCommand.Create(() => {
            _logger.Information("Cut command executed");
        }, canExecute);
        CopyCommand = ReactiveCommand.Create(() => {
            _logger.Information("Copy command executed");
        }, canExecute);
        PasteCommand = ReactiveCommand.Create(() => {
            _logger.Information("Paste command executed");
        }, canExecute);
        SelectAllCommand = ReactiveCommand.Create(() => {
            _logger.Information("Select All command executed");
        }, canExecute);
        FindCommand = ReactiveCommand.Create(() => {
            _logger.Information("Find command executed");
        }, canExecute);

        // View Menu Commands
        ToggleSidebarCommand = ReactiveCommand.Create(() => {
            IsSidebarExpanded = !IsSidebarExpanded;
            _logger.Information("Sidebar toggled: {State}", IsSidebarExpanded);
        }, canExecute);
        ToggleFullscreenCommand = ReactiveCommand.Create(() => {
            var mainWindow = GetMainWindow();
            mainWindow.WindowState = mainWindow.WindowState == WindowState.FullScreen
                ? WindowState.Normal
                : WindowState.FullScreen;
            _logger.Information("Fullscreen toggled");
        }, canExecute);

        // Required for non-nullable command properties
        OpenCommandPaletteCommand = ReactiveCommand.Create(() => CommandPalette.Toggle(), canExecute);
        UserMenuCommand = ReactiveCommand.Create(ShowUserMenu, canExecute);
        NotificationsCommand = ReactiveCommand.Create(ShowNotifications, canExecute);

        // Language switching commands
        SetLanguageEnglishCommand = ReactiveCommand.Create(() => {
            _logger.Information("Switching language to English");
            SetLanguage("en");
        }, canExecute);
        SetLanguageFrenchCommand = ReactiveCommand.Create(() => {
            _logger.Information("Switching language to French");
            SetLanguage("fr");
        }, canExecute);

        // Theme switching commands
        SetLightThemeCommand = ReactiveCommand.Create(() => {
            ThemeRequested?.Invoke(this, ThemeType.Light);
            _logger.Information("Theme set to Light");
        }, canExecute);
        SetDarkThemeCommand = ReactiveCommand.Create(() => {
            ThemeRequested?.Invoke(this, ThemeType.Dark);
            _logger.Information("Theme set to Dark");
        }, canExecute);
        SetSystemThemeCommand = ReactiveCommand.Create(() => {
            ThemeRequested?.Invoke(this, ThemeType.System);
            _logger.Information("Theme set to System");
        }, canExecute);

        // Quick Access Toolbar commands
        SaveCommand = ReactiveCommand.Create(() => _logger.Information("Save command executed"), canExecute);
        UploadCommand = ReactiveCommand.Create(() => _logger.Information("Upload command executed"), canExecute);
        SyncCommand = ReactiveCommand.Create(() => _logger.Information("Sync command executed"), canExecute);

        // Onboarding/help commands
        ShowOnboardingTourCommand = ReactiveCommand.CreateFromTask(ShowOnboardingTourAsync, canExecute);
        ShowSearchHelpCommand = ReactiveCommand.CreateFromTask(ShowSearchHelpAsync, canExecute);
        CustomizeShortcutsCommand = ReactiveCommand.CreateFromTask(ShowShortcutCustomizationAsync, canExecute);

        // ============================================
        // NAVIGATION COMMANDS (explicit, with logging)
        // ============================================
        NavigateToDashboardCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new DashboardView { DataContext = _serviceProvider.GetRequiredService<DashboardViewModel>() };
                CurrentViewName = "Dashboard";
                _logger.Information("Navigated to Dashboard");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Dashboard");
            }
        }, canExecute);

        NavigateToEmailCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new EmailInboxView { DataContext = _serviceProvider.GetRequiredService<EmailInboxViewModel>() };
                CurrentViewName = "Email";
                _logger.Information("Navigated to Email");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Email");
            }
        }, canExecute);

        NavigateToChatCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new ChatView { DataContext = _serviceProvider.GetRequiredService<ChatViewModel>() };
                CurrentViewName = "Chat";
                _logger.Information("Navigated to Chat");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Chat");
            }
        }, canExecute);

        NavigateToAlertsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new AlertListView { DataContext = _serviceProvider.GetRequiredService<AlertListViewModel>() };
                CurrentViewName = "Alerts";
                _logger.Information("Navigated to Alerts");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Alerts");
            }
        }, canExecute);

        NavigateToMetricsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new MetricsDashboardView { DataContext = _serviceProvider.GetRequiredService<MetricsDashboardViewModel>() };
                CurrentViewName = "Metrics";
                _logger.Information("Navigated to Metrics");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Metrics");
            }
        }, canExecute);

        NavigateToPredictionsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new PredictionsView { DataContext = _serviceProvider.GetRequiredService<PredictionsViewModel>() };
                CurrentViewName = "Predictions";
                _logger.Information("Navigated to Predictions");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Predictions");
            }
        }, canExecute);

        NavigateToSettingsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new SettingsView { DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>() };
                CurrentViewName = "Settings";
                _logger.Information("Navigated to Settings");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Settings");
            }
        }, canExecute);

        NavigateToKnowledgeBaseCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                CurrentView = new KnowledgeBaseView
                {
                    DataContext = new KnowledgeBaseViewModel(
                        _serviceProvider.GetRequiredService<IKnowledgeBaseService>(),
                        _sessionService,
                        _serviceProvider.GetRequiredService<IMediator>(),
                        _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KnowledgeBaseViewModel>>())
                };
                CurrentViewName = "Knowledge Base";
                _logger.Information("Navigated to Knowledge Base");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to navigate to Knowledge Base");
            }
        }, canExecute);
        NewChatCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                NavigateTo("AI Chat");
                if (CurrentView is ChatViewModel chat)
                {
                    await chat.ClearChatCommand.Execute();
                }
                _logger.Information("New chat started");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start new chat");
            }
        }, canExecute);

        NewEmailCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                NavigateTo("Email");
                if (CurrentView is EmailInboxViewModel email)
                {
                    email.ComposeCommand.Execute().Subscribe();
                }
                _logger.Information("New email initiated");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initiate new email");
            }
        }, canExecute);

        ExportMetricsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                NavigateTo("Metrics");
                if (CurrentView is MetricsDashboardViewModel metrics)
                {
                    metrics.ExportCommand.Execute().Subscribe();
                }
                _logger.Information("Metrics export initiated");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initiate metrics export");
            }
        }, canExecute);

        ExportPredictionsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                NavigateTo("Predictions");
                if (CurrentView is PredictionsViewModel predictions)
                {
                    predictions.ExportCommand.Execute().Subscribe();
                }
                _logger.Information("Predictions export initiated");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initiate predictions export");
            }
        }, canExecute);
        RefreshCommand = ReactiveCommand.Create(RefreshCurrentView, canExecute);

        ShowDocumentationCommand = ReactiveCommand.Create(OpenDocumentation, canExecute);
        ShowKeyboardShortcutsCommand = ReactiveCommand.CreateFromTask(ShowKeyboardShortcutsAsync, canExecute);
        CheckForUpdatesCommand = ReactiveCommand.Create(CheckForUpdates, canExecute);
        ShowAboutCommand = ReactiveCommand.CreateFromTask(ShowAboutDialogAsync, canExecute);
        ExitCommand = ReactiveCommand.Create(ExitApplication, canExecute);
        LogoutCommand = ReactiveCommand.Create(Logout, canExecute);

        // Initialize with dashboard view
        NavigateTo("Dashboard");
        
        // Update time
        LastUpdateTime = DateTime.Now.ToString("HH:mm");
        
        // Load notifications
        LoadNotifications();

        if (_sessionService is UserSessionService userSessionService)
        {
            userSessionService.OnSessionChanged += _ => RaiseSessionChanged();
            userSessionService.OnLogout += RaiseSessionChanged;
        }
    }

    public MainWindowViewModel()
        : this(
            Program.ServiceProvider ?? throw new InvalidOperationException("Service provider is not configured."),
            (Program.ServiceProvider ?? throw new InvalidOperationException("Service provider is not configured."))
                .GetRequiredService<ISessionService>(),
            Log.Logger)
    {
        // Also assign required commands in default constructor for CS8618
        var canExecute = this.WhenAnyValue(x => x.IsConnected).Select(_ => true);
        OpenCommandPaletteCommand = ReactiveCommand.Create(() => CommandPalette.Toggle(), canExecute);
        UserMenuCommand = ReactiveCommand.Create(ShowUserMenu, canExecute);
        NotificationsCommand = ReactiveCommand.Create(ShowNotifications, canExecute);
    }

    private void SetLanguage(string culture)
    {
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(culture);
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            NotificationService.Instance.ShowInfo($"Language set to {culture}");
            // TODO: Reload UI/resources for new language
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to set language: {culture}");
        }
    }

    #region Properties

    private bool _isSidebarExpanded = true;
    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set => SetProperty(ref _isSidebarExpanded, value);
    }

    public string Greeting => "Mbarie Intelligence Console";
    public string AppTitle => MIC.Desktop.Avalonia.Resources.ResourceHelper.GetString("AppTitle");
    
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

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => SetProperty(ref _isSidebarVisible, value);
    }

    public double SidebarWidth => IsSidebarVisible ? 260 : 0;

    public string LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    // User session properties
    public string UserName => ResolveUserName();
    public string UserInitials => ResolveUserInitials();
    public string UserRole => ResolveUserRole();

    public bool IsDashboardActive => CurrentViewName == "Dashboard";
    public bool IsAlertsActive => CurrentViewName == "Alerts";
    public bool IsMetricsActive => CurrentViewName == "Metrics";
    public bool IsPredictionsActive => CurrentViewName == "Predictions";
    public bool IsKnowledgeBaseActive => CurrentViewName == "Knowledge Base";
    public bool IsAIChatActive => CurrentViewName == "AI Chat";
    public bool IsSettingsActive => CurrentViewName == "Settings";
    public bool IsEmailActive => CurrentViewName == "Email";

    public ObservableCollection<NotificationItem> Notifications { get; } = new();
    public int UnreadNotificationCount => Notifications.Count;

    #endregion

    #region Commands

    // Edit Menu Commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CutCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CopyCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> PasteCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SelectAllCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> FindCommand { get; private set; }
    // View Menu Commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleSidebarCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleFullscreenCommand { get; private set; }

    // Theme switching commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetLightThemeCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetDarkThemeCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetSystemThemeCommand { get; }

    // Quick Access Toolbar commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SaveCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UploadCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SyncCommand { get; }

    // Onboarding/help and shortcut customization commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowOnboardingTourCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowSearchHelpCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CustomizeShortcutsCommand { get; }

    // Language switching commands
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetLanguageEnglishCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetLanguageFrenchCommand { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToDashboardCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToEmailCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToChatCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToAlertsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToMetricsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToPredictionsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToSettingsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NavigateToKnowledgeBaseCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenCommandPaletteCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UserMenuCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NotificationsCommand { get; private set; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExitCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LogoutCommand { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NewChatCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> NewEmailCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExportMetricsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExportPredictionsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowDocumentationCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowKeyboardShortcutsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowAboutCommand { get; }

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

    public void NavigateTo(string viewName)
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
        OnPropertyChanged(nameof(IsKnowledgeBaseActive));
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
            "Knowledge Base" => CreateKnowledgeBaseView(),
            "AI Chat" => CreateChatViewModel(),
            "Settings" => CreateSettingsViewModel(),
            "Email" => CreateEmailInboxViewModel(),
            _ => null
        };
    }

    private AlertListViewModel? CreateAlertListViewModel()
    {
        return _serviceProvider.GetService<AlertListViewModel>();
    }

    private MetricsDashboardViewModel? CreateMetricsDashboardViewModel()
    {
        return _serviceProvider.GetService<MetricsDashboardViewModel>();
    }

    private PredictionsViewModel CreatePredictionsViewModel()
    {
        return _serviceProvider.GetService<PredictionsViewModel>() 
               ?? throw new InvalidOperationException("PredictionsViewModel is not registered in the service container.");
    }

    private ChatViewModel CreateChatViewModel()
    {
        return _serviceProvider.GetService<ChatViewModel>() 
               ?? throw new InvalidOperationException("ChatViewModel is not registered in the service container.");
    }

    private SettingsViewModel CreateSettingsViewModel()
    {
        return _serviceProvider.GetService<SettingsViewModel>() 
               ?? throw new InvalidOperationException("SettingsViewModel is not registered in the service container.");
    }

    private EmailInboxViewModel CreateEmailInboxViewModel()
    {
        return _serviceProvider.GetService<EmailInboxViewModel>() 
               ?? throw new InvalidOperationException("EmailInboxViewModel is not registered in the service container.");
    }

    private DashboardViewModel CreateDashboardViewModel()
    {
        return _serviceProvider.GetService<DashboardViewModel>() ?? throw new InvalidOperationException("DashboardViewModel is not registered in the service container.");
    }

    #endregion

    #region Methods

    private void ShowUserMenu()
    {
        Console.WriteLine("User menu clicked - implement flyout");
    }

    private void ShowNotifications()
    {
        Console.WriteLine("Notifications clicked - implement panel");
    }

    private void LoadNotifications()
    {
        Notifications.Clear();
        OnPropertyChanged(nameof(UnreadNotificationCount));
    }

    private Task NewChatAsync()
    {
        NavigateTo("AI Chat");
        if (CurrentView is ChatViewModel chat)
        {
            chat.ClearChatCommand.Execute().Subscribe();
        }

        return Task.CompletedTask;
    }

    private void NewEmail()
    {
        NavigateTo("Email");
        if (CurrentView is EmailInboxViewModel email)
        {
            email.ComposeCommand.Execute().Subscribe();
        }
    }

    private void ExportMetrics()
    {
        NavigateTo("Metrics");
        if (CurrentView is MetricsDashboardViewModel metrics)
        {
            metrics.ExportCommand.Execute().Subscribe();
        }
    }

    private void ExportPredictions()
    {
        NavigateTo("Predictions");
        if (CurrentView is PredictionsViewModel predictions)
        {
            predictions.ExportCommand.Execute().Subscribe();
        }
    }

    private void RefreshCurrentView()
    {
        switch (CurrentView)
        {
            case DashboardViewModel dashboard:
                dashboard.RefreshCommand.Execute().Subscribe();
                break;
            case AlertListViewModel alerts:
                alerts.RefreshCommand.Execute().Subscribe();
                break;
            case MetricsDashboardViewModel metrics:
                metrics.RefreshCommand.Execute().Subscribe();
                break;
            case PredictionsViewModel predictions:
                predictions.RefreshCommand.Execute().Subscribe();
                break;
            case EmailInboxViewModel email:
                email.RefreshCommand.Execute().Subscribe();
                break;
            case ChatViewModel chat:
                chat.ClearChatCommand.Execute().Subscribe();
                break;
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
        OnPropertyChanged(nameof(SidebarWidth));
    }

    private void ToggleFullscreen()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window == null) return;

            _isFullscreen = !_isFullscreen;
            window.WindowState = _isFullscreen ? WindowState.FullScreen : WindowState.Normal;
        }
    }

    private void OpenDocumentation()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://mbarieservicesltd.com",
                UseShellExecute = true
            });
        }
        catch
        {
            NotificationService.Instance.ShowError("Unable to open documentation.");
        }
    }

    private async Task ShowKeyboardShortcutsAsync()
    {
        try
        {
            var window = GetMainWindow();
            var dialog = new KeyboardShortcutsDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show keyboard shortcuts dialog");
            NotificationService.Instance.ShowError("Failed to open keyboard shortcuts.");
        }
    }

    private void CheckForUpdates()
    {
        NotificationService.Instance.ShowInfo("Checking for updates...");
    }

    private async Task ShowAboutDialogAsync()
    {
        try
        {
            var window = GetMainWindow();
            var dialog = new AboutDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show about dialog");
            NotificationService.Instance.ShowError("Failed to open about dialog.");
        }
    }

    private async Task ShowOnboardingTourAsync()
    {
        try
        {
            var window = GetMainWindow();
            var dialog = new OnboardingTourDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show onboarding tour");
            NotificationService.Instance.ShowError("Failed to open onboarding tour.");
        }
    }

    private async Task ShowSearchHelpAsync()
    {
        try
        {
            var window = GetMainWindow();
            var dialog = new SearchHelpDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show search help");
            NotificationService.Instance.ShowError("Failed to open search help.");
        }
    }

    private async Task ShowShortcutCustomizationAsync()
    {
        try
        {
            var window = GetMainWindow();
            var dialog = new ShortcutCustomizationDialog();
            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to show shortcut customization");
            NotificationService.Instance.ShowError("Failed to open shortcut customization.");
        }
    }

    private void CutFocusedText()
    {
        if (GetFocusedTextBox() is { } textBox)
        {
            textBox.Cut();
        }
    }

    private void CopyFocusedText()
    {
        if (GetFocusedTextBox() is { } textBox)
        {
            textBox.Copy();
        }
    }

    private void PasteFocusedText()
    {
        if (GetFocusedTextBox() is { } textBox)
        {
            textBox.Paste();
        }
    }

    private void SelectAllFocusedText()
    {
        if (GetFocusedTextBox() is { } textBox)
        {
            textBox.SelectAll();
        }
    }

    private void OpenFind()
    {
        CommandPalette.Toggle();
    }

    private static TextBox? GetFocusedTextBox()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        var focused = desktop.MainWindow?.FocusManager?.GetFocusedElement();
        return focused as TextBox;
    }

    private void Logout()
    {
        _sessionService.Clear();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var current = desktop.MainWindow;
            var loginWindow = new LoginWindow();
            desktop.MainWindow = loginWindow;
            loginWindow.Show();
            current?.Close();
        }
    }

    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private KnowledgeBaseView CreateKnowledgeBaseView()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Service provider is not configured.");
        var knowledgeBaseService = serviceProvider.GetRequiredService<IKnowledgeBaseService>();

        var viewModel = new KnowledgeBaseViewModel(
            knowledgeBaseService,
            _sessionService,
            serviceProvider.GetRequiredService<IMediator>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KnowledgeBaseViewModel>>());

        return new KnowledgeBaseView
        {
            DataContext = viewModel
        };
    }

    private Window GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            return desktop.MainWindow;
        }

        throw new InvalidOperationException("Unable to locate the main window.");
    }

    private void RaiseSessionChanged()
    {
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(UserInitials));
        OnPropertyChanged(nameof(UserRole));
    }

    private string ResolveUserName()
    {
        if (!_sessionService.IsAuthenticated)
        {
            return "Signed out";
        }

        var user = _sessionService.GetUser();
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName;
        }

        return !string.IsNullOrWhiteSpace(user.Username) ? user.Username : "Unknown";
    }

    private string ResolveUserInitials()
    {
        var name = ResolveUserName();
        if (string.IsNullOrWhiteSpace(name)) return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        }

        return name.Length >= 2
            ? $"{char.ToUpper(name[0])}{char.ToUpper(name[1])}"
            : char.ToUpper(name[0]).ToString();
    }

    private string ResolveUserRole()
    {
        if (!_sessionService.IsAuthenticated)
        {
            return "Viewer";
        }

        var user = _sessionService.GetUser();
        return user.Role.ToString();
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








