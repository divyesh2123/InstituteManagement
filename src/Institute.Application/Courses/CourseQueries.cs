using Institute.Application.Abstractions;
using Institute.Application.Common;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Courses;

public sealed record CourseResponse(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    int DurationInWeeks,
    decimal FeeAmount,
    string Currency,
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    int OpenBatchCount);

public sealed record GetCoursesQuery(string? Search, Guid? DepartmentId, bool? IsActive, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<CourseResponse>>;

public sealed record GetCourseByIdQuery(Guid CourseId) : IQuery<CourseResponse>;

internal sealed class CourseQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetCoursesQuery, PagedList<CourseResponse>>,
      IQueryHandler<GetCourseByIdQuery, CourseResponse>
{
    public async Task<Result<PagedList<CourseResponse>>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Courses;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var code = CourseCode.Create(term);
            query = code.IsSuccess
                ? query.Where(c => c.Title.Contains(term) || c.Code == code.Value)
                : query.Where(c => c.Title.Contains(term));
        }

        if (request.DepartmentId is { } departmentGuid)
        {
            var departmentId = new DepartmentId(departmentGuid);
            query = query.Where(c => c.DepartmentId == departmentId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        return await PagedList<CourseResponse>.CreateAsync(
            Project(query.OrderBy(c => c.Title)), request.Page, request.PageSize, cancellationToken);
    }

    public async Task<Result<CourseResponse>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new CourseId(request.CourseId);
        var course = await Project(db.Courses.Where(c => c.Id == id)).FirstOrDefaultAsync(cancellationToken);

        return course is null ? CourseErrors.NotFound(id) : course;
    }

    private IQueryable<CourseResponse> Project(IQueryable<Course> query) =>
        from c in query
        join d in db.Departments on c.DepartmentId equals d.Id
        select new CourseResponse(
            c.Id.Value,
            c.Code.Value,
            c.Title,
            c.Description,
            c.DurationInWeeks,
            c.Fee.Amount,
            c.Fee.Currency,
            d.Id.Value,
            d.Name,
            c.IsActive,
            db.Batches.Count(b => b.CourseId == c.Id && (b.Status == BatchStatus.Planned || b.Status == BatchStatus.Ongoing)));
}
