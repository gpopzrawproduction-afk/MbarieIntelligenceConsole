using System;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

// NEW: user roles for authorization and seeding
public enum UserRole
{
	Admin = 0,
	User = 1,
	Guest = 2
}

/// <summary>
/// Represents an application user account.
/// </summary>
public sealed class User : BaseEntity
{
	public string Username { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	// REQUIRED for Argon2 hashing
	public string Salt { get; set; } = string.Empty;

	// Full display name for UI
	public string? FullName { get; set; }

	// Role for basic authorization
	public UserRole Role { get; set; } = UserRole.User;

	public bool IsActive { get; set; } = true;

	public DateTimeOffset? LastLoginAt { get; set; }

	public new DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }
	
	// Job position for organizational intelligence
	public string? JobPosition { get; set; }
	
	// Department for organizational intelligence
	public string? Department { get; set; }
	
	// Seniority level for organizational intelligence
	public string? SeniorityLevel { get; set; }
}
