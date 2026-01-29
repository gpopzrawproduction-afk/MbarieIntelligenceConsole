using Ardalis.GuardClauses;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// Represents an email attachment with extracted intelligence.
/// Supports document parsing and knowledge base integration.
/// </summary>
public class EmailAttachment : BaseEntity
{
    #region Properties

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// MIME content type
    /// </summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeInBytes { get; private set; }

    /// <summary>
    /// Path in blob/file storage
    /// </summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>
    /// Parent email message ID
    /// </summary>
    public Guid EmailMessageId { get; private set; }

    /// <summary>
    /// External attachment ID from email provider
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// Determined attachment type
    /// </summary>
    public AttachmentType Type { get; private set; }

    #endregion

    #region Content Extraction

    /// <summary>
    /// Extracted text content from the attachment
    /// </summary>
    public string? ExtractedText { get; private set; }

    /// <summary>
    /// Has this attachment been processed for text extraction
    /// </summary>
    public bool IsProcessed { get; private set; }

    /// <summary>
    /// When content extraction was completed
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Number of pages (for documents)
    /// </summary>
    public int? PageCount { get; private set; }

    /// <summary>
    /// Word count of extracted text
    /// </summary>
    public int? WordCount { get; private set; }

    /// <summary>
    /// Processing error message if failed
    /// </summary>
    public string? ProcessingError { get; private set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public ProcessingStatus Status { get; private set; }

    #endregion

    #region AI Analysis

    /// <summary>
    /// AI-generated summary of document content
    /// </summary>
    public string? AISummary { get; private set; }

    /// <summary>
    /// AI-extracted keywords
    /// </summary>
    public List<string> ExtractedKeywords { get; private set; } = new();

    /// <summary>
    /// Document category determined by AI
    /// </summary>
    public DocumentCategory? DocumentCategory { get; private set; }

    /// <summary>
    /// Confidence score of document classification
    /// </summary>
    public double? ClassificationConfidence { get; private set; }

    #endregion

    #region Knowledge Base

    /// <summary>
    /// Knowledge base entry ID if indexed
    /// </summary>
    public Guid? KnowledgeEntryId { get; private set; }

    /// <summary>
    /// Vector embedding ID for semantic search
    /// </summary>
    public string? EmbeddingId { get; private set; }

    /// <summary>
    /// Has this been indexed in knowledge base
    /// </summary>
    public bool IsIndexed { get; private set; }

    /// <summary>
    /// When indexing was completed
    /// </summary>
    public DateTime? IndexedAt { get; private set; }

    #endregion

    #region Constructors

    private EmailAttachment() { } // EF Core

    public EmailAttachment(
        string fileName,
        string contentType,
        long sizeInBytes,
        string storagePath,
        Guid emailMessageId,
        string? externalId = null)
    {
        FileName = Guard.Against.NullOrWhiteSpace(fileName);
        ContentType = Guard.Against.NullOrWhiteSpace(contentType);
        SizeInBytes = Guard.Against.NegativeOrZero(sizeInBytes);
        StoragePath = Guard.Against.NullOrWhiteSpace(storagePath);
        EmailMessageId = Guard.Against.Default(emailMessageId);
        ExternalId = externalId;

        Type = DetermineType(fileName, contentType);
        Status = ProcessingStatus.Pending;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Sets the extracted text content from document processing.
    /// </summary>
    public void SetExtractedContent(string text, int? pageCount = null)
    {
        ExtractedText = text;
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        PageCount = pageCount;
        WordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        Status = ProcessingStatus.Completed;
        ProcessingError = null;
    }

    /// <summary>
    /// Marks processing as failed with error message.
    /// </summary>
    public void SetProcessingFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ProcessingError = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets AI analysis results for the attachment.
    /// </summary>
    public void SetAIAnalysis(
        string? summary,
        List<string>? keywords,
        DocumentCategory? category,
        double? confidence)
    {
        AISummary = summary;
        
        if (keywords != null)
        {
            ExtractedKeywords = keywords;
        }

        DocumentCategory = category;
        ClassificationConfidence = confidence;
    }

    /// <summary>
    /// Links this attachment to a knowledge base entry.
    /// </summary>
    public void LinkToKnowledgeBase(Guid knowledgeEntryId, string? embeddingId = null)
    {
        KnowledgeEntryId = knowledgeEntryId;
        EmbeddingId = embeddingId;
        IsIndexed = true;
        IndexedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a human-readable file size.
    /// </summary>
    public string GetFormattedSize()
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

    /// <summary>
    /// Determines if this attachment can be processed for text extraction.
    /// </summary>
    public bool CanExtractText()
    {
        return Type is AttachmentType.PDF 
            or AttachmentType.Word 
            or AttachmentType.Excel 
            or AttachmentType.PowerPoint 
            or AttachmentType.Text;
    }

    /// <summary>
    /// Determines the attachment type from filename and content type.
    /// </summary>
    private static AttachmentType DetermineType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".pdf" => AttachmentType.PDF,
            ".docx" or ".doc" => AttachmentType.Word,
            ".xlsx" or ".xls" or ".csv" => AttachmentType.Excel,
            ".pptx" or ".ppt" => AttachmentType.PowerPoint,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => AttachmentType.Image,
            ".txt" or ".md" or ".rtf" => AttachmentType.Text,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => AttachmentType.Archive,
            ".mp3" or ".wav" or ".m4a" => AttachmentType.Audio,
            ".mp4" or ".avi" or ".mov" or ".mkv" => AttachmentType.Video,
            ".eml" or ".msg" => AttachmentType.Email,
            ".ics" or ".vcf" => AttachmentType.Calendar,
            _ => DetermineFromContentType(contentType)
        };
    }

    private static AttachmentType DetermineFromContentType(string contentType)
    {
        if (contentType.StartsWith("image/")) return AttachmentType.Image;
        if (contentType.StartsWith("audio/")) return AttachmentType.Audio;
        if (contentType.StartsWith("video/")) return AttachmentType.Video;
        if (contentType.Contains("pdf")) return AttachmentType.PDF;
        if (contentType.Contains("word")) return AttachmentType.Word;
        if (contentType.Contains("excel") || contentType.Contains("spreadsheet")) return AttachmentType.Excel;
        if (contentType.Contains("powerpoint") || contentType.Contains("presentation")) return AttachmentType.PowerPoint;
        if (contentType.StartsWith("text/")) return AttachmentType.Text;
        
        return AttachmentType.Other;
    }

    #endregion
}

#region Enums

/// <summary>
/// Type of email attachment
/// </summary>
public enum AttachmentType
{
    PDF = 0,
    Word = 1,
    Excel = 2,
    PowerPoint = 3,
    Image = 4,
    Text = 5,
    Archive = 6,
    Audio = 7,
    Video = 8,
    Email = 9,
    Calendar = 10,
    Other = 99
}

/// <summary>
/// Processing status for attachment content extraction
/// </summary>
public enum ProcessingStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}

/// <summary>
/// AI-determined document category
/// </summary>
public enum DocumentCategory
{
    Contract = 0,
    Report = 1,
    Invoice = 2,
    Proposal = 3,
    Specification = 4,
    Manual = 5,
    Presentation = 6,
    Spreadsheet = 7,
    Correspondence = 8,
    Legal = 9,
    Financial = 10,
    Technical = 11,
    Marketing = 12,
    HR = 13,
    Other = 99
}

#endregion
