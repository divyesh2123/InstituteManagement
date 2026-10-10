using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", t => t.HasCheckConstraint(
            "CK_Courses_DurationInWeeks", $"[DurationInWeeks] BETWEEN {Course.MinDurationInWeeks} AND {Course.MaxDurationInWeeks}"));
        builder.ConfigureAggregateRoot<Course, CourseId>();

        builder.Property(c => c.Code).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(Course.TitleMaxLength).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(Course.DescriptionMaxLength);

        builder.ComplexProperty(c => c.Fee, fee =>
        {
            fee.Property(m => m.Amount).HasColumnName("FeeAmount");
            fee.Property(m => m.Currency).HasColumnName("FeeCurrency").HasMaxLength(3).IsFixedLength().IsRequired();
        });

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Title);
    }
}
