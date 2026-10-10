using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public sealed class Address : ValueObject
{
    public const int StreetMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 100;
    public const int PostalCodeMaxLength = 20;
    public const int CountryMaxLength = 100;

    private Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>Required by EF Core.</summary>
    private Address()
    {
        Street = City = State = PostalCode = Country = null!;
    }

    public string Street { get; private set; }

    public string City { get; private set; }

    public string State { get; private set; }

    public string PostalCode { get; private set; }

    public string Country { get; private set; }

    public static Result<Address> Create(string? street, string? city, string? state, string? postalCode, string? country)
    {
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(postalCode) || string.IsNullOrWhiteSpace(country))
        {
            return SharedErrors.Address.Incomplete;
        }

        var address = new Address(street.Trim(), city.Trim(), state.Trim(), postalCode.Trim(), country.Trim());

        if (address.Street.Length > StreetMaxLength || address.City.Length > CityMaxLength
            || address.State.Length > StateMaxLength || address.PostalCode.Length > PostalCodeMaxLength
            || address.Country.Length > CountryMaxLength)
        {
            return SharedErrors.Address.TooLong;
        }

        return address;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
}
