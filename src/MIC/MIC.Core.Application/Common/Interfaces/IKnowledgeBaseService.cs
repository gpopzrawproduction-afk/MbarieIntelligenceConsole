using MIC.Core.Domain.Entities;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for knowledge base services to store and retrieve information
    /// from emails and attachments
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// Indexes an email attachment in the knowledge base
        /// </summary>
        /// <param name="attachment">The attachment to index</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task IndexAttachmentAsync(MIC.Core.Domain.Entities.EmailAttachment attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes an email message in the knowledge base
        /// </summary>
        /// <param name="emailMessage">The email message to index</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task IndexEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the knowledge base for relevant information
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="userId">User ID to limit search scope</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of relevant knowledge entries</returns>
        Task<List<KnowledgeEntry>> SearchAsync(string query, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves knowledge entries related to a specific topic
        /// </summary>
        /// <param name="topic">Topic to search for</param>
        /// <param name="userId">User ID to limit search scope</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of relevant knowledge entries</returns>
        Task<List<KnowledgeEntry>> GetRelatedEntriesAsync(string topic, Guid userId, int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a knowledge entry from structured data
        /// </summary>
        /// <param name="entry">Knowledge entry to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created entry with assigned ID</returns>
        Task<KnowledgeEntry> CreateEntryAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing knowledge entry
        /// </summary>
        /// <param name="entryId">ID of the entry to update</param>
        /// <param name="updatedEntry">Updated entry data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateEntryAsync(Guid entryId, KnowledgeEntry updatedEntry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a knowledge entry
        /// </summary>
        /// <param name="entryId">ID of the entry to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an entry in the knowledge base
    /// </summary>
    public class KnowledgeEntry : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FullContent { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty; // Email, Attachment, etc.
        public Guid SourceId { get; set; } // ID of the source entity
        public List<string> Tags { get; set; } = new();
        public string? AISummary { get; set; }
        public double RelevanceScore { get; set; } = 0.0;
        public DateTime? LastAccessed { get; set; }
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? ContentType { get; set; }

        public void UpdateAccessTime()
        {
            LastAccessed = DateTime.UtcNow;
        }

        public void UpdateRelevanceScore(double score)
        {
            RelevanceScore = score;
        }
    }
}