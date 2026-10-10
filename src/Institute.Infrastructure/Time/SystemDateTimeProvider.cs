using Institute.Application.Abstractions;

namespace Institute.Infrastructure.Time;

internal sealed class SystemDateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
