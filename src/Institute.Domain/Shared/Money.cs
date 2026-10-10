using Institute.Domain.Abstractions;

namespace Institute.Domain.Shared;

public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "INR";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Required by EF Core.</summary>
    private Money() => Currency = DefaultCurrency;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public bool IsZero => Amount == 0;

    public static Money Zero(string currency = DefaultCurrency) => new(0, currency);

    public static Result<Money> Create(decimal amount, string? currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            return SharedErrors.Money.Negative;
        }

        var code = currency?.Trim();

        if (code is null || code.Length != 3 || !code.All(char.IsAsciiLetter))
        {
            return SharedErrors.Money.InvalidCurrency;
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), code.ToUpperInvariant());
    }

    public Result<Money> Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return other.Currency != Currency
            ? SharedErrors.Money.CurrencyMismatch
            : new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
}
