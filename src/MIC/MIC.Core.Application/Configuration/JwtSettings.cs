namespace MIC.Core.Application.Configuration;

/// <summary>
/// Configuration settings for JWT (JSON Web Token) generation and validation.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Gets or sets the secret key used to sign JWT tokens.
    /// This should be a secure, random string of at least 32 characters.
    /// In production, use a secure key store or environment variable.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer of the JWT tokens.
    /// Typically the application name or URL.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience of the JWT tokens.
    /// Typically the application name or URL.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in hours.
    /// Default is 8 hours.
    /// </summary>
    public int ExpirationHours { get; set; } = 8;
}