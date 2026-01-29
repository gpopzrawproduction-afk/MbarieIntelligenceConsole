using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Abstraction for generating JSON Web Tokens (JWT) for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(User user);
}