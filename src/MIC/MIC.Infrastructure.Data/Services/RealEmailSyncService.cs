using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MIC.Core.Domain.Entities;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Infrastructure.Identity.Services;
using MIC.Infrastructure.AI.Services;

namespace MIC.Infrastructure.Data.Services;

public class RealEmailSyncService : IEmailSyncService
{
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IEmailOAuth2Service _oauth2Service;
    private readonly IEmailAnalysisService _analysisService;
    private readonly ILogger<RealEmailSyncService> _logger;
    private readonly IConfiguration _configuration;

    public RealEmailSyncService(
        IEmailRepository emailRepository,
        IEmailAccountRepository emailAccountRepository,
        IEmailOAuth2Service oauth2Service,
        IEmailAnalysisService analysisService, // ADD THIS
        ILogger<RealEmailSyncService> logger,
        IConfiguration configuration)
    {
        _emailRepository = emailRepository;
        _emailAccountRepository = emailAccountRepository;
        _oauth2Service = oauth2Service;
        _analysisService = analysisService; // ADD THIS
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<EmailSyncResult> SyncAccountAsync(
        EmailAccount account, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting sync for account: {Email} (Provider: {Provider})", 
            account.EmailAddress, account.Provider);

        try
        {
            using var client = new ImapClient();
            
            // Connect to IMAP server
            var (host, port) = GetImapSettings(account);
            var secureSocketOptions = GetSecureSocketOptions(account);
            await client.ConnectAsync(host, port, secureSocketOptions, ct);
            
            // Authenticate based on provider
            if (account.Provider == EmailProvider.IMAP)
            {
                // For IMAP provider, use password authentication
                if (string.IsNullOrEmpty(account.PasswordEncrypted))
                {
                    throw new InvalidOperationException("Password is required for IMAP provider");
                }
                
                // In production, the password should be decrypted here
                var password = account.PasswordEncrypted; // Should be decrypted
                await client.AuthenticateAsync(account.EmailAddress, password, ct);
            }
            else
            {
                // For OAuth2 providers (Gmail, Outlook)
                var accessToken = await GetAccessTokenAsync(account, ct);
                var oauth2 = new SaslMechanismOAuth2(account.EmailAddress, accessToken);
                await client.AuthenticateAsync(oauth2, ct);
            }
            
            _logger.LogInformation("Connected to IMAP server for {Email}", account.EmailAddress);

            // Open INBOX
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
            
            // Fetch recent emails (last 3 months)
            var syncMonths = int.Parse(_configuration["EmailSync:InitialSyncMonths"] ?? "3");
            var cutoffDate = DateTime.UtcNow.AddMonths(-syncMonths);
            
            var query = SearchQuery.DeliveredAfter(cutoffDate);
            var uids = await inbox.SearchAsync(query, ct);
            
            _logger.LogInformation("Found {Count} emails to sync", uids.Count);

            int newCount = 0;
            int processedCount = 0;

            foreach (var uid in uids)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var message = await inbox.GetMessageAsync(uid, ct);
                    
                    // Check if already exists
                    if (await _emailRepository.ExistsAsync(message.MessageId, ct))
                    {
                        _logger.LogDebug("Email {MessageId} already exists, skipping", message.MessageId);
                        continue;
                    }

                    // Convert to EmailMessage entity
                    var emailMessage = ConvertToEntity(message, account);

                    // ADD REAL-TIME AI ANALYSIS
                    try
                    {
                        var analysis = await _analysisService.AnalyzeEmailAsync(emailMessage, ct);
                        
                        // Apply AI analysis results
                        emailMessage.SetInboxFlags(analysis.Priority, analysis.IsUrgent, false, false, analysis.ActionItems.Any());
                        
                        _logger.LogInformation("AI analyzed email: {Subject} - Priority: {Priority}, Urgent: {IsUrgent}",
                            emailMessage.Subject, analysis.Priority, analysis.IsUrgent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AI analysis failed, using defaults");
                    }

                    // Save to database
                    await _emailRepository.AddAsync(emailMessage, ct);
                    
                    newCount++;
                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        _logger.LogInformation("Processed {Count}/{Total} emails", processedCount, uids.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email UID {Uid}", uid);
                    // Continue with next email
                }
            }

            // Update account sync status
            account.UpdateSyncStatus(SyncStatus.Completed, newCount);
            await _emailAccountRepository.UpdateAsync(account, ct);

            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Sync completed: {New} new emails out of {Total} checked", 
                newCount, uids.Count);

            return new EmailSyncResult
            {
                Success = true,
                NewEmailsCount = newCount,
                TotalEmailsChecked = uids.Count,
                SyncedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sync failed for {Email}", account.EmailAddress);
            account.SetSyncFailed(ex.Message);
            await _emailAccountRepository.UpdateAsync(account, ct);
            return new EmailSyncResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SyncedAt = DateTime.UtcNow
            };
        }
    }

    private EmailMessage ConvertToEntity(MimeMessage message, EmailAccount account)
    {
        var toRecipients = string.Join("; ", message.To.Mailboxes.Select(m => m.Address));
        var ccRecipients = string.Join("; ", message.Cc.Mailboxes.Select(m => m.Address));
        var bccRecipients = string.Join("; ", message.Bcc.Mailboxes.Select(m => m.Address));

        var emailMessage = new EmailMessage(
            message.MessageId,
            message.Subject ?? "(No Subject)",
            message.From.Mailboxes.FirstOrDefault()?.Address ?? "",
            message.From.Mailboxes.FirstOrDefault()?.Name ?? "",
            toRecipients,
            message.Date.UtcDateTime,
            message.Date.UtcDateTime,
            message.TextBody ?? message.HtmlBody ?? "",
            account.UserId,
            account.Id,
            EmailFolder.Inbox
        );

        // Set additional properties
        emailMessage.SetHtmlBody(message.HtmlBody);
        emailMessage.MoveToFolder(EmailFolder.Inbox);

        return emailMessage;
    }

    private async Task<string> GetAccessTokenAsync(EmailAccount account, CancellationToken ct)
    {
        return account.Provider switch
        {
            EmailProvider.Gmail => await _oauth2Service.GetGmailAccessTokenAsync(account.EmailAddress, ct),
            EmailProvider.Outlook => await _oauth2Service.GetOutlookAccessTokenAsync(account.EmailAddress, ct),
            _ => throw new NotSupportedException($"Provider {account.Provider} not supported")
        };
    }

    private (string host, int port) GetImapSettings(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            if (string.IsNullOrEmpty(account.ImapServer))
            {
                throw new InvalidOperationException("IMAP server is not configured for this account");
            }
            
            return (account.ImapServer, account.ImapPort > 0 ? account.ImapPort : 993);
        }
        
        // For OAuth providers, use predefined settings
        return account.Provider switch
        {
            EmailProvider.Gmail => ("imap.gmail.com", 993),
            EmailProvider.Outlook => ("outlook.office365.com", 993),
            EmailProvider.Exchange => throw new NotSupportedException("Exchange provider not yet implemented"),
            _ => throw new NotSupportedException($"Provider {account.Provider} not supported")
        };
    }

    private SecureSocketOptions GetSecureSocketOptions(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            return account.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
        }
        
        // OAuth providers always use SSL
        return SecureSocketOptions.SslOnConnect;
    }
}
