using Institute.Domain.Abstractions;

namespace Institute.Domain.Departments;

public readonly record struct DepartmentId(Guid Value)
{
    public static DepartmentId New() => new(Guid.NewGuid());
}

public sealed class Department : AggregateRoot<DepartmentId>
{
    public const int NameMaxLength = 100;
    public const int CodeMaxLength = 10;

    private Department(DepartmentId id, string name, string code)
        : base(id)
    {
        Name = name;
        Code = code;
    }

    private Department()
    {
        Name = null!;
        Code = null!;
    }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public static Result<Department> Create(string? name, string? code)
    {
        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > CodeMaxLength || !code.Trim().All(char.IsAsciiLetterOrDigit))
        {
            return DepartmentErrors.InvalidCode;
        }

        return new Department(DepartmentId.New(), nameResult.Value, code.Trim().ToUpperInvariant());
    }

    public Result Rename(string? name)
    {
        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        Name = nameResult.Value;
        return Result.Success();
    }

    private static Result<string> ValidateName(string? name) =>
        string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength
            ? DepartmentErrors.InvalidName
            : name.Trim();
}

public static class DepartmentErrors
{
    public static readonly Error InvalidName = Error.Validation(
        "Department.InvalidName", $"Department name is required and must not exceed {Department.NameMaxLength} characters.");

    public static readonly Error InvalidCode = Error.Validation(
        "Department.InvalidCode", $"Department code is required, alphanumeric and at most {Department.CodeMaxLength} characters.");

    public static readonly Error CodeNotUnique = Error.Conflict("Department.CodeNotUnique", "A department with this code already exists.");

    public static Error NotFound(DepartmentId id) => Error.NotFound("Department.NotFound", $"Department '{id.Value}' was not found.");
}

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(DepartmentId id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Department department);
}
