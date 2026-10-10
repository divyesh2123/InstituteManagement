using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Domain.Tests;

public sealed class StudentTests
{
    [Fact]
    public void Register_ShouldCreateActiveStudent_AndRaiseEvent()
    {
        var student = TestData.Student();

        student.Status.Should().Be(StudentStatus.Active);
        student.RegistrationNumber.Value.Should().MatchRegex(@"^STU-2026-[A-Z0-9]{6}$");
        student.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StudentRegisteredDomainEvent>();
    }

    [Fact]
    public void Register_ShouldFail_WhenStudentIsTooYoung()
    {
        var result = Student.Register(
            TestData.Value(PersonName.Create("Kid", "Doe")),
            TestData.Value(Email.Create("kid@example.com")),
            TestData.Value(PhoneNumber.Create("9800000000")),
            TestData.Today.AddYears(-9),
            TestData.Value(Address.Create("a", "b", "c", "d", "e")),
            TestData.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StudentErrors.TooYoung);
    }

    [Theory]
    [InlineData(2016, 10, 10, 10)]
    [InlineData(2016, 10, 11, 9)]
    [InlineData(2000, 2, 29, 26)]
    public void CalculateAge_ShouldRespectBirthday(int year, int month, int day, int expected) =>
        Student.CalculateAge(new DateOnly(year, month, day), TestData.Today).Should().Be(expected);

    [Fact]
    public void StatusTransitions_ShouldFollowLifecycle()
    {
        var student = TestData.Student();

        student.Suspend().IsSuccess.Should().BeTrue();
        student.Graduate().IsFailure.Should().BeTrue("a suspended student can not graduate");
        student.Reactivate().IsSuccess.Should().BeTrue();
        student.Graduate().IsSuccess.Should().BeTrue();
        student.Withdraw().IsFailure.Should().BeTrue("graduated is a terminal state");
        student.Status.Should().Be(StudentStatus.Graduated);
    }
}
