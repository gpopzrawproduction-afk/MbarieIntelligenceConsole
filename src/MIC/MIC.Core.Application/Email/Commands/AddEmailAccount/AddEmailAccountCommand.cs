using MediatR;
using ErrorOr;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Email.Commands.AddEmailAccount;

/// <summary>
/// Command to add a new email account (Gmail, Outlook, IMAP/SMTP, etc.) for a user.
/// </summary>
public record AddEmailAccountCommand : ICommand<Guid>
{
    public Guid UserId { get; init; }
    public string EmailAddress { get; init; } = string.Empty;
    public string? AccountName { get; init; }
    
    // OAuth fields (optional)
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    
    // IMAP/SMTP fields (optional)
    public string? ImapServer { get; init; }
    public int? ImapPort { get; init; }
    public string? SmtpServer { get; init; }
    public int? SmtpPort { get; init; }
    public bool UseSsl { get; init; } = true;
    public string? Password { get; init; }
    
    public string Provider { get; init; } = string.Empty; // "Gmail", "Outlook", or "IMAP/SMTP"
}
