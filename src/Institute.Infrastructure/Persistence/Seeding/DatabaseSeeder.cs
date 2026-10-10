using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace Institute.Infrastructure.Persistence.Seeding;

/// <summary>Idempotent reference data seeding. Only runs when the database has no departments.</summary>
internal sealed class DatabaseSeeder(InstituteDbContext db, IDateTimeProvider clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Departments.AnyAsync(cancellationToken))
        {
            return;
        }

        var cs = Get(Department.Create("Computer Science", "CS"));
        var math = Get(Department.Create("Mathematics", "MATH"));
        var biz = Get(Department.Create("Business Studies", "BUS"));
        db.Departments.AddRange(cs, math, biz);

        var today = clock.Today;
        db.Instructors.AddRange(
            NewInstructor(cs.Id, "Anita", "Sharma", "anita.sharma@institute.local", "+919800000001", today),
            NewInstructor(math.Id, "Rahul", "Mehta", "rahul.mehta@institute.local", "+919800000002", today),
            NewInstructor(biz.Id, "Priya", "Nair", "priya.nair@institute.local", "+919800000003", today));

        db.Courses.AddRange(
            NewCourse("CS-101", "Programming Fundamentals with C#", "Variables, control flow, OOP and collections.", 12, 15000m, cs.Id),
            NewCourse("CS-201", "Web APIs with ASP.NET Core", "REST, EF Core, testing and deployment.", 10, 22000m, cs.Id),
            NewCourse("MATH-101", "Discrete Mathematics", "Logic, sets, combinatorics and graphs.", 8, 9000m, math.Id),
            NewCourse("BUS-110", "Principles of Accounting", "Double-entry bookkeeping and financial statements.", 8, 8000m, biz.Id));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Instructor NewInstructor(
        DepartmentId departmentId, string firstName, string lastName, string email, string phone, DateOnly today) =>
        Get(Instructor.Hire(
            Get(PersonName.Create(firstName, lastName)),
            Get(Email.Create(email)),
            Get(PhoneNumber.Create(phone)),
            departmentId,
            today,
            today));

    private static Course NewCourse(string code, string title, string description, int weeks, decimal fee, DepartmentId departmentId) =>
        Get(Course.Create(Get(CourseCode.Create(code)), title, description, weeks, Get(Money.Create(fee)), departmentId));

    private static T Get<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw new DomainException($"Invalid seed data: {result.Error.Description}");
}
