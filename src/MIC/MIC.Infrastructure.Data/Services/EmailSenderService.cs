using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using EmailAttachmentDto = MIC.Core.Application.Common.Interfaces.EmailAttachment;
using System.Net;
using System.Text.RegularExpressions;

namespace MIC.Infrastructure.Data.Services;

/// <summary>
/// Service for sending emails via SMTP using MailKit.
/// </summary>
public class EmailSenderService : IEmailSenderService
{
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(
        IEmailAccountRepository emailAccountRepository,
        IEmailRepository emailRepository,
        ILogger<EmailSenderService> logger)
    {
        _emailAccountRepository = emailAccountRepository;
        _emailRepository = emailRepository;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendEmailAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        string? cc = null,
        string? bcc = null,
        bool isHtml = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Sending email from account {AccountId} to {To}", emailAccountId, to);

        try
        {
            // Get the email account
            var account = await _emailAccountRepository.GetByIdAsync(emailAccountId, ct);
            if (account == null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email account with ID {emailAccountId} not found."
                };
            }

            // Validate account is active and has SMTP settings
            if (!account.IsActive)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email account {account.EmailAddress} is not active."
                };
            }

            // Create and send the message
            var message = CreateMimeMessage(account, to, subject, body, cc, bcc, isHtml);
            return await SendMessageAsync(account, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email from account {AccountId} to {To}", emailAccountId, to);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailSendResult> SendEmailWithAttachmentsAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        IEnumerable<EmailAttachmentDto> attachments,
        string? cc = null,
        string? bcc = null,
        bool isHtml = false,
        CancellationToken ct = default)
    {
        var attachmentList = attachments?.ToList() ?? new List<EmailAttachmentDto>();

        _logger.LogInformation("Sending email with {Count} attachments from account {AccountId} to {To}", 
            attachmentList.Count, emailAccountId, to);

        try
        {
            // Get the email account
            var account = await _emailAccountRepository.GetByIdAsync(emailAccountId, ct);
            if (account == null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email account with ID {emailAccountId} not found."
                };
            }

            if (!account.IsActive)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email account {account.EmailAddress} is not active."
                };
            }

            // Create the message
            var message = CreateMimeMessage(account, to, subject, body, cc, bcc, isHtml);

            // Add attachments
            foreach (var attachment in attachmentList)
            {
                var mimePart = new MimePart(attachment.ContentType)
                {
                    Content = new MimeContent(new MemoryStream(attachment.Content)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = attachment.FileName
                };
                
                if (message.Body is Multipart multipart)
                {
                    multipart.Add(mimePart);
                }
                else
                {
                    var mixedMultipart = new Multipart("mixed");
                    if (message.Body != null)
                        mixedMultipart.Add(message.Body);
                    mixedMultipart.Add(mimePart);
                    message.Body = mixedMultipart;
                }
            }

            return await SendMessageAsync(account, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments from account {AccountId}", emailAccountId);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailSendResult> ReplyToEmailAsync(
        Guid emailAccountId,
        Guid replyToEmailId,
        string body,
        bool replyToAll = false,
        bool isHtml = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Replying to email {EmailId} from account {AccountId}", replyToEmailId, emailAccountId);

        try
        {
            // Get the original email
            var originalEmail = await _emailRepository.GetByIdAsync(replyToEmailId, ct);
            if (originalEmail == null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email with ID {replyToEmailId} not found."
                };
            }

            // Get the email account
            var account = await _emailAccountRepository.GetByIdAsync(emailAccountId, ct);
            if (account == null || !account.IsActive)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = account == null 
                        ? $"Email account with ID {emailAccountId} not found."
                        : $"Email account {account.EmailAddress} is not active."
                };
            }

            // Create reply message
            var replyBody = $"{body}\n\n--- Original Message ---\n" +
                           $"From: {originalEmail.FromName} <{originalEmail.FromAddress}>\n" +
                           $"To: {originalEmail.ToRecipients}\n" +
                           $"Date: {originalEmail.SentDate:yyyy-MM-dd HH:mm}\n" +
                           $"Subject: {originalEmail.Subject}\n\n" +
                           $"{originalEmail.BodyText}";

            string toRecipients;
            if (replyToAll)
            {
                // Include all original recipients
                var allRecipients = new List<string> { originalEmail.FromAddress };
                if (!string.IsNullOrEmpty(originalEmail.CcRecipients))
                    allRecipients.AddRange(originalEmail.CcRecipients.Split(';', ',').Select(r => r.Trim()));
                toRecipients = string.Join(",", allRecipients);
            }
            else
            {
                // Reply only to sender
                toRecipients = originalEmail.FromAddress;
            }

            var subject = originalEmail.Subject.StartsWith("Re: ", StringComparison.OrdinalIgnoreCase)
                ? originalEmail.Subject
                : $"Re: {originalEmail.Subject}";

            var message = CreateMimeMessage(account, toRecipients, subject, replyBody, isHtml: isHtml);
            
            // Add In-Reply-To and References headers for threading
            message.Headers.Add("In-Reply-To", $"<{originalEmail.MessageId}>");
            message.Headers.Add("References", $"<{originalEmail.MessageId}>");

            return await SendMessageAsync(account, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reply to email {EmailId}", replyToEmailId);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailSendResult> ForwardEmailAsync(
        Guid emailAccountId,
        Guid forwardEmailId,
        string to,
        string? cc = null,
        string? bcc = null,
        string? additionalMessage = null,
        bool isHtml = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Forwarding email {EmailId} from account {AccountId} to {To}", 
            forwardEmailId, emailAccountId, to);

        try
        {
            // Get the original email
            var originalEmail = await _emailRepository.GetByIdAsync(forwardEmailId, ct);
            if (originalEmail == null)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = $"Email with ID {forwardEmailId} not found."
                };
            }

            // Get the email account
            var account = await _emailAccountRepository.GetByIdAsync(emailAccountId, ct);
            if (account == null || !account.IsActive)
            {
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = account == null 
                        ? $"Email account with ID {emailAccountId} not found."
                        : $"Email account {account.EmailAddress} is not active."
                };
            }

            // Create forward message
            var forwardBody = additionalMessage != null
                ? $"{additionalMessage}\n\n--- Forwarded Message ---\n"
                : "--- Forwarded Message ---\n";

            forwardBody += $"From: {originalEmail.FromName} <{originalEmail.FromAddress}>\n" +
                          $"To: {originalEmail.ToRecipients}\n" +
                          $"Date: {originalEmail.SentDate:yyyy-MM-dd HH:mm}\n" +
                          $"Subject: {originalEmail.Subject}\n\n" +
                          $"{originalEmail.BodyText}";

            var subject = originalEmail.Subject.StartsWith("Fwd: ", StringComparison.OrdinalIgnoreCase) ||
                          originalEmail.Subject.StartsWith("FW: ", StringComparison.OrdinalIgnoreCase)
                ? originalEmail.Subject
                : $"Fwd: {originalEmail.Subject}";

            var message = CreateMimeMessage(account, to, subject, forwardBody, cc, bcc, isHtml);
            return await SendMessageAsync(account, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward email {EmailId}", forwardEmailId);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    private MimeMessage CreateMimeMessage(
        EmailAccount account,
        string to,
        string subject,
        string body,
        string? cc = null,
        string? bcc = null,
        bool isHtml = false)
    {
        var message = new MimeMessage();

        // Set From
        message.From.Add(new MailboxAddress(account.DisplayName ?? account.EmailAddress, account.EmailAddress));

        // Set To recipients
        foreach (var recipient in to.Split(',', ';').Select(r => r.Trim()))
        {
            if (!string.IsNullOrWhiteSpace(recipient))
                message.To.Add(MailboxAddress.Parse(recipient));
        }

        // Set CC recipients
        if (!string.IsNullOrWhiteSpace(cc))
        {
            foreach (var recipient in cc.Split(',', ';').Select(r => r.Trim()))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                    message.Cc.Add(MailboxAddress.Parse(recipient));
            }
        }

        // Set BCC recipients
        if (!string.IsNullOrWhiteSpace(bcc))
        {
            foreach (var recipient in bcc.Split(',', ';').Select(r => r.Trim()))
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                    message.Bcc.Add(MailboxAddress.Parse(recipient));
            }
        }

        if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient is required.");
        }

        // Set subject
        message.Subject = subject;

        // Set body
        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
            bodyBuilder.TextBody = HtmlToText(body);
        }
        else
        {
            bodyBuilder.TextBody = body;
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Add headers
        message.Headers.Add("X-Mailer", "MIC Intelligence Console");
        message.Headers.Add("Date", DateTimeOffset.UtcNow.ToString("r"));

        return message;
    }

    private async Task<EmailSendResult> SendMessageAsync(EmailAccount account, MimeMessage message, CancellationToken ct)
    {
        var (smtpHost, smtpPort) = GetSmtpSettings(account);
        var secureSocketOptions = GetSecureSocketOptions(account);

        using var client = new SmtpClient();
        
        try
        {
            // Connect to SMTP server
            _logger.LogDebug("Connecting to SMTP server {Host}:{Port}", smtpHost, smtpPort);
            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions, ct);

            // Authenticate
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
                // For OAuth2 providers (Gmail, Outlook) - currently not implemented for SMTP
                // This would require implementing OAuth2 for SMTP which is more complex
                // For now, we'll use the same password approach or fail
                throw new NotSupportedException($"OAuth2 SMTP authentication for {account.Provider} not yet implemented");
            }

            // Send the message
            _logger.LogDebug("Sending email: {Subject}", message.Subject);
            var messageId = await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully from {From} to {To}", 
                account.EmailAddress, string.Join(",", message.To));

            return new EmailSendResult
            {
                Success = true,
                MessageId = messageId,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP");
            throw;
        }
    }

    private (string host, int port) GetSmtpSettings(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            if (string.IsNullOrEmpty(account.SmtpServer))
            {
                throw new InvalidOperationException("SMTP server is not configured for this account");
            }
            
            return (account.SmtpServer, account.SmtpPort > 0 ? account.SmtpPort : 465);
        }
        
        // For OAuth providers, use predefined settings
        return account.Provider switch
        {
            EmailProvider.Gmail => ("smtp.gmail.com", 587), // Use port 587 for Gmail (STARTTLS)
            EmailProvider.Outlook => ("smtp.office365.com", 587), // Use port 587 for Outlook (STARTTLS)
            EmailProvider.Exchange => throw new NotSupportedException("Exchange provider not yet implemented for SMTP"),
            _ => throw new NotSupportedException($"Provider {account.Provider} not supported")
        };
    }

    private SecureSocketOptions GetSecureSocketOptions(EmailAccount account)
    {
        if (account.Provider == EmailProvider.IMAP)
        {
            return account.UseSsl 
                ? SecureSocketOptions.SslOnConnect 
                : SecureSocketOptions.StartTlsWhenAvailable;
        }
        
        // OAuth providers use STARTTLS on port 587
        // For Gmail/Outlook we use port 587 with STARTTLS
        return SecureSocketOptions.StartTlsWhenAvailable;
    }

    private string HtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var withLineBreaks = Regex.Replace(html, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*/\s*p\s*>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*p\s*>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*/\s*div\s*>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*div\s*>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*/\s*li\s*>", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(withLineBreaks, @"<\s*li\s*>", "- ", RegexOptions.IgnoreCase);

        var decoded = WebUtility.HtmlDecode(withLineBreaks);
        var stripped = Regex.Replace(decoded, @"<.*?>", string.Empty);

        return stripped.Trim();
    }
}
