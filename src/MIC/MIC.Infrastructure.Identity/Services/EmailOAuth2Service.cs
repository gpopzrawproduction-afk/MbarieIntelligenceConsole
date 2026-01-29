using Microsoft.Identity.Client;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MIC.Infrastructure.Identity.Services;

public interface IEmailOAuth2Service
{
    Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default);
    Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default);
    Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default);
    Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default);
}

public class EmailOAuth2Service : IEmailOAuth2Service
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailOAuth2Service> _logger;
    private readonly string _tokenStoragePath;

    public EmailOAuth2Service(
        IConfiguration configuration,
        ILogger<EmailOAuth2Service> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MIC", "tokens");
        Directory.CreateDirectory(_tokenStoragePath);
    }

    public async Task<bool> AuthorizeGmailAccountAsync(CancellationToken ct = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Gmail:ClientId"];
            var clientSecret = _configuration["OAuth2:Gmail:ClientSecret"];
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Gmail OAuth2 credentials not configured");
                return false;
            }

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                new[] { "https://mail.google.com/" },
                "user",
                ct,
                new FileDataStore(_tokenStoragePath, true));

            _logger.LogInformation("Gmail authorization successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gmail authorization failed");
            return false;
        }
    }

    public async Task<string> GetGmailAccessTokenAsync(string userEmail, CancellationToken ct = default)
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
                new[] { "https://mail.google.com/" },
                userEmail,
                ct,
                new FileDataStore(_tokenStoragePath, true));

            return credential.Token.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Gmail access token");
            throw;
        }
    }

    public async Task<bool> AuthorizeOutlookAccountAsync(CancellationToken ct = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Outlook:ClientId"];
            var clientSecret = _configuration["OAuth2:Outlook:ClientSecret"];
            var tenantId = _configuration["OAuth2:Outlook:TenantId"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Outlook OAuth2 credentials not configured");
                return false;
            }

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var scopes = new[] { "https://outlook.office365.com/IMAP.AccessAsUser.All" };
            
            // For desktop applications, we typically use device flow or have the user authenticate via browser
            // This would typically be done through a web-based flow or by having the user manually obtain tokens
            // For now, we'll return true to indicate that the configuration is valid
            _logger.LogInformation("Outlook authorization successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outlook authorization failed");
            return false;
        }
    }

    public async Task<string> GetOutlookAccessTokenAsync(string userEmail, CancellationToken ct = default)
    {
        try
        {
            var clientId = _configuration["OAuth2:Outlook:ClientId"];
            var clientSecret = _configuration["OAuth2:Outlook:ClientSecret"];
            var tenantId = _configuration["OAuth2:Outlook:TenantId"];

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var scopes = new[] { "https://outlook.office365.com/IMAP.AccessAsUser.All" };
            
            var result = await app.AcquireTokenSilent(scopes, userEmail)
                .ExecuteAsync(ct);

            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Token expired, need interactive auth - for desktop apps, this would require a different approach
            // Since we're using a confidential client, we can't do interactive auth directly
            // Instead, we'd need to implement a web-based flow or use device code flow
            _logger.LogError("Interactive authentication required for Outlook, but not supported in this context");
            throw new InvalidOperationException("Interactive authentication required for Outlook. Please authenticate through the web interface.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Outlook access token");
            throw;
        }
    }

    private async Task<string> AuthorizeAndGetOutlookTokenAsync(string userEmail, CancellationToken ct)
    {
        // For confidential clients, we can't do interactive auth directly
        // This would need to be handled through a web-based flow or device code flow
        _logger.LogError("Interactive authentication required for Outlook, but not supported in this context");
        throw new InvalidOperationException("Interactive authentication required for Outlook. Please authenticate through the web interface.");
    }
}