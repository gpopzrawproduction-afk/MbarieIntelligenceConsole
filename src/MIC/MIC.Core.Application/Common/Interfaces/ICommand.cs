using MediatR;
using ErrorOr;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that modify system state
/// </summary>
public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>> { }

/// <summary>
/// Handler interface for command processing
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, ErrorOr<TResponse>>
    where TCommand : ICommand<TResponse> { }
