using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Abstractions;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Services;
using KnowledgeEntry = MIC.Core.Application.Common.Interfaces.KnowledgeEntry;

namespace MIC.Infrastructure.Data.Persistence;

/// <summary>
/// Main database context for Mbarie Intelligence Console
/// </summary>
public class MicDbContext : DbContext
{
    // Core entities
    public DbSet<IntelligenceAlert> Alerts => Set<IntelligenceAlert>();
    public DbSet<AssetMonitor> Assets => Set<AssetMonitor>();
    public DbSet<DecisionContext> Decisions => Set<DecisionContext>();
    public DbSet<OperationalMetric> Metrics => Set<OperationalMetric>();
    public DbSet<User> Users => Set<User>();
    
    // Email Intelligence entities
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
    public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();

    // Knowledge Base entities
    public DbSet<KnowledgeEntry> KnowledgeEntries => Set<KnowledgeEntry>();

    // New entities for infrastructure fixes
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();

    public MicDbContext(DbContextOptions<MicDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicDbContext).Assembly);
        
        // Apply knowledge base configuration
        modelBuilder.ConfigureKnowledgeBase();

        // Configure new entities
        ConfigureUserSettings(modelBuilder);
        ConfigureChatHistory(modelBuilder);
        ConfigureAlertIndexes(modelBuilder);
        ConfigureEmailIndexes(modelBuilder);

        // Global query filter for soft deletes if BaseEntity exposes IsDeleted
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // CreatedAt set in BaseEntity
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetModifiedNow();
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        // Phase 4: dispatch domain events
        return result;
    }

    private void ConfigureUserSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SettingsJson)
                .IsRequired()
                .HasMaxLength(8000); // Reasonable limit for JSON settings
            
            entity.Property(e => e.LastUpdated)
                .IsRequired();
            
            entity.Property(e => e.SettingsVersion)
                .IsRequired()
                .HasDefaultValue(1);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for faster user lookup
            entity.HasIndex(e => e.UserId)
                .IsUnique(); // One settings record per user
        });
    }

    private void ConfigureChatHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Query)
                .IsRequired()
                .HasMaxLength(4000);
            
            entity.Property(e => e.Response)
                .IsRequired()
                .HasMaxLength(8000);
            
            entity.Property(e => e.Timestamp)
                .IsRequired();
            
            entity.Property(e => e.AIProvider)
                .HasMaxLength(50);
            
            entity.Property(e => e.ModelUsed)
                .HasMaxLength(100);
            
            entity.Property(e => e.TokenCount)
                .IsRequired()
                .HasDefaultValue(0);
            
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000);
            
            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
        });
    }

    private void ConfigureAlertIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntelligenceAlert>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TriggeredAt);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.Severity, e.Status });
        });
    }

    private void ConfigureEmailIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.HasIndex(e => e.ReceivedDate);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.ReceivedDate });
        });
    }
}
