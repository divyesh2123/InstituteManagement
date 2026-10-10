using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Payments;
using Institute.Domain.Shared;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Institute.Infrastructure.Persistence;

/// <summary>
/// Model-wide conventions: strongly-typed IDs ⇄ GUID, single-value value objects ⇄ string,
/// enums ⇄ string and sensible defaults for decimals and strings.
/// </summary>
internal static class ModelConventions
{
    private const int EnumMaxLength = 20;

    public static void Apply(ModelConfigurationBuilder builder)
    {
        builder.Properties<DepartmentId>().HaveConversion<DepartmentIdConverter>();
        builder.Properties<InstructorId>().HaveConversion<InstructorIdConverter>();
        builder.Properties<CourseId>().HaveConversion<CourseIdConverter>();
        builder.Properties<StudentId>().HaveConversion<StudentIdConverter>();
        builder.Properties<BatchId>().HaveConversion<BatchIdConverter>();
        builder.Properties<EnrollmentId>().HaveConversion<EnrollmentIdConverter>();
        builder.Properties<FeePaymentId>().HaveConversion<FeePaymentIdConverter>();

        builder.Properties<Email>().HaveConversion<EmailConverter>().HaveMaxLength(Email.MaxLength);
        builder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>().HaveMaxLength(PhoneNumber.MaxLength);
        builder.Properties<CourseCode>().HaveConversion<CourseCodeConverter>().HaveMaxLength(CourseCode.MaxLength);
        builder.Properties<RegistrationNumber>().HaveConversion<RegistrationNumberConverter>().HaveMaxLength(RegistrationNumber.MaxLength);

        builder.Properties<StudentStatus>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);
        builder.Properties<BatchStatus>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);
        builder.Properties<EnrollmentStatus>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);
        builder.Properties<PaymentMethod>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);
        builder.Properties<PaymentStatus>().HaveConversion<string>().HaveMaxLength(EnumMaxLength);

        builder.Properties<decimal>().HavePrecision(18, 2);
        builder.Properties<string>().HaveMaxLength(256);
    }

    /// <summary>Common configuration for every aggregate root: key, optimistic concurrency and ignored events.</summary>
    public static void ConfigureAggregateRoot<TAggregate, TId>(this EntityTypeBuilder<TAggregate> builder)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Ignore(e => e.DomainEvents);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }

    private sealed class DepartmentIdConverter() : ValueConverter<DepartmentId, Guid>(id => id.Value, value => new DepartmentId(value));

    private sealed class InstructorIdConverter() : ValueConverter<InstructorId, Guid>(id => id.Value, value => new InstructorId(value));

    private sealed class CourseIdConverter() : ValueConverter<CourseId, Guid>(id => id.Value, value => new CourseId(value));

    private sealed class StudentIdConverter() : ValueConverter<StudentId, Guid>(id => id.Value, value => new StudentId(value));

    private sealed class BatchIdConverter() : ValueConverter<BatchId, Guid>(id => id.Value, value => new BatchId(value));

    private sealed class EnrollmentIdConverter() : ValueConverter<EnrollmentId, Guid>(id => id.Value, value => new EnrollmentId(value));

    private sealed class FeePaymentIdConverter() : ValueConverter<FeePaymentId, Guid>(id => id.Value, value => new FeePaymentId(value));

    private sealed class EmailConverter() : ValueConverter<Email, string>(e => e.Value, value => Email.Create(value).Value);

    private sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(p => p.Value, value => PhoneNumber.Create(value).Value);

    private sealed class CourseCodeConverter() : ValueConverter<CourseCode, string>(c => c.Value, value => CourseCode.Create(value).Value);

    private sealed class RegistrationNumberConverter()
        : ValueConverter<RegistrationNumber, string>(r => r.Value, value => RegistrationNumber.Create(value).Value);
}
