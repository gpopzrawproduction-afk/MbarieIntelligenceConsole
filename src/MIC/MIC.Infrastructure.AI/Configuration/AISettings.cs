namespace MIC.Infrastructure.AI.Configuration;

/// <summary>
/// Configuration settings for AI services in MIC.
/// Supports OpenAI, Azure OpenAI, and local model providers.
/// </summary>
public class AISettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AI";

    /// <summary>
    /// The AI provider to use: OpenAI, AzureOpenAI, or Local.
    /// </summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// OpenAI configuration settings.
    /// </summary>
    public OpenAISettings OpenAI { get; set; } = new();

    /// <summary>
    /// Azure OpenAI configuration settings.
    /// </summary>
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();

    /// <summary>
    /// Feature flags for AI capabilities.
    /// </summary>
    public AIFeatureFlags Features { get; set; } = new();

    /// <summary>
    /// System prompt configuration.
    /// </summary>
    public SystemPromptSettings SystemPrompt { get; set; } = new();
}

/// <summary>
/// OpenAI-specific configuration.
/// </summary>
public class OpenAISettings
{
    /// <summary>
    /// OpenAI API key. Store securely - use environment variables or secrets manager.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use for chat completions. Recommended: gpt-4o, gpt-4-turbo.
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Model to use for text embeddings. Recommended: text-embedding-3-small.
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Temperature for response generation (0.0-2.0). Lower = more deterministic.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens in response.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Organization ID (optional).
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Azure OpenAI-specific configuration.
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>
    /// Azure OpenAI endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name for chat model.
    /// </summary>
    public string ChatDeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name for embedding model.
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Temperature for response generation.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens in response.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
/// Feature flags to enable/disable AI capabilities.
/// </summary>
public class AIFeatureFlags
{
    /// <summary>
    /// Enable the chat assistant feature.
    /// </summary>
    public bool ChatEnabled { get; set; } = true;

    /// <summary>
    /// Enable email intelligence extraction.
    /// </summary>
    public bool EmailIntelligenceEnabled { get; set; } = false;

    /// <summary>
    /// Enable ML-powered predictions.
    /// </summary>
    public bool PredictionsEnabled { get; set; } = true;

    /// <summary>
    /// Enable automated insight generation.
    /// </summary>
    public bool InsightsEnabled { get; set; } = true;

    /// <summary>
    /// Enable voice interface (experimental).
    /// </summary>
    public bool VoiceEnabled { get; set; } = false;

    /// <summary>
    /// Enable anomaly detection.
    /// </summary>
    public bool AnomalyDetectionEnabled { get; set; } = true;
}

/// <summary>
/// Configuration for the system prompt used by the AI assistant.
/// </summary>
public class SystemPromptSettings
{
    /// <summary>
    /// The business name to personalize responses.
    /// </summary>
    public string BusinessName { get; set; } = "Mbarie Intelligence Console";

    /// <summary>
    /// Custom system prompt (optional). If empty, uses default.
    /// </summary>
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// Whether to include current metrics in context.
    /// </summary>
    public bool IncludeMetricsContext { get; set; } = true;

    /// <summary>
    /// Whether to include recent alerts in context.
    /// </summary>
    public bool IncludeAlertsContext { get; set; } = true;

    /// <summary>
    /// Maximum number of alerts to include in context.
    /// </summary>
    public int MaxAlertsInContext { get; set; } = 5;
}
