using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MIC.Infrastructure.Data.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Supports SQLite (default for dev) and PostgreSQL.
/// </summary>
public class MicDbContextFactory : IDesignTimeDbContextFactory<MicDbContext>
{
    public MicDbContext CreateDbContext(string[] args)
    {
        Console.WriteLine("[EF DESIGN-TIME] MicDbContextFactory.CreateDbContext invoked");

        var optionsBuilder = new DbContextOptionsBuilder<MicDbContext>();

        // Check for SQLite mode (default for development)
        var useSqliteEnv = Environment.GetEnvironmentVariable("USE_SQLITE");
        var useSqlite = string.IsNullOrEmpty(useSqliteEnv) || bool.TryParse(useSqliteEnv, out var b) && b;

        // If MIC_CONNECTION_STRING is set, use PostgreSQL
        var pgConn = Environment.GetEnvironmentVariable("MIC_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(pgConn))
        {
            Console.WriteLine("[EF DESIGN-TIME] Using PostgreSQL from MIC_CONNECTION_STRING");
            Console.WriteLine($"[EF DESIGN-TIME] Connection: {MaskPassword(pgConn)}");
            
            var normalizedConn = NormalizeConnectionString(pgConn);
            optionsBuilder.UseNpgsql(normalizedConn, b => b.MigrationsAssembly("MIC.Infrastructure.Data"));
            return new MicDbContext(optionsBuilder.Options);
        }

        // Try to load from appsettings.json
        var configuration = LoadConfiguration();
        var configConn = configuration?.GetConnectionString("MicDatabase");
        
        if (!string.IsNullOrWhiteSpace(configConn) && !useSqlite)
        {
            Console.WriteLine("[EF DESIGN-TIME] Using PostgreSQL from appsettings.json");
            Console.WriteLine($"[EF DESIGN-TIME] Connection: {MaskPassword(configConn)}");
            
            var normalizedConn = NormalizeConnectionString(configConn);
            optionsBuilder.UseNpgsql(normalizedConn, b => b.MigrationsAssembly("MIC.Infrastructure.Data"));
            return new MicDbContext(optionsBuilder.Options);
        }

        // Default: Use SQLite for development
        var sqliteConn = configuration?.GetConnectionString("MicSqlite") ?? "Data Source=mic_dev.db";
        Console.WriteLine($"[EF DESIGN-TIME] Using SQLite: {sqliteConn}");
        
        optionsBuilder.UseSqlite(sqliteConn, b => b.MigrationsAssembly("MIC.Infrastructure.Data"));
        return new MicDbContext(optionsBuilder.Options);
    }

    private static IConfiguration? LoadConfiguration()
    {
        var basePath = FindStartupProjectPath();
        if (basePath == null) return null;

        try
        {
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .Build();
        }
        catch
        {
            return null;
        }
    }

    private static string? FindStartupProjectPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MIC.Console"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MIC.Desktop.Avalonia"),
            Directory.GetCurrentDirectory()
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
            {
                return fullPath;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string NormalizeConnectionString(string connectionString)
    {
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

    private static string MaskPassword(string connectionString)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=****",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

