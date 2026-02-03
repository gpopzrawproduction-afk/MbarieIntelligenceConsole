using System.Threading;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

public interface IEmailOAuth2Service
{
    // New unified OAuth methods
    Task<OAuthResult> AuthenticateGmailAsync(CancellationToken cancellationToken = default);
    Task<OAuthResult> AuthenticateOutlookAsync(CancellationToken cancellationToken = default);
    Task<string?> RefreshGmailTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<string?> RefreshOutlookTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string accessToken, MIC.Core.Domain.Entities.EmailProvider provider);
    
    // Legacy methods for compatibility with existing code
    Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default);
    Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default);
    Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default);
    Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default);
}

public record OAuthResult
{
    public bool Success { get; init; }
    public string? EmailAddress { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public MIC.Core.Domain.Entities.EmailProvider Provider { get; init; }
    public string? ErrorMessage { get; init; }
}
