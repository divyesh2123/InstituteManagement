using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Payments;
using Institute.Domain.Shared;
using Institute.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Institute.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(InstituteDbContext db) : IDepartmentRepository
{
    public Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken = default) =>
        db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(DepartmentId id, CancellationToken cancellationToken = default) =>
        db.Departments.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.Departments.AnyAsync(d => d.Code == code, cancellationToken);

    public void Add(Department department) => db.Departments.Add(department);
}

internal sealed class InstructorRepository(InstituteDbContext db) : IInstructorRepository
{
    public Task<Instructor?> GetByIdAsync(InstructorId id, CancellationToken cancellationToken = default) =>
        db.Instructors.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        db.Instructors.AnyAsync(i => i.Email == email, cancellationToken);

    public void Add(Instructor instructor) => db.Instructors.Add(instructor);
}

internal sealed class CourseRepository(InstituteDbContext db) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken = default) =>
        db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(CourseCode code, CancellationToken cancellationToken = default) =>
        db.Courses.AnyAsync(c => c.Code == code, cancellationToken);

    public void Add(Course course) => db.Courses.Add(course);
}

internal sealed class StudentRepository(InstituteDbContext db) : IStudentRepository
{
    public Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default) =>
        db.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, StudentId? excludeStudentId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Students.Where(s => s.Email == email);

        if (excludeStudentId is { } excluded)
        {
            query = query.Where(s => s.Id != excluded);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(Student student) => db.Students.Add(student);

    public void Remove(Student student) => db.Students.Remove(student);
}

internal sealed class BatchRepository(InstituteDbContext db) : IBatchRepository
{
    public Task<Batch?> GetByIdAsync(BatchId id, CancellationToken cancellationToken = default) =>
        db.Batches.Include(b => b.Enrollments).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void Add(Batch batch) => db.Batches.Add(batch);
}

internal sealed class FeePaymentRepository(InstituteDbContext db) : IFeePaymentRepository
{
    public Task<FeePayment?> GetByIdAsync(FeePaymentId id, CancellationToken cancellationToken = default) =>
        db.FeePayments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(FeePayment payment) => db.FeePayments.Add(payment);
}
