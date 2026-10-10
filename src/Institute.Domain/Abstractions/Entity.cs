namespace Institute.Domain.Abstractions;

public interface IAuditableEntity
{
    DateTime CreatedOnUtc { get; }

    DateTime? ModifiedOnUtc { get; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTime? DeletedOnUtc { get; }
}

public abstract class Entity<TId> : IEquatable<Entity<TId>>, IAuditableEntity
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>Required by EF Core for materialization.</summary>
    protected Entity() => Id = default!;

    public TId Id { get; private init; }

    /// <summary>Set by the persistence layer.</summary>
    public DateTime CreatedOnUtc { get; private set; }

    /// <summary>Set by the persistence layer.</summary>
    public DateTime? ModifiedOnUtc { get; private set; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
