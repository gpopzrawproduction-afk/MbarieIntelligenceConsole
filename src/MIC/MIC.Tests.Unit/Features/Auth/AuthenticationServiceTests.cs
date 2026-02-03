using System;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Auth;

public class AuthenticationServiceTests
{
    private readonly AuthenticationService _sut;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _logger = Substitute.For<ILogger<AuthenticationService>>();
        _sut = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenService, _logger);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var username = "admin";
        var password = "password123";
        var user = new User
        {
            Username = username,
            PasswordHash = "hashedPassword",
            Salt = "salt",
            IsActive = true
        };
        var token = "jwt-token";

        _userRepository.GetByUsernameAsync(username).Returns(user);
        _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt).Returns(true);
        _jwtTokenService.GenerateToken(user).Returns(token);

        // Act
        var result = await _sut.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        result.User.Should().Be(user);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var username = "admin";
        var password = "wrongpassword";
        var user = new User
        {
            Username = username,
            PasswordHash = "hashedPassword",
            Salt = "salt",
            IsActive = true
        };

        _userRepository.GetByUsernameAsync(username).Returns(user);
        _passwordHasher.VerifyPassword(password, user.PasswordHash, user.Salt).Returns(false);

        // Act
        var result = await _sut.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ReturnsFailure()
    {
        // Arrange
        var username = "nonexistent";
        var password = "password123";

        _userRepository.GetByUsernameAsync(username).Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _sut.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var username = "inactive";
        var password = "password123";
        var user = new User
        {
            Username = username,
            PasswordHash = "hashedPassword",
            Salt = "salt",
            IsActive = false
        };

        _userRepository.GetByUsernameAsync(username).Returns(user);

        // Act
        var result = await _sut.LoginAsync(username, password);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithEmptyCredentials_ReturnsFailure()
    {
        // Act
        var result = await _sut.LoginAsync("", "");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithValidInput_ReturnsSuccessWithToken()
    {
        // Arrange
        var username = "newuser";
        var email = "newuser@example.com";
        var password = "password123";
        var displayName = "New User";
        var token = "jwt-token";
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hash",
            Salt = "salt",
            FullName = displayName,
            IsActive = true
        };

        _userRepository.GetByUsernameAsync(username).Returns(Task.FromResult<User?>(null));
        _userRepository.GetByEmailAsync(email).Returns(Task.FromResult<User?>(null));
        _passwordHasher.HashPassword(password).Returns(("hash", "salt"));
        _jwtTokenService.GenerateToken(Arg.Any<User>()).Returns(token);
        _userRepository.CreateAsync(Arg.Any<User>()).Returns(user);

        // Act
        var result = await _sut.RegisterAsync(username, email, password, displayName);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);
        result.User!.Email.Should().Be(email);
        result.User!.FullName.Should().Be(displayName);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsFailure()
    {
        // Arrange
        var username = "existing";
        var email = "new@example.com";
        var password = "password123";
        var existingUser = new User { Username = username };

        _userRepository.GetByUsernameAsync(username).Returns(existingUser);

        // Act
        var result = await _sut.RegisterAsync(username, email, password, "Display");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var username = "newuser";
        var email = "existing@example.com";
        var password = "password123";
        var existingUser = new User { Email = email };

        _userRepository.GetByUsernameAsync(username).Returns(Task.FromResult<User?>(null));
        _userRepository.GetByEmailAsync(email).Returns(existingUser);

        // Act
        var result = await _sut.RegisterAsync(username, email, password, "Display");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var username = "newuser";
        var email = "invalid-email";
        var password = "password123";

        _userRepository.GetByUsernameAsync(username).Returns(Task.FromResult<User?>(null));
        _userRepository.GetByEmailAsync(email).Returns(Task.FromResult<User?>(null));

        // Act
        var result = await _sut.RegisterAsync(username, email, password, "Display");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithMissingRequiredFields_ReturnsFailure()
    {
        // Act
        var result = await _sut.RegisterAsync("", "", "", "");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}