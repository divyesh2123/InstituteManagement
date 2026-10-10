using Institute.Domain.Abstractions;
using Institute.Domain.Departments;
using Institute.Domain.Shared;

namespace Institute.Domain.Instructors;

public readonly record struct InstructorId(Guid Value)
{
    public static InstructorId New() => new(Guid.NewGuid());
}

public sealed class Instructor : AggregateRoot<InstructorId>
{
    private Instructor(InstructorId id, PersonName name, Email email, PhoneNumber phone, DepartmentId departmentId, DateOnly hireDate)
        : base(id)
    {
        Name = name;
        Email = email;
        Phone = phone;
        DepartmentId = departmentId;
        HireDate = hireDate;
        IsActive = true;
    }

    private Instructor()
    {
        Name = null!;
        Email = null!;
        Phone = null!;
    }

    public PersonName Name { get; private set; }

    public Email Email { get; private set; }

    public PhoneNumber Phone { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public DateOnly HireDate { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Instructor> Hire(
        PersonName name, Email email, PhoneNumber phone, DepartmentId departmentId, DateOnly hireDate, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(phone);

        if (hireDate > today.AddMonths(3))
        {
            return InstructorErrors.HireDateTooFarInFuture;
        }

        return new Instructor(InstructorId.New(), name, email, phone, departmentId, hireDate);
    }

    public void UpdateContact(Email email, PhoneNumber phone)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
    }

    public Result TransferTo(DepartmentId departmentId)
    {
        if (!IsActive)
        {
            return InstructorErrors.Inactive;
        }

        DepartmentId = departmentId;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return InstructorErrors.AlreadyInactive;
        }

        IsActive = false;
        return Result.Success();
    }
}

public static class InstructorErrors
{
    public static readonly Error HireDateTooFarInFuture = Error.Validation(
        "Instructor.HireDateTooFarInFuture", "Hire date can not be more than 3 months in the future.");

    public static readonly Error EmailNotUnique = Error.Conflict("Instructor.EmailNotUnique", "An instructor with this email already exists.");

    public static readonly Error Inactive = Error.Conflict("Instructor.Inactive", "The instructor is inactive.");

    public static readonly Error AlreadyInactive = Error.Conflict("Instructor.AlreadyInactive", "The instructor is already inactive.");

    public static Error NotFound(InstructorId id) => Error.NotFound("Instructor.NotFound", $"Instructor '{id.Value}' was not found.");
}

public interface IInstructorRepository
{
    Task<Instructor?> GetByIdAsync(InstructorId id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    void Add(Instructor instructor);
}
