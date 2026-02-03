using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.Data.Configuration;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Repositories;
using MIC.Infrastructure.Data.Services;
using System.Text.RegularExpressions;

namespace MIC.Infrastructure.Data;

/// <summary>
/// Dependency injection registration for data infrastructure.
/// Supports SQLite (development) and PostgreSQL (production).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers data infrastructure services.
    /// Chooses database provider based on configuration and connection string:
    /// 1. Reads connection string from env or configuration.
    /// 2. Detects provider from Database:Provider or connection string format.
    /// 3. Logs provider choice without exposing passwords.
    /// </summary>
    public static IServiceCollection AddDataInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);
        var provider = DetectProvider(configuration, connectionString);

        LogProviderChoice(provider, connectionString);

        if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureSqlite(services, connectionString);
        }
        else if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            ConfigurePostgreSql(services, NormalizePostgreSqlConnectionString(connectionString));
        }
        else
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported providers are SQLite and Postgres.");
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IMetricsRepository, MetricsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();
        services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
        services.AddScoped<IEmailSyncService, RealEmailSyncService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register database configuration and services
        services.AddOptions<DatabaseSettings>()
            .Configure<IConfiguration>((settings, config) =>
            {
                config.GetSection("Database").Bind(settings);
            });
        services.AddScoped<DatabaseMigrationService>();
        services.AddScoped<DbInitializer>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var envConn = Environment.GetEnvironmentVariable("MIC_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            return envConn;
        }

        var sqliteConn = configuration.GetConnectionString("MicSqlite");
        var pgConn = configuration.GetConnectionString("MicDatabase");

        if (!string.IsNullOrWhiteSpace(pgConn))
        {
            return pgConn;
        }

        if (!string.IsNullOrWhiteSpace(sqliteConn))
        {
            return sqliteConn;
        }

        throw new InvalidOperationException(
            "No database connection string configured. " +
            "Set MIC_CONNECTION_STRING environment variable or configure ConnectionStrings:MicDatabase or ConnectionStrings:MicSqlite.");
    }

    private static string DetectProvider(IConfiguration configuration, string connectionString)
    {
        var providerFromConfig = configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(providerFromConfig))
        {
            return providerFromConfig;
        }

        if (connectionString.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
        {
            return "SQLite";
        }

        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            return "Postgres";
        }

        return "SQLite";
    }

    private static void LogProviderChoice(string provider, string connectionString)
    {
        var redacted = RedactPassword(connectionString);
        Console.WriteLine($"[MIC.Data] Using provider '{provider}' with connection string '{redacted}'");
    }

    private static string RedactPassword(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var pattern = "(?i)(Password|Pwd)=[^;]*";
        return Regex.Replace(connectionString, pattern, m => m.Groups[1].Value + "=****");
    }

    private static void ConfigureSqlite(IServiceCollection services, string connectionString)
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        var sqliteConn = string.IsNullOrWhiteSpace(connectionString)
            ? $"Data Source={System.IO.Path.Combine(baseDir, "mic_dev.db")}" 
            : connectionString.Replace("%BASE_DIR%", baseDir.Replace("\\", "/"));

        services.AddDbContext<MicDbContext>(options =>
        {
            options.UseSqlite(sqliteConn, b => b.MigrationsAssembly("MIC.Infrastructure.Data"));
        });
    }

    private static void ConfigurePostgreSql(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MicDbContext>(options =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("MIC.Infrastructure.Data"));
        });
    }

    private static string NormalizePostgreSqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string is empty.");
        }

        if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";SSL Mode=Disable";
        }

        if (!connectionString.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Trust Server Certificate=true";
        }

        return connectionString;
    }
}
