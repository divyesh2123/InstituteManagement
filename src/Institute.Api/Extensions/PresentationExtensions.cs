using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Institute.Api.Infrastructure;
using Institute.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Institute.Api.Extensions;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    [Range(1, 100_000)]
    public int PermitLimit { get; init; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    [Range(0, 1000)]
    public int QueueLimit { get; init; }
}

internal static class PresentationExtensions
{
    public const string RateLimitPolicy = "fixed";

    public static IServiceCollection AddPresentation(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            });

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Institute Management API",
                Version = "v1",
                Description = "Departments, instructors, courses, students, batches, enrollments and fee payments.",
            });
            options.SupportNonNullableReferenceTypes();
        });

        var cors = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (cors.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(cors.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        }));

        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var rateLimit = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimit.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                    QueueLimit = rateLimit.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }));
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        if (!environment.IsDevelopment())
        {
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });
        }

        return services;
    }

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.DocumentTitle = "Institute Management API");
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseSecurityHeaders();
        app.UseSerilogRequestLogging(options =>
            options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
                diagnostics.Set("TraceId", httpContext.TraceIdentifier));

        app.UseResponseCompression();
        app.UseCors();
        app.UseRateLimiter();

        app.MapControllers().RequireRateLimiting(RateLimitPolicy);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(DependencyInjection.ReadyHealthCheckTag),
            ResponseWriter = WriteHealthResponse,
        });

        return app;
    }

    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
