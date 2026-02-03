using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MIC.Infrastructure.Identity.Services;

public class GmailOAuthService : IEmailOAuth2Service
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailOAuthService> _logger;
    private readonly string[] _scopes = new[]
    {
        GmailService.Scope.GmailReadonly,
        GmailService.Scope.GmailSend,
        GmailService.Scope.GmailModify
    };
    private readonly string _tokenStoragePath;

    public GmailOAuthService(
        IConfiguration configuration,
        ILogger<GmailOAuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MIC", "tokens", "gmail");
        Directory.CreateDirectory(_tokenStoragePath);
    }

    public async Task<OAuthResult> AuthenticateGmailAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Gmail:ClientId"];
            var clientSecret = _configuration["OAuth2:Gmail:ClientSecret"];
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Gmail OAuth credentials not configured");
                return new OAuthResult
                {
                    Success = false,
                    ErrorMessage = "Gmail OAuth credentials not configured. Please configure ClientId and ClientSecret in appsettings.json.",
                    Provider = MIC.Core.Domain.Entities.EmailProvider.Gmail
                };
            }
            
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };
            
            // Start OAuth flow with embedded browser
            _logger.LogInformation("Starting Gmail OAuth flow");
            
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                _scopes,
                "user",
                cancellationToken,
                new FileDataStore(_tokenStoragePath, true)
            );
            
            // Get user email
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mbarie Intelligence Console"
            });
            
            var profile = await service.Users.GetProfile("me").ExecuteAsync(cancellationToken);
            
            _logger.LogInformation("Gmail authentication successful for {Email}", profile.EmailAddress);
            
            return new OAuthResult
            {
                Success = true,
                EmailAddress = profile.EmailAddress,
                AccessToken = credential.Token.AccessToken,
                RefreshToken = credential.Token.RefreshToken,
                ExpiresAt = credential.Token.IssuedUtc.AddSeconds(
                    credential.Token.ExpiresInSeconds ?? 3600),
                Provider = MIC.Core.Domain.Entities.EmailProvider.Gmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail authentication failed");
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = MIC.Core.Domain.Entities.EmailProvider.Gmail
            };
        }
    }

    public async Task<string?> RefreshGmailTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Gmail:ClientId"];
            var clientSecret = _configuration["OAuth2:Gmail:ClientSecret"];
            
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = _scopes
                });
            
            var tokenResponse = await flow.RefreshTokenAsync(
                "user",
                refreshToken,
                cancellationToken);
            
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail token refresh failed");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(
        string accessToken,
        MIC.Core.Domain.Entities.EmailProvider provider)
    {
        if (provider != MIC.Core.Domain.Entities.EmailProvider.Gmail) return false;
        
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Mbarie Intelligence Console"
            });
            
            // Try to get profile to validate token
            var profile = await service.Users.GetProfile("me").ExecuteAsync();
            return !string.IsNullOrEmpty(profile.EmailAddress);
        }
        catch
        {
            return false;
        }
    }

    // Outlook methods - not implemented in this service
    public Task<OAuthResult> AuthenticateOutlookAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use OutlookOAuthService");
    }
    
    public Task<string?> RefreshOutlookTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use OutlookOAuthService");
    }

    // Helper method to get stored tokens for a specific user
    public async Task<OAuthResult?> GetStoredTokenAsync(string userEmail)
    {
        try
        {
            var clientId = _configuration["OAuth2:Gmail:ClientId"];
            var clientSecret = _configuration["OAuth2:Gmail:ClientSecret"];
            
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                _scopes,
                userEmail,
                CancellationToken.None,
                new FileDataStore(_tokenStoragePath, true));
            
            if (credential.Token != null)
            {
                return new OAuthResult
                {
                    Success = true,
                    EmailAddress = userEmail,
                    AccessToken = credential.Token.AccessToken,
                    RefreshToken = credential.Token.RefreshToken,
                    ExpiresAt = credential.Token.IssuedUtc.AddSeconds(
                        credential.Token.ExpiresInSeconds ?? 3600),
                    Provider = MIC.Core.Domain.Entities.EmailProvider.Gmail
                };
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    // Methods for compatibility with legacy interface
    public async Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        var storedToken = await GetStoredTokenAsync(userEmail);
        if (storedToken?.Success == true && !string.IsNullOrEmpty(storedToken.AccessToken))
        {
            return storedToken.AccessToken;
        }
        
        // If no stored token, try to authenticate
        var authResult = await AuthenticateGmailAsync(ct);
        if (authResult.Success && !string.IsNullOrEmpty(authResult.AccessToken))
        {
            return authResult.AccessToken;
        }
        
        throw new InvalidOperationException($"Failed to get access token for {userEmail}: {authResult.ErrorMessage}");
    }

    public Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        throw new NotImplementedException("Use OutlookOAuthService for Outlook access tokens");
    }

    public async Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await AuthenticateGmailAsync(ct);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("Use OutlookOAuthService for Outlook authorization");
    }
}