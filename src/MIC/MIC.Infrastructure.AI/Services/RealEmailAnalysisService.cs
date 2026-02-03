using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.AI.Services;

public class RealEmailAnalysisService : IEmailAnalysisService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RealEmailAnalysisService> _logger;
    private readonly OpenAIClient? _openAIClient;
    private readonly string _modelId;
    private readonly bool _isConfigured;

    public RealEmailAnalysisService(
        IConfiguration configuration,
        ILogger<RealEmailAnalysisService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _modelId = configuration["AI:OpenAI:ModelId"] ?? "gpt-4o";

        var provider = configuration["AI:Provider"];
        var apiKey = configuration["AI:OpenAI:ApiKey"] 
                     ?? Environment.GetEnvironmentVariable("MIC_AI__OpenAI__ApiKey");

        if (!string.IsNullOrEmpty(apiKey) && provider == "OpenAI")
        {
            _openAIClient = new OpenAIClient(apiKey);
            _isConfigured = true;
            _logger.LogInformation("OpenAI client initialized successfully");
        }
        else
        {
            _logger.LogWarning("OpenAI not configured - AI analysis will be disabled");
            _isConfigured = false;
        }
    }

    public async Task<EmailAnalysisResult> AnalyzeEmailAsync(
        EmailMessage email, 
        CancellationToken ct = default)
    {
        if (!_isConfigured || _openAIClient == null)
        {
            _logger.LogWarning("AI not configured, returning default analysis");
            return GetDefaultAnalysis(email);
        }

        try
        {
            var prompt = BuildAnalysisPrompt(email);
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _modelId,
                Messages =
                {
                    new ChatRequestSystemMessage(_configuration["AI:Prompts:EmailAnalysis"]),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 500,
                Temperature = 0.3f, // Lower for more consistent analysis
            };

            var response = await _openAIClient.GetChatCompletionsAsync(
                chatCompletionsOptions, 
                ct);

            var result = response.Value.Choices[0].Message.Content;
            
            _logger.LogInformation("AI analysis completed for email: {Subject}", email.Subject);
            
            return ParseAnalysisResult(result, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed for email: {Subject}", email.Subject);
            return GetDefaultAnalysis(email);
        }
    }

    public async Task<string> GenerateSummaryAsync(
        EmailMessage email, 
        CancellationToken ct = default)
    {
        if (!_isConfigured || _openAIClient == null)
        {
            return $"Email from {email.FromName} regarding {email.Subject}";
        }

        try
        {
            var prompt = $"Summarize this email in 2-3 sentences:\n\n" +
                        $"From: {email.FromName}\n" +
                        $"Subject: {email.Subject}\n" +
                        $"Body: {email.BodyText}";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _modelId,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a professional email summarizer. Provide concise, accurate summaries."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 150,
                Temperature = 0.5f,
            };

            var response = await _openAIClient.GetChatCompletionsAsync(
                chatCompletionsOptions, 
                ct);

            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary generation failed");
            return $"Email from {email.FromName} regarding {email.Subject}";
        }
    }

    public async Task<List<string>> ExtractActionItemsAsync(
        EmailMessage email, 
        CancellationToken ct = default)
    {
        if (!_isConfigured || _openAIClient == null)
        {
            return new List<string>();
        }

        try
        {
            var prompt = $"Extract all action items from this email. List each item on a new line:\n\n" +
                        $"Subject: {email.Subject}\n" +
                        $"Body: {email.BodyText}";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _modelId,
                Messages =
                {
                    new ChatRequestSystemMessage("Extract action items from emails. Return only the list of action items, one per line."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 300,
                Temperature = 0.3f,
            };

            var response = await _openAIClient.GetChatCompletionsAsync(
                chatCompletionsOptions, 
                ct);

            var result = response.Value.Choices[0].Message.Content;
            
            return result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim('-', ' ', '•', '·'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action item extraction failed");
            return new List<string>();
        }
    }

    private string BuildAnalysisPrompt(EmailMessage email)
    {
        return $@"Analyze this email:

From: {email.FromName} <{email.FromAddress}>
To: {email.ToRecipients}
Subject: {email.Subject}
Date: {email.ReceivedDate:yyyy-MM-dd HH:mm}

Body:
{email.BodyText}

Provide analysis in this exact JSON format:
{{
  ""priority"": ""High|Normal|Low"",
  ""isUrgent"": true|false,
  ""sentiment"": ""Positive|Neutral|Negative"",
  ""actionItems"": [""item1"", ""item2""],
  ""summary"": ""2-3 sentence summary"",
  ""confidence"": 0.0-1.0
}}";
    }

    private EmailAnalysisResult ParseAnalysisResult(string aiResponse, EmailMessage email)
    {
        try
        {
            // Try to parse JSON response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (parsed != null)
                {
                    return new EmailAnalysisResult
                    {
                        Priority = Enum.Parse<EmailPriority>(parsed["priority"].GetString() ?? "Normal", true),
                        IsUrgent = parsed["isUrgent"].GetBoolean(),
                        Sentiment = Enum.Parse<SentimentType>(parsed["sentiment"].GetString() ?? "Neutral", true),
                        ActionItems = parsed.ContainsKey("actionItems") 
                            ? parsed["actionItems"].EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                            : new List<string>(),
                        Summary = parsed["summary"].GetString() ?? "",
                        ConfidenceScore = parsed.ContainsKey("confidence") 
                            ? parsed["confidence"].GetDouble() 
                            : 0.8
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response, using heuristics");
        }

        // Fallback to default analysis
        return GetDefaultAnalysis(email);
    }

    private EmailAnalysisResult GetDefaultAnalysis(EmailMessage email)
    {
        // Simple heuristic-based analysis as fallback
        var isUrgent = email.Subject?.ToLower().Contains("urgent") == true ||
                      email.Subject?.ToLower().Contains("asap") == true;

        var priority = isUrgent ? EmailPriority.High :
                      email.Subject?.ToLower().Contains("fyi") == true ? EmailPriority.Low :
                      EmailPriority.Normal;

        return new EmailAnalysisResult
        {
            Priority = priority,
            IsUrgent = isUrgent,
            Sentiment = SentimentType.Neutral,
            ActionItems = new List<string>(),
            Summary = $"Email from {email.FromName} regarding {email.Subject}",
            ConfidenceScore = 0.5
        };
    }
}