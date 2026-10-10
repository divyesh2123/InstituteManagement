using Institute.Application.Abstractions;
using Institute.Application.Common;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Batches;

public sealed record BatchSummaryResponse(
    Guid Id,
    string Name,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    Guid InstructorId,
    string InstructorName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Capacity,
    int EnrolledCount,
    string Status);

public sealed record BatchEnrollmentResponse(
    Guid EnrollmentId, Guid StudentId, string RegistrationNumber, string StudentName, DateTime EnrolledOnUtc, string Status);

public sealed record BatchResponse(BatchSummaryResponse Batch, IReadOnlyList<BatchEnrollmentResponse> Enrollments);

public sealed record GetBatchesQuery(Guid? CourseId, BatchStatus? Status, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<BatchSummaryResponse>>;

public sealed record GetBatchByIdQuery(Guid BatchId) : IQuery<BatchResponse>;

internal sealed class BatchQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetBatchesQuery, PagedList<BatchSummaryResponse>>,
      IQueryHandler<GetBatchByIdQuery, BatchResponse>
{
    public async Task<Result<PagedList<BatchSummaryResponse>>> Handle(GetBatchesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Batches;

        if (request.CourseId is { } courseGuid)
        {
            var courseId = new CourseId(courseGuid);
            query = query.Where(b => b.CourseId == courseId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(b => b.Status == status);
        }

        return await PagedList<BatchSummaryResponse>.CreateAsync(
            Project(query.OrderByDescending(b => b.StartDate)), request.Page, request.PageSize, cancellationToken);
    }

    public async Task<Result<BatchResponse>> Handle(GetBatchByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new BatchId(request.BatchId);

        var batch = await Project(db.Batches.Where(b => b.Id == id)).FirstOrDefaultAsync(cancellationToken);
        if (batch is null)
        {
            return BatchErrors.NotFound(id);
        }

        // IgnoreQueryFilters so enrollments of soft-deleted students remain visible in the batch history.
        var enrollments = await (
            from e in db.Enrollments
            join s in db.Students.IgnoreQueryFilters() on e.StudentId equals s.Id
            where e.BatchId == id
            orderby s.Name.LastName, s.Name.FirstName
            select new BatchEnrollmentResponse(
                e.Id.Value,
                s.Id.Value,
                s.RegistrationNumber.Value,
                s.Name.FirstName + " " + s.Name.LastName,
                e.EnrolledOnUtc,
                e.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new BatchResponse(batch, enrollments);
    }

    private IQueryable<BatchSummaryResponse> Project(IQueryable<Batch> query) =>
        from b in query
        join c in db.Courses on b.CourseId equals c.Id
        join i in db.Instructors on b.InstructorId equals i.Id
        select new BatchSummaryResponse(
            b.Id.Value,
            b.Name,
            c.Id.Value,
            c.Code.Value,
            c.Title,
            i.Id.Value,
            i.Name.FirstName + " " + i.Name.LastName,
            b.StartDate,
            b.EndDate,
            b.Capacity,
            b.Enrollments.Count(e => e.Status != EnrollmentStatus.Cancelled),
            b.Status.ToString());
}
