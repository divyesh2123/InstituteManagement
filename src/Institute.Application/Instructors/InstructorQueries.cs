using Institute.Application.Abstractions;
using Institute.Application.Common;
using Institute.Domain.Abstractions;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Instructors;

public sealed record InstructorResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    Guid DepartmentId,
    string DepartmentName,
    DateOnly HireDate,
    bool IsActive);

public sealed record GetInstructorsQuery(string? Search, Guid? DepartmentId, bool? IsActive, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<InstructorResponse>>;

public sealed record GetInstructorByIdQuery(Guid InstructorId) : IQuery<InstructorResponse>;

internal sealed class InstructorQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetInstructorsQuery, PagedList<InstructorResponse>>,
      IQueryHandler<GetInstructorByIdQuery, InstructorResponse>
{
    public async Task<Result<PagedList<InstructorResponse>>> Handle(GetInstructorsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Instructors;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(i => i.Name.FirstName.Contains(term) || i.Name.LastName.Contains(term));
        }

        if (request.DepartmentId is { } departmentGuid)
        {
            var departmentId = new DepartmentId(departmentGuid);
            query = query.Where(i => i.DepartmentId == departmentId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(i => i.IsActive == isActive);
        }

        var ordered = query.OrderBy(i => i.Name.LastName).ThenBy(i => i.Name.FirstName);

        return await PagedList<InstructorResponse>.CreateAsync(Project(ordered), request.Page, request.PageSize, cancellationToken);
    }

    public async Task<Result<InstructorResponse>> Handle(GetInstructorByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new InstructorId(request.InstructorId);
        var instructor = await Project(db.Instructors.Where(i => i.Id == id)).FirstOrDefaultAsync(cancellationToken);

        return instructor is null ? InstructorErrors.NotFound(id) : instructor;
    }

    private IQueryable<InstructorResponse> Project(IQueryable<Instructor> query) =>
        from i in query
        join d in db.Departments on i.DepartmentId equals d.Id
        select new InstructorResponse(
            i.Id.Value,
            i.Name.FirstName,
            i.Name.LastName,
            i.Email.Value,
            i.Phone.Value,
            d.Id.Value,
            d.Name,
            i.HireDate,
            i.IsActive);
}
