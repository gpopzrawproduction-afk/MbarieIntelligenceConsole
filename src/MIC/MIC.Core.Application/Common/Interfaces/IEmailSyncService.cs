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
}