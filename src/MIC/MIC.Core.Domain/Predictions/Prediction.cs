using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Predictions;

public class Prediction : BaseEntity
{
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty; // Email, Alerts, Metrics
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PredictionType Type { get; set; }
    public double Confidence { get; set; } // 0.0 - 1.0
    public DateTime PredictionDate { get; set; }
    public DateTime? OccurrenceDate { get; set; } // When predicted event will occur
    public PredictionStatus Status { get; set; }
    public string DataSummary { get; set; } = string.Empty; // JSON of supporting data
    public int TimeHorizonDays { get; set; } // How far into future (7, 30, 90 days)
}

public enum PredictionType
{
    EmailVolumeIncrease,
    EmailVolumeDecrease,
    HighPriorityAlertSpike,
    MetricAnomaly,
    DeadlineApproaching,
    ResourceBottleneck,
    TrendReversal,
    SeasonalPattern
}

public enum PredictionStatus
{
    Active,
    Confirmed,
    Dismissed,
    Expired
}