using MediatR;
using ErrorOr;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that read system state
/// </summary>
public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>> { }

/// <summary>
/// Handler interface for query processing
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, ErrorOr<TResponse>>
    where TQuery : IQuery<TResponse> { }
