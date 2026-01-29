using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using MIC.Infrastructure.AI.Models;

namespace MIC.Infrastructure.AI.Services;

public class RealChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<RealChatService> _logger;
    private readonly Dictionary<string, ChatHistory> _userHistories = new();
    private readonly bool _isConfigured;

    public RealChatService(
        IConfiguration configuration,
        ILogger<RealChatService> logger)
    {
        _logger = logger;

        var apiKey = configuration["AI:OpenAI:ApiKey"] 
                     ?? Environment.GetEnvironmentVariable("MIC_AI__OpenAI__ApiKey");

        if (!string.IsNullOrEmpty(apiKey))
        {
            var kernelBuilder = Kernel.CreateBuilder();
            
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: configuration["AI:OpenAI:ModelId"] ?? "gpt-4-turbo-preview",
                apiKey: apiKey);

            _kernel = kernelBuilder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            _isConfigured = true;

            _logger.LogInformation("Chat service initialized with OpenAI");
        }
        else
        {
            _logger.LogWarning("OpenAI not configured - chat will use fallback responses");
            _isConfigured = false;
        }
    }

    public async Task<ChatCompletionResult> SendMessageAsync(
        string userMessage,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new ChatCompletionResult
            {
                Success = false,
                Error = "Message cannot be empty."
            };
        }

        conversationId ??= Guid.NewGuid().ToString();

        if (!_isConfigured || _chatCompletion == null)
        {
            return new ChatCompletionResult
            {
                Success = false,
                Error = "AI chat is not configured. Please set up your OpenAI API key in settings.",
                Duration = TimeSpan.Zero
            };
        }

        try
        {
            // Get or create chat history for user
            if (!_userHistories.ContainsKey(conversationId))
            {
                var history = new ChatHistory();
                history.AddSystemMessage(@"You are an AI assistant for the Mbarie Intelligence Console. 
You help executives manage their business communications efficiently. 
You have access to their emails and can provide intelligent insights about:
- Email priorities and urgency
- Action items and deadlines
- Communication patterns
- Important contacts (Saipem, Daewoo, NLNG)
Be professional, concise, and helpful.");
                
                _userHistories[conversationId] = history;
            }

            var chatHistory = _userHistories[conversationId];
            chatHistory.AddUserMessage(userMessage);

            var response = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 500,
                    Temperature = 0.7,
                    TopP = 1.0,
                },
                cancellationToken: cancellationToken);

            var reply = response.Content ?? "I apologize, but I couldn't generate a response.";
            
            chatHistory.AddAssistantMessage(reply);

            _logger.LogInformation("Chat response generated for user {UserId}", conversationId);

            return new ChatCompletionResult
            {
                Success = true,
                Response = reply,
                Duration = TimeSpan.Zero // We could add timing if needed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat service failed");
            return new ChatCompletionResult
            {
                Success = false,
                Error = "I apologize, but I encountered an error processing your request. Please try again.",
                Duration = TimeSpan.Zero
            };
        }
    }

    public Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId)
    {
        if (_userHistories.TryGetValue(conversationId, out var history))
        {
            // Convert Semantic Kernel ChatHistory to our ChatMessage format
            var messages = new List<ChatMessage>();
            foreach (var msg in history)
            {
                messages.Add(new ChatMessage
                {
                    Role = MapSemanticKernelRole(msg.Role), // Convert from Semantic Kernel role to our role
                    Content = msg.Content
                });
            }
            return Task.FromResult(messages);
        }

        return Task.FromResult(new List<ChatMessage>());
    }

    public Task ClearConversationAsync(string conversationId)
    {
        if (_userHistories.ContainsKey(conversationId))
        {
            _userHistories.Remove(conversationId);
            _logger.LogInformation("Chat history cleared for user {UserId}", conversationId);
        }
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetActiveConversations()
    {
        return _userHistories.Keys;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
            return false;

        try
        {
            var testHistory = new ChatHistory();
            testHistory.AddUserMessage("Hello");

            var response = await _chatCompletion.GetChatMessageContentAsync(
                testHistory,
                new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 10,
                    Temperature = 0.1,
                },
                cancellationToken: cancellationToken);

            return !string.IsNullOrEmpty(response?.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service availability check failed");
            return false;
        }
    }

    private static ChatRole MapSemanticKernelRole(AuthorRole role)
    {
        return role.ToString().ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            _ => ChatRole.User // Default fallback
        };
    }
}
