namespace Institute.Domain.Abstractions;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);

    public bool Equals(ValueObject? other) =>
        other is not null && other.GetType() == GetType() && GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetAtomicValues().Aggregate(GetType().GetHashCode(), HashCode.Combine);

    protected abstract IEnumerable<object?> GetAtomicValues();
}
