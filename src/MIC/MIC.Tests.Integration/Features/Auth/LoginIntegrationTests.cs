using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application;
using MIC.Core.Application.Authentication;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Identity;
using MIC.Infrastructure.Identity.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace MIC.Tests.Integration.Features.Auth;

public class LoginIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private IServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL test container
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("test_mic")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _dbContainer.StartAsync();

        // Set up DI with test database
        var services = new ServiceCollection();
        var connectionString = _dbContainer.GetConnectionString();

        // Create configuration with connection string
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MicDatabase"] = connectionString,
                ["Database:Provider"] = "Postgres"
            })
            .Build();

        // Add logging
        services.AddLogging();

        // Add services with configuration
        services.AddApplication();
        services.AddDataInfrastructure(configuration);
        services.AddIdentityInfrastructure();

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();

        // Initialize database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MicDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        // Create test user
        var passwordHasher = new PasswordHasher();
        var (passwordHash, salt) = passwordHasher.HashPassword("Admin@123");

        var user = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = passwordHash,
            Salt = salt,
            FullName = "Administrator",
            IsActive = true,
            Role = UserRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessWithToken()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var username = "admin";
        var password = "Admin@123";

        // Act
        var result = await authService.LoginAsync(username, password);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(username, result.User.Username);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var username = "admin";
        var wrongPassword = "WrongPassword123";

        // Act
        var result = await authService.LoginAsync(username, wrongPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var username = "nonexistent";
        var password = "SomePassword123";

        // Act
        var result = await authService.LoginAsync(username, password);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.NotNull(result.ErrorMessage);
    }

    public async Task DisposeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }
        _serviceProvider = null;
    }
}
