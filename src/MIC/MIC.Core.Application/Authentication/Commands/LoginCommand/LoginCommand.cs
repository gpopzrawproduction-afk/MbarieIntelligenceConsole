using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Authentication.Commands.LoginCommand;

/// <summary>
/// Command to authenticate a user with username and password.
/// </summary>
public record LoginCommand(
    string Username,
    string Password
) : ICommand<LoginResult>;