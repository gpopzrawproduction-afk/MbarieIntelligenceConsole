using Ardalis.GuardClauses;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents a connected email account (Outlook, Gmail, Exchange).
/// Manages OAuth tokens and sync state for email intelligence.
/// </summary>
public class EmailAccount : BaseEntity
{
    #region Account Information

    /// <summary>
    /// Email address for this account
    /// </summary>
    public string EmailAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for this account
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// Email provider type
    /// </summary>
    public EmailProvider Provider { get; private set; }

    /// <summary>
    /// User who owns this account
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Is this account active for syncing
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Is this the primary email account
    /// </summary>
    public bool IsPrimary { get; private set; }

    #endregion

    #region OAuth Tokens (Encrypted)

    /// <summary>
    /// Encrypted OAuth access token
    /// </summary>
    public string? AccessTokenEncrypted { get; private set; }

    /// <summary>
    /// Encrypted OAuth refresh token
    /// </summary>
    public string? RefreshTokenEncrypted { get; private set; }

    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime? TokenExpiresAt { get; private set; }

    /// <summary>
    /// OAuth scopes granted
    /// </summary>
    public string? GrantedScopes { get; private set; }

    #endregion

    #region Sync State

    /// <summary>
    /// Current sync status
    /// </summary>
    public SyncStatus Status { get; private set; }

    /// <summary>
    /// Last successful sync timestamp
    /// </summary>
    public DateTime? LastSyncedAt { get; private set; }

    /// <summary>
    /// Last sync attempt timestamp
    /// </summary>
    public DateTime? LastSyncAttemptAt { get; private set; }

    /// <summary>
    /// Total emails synced for this account
    /// </summary>
    public int TotalEmailsSynced { get; private set; }

    /// <summary>
    /// Total attachments synced
    /// </summary>
    public int TotalAttachmentsSynced { get; private set; }

    /// <summary>
    /// Delta link for incremental sync (Microsoft Graph)
    /// </summary>
    public string? DeltaLink { get; private set; }

    /// <summary>
    /// History ID for incremental sync (Gmail)
    /// </summary>
    public string? HistoryId { get; private set; }

    /// <summary>
    /// Last sync error message
    /// </summary>
    public string? LastSyncError { get; private set; }

    /// <summary>
    /// Count of consecutive sync failures
    /// </summary>
    public int ConsecutiveFailures { get; private set; }

    #endregion

    #region Sync Settings

    /// <summary>
    /// Sync frequency in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; private set; }

    /// <summary>
    /// How far back to sync on initial setup (days)
    /// </summary>
    public int InitialSyncDays { get; private set; }

    /// <summary>
    /// Should attachments be downloaded
    /// </summary>
    public bool SyncAttachments { get; private set; }

    /// <summary>
    /// Maximum attachment size to download (MB)
    /// </summary>
    public int MaxAttachmentSizeMB { get; private set; }

    /// <summary>
    /// Folders to sync (null = all)
    /// </summary>
    public List<string>? FoldersToSync { get; private set; }

    #endregion

    #region Statistics

    /// <summary>
    /// Storage used by this account in bytes
    /// </summary>
    public long StorageUsedBytes { get; private set; }

    /// <summary>
    /// Count of unread emails
    /// </summary>
    public int UnreadCount { get; private set; }

    /// <summary>
    /// Count of emails requiring response
    /// </summary>
    public int RequiresResponseCount { get; private set; }

    #endregion

    #region Constructors

    private EmailAccount() { } // EF Core

    public EmailAccount(
        string emailAddress,
        EmailProvider provider,
        Guid userId,
        string? displayName = null)
    {
        EmailAddress = Guard.Against.NullOrWhiteSpace(emailAddress);
        Provider = provider;
        UserId = Guard.Against.Default(userId);
        DisplayName = displayName ?? emailAddress;

        IsActive = true;
        IsPrimary = true;
        Status = SyncStatus.NotStarted;

        // Default sync settings
        SyncIntervalMinutes = 5;
        InitialSyncDays = 365; // 1 year of history
        SyncAttachments = true;
        MaxAttachmentSizeMB = 25;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Sets OAuth tokens for this account.
    /// </summary>
    public void SetTokens(string accessToken, string? refreshToken, DateTime expiresAt, string? scopes = null)
    {
        // In production, these should be encrypted before storing
        AccessTokenEncrypted = accessToken;
        RefreshTokenEncrypted = refreshToken;
        TokenExpiresAt = expiresAt;
        GrantedScopes = scopes;
        
        // Clear any previous errors when tokens are refreshed
        LastSyncError = null;
        ConsecutiveFailures = 0;
    }

    /// <summary>
    /// Checks if the access token is expired or about to expire.
    /// </summary>
    public bool IsTokenExpired(TimeSpan? buffer = null)
    {
        if (!TokenExpiresAt.HasValue)
            return true;

        buffer ??= TimeSpan.FromMinutes(5);
        return DateTime.UtcNow.Add(buffer.Value) >= TokenExpiresAt.Value;
    }

    /// <summary>
    /// Updates sync status and statistics.
    /// </summary>
    public void UpdateSyncStatus(SyncStatus status, int emailsProcessed = 0, int attachmentsProcessed = 0)
    {
        Status = status;
        LastSyncAttemptAt = DateTime.UtcNow;

        if (status == SyncStatus.Completed)
        {
            TotalEmailsSynced += emailsProcessed;
            TotalAttachmentsSynced += attachmentsProcessed;
            LastSyncedAt = DateTime.UtcNow;
            LastSyncError = null;
            ConsecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Records a sync failure.
    /// </summary>
    public void SetSyncFailed(string errorMessage)
    {
        Status = SyncStatus.Failed;
        LastSyncAttemptAt = DateTime.UtcNow;
        LastSyncError = errorMessage;
        ConsecutiveFailures++;

        // Deactivate after 5 consecutive failures
        if (ConsecutiveFailures >= 5)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Sets the delta link for incremental sync.
    /// </summary>
    public void SetDeltaLink(string deltaLink)
    {
        DeltaLink = deltaLink;
    }

    /// <summary>
    /// Sets the history ID for Gmail incremental sync.
    /// </summary>
    public void SetHistoryId(string historyId)
    {
        HistoryId = historyId;
    }

    /// <summary>
    /// Updates sync settings.
    /// </summary>
    public void UpdateSyncSettings(
        int? syncIntervalMinutes = null,
        int? initialSyncDays = null,
        bool? syncAttachments = null,
        int? maxAttachmentSizeMB = null,
        List<string>? foldersToSync = null)
    {
        if (syncIntervalMinutes.HasValue)
            SyncIntervalMinutes = Math.Max(1, Math.Min(1440, syncIntervalMinutes.Value));

        if (initialSyncDays.HasValue)
            InitialSyncDays = Math.Max(1, Math.Min(3650, initialSyncDays.Value));

        if (syncAttachments.HasValue)
            SyncAttachments = syncAttachments.Value;

        if (maxAttachmentSizeMB.HasValue)
            MaxAttachmentSizeMB = Math.Max(1, Math.Min(100, maxAttachmentSizeMB.Value));

        if (foldersToSync != null)
            FoldersToSync = foldersToSync;
    }

    /// <summary>
    /// Updates account statistics.
    /// </summary>
    public void UpdateStatistics(long storageUsed, int unreadCount, int requiresResponseCount)
    {
        StorageUsedBytes = storageUsed;
        UnreadCount = unreadCount;
        RequiresResponseCount = requiresResponseCount;
    }

    /// <summary>
    /// Activates this account for syncing.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ConsecutiveFailures = 0;
        LastSyncError = null;
    }

    /// <summary>
    /// Deactivates this account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Sets this as the primary account.
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Removes primary status.
    /// </summary>
    public void RemovePrimary()
    {
        IsPrimary = false;
    }

    /// <summary>
    /// Gets formatted storage used string.
    /// </summary>
    public string GetFormattedStorageUsed()
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = StorageUsedBytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Determines if this account should sync now.
    /// </summary>
    public bool ShouldSyncNow()
    {
        if (!IsActive) return false;
        if (Status == SyncStatus.InProgress) return false;
        if (!LastSyncedAt.HasValue) return true;

        return DateTime.UtcNow.Subtract(LastSyncedAt.Value).TotalMinutes >= SyncIntervalMinutes;
    }

    #endregion
}

#region Enums

/// <summary>
/// Email provider type
/// </summary>
public enum EmailProvider
{
    /// <summary>
    /// Microsoft 365 / Outlook
    /// </summary>
    Outlook = 0,

    /// <summary>
    /// Google Gmail
    /// </summary>
    Gmail = 1,

    /// <summary>
    /// Microsoft Exchange Server (on-premises)
    /// </summary>
    Exchange = 2,

    /// <summary>
    /// Generic IMAP server
    /// </summary>
    IMAP = 3
}

/// <summary>
/// Email sync status
/// </summary>
public enum SyncStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Paused = 4,
    Cancelled = 5
}

#endregion
