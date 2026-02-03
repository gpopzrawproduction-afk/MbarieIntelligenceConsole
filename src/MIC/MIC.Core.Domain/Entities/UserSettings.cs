using System;
using MIC.Core.Domain.Abstractions;

namespace MIC.Core.Domain.Entities;

/// <summary>
/// User-specific settings stored in the database.
/// </summary>
public class UserSettings : BaseEntity
{
    /// <summary>
    /// Foreign key to the User entity.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property to the User entity.
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// JSON-serialized settings for this user.
    /// </summary>
    public string SettingsJson { get; set; } = "{}";
    
    /// <summary>
    /// When these settings were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Version of the settings schema (for migration purposes).
    /// </summary>
    public int SettingsVersion { get; set; } = 1;
    
    /// <summary>
    /// Creates a new UserSettings entity.
    /// </summary>
    public UserSettings() { }
    
    /// <summary>
    /// Creates a new UserSettings entity for a specific user.
    /// </summary>
    public UserSettings(Guid userId, string settingsJson)
    {
        UserId = userId;
        SettingsJson = settingsJson ?? "{}";
        LastUpdated = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Updates the settings JSON and timestamp.
    /// </summary>
    public void UpdateSettings(string settingsJson)
    {
        SettingsJson = settingsJson ?? "{}";
        LastUpdated = DateTimeOffset.UtcNow;
        MarkAsModified("system");
    }
}