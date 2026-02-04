using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using EmailAttachment = MIC.Core.Domain.Entities.EmailAttachment;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Services
{
    /// <summary>
    /// Implementation of knowledge base service using Entity Framework
    /// </summary>
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly MicDbContext _dbContext;

        public KnowledgeBaseService(MicDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Indexes an email attachment in the knowledge base
        /// </summary>
        public async Task IndexAttachmentAsync(EmailAttachment attachment, CancellationToken cancellationToken = default)
        {
            // Get the email message to retrieve the actual user ID
            var emailMessage = await _dbContext.EmailMessages
                .Where(em => em.Attachments.Any(a => a.Id == attachment.Id))
                .Select(em => new { em.UserId })
                .FirstOrDefaultAsync(cancellationToken);

            if (emailMessage == null) return;

            // Create knowledge entry from attachment
            var knowledgeEntry = new KnowledgeEntry
            {
                Title = attachment.FileName,
                Content = attachment.ExtractedText ?? string.Empty,
                FullContent = attachment.ExtractedText ?? string.Empty,
                SourceType = "EmailAttachment",
                SourceId = attachment.Id,
                UserId = emailMessage.UserId,
                Tags = new List<string> { "attachment", attachment.Type.ToString(), "email-content" }
            };

            // Add to knowledge base
            await _dbContext.KnowledgeEntries.AddAsync(knowledgeEntry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Indexes an email message in the knowledge base
        /// </summary>
        public async Task IndexEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
        {
            var tags = new List<string>();

            // Add AI-derived tags
            if (emailMessage.AICategory != EmailCategory.General)
            {
                tags.Add(emailMessage.AICategory.ToString().ToLower());
            }

            if (emailMessage.AIPriority != EmailPriority.Normal)
            {
                tags.Add(emailMessage.AIPriority.ToString().ToLower());
            }

            if (emailMessage.ContainsActionItems)
            {
                tags.Add("action-items");
            }

            if (emailMessage.RequiresResponse)
            {
                tags.Add("requires-response");
            }

            // Create knowledge entry from email
            var knowledgeEntry = new KnowledgeEntry
            {
                Title = emailMessage.Subject,
                Content = emailMessage.BodyPreview ?? emailMessage.BodyText,
                FullContent = emailMessage.BodyText,
                SourceType = "EmailMessage",
                SourceId = emailMessage.Id,
                UserId = emailMessage.UserId,
                Tags = tags
            };

            // Add to knowledge base
            await _dbContext.KnowledgeEntries.AddAsync(knowledgeEntry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Searches the knowledge base for relevant information
        /// </summary>
        public async Task<List<KnowledgeEntry>> SearchAsync(string query, Guid userId, CancellationToken cancellationToken = default)
        {
            // Basic search implementation - in a real system, this would use full-text search
            // or a vector database for semantic search
            var normalizedQuery = query.ToLower();

            var results = await _dbContext.KnowledgeEntries
                .Where(ke => ke.UserId == userId &&
                             (ke.Title.ToLower().Contains(normalizedQuery) ||
                              ke.Content.ToLower().Contains(normalizedQuery) ||
                              ke.Tags.Any(t => t.ToLower().Contains(normalizedQuery))))
                .OrderByDescending(ke => ke.RelevanceScore)
                .Take(20) // Limit results
                .ToListAsync(cancellationToken);

            return results;
        }

        /// <summary>
        /// Retrieves knowledge entries related to a specific topic
        /// </summary>
        public async Task<List<KnowledgeEntry>> GetRelatedEntriesAsync(string topic, Guid userId, int limit = 10, CancellationToken cancellationToken = default)
        {
            var normalizedTopic = topic.ToLower();

            var results = await _dbContext.KnowledgeEntries
                .Where(ke => ke.UserId == userId &&
                             (ke.Title.ToLower().Contains(normalizedTopic) ||
                              ke.Content.ToLower().Contains(normalizedTopic) ||
                              ke.Tags.Contains(normalizedTopic)))
                .OrderByDescending(ke => ke.CreatedAt) // Most recent first
                .Take(limit)
                .ToListAsync(cancellationToken);

            return results;
        }

        /// <summary>
        /// Creates a knowledge entry from structured data
        /// </summary>
        public async Task<KnowledgeEntry> CreateEntryAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
        {
            await _dbContext.KnowledgeEntries.AddAsync(entry, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entry;
        }

        /// <summary>
        /// Updates an existing knowledge entry
        /// </summary>
        public async Task UpdateEntryAsync(Guid entryId, KnowledgeEntry updatedEntry, CancellationToken cancellationToken = default)
        {
            var existingEntry = await _dbContext.KnowledgeEntries
                .FirstOrDefaultAsync(ke => ke.Id == entryId, cancellationToken);

            if (existingEntry != null)
            {
                existingEntry.Title = updatedEntry.Title;
                existingEntry.Content = updatedEntry.Content;
                existingEntry.Tags = updatedEntry.Tags;
                existingEntry.MarkAsModified(null); // Set modification timestamp

                _dbContext.KnowledgeEntries.Update(existingEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Deletes a knowledge entry
        /// </summary>
        public async Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
        {
            var entry = await _dbContext.KnowledgeEntries
                .FirstOrDefaultAsync(ke => ke.Id == entryId, cancellationToken);

            if (entry != null)
            {
                _dbContext.KnowledgeEntries.Remove(entry);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Extension methods for configuring knowledge base in Entity Framework
    /// </summary>
    public static class KnowledgeBaseConfiguration
    {
        /// <summary>
        /// Configures the knowledge entry entity
        /// </summary>
        public static void ConfigureKnowledgeBase(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KnowledgeEntry>(entity =>
            {
                entity.HasKey(ke => ke.Id);
                entity.Property(ke => ke.Title).HasMaxLength(500).IsRequired();
                entity.Property(ke => ke.Content).IsRequired();
                entity.Property(ke => ke.SourceType).HasMaxLength(100);
                entity.HasIndex(ke => ke.UserId);
                entity.HasIndex(ke => ke.SourceId);
                entity.HasIndex(ke => ke.CreatedAt);
            });
        }
    }
}