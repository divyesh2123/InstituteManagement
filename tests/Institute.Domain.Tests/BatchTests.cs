using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Students;

namespace Institute.Domain.Tests;

public sealed class BatchTests
{
    private static Batch NewBatch(int capacity = 2) =>
        TestData.Value(Batch.Schedule(
            TestData.Course(), TestData.Instructor(), "Morning", TestData.Today.AddDays(7), TestData.Today.AddDays(90), capacity, TestData.Today));

    [Fact]
    public void Schedule_ShouldFail_WhenEndDateBeforeStart()
    {
        var result = Batch.Schedule(
            TestData.Course(), TestData.Instructor(), "X", TestData.Today.AddDays(10), TestData.Today.AddDays(5), 10, TestData.Today);

        result.Error.Should().Be(BatchErrors.InvalidDateRange);
    }

    [Fact]
    public void Schedule_ShouldFail_WhenCourseInactive()
    {
        var course = TestData.Course();
        course.Deactivate();

        var result = Batch.Schedule(
            course, TestData.Instructor(), "X", TestData.Today.AddDays(1), TestData.Today.AddDays(5), 10, TestData.Today);

        result.Error.Should().Be(CourseErrors.Inactive);
    }

    [Fact]
    public void Enroll_ShouldAddEnrollment_AndRaiseEvent()
    {
        var batch = NewBatch();

        var result = batch.Enroll(TestData.Student(), TestData.UtcNow);

        result.IsSuccess.Should().BeTrue();
        batch.ActiveEnrollmentCount.Should().Be(1);
        batch.DomainEvents.Should().ContainSingle(e => e is StudentEnrolledDomainEvent);
    }

    [Fact]
    public void Enroll_ShouldRejectDuplicates()
    {
        var batch = NewBatch();
        var student = TestData.Student();
        batch.Enroll(student, TestData.UtcNow);

        batch.Enroll(student, TestData.UtcNow).Error.Should().Be(BatchErrors.AlreadyEnrolled);
    }

    [Fact]
    public void Enroll_ShouldRejectWhenFull()
    {
        var batch = NewBatch(capacity: 1);
        batch.Enroll(TestData.Student("a@example.com"), TestData.UtcNow);

        batch.Enroll(TestData.Student("b@example.com"), TestData.UtcNow).Error.Should().Be(BatchErrors.Full);
    }

    [Fact]
    public void Enroll_ShouldRejectInactiveStudent()
    {
        var student = TestData.Student();
        student.Suspend();

        NewBatch().Enroll(student, TestData.UtcNow).Error.Should().Be(StudentErrors.NotActive);
    }

    [Fact]
    public void CancelledEnrollment_ShouldFreeSeat_AndAllowReEnrollment()
    {
        var batch = NewBatch(capacity: 1);
        var student = TestData.Student();
        batch.Enroll(student, TestData.UtcNow);

        batch.CancelEnrollment(student.Id).IsSuccess.Should().BeTrue();
        batch.AvailableSeats.Should().Be(1);
        batch.Enroll(student, TestData.UtcNow).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Lifecycle_ShouldCompleteEnrollments_AndBlockChangesWhenClosed()
    {
        var batch = NewBatch();
        batch.Enroll(TestData.Student(), TestData.UtcNow);

        batch.Complete().IsFailure.Should().BeTrue("a planned batch must be started first");
        batch.Start().IsSuccess.Should().BeTrue();
        batch.Complete().IsSuccess.Should().BeTrue();

        batch.Enrollments.Should().OnlyContain(e => e.Status == EnrollmentStatus.Completed);
        batch.Enroll(TestData.Student("late@example.com"), TestData.UtcNow).IsFailure.Should().BeTrue();
        batch.Cancel().IsFailure.Should().BeTrue();
    }
}
