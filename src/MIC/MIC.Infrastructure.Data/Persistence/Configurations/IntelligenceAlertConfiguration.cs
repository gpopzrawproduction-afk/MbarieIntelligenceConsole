using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;
using System.Text.Json;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for IntelligenceAlert
/// </summary>
public class IntelligenceAlertConfiguration : IEntityTypeConfiguration<IntelligenceAlert>
{
    public void Configure(EntityTypeBuilder<IntelligenceAlert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AlertName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100);

        // Use JSON serialization for complex types (works with both SQLite and PostgreSQL)
        builder.Property(x => x.Context)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("TEXT");

        builder.Property(x => x.Severity)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TriggeredAt);
        builder.HasIndex(x => x.Source);

        builder.Ignore(x => x.DomainEvents);
    }
}
