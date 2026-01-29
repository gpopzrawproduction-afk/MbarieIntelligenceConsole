using MIC.Infrastructure.AI.Models;

namespace MIC.Infrastructure.AI.Services;

/// <summary>
/// Interface for automated insight generation.
/// </summary>
public interface IInsightGeneratorService
{
    /// <summary>
    /// Generates a daily business summary.
    /// </summary>
    Task<AIInsight> GenerateDailySummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes current metrics and generates insights.
    /// </summary>
    Task<List<AIInsight>> AnalyzeMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates insights from alerts.
    /// </summary>
    Task<List<AIInsight>> AnalyzeAlertsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for generating automated business insights using AI.
/// </summary>
public class InsightGeneratorService : IInsightGeneratorService
{
    private readonly IChatService _chatService;

    public InsightGeneratorService(IChatService chatService)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
    }

    /// <inheritdoc />
    public async Task<AIInsight> GenerateDailySummaryAsync(CancellationToken cancellationToken = default)
    {
        var prompt = """
            Generate a brief daily business summary covering:
            1. Overall business health (one sentence)
            2. Key metrics status (bullets)
            3. Critical alerts requiring attention
            4. One actionable recommendation
            
            Keep it concise and executive-friendly.
            """;

        var result = await _chatService.SendMessageAsync(prompt, "daily-summary", cancellationToken);

        return new AIInsight
        {
            Type = InsightType.Summary,
            Title = "Daily Business Summary",
            Description = result.Success ? result.Response : "Unable to generate summary.",
            Severity = InsightSeverity.Info,
            Confidence = result.Success ? 0.85 : 0.0
        };
    }

    /// <inheritdoc />
    public async Task<List<AIInsight>> AnalyzeMetricsAsync(CancellationToken cancellationToken = default)
    {
        var insights = new List<AIInsight>();

        var prompt = """
            Analyze the current business metrics and identify:
            1. Any metrics significantly above or below target
            2. Concerning trends that need attention
            3. Positive trends worth celebrating
            
            For each finding, provide a brief insight.
            """;

        var result = await _chatService.SendMessageAsync(prompt, "metric-analysis", cancellationToken);

        if (result.Success)
        {
            insights.Add(new AIInsight
            {
                Type = InsightType.Trend,
                Title = "Metrics Analysis",
                Description = result.Response,
                Severity = InsightSeverity.Info,
                Confidence = 0.8
            });
        }

        return insights;
    }

    /// <inheritdoc />
    public async Task<List<AIInsight>> AnalyzeAlertsAsync(CancellationToken cancellationToken = default)
    {
        var insights = new List<AIInsight>();

        var prompt = """
            Review the current active alerts and provide:
            1. A prioritized summary of critical issues
            2. Any patterns in alert types or sources
            3. Recommended actions for resolution
            
            Focus on actionable insights.
            """;

        var result = await _chatService.SendMessageAsync(prompt, "alert-analysis", cancellationToken);

        if (result.Success)
        {
            insights.Add(new AIInsight
            {
                Type = InsightType.Alert,
                Title = "Alert Analysis",
                Description = result.Response,
                Severity = InsightSeverity.Warning,
                Confidence = 0.75
            });
        }

        return insights;
    }
}
