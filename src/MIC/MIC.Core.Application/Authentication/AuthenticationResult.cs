using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Authentication;

/// <summary>
/// Represents the outcome of an authentication operation.
/// </summary>
public sealed class AuthenticationResult
{
    public bool Success { get; init; }

    public string? Token { get; init; }

    public User? User { get; init; }

    public string? ErrorMessage { get; init; }
}