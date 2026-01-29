using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MIC.Infrastructure.AI.Configuration;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Configures and builds the Semantic Kernel instance for AI operations.
/// </summary>
public static class SemanticKernelConfig
{
    /// <summary>
    /// Default system prompt for the MIC AI assistant.
    /// </summary>
    public const string DefaultSystemPrompt = """
        You are an elite business intelligence AI assistant for Mbarie Intelligence Console (MIC).
        
        Your role is to help executives and analysts make data-driven decisions by:
        - Analyzing business metrics and trends
        - Explaining alerts and anomalies  
        - Providing actionable insights
        - Answering questions about business performance
        - Identifying patterns and correlations in data
        
        Communication style:
        - Professional and concise
        - Data-driven with specific numbers when available
        - Actionable recommendations
        - Clear explanations without jargon
        
        When you don't have specific data, say so clearly and provide general guidance.
        Always be helpful, accurate, and focused on business value.
        """;

    /// <summary>
    /// Builds a configured Semantic Kernel instance.
    /// </summary>
    /// <param name="settings">AI settings from configuration.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>Configured Kernel instance.</returns>
    public static Kernel BuildKernel(AISettings settings, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var builder = Kernel.CreateBuilder();

        // Add logging if provided
        if (loggerFactory != null)
        {
            builder.Services.AddSingleton(loggerFactory);
        }

        // Configure based on provider
        switch (settings.Provider.ToLowerInvariant())
        {
            case "openai":
                ConfigureOpenAI(builder, settings.OpenAI);
                break;
            case "azureopenai":
                ConfigureAzureOpenAI(builder, settings.AzureOpenAI);
                break;
            default:
                throw new InvalidOperationException($"Unknown AI provider: {settings.Provider}");
        }

        return builder.Build();
    }

    private static void ConfigureOpenAI(IKernelBuilder builder, OpenAISettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            // Fail fast with a clear, non-secret-bearing message when the API key is missing
            throw new InvalidOperationException(
                "OpenAI API key is required but was not provided. " +
                "Set AI:OpenAI:ApiKey via user secrets or environment variables (e.g. MIC_AI__OpenAI__ApiKey).");
        }

        builder.AddOpenAIChatCompletion(
            modelId: settings.Model,
            apiKey: settings.ApiKey,
            orgId: settings.OrganizationId);

        // Add embedding service for semantic search - suppress experimental warnings
#pragma warning disable SKEXP0052
        builder.AddOpenAITextEmbeddingGeneration(
            modelId: settings.EmbeddingModel,
            apiKey: settings.ApiKey,
            orgId: settings.OrganizationId);
#pragma warning restore SKEXP0052
    }

    private static void ConfigureAzureOpenAI(IKernelBuilder builder, AzureOpenAISettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint and API key are required.");
        }

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: settings.ChatDeploymentName,
            endpoint: settings.Endpoint,
            apiKey: settings.ApiKey);

        if (!string.IsNullOrWhiteSpace(settings.EmbeddingDeploymentName))
        {
#pragma warning disable SKEXP0052
            builder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: settings.EmbeddingDeploymentName,
                endpoint: settings.Endpoint,
                apiKey: settings.ApiKey);
#pragma warning restore SKEXP0052
        }
    }

    /// <summary>
    /// Gets the system prompt, either custom or default.
    /// </summary>
    public static string GetSystemPrompt(SystemPromptSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.CustomPrompt))
        {
            return settings.CustomPrompt;
        }

        return DefaultSystemPrompt.Replace(
            "Mbarie Intelligence Console (MIC)",
            settings.BusinessName);
    }
}
