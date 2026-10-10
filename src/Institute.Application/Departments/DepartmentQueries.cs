using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace Institute.Application.Departments;

public sealed record DepartmentResponse(Guid Id, string Name, string Code, int InstructorCount, int CourseCount, DateTime CreatedOnUtc);

public sealed record GetDepartmentsQuery : IQuery<IReadOnlyList<DepartmentResponse>>;

public sealed record GetDepartmentByIdQuery(Guid DepartmentId) : IQuery<DepartmentResponse>;

internal sealed class DepartmentQueryHandlers(IReadDbContext db)
    : IQueryHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponse>>,
      IQueryHandler<GetDepartmentByIdQuery, DepartmentResponse>
{
    public async Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var items = await Project(db.Departments.OrderBy(d => d.Name)).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DepartmentResponse>>(items);
    }

    public async Task<Result<DepartmentResponse>> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var id = new DepartmentId(request.DepartmentId);
        var department = await Project(db.Departments.Where(d => d.Id == id)).FirstOrDefaultAsync(cancellationToken);

        return department is null ? DepartmentErrors.NotFound(id) : department;
    }

    private IQueryable<DepartmentResponse> Project(IQueryable<Department> query) =>
        query.Select(d => new DepartmentResponse(
            d.Id.Value,
            d.Name,
            d.Code,
            db.Instructors.Count(i => i.DepartmentId == d.Id && i.IsActive),
            db.Courses.Count(c => c.DepartmentId == d.Id),
            d.CreatedOnUtc));
}
