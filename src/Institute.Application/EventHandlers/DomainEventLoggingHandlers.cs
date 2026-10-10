using Institute.Application.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Payments;
using Institute.Domain.Students;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Institute.Application.EventHandlers;

/// <summary>
/// Reacts to domain events after they are committed. These are the extension points for side effects such as
/// welcome emails, SMS receipts or integration events (see backlog ticket INST-020 for a transactional outbox).
/// </summary>
internal sealed partial class DomainEventLoggingHandlers(ILogger<DomainEventLoggingHandlers> logger)
    : INotificationHandler<DomainEventNotification<StudentRegisteredDomainEvent>>,
      INotificationHandler<DomainEventNotification<StudentEnrolledDomainEvent>>,
      INotificationHandler<DomainEventNotification<BatchCancelledDomainEvent>>,
      INotificationHandler<DomainEventNotification<FeePaymentRecordedDomainEvent>>
{
    public Task Handle(DomainEventNotification<StudentRegisteredDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        LogStudentRegistered(logger, e.StudentId.Value, e.RegistrationNumber);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<StudentEnrolledDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        LogStudentEnrolled(logger, e.StudentId.Value, e.BatchId.Value);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<BatchCancelledDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        LogBatchCancelled(logger, e.BatchId.Value, e.AffectedStudents.Count);
        return Task.CompletedTask;
    }

    public Task Handle(DomainEventNotification<FeePaymentRecordedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        LogPaymentRecorded(logger, e.PaymentId.Value, e.Amount, e.Currency, e.StudentId.Value);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Student {StudentId} registered with number {RegistrationNumber}")]
    private static partial void LogStudentRegistered(ILogger logger, Guid studentId, string registrationNumber);

    [LoggerMessage(Level = LogLevel.Information, Message = "Student {StudentId} enrolled in batch {BatchId}")]
    private static partial void LogStudentEnrolled(ILogger logger, Guid studentId, Guid batchId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch {BatchId} cancelled, {AffectedCount} enrollments cancelled")]
    private static partial void LogBatchCancelled(ILogger logger, Guid batchId, int affectedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Payment {PaymentId} of {Amount} {Currency} recorded for student {StudentId}")]
    private static partial void LogPaymentRecorded(ILogger logger, Guid paymentId, decimal amount, string currency, Guid studentId);
}
