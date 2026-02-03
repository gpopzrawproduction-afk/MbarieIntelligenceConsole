using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Authentication.Commands.LoginCommand;

/// <summary>
/// Handler for authenticating users with username and password.
/// </summary>
public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Error.Validation(
                    code: "Login.Validation",
                    description: "Username and password are required.");
            }

            // Fetch user without tracking for password verification
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user is null || !user.IsActive)
            {
                _logger.LogWarning("Login failed for username '{Username}': user not found or inactive", request.Username);
                return Error.Validation(
                    code: "Login.InvalidCredentials",
                    description: "Invalid username or password.");
            }

            // Verify password against stored hash
            var verified = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt);
            if (!verified)
            {
                _logger.LogWarning("Login failed for username '{Username}': password verification failed", request.Username);
                return Error.Validation(
                    code: "Login.InvalidCredentials",
                    description: "Invalid username or password.");
            }

            // Update last login timestamp using repository abstraction
            var trackedUser = await _userRepository.GetTrackedByIdAsync(user.Id);
            if (trackedUser is not null)
            {
                trackedUser.LastLoginAt = DateTimeOffset.UtcNow;
                await _userRepository.UpdateAsync(trackedUser);
            }

            var token = _jwtTokenService.GenerateToken(user);
            _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

            return new LoginResult
            {
                Success = true,
                Token = token,
                User = UserDto.FromUser(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing login for username '{Username}'", request.Username);
            return Error.Failure(
                code: "Login.Failure",
                description: $"An error occurred during login: {ex.Message}");
        }
    }
}