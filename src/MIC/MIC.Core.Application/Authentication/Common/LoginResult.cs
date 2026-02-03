namespace MIC.Core.Application.Authentication.Common;

/// <summary>
/// Result of a login operation.
/// </summary>
public sealed class LoginResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}