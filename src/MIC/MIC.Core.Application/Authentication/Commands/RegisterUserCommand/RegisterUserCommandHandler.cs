using System.Text.RegularExpressions;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Authentication.Commands.RegisterUserCommand;

/// <summary>
/// Handler for registering a new user.
/// </summary>
public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, AuthenticationResult>
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IAuthenticationService authService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<AuthenticationResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            var validationError = ValidateRequest(request);
            if (validationError is not null)
            {
                return validationError.Value;
            }

            // Call authentication service
            var result = await _authService.RegisterAsync(
                request.Username,
                request.Email,
                request.Password,
                request.FullName ?? string.Empty
            ).ConfigureAwait(false);

            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for username '{Username}': {ErrorMessage}", 
                    request.Username, result.ErrorMessage);
                return Error.Validation(
                    code: "Registration.Failed",
                    description: result.ErrorMessage ?? "Registration failed.");
            }

            // Log successful registration
            _logger.LogInformation("User '{Username}' registered successfully", request.Username);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing registration for username '{Username}'", request.Username);
            return Error.Failure(
                code: "Registration.Failure",
                description: $"An error occurred during registration: {ex.Message}");
        }
    }

    private Error? ValidateRequest(RegisterUserCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return Error.Validation(
                code: "Registration.Validation.Username",
                description: "Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Error.Validation(
                code: "Registration.Validation.Email",
                description: "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Error.Validation(
                code: "Registration.Validation.Password",
                description: "Password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return Error.Validation(
                code: "Registration.Validation.ConfirmPassword",
                description: "Confirm password is required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Error.Validation(
                code: "Registration.Validation.PasswordMismatch",
                description: "Passwords do not match.");
        }

        if (request.Password.Length < 8)
        {
            return Error.Validation(
                code: "Registration.Validation.PasswordTooShort",
                description: "Password must be at least 8 characters.");
        }

        if (!IsValidEmail(request.Email))
        {
            return Error.Validation(
                code: "Registration.Validation.InvalidEmail",
                description: "Invalid email format.");
        }

        return null;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Simple regex for basic email validation
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}