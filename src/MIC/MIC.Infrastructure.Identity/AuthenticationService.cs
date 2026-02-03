using System;
using System.Threading.Tasks;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MIC.Infrastructure.Identity;

/// <summary>
/// Authentication service using the domain user model, Argon2id password hashing, and JWT tokens.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly Microsoft.Extensions.Logging.ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        Microsoft.Extensions.Logging.ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthenticationResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Username and password are required."
            };
        }

        var user = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }


        // LOGIN DEBUG: Log input and stored values before verification
        _logger.LogInformation($"LOGIN DEBUG: Attempting login. Username: '{username}', InputPassword: '{password}', StoredHash: '{user.PasswordHash}', StoredSalt: '{user.Salt}'");

        var verified = _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);

        _logger.LogInformation($"LOGIN DEBUG: Verification result for Username: '{username}': {verified}");

        if (!verified)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthenticationResult
        {
            Success = true,
            Token = token,
            User = user
        };
    }

    public async Task<AuthenticationResult> RegisterAsync(
        string username,
        string email,
        string password,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Username, email, and password are required."
            };
        }

        // Very basic email shape check; full validation can be added later.
        if (!email.Contains("@", StringComparison.Ordinal))
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Email address is not in a valid format."
            };
        }

        var existingByUsername = await _userRepository.GetByUsernameAsync(username).ConfigureAwait(false);
        if (existingByUsername is not null)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "A user with this username already exists."
            };
        }

        var existingByEmail = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
        if (existingByEmail is not null)
        {
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "A user with this email address already exists."
            };
        }

        var (hash, salt) = _passwordHasher.HashPassword(password);
        var now = DateTimeOffset.UtcNow;

        // Check if this is the first user
        var anyUsers = await _userRepository.GetByUsernameAsync("admin").ConfigureAwait(false);
        bool isFirstUser = anyUsers == null;

        var user = new User
        {
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = hash,
            Salt = salt,
            FullName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Role = isFirstUser ? UserRole.Admin : UserRole.User
        };

        user = await _userRepository.CreateAsync(user).ConfigureAwait(false);

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthenticationResult
        {
            Success = true,
            Token = token,
            User = user
        };
    }

    public Task LogoutAsync()
    {
        // For a desktop app, logout is typically handled by clearing local session/token.
        // This implementation is a no-op placeholder for future expansion (e.g., server-side revocation).
        return Task.CompletedTask;
    }

    public Task<User?> GetCurrentUserAsync()
    {
        // This service is stateless. Current user should be tracked by a higher-level session service.
        return Task.FromResult<User?>(null);
    }
}