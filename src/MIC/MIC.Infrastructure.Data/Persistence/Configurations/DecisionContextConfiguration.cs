using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;
using System.Text.Json;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for DecisionContext
/// </summary>
public class DecisionContextConfiguration : IEntityTypeConfiguration<DecisionContext>
{
    public void Configure(EntityTypeBuilder<DecisionContext> builder)
    {
        builder.ToTable("Decisions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContextName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.DecisionMaker)
            .IsRequired()
            .HasMaxLength(100);

        // Use JSON serialization for complex types (works with both SQLite and PostgreSQL)
        builder.Property(x => x.ContextData)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("TEXT");

        builder.Property(x => x.ConsideredOptions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("TEXT");

        builder.Property(x => x.Priority)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.AIConfidence);

        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Deadline);
        builder.HasIndex(x => x.DecisionMaker);

        builder.Ignore(x => x.DomainEvents);
    }
}
