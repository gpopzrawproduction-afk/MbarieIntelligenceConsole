using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Emails.Queries.GetEmails;

/// <summary>
/// Query to get paginated emails with filtering options.
/// </summary>
public record GetEmailsQuery : IQuery<IReadOnlyList<Common.EmailDto>>
{
    /// <summary>
    /// User ID to filter emails
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Optional email account ID filter
    /// </summary>
    public Guid? EmailAccountId { get; init; }

    /// <summary>
    /// Optional folder filter
    /// </summary>
    public EmailFolder? Folder { get; init; }

    /// <summary>
    /// Optional category filter
    /// </summary>
    public EmailCategory? Category { get; init; }

    /// <summary>
    /// Optional priority filter
    /// </summary>
    public EmailPriority? Priority { get; init; }

    /// <summary>
    /// Filter unread only
    /// </summary>
    public bool? IsUnread { get; init; }

    /// <summary>
    /// Filter flagged only
    /// </summary>
    public bool? IsFlagged { get; init; }

    /// <summary>
    /// Filter emails requiring response
    /// </summary>
    public bool? RequiresResponse { get; init; }

    /// <summary>
    /// Filter emails with action items
    /// </summary>
    public bool? HasActionItems { get; init; }

    /// <summary>
    /// Search text (subject, from, body)
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Number of items to skip
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Number of items to take
    /// </summary>
    public int Take { get; init; } = 50;

    /// <summary>
    /// Sort by field
    /// </summary>
    public EmailSortBy SortBy { get; init; } = EmailSortBy.ReceivedDate;

    /// <summary>
    /// Sort descending
    /// </summary>
    public bool SortDescending { get; init; } = true;
}

public enum EmailSortBy
{
    ReceivedDate,
    SentDate,
    Subject,
    FromAddress,
    Priority
}
