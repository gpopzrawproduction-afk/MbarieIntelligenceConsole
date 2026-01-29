using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailAttachment entity.
/// </summary>
public class EmailAttachmentConfiguration : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> builder)
    {
        builder.ToTable("EmailAttachments");

        builder.HasKey(e => e.Id);

        // File properties
        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.ExternalId)
            .HasMaxLength(500);

        // Content extraction
        builder.Property(e => e.ExtractedText);

        builder.Property(e => e.ProcessingError)
            .HasMaxLength(2000);

        // AI properties
        builder.Property(e => e.AISummary)
            .HasMaxLength(2000);

        builder.Property(e => e.ExtractedKeywords)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);

        builder.Property(e => e.EmbeddingId)
            .HasMaxLength(500);

        // Enums
        builder.Property(e => e.Type)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>();

        builder.Property(e => e.DocumentCategory)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(e => e.EmailMessageId);

        builder.HasIndex(e => e.Type);

        builder.HasIndex(e => e.Status);

        builder.HasIndex(e => e.IsProcessed);

        builder.HasIndex(e => e.IsIndexed);

        builder.HasIndex(e => e.KnowledgeEntryId);

        // Composite indexes
        builder.HasIndex(e => new { e.EmailMessageId, e.Type });
    }
}
