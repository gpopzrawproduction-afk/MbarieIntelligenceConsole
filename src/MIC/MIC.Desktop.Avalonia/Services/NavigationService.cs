using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.ViewModels;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Implementation of INavigationService that handles view navigation within the desktop application.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for accessing view models.</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public Task NavigateToAsync(string viewName)
    {
        NavigateTo(viewName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void NavigateTo(string viewName)
    {
        // Get the MainWindowViewModel from the service provider
        var mainViewModel = _serviceProvider.GetService<MainWindowViewModel>();
        
        if (mainViewModel == null)
        {
            Console.WriteLine($"NavigationService: MainWindowViewModel not found in DI container. Cannot navigate to '{viewName}'.");
            return;
        }

        // Use the MainWindowViewModel's navigation logic
        // MainWindowViewModel has a NavigateTo method that handles view switching
        mainViewModel.NavigateTo(viewName);
        
        Console.WriteLine($"NavigationService: Navigated to '{viewName}'.");
    }
}