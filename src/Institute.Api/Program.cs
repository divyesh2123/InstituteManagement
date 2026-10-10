using Institute.Api.Extensions;
using Institute.Application;
using Institute.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPresentation(builder.Configuration, builder.Environment);

    var app = builder.Build();

    await app.Services.InitializeDatabaseAsync();

    app.UsePresentation();

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
