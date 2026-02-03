using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Interface for navigation service that handles view navigation within the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified view by name.
    /// </summary>
    /// <param name="viewName">Name of the view to navigate to (e.g., "Dashboard", "Email", "Alerts", "AI Chat").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NavigateToAsync(string viewName);
    
    /// <summary>
    /// Navigates to the specified view by name (synchronous version).
    /// </summary>
    /// <param name="viewName">Name of the view to navigate to.</param>
    void NavigateTo(string viewName);
}