using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("Instructors");
        builder.ConfigureAggregateRoot<Instructor, InstructorId>();

        builder.ComplexProperty(i => i.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("FirstName").HasMaxLength(PersonName.MaxLength).IsRequired();
            name.Property(n => n.LastName).HasColumnName("LastName").HasMaxLength(PersonName.MaxLength).IsRequired();
        });

        builder.Property(i => i.Email).IsRequired();
        builder.Property(i => i.Phone).IsRequired();
        builder.Property(i => i.HireDate).IsRequired();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(i => i.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.Email).IsUnique();
        builder.HasIndex(i => new { i.DepartmentId, i.IsActive });
    }
}
