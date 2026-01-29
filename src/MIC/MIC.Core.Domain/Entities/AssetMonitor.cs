using Ardalis.GuardClauses;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a monitored asset in the intelligence platform (equipment, infrastructure, system)
/// </summary>
public class AssetMonitor : BaseEntity
{
    /// <summary>
    /// Gets the name of the asset
    /// </summary>
    public string AssetName { get; private set; }
    
    /// <summary>
    /// Gets the type/category of the asset
    /// </summary>
    public string AssetType { get; private set; }
    
    /// <summary>
    /// Gets the physical or logical location of the asset
    /// </summary>
    public string Location { get; private set; }
    
    /// <summary>
    /// Gets the current operational status of the asset
    /// </summary>
    public AssetStatus Status { get; private set; }
    
    /// <summary>
    /// Gets the health score of the asset (0-100)
    /// </summary>
    public double? HealthScore { get; private set; }
    
    /// <summary>
    /// Gets the timestamp of the last monitoring check
    /// </summary>
    public DateTime LastMonitoredAt { get; private set; }
    
    /// <summary>
    /// Gets the technical specifications of the asset
    /// </summary>
    public Dictionary<string, string> Specifications { get; private set; }
    
    /// <summary>
    /// Gets the list of metrics associated with this asset
    /// </summary>
    public List<string> AssociatedMetrics { get; private set; }
    
    private AssetMonitor() 
    { 
        // EF Core constructor - initialize required collections
        AssetName = string.Empty;
        AssetType = string.Empty;
        Location = string.Empty;
        Specifications = new Dictionary<string, string>();
        AssociatedMetrics = new List<string>();
    }

    /// <summary>
    /// Creates a new asset monitor
    /// </summary>
    public AssetMonitor(string assetName, string assetType, string location)
    {
        AssetName = Guard.Against.NullOrWhiteSpace(assetName, nameof(assetName));
        AssetType = Guard.Against.NullOrWhiteSpace(assetType, nameof(assetType));
        Location = Guard.Against.NullOrWhiteSpace(location, nameof(location));
        Status = AssetStatus.Online;
        LastMonitoredAt = DateTime.UtcNow;
        Specifications = new Dictionary<string, string>();
        AssociatedMetrics = new List<string>();
    }

    /// <summary>
    /// Updates the operational status of the asset
    /// </summary>
    public void UpdateStatus(AssetStatus newStatus, string updatedBy)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        
        var previousStatus = Status;
        Status = newStatus;
        LastMonitoredAt = DateTime.UtcNow;
        MarkAsModified(updatedBy);
        
        if (previousStatus != newStatus)
        {
            AddDomainEvent(new AssetStatusChangedEvent(Id, AssetName, previousStatus, newStatus));
        }
    }

    /// <summary>
    /// Updates the health score of the asset
    /// </summary>
    public void UpdateHealthScore(double healthScore, string updatedBy)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        Guard.Against.OutOfRange(healthScore, nameof(healthScore), 0.0, 100.0);
        
        HealthScore = healthScore;
        LastMonitoredAt = DateTime.UtcNow;
        MarkAsModified(updatedBy);
        
        if (healthScore < 50.0)
        {
            AddDomainEvent(new AssetHealthDegradedEvent(Id, AssetName, healthScore));
        }
    }

    /// <summary>
    /// Adds a technical specification to the asset
    /// </summary>
    public void AddSpecification(string key, string value)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        
        Specifications[key] = value;
    }

    /// <summary>
    /// Associates a metric with this asset
    /// </summary>
    public void AssociateMetric(string metricName)
    {
        Guard.Against.NullOrWhiteSpace(metricName, nameof(metricName));
        
        if (!AssociatedMetrics.Contains(metricName))
        {
            AssociatedMetrics.Add(metricName);
        }
    }
}

/// <summary>
/// Operational status of a monitored asset
/// </summary>
public enum AssetStatus
{
    /// <summary>Asset is operational</summary>
    Online = 0,
    /// <summary>Asset is not operational</summary>
    Offline = 1,
    /// <summary>Asset is under maintenance</summary>
    Maintenance = 2,
    /// <summary>Asset is operational but degraded</summary>
    Degraded = 3,
    /// <summary>Asset has failed</summary>
    Failed = 4
}

/// <summary>
/// Domain event raised when asset status changes
/// </summary>
public record AssetStatusChangedEvent(
    Guid AssetId, 
    string AssetName, 
    AssetStatus PreviousStatus, 
    AssetStatus NewStatus) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Domain event raised when asset health degrades below threshold
/// </summary>
public record AssetHealthDegradedEvent(Guid AssetId, string AssetName, double HealthScore) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
