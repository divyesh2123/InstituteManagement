using System.Text.RegularExpressions;
using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public sealed partial class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return SharedErrors.Email.Empty;
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return SharedErrors.Email.TooLong;
        }

        if (!EmailRegex().IsMatch(normalized))
        {
            return SharedErrors.Email.Invalid;
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailRegex();
}
