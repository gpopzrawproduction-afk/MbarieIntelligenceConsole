using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Core.Domain.Abstractions;
using Ardalis.GuardClauses;

namespace MIC.Core.Application.Email.Commands.AddEmailAccount;

/// <summary>
/// Handler for adding a new email account.
/// </summary>
public class AddEmailAccountCommandHandler 
    : ICommandHandler<AddEmailAccountCommand, Guid>
{
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddEmailAccountCommandHandler> _logger;

    public AddEmailAccountCommandHandler(
        IEmailAccountRepository emailAccountRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddEmailAccountCommandHandler> logger)
    {
        _emailAccountRepository = emailAccountRepository ?? throw new ArgumentNullException(nameof(emailAccountRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Guid>> Handle(
        AddEmailAccountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing AddEmailAccountCommand for user {UserId}, email {Email}", 
                request.UserId, request.EmailAddress);

            // Validate input
            var validationError = ValidateRequest(request);
            if (validationError is not null)
            {
                return validationError.Value;
            }

            // Check if account already exists for this user
            var existingAccounts = await _emailAccountRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var existingAccount = existingAccounts.FirstOrDefault(a => a.EmailAddress.Equals(request.EmailAddress, StringComparison.OrdinalIgnoreCase));
            
            if (existingAccount != null)
            {
                _logger.LogInformation("Email account {Email} already exists for user {UserId}, updating tokens", 
                    request.EmailAddress, request.UserId);
                
                // Update existing account with new tokens
                UpdateEmailAccountTokens(existingAccount, request);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return existingAccount.Id;
            }

            // Create new email account entity
            var emailAccount = CreateEmailAccountFromCommand(request);
            await _emailAccountRepository.AddAsync(emailAccount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added email account {Email} for user {UserId} with ID {AccountId}", 
                request.EmailAddress, request.UserId, emailAccount.Id);
            
            return emailAccount.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add email account {Email} for user {UserId}", 
                request.EmailAddress, request.UserId);
            return Error.Failure(
                code: "EmailAccount.AddFailed",
                description: $"Failed to add email account: {ex.Message}");
        }
    }

    private Error? ValidateRequest(AddEmailAccountCommand request)
    {
        if (request.UserId == Guid.Empty)
        {
            return Error.Validation(
                code: "EmailAccount.Validation.UserId",
                description: "User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EmailAddress))
        {
            return Error.Validation(
                code: "EmailAccount.Validation.EmailAddress",
                description: "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return Error.Validation(
                code: "EmailAccount.Validation.Provider",
                description: "Provider is required.");
        }

        // Validate email format
        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(request.EmailAddress);
            if (mailAddress.Address != request.EmailAddress)
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.InvalidEmail",
                    description: "Invalid email address format.");
            }
        }
        catch
        {
            return Error.Validation(
                code: "EmailAccount.Validation.InvalidEmail",
                description: "Invalid email address format.");
        }

        // Validate provider
        if (!Enum.TryParse<EmailProvider>(request.Provider, out var providerEnum))
        {
            return Error.Validation(
                code: "EmailAccount.Validation.InvalidProvider",
                description: $"Invalid provider '{request.Provider}'. Must be 'Gmail', 'Outlook', or 'IMAP'.");
        }

        // Validate based on provider type
        if (providerEnum == EmailProvider.IMAP)
        {
            // For IMAP provider, validate IMAP/SMTP credentials
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.Password",
                    description: "Password is required for IMAP accounts.");
            }

            if (string.IsNullOrWhiteSpace(request.ImapServer))
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.ImapServer",
                    description: "IMAP server is required for IMAP accounts.");
            }

            if (string.IsNullOrWhiteSpace(request.SmtpServer))
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.SmtpServer",
                    description: "SMTP server is required for IMAP accounts.");
            }

            if (!request.ImapPort.HasValue || request.ImapPort < 1 || request.ImapPort > 65535)
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.ImapPort",
                    description: "Valid IMAP port (1-65535) is required.");
            }

            if (!request.SmtpPort.HasValue || request.SmtpPort < 1 || request.SmtpPort > 65535)
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.SmtpPort",
                    description: "Valid SMTP port (1-65535) is required.");
            }
        }
        else
        {
            // For OAuth providers (Gmail, Outlook), validate access token
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                return Error.Validation(
                    code: "EmailAccount.Validation.AccessToken",
                    description: "Access token is required for OAuth accounts.");
            }
        }

        return null;
    }

    private EmailAccount CreateEmailAccountFromCommand(AddEmailAccountCommand request)
    {
        Guard.Against.NullOrWhiteSpace(request.EmailAddress, nameof(request.EmailAddress));
        Guard.Against.NullOrWhiteSpace(request.Provider, nameof(request.Provider));
        
        // Parse provider enum
        if (!Enum.TryParse<EmailProvider>(request.Provider, out var provider))
        {
            throw new ArgumentException($"Invalid provider '{request.Provider}'", nameof(request.Provider));
        }

        // Create email account
        var emailAccount = new EmailAccount(
            emailAddress: request.EmailAddress,
            provider: provider,
            userId: request.UserId,
            displayName: request.AccountName ?? request.EmailAddress);

        if (provider == EmailProvider.IMAP)
        {
            // Set IMAP/SMTP credentials
            Guard.Against.NullOrWhiteSpace(request.Password, nameof(request.Password));
            Guard.Against.NullOrWhiteSpace(request.ImapServer, nameof(request.ImapServer));
            Guard.Against.NullOrWhiteSpace(request.SmtpServer, nameof(request.SmtpServer));
            Guard.Against.Null(request.ImapPort, nameof(request.ImapPort));
            Guard.Against.Null(request.SmtpPort, nameof(request.SmtpPort));
            
            emailAccount.SetImapSmtpCredentials(
                imapServer: request.ImapServer!,
                imapPort: request.ImapPort!.Value,
                smtpServer: request.SmtpServer!,
                smtpPort: request.SmtpPort!.Value,
                useSsl: request.UseSsl,
                password: request.Password!);
        }
        else
        {
            // Set OAuth tokens for OAuth providers
            Guard.Against.NullOrWhiteSpace(request.AccessToken, nameof(request.AccessToken));
            
            emailAccount.SetTokens(
                accessToken: request.AccessToken,
                refreshToken: request.RefreshToken,
                expiresAt: request.ExpiresAt ?? DateTime.UtcNow.AddHours(1), // Default 1 hour if not specified
                scopes: GetScopesForProvider(provider));
        }

        return emailAccount;
    }

    private void UpdateEmailAccountTokens(EmailAccount emailAccount, AddEmailAccountCommand request)
    {
        // Parse provider enum
        if (!Enum.TryParse<EmailProvider>(request.Provider, out var provider))
        {
            throw new ArgumentException($"Invalid provider '{request.Provider}'", nameof(request.Provider));
        }

        // Update provider if changed
        if (emailAccount.Provider != provider)
        {
            // In a real implementation, you might want to handle provider changes differently
            _logger.LogWarning("Provider changed from {OldProvider} to {NewProvider} for email {Email}", 
                emailAccount.Provider, provider, request.EmailAddress);
        }

        if (provider == EmailProvider.IMAP)
        {
            // Update IMAP/SMTP credentials
            Guard.Against.NullOrWhiteSpace(request.Password, nameof(request.Password));
            Guard.Against.NullOrWhiteSpace(request.ImapServer, nameof(request.ImapServer));
            Guard.Against.NullOrWhiteSpace(request.SmtpServer, nameof(request.SmtpServer));
            Guard.Against.Null(request.ImapPort, nameof(request.ImapPort));
            Guard.Against.Null(request.SmtpPort, nameof(request.SmtpPort));
            
            emailAccount.SetImapSmtpCredentials(
                imapServer: request.ImapServer!,
                imapPort: request.ImapPort!.Value,
                smtpServer: request.SmtpServer!,
                smtpPort: request.SmtpPort!.Value,
                useSsl: request.UseSsl,
                password: request.Password!);
        }
        else
        {
            // Update OAuth tokens for OAuth providers
            Guard.Against.NullOrWhiteSpace(request.AccessToken, nameof(request.AccessToken));
            
            emailAccount.SetTokens(
                accessToken: request.AccessToken,
                refreshToken: request.RefreshToken,
                expiresAt: request.ExpiresAt ?? DateTime.UtcNow.AddHours(1),
                scopes: GetScopesForProvider(provider));
        }

        // Reactivate account if it was deactivated
        if (!emailAccount.IsActive)
        {
            emailAccount.Activate();
        }
    }

    private string? GetScopesForProvider(EmailProvider provider)
    {
        return provider switch
        {
            EmailProvider.Gmail => "https://www.googleapis.com/auth/gmail.readonly " +
                                  "https://www.googleapis.com/auth/gmail.send " +
                                  "https://www.googleapis.com/auth/gmail.modify",
            EmailProvider.Outlook => "https://graph.microsoft.com/Mail.Read " +
                                    "https://graph.microsoft.com/Mail.Send " +
                                    "https://graph.microsoft.com/Mail.ReadWrite",
            _ => null
        };
    }
}
