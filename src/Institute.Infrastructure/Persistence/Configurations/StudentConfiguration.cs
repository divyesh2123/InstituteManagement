using Institute.Domain.Shared;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Institute.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.ConfigureAggregateRoot<Student, StudentId>();

        builder.Property(s => s.RegistrationNumber).IsRequired();

        builder.ComplexProperty(s => s.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("FirstName").HasMaxLength(PersonName.MaxLength).IsRequired();
            name.Property(n => n.LastName).HasColumnName("LastName").HasMaxLength(PersonName.MaxLength).IsRequired();
        });

        builder.Property(s => s.Email).IsRequired();
        builder.Property(s => s.Phone).IsRequired();
        builder.Property(s => s.DateOfBirth).IsRequired();
        builder.Property(s => s.Status).IsRequired();

        builder.ComplexProperty(s => s.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(Address.StreetMaxLength).IsRequired();
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(Address.CityMaxLength).IsRequired();
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(Address.StateMaxLength).IsRequired();
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(Address.PostalCodeMaxLength).IsRequired();
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(Address.CountryMaxLength).IsRequired();
        });

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.RegistrationNumber).IsUnique();

        // Email must be unique among non-deleted students only, so a soft-deleted student's email can be reused.
        builder.HasIndex(s => s.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(s => s.Status);
    }
}
