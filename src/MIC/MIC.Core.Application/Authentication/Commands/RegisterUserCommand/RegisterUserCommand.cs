using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Authentication.Commands.RegisterUserCommand;

/// <summary>
/// Command to register a new user with username, email, password, and optional full name.
/// </summary>
public record RegisterUserCommand : ICommand<AuthenticationResult>
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string? FullName { get; init; }
}