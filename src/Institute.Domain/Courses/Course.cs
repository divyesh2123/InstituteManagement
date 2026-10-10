using System.Text.RegularExpressions;
using Institute.Domain.Abstractions;
using Institute.Domain.Departments;
using Institute.Domain.Shared;

namespace Institute.Domain.Courses;

public readonly record struct CourseId(Guid Value)
{
    public static CourseId New() => new(Guid.NewGuid());
}

public sealed partial class CourseCode : ValueObject
{
    public const int MaxLength = 20;

    private CourseCode(string value) => Value = value;

    public string Value { get; }

    public static Result<CourseCode> Create(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();

        return normalized is null || !CodeRegex().IsMatch(normalized)
            ? CourseErrors.InvalidCode
            : new CourseCode(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9\-]{1,19}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex CodeRegex();
}

public sealed class Course : AggregateRoot<CourseId>
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MinDurationInWeeks = 1;
    public const int MaxDurationInWeeks = 208;

    private Course(CourseId id, CourseCode code, string title, string? description, int durationInWeeks, Money fee, DepartmentId departmentId)
        : base(id)
    {
        Code = code;
        Title = title;
        Description = description;
        DurationInWeeks = durationInWeeks;
        Fee = fee;
        DepartmentId = departmentId;
        IsActive = true;
    }

    private Course()
    {
        Code = null!;
        Title = null!;
        Fee = null!;
    }

    public CourseCode Code { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public int DurationInWeeks { get; private set; }

    public Money Fee { get; private set; }

    public DepartmentId DepartmentId { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Course> Create(
        CourseCode code, string? title, string? description, int durationInWeeks, Money fee, DepartmentId departmentId)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(fee);

        var validation = ValidateDetails(title, description, durationInWeeks);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        return new Course(CourseId.New(), code, title!.Trim(), Normalize(description), durationInWeeks, fee, departmentId);
    }

    public Result UpdateDetails(string? title, string? description, int durationInWeeks)
    {
        var validation = ValidateDetails(title, description, durationInWeeks);
        if (validation.IsFailure)
        {
            return validation;
        }

        Title = title!.Trim();
        Description = Normalize(description);
        DurationInWeeks = durationInWeeks;
        return Result.Success();
    }

    public void ChangeFee(Money fee) => Fee = fee ?? throw new ArgumentNullException(nameof(fee));

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static Result ValidateDetails(string? title, string? description, int durationInWeeks)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
        {
            return CourseErrors.InvalidTitle;
        }

        if (description?.Trim().Length > DescriptionMaxLength)
        {
            return CourseErrors.DescriptionTooLong;
        }

        if (durationInWeeks is < MinDurationInWeeks or > MaxDurationInWeeks)
        {
            return CourseErrors.InvalidDuration;
        }

        return Result.Success();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public static class CourseErrors
{
    public static readonly Error InvalidCode = Error.Validation(
        "Course.InvalidCode", "Course code must be 2-20 characters of letters, digits or '-' (e.g. CS-101).");

    public static readonly Error InvalidTitle = Error.Validation(
        "Course.InvalidTitle", $"Course title is required and must not exceed {Course.TitleMaxLength} characters.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "Course.DescriptionTooLong", $"Description must not exceed {Course.DescriptionMaxLength} characters.");

    public static readonly Error InvalidDuration = Error.Validation(
        "Course.InvalidDuration", $"Duration must be between {Course.MinDurationInWeeks} and {Course.MaxDurationInWeeks} weeks.");

    public static readonly Error CodeNotUnique = Error.Conflict("Course.CodeNotUnique", "A course with this code already exists.");

    public static readonly Error Inactive = Error.Conflict("Course.Inactive", "The course is inactive.");

    public static Error NotFound(CourseId id) => Error.NotFound("Course.NotFound", $"Course '{id.Value}' was not found.");
}

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(CourseCode code, CancellationToken cancellationToken = default);

    void Add(Course course);
}
