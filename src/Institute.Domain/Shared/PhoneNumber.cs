using System.Text.RegularExpressions;
using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public sealed partial class PhoneNumber : ValueObject
{
    public const int MaxLength = 16;

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return SharedErrors.Phone.Empty;
        }

        // Normalize: drop spaces, dashes, dots and parentheses.
        var normalized = SeparatorRegex().Replace(phone.Trim(), string.Empty);

        if (!PhoneRegex().IsMatch(normalized))
        {
            return SharedErrors.Phone.Invalid;
        }

        return new PhoneNumber(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(@"[\s\-\.\(\)]", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"^\+?[0-9]{7,15}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex PhoneRegex();
}
