using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Institute.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> tooling only. Reads the connection string from the
/// <c>ConnectionStrings__InstituteDb</c> environment variable, falling back to LocalDB.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InstituteDbContext>
{
    private const string LocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=InstituteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public InstituteDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{DatabaseOptions.ConnectionStringName}")
            ?? LocalDbConnectionString;

        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", InstituteDbContext.Schema))
            .Options;

        return new InstituteDbContext(options);
    }
}
