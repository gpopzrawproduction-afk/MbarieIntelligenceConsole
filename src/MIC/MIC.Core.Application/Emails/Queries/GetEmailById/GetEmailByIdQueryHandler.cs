using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;

namespace MIC.Core.Application.Emails.Queries.GetEmailById;

/// <summary>
/// Handler for GetEmailByIdQuery.
/// </summary>
public class GetEmailByIdQueryHandler : IRequestHandler<GetEmailByIdQuery, ErrorOr<EmailDto?>>
{
    private readonly IEmailRepository _emailRepository;

    public GetEmailByIdQueryHandler(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository;
    }

    public async Task<ErrorOr<EmailDto?>> Handle(GetEmailByIdQuery request, CancellationToken cancellationToken)
    {
        var email = await _emailRepository.GetByIdAsync(request.EmailId, cancellationToken);

        if (email == null)
            return (EmailDto?)null;

        return new EmailDto
        {
            Id = email.Id,
            MessageId = email.MessageId,
            Subject = email.Subject,
            FromAddress = email.FromAddress,
            FromName = email.FromName,
            ToRecipients = email.ToRecipients,
            CcRecipients = email.CcRecipients,
            SentDate = email.SentDate,
            ReceivedDate = email.ReceivedDate,
            BodyPreview = email.BodyPreview,
            BodyText = email.BodyText,
            BodyHtml = email.BodyHtml,
            IsRead = email.IsRead,
            IsFlagged = email.IsFlagged,
            HasAttachments = email.HasAttachments,
            Folder = email.Folder,
            Importance = email.Importance,
            AIPriority = email.AIPriority,
            AICategory = email.AICategory,
            Sentiment = email.Sentiment,
            ContainsActionItems = email.ContainsActionItems,
            RequiresResponse = email.RequiresResponse,
            SuggestedResponseBy = email.SuggestedResponseBy,
            AISummary = email.AISummary,
            ExtractedKeywords = email.ExtractedKeywords,
            ActionItems = email.ActionItems,
            IsAIProcessed = email.IsAIProcessed,
            EmailAccountId = email.EmailAccountId,
            ConversationId = email.ConversationId,
            Attachments = email.Attachments.Select(a => new EmailAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                SizeInBytes = a.SizeInBytes,
                Type = a.Type,
                IsProcessed = a.IsProcessed,
                AISummary = a.AISummary
            }).ToList()
        };
    }
}
