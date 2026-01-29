using MIC.Core.Domain.Entities;

namespace MIC.Infrastructure.AI.Services;

public interface IEmailAnalysisService
{
    Task<EmailAnalysisResult> AnalyzeEmailAsync(EmailMessage email, CancellationToken ct = default);
    Task<string> GenerateSummaryAsync(EmailMessage email, CancellationToken ct = default);
    Task<List<string>> ExtractActionItemsAsync(EmailMessage email, CancellationToken ct = default);
}

public class EmailAnalysisResult
{
    public EmailPriority Priority { get; set; }
    public bool IsUrgent { get; set; }
    public SentimentType Sentiment { get; set; }
    public List<string> ActionItems { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}