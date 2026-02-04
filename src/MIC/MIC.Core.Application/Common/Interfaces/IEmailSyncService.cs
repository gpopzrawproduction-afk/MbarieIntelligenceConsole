using MIC.Core.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MIC.Core.Application.Common.Interfaces;

public class EmailSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int NewEmailsCount { get; set; }
    public int TotalEmailsChecked { get; set; }
    public DateTime SyncedAt { get; set; }
}

public interface IEmailSyncService
{
    Task<EmailSyncResult> SyncAccountAsync(EmailAccount account, CancellationToken ct = default);
    
    // Historical sync result used for long-running historical synchronization
    public class HistoricalSyncResult
    {
        public Guid UserId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int TotalEmailsFound { get; set; }
        public int EmailsSynced { get; set; }
        public SyncStatus Status { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public enum SyncStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        NoAccountsConfigured
    }

    public class SyncProgress
    {
        public Guid UserId { get; set; }
        public string? AccountEmail { get; set; }
        public int TotalFound { get; set; }
        public int Processed { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Synchronize historical emails for a user using provided settings. Progress updates are reported via IProgress.
    /// </summary>
    Task<HistoricalSyncResult> SyncHistoricalEmailsAsync(Guid userId, MIC.Core.Domain.Settings.EmailSyncSettings settings, IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);
}