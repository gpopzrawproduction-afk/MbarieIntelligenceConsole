using System.Threading.Tasks;
using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Authentication;

/// <summary>
/// Defines operations for authenticating and managing the current user session.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string username, string password);

    Task<AuthenticationResult> RegisterAsync(string username, string email, string password, string displayName);

    Task LogoutAsync();

    Task<User?> GetCurrentUserAsync();
}