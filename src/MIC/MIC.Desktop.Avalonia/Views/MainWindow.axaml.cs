using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
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
            // Enforce system decorations and resize for production polish
            SystemDecorations = SystemDecorations.Full;
            ExtendClientAreaToDecorationsHint = false;
            CanResize = true;
            MinWidth = 1200;
            MinHeight = 800;
            _viewModel = DataContext as MainWindowViewModel
                         ?? Program.ServiceProvider?.GetRequiredService<MainWindowViewModel>()
                         ?? new MainWindowViewModel();
            Console.WriteLine("=== MainWindowViewModel Resolved ===");
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

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
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
                    _viewModel.NavigateToDashboardCommand.Execute().Subscribe();
                    break;
                case "Alerts":
                    _viewModel.NavigateToAlertsCommand.Execute().Subscribe();
                    break;
                case "Metrics":
                    _viewModel.NavigateToMetricsCommand.Execute().Subscribe();
                    break;
                case "Predictions":
                    _viewModel.NavigateToPredictionsCommand.Execute().Subscribe();
                    break;
                case "AI Chat":
                    _viewModel.NavigateToChatCommand.Execute().Subscribe();
                    break;
                case "Settings":
                    _viewModel.NavigateToSettingsCommand.Execute().Subscribe();
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
                _viewModel.NavigateToDashboardCommand.Execute().Subscribe();
                break;
            case "Alerts":
                _viewModel.NavigateToAlertsCommand.Execute().Subscribe();
                break;
            case "Metrics":
                _viewModel.NavigateToMetricsCommand.Execute().Subscribe();
                break;
            case "Predictions":
                _viewModel.NavigateToPredictionsCommand.Execute().Subscribe();
                break;
            case "AI Chat":
                _viewModel.NavigateToChatCommand.Execute().Subscribe();
                break;
            case "Settings":
                _viewModel.NavigateToSettingsCommand.Execute().Subscribe();
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