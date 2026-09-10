using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using ProfileService.Api.Middleware;
using ProfileService.Infrastructure;
using ProfileService.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Prefer ASPNETCORE_URLS from systemd; avoid duplicate Kestrel endpoint conflicts.
    builder.WebHost.ConfigureKestrel(options => { /* defaults from ASPNETCORE_URLS */ });

    var logFile = builder.Configuration["Logging:File"];
    if (string.IsNullOrWhiteSpace(logFile))
        logFile = Path.Combine(builder.Environment.ContentRootPath, "logs", "app-.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: logFile,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var traceId = context.HttpContext.TraceIdentifier;
            var message = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request payload.";

            return new BadRequestObjectResult(new { code = "VALIDATION_ERROR", message, traceId });
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Profile Service API (سامانه اطلاعات کاربران)",
            Version = "v1",
            Description = "مرجع واحد اطلاعات هویتی و تکمیلی کاربران مبتنی بر SSO وزارت کشور. کد ملی کلید اصلی است."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT issued by sso-login-service."
        });
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Name = "X-Api-Key",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Internal API key for backend services (SSO, other systems)."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            },
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        });

        if (builder.Environment.IsDevelopment())
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        }
    });

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
        .AddDbContextCheck<ProfileDbContext>("database", tags: new[] { "ready" });

    var app = builder.Build();

    // Apply migrations but do not prevent Kestrel from binding if DB is briefly unavailable.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        try
        {
            var cs = app.Configuration.GetConnectionString("Profile") ?? "";
            var masked = System.Text.RegularExpressions.Regex.Replace(
                cs, @"Password=[^;]*", "Password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            logger.LogInformation("Using connection: {Connection}", masked);

            if (db.Database.CanConnect())
            {
                db.Database.Migrate();
                logger.LogInformation("Database migrations applied.");
            }
            else
            {
                logger.LogWarning("Database not reachable yet — service will start; retry migrate on next restart.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migrate failed — service will still listen. Fix DB then restart.");
        }
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    });

    app.UseSerilogRequestLogging();
    app.UseRouting();

    if (app.Configuration.GetValue("Swagger:Enabled", true)
        || app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Profile Service v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
        app.UseCors("AllowAll");
    else
        app.UseCors("AllowFrontend");

    app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
    app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));

    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
    app.MapHealthChecks("/api/health", new HealthCheckOptions
    {
        Predicate = _ => true
    });

    Log.Information("Profile Service listening (ASPNETCORE_URLS / Kestrel).");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Profile Service failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
