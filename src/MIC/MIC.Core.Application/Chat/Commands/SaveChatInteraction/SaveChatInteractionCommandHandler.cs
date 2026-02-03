using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Chat.Commands.SaveChatInteraction;

public sealed class SaveChatInteractionCommandHandler
    : IRequestHandler<SaveChatInteractionCommand, ErrorOr<Guid>>
{
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveChatInteractionCommandHandler(
        IChatHistoryRepository chatHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _chatHistoryRepository = chatHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Guid>> Handle(
        SaveChatInteractionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = new ChatHistory(
                userId: request.UserId,
                sessionId: request.SessionId,
                query: request.Query,
                response: request.Response)
            {
                Timestamp = request.Timestamp ?? DateTimeOffset.UtcNow,
                AIProvider = request.AiProvider,
                ModelUsed = request.ModelUsed,
                TokenCount = request.TokenCount ?? 0,
                IsSuccessful = request.IsSuccessful,
                ErrorMessage = request.ErrorMessage,
                Context = request.Context,
                Metadata = request.Metadata
            };

            await _chatHistoryRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entry.Id;
        }
        catch (Exception ex)
        {
            return Error.Failure("ChatHistory.SaveFailed", ex.Message);
        }
    }
}
