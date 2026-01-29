using System;
using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for accessing and managing user accounts.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(Guid id);

    Task<User> CreateAsync(User user);

    Task UpdateAsync(User user);

    Task<bool> UsernameExistsAsync(string username);
}