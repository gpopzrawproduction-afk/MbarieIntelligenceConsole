using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        try
        {
            InitializeComponent();

            var viewModel = Program.ServiceProvider!
                .GetRequiredService<LoginViewModel>();

            viewModel.OnLoginSuccess += OnLoginSuccess;
            DataContext = viewModel;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ LoginWindow failed to initialize:");
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

        mainWindow.Show();
        Close();
    }
}
