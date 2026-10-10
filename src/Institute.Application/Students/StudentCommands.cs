using FluentValidation;
using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Application.Students;

public sealed record AddressInput(string Street, string City, string State, string PostalCode, string Country);

public sealed record RegisterStudentCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    AddressInput Address) : ICommand<Guid>;

public sealed record UpdateStudentCommand(
    Guid StudentId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    AddressInput Address) : ICommand;

public sealed record ChangeStudentStatusCommand(Guid StudentId, StudentStatus Status) : ICommand;

public sealed record DeleteStudentCommand(Guid StudentId) : ICommand;

internal sealed class AddressInputValidator : AbstractValidator<AddressInput>
{
    public AddressInputValidator()
    {
        RuleFor(a => a.Street).NotEmpty().MaximumLength(Address.StreetMaxLength);
        RuleFor(a => a.City).NotEmpty().MaximumLength(Address.CityMaxLength);
        RuleFor(a => a.State).NotEmpty().MaximumLength(Address.StateMaxLength);
        RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(Address.PostalCodeMaxLength);
        RuleFor(a => a.Country).NotEmpty().MaximumLength(Address.CountryMaxLength);
    }
}

internal sealed class RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>
{
    public RegisterStudentCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(Email.MaxLength);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.DateOfBirth).NotEmpty();
        RuleFor(c => c.Address).NotNull().SetValidator(new AddressInputValidator());
    }
}

internal sealed class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(Email.MaxLength);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Address).NotNull().SetValidator(new AddressInputValidator());
    }
}

internal sealed class ChangeStudentStatusCommandValidator : AbstractValidator<ChangeStudentStatusCommand>
{
    public ChangeStudentStatusCommandValidator()
    {
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
    }
}

internal sealed class StudentCommandHandlers(IStudentRepository students, IUnitOfWork unitOfWork, IDateTimeProvider clock)
    : ICommandHandler<RegisterStudentCommand, Guid>,
      ICommandHandler<UpdateStudentCommand>,
      ICommandHandler<ChangeStudentStatusCommand>,
      ICommandHandler<DeleteStudentCommand>
{
    public async Task<Result<Guid>> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {
        var profile = CreateProfile(request.FirstName, request.LastName, request.Email, request.Phone, request.Address);
        if (profile.IsFailure)
        {
            return profile.Error;
        }

        var (name, email, phone, address) = profile.Value;

        if (await students.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            return StudentErrors.EmailNotUnique;
        }

        var student = Student.Register(name, email, phone, request.DateOfBirth, address, clock.UtcNow);
        if (student.IsFailure)
        {
            return student.Error;
        }

        students.Add(student.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return student.Value.Id.Value;
    }

    public async Task<Result> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var id = new StudentId(request.StudentId);
        var student = await students.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return StudentErrors.NotFound(id);
        }

        var profile = CreateProfile(request.FirstName, request.LastName, request.Email, request.Phone, request.Address);
        if (profile.IsFailure)
        {
            return profile.Error;
        }

        var (name, email, phone, address) = profile.Value;

        if (await students.EmailExistsAsync(email, id, cancellationToken))
        {
            return StudentErrors.EmailNotUnique;
        }

        student.UpdateProfile(name, email, phone);
        student.ChangeAddress(address);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ChangeStudentStatusCommand request, CancellationToken cancellationToken)
    {
        var id = new StudentId(request.StudentId);
        var student = await students.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return StudentErrors.NotFound(id);
        }

        var result = student.ChangeStatus(request.Status);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var id = new StudentId(request.StudentId);
        var student = await students.GetByIdAsync(id, cancellationToken);
        if (student is null)
        {
            return StudentErrors.NotFound(id);
        }

        students.Remove(student);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Result<(PersonName Name, Email Email, PhoneNumber Phone, Address Address)> CreateProfile(
        string firstName, string lastName, string email, string phone, AddressInput address)
    {
        var nameResult = PersonName.Create(firstName, lastName);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsFailure)
        {
            return phoneResult.Error;
        }

        var addressResult = Address.Create(address.Street, address.City, address.State, address.PostalCode, address.Country);
        if (addressResult.IsFailure)
        {
            return addressResult.Error;
        }

        return (nameResult.Value, emailResult.Value, phoneResult.Value, addressResult.Value);
    }
}
