using System.Diagnostics.CodeAnalysis;

namespace MIC.Core.Domain.Abstractions;

/// <summary>
/// Base entity with identity and domain event support.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Entity identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? ModifiedAt { get; protected internal set; }

    /// <summary>
    /// Last modification actor.
    /// </summary>
    public string? LastModifiedBy { get; private set; }

    /// <summary>
    /// Adds a domain event to the entity.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears domain events after dispatch.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Marks the entity as modified by the specified user.
    /// </summary>
    public void MarkAsModified(string? modifiedBy)
    {
        ModifiedAt = DateTime.UtcNow;
        LastModifiedBy = string.IsNullOrWhiteSpace(modifiedBy) ? null : modifiedBy;
    }

    /// <summary>
    /// Marks the entity as soft deleted.
    /// </summary>
    public void MarkAsDeleted(string? deletedBy = null)
    {
        IsDeleted = true;
        MarkAsModified(deletedBy);
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public void Restore(string? restoredBy = null)
    {
        IsDeleted = false;
        MarkAsModified(restoredBy);
    }

    /// <summary>
    /// Sets the ModifiedAt timestamp to now. Intended for infrastructure use.
    /// </summary>
    public void SetModifiedNow()
    {
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Required by EF Core.
    /// </summary>
    protected BaseEntity() { }
}
