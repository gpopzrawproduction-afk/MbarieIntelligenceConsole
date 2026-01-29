namespace MIC.Infrastructure.Data.Configuration;

/// <summary>
/// Database-related configuration settings bound from the 'Database' section.
/// </summary>
public sealed class DatabaseSettings
{
    public bool RunMigrationsOnStartup { get; set; } = false;

    public bool DeleteDatabaseOnStartup { get; set; } = false;

    public bool SeedDataOnStartup { get; set; } = false;

    public int ConnectionRetryCount { get; set; } = 3;

    public int ConnectionRetryDelaySeconds { get; set; } = 5;

    public bool CreateBackupBeforeMigration { get; set; } = false;
}