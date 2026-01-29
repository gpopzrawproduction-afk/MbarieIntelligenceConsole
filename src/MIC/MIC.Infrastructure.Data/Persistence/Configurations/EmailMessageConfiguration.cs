using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailMessage entity.
/// </summary>
public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailMessages");

        builder.HasKey(e => e.Id);

        // Core properties
        builder.Property(e => e.MessageId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.FromAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FromName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ToRecipients)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.CcRecipients)
            .HasMaxLength(4000);

        builder.Property(e => e.BccRecipients)
            .HasMaxLength(4000);

        builder.Property(e => e.BodyText)
            .IsRequired();

        builder.Property(e => e.BodyHtml);

        builder.Property(e => e.BodyPreview)
            .HasMaxLength(500);

        builder.Property(e => e.ConversationId)
            .HasMaxLength(500);

        builder.Property(e => e.InReplyTo)
            .HasMaxLength(500);

        // AI properties
        builder.Property(e => e.AISummary)
            .HasMaxLength(2000);

        builder.Property(e => e.ExtractedKeywords)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);

        builder.Property(e => e.ActionItems)
            .HasConversion(
                v => string.Join("|||", v),
                v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(4000);

        // Enums
        builder.Property(e => e.Folder)
            .HasConversion<int>();

        builder.Property(e => e.Importance)
            .HasConversion<int>();

        builder.Property(e => e.AIPriority)
            .HasConversion<int>();

        builder.Property(e => e.AICategory)
            .HasConversion<int>();

        builder.Property(e => e.Sentiment)
            .HasConversion<int>();

        // Relationships
        builder.HasMany(e => e.Attachments)
            .WithOne()
            .HasForeignKey(a => a.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => e.MessageId)
            .IsUnique();

        builder.HasIndex(e => e.UserId);

        builder.HasIndex(e => e.EmailAccountId);

        builder.HasIndex(e => e.SentDate);

        builder.HasIndex(e => e.ReceivedDate);

        builder.HasIndex(e => e.FromAddress);

        builder.HasIndex(e => e.IsRead);

        builder.HasIndex(e => e.Folder);

        builder.HasIndex(e => e.AIPriority);

        builder.HasIndex(e => e.AICategory);

        builder.HasIndex(e => e.RequiresResponse);

        builder.HasIndex(e => e.ConversationId);

        // Composite indexes
        builder.HasIndex(e => new { e.UserId, e.ReceivedDate });
        
        builder.HasIndex(e => new { e.EmailAccountId, e.ReceivedDate });
        
        builder.HasIndex(e => new { e.UserId, e.IsRead, e.Folder });
        
        builder.HasIndex(e => new { e.UserId, e.RequiresResponse });

		// Inbox UX flags
		builder.Property(e => e.Priority)
			.IsRequired();

		builder.Property(e => e.IsUrgent)
			.IsRequired();
    }
}
