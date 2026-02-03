using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class LoginWindow : Window
{
    private void MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindow(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.Maximized;
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    public LoginWindow()
    {
        try
        {
            InitializeComponent();
            SystemDecorations = SystemDecorations.Full;
            ExtendClientAreaToDecorationsHint = false;
            CanResize = true;
            MinWidth = 400;
            MinHeight = 500;

            var viewModel = Program.ServiceProvider!
                .GetRequiredService<LoginViewModel>();

            viewModel.OnLoginSuccess += OnLoginSuccess;
            DataContext = viewModel;
        }
        catch (Exception ex)
        {
            Console.WriteLine("\u274c LoginWindow failed to initialize:");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Exiting application...");
            throw;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoginSuccess()
    {
        var mainWindow = new MainWindow
        {
            DataContext = Program.ServiceProvider!
                .GetRequiredService<MainWindowViewModel>()
        };
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;
        }

        mainWindow.Show();
        Close();
    }

    private void OnDragWindow(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
