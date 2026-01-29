using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Emails.Queries.GetEmails;

/// <summary>
/// Handler for GetEmailsQuery.
/// </summary>
public class GetEmailsQueryHandler : IRequestHandler<GetEmailsQuery, ErrorOr<IReadOnlyList<EmailDto>>>
{
    private readonly IEmailRepository _emailRepository;

    public GetEmailsQueryHandler(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<EmailDto>>> Handle(GetEmailsQuery request, CancellationToken cancellationToken)
    {
        var emails = await _emailRepository.GetEmailsAsync(
            request.UserId,
            request.EmailAccountId,
            request.Folder,
            request.IsUnread,
            request.Skip,
            request.Take,
            cancellationToken);

        // Map to DTOs
        var dtos = emails.Select(e => new EmailDto
        {
            Id = e.Id,
            MessageId = e.MessageId,
            Subject = e.Subject,
            FromAddress = e.FromAddress,
            FromName = e.FromName,
            ToRecipients = e.ToRecipients,
            CcRecipients = e.CcRecipients,
            SentDate = e.SentDate,
            ReceivedDate = e.ReceivedDate,
            BodyPreview = e.BodyPreview,
            BodyText = e.BodyText,
            BodyHtml = e.BodyHtml,
            IsRead = e.IsRead,
            IsFlagged = e.IsFlagged,
            HasAttachments = e.HasAttachments,
            Folder = e.Folder,
            Importance = e.Importance,
            AIPriority = e.AIPriority,
            AICategory = e.AICategory,
            Sentiment = e.Sentiment,
            ContainsActionItems = e.ContainsActionItems,
            RequiresResponse = e.RequiresResponse,
            SuggestedResponseBy = e.SuggestedResponseBy,
            AISummary = e.AISummary,
            ExtractedKeywords = e.ExtractedKeywords,
            ActionItems = e.ActionItems,
            IsAIProcessed = e.IsAIProcessed,
            EmailAccountId = e.EmailAccountId,
            ConversationId = e.ConversationId,
            Attachments = e.Attachments.Select(a => new EmailAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                SizeInBytes = a.SizeInBytes,
                Type = a.Type,
                IsProcessed = a.IsProcessed,
                AISummary = a.AISummary
            }).ToList()
        }).ToList();

        return dtos;
    }
}
