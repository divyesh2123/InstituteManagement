using Institute.Domain.Abstractions;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Domain.Tests;

internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    public static Student Student(string email = "student@example.com") =>
        Value(Domain.Students.Student.Register(
            Value(PersonName.Create("Asha", "Rao")),
            Value(Email.Create(email)),
            Value(PhoneNumber.Create("+919800000000")),
            new DateOnly(2003, 1, 1),
            Value(Address.Create("1 Main St", "Vadodara", "Gujarat", "390001", "India")),
            UtcNow));

    public static Course Course(decimal fee = 10_000m) =>
        Value(Domain.Courses.Course.Create(
            Value(CourseCode.Create("CS-101")), "Intro", null, 12, Value(Money.Create(fee)), DepartmentId.New()));

    public static Instructor Instructor() =>
        Value(Domain.Instructors.Instructor.Hire(
            Value(PersonName.Create("Ravi", "Iyer")),
            Value(Email.Create("ravi@example.com")),
            Value(PhoneNumber.Create("9800000001")),
            DepartmentId.New(),
            Today,
            Today));

    public static T Value<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Description : string.Empty);
        return result.Value;
    }
}
