using System.Text.Json;
using Fatturazione.Domain.Exceptions;

namespace Fatturazione.Api.Middleware;

/// <summary>
/// Middleware that catches domain exceptions and maps them to appropriate HTTP responses.
/// Implements the SBA two-tier error handling pattern:
///   - Tier 1 (DomainException subtypes): expected business errors, logged at Information level
///   - Tier 2 (all other exceptions): unexpected errors, logged at Error level
/// </summary>
public class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DomainExceptionMiddleware(
        RequestDelegate next,
        ILogger<DomainExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation(
                "Not found: {Entity} - {Message}",
                ex.Entity ?? "Unknown", ex.Message);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
        }
        catch (InvalidInputException ex)
        {
            _logger.LogInformation(
                "Validation failed: {Message}",
                ex.Message);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errorDict = ex.Errors
                .Select((e, i) => new { Key = $"Error{i}", Value = new[] { e } })
                .ToDictionary(x => x.Key, x => x.Value);

            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Errori di validazione",
                status = 400,
                errors = errorDict
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails, JsonOptions));
        }
        catch (ForbiddenOperationException ex)
        {
            _logger.LogInformation(
                "Forbidden operation: {Operation} on {Entity} - {Reason}",
                ex.Operation ?? "Unknown",
                ex.Entity ?? "Unknown",
                ex.Reason ?? ex.Message);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
        }
        catch (ConflictException ex)
        {
            _logger.LogInformation(
                "Conflict: {Entity} - {Reason}",
                ex.Entity ?? "Unknown",
                ex.Reason ?? ex.Message);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
        }
        catch (DomainException ex)
        {
            // Catch-all for any DomainException subtype not explicitly handled above
            _logger.LogInformation(
                "Domain error: {Message}",
                ex.Message);

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions));
        }
    }
}

/// <summary>
/// Extension methods for registering DomainExceptionMiddleware in the request pipeline.
/// </summary>
public static class DomainExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds domain exception handling middleware to the pipeline.
    /// Should be registered early in the pipeline so it catches exceptions from all downstream middleware.
    /// </summary>
    public static IApplicationBuilder UseDomainExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DomainExceptionMiddleware>();
    }
}
