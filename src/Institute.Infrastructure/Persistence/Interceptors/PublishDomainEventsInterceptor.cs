using System.Collections.Concurrent;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Institute.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Publishes domain events raised by aggregates once the changes are committed successfully.
/// Handlers therefore never observe state that was rolled back.
/// </summary>
internal sealed class PublishDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, Type> NotificationTypes = new();

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await PublishAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = NotificationTypes.GetOrAdd(
                domainEvent.GetType(), t => typeof(DomainEventNotification<>).MakeGenericType(t));

            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
        }
    }
}
