using MIC.Infrastructure.AI.Models;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Interface for AI chat service operations.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a message to the AI and receives a response.
    /// </summary>
    /// <param name="userMessage">The user's message.</param>
    /// <param name="conversationId">Optional conversation ID to continue a conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI's response.</returns>
    Task<ChatCompletionResult> SendMessageAsync(
        string userMessage,
        string? conversationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the conversation history for a given conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <returns>List of messages in the conversation.</returns>
    Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId);

    /// <summary>
    /// Clears the conversation history.
    /// </summary>
    /// <param name="conversationId">The conversation ID to clear.</param>
    Task ClearConversationAsync(string conversationId);

    /// <summary>
    /// Gets all active conversation IDs.
    /// </summary>
    IEnumerable<string> GetActiveConversations();

    /// <summary>
    /// Checks if the AI service is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
