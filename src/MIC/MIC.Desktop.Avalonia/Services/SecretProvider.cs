using MIC.Core.Application.Common.Interfaces;

namespace MIC.Desktop.Avalonia.Services;

internal sealed class SecretProvider : ISecretProvider
{
    public string? GetSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return name switch
        {
            "AI:OpenAI:ApiKey" => SettingsService.Instance.OpenAIApiKey,
            _ => null
        };
    }
}
