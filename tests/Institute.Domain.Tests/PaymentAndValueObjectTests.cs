using Institute.Domain.Batches;
using Institute.Domain.Payments;
using Institute.Domain.Shared;
using Institute.Domain.Students;

namespace Institute.Domain.Tests;

public sealed class PaymentTests
{
    private static (Batch Batch, Student Student) EnrolledStudent()
    {
        var batch = TestData.Value(Batch.Schedule(
            TestData.Course(), TestData.Instructor(), "B1", TestData.Today.AddDays(1), TestData.Today.AddDays(30), 10, TestData.Today));
        var student = TestData.Student();
        batch.Enroll(student, TestData.UtcNow);
        return (batch, student);
    }

    [Fact]
    public void Record_ShouldFail_WhenStudentNotEnrolled()
    {
        var (batch, _) = EnrolledStudent();

        var result = FeePayment.Record(
            batch, StudentId.New(), TestData.Value(Money.Create(100)), PaymentMethod.Cash, null, TestData.UtcNow);

        result.Error.Should().Be(PaymentErrors.StudentNotEnrolled);
    }

    [Fact]
    public void Record_ShouldFail_WhenAmountIsZero()
    {
        var (batch, student) = EnrolledStudent();

        FeePayment.Record(batch, student.Id, Money.Zero(), PaymentMethod.Cash, null, TestData.UtcNow)
            .Error.Should().Be(PaymentErrors.AmountMustBePositive);
    }

    [Fact]
    public void Refund_ShouldOnlyBeAllowedOnce()
    {
        var (batch, student) = EnrolledStudent();
        var payment = TestData.Value(FeePayment.Record(
            batch, student.Id, TestData.Value(Money.Create(500)), PaymentMethod.Upi, "UPI-1", TestData.UtcNow));

        payment.ReceiptNumber.Should().StartWith("RCPT-20261010-");
        payment.Refund("Duplicate", TestData.UtcNow).IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.Refund("Again", TestData.UtcNow).Error.Should().Be(PaymentErrors.AlreadyRefunded);
    }
}

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("  John.Doe@Example.COM ", "john.doe@example.com")]
    [InlineData("a@b.in", "a@b.in")]
    public void Email_ShouldNormalize(string input, string expected) =>
        TestData.Value(Email.Create(input)).Value.Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Email_ShouldRejectInvalid(string input) => Email.Create(input).IsFailure.Should().BeTrue();

    [Fact]
    public void Phone_ShouldStripSeparators() =>
        TestData.Value(PhoneNumber.Create("+91 (982) 501-2345")).Value.Should().Be("+919825012345");

    [Fact]
    public void Money_ShouldHaveValueEquality_AndRejectCurrencyMismatch()
    {
        var a = TestData.Value(Money.Create(10.005m, "inr"));
        var b = TestData.Value(Money.Create(10.01m, "INR"));
        var usd = TestData.Value(Money.Create(1, "USD"));

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.Add(usd).Error.Should().Be(SharedErrors.Money.CurrencyMismatch);
        Money.Create(-1).IsFailure.Should().BeTrue();
    }
}
