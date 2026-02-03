using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using MIC.Infrastructure.AI.Models;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Infrastructure.AI.Services;

public class RealChatService : IChatService
{
    private Kernel? _kernel;
    private IChatCompletionService? _chatCompletion;
    private readonly ILogger<RealChatService> _logger;
    private readonly Dictionary<string, ChatHistory> _userHistories = new();
    private bool _isConfigured;
    private string? _currentApiKey;
    private readonly IConfiguration _configuration;
    private readonly ISecretProvider? _secretProvider;
    private readonly object _configLock = new();

    public RealChatService(
        IConfiguration configuration,
        ILogger<RealChatService> logger,
        ISecretProvider? secretProvider = null)
    {
        _logger = logger;
        _configuration = configuration;
        _secretProvider = secretProvider;

        TryConfigure();
    }

    public async Task<ChatCompletionResult> SendMessageAsync(
        string userMessage,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[RealChatService] Processing message: '{userMessage}'");
        Console.WriteLine($"[RealChatService] Conversation ID: {conversationId}");
        Console.WriteLine($"[RealChatService] Service configured: {_isConfigured}");
        Console.WriteLine($"[RealChatService] Chat completion available: {_chatCompletion != null}");

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            Console.WriteLine($"[RealChatService] Empty message - returning error");
            return new ChatCompletionResult
            {
                Success = false,
                Error = "Message cannot be empty."
            };
        }

        conversationId ??= Guid.NewGuid().ToString();

        if (!_isConfigured || _chatCompletion == null)
        {
            TryConfigure();
        }

        if (!_isConfigured || _chatCompletion == null)
        {
            Console.WriteLine("[RealChatService] AI not configured - refusing request");
            return new ChatCompletionResult
            {
                Success = false,
                Error = "AI service not configured. Please set your OpenAI API key.",
                Duration = TimeSpan.Zero
            };
        }

        try
        {
            // Get or create chat history for user
            if (!_userHistories.ContainsKey(conversationId))
            {
                Console.WriteLine($"[RealChatService] Creating new chat history for conversation {conversationId}");
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

            Console.WriteLine($"[RealChatService] Calling OpenAI API with {chatHistory.Count} messages in history");
            
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

            Console.WriteLine($"[RealChatService] API call successful, response length: {reply.Length}");
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
            Console.WriteLine($"[RealChatService] API call failed: {ex.Message}");
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
                    Content = msg.Content ?? string.Empty
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
        if (!_isConfigured || _chatCompletion == null)
        {
            TryConfigure();
        }

        if (!_isConfigured || _chatCompletion == null)
        {
            return false;
        }

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

    private bool TryConfigure()
    {
        lock (_configLock)
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _isConfigured = false;
                _kernel = null;
                _chatCompletion = null;
                _currentApiKey = null;
                _logger.LogWarning("OpenAI not configured - chat is disabled until an API key is provided");
                return false;
            }

            if (_isConfigured && string.Equals(apiKey, _currentApiKey, StringComparison.Ordinal))
            {
                return true;
            }

            var modelId = _configuration["AI:OpenAI:Model"]
                          ?? _configuration["AI:OpenAI:ModelId"]
                          ?? "gpt-4o";

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(modelId: modelId, apiKey: apiKey);

            _kernel = kernelBuilder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            _isConfigured = true;
            _currentApiKey = apiKey;

            _logger.LogInformation("Chat service initialized with OpenAI");
            return true;
        }
    }

    private string? GetApiKey()
    {
        return _configuration["AI:OpenAI:ApiKey"]
               ?? Environment.GetEnvironmentVariable("AI__OpenAI__ApiKey")
               ?? Environment.GetEnvironmentVariable("MIC_AI__OpenAI__ApiKey")
               ?? _secretProvider?.GetSecret("AI:OpenAI:ApiKey");
    }
}
