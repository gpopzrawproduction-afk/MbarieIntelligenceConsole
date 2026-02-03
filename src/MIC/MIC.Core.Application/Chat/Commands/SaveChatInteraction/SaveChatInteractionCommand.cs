using System;
using ErrorOr;
using MediatR;

namespace MIC.Core.Application.Chat.Commands.SaveChatInteraction;

public sealed record SaveChatInteractionCommand(
    Guid UserId,
    string SessionId,
    string Query,
    string Response,
    DateTimeOffset? Timestamp = null,
    string? AiProvider = null,
    string? ModelUsed = null,
    int? TokenCount = null,
    bool IsSuccessful = true,
    string? ErrorMessage = null,
    string? Context = null,
    string? Metadata = null) : IRequest<ErrorOr<Guid>>;
