using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;
using System.Text.Json;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OperationalMetric
/// </summary>
public class OperationalMetricConfiguration : IEntityTypeConfiguration<OperationalMetric>
{
    public void Configure(EntityTypeBuilder<OperationalMetric> builder)
    {
        builder.ToTable("Metrics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50);

        // Use JSON serialization for complex types (works with both SQLite and PostgreSQL)
        builder.Property(x => x.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("TEXT");

        builder.Property(x => x.Severity)
            .HasConversion<string>();

        builder.HasIndex(x => x.MetricName);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Severity);

        builder.Ignore(x => x.DomainEvents);
    }
}
