using System.Security.Cryptography;
using Institute.Domain.Abstractions;
using Institute.Domain.Shared;

namespace Institute.Domain.Students;

public readonly record struct StudentId(Guid Value)
{
    public static StudentId New() => new(Guid.NewGuid());
}

public enum StudentStatus
{
    Active = 1,
    Suspended = 2,
    Graduated = 3,
    Withdrawn = 4,
}

/// <summary>Human friendly, unique registration number in the form <c>STU-2026-7K3QZ9</c>.</summary>
public sealed class RegistrationNumber : ValueObject
{
    public const int MaxLength = 20;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private RegistrationNumber(string value) => Value = value;

    public string Value { get; }

    public static RegistrationNumber Generate(int year) =>
        new($"STU-{year}-{RandomNumberGenerator.GetString(Alphabet, 6)}");

    public static Result<RegistrationNumber> Create(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > MaxLength
            ? StudentErrors.InvalidRegistrationNumber
            : new RegistrationNumber(value.Trim().ToUpperInvariant());

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}

public sealed record StudentRegisteredDomainEvent(StudentId StudentId, string RegistrationNumber, string Email) : DomainEvent;

public sealed record StudentStatusChangedDomainEvent(StudentId StudentId, StudentStatus From, StudentStatus To) : DomainEvent;

public sealed class Student : AggregateRoot<StudentId>, ISoftDeletable
{
    public const int MinimumAge = 10;

    private Student(
        StudentId id, RegistrationNumber registrationNumber, PersonName name, Email email, PhoneNumber phone, DateOnly dateOfBirth, Address address)
        : base(id)
    {
        RegistrationNumber = registrationNumber;
        Name = name;
        Email = email;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Address = address;
        Status = StudentStatus.Active;
    }

    private Student()
    {
        RegistrationNumber = null!;
        Name = null!;
        Email = null!;
        Phone = null!;
        Address = null!;
    }

    public RegistrationNumber RegistrationNumber { get; private set; }

    public PersonName Name { get; private set; }

    public Email Email { get; private set; }

    public PhoneNumber Phone { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public Address Address { get; private set; }

    public StudentStatus Status { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedOnUtc { get; private set; }

    public static Result<Student> Register(
        PersonName name, Email email, PhoneNumber phone, DateOnly dateOfBirth, Address address, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(phone);
        ArgumentNullException.ThrowIfNull(address);

        var today = DateOnly.FromDateTime(utcNow);

        if (dateOfBirth > today)
        {
            return StudentErrors.DateOfBirthInFuture;
        }

        if (CalculateAge(dateOfBirth, today) < MinimumAge)
        {
            return StudentErrors.TooYoung;
        }

        var student = new Student(
            StudentId.New(), RegistrationNumber.Generate(utcNow.Year), name, email, phone, dateOfBirth, address);

        student.RaiseDomainEvent(new StudentRegisteredDomainEvent(student.Id, student.RegistrationNumber.Value, email.Value));

        return student;
    }

    public void UpdateProfile(PersonName name, Email email, PhoneNumber phone)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Phone = phone ?? throw new ArgumentNullException(nameof(phone));
    }

    public void ChangeAddress(Address address) => Address = address ?? throw new ArgumentNullException(nameof(address));

    public Result Suspend() => TransitionTo(StudentStatus.Suspended, StudentStatus.Active);

    public Result Reactivate() => TransitionTo(StudentStatus.Active, StudentStatus.Suspended);

    public Result Graduate() => TransitionTo(StudentStatus.Graduated, StudentStatus.Active);

    public Result Withdraw() => TransitionTo(StudentStatus.Withdrawn, StudentStatus.Active, StudentStatus.Suspended);

    public Result ChangeStatus(StudentStatus target) => target switch
    {
        StudentStatus.Active => Reactivate(),
        StudentStatus.Suspended => Suspend(),
        StudentStatus.Graduated => Graduate(),
        StudentStatus.Withdrawn => Withdraw(),
        _ => StudentErrors.InvalidStatusTransition(Status, target),
    };

    internal static int CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private Result TransitionTo(StudentStatus target, params StudentStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
        {
            return StudentErrors.InvalidStatusTransition(Status, target);
        }

        var previous = Status;
        Status = target;
        RaiseDomainEvent(new StudentStatusChangedDomainEvent(Id, previous, target));
        return Result.Success();
    }
}

public static class StudentErrors
{
    public static readonly Error DateOfBirthInFuture = Error.Validation("Student.DateOfBirthInFuture", "Date of birth can not be in the future.");

    public static readonly Error TooYoung = Error.Validation("Student.TooYoung", $"Student must be at least {Student.MinimumAge} years old.");

    public static readonly Error EmailNotUnique = Error.Conflict("Student.EmailNotUnique", "A student with this email already exists.");

    public static readonly Error InvalidRegistrationNumber = Error.Validation("Student.InvalidRegistrationNumber", "Registration number is invalid.");

    public static readonly Error NotActive = Error.Conflict("Student.NotActive", "Only active students can perform this action.");

    public static Error InvalidStatusTransition(StudentStatus from, StudentStatus to) =>
        Error.Conflict("Student.InvalidStatusTransition", $"Can not change student status from {from} to {to}.");

    public static Error NotFound(StudentId id) => Error.NotFound("Student.NotFound", $"Student '{id.Value}' was not found.");
}

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, StudentId? excludeStudentId = null, CancellationToken cancellationToken = default);

    void Add(Student student);

    /// <summary>Soft deletes the student.</summary>
    void Remove(Student student);
}
