using Institute.Application.Abstractions;
using Institute.Application.Common;
using Institute.Domain.Abstractions;
using Institute.Domain.Shared;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Students;

public sealed record AddressResponse(string Street, string City, string State, string PostalCode, string Country);

public sealed record StudentSummaryResponse(
    Guid Id, string RegistrationNumber, string FirstName, string LastName, string Email, string Phone, string Status);

public sealed record StudentEnrollmentResponse(
    Guid EnrollmentId, Guid BatchId, string BatchName, string CourseCode, string CourseTitle, DateTime EnrolledOnUtc, string Status);

public sealed record StudentResponse(
    Guid Id,
    string RegistrationNumber,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    AddressResponse Address,
    string Status,
    DateTime CreatedOnUtc,
    IReadOnlyList<StudentEnrollmentResponse> Enrollments);

public sealed record GetStudentsQuery(string? Search, StudentStatus? Status, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<StudentSummaryResponse>>;

public sealed record GetStudentByIdQuery(Guid StudentId) : IQuery<StudentResponse>;

internal sealed class StudentQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetStudentsQuery, PagedList<StudentSummaryResponse>>,
      IQueryHandler<GetStudentByIdQuery, StudentResponse>
{
    public async Task<Result<PagedList<StudentSummaryResponse>>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Students;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var email = Email.Create(term);
            var registrationNumber = RegistrationNumber.Create(term);

            if (email.IsSuccess)
            {
                query = query.Where(s => s.Email == email.Value);
            }
            else if (term.StartsWith("STU-", StringComparison.OrdinalIgnoreCase) && registrationNumber.IsSuccess)
            {
                query = query.Where(s => s.RegistrationNumber == registrationNumber.Value);
            }
            else
            {
                query = query.Where(s => s.Name.FirstName.Contains(term) || s.Name.LastName.Contains(term));
            }
        }

        if (request.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        var projected = query
            .OrderBy(s => s.Name.LastName)
            .ThenBy(s => s.Name.FirstName)
            .Select(s => new StudentSummaryResponse(
                s.Id.Value,
                s.RegistrationNumber.Value,
                s.Name.FirstName,
                s.Name.LastName,
                s.Email.Value,
                s.Phone.Value,
                s.Status.ToString()));

        return await PagedList<StudentSummaryResponse>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }

    public async Task<Result<StudentResponse>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new StudentId(request.StudentId);

        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return StudentErrors.NotFound(id);
        }

        var enrollments = await (
            from e in db.Enrollments
            join b in db.Batches on e.BatchId equals b.Id
            join c in db.Courses on b.CourseId equals c.Id
            where e.StudentId == id
            orderby e.EnrolledOnUtc descending
            select new StudentEnrollmentResponse(
                e.Id.Value, b.Id.Value, b.Name, c.Code.Value, c.Title, e.EnrolledOnUtc, e.Status.ToString()))
            .ToListAsync(cancellationToken);

        var a = student.Address;

        return new StudentResponse(
            student.Id.Value,
            student.RegistrationNumber.Value,
            student.Name.FirstName,
            student.Name.LastName,
            student.Email.Value,
            student.Phone.Value,
            student.DateOfBirth,
            new AddressResponse(a.Street, a.City, a.State, a.PostalCode, a.Country),
            student.Status.ToString(),
            student.CreatedOnUtc,
            enrollments);
    }
}
