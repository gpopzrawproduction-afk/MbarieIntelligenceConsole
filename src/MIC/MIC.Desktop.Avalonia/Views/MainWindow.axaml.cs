using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly KeyboardShortcutService _shortcuts;

    public MainWindow()
    {
        try
        {
            Console.WriteLine("=== MainWindow Constructor Started ===");
            InitializeComponent();
            Console.WriteLine("=== InitializeComponent Completed ===");
            _viewModel = new MainWindowViewModel();
            Console.WriteLine("=== MainWindowViewModel Created ===");
            DataContext = _viewModel;
            Console.WriteLine("=== DataContext Set ===");

            // Initialize keyboard shortcuts
            _shortcuts = KeyboardShortcutService.Instance;
            Console.WriteLine("=== KeyboardShortcutService Instance Retrieved ===");
            SetupKeyboardShortcuts();
            Console.WriteLine("=== SetupKeyboardShortcuts Completed ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== MainWindow Constructor Exception: {ex.Message} ===");
            Console.WriteLine($"=== Stack Trace: {ex.StackTrace} ===");
            throw;
        }
    }

    private void SetupKeyboardShortcuts()
    {
        // Command Palette
        _shortcuts.OnOpenCommandPalette += () => _viewModel.CommandPalette.Toggle();

        // Navigation
        _shortcuts.OnNavigate += viewName =>
        {
            switch (viewName)
            {
                case "Dashboard":
                    _viewModel.NavigateToDashboardCommand.Execute(null);
                    break;
                case "Alerts":
                    _viewModel.NavigateToAlertsCommand.Execute(null);
                    break;
                case "Metrics":
                    _viewModel.NavigateToMetricsCommand.Execute(null);
                    break;
                case "Predictions":
                    _viewModel.NavigateToPredictionsCommand.Execute(null);
                    break;
                case "AI Chat":
                    _viewModel.NavigateToAIChatCommand.Execute(null);
                    break;
                case "Settings":
                    _viewModel.NavigateToSettingsCommand.Execute(null);
                    break;
            }
        };

        // Actions
        _shortcuts.OnRefresh += () => NotificationService.Instance.ShowInfo("Refreshing data...");
        _shortcuts.OnSearch += () => _viewModel.CommandPalette.Toggle();
        _shortcuts.OnEscape += () =>
        {
            if (_viewModel.CommandPalette.IsOpen)
                _viewModel.CommandPalette.IsOpen = false;
        };

        // Wire up command palette navigation - use direct navigation instead of keyboard event
        _viewModel.CommandPalette.OnNavigate += HandleCommandPaletteNavigation;
        _viewModel.CommandPalette.OnAction += HandleAction;
    }
    
    private void HandleCommandPaletteNavigation(string viewName)
    {
        switch (viewName)
        {
            case "Dashboard":
                _viewModel.NavigateToDashboardCommand.Execute(null);
                break;
            case "Alerts":
                _viewModel.NavigateToAlertsCommand.Execute(null);
                break;
            case "Metrics":
                _viewModel.NavigateToMetricsCommand.Execute(null);
                break;
            case "Predictions":
                _viewModel.NavigateToPredictionsCommand.Execute(null);
                break;
            case "AI Chat":
                _viewModel.NavigateToAIChatCommand.Execute(null);
                break;
            case "Settings":
                _viewModel.NavigateToSettingsCommand.Execute(null);
                break;
        }
    }

    private async void HandleAction(string action)
    {
        switch (action)
        {
            case "Refresh":
                await RealTimeDataService.Instance.RefreshNowAsync();
                break;
            case "Export":
                // Generate and open HTML report
                NotificationService.Instance.ShowInfo("Generating report...");
                break;
            case "CreateAlert":
                await ShowCreateAlertDialogAsync();
                break;
            case "Search":
                _viewModel.CommandPalette.Toggle();
                break;
            case "About":
                ShowAboutWindow();
                break;
            case "ShowShortcuts":
                NotificationService.Instance.ShowInfo("Press Ctrl+K for Command Palette", "Keyboard Shortcuts");
                break;
            default:
                NotificationService.Instance.ShowInfo($"Action: {action}");
                break;
        }
    }
    
    private async Task ShowCreateAlertDialogAsync()
    {
        var dialog = new CreateAlertDialog();
        var result = await dialog.ShowDialog<bool?>(this);
        
        if (result == true)
        {
            // Refresh data after creating alert
            await RealTimeDataService.Instance.RefreshNowAsync();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Let the keyboard shortcut service handle it
        _shortcuts.HandleKeyDown(e);
    }

    private void ShowAboutWindow()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    /// <summary>
    /// Shows the About dialog.
    /// </summary>
    public void ShowAbout()
    {
        ShowAboutWindow();
    }
}