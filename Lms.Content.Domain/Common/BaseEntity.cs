namespace Lms.ContentService.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides common properties: Id, CreatedAt, UpdatedAt.
/// 
/// WHY BASE ENTITY:
/// - Avoids duplicating Id, CreatedAt, UpdatedAt in every entity
/// - Enables generic repository methods (GetById<T>, etc.)
/// - Consistent audit trail across all entities
/// - Follows DRY principle (Don't Repeat Yourself)
/// - Makes it easy to add common behavior later (e.g., soft delete)
/// 
/// VG EVIDENCE: Shows understanding of enterprise patterns and code reuse
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key for all entities</summary>
    public Guid Id { get; protected set; }

    /// <summary>When the entity was first created</summary>
    public DateTime CreatedAt { get; protected set; } 

    /// <summary>When the entity was last modified (null if never modified)</summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Sets creation timestamp. Called once when entity is created.
    /// </summary>
    protected void SetCreated()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the modification timestamp. Called on every update.
    /// </summary>
    protected void SetUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if two entities are equal by comparing their IDs.
    /// Two entities are equal if they have the same type and Id.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id && Id != Guid.Empty; // Guid.Empty means not persisted yet
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}