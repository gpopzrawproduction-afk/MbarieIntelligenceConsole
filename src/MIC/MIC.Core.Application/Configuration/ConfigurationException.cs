namespace MIC.Core.Application.Configuration;

/// <summary>
/// Represents configuration errors detected at application startup.
/// </summary>
public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message)
        : base(message)
    {
    }
}