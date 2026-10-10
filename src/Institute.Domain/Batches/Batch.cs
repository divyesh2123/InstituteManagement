using Institute.Domain.Abstractions;
using Institute.Domain.Courses;
using Institute.Domain.Instructors;
using Institute.Domain.Students;

namespace Institute.Domain.Batches;

public readonly record struct BatchId(Guid Value)
{
    public static BatchId New() => new(Guid.NewGuid());
}

public enum BatchStatus
{
    Planned = 1,
    Ongoing = 2,
    Completed = 3,
    Cancelled = 4,
}

public sealed record StudentEnrolledDomainEvent(BatchId BatchId, StudentId StudentId, EnrollmentId EnrollmentId) : DomainEvent;

public sealed record BatchCancelledDomainEvent(BatchId BatchId, IReadOnlyList<StudentId> AffectedStudents) : DomainEvent;

/// <summary>
/// A scheduled offering of a <see cref="Course"/> taught by an <see cref="Instructor"/>.
/// The batch is the consistency boundary for enrollments (capacity, duplicates, lifecycle).
/// </summary>
public sealed class Batch : AggregateRoot<BatchId>
{
    public const int NameMaxLength = 100;
    public const int MinCapacity = 1;
    public const int MaxCapacity = 500;

    private readonly List<Enrollment> _enrollments = [];

    private Batch(
        BatchId id, CourseId courseId, InstructorId instructorId, string name, DateOnly startDate, DateOnly endDate, int capacity)
        : base(id)
    {
        CourseId = courseId;
        InstructorId = instructorId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        Status = BatchStatus.Planned;
    }

    private Batch() => Name = null!;

    public CourseId CourseId { get; private set; }

    public InstructorId InstructorId { get; private set; }

    public string Name { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int Capacity { get; private set; }

    public BatchStatus Status { get; private set; }

    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.AsReadOnly();

    public int ActiveEnrollmentCount => _enrollments.Count(e => e.Status == EnrollmentStatus.Active);

    public int AvailableSeats => Capacity - ActiveEnrollmentCount;

    public static Result<Batch> Schedule(
        Course course, Instructor instructor, string? name, DateOnly startDate, DateOnly endDate, int capacity, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(course);
        ArgumentNullException.ThrowIfNull(instructor);

        if (!course.IsActive)
        {
            return CourseErrors.Inactive;
        }

        if (!instructor.IsActive)
        {
            return InstructorErrors.Inactive;
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            return BatchErrors.InvalidName;
        }

        if (startDate < today)
        {
            return BatchErrors.StartDateInPast;
        }

        if (endDate <= startDate)
        {
            return BatchErrors.InvalidDateRange;
        }

        if (capacity is < MinCapacity or > MaxCapacity)
        {
            return BatchErrors.InvalidCapacity;
        }

        return new Batch(BatchId.New(), course.Id, instructor.Id, name.Trim(), startDate, endDate, capacity);
    }

    public Result AssignInstructor(Instructor instructor)
    {
        ArgumentNullException.ThrowIfNull(instructor);

        if (IsClosed)
        {
            return BatchErrors.Closed(Status);
        }

        if (!instructor.IsActive)
        {
            return InstructorErrors.Inactive;
        }

        InstructorId = instructor.Id;
        return Result.Success();
    }

    public Result<Enrollment> Enroll(Student student, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (IsClosed)
        {
            return BatchErrors.Closed(Status);
        }

        if (student.Status != StudentStatus.Active)
        {
            return StudentErrors.NotActive;
        }

        if (IsEnrolled(student.Id))
        {
            return BatchErrors.AlreadyEnrolled;
        }

        if (AvailableSeats <= 0)
        {
            return BatchErrors.Full;
        }

        var enrollment = new Enrollment(EnrollmentId.New(), Id, student.Id, utcNow);
        _enrollments.Add(enrollment);

        RaiseDomainEvent(new StudentEnrolledDomainEvent(Id, student.Id, enrollment.Id));

        return enrollment;
    }

    public Result CancelEnrollment(StudentId studentId)
    {
        if (IsClosed)
        {
            return BatchErrors.Closed(Status);
        }

        var enrollment = _enrollments.SingleOrDefault(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active);

        if (enrollment is null)
        {
            return BatchErrors.EnrollmentNotFound;
        }

        enrollment.Cancel();
        return Result.Success();
    }

    public bool IsEnrolled(StudentId studentId) =>
        _enrollments.Any(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Cancelled);

    public Result Start()
    {
        if (Status != BatchStatus.Planned)
        {
            return BatchErrors.InvalidTransition(Status, BatchStatus.Ongoing);
        }

        Status = BatchStatus.Ongoing;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != BatchStatus.Ongoing)
        {
            return BatchErrors.InvalidTransition(Status, BatchStatus.Completed);
        }

        foreach (var enrollment in _enrollments.Where(e => e.Status == EnrollmentStatus.Active))
        {
            enrollment.Complete();
        }

        Status = BatchStatus.Completed;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (IsClosed)
        {
            return BatchErrors.InvalidTransition(Status, BatchStatus.Cancelled);
        }

        var affected = _enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
        foreach (var enrollment in affected)
        {
            enrollment.Cancel();
        }

        Status = BatchStatus.Cancelled;
        RaiseDomainEvent(new BatchCancelledDomainEvent(Id, affected.Select(e => e.StudentId).ToList()));
        return Result.Success();
    }

    private bool IsClosed => Status is BatchStatus.Completed or BatchStatus.Cancelled;
}

public static class BatchErrors
{
    public static readonly Error InvalidName = Error.Validation(
        "Batch.InvalidName", $"Batch name is required and must not exceed {Batch.NameMaxLength} characters.");

    public static readonly Error StartDateInPast = Error.Validation("Batch.StartDateInPast", "Start date can not be in the past.");

    public static readonly Error InvalidDateRange = Error.Validation("Batch.InvalidDateRange", "End date must be after the start date.");

    public static readonly Error InvalidCapacity = Error.Validation(
        "Batch.InvalidCapacity", $"Capacity must be between {Batch.MinCapacity} and {Batch.MaxCapacity}.");

    public static readonly Error Full = Error.Conflict("Batch.Full", "The batch has no available seats.");

    public static readonly Error AlreadyEnrolled = Error.Conflict("Batch.AlreadyEnrolled", "The student is already enrolled in this batch.");

    public static readonly Error EnrollmentNotFound = Error.NotFound("Batch.EnrollmentNotFound", "The student has no active enrollment in this batch.");

    public static Error Closed(BatchStatus status) => Error.Conflict("Batch.Closed", $"The batch is {status} and can not be changed.");

    public static Error InvalidTransition(BatchStatus from, BatchStatus to) =>
        Error.Conflict("Batch.InvalidTransition", $"Can not change batch status from {from} to {to}.");

    public static Error NotFound(BatchId id) => Error.NotFound("Batch.NotFound", $"Batch '{id.Value}' was not found.");
}

public interface IBatchRepository
{
    /// <summary>Loads the batch including its enrollments.</summary>
    Task<Batch?> GetByIdAsync(BatchId id, CancellationToken cancellationToken = default);

    void Add(Batch batch);
}
