using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Chat.Commands.ClearChatSession;

public sealed class ClearChatSessionCommandHandler
    : IRequestHandler<ClearChatSessionCommand, ErrorOr<bool>>
{
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClearChatSessionCommandHandler(
        IChatHistoryRepository chatHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _chatHistoryRepository = chatHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<bool>> Handle(
        ClearChatSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return Error.Validation("ChatHistory.SessionIdRequired", "SessionId is required.");
            }

            await _chatHistoryRepository
                .DeleteBySessionAsync(request.UserId, request.SessionId, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            return Error.Failure("ChatHistory.ClearFailed", ex.Message);
        }
    }
}
