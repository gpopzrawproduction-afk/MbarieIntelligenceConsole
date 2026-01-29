using Ardalis.GuardClauses;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents an email message with AI-enhanced intelligence.
/// Core entity for the Email Intelligence System.
/// </summary>
public class EmailMessage : BaseEntity
{
    #region Core Email Properties

    /// <summary>
    /// External email ID from provider (e.g., Microsoft Graph message ID)
    /// </summary>
    public string MessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Sender display name
    /// </summary>
    public string FromName { get; private set; } = string.Empty;

    /// <summary>
    /// To recipients (semicolon-separated)
    /// </summary>
    public string ToRecipients { get; private set; } = string.Empty;

    /// <summary>
    /// CC recipients (semicolon-separated)
    /// </summary>
    public string? CcRecipients { get; private set; }

    /// <summary>
    /// BCC recipients (semicolon-separated)
    /// </summary>
    public string? BccRecipients { get; private set; }

    /// <summary>
    /// When the email was sent
    /// </summary>
    public DateTime SentDate { get; private set; }

    /// <summary>
    /// When the email was received
    /// </summary>
    public DateTime ReceivedDate { get; private set; }

    /// <summary>
    /// Plain text body content
    /// </summary>
    public string BodyText { get; private set; } = string.Empty;

    /// <summary>
    /// HTML body content
    /// </summary>
    public string? BodyHtml { get; private set; }

    /// <summary>
    /// Preview text (first ~200 chars)
    /// </summary>
    public string? BodyPreview { get; private set; }

    /// <summary>
    /// Has this email been read
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Is this email flagged/starred
    /// </summary>
    public bool IsFlagged { get; private set; }

    /// <summary>
    /// Is this a draft
    /// </summary>
    public bool IsDraft { get; private set; }

    /// <summary>
    /// Has attachments
    /// </summary>
    public bool HasAttachments { get; private set; }

    /// <summary>
    /// Email folder location
    /// </summary>
    public EmailFolder Folder { get; private set; }

    /// <summary>
    /// Original importance from email client
    /// </summary>
    public EmailImportance Importance { get; private set; }

    #endregion

    #region AI Intelligence Properties

    /// <summary>
    /// AI-determined priority level
    /// </summary>
    public EmailPriority AIPriority { get; private set; }

    /// <summary>
    /// AI-determined category
    /// </summary>
    public EmailCategory AICategory { get; private set; }

    /// <summary>
    /// AI-detected sentiment
    /// </summary>
    public SentimentType Sentiment { get; private set; }

    /// <summary>
    /// AI detected action items in this email
    /// </summary>
    public bool ContainsActionItems { get; private set; }

    /// <summary>
    /// AI determined this email requires a response
    /// </summary>
    public bool RequiresResponse { get; private set; }

    /// <summary>
    /// AI suggested response deadline
    /// </summary>
    public DateTime? SuggestedResponseBy { get; private set; }

    /// <summary>
    /// AI-generated summary of the email
    /// </summary>
    public string? AISummary { get; private set; }

    /// <summary>
    /// AI-extracted keywords for search
    /// </summary>
    public List<string> ExtractedKeywords { get; private set; } = new();

    /// <summary>
    /// AI-extracted action items
    /// </summary>
    public List<string> ActionItems { get; private set; } = new();

    /// <summary>
    /// AI confidence score (0.0 - 1.0)
    /// </summary>
    public double AIConfidenceScore { get; private set; }

    /// <summary>
    /// Has this email been processed by AI
    /// </summary>
    public bool IsAIProcessed { get; private set; }

    /// <summary>
    /// When AI analysis was completed
    /// </summary>
    public DateTime? AIProcessedAt { get; private set; }

    #endregion

	#region Inbox UX Properties

	/// <summary>
	/// Primary priority used for inbox display.
	/// Typically aligned with AIPriority but can be overridden.
	/// </summary>
	public EmailPriority Priority { get; private set; } = EmailPriority.Normal;

	/// <summary>
	/// Marks this email as urgent for visual emphasis.
	/// </summary>
	public bool IsUrgent { get; private set; }

	#endregion

    #region Relationships

    /// <summary>
    /// User who owns this email
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Email account this message belongs to
    /// </summary>
    public Guid EmailAccountId { get; private set; }

    /// <summary>
    /// Conversation/thread ID for grouping
    /// </summary>
    public string? ConversationId { get; private set; }

    /// <summary>
    /// In-reply-to message ID
    /// </summary>
    public string? InReplyTo { get; private set; }

    /// <summary>
    /// Attachments collection
    /// </summary>
    public List<EmailAttachment> Attachments { get; private set; } = new();

    /// <summary>
    /// Knowledge base entry ID if indexed
    /// </summary>
    public Guid? KnowledgeEntryId { get; private set; }

    #endregion

    #region Constructors

    private EmailMessage() { } // EF Core

    public EmailMessage(
        string messageId,
        string subject,
        string fromAddress,
        string fromName,
        string toRecipients,
        DateTime sentDate,
        DateTime receivedDate,
        string bodyText,
        Guid userId,
        Guid emailAccountId,
        EmailFolder folder = EmailFolder.Inbox)
    {
        MessageId = Guard.Against.NullOrWhiteSpace(messageId);
        Subject = subject ?? "(No Subject)";
        FromAddress = Guard.Against.NullOrWhiteSpace(fromAddress);
        FromName = fromName ?? fromAddress;
        ToRecipients = Guard.Against.NullOrWhiteSpace(toRecipients);
        SentDate = sentDate;
        ReceivedDate = receivedDate;
        BodyText = bodyText ?? string.Empty;
        UserId = Guard.Against.Default(userId);
        EmailAccountId = Guard.Against.Default(emailAccountId);
        Folder = folder;

        // Generate preview
        BodyPreview = bodyText?.Length > 200 ? bodyText[..200] + "..." : bodyText;

        // Initialize defaults
        AIPriority = EmailPriority.Normal;
        AICategory = EmailCategory.General;
        Sentiment = SentimentType.Neutral;
        Importance = EmailImportance.Normal;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Sets the AI analysis results for this email.
    /// </summary>
    public void SetAIAnalysis(
        EmailPriority priority,
        EmailCategory category,
        SentimentType sentiment,
        bool hasActionItems,
        bool requiresResponse,
        string? summary,
        List<string>? keywords = null,
        List<string>? actionItems = null,
        double confidenceScore = 0.8)
    {
        AIPriority = priority;
        AICategory = category;
        Sentiment = sentiment;
        ContainsActionItems = hasActionItems;
        RequiresResponse = requiresResponse;
        AISummary = summary;
        AIConfidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0);
        IsAIProcessed = true;
        AIProcessedAt = DateTime.UtcNow;

        if (keywords != null)
        {
            ExtractedKeywords = keywords;
        }

        if (actionItems != null)
        {
            ActionItems = actionItems;
        }

        // Set suggested response deadline based on priority
        if (requiresResponse)
        {
            SuggestedResponseBy = priority switch
            {
                EmailPriority.Urgent => DateTime.UtcNow.AddHours(2),
                EmailPriority.High => DateTime.UtcNow.AddHours(24),
                EmailPriority.Normal => DateTime.UtcNow.AddDays(3),
                EmailPriority.Low => DateTime.UtcNow.AddDays(7),
                _ => null
            };
        }
    }

	/// <summary>
	/// Sets inbox-related flags used by the UI.
	/// </summary>
	public void SetInboxFlags(EmailPriority priority, bool isUrgent, bool isRead, bool requiresResponse, bool containsActionItems)
	{
		Priority = priority;
		IsUrgent = isUrgent;
		if (isRead)
		{
			IsRead = true;
		}
		RequiresResponse = requiresResponse;
		ContainsActionItems = containsActionItems;
	}

    /// <summary>
    /// Marks the email as read.
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }

    /// <summary>
    /// Marks the email as unread.
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
    }

    /// <summary>
    /// Toggles the flagged status.
    /// </summary>
    public void ToggleFlag()
    {
        IsFlagged = !IsFlagged;
    }

    /// <summary>
    /// Moves email to a different folder.
    /// </summary>
    public void MoveToFolder(EmailFolder folder)
    {
        Folder = folder;
    }

    /// <summary>
    /// Adds an attachment to this email.
    /// </summary>
    public void AddAttachment(EmailAttachment attachment)
    {
        Guard.Against.Null(attachment);
        Attachments.Add(attachment);
        HasAttachments = true;
    }

    /// <summary>
    /// Sets HTML body content.
    /// </summary>
    public void SetHtmlBody(string? htmlBody)
    {
        BodyHtml = htmlBody;
    }

    /// <summary>
    /// Sets conversation threading information.
    /// </summary>
    public void SetThreadInfo(string? conversationId, string? inReplyTo)
    {
        ConversationId = conversationId;
        InReplyTo = inReplyTo;
    }

    /// <summary>
    /// Links this email to a knowledge base entry.
    /// </summary>
    public void LinkToKnowledgeBase(Guid knowledgeEntryId)
    {
        KnowledgeEntryId = knowledgeEntryId;
    }

    #endregion
}

#region Enums

/// <summary>
/// AI-determined email priority
/// </summary>
public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// AI-determined email category
/// </summary>
public enum EmailCategory
{
    General = 0,
    Meeting = 1,
    Project = 2,
    Decision = 3,
    Action = 4,
    Report = 5,
    FYI = 6,
    Social = 7,
    Newsletter = 8,
    Spam = 9
}

/// <summary>
/// Email folder location
/// </summary>
public enum EmailFolder
{
    Inbox = 0,
    Sent = 1,
    Drafts = 2,
    Archive = 3,
    Junk = 4,
    Trash = 5,
    Custom = 99
}

/// <summary>
/// Original email importance from client
/// </summary>
public enum EmailImportance
{
    Low = 0,
    Normal = 1,
    High = 2
}

/// <summary>
/// AI-detected sentiment type
/// </summary>
public enum SentimentType
{
    VeryNegative = 0,
    Negative = 1,
    Neutral = 2,
    Positive = 3,
    VeryPositive = 4
}

#endregion
