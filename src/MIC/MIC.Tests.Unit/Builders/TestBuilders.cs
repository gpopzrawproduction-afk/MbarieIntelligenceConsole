using System;
using MIC.Core.Domain.Entities;

namespace MIC.Tests.Unit.Builders;

/// <summary>
/// Builder pattern for creating test IntelligenceAlert instances.
/// </summary>
public class AlertBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alertName = "Test Alert";
    private string _description = "Test Description";
    private AlertSeverity _severity = AlertSeverity.Info;
    private AlertStatus _status = AlertStatus.Active;
    private string _source = "Test Source";
    private DateTime _triggeredAt = DateTime.UtcNow;
    private bool _isDeleted;
    private string? _deletedBy;
    private DateTimeOffset? _deletedAt;
    private string? _acknowledgedBy;
    private DateTime? _acknowledgedAt;
    private string? _resolvedBy;
    private DateTime? _resolvedAt;
    private string? _resolutionNotes;

    public AlertBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AlertBuilder WithName(string name)
    {
        _alertName = name;
        return this;
    }

    public AlertBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AlertBuilder WithSeverity(AlertSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public AlertBuilder WithStatus(AlertStatus status)
    {
        _status = status;
        return this;
    }

    public AlertBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    public AlertBuilder AsDeleted(string deletedBy = "admin")
    {
        _isDeleted = true;
        _deletedBy = deletedBy;
        _deletedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public AlertBuilder AsAcknowledged(string acknowledgedBy = "admin")
    {
        _status = AlertStatus.Acknowledged;
        _acknowledgedBy = acknowledgedBy;
        _acknowledgedAt = DateTime.UtcNow;
        return this;
    }

    public AlertBuilder AsResolved(string resolvedBy = "admin", string notes = "Resolved")
    {
        _status = AlertStatus.Resolved;
        _resolvedBy = resolvedBy;
        _resolvedAt = DateTime.UtcNow;
        _resolutionNotes = notes;
        return this;
    }

    public IntelligenceAlert Build()
    {
        var alert = new IntelligenceAlert(_alertName, _description, _severity, _source);
        
        // Set Id via reflection
        SetProperty(alert, "Id", _id);
        SetProperty(alert, "TriggeredAt", _triggeredAt);

        if (_isDeleted)
        {
            alert.MarkAsDeleted(_deletedBy ?? "admin");
        }

        if (_status == AlertStatus.Acknowledged && _acknowledgedBy != null)
        {
            alert.Acknowledge(_acknowledgedBy);
        }

        if (_status == AlertStatus.Resolved && _resolvedBy != null)
        {
            alert.Resolve(_resolvedBy, _resolutionNotes ?? "Resolved");
        }

        return alert;
    }

    private static void SetProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property?.SetValue(obj, value);
    }
}

/// <summary>
/// Builder pattern for creating test User instances.
/// </summary>
public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = "testuser";
    private string _email = "test@example.com";
    private string _fullName = "Test User";
    private string _passwordHash = "hashedpassword";
    private string _salt = "salt";
    private bool _isActive = true;
    private UserRole _role = UserRole.User;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow.AddDays(-30);
    private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _lastLoginAt;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        _email = $"{username}@example.com";
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public UserBuilder WithPasswordHash(string hash, string salt)
    {
        _passwordHash = hash;
        _salt = salt;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public UserBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _role = UserRole.Admin;
        return this;
    }

    public UserBuilder WithLastLogin(DateTimeOffset lastLogin)
    {
        _lastLoginAt = lastLogin;
        return this;
    }

    public User Build()
    {
        var user = new User
        {
            Username = _username,
            Email = _email,
            FullName = _fullName,
            PasswordHash = _passwordHash,
            Salt = _salt,
            IsActive = _isActive,
            Role = _role,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            LastLoginAt = _lastLoginAt
        };
        return user;
    }
}

/// <summary>
/// Builder pattern for creating test OperationalMetric instances.
/// </summary>
public class MetricBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _metricName = "Test Metric";
    private string _category = "Test Category";
    private double _value = 50.0;
    private string _unit = "%";
    private string _source = "Test Source";
    private DateTime _recordedAt = DateTime.UtcNow;
    private MetricSeverity _severity = MetricSeverity.Normal;
    private double? _targetValue;
    private double? _previousValue;
    private double _changePercent;

    public MetricBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public MetricBuilder WithName(string name)
    {
        _metricName = name;
        return this;
    }

    public MetricBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public MetricBuilder WithValue(double value)
    {
        _value = value;
        return this;
    }

    public MetricBuilder WithUnit(string unit)
    {
        _unit = unit;
        return this;
    }

    public MetricBuilder WithSource(string source)
    {
        _source = source;
        return this;
    }

    public MetricBuilder WithSeverity(MetricSeverity severity)
    {
        _severity = severity;
        return this;
    }

    public MetricBuilder WithTarget(double target)
    {
        _targetValue = target;
        return this;
    }

    public MetricBuilder WithPreviousValue(double previous)
    {
        _previousValue = previous;
        return this;
    }

    public MetricBuilder WithChangePercent(double change)
    {
        _changePercent = change;
        return this;
    }

    public MetricBuilder AtTime(DateTime recordedAt)
    {
        _recordedAt = recordedAt;
        return this;
    }

    public OperationalMetric Build()
    {
        return new OperationalMetric(
            metricName: _metricName,
            category: _category,
            source: _source,
            value: _value,
            unit: _unit,
            severity: _severity);
    }
}
