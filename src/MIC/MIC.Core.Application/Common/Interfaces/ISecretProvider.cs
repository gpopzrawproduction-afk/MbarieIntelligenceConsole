namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Provides access to secrets (API keys, tokens) from a secure store.
/// </summary>
public interface ISecretProvider
{
    string? GetSecret(string name);
}
