using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Shared;

namespace Institute.Application.Courses;

public sealed record CreateCourseCommand(
    string Code,
    string Title,
    string? Description,
    int DurationInWeeks,
    decimal FeeAmount,
    string Currency,
    Guid DepartmentId) : ICommand<Guid>;

public sealed record UpdateCourseCommand(Guid CourseId, string Title, string? Description, int DurationInWeeks) : ICommand;

public sealed record ChangeCourseFeeCommand(Guid CourseId, decimal FeeAmount, string Currency) : ICommand;

public sealed record SetCourseActiveCommand(Guid CourseId, bool IsActive) : ICommand;

internal sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(CourseCode.MaxLength);
        RuleFor(c => c.Title).NotEmpty().MaximumLength(Course.TitleMaxLength);
        RuleFor(c => c.Description).MaximumLength(Course.DescriptionMaxLength);
        RuleFor(c => c.DurationInWeeks).InclusiveBetween(Course.MinDurationInWeeks, Course.MaxDurationInWeeks);
        RuleFor(c => c.FeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.DepartmentId).NotEmpty();
    }
}

internal sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(Course.TitleMaxLength);
        RuleFor(c => c.Description).MaximumLength(Course.DescriptionMaxLength);
        RuleFor(c => c.DurationInWeeks).InclusiveBetween(Course.MinDurationInWeeks, Course.MaxDurationInWeeks);
    }
}

internal sealed class ChangeCourseFeeCommandValidator : AbstractValidator<ChangeCourseFeeCommand>
{
    public ChangeCourseFeeCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.FeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}

internal sealed class CourseCommandHandlers(
    ICourseRepository courses,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCourseCommand, Guid>,
      ICommandHandler<UpdateCourseCommand>,
      ICommandHandler<ChangeCourseFeeCommand>,
      ICommandHandler<SetCourseActiveCommand>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var code = CourseCode.Create(request.Code);
        if (code.IsFailure)
        {
            return code.Error;
        }

        var fee = Money.Create(request.FeeAmount, request.Currency);
        if (fee.IsFailure)
        {
            return fee.Error;
        }

        var departmentId = new DepartmentId(request.DepartmentId);
        if (!await departments.ExistsAsync(departmentId, cancellationToken))
        {
            return DepartmentErrors.NotFound(departmentId);
        }

        if (await courses.CodeExistsAsync(code.Value, cancellationToken))
        {
            return CourseErrors.CodeNotUnique;
        }

        var course = Course.Create(code.Value, request.Title, request.Description, request.DurationInWeeks, fee.Value, departmentId);
        if (course.IsFailure)
        {
            return course.Error;
        }

        courses.Add(course.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return course.Value.Id.Value;
    }

    public Task<Result> Handle(UpdateCourseCommand request, CancellationToken cancellationToken) =>
        Mutate(request.CourseId, c => c.UpdateDetails(request.Title, request.Description, request.DurationInWeeks), cancellationToken);

    public Task<Result> Handle(ChangeCourseFeeCommand request, CancellationToken cancellationToken) =>
        Mutate(
            request.CourseId,
            c =>
            {
                var fee = Money.Create(request.FeeAmount, request.Currency);
                if (fee.IsFailure)
                {
                    return fee.Error;
                }

                c.ChangeFee(fee.Value);
                return Result.Success();
            },
            cancellationToken);

    public Task<Result> Handle(SetCourseActiveCommand request, CancellationToken cancellationToken) =>
        Mutate(
            request.CourseId,
            c =>
            {
                if (request.IsActive)
                {
                    c.Activate();
                }
                else
                {
                    c.Deactivate();
                }

                return Result.Success();
            },
            cancellationToken);

    private async Task<Result> Mutate(Guid courseId, Func<Course, Result> action, CancellationToken cancellationToken)
    {
        var id = new CourseId(courseId);
        var course = await courses.GetByIdAsync(id, cancellationToken);
        if (course is null)
        {
            return CourseErrors.NotFound(id);
        }

        var result = action(course);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
