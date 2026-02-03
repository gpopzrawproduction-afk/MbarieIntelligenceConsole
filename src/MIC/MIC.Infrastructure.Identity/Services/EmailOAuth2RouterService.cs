using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MIC.Infrastructure.Identity.Services;

public sealed class EmailOAuth2RouterService : IEmailOAuth2Service
{
    private readonly IServiceProvider _serviceProvider;

    public EmailOAuth2RouterService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<OAuthResult> AuthenticateGmailAsync(CancellationToken cancellationToken = default)
        => GetRequiredService("Gmail").AuthenticateGmailAsync(cancellationToken);

    public Task<OAuthResult> AuthenticateOutlookAsync(CancellationToken cancellationToken = default)
        => GetRequiredService("Outlook").AuthenticateOutlookAsync(cancellationToken);

    public Task<string?> RefreshGmailTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => GetRequiredService("Gmail").RefreshGmailTokenAsync(refreshToken, cancellationToken);

    public Task<string?> RefreshOutlookTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        => GetRequiredService("Outlook").RefreshOutlookTokenAsync(refreshToken, cancellationToken);

    public Task<bool> ValidateTokenAsync(string accessToken, EmailProvider provider)
        => provider switch
        {
            EmailProvider.Gmail => GetRequiredService("Gmail").ValidateTokenAsync(accessToken, provider),
            EmailProvider.Outlook => GetRequiredService("Outlook").ValidateTokenAsync(accessToken, provider),
            _ => Task.FromResult(false)
        };

    public Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
        => GetRequiredService("Gmail").GetGmailAccessTokenAsync(userEmail, ct);

    public Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
        => GetRequiredService("Outlook").GetOutlookAccessTokenAsync(userEmail, ct);

    public Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
        => GetRequiredService("Gmail").AuthorizeGmailAccountAsync(ct);

    public Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
        => GetRequiredService("Outlook").AuthorizeOutlookAccountAsync(ct);

    private IEmailOAuth2Service GetRequiredService(string key)
    {
        var service = _serviceProvider.GetKeyedService<IEmailOAuth2Service>(key);
        if (service is null)
        {
            throw new InvalidOperationException($"IEmailOAuth2Service '{key}' is not registered.");
        }

        return service;
    }
}
