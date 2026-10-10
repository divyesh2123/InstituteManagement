using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Payments;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Institute.Infrastructure.Persistence;

public sealed class InstituteDbContext(DbContextOptions<InstituteDbContext> options)
    : DbContext(options), IUnitOfWork, IReadDbContext
{
    public const string Schema = "institute";

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Instructor> Instructors => Set<Instructor>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<FeePayment> FeePayments => Set<FeePayment>();

    IQueryable<Department> IReadDbContext.Departments => Departments.AsNoTracking();

    IQueryable<Instructor> IReadDbContext.Instructors => Instructors.AsNoTracking();

    IQueryable<Course> IReadDbContext.Courses => Courses.AsNoTracking();

    IQueryable<Student> IReadDbContext.Students => Students.AsNoTracking();

    IQueryable<Batch> IReadDbContext.Batches => Batches.AsNoTracking();

    IQueryable<Enrollment> IReadDbContext.Enrollments => Enrollments.AsNoTracking();

    IQueryable<FeePayment> IReadDbContext.FeePayments => FeePayments.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstituteDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ModelConventions.Apply(configurationBuilder);
    }
}
