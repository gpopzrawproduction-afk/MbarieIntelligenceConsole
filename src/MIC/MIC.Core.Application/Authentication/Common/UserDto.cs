using MIC.Core.Domain.Entities;

namespace MIC.Core.Application.Authentication.Common;

/// <summary>
/// Data transfer object for user information.
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? JobPosition { get; init; }
    public string? Department { get; init; }
    public string? SeniorityLevel { get; init; }

    public static UserDto FromUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            JobPosition = user.JobPosition,
            Department = user.Department,
            SeniorityLevel = user.SeniorityLevel
        };
    }
}