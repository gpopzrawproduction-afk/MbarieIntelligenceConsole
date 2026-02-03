using System;
using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for accessing and managing user accounts.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by ID with EF Core tracking enabled (for updates).
    /// </summary>
    Task<User?> GetTrackedByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(Guid id);

    Task<User> CreateAsync(User user);

    Task UpdateAsync(User user);

    Task<bool> UsernameExistsAsync(string username);
    
    Task<bool> EmailExistsAsync(string email);
}
