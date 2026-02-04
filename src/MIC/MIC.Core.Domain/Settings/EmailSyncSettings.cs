namespace MIC.Core.Domain.Settings;

public class EmailSyncSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int HistoryMonths { get; set; } = 6; // Default: 6 months
    public bool DownloadAttachments { get; set; } = true;
    public bool IncludeSentFolder { get; set; } = true;
    public bool IncludeDraftsFolder { get; set; } = false;
    public bool IncludeArchiveFolder { get; set; } = false;
    public DateTime? LastSyncDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
