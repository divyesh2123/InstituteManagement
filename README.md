# Institute Management API

A Domain-Driven Design (DDD) Web API for managing an institute: departments, instructors, courses,
students, batches (course offerings), enrollments and fee payments.

**Stack:** .NET 8 · ASP.NET Core · EF Core 8 (Code First) · SQL Server LocalDB · MediatR (CQRS) ·
FluentValidation · Serilog · xUnit · NetArchTest

See [`docs/TICKETS.md`](docs/TICKETS.md) for the implementation tickets and backlog.

## Architecture

```
src/
  Institute.Domain          Aggregates, value objects, domain events, repository interfaces. No framework dependencies.
  Institute.Application     Use cases (commands/queries), validators, pipeline behaviors, DTOs.
  Institute.Infrastructure  EF Core DbContext, configurations, migrations, repositories, interceptors, seeding.
  Institute.Api             Controllers, ProblemDetails, Serilog, Swagger, health checks, rate limiting.
tests/
  Institute.Domain.Tests         Business rule unit tests.
  Institute.ArchitectureTests    Enforces the layer dependency rules.
```

Dependencies point inwards only: `Api → Infrastructure → Application → Domain`.

### Aggregates

| Aggregate    | Key rules |
|--------------|-----------|
| `Department` | Unique upper-case code |
| `Instructor` | Unique email; inactive instructors can't be assigned to batches |
| `Course`     | Unique code (e.g. `CS-101`), fee as `Money`, 1–208 weeks |
| `Student`    | Unique email & registration number (`STU-2026-XXXXXX`), minimum age 10, guarded status lifecycle, soft delete |
| `Batch`      | Owns `Enrollment`s; capacity, no duplicates, only active students, Planned → Ongoing → Completed / Cancelled |
| `FeePayment` | Amount > 0, student must be enrolled in the batch, course currency, refund once |

### Key design decisions

- **Strongly-typed IDs** (`StudentId`, `BatchId`, …) prevent mixing up identifiers.
- **Result pattern:** business rule violations are returned as `Result` failures and mapped to 400/404/409 `ProblemDetails`. Exceptions are reserved for genuinely unexpected errors.
- **CQRS:** commands load aggregates through repositories and save through `IUnitOfWork`. Queries project straight to DTOs through `IReadDbContext` with no tracking.
- **Domain events** are published via MediatR *after* `SaveChanges` succeeds (`PublishDomainEventsInterceptor`).
- **Auditing & soft delete:** `CreatedOnUtc` and `ModifiedOnUtc` are set automatically. Deleting a student sets `IsDeleted`, and a global query filter hides deleted students.
- **Optimistic concurrency:** each aggregate table has a `rowversion` column; a conflicting update returns 409.
- **Database integrity:** unique and filtered indexes, foreign keys with `Restrict`, check constraints, `decimal(18,2)`, enums stored as strings, all tables in the `institute` schema.

## Running locally

Prerequisites: .NET 8 SDK (or newer SDK with the .NET 8 runtime) and SQL Server LocalDB.

```powershell
dotnet tool restore
dotnet run --project src/Institute.Api --launch-profile http
```

In `Development`, the app applies migrations and seeds reference data automatically.
The database is `InstituteDb` on `(localdb)\MSSQLLocalDB`.

- Swagger UI: http://localhost:5294/swagger
- Liveness: http://localhost:5294/health/live
- Readiness (includes a DB check): http://localhost:5294/health/ready

### Tests

```powershell
dotnet test
```

## Migrations

```powershell
# add a migration after changing the model
dotnet ef migrations add <Name> --project src/Institute.Infrastructure --startup-project src/Institute.Infrastructure --output-dir Persistence/Migrations

# apply to the database
dotnet ef database update --project src/Institute.Infrastructure --startup-project src/Institute.Infrastructure

# production: generate an idempotent script or a migration bundle
dotnet ef migrations script --idempotent -o migrate.sql --project src/Institute.Infrastructure --startup-project src/Institute.Infrastructure
dotnet ef migrations bundle -o efbundle.exe --project src/Institute.Infrastructure --startup-project src/Institute.Infrastructure
```

The design-time factory reads `ConnectionStrings__InstituteDb` from the environment and falls back to LocalDB.

## Configuration

| Key | Default | Notes |
|-----|---------|-------|
| `ConnectionStrings:InstituteDb` | *(empty in base config)* | Required. Set via env var / secret store outside Development |
| `Database:ApplyMigrationsOnStartup` | `false` (`true` in Dev) | Prefer scripts or bundles in production |
| `Database:SeedData` | `false` (`true` in Dev) | Idempotent |
| `Database:MaxRetryCount` | `5` | SQL transient-fault retries |
| `Database:CommandTimeoutSeconds` | `30` | |
| `Cors:AllowedOrigins` | `[]` | |
| `RateLimiting:PermitLimit` / `WindowSeconds` | `100` / `60` | Fixed window per client IP |
| `Serilog` | Console + rolling file `logs/` | |

## API overview (`/api/v1`)

| Method | Route | Description |
|--------|-------|-------------|
| GET/POST | `/departments`, `/departments/{id}` | List, get, create |
| GET/POST | `/instructors`, `/instructors/{id}` | List (search, department, active, paging), get, hire |
| POST | `/instructors/{id}/deactivate` | |
| GET/POST/PUT | `/courses`, `/courses/{id}` | List (search, paging), get, create, update |
| PUT | `/courses/{id}/fee` | Change fee |
| POST | `/courses/{id}/activate`, `/courses/{id}/deactivate` | |
| GET/POST/PUT/DELETE | `/students`, `/students/{id}` | List (search by name/email/reg-no, status, paging), get, register, update, soft delete |
| POST | `/students/{id}/status` | `Active`, `Suspended`, `Graduated`, `Withdrawn` |
| GET | `/students/{id}/payments`, `/students/{id}/fee-summary` | Payment history; fee vs paid vs due per batch |
| GET/POST | `/batches`, `/batches/{id}` | List, get with enrollments, schedule |
| POST | `/batches/{id}/start`, `/complete`, `/cancel` | Lifecycle |
| PUT | `/batches/{id}/instructor` | Reassign instructor |
| POST/DELETE | `/batches/{id}/enrollments`, `/batches/{id}/enrollments/{studentId}` | Enroll / cancel enrollment |
| GET/POST | `/payments`, `/payments/{id}` | Record, get |
| POST | `/payments/{id}/refund` | |

Errors use RFC 7807 `application/problem+json` and include `errorCode` and `traceId`.

## Next steps (backlog)

Authentication and authorization (JWT / Entra ID), a transactional outbox, integration tests,
an attendance and exams bounded context, and a CI pipeline. See `docs/TICKETS.md`.
