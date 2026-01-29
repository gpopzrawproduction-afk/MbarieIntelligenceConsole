using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.Identity;

/// <summary>
/// Generates JSON Web Tokens (JWT) for authenticated users.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly TimeSpan _tokenLifetime;

    public JwtTokenService(string secretKey, TimeSpan? tokenLifetime = null)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new ArgumentException("JWT secret key cannot be null or whitespace.", nameof(secretKey));
        }

        _secretKey = secretKey;
        _tokenLifetime = tokenLifetime ?? TimeSpan.FromHours(8);
    }

    /// <summary>
    /// Generates a signed JWT for the specified user.
    /// </summary>
    public string GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException("User.Id must be a non-empty GUID to generate a JWT.", nameof(user));
        }

        var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(_tokenLifetime);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var tokenDescriptor = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}