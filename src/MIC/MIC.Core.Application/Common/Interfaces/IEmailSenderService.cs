using System.Threading;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Service for sending emails via SMTP.
/// </summary>
public interface IEmailSenderService
{
    /// <summary>
    /// Sends an email using the specified email account's SMTP settings.
    /// </summary>
    /// <param name="emailAccountId">The ID of the email account to use for sending.</param>
    /// <param name="to">Recipient email addresses (comma-separated).</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body (plain text or HTML).</param>
    /// <param name="cc">CC recipients (optional, comma-separated).</param>
    /// <param name="bcc">BCC recipients (optional, comma-separated).</param>
    /// <param name="isHtml">Whether the body is HTML.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<EmailSendResult> SendEmailAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        string? cc = null,
        string? bcc = null,
        bool isHtml = false,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an email with attachments.
    /// </summary>
    Task<EmailSendResult> SendEmailWithAttachmentsAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        IEnumerable<EmailAttachment> attachments,
        string? cc = null,
        string? bcc = null,
        bool isHtml = false,
        CancellationToken ct = default);

    /// <summary>
    /// Replies to an existing email.
    /// </summary>
    Task<EmailSendResult> ReplyToEmailAsync(
        Guid emailAccountId,
        Guid replyToEmailId,
        string body,
        bool replyToAll = false,
        bool isHtml = false,
        CancellationToken ct = default);

    /// <summary>
    /// Forwards an existing email.
    /// </summary>
    Task<EmailSendResult> ForwardEmailAsync(
        Guid emailAccountId,
        Guid forwardEmailId,
        string to,
        string? cc = null,
        string? bcc = null,
        string? additionalMessage = null,
        bool isHtml = false,
        CancellationToken ct = default);
}

/// <summary>
/// Result of an email send operation.
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Represents an email attachment for sending.
/// </summary>
public class EmailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "application/octet-stream";
}