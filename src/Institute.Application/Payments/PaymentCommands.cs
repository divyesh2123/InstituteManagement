using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Payments;
using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Application.Payments;

public sealed record RecordPaymentCommand(
    Guid StudentId,
    Guid BatchId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string? TransactionReference) : ICommand<Guid>;

public sealed record RefundPaymentCommand(Guid PaymentId, string Reason) : ICommand;

internal sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.BatchId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.Method).IsInEnum();
        RuleFor(c => c.TransactionReference).MaximumLength(FeePayment.ReferenceMaxLength);
    }
}

internal sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(c => c.PaymentId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(FeePayment.RefundReasonMaxLength);
    }
}

internal sealed class PaymentCommandHandlers(
    IFeePaymentRepository payments,
    IBatchRepository batches,
    IStudentRepository students,
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : ICommandHandler<RecordPaymentCommand, Guid>,
      ICommandHandler<RefundPaymentCommand>
{
    public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        if (await students.GetByIdAsync(studentId, cancellationToken) is null)
        {
            return StudentErrors.NotFound(studentId);
        }

        var batchId = new BatchId(request.BatchId);
        var batch = await batches.GetByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return BatchErrors.NotFound(batchId);
        }

        var amount = Money.Create(request.Amount, request.Currency);
        if (amount.IsFailure)
        {
            return amount.Error;
        }

        // Payments must be in the course currency so dues can be computed reliably.
        var course = await courses.GetByIdAsync(batch.CourseId, cancellationToken);
        if (course is not null && course.Fee.Currency != amount.Value.Currency)
        {
            return SharedErrors.Money.CurrencyMismatch;
        }

        var payment = FeePayment.Record(batch, studentId, amount.Value, request.Method, request.TransactionReference, clock.UtcNow);
        if (payment.IsFailure)
        {
            return payment.Error;
        }

        payments.Add(payment.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.Value.Id.Value;
    }

    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var id = new FeePaymentId(request.PaymentId);
        var payment = await payments.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return PaymentErrors.NotFound(id);
        }

        var result = payment.Refund(request.Reason, clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
