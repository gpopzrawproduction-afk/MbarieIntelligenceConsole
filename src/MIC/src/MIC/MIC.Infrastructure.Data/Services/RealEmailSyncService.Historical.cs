using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MIC.Core.Domain.Entities;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace MIC.Infrastructure.Data.Services;

public partial class RealEmailSyncService
{
    public async Task<SyncResult> SyncHistoricalEmailsAsync(
        Guid userId,
        MIC.Core.Domain.Settings.EmailSyncSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting historical email sync for user {UserId}, {Months} months",
                userId, settings.HistoryMonths);

            var syncResult = new SyncResult
            {
                StartTime = DateTimeOffset.UtcNow,
                UserId = userId
            };

            // Get user's email accounts
            var accounts = await _emailAccountRepository.GetByUserIdAsync(userId, cancellationToken);

            if (accounts == null || !accounts.Any())
            {
                _logger.LogWarning("No email accounts configured for user {UserId}", userId);
                syncResult.Status = SyncStatus.NoAccountsConfigured;
                return syncResult;
            }

            // Calculate date range
            var sinceDate = settings.HistoryMonths == 0
                ? DateTime.MinValue
                : DateTime.UtcNow.AddMonths(-settings.HistoryMonths);

            _logger.LogInformation("Syncing emails since {SinceDate}", sinceDate);

            // Sync each account
            foreach (var account in accounts)
            {
                try
                {
                    // Connect to IMAP
                    using var client = new ImapClient();
                    var (host, port) = GetImapSettings(account);
                    var secureSocketOptions = GetSecureSocketOptions(account);
                    await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
                    if (account.Provider == EmailProvider.IMAP)
                    {
                        var password = account.PasswordEncrypted; // should be decrypted
                        await client.AuthenticateAsync(account.EmailAddress, password, cancellationToken);
                    }
                    else
                    {
                        var accessToken = await GetAccessTokenAsync(account, cancellationToken);
                        var oauth2 = new SaslMechanismOAuth2(account.EmailAddress, accessToken);
                        await client.AuthenticateAsync(oauth2, cancellationToken);
                    }

                    // Sync Inbox
                    var inbox = client.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                    var query = settings.HistoryMonths == 0
                        ? SearchQuery.All
                        : SearchQuery.DeliveredAfter(sinceDate);

                    var uids = await inbox.SearchAsync(query, cancellationToken);

                    _logger.LogInformation("Found {Count} emails in inbox since {Date}", uids.Count, sinceDate);

                    syncResult.TotalEmailsFound += uids.Count;

                    // Download emails
                    foreach (var uid in uids.Take(1000)) // Limit to prevent timeout
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var message = await inbox.GetMessageAsync(uid, cancellationToken);

                        var email = new Email
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            EmailAccountId = account.Id,
                            MessageId = message.MessageId,
                            From = message.From.ToString(),
                            To = message.To.ToString(),
                            Subject = message.Subject,
                            Body = message.TextBody ?? message.HtmlBody,
                            ReceivedDate = message.Date.DateTime,
                            IsRead = false,
                            HasAttachments = message.Attachments.Any(),
                            Folder = "Inbox"
                        };

                        if (settings.DownloadAttachments && message.Attachments.Any())
                        {
                            email.AttachmentCount = message.Attachments.Count();
                            // TODO: store attachments
                        }

                        await _emailRepository.AddAsync(email);
                        syncResult.EmailsSynced++;
                    }

                    // Sync Sent folder if enabled
                    if (settings.IncludeSentFolder)
                    {
                        var sentFolder = client.GetFolder(SpecialFolder.Sent);
                        await sentFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
                        var sentUids = await sentFolder.SearchAsync(query, cancellationToken);
                        _logger.LogInformation("Found {Count} sent emails", sentUids.Count);
                        syncResult.TotalEmailsFound += sentUids.Count;
                        foreach (var uid in sentUids.Take(1000))
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            var message = await sentFolder.GetMessageAsync(uid, cancellationToken);
                            var email = new Email
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                EmailAccountId = account.Id,
                                MessageId = message.MessageId,
                                From = message.From.ToString(),
                                To = message.To.ToString(),
                                Subject = message.Subject,
                                Body = message.TextBody ?? message.HtmlBody,
                                ReceivedDate = message.Date.DateTime,
                                IsRead = false,
                                HasAttachments = message.Attachments.Any(),
                                Folder = "Sent"
                            };

                            if (settings.DownloadAttachments && message.Attachments.Any())
                            {
                                email.AttachmentCount = message.Attachments.Count();
                            }

                            await _emailRepository.AddAsync(email);
                            syncResult.EmailsSynced++;
                        }
                    }

                    await client.DisconnectAsync(true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing account {Email}", account.EmailAddress);
                    syncResult.Errors.Add($"{account.EmailAddress}: {ex.Message}");
                }
            }

            syncResult.EndTime = DateTimeOffset.UtcNow;
            syncResult.Status = SyncStatus.Completed;

            _logger.LogInformation("Historical sync complete: {Synced}/{Total} emails", syncResult.EmailsSynced, syncResult.TotalEmailsFound);

            return syncResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Historical email sync failed");
            throw;
        }
    }

    public class SyncResult
    {
        public Guid UserId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int TotalEmailsFound { get; set; }
        public int EmailsSynced { get; set; }
        public SyncStatus Status { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public enum SyncStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        NoAccountsConfigured
    }
}
