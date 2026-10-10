namespace Institute.Domain.Abstractions;

/// <summary>
/// Thrown when an invariant is violated in a way that indicates a programming error.
/// Expected business rule violations are returned as <see cref="Result"/> failures instead.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException()
    {
    }
}
