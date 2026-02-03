using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using MIC.Core.Application.Authentication;
using MIC.Desktop.Avalonia.Services;
using Serilog;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the login screen with registration support.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger _logger;

    // Login properties
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe = true;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    
    // Registration properties
    private string _registerUsername = string.Empty;
    private string _registerEmail = string.Empty;
    private string _registerPassword = string.Empty;
    private string _registerConfirmPassword = string.Empty;
    private string _registerFullName = string.Empty;
    private bool _showRegistration = false;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _logger = Log.ForContext<LoginViewModel>();

        // Login commands
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
        
        // Registration commands
        ShowRegisterCommand = ReactiveCommand.Create(ShowRegisterForm);
        BackToLoginCommand = ReactiveCommand.Create(ShowLoginForm);
        
        var canRegister = this.WhenAnyValue(
            x => x.RegisterUsername,
            x => x.RegisterEmail,
            x => x.RegisterPassword,
            x => x.RegisterConfirmPassword,
            x => x.IsLoading,
            (u, e, p, c, loading) => 
                !string.IsNullOrWhiteSpace(u) && 
                !string.IsNullOrWhiteSpace(e) && 
                !string.IsNullOrWhiteSpace(p) && 
                !string.IsNullOrWhiteSpace(c) &&
                !loading);
        
        RegisterCommand = ReactiveCommand.CreateFromTask(RegisterAsync, canRegister);
    }

    #region Properties

    // Login Properties
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

    // Registration Properties
    public string RegisterUsername
    {
        get => _registerUsername;
        set => this.RaiseAndSetIfChanged(ref _registerUsername, value);
    }

    public string RegisterEmail
    {
        get => _registerEmail;
        set => this.RaiseAndSetIfChanged(ref _registerEmail, value);
    }

    public string RegisterPassword
    {
        get => _registerPassword;
        set => this.RaiseAndSetIfChanged(ref _registerPassword, value);
    }

    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set => this.RaiseAndSetIfChanged(ref _registerConfirmPassword, value);
    }

    public string RegisterFullName
    {
        get => _registerFullName;
        set => this.RaiseAndSetIfChanged(ref _registerFullName, value);
    }

    public bool ShowRegistration
    {
        get => _showRegistration;
        set => this.RaiseAndSetIfChanged(ref _showRegistration, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }
    public ReactiveCommand<Unit, Unit> ContinueAsGuestCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowRegisterCommand { get; }
    public ReactiveCommand<Unit, Unit> BackToLoginCommand { get; }
    public ReactiveCommand<Unit, Unit> RegisterCommand { get; }

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

                _logger.Information("? Login successful for user: {Username}", Username);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Invalid username or password."
                    : result.ErrorMessage;
                ErrorMessage = message;
                _logger.Warning("?? Login failed for user: {Username} - {Error}", Username, message);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            _logger.Error(ex, "? Login error for user: {Username}", Username);
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
            
            // Set guest session directly
            UserSessionService.Instance.SetSession(
                "guest-id",
                "Guest",
                "guest@example.com",
                "Guest User",
                "guest-token");
            
            _logger.Information("? Guest login successful");
            OnLoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Guest login failed: {ex.Message}";
            _logger.Error(ex, "? Guest login error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Registration methods
    private void ShowRegisterForm()
    {
        ShowRegistration = true;
        ErrorMessage = string.Empty;
        _logger.Information("?? Showing registration form");
    }

    private void ShowLoginForm()
    {
        ShowRegistration = false;
        ErrorMessage = string.Empty;
        _logger.Information("?? Showing login form");
    }

    private async Task RegisterAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Validate fields
            if (string.IsNullOrWhiteSpace(RegisterUsername) ||
                string.IsNullOrWhiteSpace(RegisterEmail) ||
                string.IsNullOrWhiteSpace(RegisterPassword) ||
                string.IsNullOrWhiteSpace(RegisterConfirmPassword))
            {
                ErrorMessage = "All fields are required for registration";
                return;
            }

            if (RegisterPassword != RegisterConfirmPassword)
            {
                ErrorMessage = "Passwords do not match";
                return;
            }

            if (RegisterPassword.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters";
                return;
            }

            if (!RegisterEmail.Contains("@"))
            {
                ErrorMessage = "Please enter a valid email address";
                return;
            }

            _logger.Information("?? Registration attempt for username: {Username}, email: {Email}", RegisterUsername, RegisterEmail);

            // Register the user
            var result = await _authenticationService.RegisterAsync(
                RegisterUsername,
                RegisterEmail,
                RegisterPassword,
                RegisterFullName ?? RegisterUsername).ConfigureAwait(false);

            if (result.Success && result.User is { } user)
            {
                _logger.Information("? Registration successful for {Username}", RegisterUsername);

                // Show first-time setup dialog BEFORE auto-login
                await ShowFirstTimeSetupDialogAsync();

                // Auto-login after successful registration
                Username = RegisterUsername;
                Password = RegisterPassword;
                ShowRegistration = false;
                
                // Clear registration form
                RegisterUsername = string.Empty;
                RegisterEmail = string.Empty;
                RegisterPassword = string.Empty;
                RegisterConfirmPassword = string.Empty;
                RegisterFullName = string.Empty;

                // Perform login
                await LoginAsync();
            }
            else
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Registration failed. Please try again."
                    : result.ErrorMessage;
                ErrorMessage = message;
                _logger.Warning("?? Registration failed: {Error}", message);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Registration error: {ex.Message}";
            _logger.Error(ex, "? Registration error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowFirstTimeSetupDialogAsync()
    {
        try
        {
            _logger.Information("?? Showing first-time setup dialog");

            // Must run on UI thread
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var setupDialog = new Views.Dialogs.FirstTimeSetupDialog
                {
                    DataContext = new FirstTimeSetupDialogViewModel()
                };

                // Find the login window to use as parent
                var loginWindow = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.Windows.FirstOrDefault(w => w is Views.LoginWindow)
                    : null;

                if (loginWindow != null)
                {
                    var setupCompleted = await setupDialog.ShowDialog<bool>(loginWindow);
                    if (setupCompleted)
                    {
                        _logger.Information("? First-time setup completed - user configured preferences");
                    }
                    else
                    {
                        _logger.Information("??  First-time setup skipped - using default settings");
                    }
                }
                else
                {
                    _logger.Warning("?? Could not find login window to show first-time setup dialog");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "?? Failed to show first-time setup dialog - continuing with registration");
            // Don't block registration if dialog fails
        }
    }

    #endregion
}
