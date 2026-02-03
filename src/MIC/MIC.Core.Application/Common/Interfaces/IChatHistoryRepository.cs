using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

public interface IChatHistoryRepository
{
    Task AddAsync(ChatHistory entry, CancellationToken cancellationToken = default);

    Task<List<ChatHistory>> GetBySessionAsync(
        Guid userId,
        string sessionId,
        int limit,
        CancellationToken cancellationToken = default);

    Task DeleteBySessionAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken = default);
}
