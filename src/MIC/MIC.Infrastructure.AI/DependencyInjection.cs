using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MIC.Infrastructure.AI.Configuration;
using MIC.Infrastructure.AI.Plugins;
using MIC.Infrastructure.AI.Services;

namespace MIC.Infrastructure.AI;

/// <summary>
/// Dependency injection extensions for AI services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds AI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register REAL AI services
        services.AddSingleton<IEmailAnalysisService, RealEmailAnalysisService>();
        services.AddSingleton<IChatService, RealChatService>();
        // Register prediction service
        services.AddScoped<IPredictionService, PredictionService>();
        
        // Keep other existing registrations
        services.Configure<AISettings>(configuration.GetSection("AI"));
        
        return services;
    }
}

/// <summary>
/// Stub chat service for when AI is not configured.
/// </summary>
internal class StubChatService : IChatService
{
    public Task<Models.ChatCompletionResult> SendMessageAsync(
        string userMessage,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Models.ChatCompletionResult
        {
            Success = false,
            Error = "AI services are not configured. Please add your OpenAI API key to appsettings.json.",
            Duration = TimeSpan.Zero
        });
    }

    public Task<List<Models.ChatMessage>> GetConversationHistoryAsync(string conversationId)
        => Task.FromResult(new List<Models.ChatMessage>());

    public Task ClearConversationAsync(string conversationId)
        => Task.CompletedTask;

    public IEnumerable<string> GetActiveConversations()
        => Enumerable.Empty<string>();

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
