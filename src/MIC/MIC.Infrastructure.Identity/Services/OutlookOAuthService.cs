using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MIC.Infrastructure.Identity.Services;

public class OutlookOAuthService : IEmailOAuth2Service
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutlookOAuthService> _logger;
    private readonly string[] _scopes = new[]
    {
        "https://graph.microsoft.com/Mail.Read",
        "https://graph.microsoft.com/Mail.Send",
        "https://graph.microsoft.com/Mail.ReadWrite",
        "https://graph.microsoft.com/User.Read"
    };

    public OutlookOAuthService(
        IConfiguration configuration,
        ILogger<OutlookOAuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private IPublicClientApplication BuildPublicClientApp()
    {
        var clientId = _configuration["OAuth2:Outlook:ClientId"];
        var tenantId = _configuration["OAuth2:Outlook:TenantId"] ?? "common";
        var redirectUri = _configuration["OAuth2:Outlook:RedirectUri"] ?? "http://localhost";

        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Outlook OAuth ClientId not configured. Please configure OAuth2:Outlook:ClientId in appsettings.json.");
        }

        return PublicClientApplicationBuilder
            .Create(clientId)
            .WithRedirectUri(redirectUri)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
            .WithLogging((level, message, containsPii) =>
            {
                _logger.LogDebug($"MSAL: {level} {message}");
            })
            .Build();
    }

    public async Task<OAuthResult> AuthenticateOutlookAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Outlook:ClientId"];
            
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Outlook OAuth credentials not configured");
                return new OAuthResult
                {
                    Success = false,
                    ErrorMessage = "Outlook OAuth credentials not configured. Please configure ClientId in appsettings.json.",
                    Provider = MIC.Core.Domain.Entities.EmailProvider.Outlook
                };
            }

            var app = BuildPublicClientApp();
            
            AuthenticationResult result;
            var accounts = await app.GetAccountsAsync();
            
            try
            {
                // Try silent authentication first
                var firstAccount = accounts.FirstOrDefault();
                if (firstAccount != null)
                {
                    result = await app.AcquireTokenSilent(_scopes, firstAccount)
                        .ExecuteAsync(cancellationToken);
                    _logger.LogInformation("Outlook silent authentication successful");
                }
                else
                {
                    // Interactive authentication required
                    result = await app.AcquireTokenInteractive(_scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync(cancellationToken);
                    _logger.LogInformation("Outlook interactive authentication successful");
                }
            }
            catch (MsalUiRequiredException)
            {
                // Interactive authentication required
                result = await app.AcquireTokenInteractive(_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
                _logger.LogInformation("Outlook interactive authentication successful after UI required");
            }

            // Get user email from Microsoft Graph
            var emailAddress = await GetUserEmailAsync(result.AccessToken, cancellationToken);
            
            _logger.LogInformation("Outlook authentication successful for {Email}", emailAddress);

            return new OAuthResult
            {
                Success = true,
                EmailAddress = emailAddress,
                AccessToken = result.AccessToken,
                RefreshToken = null, // MSAL handles refresh internally
                ExpiresAt = result.ExpiresOn.UtcDateTime,
                Provider = MIC.Core.Domain.Entities.EmailProvider.Outlook
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outlook authentication failed");
            return new OAuthResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Provider = MIC.Core.Domain.Entities.EmailProvider.Outlook
            };
        }
    }

    private async Task<string?> GetUserEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(json);
            
            if (jsonDoc.RootElement.TryGetProperty("userPrincipalName", out var upnElement))
            {
                return upnElement.GetString();
            }
            
            if (jsonDoc.RootElement.TryGetProperty("mail", out var mailElement))
            {
                return mailElement.GetString();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user email from Microsoft Graph");
            return null;
        }
    }

    public async Task<string?> RefreshOutlookTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // MSAL handles token refresh automatically
        // This method is not needed for Outlook as MSAL manages refresh internally
        return null;
    }

    public async Task<bool> ValidateTokenAsync(
        string accessToken,
        MIC.Core.Domain.Entities.EmailProvider provider)
    {
        if (provider != MIC.Core.Domain.Entities.EmailProvider.Outlook) return false;
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Gmail methods - not implemented in this service
    public Task<OAuthResult> AuthenticateGmailAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use GmailOAuthService");
    }
    
    public Task<string?> RefreshGmailTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Use GmailOAuthService");
    }

    // Helper method to check if credentials are configured
    public bool AreCredentialsConfigured()
    {
        var clientId = _configuration["OAuth2:Outlook:ClientId"];
        return !string.IsNullOrEmpty(clientId);
    }

    // Helper method to get available accounts
    public async Task<List<string>> GetAvailableAccountsAsync()
    {
        try
        {
            var app = BuildPublicClientApp();
            var accounts = await app.GetAccountsAsync();
            return accounts.Select(a => a.Username).Where(u => !string.IsNullOrEmpty(u)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available accounts");
            return new List<string>();
        }
    }

    // Methods for compatibility with legacy interface
    public Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        throw new NotImplementedException("Use GmailOAuthService for Gmail access tokens");
    }

    public async Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        try
        {
            var app = BuildPublicClientApp();
            var accounts = await app.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.Username == userEmail);
            
            AuthenticationResult result;
            if (account != null)
            {
                // Try silent authentication first
                result = await app.AcquireTokenSilent(_scopes, account).ExecuteAsync(ct);
            }
            else
            {
                // Need interactive authentication
                result = await app.AcquireTokenInteractive(_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(ct);
            }
            
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Outlook access token for {UserEmail}", userEmail);
            throw new InvalidOperationException($"Failed to get Outlook access token: {ex.Message}");
        }
    }

    public Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("Use GmailOAuthService for Gmail authorization");
    }

    public async Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await AuthenticateOutlookAsync(ct);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }
}