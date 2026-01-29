using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;
using System.Text.Json;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AssetMonitor
/// </summary>
public class AssetMonitorConfiguration : IEntityTypeConfiguration<AssetMonitor>
{
    public void Configure(EntityTypeBuilder<AssetMonitor> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AssetType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Location)
            .IsRequired()
            .HasMaxLength(200);

        // Use JSON serialization for complex types (works with both SQLite and PostgreSQL)
        builder.Property(x => x.Specifications)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("TEXT");

        builder.Property(x => x.AssociatedMetrics)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("TEXT");

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.HealthScore);

        builder.HasIndex(x => x.AssetType);
        builder.HasIndex(x => x.Location);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastMonitoredAt);

        builder.Ignore(x => x.DomainEvents);
    }
}
