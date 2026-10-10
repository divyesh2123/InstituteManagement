using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Instructors;
using Institute.Domain.Students;

namespace Institute.Application.Batches;

public sealed record ScheduleBatchCommand(
    Guid CourseId, Guid InstructorId, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity) : ICommand<Guid>;

public sealed record ChangeBatchStatusCommand(Guid BatchId, BatchStatus Status) : ICommand;

public sealed record AssignBatchInstructorCommand(Guid BatchId, Guid InstructorId) : ICommand;

public sealed record EnrollStudentCommand(Guid BatchId, Guid StudentId) : ICommand<Guid>;

public sealed record CancelEnrollmentCommand(Guid BatchId, Guid StudentId) : ICommand;

internal sealed class ScheduleBatchCommandValidator : AbstractValidator<ScheduleBatchCommand>
{
    public ScheduleBatchCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.InstructorId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Batch.NameMaxLength);
        RuleFor(c => c.StartDate).NotEmpty();
        RuleFor(c => c.EndDate).NotEmpty().GreaterThan(c => c.StartDate);
        RuleFor(c => c.Capacity).InclusiveBetween(Batch.MinCapacity, Batch.MaxCapacity);
    }
}

internal sealed class ChangeBatchStatusCommandValidator : AbstractValidator<ChangeBatchStatusCommand>
{
    public ChangeBatchStatusCommandValidator()
    {
        RuleFor(c => c.BatchId).NotEmpty();
        RuleFor(c => c.Status).IsInEnum().NotEqual(BatchStatus.Planned).WithMessage("A batch can not be moved back to Planned.");
    }
}

internal sealed class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(c => c.BatchId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
    }
}

internal sealed class BatchCommandHandlers(
    IBatchRepository batches,
    ICourseRepository courses,
    IInstructorRepository instructors,
    IStudentRepository students,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : ICommandHandler<ScheduleBatchCommand, Guid>,
      ICommandHandler<ChangeBatchStatusCommand>,
      ICommandHandler<AssignBatchInstructorCommand>,
      ICommandHandler<EnrollStudentCommand, Guid>,
      ICommandHandler<CancelEnrollmentCommand>
{
    public async Task<Result<Guid>> Handle(ScheduleBatchCommand request, CancellationToken cancellationToken)
    {
        var courseId = new CourseId(request.CourseId);
        var course = await courses.GetByIdAsync(courseId, cancellationToken);
        if (course is null)
        {
            return CourseErrors.NotFound(courseId);
        }

        var instructorId = new InstructorId(request.InstructorId);
        var instructor = await instructors.GetByIdAsync(instructorId, cancellationToken);
        if (instructor is null)
        {
            return InstructorErrors.NotFound(instructorId);
        }

        var batch = Batch.Schedule(course, instructor, request.Name, request.StartDate, request.EndDate, request.Capacity, clock.Today);
        if (batch.IsFailure)
        {
            return batch.Error;
        }

        batches.Add(batch.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return batch.Value.Id.Value;
    }

    public Task<Result> Handle(ChangeBatchStatusCommand request, CancellationToken cancellationToken) =>
        Mutate(
            request.BatchId,
            batch => Task.FromResult(request.Status switch
            {
                BatchStatus.Ongoing => batch.Start(),
                BatchStatus.Completed => batch.Complete(),
                BatchStatus.Cancelled => batch.Cancel(),
                _ => BatchErrors.InvalidTransition(batch.Status, request.Status),
            }),
            cancellationToken);

    public Task<Result> Handle(AssignBatchInstructorCommand request, CancellationToken cancellationToken) =>
        Mutate(
            request.BatchId,
            async batch =>
            {
                var instructorId = new InstructorId(request.InstructorId);
                var instructor = await instructors.GetByIdAsync(instructorId, cancellationToken);

                return instructor is null ? InstructorErrors.NotFound(instructorId) : batch.AssignInstructor(instructor);
            },
            cancellationToken);

    public async Task<Result<Guid>> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var batchId = new BatchId(request.BatchId);
        var batch = await batches.GetByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return BatchErrors.NotFound(batchId);
        }

        var studentId = new StudentId(request.StudentId);
        var student = await students.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return StudentErrors.NotFound(studentId);
        }

        var enrollment = batch.Enroll(student, clock.UtcNow);
        if (enrollment.IsFailure)
        {
            return enrollment.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return enrollment.Value.Id.Value;
    }

    public Task<Result> Handle(CancelEnrollmentCommand request, CancellationToken cancellationToken) =>
        Mutate(request.BatchId, batch => Task.FromResult(batch.CancelEnrollment(new StudentId(request.StudentId))), cancellationToken);

    private async Task<Result> Mutate(Guid batchGuid, Func<Batch, Task<Result>> action, CancellationToken cancellationToken)
    {
        var batchId = new BatchId(batchGuid);
        var batch = await batches.GetByIdAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return BatchErrors.NotFound(batchId);
        }

        var result = await action(batch);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
