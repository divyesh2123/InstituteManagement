using Institute.Domain.Abstractions;
using Institute.Domain.Students;

namespace Institute.Domain.Batches;

public readonly record struct EnrollmentId(Guid Value)
{
    public static EnrollmentId New() => new(Guid.NewGuid());
}

public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
}

/// <summary>Child entity of the <see cref="Batch"/> aggregate. Only modified through the batch.</summary>
public sealed class Enrollment : Entity<EnrollmentId>
{
    internal Enrollment(EnrollmentId id, BatchId batchId, StudentId studentId, DateTime enrolledOnUtc)
        : base(id)
    {
        BatchId = batchId;
        StudentId = studentId;
        EnrolledOnUtc = enrolledOnUtc;
        Status = EnrollmentStatus.Active;
    }

    private Enrollment()
    {
    }

    public BatchId BatchId { get; private set; }

    public StudentId StudentId { get; private set; }

    public DateTime EnrolledOnUtc { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    internal void Cancel() => Status = EnrollmentStatus.Cancelled;

    internal void Complete() => Status = EnrollmentStatus.Completed;
}
