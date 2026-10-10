using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Departments;

namespace Institute.Application.Departments;

public sealed record CreateDepartmentCommand(string Name, string Code) : ICommand<Guid>;

internal sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Department.NameMaxLength);
        RuleFor(c => c.Code).NotEmpty().MaximumLength(Department.CodeMaxLength).Matches("^[A-Za-z0-9]+$");
    }
}

internal sealed class CreateDepartmentCommandHandler(IDepartmentRepository departments, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var result = Department.Create(request.Name, request.Code);
        if (result.IsFailure)
        {
            return result.Error;
        }

        if (await departments.CodeExistsAsync(result.Value.Code, cancellationToken))
        {
            return DepartmentErrors.CodeNotUnique;
        }

        departments.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id.Value;
    }
}
