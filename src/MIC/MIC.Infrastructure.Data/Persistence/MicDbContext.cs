using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Domain.Abstractions;
using MIC.Core.Domain.Entities;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.Data.Services;

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

    public MicDbContext(DbContextOptions<MicDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MicDbContext).Assembly);
        
        // Apply knowledge base configuration
        modelBuilder.ConfigureKnowledgeBase();

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
}
