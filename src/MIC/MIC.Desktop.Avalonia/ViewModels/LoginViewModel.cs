using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using MIC.Core.Application.Authentication;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the login screen.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe = true;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        // Commands
        var canLogin = this.WhenAnyValue(
            x => x.Username, 
            x => x.Password,
            x => x.IsLoading,
            (user, pass, loading) => 
                !string.IsNullOrWhiteSpace(user) && 
                !string.IsNullOrWhiteSpace(pass) && 
                !loading);

        LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync, canLogin);
        ContinueAsGuestCommand = ReactiveCommand.CreateFromTask(ContinueAsGuestAsync);
    }

    #region Properties

    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => this.RaiseAndSetIfChanged(ref _rememberMe, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> ContinueAsGuestCommand { get; }

    /// <summary>
    /// Event raised when login is successful.
    /// </summary>
    public event Action? OnLoginSuccess;

    #endregion

    #region Methods

    private async Task LoginAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _authenticationService.LoginAsync(Username, Password).ConfigureAwait(false);

            if (result.Success && result.User is { } user && !string.IsNullOrWhiteSpace(result.Token))
            {
                UserSessionService.Instance.SetSession(
                    user.Id.ToString(),
                    user.Username,
                    user.Email,
                    user.FullName ?? user.Username,
                    result.Token);

                OnLoginSuccess?.Invoke();
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Invalid username or password."
                    : result.ErrorMessage;
                ErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ContinueAsGuestAsync()
    {
        try
        {
            IsLoading = true;
            
            // Login as guest
            await Services.UserSessionService.Instance.LoginAsync("Guest", "guest");
            OnLoginSuccess?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
