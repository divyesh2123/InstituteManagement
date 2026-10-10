using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Shared;

namespace Institute.Application.Instructors;

public sealed record HireInstructorCommand(
    string FirstName, string LastName, string Email, string Phone, Guid DepartmentId, DateOnly HireDate) : ICommand<Guid>;

internal sealed class HireInstructorCommandValidator : AbstractValidator<HireInstructorCommand>
{
    public HireInstructorCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(Email.MaxLength);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.DepartmentId).NotEmpty();
        RuleFor(c => c.HireDate).NotEmpty();
    }
}

internal sealed class HireInstructorCommandHandler(
    IInstructorRepository instructors,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : ICommandHandler<HireInstructorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(HireInstructorCommand request, CancellationToken cancellationToken)
    {
        var name = PersonName.Create(request.FirstName, request.LastName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var email = Email.Create(request.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        var phone = PhoneNumber.Create(request.Phone);
        if (phone.IsFailure)
        {
            return phone.Error;
        }

        var departmentId = new DepartmentId(request.DepartmentId);
        if (!await departments.ExistsAsync(departmentId, cancellationToken))
        {
            return DepartmentErrors.NotFound(departmentId);
        }

        if (await instructors.EmailExistsAsync(email.Value, cancellationToken))
        {
            return InstructorErrors.EmailNotUnique;
        }

        var instructor = Instructor.Hire(name.Value, email.Value, phone.Value, departmentId, request.HireDate, clock.Today);
        if (instructor.IsFailure)
        {
            return instructor.Error;
        }

        instructors.Add(instructor.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return instructor.Value.Id.Value;
    }
}

public sealed record DeactivateInstructorCommand(Guid InstructorId) : ICommand;

internal sealed class DeactivateInstructorCommandHandler(IInstructorRepository instructors, IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateInstructorCommand>
{
    public async Task<Result> Handle(DeactivateInstructorCommand request, CancellationToken cancellationToken)
    {
        var id = new InstructorId(request.InstructorId);
        var instructor = await instructors.GetByIdAsync(id, cancellationToken);
        if (instructor is null)
        {
            return InstructorErrors.NotFound(id);
        }

        var result = instructor.Deactivate();
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
