using Ardalis.GuardClauses;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a decision-making context with AI-assisted intelligence
/// </summary>
public class DecisionContext : BaseEntity
{
    /// <summary>
    /// Gets the name of the decision context
    /// </summary>
    public string ContextName { get; private set; }
    
    /// <summary>
    /// Gets the description of the decision
    /// </summary>
    public string Description { get; private set; }
    
    /// <summary>
    /// Gets the decision maker
    /// </summary>
    public string DecisionMaker { get; private set; }
    
    /// <summary>
    /// Gets the priority level of the decision
    /// </summary>
    public DecisionPriority Priority { get; private set; }
    
    /// <summary>
    /// Gets the current status of the decision
    /// </summary>
    public DecisionStatus Status { get; private set; }
    
    /// <summary>
    /// Gets the deadline for the decision
    /// </summary>
    public DateTime Deadline { get; private set; }
    
    /// <summary>
    /// Gets the contextual data for the decision
    /// </summary>
    public Dictionary<string, object> ContextData { get; private set; }
    
    /// <summary>
    /// Gets the list of options being considered
    /// </summary>
    public List<string> ConsideredOptions { get; private set; }
    
    /// <summary>
    /// Gets the selected option
    /// </summary>
    public string? SelectedOption { get; private set; }
    
    /// <summary>
    /// Gets the AI-generated recommendation
    /// </summary>
    public string? AIRecommendation { get; private set; }
    
    /// <summary>
    /// Gets the confidence level of the AI recommendation (0-1)
    /// </summary>
    public double? AIConfidence { get; private set; }
    
    /// <summary>
    /// Gets the timestamp when the decision was made
    /// </summary>
    public DateTime? DecidedAt { get; private set; }
    
    private DecisionContext() 
    { 
        // EF Core constructor - initialize required collections
        ContextName = string.Empty;
        Description = string.Empty;
        DecisionMaker = string.Empty;
        ContextData = new Dictionary<string, object>();
        ConsideredOptions = new List<string>();
    }

    /// <summary>
    /// Creates a new decision context
    /// </summary>
    public DecisionContext(
        string contextName,
        string description,
        string decisionMaker,
        DateTime deadline,
        DecisionPriority priority = DecisionPriority.Medium)
    {
        ContextName = Guard.Against.NullOrWhiteSpace(contextName, nameof(contextName));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        DecisionMaker = Guard.Against.NullOrWhiteSpace(decisionMaker, nameof(decisionMaker));
        Deadline = Guard.Against.OutOfSQLDateRange(deadline, nameof(deadline));
        Priority = priority;
        Status = DecisionStatus.Pending;
        ContextData = new Dictionary<string, object>();
        ConsideredOptions = new List<string>();
    }

    /// <summary>
    /// Adds an option to consider for the decision
    /// </summary>
    public void AddOption(string option)
    {
        Guard.Against.NullOrWhiteSpace(option, nameof(option));
        
        if (!ConsideredOptions.Contains(option))
        {
            ConsideredOptions.Add(option);
        }
    }

    /// <summary>
    /// Sets the AI-generated recommendation
    /// </summary>
    public void SetAIRecommendation(string recommendation, double confidence)
    {
        Guard.Against.NullOrWhiteSpace(recommendation, nameof(recommendation));
        Guard.Against.OutOfRange(confidence, nameof(confidence), 0.0, 1.0);
        
        AIRecommendation = recommendation;
        AIConfidence = confidence;
        
        AddDomainEvent(new AIRecommendationGeneratedEvent(Id, ContextName, recommendation, confidence));
    }

    /// <summary>
    /// Makes the final decision
    /// </summary>
    public void MakeDecision(string selectedOption, string decidedBy)
    {
        Guard.Against.NullOrWhiteSpace(selectedOption, nameof(selectedOption));
        Guard.Against.NullOrWhiteSpace(decidedBy, nameof(decidedBy));
        
        if (!ConsideredOptions.Contains(selectedOption))
        {
            throw new InvalidOperationException($"Selected option '{selectedOption}' was not in the considered options");
        }
        
        SelectedOption = selectedOption;
        Status = DecisionStatus.Decided;
        DecidedAt = DateTime.UtcNow;
        MarkAsModified(decidedBy);
        
        AddDomainEvent(new DecisionMadeEvent(Id, ContextName, selectedOption, decidedBy));
    }

    /// <summary>
    /// Adds contextual data to the decision
    /// </summary>
    public void AddContextData(string key, object value)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.Null(value, nameof(value));
        
        ContextData[key] = value;
    }
}

/// <summary>
/// Priority levels for decisions
/// </summary>
public enum DecisionPriority
{
    /// <summary>Low priority decision</summary>
    Low = 0,
    /// <summary>Medium priority decision</summary>
    Medium = 1,
    /// <summary>High priority decision</summary>
    High = 2,
    /// <summary>Critical decision requiring immediate attention</summary>
    Critical = 3
}

/// <summary>
/// Status of a decision
/// </summary>
public enum DecisionStatus
{
    /// <summary>Decision is pending</summary>
    Pending = 0,
    /// <summary>Decision is under review</summary>
    UnderReview = 1,
    /// <summary>Decision has been made</summary>
    Decided = 2,
    /// <summary>Decision has been implemented</summary>
    Implemented = 3,
    /// <summary>Decision was abandoned</summary>
    Abandoned = 4
}

/// <summary>
/// Domain event raised when AI generates a recommendation
/// </summary>
public record AIRecommendationGeneratedEvent(
    Guid ContextId, 
    string ContextName, 
    string Recommendation, 
    double Confidence) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Domain event raised when a decision is made
/// </summary>
public record DecisionMadeEvent(
    Guid ContextId, 
    string ContextName, 
    string SelectedOption, 
    string DecidedBy) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
