using System.Text.Json;
using ProfileService.Application.Exceptions;

namespace ProfileService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;

        try
        {
            await _next(context);
        }
        catch (DomainValidationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message, traceId);
        }
        catch (ConflictException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "CONFLICT", ex.Message, traceId);
        }
        catch (NotAuthenticatedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message, traceId);
        }
        catch (NotAuthorizedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "FORBIDDEN", ex.Message, traceId);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message, traceId);
        }
        catch (DependencyUnavailableException ex)
        {
            _logger.LogError(ex, "Dependency unavailable on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status503ServiceUnavailable, "DEPENDENCY_UNAVAILABLE", ex.Message, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR",
                "An unexpected error occurred.", traceId);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context, int statusCode, string code, string message, string traceId)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { code, message, traceId }));
    }
}
