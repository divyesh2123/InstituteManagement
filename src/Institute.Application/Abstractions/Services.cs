using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Payments;
using Institute.Domain.Students;

namespace Institute.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}

/// <summary>
/// Read-only, no-tracking access to the data model for query handlers (the "Q" in CQRS).
/// Write operations always go through the aggregate repositories.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Department> Departments { get; }

    IQueryable<Instructor> Instructors { get; }

    IQueryable<Course> Courses { get; }

    IQueryable<Student> Students { get; }

    IQueryable<Batch> Batches { get; }

    IQueryable<Enrollment> Enrollments { get; }

    IQueryable<FeePayment> FeePayments { get; }
}
