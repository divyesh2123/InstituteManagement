using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public static class SharedErrors
{
    public static class Email
    {
        public static readonly Error Empty = Error.Validation("Email.Empty", "Email is required.");
        public static readonly Error TooLong = Error.Validation("Email.TooLong", $"Email must not exceed {Shared.Email.MaxLength} characters.");
        public static readonly Error Invalid = Error.Validation("Email.Invalid", "Email format is invalid.");
    }

    public static class Phone
    {
        public static readonly Error Empty = Error.Validation("Phone.Empty", "Phone number is required.");
        public static readonly Error Invalid = Error.Validation("Phone.Invalid", "Phone number must contain 7 to 15 digits and may start with '+'.");
    }

    public static class Name
    {
        public static readonly Error FirstNameEmpty = Error.Validation("Name.FirstNameEmpty", "First name is required.");
        public static readonly Error LastNameEmpty = Error.Validation("Name.LastNameEmpty", "Last name is required.");
        public static readonly Error TooLong = Error.Validation("Name.TooLong", $"Names must not exceed {PersonName.MaxLength} characters.");
    }

    public static class Address
    {
        public static readonly Error Incomplete = Error.Validation("Address.Incomplete", "Street, city, state, postal code and country are required.");
        public static readonly Error TooLong = Error.Validation("Address.TooLong", "One or more address parts exceed the maximum length.");
    }

    public static class Money
    {
        public static readonly Error Negative = Error.Validation("Money.Negative", "Amount can not be negative.");
        public static readonly Error InvalidCurrency = Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO 4217 code.");
        public static readonly Error CurrencyMismatch = Error.Validation("Money.CurrencyMismatch", "Can not combine amounts with different currencies.");
    }
}
