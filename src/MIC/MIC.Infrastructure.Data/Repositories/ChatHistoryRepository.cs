using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

internal sealed class ChatHistoryRepository : IChatHistoryRepository
{
    private readonly MicDbContext _db;

    public ChatHistoryRepository(MicDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(ChatHistory entry, CancellationToken cancellationToken = default)
        => _db.ChatHistories.AddAsync(entry, cancellationToken).AsTask();

    public Task<List<ChatHistory>> GetBySessionAsync(Guid userId, string sessionId, int limit, CancellationToken cancellationToken = default)
        => _db.ChatHistories
            .Where(x => x.UserId == userId && x.SessionId == sessionId)
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);

    public async Task DeleteBySessionAsync(Guid userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var entries = await _db.ChatHistories
            .Where(x => x.UserId == userId && x.SessionId == sessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _db.ChatHistories.RemoveRange(entries);
    }
}
