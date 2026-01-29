using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Emails.Common;

/// <summary>
/// Data transfer object for EmailMessage.
/// </summary>
public record EmailDto
{
    public Guid Id { get; init; }
    public string MessageId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string ToRecipients { get; init; } = string.Empty;
    public string? CcRecipients { get; init; }
    public DateTime SentDate { get; init; }
    public DateTime ReceivedDate { get; init; }
    public string? BodyPreview { get; init; }
    public string BodyText { get; init; } = string.Empty;
    public string? BodyHtml { get; init; }
    public bool IsRead { get; init; }
    public bool IsFlagged { get; init; }
    public bool HasAttachments { get; init; }
    public EmailFolder Folder { get; init; }
    public EmailImportance Importance { get; init; }

    // AI Intelligence
    public EmailPriority AIPriority { get; init; }
    public EmailCategory AICategory { get; init; }
    public SentimentType Sentiment { get; init; }
    public bool ContainsActionItems { get; init; }
    public bool RequiresResponse { get; init; }
    public DateTime? SuggestedResponseBy { get; init; }
    public string? AISummary { get; init; }
    public List<string> ExtractedKeywords { get; init; } = new();
    public List<string> ActionItems { get; init; } = new();
    public bool IsAIProcessed { get; init; }

    // Relationships
    public Guid EmailAccountId { get; init; }
    public string? ConversationId { get; init; }
    public List<EmailAttachmentDto> Attachments { get; init; } = new();

    // Display helpers
    public string SenderDisplay => !string.IsNullOrEmpty(FromName) ? FromName : FromAddress;
    public string TimeAgo => GetTimeAgo(ReceivedDate);
    
    public string PriorityColor => AIPriority switch
    {
        EmailPriority.Urgent => "#FF0055",
        EmailPriority.High => "#FF6B00",
        EmailPriority.Normal => "#00E5FF",
        EmailPriority.Low => "#607D8B",
        _ => "#607D8B"
    };

    public string CategoryIcon => AICategory switch
    {
        EmailCategory.Meeting => "??",
        EmailCategory.Project => "??",
        EmailCategory.Decision => "?",
        EmailCategory.Action => "?",
        EmailCategory.Report => "??",
        EmailCategory.FYI => "?",
        EmailCategory.Newsletter => "??",
        _ => "?"
    };

    public string SentimentIcon => Sentiment switch
    {
        SentimentType.VeryPositive => "??",
        SentimentType.Positive => "??",
        SentimentType.Neutral => "??",
        SentimentType.Negative => "??",
        SentimentType.VeryNegative => "??",
        _ => "??"
    };

    private static string GetTimeAgo(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)}w ago";
        
        return date.ToString("MMM dd");
    }
}

/// <summary>
/// Data transfer object for EmailAttachment.
/// </summary>
public record EmailAttachmentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
    public AttachmentType Type { get; init; }
    public bool IsProcessed { get; init; }
    public string? AISummary { get; init; }

    public string FormattedSize
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = SizeInBytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public string TypeIcon => Type switch
    {
        AttachmentType.PDF => "??",
        AttachmentType.Word => "??",
        AttachmentType.Excel => "??",
        AttachmentType.PowerPoint => "??",
        AttachmentType.Image => "??",
        AttachmentType.Archive => "??",
        _ => "??"
    };
}

/// <summary>
/// Data transfer object for EmailAccount.
/// </summary>
public record EmailAccountDto
{
    public Guid Id { get; init; }
    public string EmailAddress { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public EmailProvider Provider { get; init; }
    public bool IsActive { get; init; }
    public bool IsPrimary { get; init; }
    public SyncStatus Status { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public int TotalEmailsSynced { get; init; }
    public int UnreadCount { get; init; }
    public int RequiresResponseCount { get; init; }
    public string? LastSyncError { get; init; }

    public string ProviderName => Provider switch
    {
        EmailProvider.Outlook => "Microsoft 365",
        EmailProvider.Gmail => "Gmail",
        EmailProvider.Exchange => "Exchange",
        EmailProvider.IMAP => "IMAP",
        _ => "Unknown"
    };

    public string StatusText => Status switch
    {
        SyncStatus.NotStarted => "Not synced",
        SyncStatus.InProgress => "Syncing...",
        SyncStatus.Completed => "Up to date",
        SyncStatus.Failed => "Sync failed",
        SyncStatus.Paused => "Paused",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        SyncStatus.Completed => "#39FF14",
        SyncStatus.InProgress => "#00E5FF",
        SyncStatus.Failed => "#FF0055",
        SyncStatus.Paused => "#FF6B00",
        _ => "#607D8B"
    };
}
