using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailAccount entity.
/// </summary>
public class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("EmailAccounts");

        builder.HasKey(e => e.Id);

        // Account info
        builder.Property(e => e.EmailAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(500);

        // OAuth tokens (should be encrypted in production)
        builder.Property(e => e.AccessTokenEncrypted)
            .HasMaxLength(4000);

        builder.Property(e => e.RefreshTokenEncrypted)
            .HasMaxLength(4000);

        builder.Property(e => e.GrantedScopes)
            .HasMaxLength(2000);

        // Sync state
        builder.Property(e => e.DeltaLink)
            .HasMaxLength(2000);

        builder.Property(e => e.HistoryId)
            .HasMaxLength(500);

        builder.Property(e => e.LastSyncError)
            .HasMaxLength(2000);

        // Folders to sync (stored as JSON or semicolon-separated)
        builder.Property(e => e.FoldersToSync)
            .HasConversion(
                v => v == null ? null : string.Join(";", v),
                v => v == null ? null : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);

        // Enums
        builder.Property(e => e.Provider)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(e => e.UserId);

        builder.HasIndex(e => e.EmailAddress);

        builder.HasIndex(e => e.Provider);

        builder.HasIndex(e => e.IsActive);

        builder.HasIndex(e => e.IsPrimary);

        builder.HasIndex(e => e.Status);

        builder.HasIndex(e => e.LastSyncedAt);

        // Composite indexes
        builder.HasIndex(e => new { e.UserId, e.EmailAddress })
            .IsUnique();

        builder.HasIndex(e => new { e.UserId, e.IsActive });

        builder.HasIndex(e => new { e.UserId, e.IsPrimary });
    }
}
