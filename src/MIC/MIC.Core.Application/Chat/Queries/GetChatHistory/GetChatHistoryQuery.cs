using System;
using System.Collections.Generic;
using ErrorOr;
using MediatR;

namespace MIC.Core.Application.Chat.Queries.GetChatHistory;

public sealed record GetChatHistoryQuery(
    Guid UserId,
    string SessionId,
    int Limit = 100) : IRequest<ErrorOr<List<ChatMessageDto>>>;

public sealed record ChatMessageDto(
    Guid Id,
    string Query,
    string Response,
    DateTimeOffset Timestamp,
    bool IsSuccessful,
    string? ErrorMessage);
