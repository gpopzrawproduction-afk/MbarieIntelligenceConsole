using System;
using ErrorOr;
using MediatR;

namespace MIC.Core.Application.Chat.Commands.ClearChatSession;

public sealed record ClearChatSessionCommand(
    Guid UserId,
    string SessionId) : IRequest<ErrorOr<bool>>;
