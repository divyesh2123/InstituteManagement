using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Payments;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Payments;

public sealed record PaymentResponse(
    Guid Id,
    string ReceiptNumber,
    Guid StudentId,
    Guid BatchId,
    string BatchName,
    decimal Amount,
    string Currency,
    string Method,
    string? TransactionReference,
    DateTime PaidOnUtc,
    string Status,
    DateTime? RefundedOnUtc,
    string? RefundReason);

public sealed record BatchFeeLineResponse(
    Guid BatchId, string BatchName, string CourseCode, decimal CourseFee, decimal Paid, decimal Due, string Currency);

public sealed record StudentFeeSummaryResponse(
    Guid StudentId, IReadOnlyList<BatchFeeLineResponse> Lines, decimal TotalFee, decimal TotalPaid, decimal TotalDue);

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentResponse>;

public sealed record GetStudentPaymentsQuery(Guid StudentId) : IQuery<IReadOnlyList<PaymentResponse>>;

public sealed record GetStudentFeeSummaryQuery(Guid StudentId) : IQuery<StudentFeeSummaryResponse>;

internal sealed class PaymentQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetPaymentByIdQuery, PaymentResponse>,
      IQueryHandler<GetStudentPaymentsQuery, IReadOnlyList<PaymentResponse>>,
      IQueryHandler<GetStudentFeeSummaryQuery, StudentFeeSummaryResponse>
{
    public async Task<Result<PaymentResponse>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new FeePaymentId(request.PaymentId);
        var payment = await Project(db.FeePayments.Where(p => p.Id == id)).FirstOrDefaultAsync(cancellationToken);

        return payment is null ? PaymentErrors.NotFound(id) : payment;
    }

    public async Task<Result<IReadOnlyList<PaymentResponse>>> Handle(GetStudentPaymentsQuery request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        if (!await db.Students.AnyAsync(s => s.Id == studentId, cancellationToken))
        {
            return StudentErrors.NotFound(studentId);
        }

        var items = await Project(db.FeePayments.Where(p => p.StudentId == studentId).OrderByDescending(p => p.PaidOnUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PaymentResponse>>(items);
    }

    public async Task<Result<StudentFeeSummaryResponse>> Handle(GetStudentFeeSummaryQuery request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        if (!await db.Students.AnyAsync(s => s.Id == studentId, cancellationToken))
        {
            return StudentErrors.NotFound(studentId);
        }

        var lines = await (
            from e in db.Enrollments
            join b in db.Batches on e.BatchId equals b.Id
            join c in db.Courses on b.CourseId equals c.Id
            where e.StudentId == studentId && e.Status != EnrollmentStatus.Cancelled
            let paid = db.FeePayments
                .Where(p => p.StudentId == studentId && p.BatchId == b.Id && p.Status == PaymentStatus.Completed)
                .Sum(p => (decimal?)p.Amount.Amount) ?? 0m
            orderby b.StartDate
            select new BatchFeeLineResponse(
                b.Id.Value,
                b.Name,
                c.Code.Value,
                c.Fee.Amount,
                paid,
                c.Fee.Amount - paid > 0 ? c.Fee.Amount - paid : 0m,
                c.Fee.Currency))
            .ToListAsync(cancellationToken);

        return new StudentFeeSummaryResponse(
            request.StudentId, lines, lines.Sum(l => l.CourseFee), lines.Sum(l => l.Paid), lines.Sum(l => l.Due));
    }

    private IQueryable<PaymentResponse> Project(IQueryable<FeePayment> query) =>
        from p in query
        join b in db.Batches on p.BatchId equals b.Id
        select new PaymentResponse(
            p.Id.Value,
            p.ReceiptNumber,
            p.StudentId.Value,
            b.Id.Value,
            b.Name,
            p.Amount.Amount,
            p.Amount.Currency,
            p.Method.ToString(),
            p.TransactionReference,
            p.PaidOnUtc,
            p.Status.ToString(),
            p.RefundedOnUtc,
            p.RefundReason);
}
