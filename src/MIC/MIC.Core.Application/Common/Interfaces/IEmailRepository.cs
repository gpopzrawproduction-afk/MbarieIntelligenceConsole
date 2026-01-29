using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for email operations.
/// </summary>
public interface IEmailRepository : IRepository<EmailMessage>
{
    /// <summary>
    /// Gets emails for a user with optional filtering.
    /// </summary>
    Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(
        Guid userId,
        Guid? emailAccountId = null,
        EmailFolder? folder = null,
        bool? isUnread = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an email by its external message ID.
    /// </summary>
    Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets emails in a conversation thread.
    /// </summary>
    Task<IReadOnlyList<EmailMessage>> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of unread emails for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, Guid? emailAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of emails requiring response.
    /// </summary>
    Task<int> GetRequiresResponseCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks multiple emails as read.
    /// </summary>
    Task MarkAsReadAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email with the given message ID already exists.
    /// </summary>
    Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for email account operations.
/// </summary>
public interface IEmailAccountRepository : IRepository<EmailAccount>
{
    /// <summary>
    /// Gets all email accounts for a user.
    /// </summary>
    Task<IReadOnlyList<EmailAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary email account for a user.
    /// </summary>
    Task<EmailAccount?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts that need syncing.
    /// </summary>
    Task<IReadOnlyList<EmailAccount>> GetAccountsNeedingSyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email address is already connected.
    /// </summary>
    Task<bool> IsEmailConnectedAsync(string emailAddress, Guid userId, CancellationToken cancellationToken = default);
}
