using MIC.Core.Application.Authentication.Common;

namespace MIC.Core.Application.Common.Interfaces;

/// <summary>
/// Service for managing user session state.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Sets the authentication token for the current session.
    /// </summary>
    void SetToken(string token);
    
    /// <summary>
    /// Sets the current user information.
    /// </summary>
    void SetUser(UserDto user);
    
    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    string GetToken();
    
    /// <summary>
    /// Gets the current user information.
    /// </summary>
    UserDto GetUser();
    
    /// <summary>
    /// Clears the current session (logout).
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}