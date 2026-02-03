using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Authentication.Commands.LoginCommand;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly LoginCommandHandler _sut;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _logger = Substitute.For<ILogger<LoginCommandHandler>>();
        
        _sut = new LoginCommandHandler(
            _userRepository,
            _passwordHasher,
            _jwtTokenService,
            _logger);
    }

    [Fact]
    public async Task Handle_WithEmptyUsername_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("", "password123");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.Validation");
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new LoginCommand("admin", "");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.Validation");
    }

    [Fact]
    public async Task Handle_WithNonexistentUser_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var command = new LoginCommand("nonexistent", "password123");
        
        _userRepository.GetByUsernameAsync("nonexistent")
            .Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var command = new LoginCommand("inactive-user", "password123");
        var user = CreateTestUser("inactive-user", isActive: false);
        
        _userRepository.GetByUsernameAsync("inactive-user")
            .Returns(user);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var command = new LoginCommand("admin", "wrongpassword");
        var user = CreateTestUser("admin", isActive: true);
        
        _userRepository.GetByUsernameAsync("admin")
            .Returns(user);
        _passwordHasher.VerifyPassword("wrongpassword", user.PasswordHash, user.Salt)
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Login.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var command = new LoginCommand("admin", "correctpassword");
        var user = CreateTestUser("admin", isActive: true);
        var expectedToken = "jwt-token-12345";
        
        _userRepository.GetByUsernameAsync("admin")
            .Returns(user);
        _passwordHasher.VerifyPassword("correctpassword", user.PasswordHash, user.Salt)
            .Returns(true);
        _userRepository.GetTrackedByIdAsync(user.Id)
            .Returns(user);
        _jwtTokenService.GenerateToken(user)
            .Returns(expectedToken);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Success.Should().BeTrue();
        result.Value.Token.Should().Be(expectedToken);
        result.Value.User.Should().NotBeNull();
        result.Value.User!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_UpdatesLastLoginTimestamp()
    {
        // Arrange
        var command = new LoginCommand("admin", "correctpassword");
        var user = CreateTestUser("admin", isActive: true);
        var trackedUser = CreateTestUser("admin", isActive: true);
        trackedUser.LastLoginAt = null; // Not logged in before
        
        _userRepository.GetByUsernameAsync("admin")
            .Returns(user);
        _passwordHasher.VerifyPassword("correctpassword", user.PasswordHash, user.Salt)
            .Returns(true);
        _userRepository.GetTrackedByIdAsync(user.Id)
            .Returns(trackedUser);
        _jwtTokenService.GenerateToken(user)
            .Returns("token");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        trackedUser.LastLoginAt.Should().NotBeNull();
        trackedUser.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        await _userRepository.Received(1).UpdateAsync(trackedUser);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_GeneratesTokenWithCorrectUser()
    {
        // Arrange
        var command = new LoginCommand("admin", "correctpassword");
        var user = CreateTestUser("admin", isActive: true);
        
        _userRepository.GetByUsernameAsync("admin")
            .Returns(user);
        _passwordHasher.VerifyPassword("correctpassword", user.PasswordHash, user.Salt)
            .Returns(true);
        _userRepository.GetTrackedByIdAsync(user.Id)
            .Returns(user);
        _jwtTokenService.GenerateToken(Arg.Any<User>())
            .Returns("token");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _jwtTokenService.Received(1).GenerateToken(Arg.Is<User>(u => u.Username == "admin"));
    }

    [Theory]
    [InlineData("  ", "password")]
    [InlineData("username", "  ")]
    [InlineData(null, "password")]
    [InlineData("username", null)]
    public async Task Handle_WithWhitespaceOrNullCredentials_ReturnsValidationError(
        string? username, string? password)
    {
        // Arrange
        var command = new LoginCommand(username!, password!);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Login.Validation");
    }

    private static User CreateTestUser(string username, bool isActive)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "hashed-password",
            Salt = "salt-value",
            FullName = $"{username} User",
            IsActive = isActive,
            Role = UserRole.User,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return user;
    }
}
