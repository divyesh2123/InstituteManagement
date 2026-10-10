using Institute.Application.Abstractions;
using Institute.Domain.Abstractions;
using Institute.Domain.Batches;
using Institute.Domain.Courses;
using Institute.Domain.Departments;
using Institute.Domain.Instructors;
using Institute.Domain.Payments;
using Institute.Domain.Students;
using Institute.Infrastructure.Persistence;
using Institute.Infrastructure.Persistence.Interceptors;
using Institute.Infrastructure.Persistence.Repositories;
using Institute.Infrastructure.Persistence.Seeding;
using Institute.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Institute.Infrastructure;

public static class DependencyInjection
{
    public const string ReadyHealthCheckTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DatabaseOptions.ConnectionStringName}' is not configured.");
        }

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<PublishDomainEventsInterceptor>();

        services.AddDbContext<InstituteDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", InstituteDbContext.Schema);
                sql.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                if (dbOptions.MaxRetryCount > 0)
                {
                    sql.EnableRetryOnFailure(dbOptions.MaxRetryCount);
                }
            });

            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<PublishDomainEventsInterceptor>());

            // Enrollments of soft-deleted students are kept for history; the filter mismatch is intentional.
            options.ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InstituteDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<InstituteDbContext>());

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IFeePaymentRepository, FeePaymentRepository>();

        services.AddScoped<DatabaseSeeder>();

        services.AddHealthChecks()
            .AddDbContextCheck<InstituteDbContext>("database", tags: [ReadyHealthCheckTag]);

        return services;
    }

    /// <summary>Applies migrations and seeds data according to <see cref="DatabaseOptions"/>.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DependencyInjection));

        if (options.ApplyMigrationsOnStartup)
        {
            var db = scope.ServiceProvider.GetRequiredService<InstituteDbContext>();
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
                await db.Database.MigrateAsync(cancellationToken);
            }
        }

        if (options.SeedData)
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
        }
    }
}
