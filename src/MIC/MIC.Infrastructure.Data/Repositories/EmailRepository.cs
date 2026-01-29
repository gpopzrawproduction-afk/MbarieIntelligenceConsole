using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailMessage entities.
/// </summary>
public class EmailRepository : Repository<EmailMessage>, IEmailRepository
{
    public EmailRepository(MicDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailMessage>> GetEmailsAsync(
        Guid userId,
        Guid? emailAccountId = null,
        EmailFolder? folder = null,
        bool? isUnread = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmailMessages
            .Include(e => e.Attachments)
            .Where(e => e.UserId == userId)
            .AsQueryable();

        if (emailAccountId.HasValue)
            query = query.Where(e => e.EmailAccountId == emailAccountId.Value);

        if (folder.HasValue)
            query = query.Where(e => e.Folder == folder.Value);

        if (isUnread.HasValue)
            query = query.Where(e => e.IsRead == !isUnread.Value);

        return await query
            .OrderByDescending(e => e.ReceivedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailMessage?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailMessage>> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .Include(e => e.Attachments)
            .Where(e => e.ConversationId == conversationId)
            .OrderBy(e => e.SentDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, Guid? emailAccountId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmailMessages.Where(e => e.UserId == userId && !e.IsRead);

        if (emailAccountId.HasValue)
            query = query.Where(e => e.EmailAccountId == emailAccountId.Value);

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetRequiresResponseCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages
            .Where(e => e.UserId == userId && e.RequiresResponse && !e.IsRead)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default)
    {
        var emails = await _context.EmailMessages
            .Where(e => emailIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var email in emails)
        {
            email.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailMessages.AnyAsync(e => e.MessageId == messageId, cancellationToken);
    }
}

/// <summary>
/// Repository implementation for EmailAccount entities.
/// </summary>
public class EmailAccountRepository : Repository<EmailAccount>, IEmailAccountRepository
{
    public EmailAccountRepository(MicDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailAccounts
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.IsPrimary)
            .ThenBy(e => e.EmailAddress)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailAccount?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailAccounts
            .FirstOrDefaultAsync(e => e.UserId == userId && e.IsPrimary, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailAccount>> GetAccountsNeedingSyncAsync(CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow;
        
        // Get all active accounts and filter in memory for SQLite compatibility
        var accounts = await _context.EmailAccounts
            .Where(e => e.IsActive && e.Status != SyncStatus.InProgress)
            .ToListAsync(cancellationToken);

        return accounts
            .Where(e => e.LastSyncedAt == null || 
                        (cutoffTime - e.LastSyncedAt.Value).TotalMinutes >= e.SyncIntervalMinutes)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailConnectedAsync(string emailAddress, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EmailAccounts
            .AnyAsync(e => e.EmailAddress.ToLower() == emailAddress.ToLower() && e.UserId == userId, cancellationToken);
    }
}
