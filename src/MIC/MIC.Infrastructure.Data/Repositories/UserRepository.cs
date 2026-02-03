using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core repository implementation for user accounts.
/// </summary>
public sealed class UserRepository : Repository<User>, IUserRepository
{
    public async Task<User?> GetTrackedByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must be a non-empty GUID.", nameof(id));
        return await _dbSet.FirstOrDefaultAsync(u => u.Id == id).ConfigureAwait(false);
    }
    public UserRepository(MicDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or whitespace.", nameof(username));
        }

        // Use case-insensitive comparison; rely on EF translation to LOWER(column) = LOWER(value)
        var normalized = username.Trim().ToLowerInvariant();

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized)
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id must be a non-empty GUID.", nameof(id));
        }

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<User> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _dbSet.AddAsync(user).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Since GetByUsernameAsync uses AsNoTracking, we need to attach the entity
        var entry = _context.Entry(user);
        if (entry.State == EntityState.Detached)
        {
            _dbSet.Attach(user);
            entry.State = EntityState.Modified;
        }
        
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        var normalized = username.Trim().ToLowerInvariant();

        return await _dbSet
            .AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == normalized)
            .ConfigureAwait(false);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalized = email.Trim().ToLowerInvariant();

        return await _dbSet
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalized)
            .ConfigureAwait(false);
    }
}
