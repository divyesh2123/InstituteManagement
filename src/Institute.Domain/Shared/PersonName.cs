using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public sealed class PersonName : ValueObject
{
    public const int MaxLength = 100;

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>Required by EF Core.</summary>
    private PersonName()
    {
        FirstName = null!;
        LastName = null!;
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static Result<PersonName> Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return SharedErrors.Name.FirstNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return SharedErrors.Name.LastNameEmpty;
        }

        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (firstName.Length > MaxLength || lastName.Length > MaxLength)
        {
            return SharedErrors.Name.TooLong;
        }

        return new PersonName(firstName, lastName);
    }

    public override string ToString() => FullName;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return FirstName;
        yield return LastName;
    }
}
