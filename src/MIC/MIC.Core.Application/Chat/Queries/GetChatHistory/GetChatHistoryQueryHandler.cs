using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Chat.Queries.GetChatHistory;

public sealed class GetChatHistoryQueryHandler
    : IRequestHandler<GetChatHistoryQuery, ErrorOr<List<ChatMessageDto>>>
{
    private readonly IChatHistoryRepository _chatHistoryRepository;

    public GetChatHistoryQueryHandler(IChatHistoryRepository chatHistoryRepository)
    {
        _chatHistoryRepository = chatHistoryRepository;
    }

    public async Task<ErrorOr<List<ChatMessageDto>>> Handle(
        GetChatHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return Error.Validation("ChatHistory.SessionIdRequired", "SessionId is required.");
            }

            var limit = request.Limit <= 0 ? 100 : Math.Min(request.Limit, 500);
            var entries = await _chatHistoryRepository
                .GetBySessionAsync(request.UserId, request.SessionId, limit, cancellationToken)
                .ConfigureAwait(false);

            return entries.Select(e => new ChatMessageDto(
                    e.Id,
                    e.Query,
                    e.Response,
                    e.Timestamp,
                    e.IsSuccessful,
                    e.ErrorMessage))
                .ToList();
        }
        catch (Exception ex)
        {
            return Error.Failure("ChatHistory.LoadFailed", ex.Message);
        }
    }
}
