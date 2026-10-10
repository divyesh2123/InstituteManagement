using System.ComponentModel.DataAnnotations;

namespace Institute.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string ConnectionStringName = "InstituteDb";

    /// <summary>Apply pending EF Core migrations at startup. Recommended for development only.</summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    /// <summary>Seed reference data (departments, instructors, courses) when the database is empty.</summary>
    public bool SeedData { get; init; }

    [Range(0, 10)]
    public int MaxRetryCount { get; init; } = 5;

    [Range(5, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    public bool EnableSensitiveDataLogging { get; init; }
}
