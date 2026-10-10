using Institute.Domain.Departments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.ConfigureAggregateRoot<Department, DepartmentId>();

        builder.Property(d => d.Name).HasMaxLength(Department.NameMaxLength).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(Department.CodeMaxLength).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique();
    }
}
