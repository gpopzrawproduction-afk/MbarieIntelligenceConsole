using System;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a chat conversation history with AI assistant.
/// </summary>
public class ChatHistory : BaseEntity
{
    /// <summary>
    /// Foreign key to the User entity.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property to the User entity.
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Unique session identifier for grouping related chat messages.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's query/message.
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// The AI assistant's response.
    /// </summary>
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// When this chat message was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Context information (e.g., email being analyzed, document reference).
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// AI provider used (e.g., OpenAI, AzureOpenAI).
    /// </summary>
    public string? AIProvider { get; set; }
    
    /// <summary>
    /// Specific model used (e.g., gpt-4-turbo-preview).
    /// </summary>
    public string? ModelUsed { get; set; }
    
    /// <summary>
    /// Estimated token count for this interaction.
    /// </summary>
    public int TokenCount { get; set; }
    
    /// <summary>
    /// Cost associated with this interaction (if applicable).
    /// </summary>
    public decimal? Cost { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Whether this chat was successful.
    /// </summary>
    public bool IsSuccessful { get; set; } = true;
    
    /// <summary>
    /// Error message if the chat failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Creates a new ChatHistory entry.
    /// </summary>
    public ChatHistory() { }
    
    /// <summary>
    /// Creates a new ChatHistory entry with required parameters.
    /// </summary>
    public ChatHistory(Guid userId, string sessionId, string query, string response)
    {
        UserId = userId;
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        Query = query ?? throw new ArgumentNullException(nameof(query));
        Response = response ?? throw new ArgumentNullException(nameof(response));
        Timestamp = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Creates a new ChatHistory entry with AI provider details.
    /// </summary>
    public ChatHistory(Guid userId, string sessionId, string query, string response, 
                       string aiProvider, string modelUsed, int tokenCount)
        : this(userId, sessionId, query, response)
    {
        AIProvider = aiProvider;
        ModelUsed = modelUsed;
        TokenCount = tokenCount;
    }
    
    /// <summary>
    /// Marks this chat as failed with an error message.
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        IsSuccessful = false;
        ErrorMessage = errorMessage;
        MarkAsModified("system");
    }
}