using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Configuration;
using MIC.Infrastructure.Data.Services;

namespace MIC.Infrastructure.Data.Persistence;

/// <summary>
/// Database initializer that applies migrations, optionally deletes/creates
/// the database, and seeds demo data based on configuration.
/// </summary>
public sealed class DbInitializer
{
    private readonly MicDbContext _context;
    private readonly DatabaseSettings _settings;
    private readonly DatabaseMigrationService _migrationService;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public DbInitializer(
        MicDbContext context,
        IOptions<DatabaseSettings> settings,
        DatabaseMigrationService migrationService,
        ILogger<DbInitializer> logger,
        IPasswordHasher passwordHasher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database initialization.");
            
            // Test database connectivity first
            if (!await CanConnectToDatabaseAsync(cancellationToken))
            {
                _logger.LogError("Cannot connect to the database. Please check your connection string.");
                throw new InvalidOperationException("Failed to connect to the database.");
            }

            _logger.LogInformation("Database connection verified.");

            if (_settings.DeleteDatabaseOnStartup)
            {
                _logger.LogWarning("DeleteDatabaseOnStartup is enabled. Deleting database...");
                await _context.Database.EnsureDeletedAsync(cancellationToken);
                _logger.LogInformation("Database deleted.");
            }

            if (_settings.RunMigrationsOnStartup)
            {
                _logger.LogInformation("RunMigrationsOnStartup is enabled. Applying migrations...");
                await _migrationService.ApplyMigrationsAsync(cancellationToken);
                _logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                _logger.LogInformation("RunMigrationsOnStartup is disabled. Ensuring database is created (dev fallback).");
                await _context.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("EnsureCreated completed.");
            }

            if (_settings.SeedDataOnStartup)
            {
                _logger.LogInformation("SeedDataOnStartup is enabled. Seeding demo data if necessary...");
                await SeedDemoDataAsync(cancellationToken);
                _logger.LogInformation("Demo data seeding completed.");
            }
            else
            {
                _logger.LogInformation("SeedDataOnStartup is disabled. Skipping demo data seeding.");
            }

            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }

    private async Task<bool> CanConnectToDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to connect to the database by executing a simple query
            await _context.Database.CanConnectAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not connect to the database.");
            return false;
        }
    }

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        if (await _context.Alerts.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Existing alerts found. Skipping demo seed.");
            return;
        }

        _logger.LogInformation("Seeding demo alerts...");
        await SeedAlertsAsync(cancellationToken);

        _logger.LogInformation("Seeding demo assets...");
        await SeedAssetsAsync(cancellationToken);

        _logger.LogInformation("Seeding demo decisions...");
        await SeedDecisionsAsync(cancellationToken);

        _logger.LogInformation("Seeding demo metrics...");
        await SeedMetricsAsync(cancellationToken);

        _logger.LogInformation("Seeding admin user...");
        await SeedAdminUserAsync(cancellationToken);

        _logger.LogInformation("Seeding demo emails...");
        await SeedEmailsAsync(cancellationToken);

        _logger.LogInformation("Saving seeded demo data...");
        await _context.SaveChangesAsync(cancellationToken);
    }

    #region Existing Seed Methods

    private async Task SeedAlertsAsync(CancellationToken cancellationToken)
    {
        var alerts = new[]
        {
            new IntelligenceAlert("High Temperature Detected", "Compressor unit #3 temperature exceeded safe threshold", AlertSeverity.Critical, "Industrial Monitoring System"),
            new IntelligenceAlert("Maintenance Due", "Scheduled maintenance required for primary generator", AlertSeverity.Warning, "Asset Management System"),
            new IntelligenceAlert("Production Milestone", "Monthly production target achieved 3 days early", AlertSeverity.Info, "Production Analytics")
        };

        await _context.Alerts.AddRangeAsync(alerts, cancellationToken);
    }

    private async Task SeedAssetsAsync(CancellationToken cancellationToken)
    {
        var assets = new[]
        {
            new AssetMonitor("Compressor Unit #3", "Industrial Equipment", "Plant A - Building 2"),
            new AssetMonitor("Primary Generator", "Power Generation", "Plant A - Power House"),
            new AssetMonitor("Production Line #1", "Manufacturing", "Plant B - Floor 3")
        };

        assets[0].UpdateHealthScore(45.0, "System");
        assets[1].UpdateHealthScore(85.0, "System");
        assets[2].UpdateHealthScore(92.0, "System");

        await _context.Assets.AddRangeAsync(assets, cancellationToken);
    }

    private async Task SeedDecisionsAsync(CancellationToken cancellationToken)
    {
        var decisions = new[]
        {
            new DecisionContext("Equipment Replacement Strategy", "Evaluate options for replacing aging compressor units", "Operations Manager", DateTime.UtcNow.AddDays(14), DecisionPriority.High)
        };

        decisions[0].AddOption("Replace Unit #3 immediately");
        decisions[0].AddOption("Schedule replacement in Q2");
        decisions[0].AddOption("Extend maintenance and defer to Q3");
        decisions[0].SetAIRecommendation("Replace Unit #3 immediately", 0.87);

        await _context.Decisions.AddRangeAsync(decisions, cancellationToken);
    }

    private async Task SeedMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = new[]
        {
            new OperationalMetric("Production Output", "Manufacturing", "Production Line #1", 1247.5, "units/hour", MetricSeverity.Normal),
            new OperationalMetric("Energy Consumption", "Utilities", "Plant A - Main Grid", 3420.8, "kWh", MetricSeverity.Warning)
        };

        await _context.Metrics.AddRangeAsync(metrics, cancellationToken);

        var sampleMetrics = MetricDataGenerator.GenerateSampleMetrics();
        await _context.Metrics.AddRangeAsync(sampleMetrics, cancellationToken);
    }

    #endregion

    #region New Seed Methods

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
        {
            _logger.LogInformation("Admin user already exists. Skipping admin seed.");
            return;
        }

		var now = DateTimeOffset.UtcNow;
		var (hash, salt) = _passwordHasher.HashPassword("Admin@123");

		var admin = new User
		{
			Username = "admin",
			Email = "admin@mbarieservicesltd.com",
			FullName = "Administrator",
			Role = UserRole.Admin,
			PasswordHash = hash,
			Salt = salt,
			CreatedAt = now,
			UpdatedAt = now
		};

		await _context.Users.AddAsync(admin, cancellationToken);
		_logger.LogInformation("Admin user seeded: admin / Admin@123");
    }

    private async Task SeedEmailsAsync(CancellationToken cancellationToken)
    {
		if (await _context.EmailMessages.AnyAsync(cancellationToken))
		{
			_logger.LogInformation("EmailMessages already exist. Skipping email seed.");
			return;
		}

		_logger.LogInformation("Seeding demo email accounts and messages...");

		// Use admin user if available; otherwise create a demo user
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);
		if (user == null)
		{
			var now = DateTimeOffset.UtcNow;
			var (hash, salt) = _passwordHasher.HashPassword("Demo@123");
			user = new User
			{
				Username = "demo",
				Email = "demo@mbarieservicesltd.com",
				FullName = "Demo User",
				Role = UserRole.User,
				PasswordHash = hash,
				Salt = salt,
				CreatedAt = now,
				UpdatedAt = now
			};
			await _context.Users.AddAsync(user, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);
		}

		// Minimal email account using updated constructor
		var workAccount = new EmailAccount(
			"alex@mbarieservicesltd.com", // email address
			EmailProvider.Outlook,       // provider
			user.Id,                     // user id
			"Alex Taylor"               // display name
		);

		await _context.EmailAccounts.AddAsync(workAccount, cancellationToken);
		await _context.SaveChangesAsync(cancellationToken);

		var nowBase = DateTime.UtcNow;
		DateTime Recent(int daysBack, int hourOffset) => nowBase.AddDays(-daysBack).AddHours(-hourOffset);

		var emails = new List<EmailMessage>();

		EmailMessage Create(string subject, string fromAddress, string fromName, string to, string cc, string bodyPreview, string bodyText, EmailFolder folder, EmailPriority priority, bool isUrgent, bool isRead, bool requiresResponse, bool containsActionItems, bool hasAttachments, int daysBack, int hourOffset)
		{
			var sent = Recent(daysBack, hourOffset);
			var received = sent.AddMinutes(5);

			var message = new EmailMessage(
				messageId: Guid.NewGuid().ToString(),
				subject: subject,
				fromAddress: fromAddress,
				fromName: fromName,
				toRecipients: to,
				sentDate: sent,
				receivedDate: received,
				bodyText: bodyText,
				userId: user.Id,
				emailAccountId: workAccount.Id,
				folder: folder);

			// basic AI / inbox flags
			message.SetAIAnalysis(
				priority: priority,
				category: EmailCategory.General,
				sentiment: SentimentType.Neutral,
				hasActionItems: containsActionItems,
				requiresResponse: requiresResponse,
				summary: bodyPreview);

			if (isRead)
			{
				message.MarkAsRead();
			}

			if (hasAttachments)
			{
				// marker only; real attachments are seeded separately if needed
				message.AddAttachment(new EmailAttachment("Placeholder.txt", "text/plain", 1024, "temp/path", message.Id));
			}

			return message;
		}

		// A few realistic demo emails (expand as needed)
		emails.Add(Create("CEO Update", "ceo@mbarieservicesltd.com", "CEO", "all-staff@mbarieservicesltd.com", "", "All staff meeting at 10 AM", "Dear team,\nAll staff meeting scheduled for 10 AM today.\nCEO", EmailFolder.Inbox, EmailPriority.High, true, false, false, false, false, 1, 2));
		emails.Add(Create("Project Alpha Status", "pm@mbarieservicesltd.com", "Project Manager", "alex@mbarieservicesltd.com", "", "Alpha phase 2 completed successfully", "Hi Alex,\nAlpha phase 2 is complete and ready for review.\nPM", EmailFolder.Inbox, EmailPriority.Normal, false, false, false, false, false, 2, 3));
		emails.Add(Create("Vendor Invoice", "vendor@mbarieservicesltd.com", "Accounts Payable", "finance@mbarieservicesltd.com", "", "Invoice #1456 for January", "Dear Finance Team,\nPlease process invoice #1456.\nVendor", EmailFolder.Inbox, EmailPriority.Low, false, false, false, false, false, 3, 1));
		emails.Add(Create("HR Reminder", "hr@mbarieservicesltd.com", "HR Department", "all-staff@mbarieservicesltd.com", "", "Submit your timesheet", "Hello Team,\nSubmit your timesheets by end of day today.\nHR", EmailFolder.Inbox, EmailPriority.Normal, true, false, false, false, false, 1, 3));
		// TODO: Add remaining demo emails as needed for richer inbox

		await _context.EmailMessages.AddRangeAsync(emails, cancellationToken);
		_logger.LogInformation("Demo email accounts and messages seeded.");
    }

    #endregion
}
