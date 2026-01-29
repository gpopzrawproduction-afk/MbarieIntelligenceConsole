namespace MIC.Infrastructure.AI.Models;

/// <summary>
/// Represents a chat message in a conversation.
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The role of the message sender.
    /// </summary>
    public ChatRole Role { get; init; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional metadata about the message.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// The role of a chat message sender.
/// </summary>
public enum ChatRole
{
    /// <summary>
    /// System message setting context.
    /// </summary>
    System,

    /// <summary>
    /// User message.
    /// </summary>
    User,

    /// <summary>
    /// Assistant (AI) response.
    /// </summary>
    Assistant
}

/// <summary>
/// Represents an AI-generated insight.
/// </summary>
public record AIInsight
{
    /// <summary>
    /// The type of insight.
    /// </summary>
    public InsightType Type { get; init; }

    /// <summary>
    /// Short title for the insight.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description of the insight.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Severity or importance level.
    /// </summary>
    public InsightSeverity Severity { get; init; }

    /// <summary>
    /// Suggested actions to take.
    /// </summary>
    public List<string> Recommendations { get; init; } = new();

    /// <summary>
    /// Related metric or alert IDs.
    /// </summary>
    public List<Guid> RelatedEntityIds { get; init; } = new();

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// When the insight was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Types of AI-generated insights.
/// </summary>
public enum InsightType
{
    Trend,
    Anomaly,
    Recommendation,
    Prediction,
    Summary,
    Alert,
    Correlation
}

/// <summary>
/// Severity levels for insights.
/// </summary>
public enum InsightSeverity
{
    Info,
    Success,
    Warning,
    Critical
}

/// <summary>
/// Result of an AI chat completion.
/// </summary>
public record ChatCompletionResult
{
    /// <summary>
    /// Whether the completion was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The AI's response message.
    /// </summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>
    /// Error message if unsuccessful.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Token usage statistics.
    /// </summary>
    public TokenUsage? Usage { get; init; }

    /// <summary>
    /// Time taken to generate response.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Token usage statistics for an AI request.
/// </summary>
public record TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}
