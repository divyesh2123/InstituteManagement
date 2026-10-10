using System.Security.Cryptography;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Domain.Payments;

public readonly record struct FeePaymentId(Guid Value)
{
    public static FeePaymentId New() => new(Guid.NewGuid());
}

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Upi = 4,
}

public enum PaymentStatus
{
    Completed = 1,
    Refunded = 2,
}

public sealed record FeePaymentRecordedDomainEvent(FeePaymentId PaymentId, StudentId StudentId, decimal Amount, string Currency) : DomainEvent;

public sealed record FeePaymentRefundedDomainEvent(FeePaymentId PaymentId, StudentId StudentId, string Reason) : DomainEvent;

public sealed class FeePayment : AggregateRoot<FeePaymentId>
{
    public const int ReceiptNumberMaxLength = 30;
    public const int ReferenceMaxLength = 100;
    public const int RefundReasonMaxLength = 500;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private FeePayment(
        FeePaymentId id, StudentId studentId, BatchId batchId, Money amount, PaymentMethod method, string receiptNumber, string? reference, DateTime paidOnUtc)
        : base(id)
    {
        StudentId = studentId;
        BatchId = batchId;
        Amount = amount;
        Method = method;
        ReceiptNumber = receiptNumber;
        TransactionReference = reference;
        PaidOnUtc = paidOnUtc;
        Status = PaymentStatus.Completed;
    }

    private FeePayment()
    {
        Amount = null!;
        ReceiptNumber = null!;
    }

    public StudentId StudentId { get; private set; }

    public BatchId BatchId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public string ReceiptNumber { get; private set; }

    public string? TransactionReference { get; private set; }

    public DateTime PaidOnUtc { get; private set; }

    public PaymentStatus Status { get; private set; }

    public DateTime? RefundedOnUtc { get; private set; }

    public string? RefundReason { get; private set; }

    public static Result<FeePayment> Record(
        Batch batch, StudentId studentId, Money amount, PaymentMethod method, string? transactionReference, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.IsZero)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        if (!Enum.IsDefined(method))
        {
            return PaymentErrors.InvalidMethod;
        }

        if (!batch.IsEnrolled(studentId))
        {
            return PaymentErrors.StudentNotEnrolled;
        }

        if (transactionReference?.Trim().Length > ReferenceMaxLength)
        {
            return PaymentErrors.ReferenceTooLong;
        }

        var receipt = $"RCPT-{utcNow:yyyyMMdd}-{RandomNumberGenerator.GetString(Alphabet, 8)}";
        var reference = string.IsNullOrWhiteSpace(transactionReference) ? null : transactionReference.Trim();

        var payment = new FeePayment(FeePaymentId.New(), studentId, batch.Id, amount, method, receipt, reference, utcNow);
        payment.RaiseDomainEvent(new FeePaymentRecordedDomainEvent(payment.Id, studentId, amount.Amount, amount.Currency));

        return payment;
    }

    public Result Refund(string? reason, DateTime utcNow)
    {
        if (Status == PaymentStatus.Refunded)
        {
            return PaymentErrors.AlreadyRefunded;
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > RefundReasonMaxLength)
        {
            return PaymentErrors.InvalidRefundReason;
        }

        Status = PaymentStatus.Refunded;
        RefundReason = reason.Trim();
        RefundedOnUtc = utcNow;

        RaiseDomainEvent(new FeePaymentRefundedDomainEvent(Id, StudentId, RefundReason));
        return Result.Success();
    }
}

public static class PaymentErrors
{
    public static readonly Error AmountMustBePositive = Error.Validation("Payment.AmountMustBePositive", "Payment amount must be greater than zero.");

    public static readonly Error InvalidMethod = Error.Validation("Payment.InvalidMethod", "Payment method is invalid.");

    public static readonly Error StudentNotEnrolled = Error.Conflict("Payment.StudentNotEnrolled", "The student is not enrolled in this batch.");

    public static readonly Error ReferenceTooLong = Error.Validation(
        "Payment.ReferenceTooLong", $"Transaction reference must not exceed {FeePayment.ReferenceMaxLength} characters.");

    public static readonly Error AlreadyRefunded = Error.Conflict("Payment.AlreadyRefunded", "The payment has already been refunded.");

    public static readonly Error InvalidRefundReason = Error.Validation(
        "Payment.InvalidRefundReason", $"Refund reason is required and must not exceed {FeePayment.RefundReasonMaxLength} characters.");

    public static Error NotFound(FeePaymentId id) => Error.NotFound("Payment.NotFound", $"Payment '{id.Value}' was not found.");
}

public interface IFeePaymentRepository
{
    Task<FeePayment?> GetByIdAsync(FeePaymentId id, CancellationToken cancellationToken = default);

    void Add(FeePayment payment);
}
