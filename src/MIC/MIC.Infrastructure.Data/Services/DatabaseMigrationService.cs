using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Handles database connectivity checks and EF Core migrations,
/// with logging around each operation.
/// </summary>
public sealed class DatabaseMigrationService
{
    private readonly MicDbContext _context;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        MicDbContext context,
        ILogger<DatabaseMigrationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<string>> GetPendingMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching pending EF Core migrations...");
            var pending = await _context.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false);

            var list = pending as IList<string> ?? new List<string>(pending);
            _logger.LogInformation("Found {Count} pending migrations.", list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch pending migrations.");
            throw;
        }
    }

    public async Task ApplyMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for pending migrations before applying...");
            var pending = await _context.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false);

            var list = pending as IList<string> ?? new List<string>(pending);
            if (list.Count == 0)
            {
                _logger.LogInformation("No pending migrations to apply.");
                return;
            }

            _logger.LogInformation(
                "Applying {Count} pending migrations: {Migrations}",
                list.Count,
                string.Join(", ", list));

            await _context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply EF Core migrations.");
            throw;
        }
    }

    public async Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking database connectivity...");
            var canConnect = await _context.Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            if (canConnect)
            {
                _logger.LogInformation("Database connectivity check succeeded.");
            }
            else
            {
                _logger.LogWarning("Database connectivity check failed.");
            }

            return canConnect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking database connectivity.");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAppliedMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching applied EF Core migrations...");
            var applied = await _context.Database
                .GetAppliedMigrationsAsync(cancellationToken)
                .ConfigureAwait(false);

            var list = applied as IList<string> ?? new List<string>(applied);
            _logger.LogInformation("Found {Count} applied migrations.", list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch applied migrations.");
            throw;
        }
    }
}
