# Institute Management – Implementation Tickets

Stack: .NET 8, ASP.NET Core Web API, EF Core 8 (Code First), SQL Server LocalDB, MediatR (CQRS), FluentValidation, Serilog, xUnit.
Architecture: Domain-Driven Design with Clean Architecture layering
(`Domain` ← `Application` ← `Infrastructure` ← `Api`).

Status legend: ☐ To do · ☑ Done

---

## EPIC 0 – Foundation

### ☑ INST-001 Solution scaffolding
**As a** developer **I want** a layered solution **so that** dependencies only point inwards.
- Projects: `Institute.Domain`, `Institute.Application`, `Institute.Infrastructure`, `Institute.Api`, `Institute.Domain.Tests`, `Institute.ArchitectureTests`.
- `Directory.Build.props` (net8.0, nullable, implicit usings, warnings as errors, analyzers).
- Central Package Management (`Directory.Packages.props`).
- `.editorconfig`, `.gitignore`, local tool manifest with `dotnet-ef`.

**Acceptance:** `dotnet build` passes with 0 warnings.

### ☑ INST-002 Domain building blocks (Shared Kernel)
- `Entity<TId>`, `AggregateRoot<TId>` (domain event collection), `ValueObject`.
- Strongly-typed IDs (`record struct StudentId(Guid Value)` …).
- `IDomainEvent`, `DomainException`, `Result` / `Result<T>` / `Error`.
- `IAuditableEntity` (CreatedAtUtc, ModifiedAtUtc), `ISoftDeletable`.
- `IDateTimeProvider` abstraction.

**Acceptance:** building blocks have no dependency on EF Core or any framework.

---

## EPIC 1 – Domain model

### ☑ INST-003 Department aggregate
- Fields: Id, Name (≤100), Code (≤10, unique, upper-case).
- Behaviour: `Create`, `Rename`.

### ☑ INST-004 Instructor aggregate
- Fields: Id, FullName (PersonName VO), Email (VO, unique), Phone (VO), DepartmentId, HireDate, IsActive.
- Behaviour: `Hire`, `UpdateContact`, `TransferTo(department)`, `Deactivate`.

### ☑ INST-005 Course aggregate
- Fields: Id, Code (CourseCode VO, unique), Title, Description, DurationInWeeks, Fee (Money VO), DepartmentId, IsActive.
- Behaviour: `Create`, `UpdateDetails`, `ChangeFee`, `Activate` / `Deactivate`.
- Rules: fee ≥ 0, duration 1–208 weeks.

### ☑ INST-006 Student aggregate
- Fields: Id, RegistrationNumber (unique, `STU-yyyy-xxxxxx`), FullName, Email (unique), Phone, DateOfBirth, Address (owned VO), Status (Active / Suspended / Graduated / Withdrawn), soft delete.
- Behaviour: `Register`, `UpdateProfile`, `ChangeAddress`, `Suspend`, `Reactivate`, `Graduate`, `Withdraw`.
- Rules: age ≥ 10 at registration; status transitions guarded.
- Raises `StudentRegisteredDomainEvent`.

### ☑ INST-007 Batch aggregate (course offering) with Enrollments
- Fields: Id, CourseId, InstructorId, Name, StartDate, EndDate, Capacity, Status (Planned / Ongoing / Completed / Cancelled), `Enrollments` (child entities).
- Behaviour: `Schedule`, `AssignInstructor`, `Enroll(student)`, `CancelEnrollment`, `Start`, `Complete`, `Cancel`.
- Rules: end > start, capacity 1–500, no enrollment when full / completed / cancelled, a student can't be enrolled twice.
- Raises `StudentEnrolledDomainEvent`.

### ☑ INST-008 FeePayment aggregate
- Fields: Id, StudentId, BatchId, Amount (Money), PaidOnUtc, Method (Cash / Card / BankTransfer / Upi), ReceiptNumber (unique), Status (Completed / Refunded).
- Behaviour: `Record`, `Refund(reason)`.
- Rules: amount > 0, can't refund twice.

---

## EPIC 2 – Persistence (EF Core Code First)

### ☑ INST-009 DbContext & entity configurations
- `InstituteDbContext` with `IEntityTypeConfiguration<>` per aggregate.
- Strongly-typed ID value converters, owned types for VOs, `decimal(18,2)`, string lengths, unique indexes, `rowversion` concurrency token, enums stored as strings.
- Global query filter for soft-deleted students.
- Schema: `institute`.

### ☑ INST-010 Repositories, Unit of Work & interceptors
- Repository per aggregate root (interfaces in Domain, implementations in Infrastructure).
- `IUnitOfWork` implemented by the DbContext.
- `AuditableEntitiesInterceptor` (timestamps), `SoftDeleteInterceptor`, `PublishDomainEventsInterceptor` (dispatches domain events via MediatR after the save succeeds).

### ☑ INST-011 Migrations & seeding
- `InitialCreate` migration.
- Optional apply-migrations-on-startup (config flag `Database:ApplyMigrationsOnStartup`).
- Idempotent seed: departments, instructors, courses.
- Connection string → LocalDB `(localdb)\MSSQLLocalDB`, database `InstituteDb`, retry-on-failure enabled.

---

## EPIC 3 – Application layer (CQRS)

### ☑ INST-012 Application pipeline
- MediatR commands/queries, `ValidationBehavior` (FluentValidation), `LoggingBehavior` (timing and failures).
- `PagedList<T>` + paging query parameters.
- Errors are returned as `Result` failures, not exceptions.

### ☑ INST-013 Use cases
- Departments: create, list, get.
- Instructors: hire, list, get, deactivate.
- Courses: create, update, change fee, list (search + paging), get.
- Students: register, update, change status, list (search + paging), get, delete (soft).
- Batches: schedule, start, complete, cancel, enroll student, cancel enrollment, get with enrollments, list.
- Payments: record, refund, list by student, get student fee summary (due vs paid).
- Read side uses `AsNoTracking` projections to DTOs.

---

## EPIC 4 – API

### ☑ INST-014 REST endpoints
- Controllers under `/api/v1/...`; `201 Created` with location, `204` for updates, `ProblemDetails` for errors.
- `Result` → HTTP mapping (`NotFound` → 404, `Conflict` → 409, `Validation` → 400).

### ☑ INST-015 Cross-cutting production concerns
- Global exception handler (`IExceptionHandler`) → RFC 7807 ProblemDetails with traceId.
- Serilog (console + rolling file) with request logging.
- Swagger/OpenAPI (Development only by default).
- Health checks: `/health/live`, `/health/ready` (DB check).
- Rate limiting (fixed window), CORS policy from config, HSTS/HTTPS redirection, response compression.
- Strongly-typed options validated on startup.

---

## EPIC 5 – Quality

### ☑ INST-016 Domain unit tests
- Student lifecycle, Batch enrollment rules, Payment refund, value objects.

### ☑ INST-017 Architecture tests
- Domain depends on nothing; Application does not depend on Infrastructure/Api; Infrastructure does not depend on Api.

### ☑ INST-018 Documentation
- README: architecture, how to run, migrations, endpoints.

---

## Backlog (not in this iteration)
- ☐ INST-019 Authentication & authorization (JWT / Entra ID, roles: Admin, Staff, Instructor).
- ☐ INST-020 Transactional outbox for integration events.
- ☐ INST-021 Integration tests with Testcontainers / LocalDB.
- ☐ INST-022 Attendance & exam results bounded context.
- ☐ INST-023 CI pipeline (build, test, migration bundle).
