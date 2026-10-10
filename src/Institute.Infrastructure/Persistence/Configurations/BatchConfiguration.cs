using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Instructors;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("Batches", t =>
        {
            t.HasCheckConstraint("CK_Batches_DateRange", "[EndDate] > [StartDate]");
            t.HasCheckConstraint("CK_Batches_Capacity", $"[Capacity] BETWEEN {Batch.MinCapacity} AND {Batch.MaxCapacity}");
        });
        builder.ConfigureAggregateRoot<Batch, BatchId>();

        builder.Property(b => b.Name).HasMaxLength(Batch.NameMaxLength).IsRequired();
        builder.Property(b => b.Status).IsRequired();

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(b => b.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Instructor>()
            .WithMany()
            .HasForeignKey(b => b.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Enrollments)
            .WithOne()
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Enrollments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(b => new { b.CourseId, b.Status });
        builder.HasIndex(b => b.InstructorId);
    }
}

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).IsRequired();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // A student can hold at most one non-cancelled enrollment per batch.
        builder.HasIndex(e => new { e.BatchId, e.StudentId })
            .IsUnique()
            .HasFilter($"[Status] <> '{nameof(EnrollmentStatus.Cancelled)}'");

        builder.HasIndex(e => e.StudentId);
    }
}
